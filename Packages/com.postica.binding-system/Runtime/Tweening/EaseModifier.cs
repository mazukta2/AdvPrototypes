using System;
using Postica.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Tweening
{
    /// <summary>
    /// A base modifier to create tween modifiers.
    /// </summary>
    [Serializable]
    [HideMember]
    // [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/motion")]
    [TypeDescription("Animates the value to the input value over time using easing functions.")]
    public abstract class EaseModifier<T> : IReadModifier<T>, IDynamicComponent where T : IEquatable<T>
    {
        [Tooltip("Animates this property to the target value over time.")]
        public Data ease;
        protected Func<float, float> EaseFunction;
        protected Func<float, float> InverseEaseFunction;
        
        [NonSerialized]
        private bool _initialized;
        private T _actualOrigin;
        private T _lastTargetValue;
        private T _lastActualValue;
        private float _startTime;
        private float _startTweenTime;
        private float _lastProgressValue;
        private int _remainingRepeats;
        private TweenDirection _direction;
        
        private bool _hasEnded;
        private bool _isAnimating;
        
        ///<inheritdoc/>
        public virtual string Id { get; } = "Tween Animation";

        ///<inheritdoc/>
        public virtual string ShortDataDescription => string.Empty;

        ///<inheritdoc/>
        public BindMode ModifyMode => BindMode.Read;
        
        ///<inheritdoc/>
        public bool IsDynamic => true;

        protected virtual void Initialize(T value)
        {
            if (_initialized)
            {
                return;
            }
            
            _initialized = true;
            (EaseFunction, InverseEaseFunction) = GetEaseFunction(ease.tweenFunction, ease.easeType);
            _actualOrigin = ease.origin.Value;
            _lastActualValue = ease.originType == TweenOriginType.LastCompletedValue ? value : _actualOrigin;
        }

        protected abstract T Lerp(T from, T to, float progress);
        protected virtual bool Equals(T a, T b) => a.Equals(b);
        protected virtual float GetLerpPoint(T actual, T from, T to) => _lastProgressValue;

        public void OnValidate()
        {
            if (Application.isPlaying)
            {
                (EaseFunction, InverseEaseFunction) = GetEaseFunction(ease.tweenFunction, ease.easeType);
            }
        }

        protected float GetNormalizedTime()
        {
            var time = ease.time == TweenTimeScale.Scaled ? Time.time : Time.unscaledTime;
            
            if (!_isAnimating)
            {
                _startTime = time;
                _startTweenTime = time + ease.delay.Value;
                _remainingRepeats = ease.repeatCount.Value;
                if (_remainingRepeats <= 0)
                {
                    _remainingRepeats = int.MaxValue; // Repeat indefinitely
                }
                _direction = ease.direction is TweenDirection.Forward or TweenDirection.PingPong ? TweenDirection.Forward : TweenDirection.Backward;
                _isAnimating = true;
                _hasEnded = false;
                
                ease.onEaseStarted?.Invoke(_lastActualValue);
            }
            
            if(time < _startTweenTime)
            {
                return 0;
            }
            
            var elapsedTime = time - _startTweenTime;
            var duration = ease.duration.Value;
            var normalizedTime = elapsedTime / duration;
            normalizedTime = Mathf.Clamp(normalizedTime, 0, 1);

            return normalizedTime;
        }

        public T ModifyRead(in T value)
        {
            if (!_initialized)
            {
                Initialize(value);
            }

            if (!Equals(_lastTargetValue, value))
            {
                var time = ease.time == TweenTimeScale.Scaled ? Time.time : Time.unscaledTime;
                if (ease.onTargetChange == TargetChangeBehavior.AdaptAnimation)
                {
                    var lerpPoint = GetLerpPoint(_lastActualValue, _actualOrigin, value);
                    var inverseTimeValue = InverseEaseFunction(lerpPoint);
                    _startTime = time - inverseTimeValue * ease.duration.Value;
                    _startTweenTime = _startTime + ease.delay.Value;
                }
                else if (ease.onTargetChange != TargetChangeBehavior.Ignore)
                {
                    _isAnimating = time < _startTweenTime;
                }

                if (ease.onTargetChange == TargetChangeBehavior.RestartAnimation)
                {
                    _actualOrigin = _lastActualValue;
                }

                _lastTargetValue = value;
                _hasEnded = false;
            }
            else if (_hasEnded)
            {
                return _direction == TweenDirection.Backward ? _lastActualValue : value;
            }
            
            var normalizedTime = GetNormalizedTime();
            var progress = _lastProgressValue = EaseFunction(normalizedTime);

            if (ComputeTweenEnd(normalizedTime))
            {
                if (ease.originType == TweenOriginType.LastCompletedValue)
                {
                    _actualOrigin = value;
                }
                
                if(_direction == TweenDirection.Forward)
                {
                    ease.onEaseCompleted?.Invoke(value);
                    _lastActualValue = value;
                    return value;
                }
                
                ease.onEaseCompleted?.Invoke(_lastActualValue);
                return _lastActualValue;
            }
            
            if(_direction == TweenDirection.Backward)
            {
                progress = 1 - progress;
            }
            
            _lastActualValue = Lerp(_actualOrigin, value, progress);
            ease.onEaseUpdated?.Invoke(_lastActualValue);
            return _lastActualValue;
        }
        
        private bool ComputeTweenEnd(float normalizedTime)
        {
            if (normalizedTime >= 1)
            {
                var time = ease.time == TweenTimeScale.Scaled ? Time.time : Time.unscaledTime;
                _startTime = time;
                _startTweenTime = time + ease.delay.Value;

                if (!ComputeNextDirection())
                {
                    _remainingRepeats--;
                }
                
                if (_remainingRepeats <= 0)
                {
                    _hasEnded = true;
                    _isAnimating = false;
                    
                    return true;
                }
            }

            return false;
        }

        private bool ComputeNextDirection()
        {
            if (ease.direction == TweenDirection.Backward || ease.direction == TweenDirection.Forward)
            {
                _direction = ease.direction;
                return false;
            }
            
            if (ease.direction == TweenDirection.PingPong)
            {
                if (_direction == TweenDirection.Backward)
                {
                    return false;
                }
                _direction = TweenDirection.Backward;
            }
            else if (ease.direction == TweenDirection.ReversePingPong)
            {
                if (_direction == TweenDirection.Forward)
                {
                    return false;
                }
                _direction = TweenDirection.Forward;
            }
            
            return true;
        }
        
        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => ModifyRead((T)value);
        
        protected (Func<float, float> forward, Func<float, float> inverse) GetEaseFunction(FunctionType tweenFunction, EaseType easeType)
        {
            if (!_initialized)
            {
                Initialize(_lastActualValue);
                _initialized = true;
            }
            
            var function = TweenSystem.GetEaseFunction(tweenFunction, easeType) ?? ease.customEase.Evaluate;
            var inverseFunction = TweenSystem.GetInverseEaseFunction(tweenFunction, easeType) ?? TweenSystem.GetIterativeInverse(ease.customEase.Evaluate);
            
            return (function, inverseFunction);
        }

        [Serializable]
        public class Data
        {
            // Origin related
            [Tooltip("The behavior when the target value changes during the tween.")]
            public TargetChangeBehavior onTargetChange = TargetChangeBehavior.Ignore;
            [Tooltip("The type of origin to use for the tween.")]
            public TweenOriginType originType = TweenOriginType.LastCompletedValue;
            [Tooltip("The origin value to use for the tween.")]
            public ReadOnlyBind<T> origin;
            
            // Easing functions related
            [Tooltip("The direction of the tween.")]
            public TweenDirection direction = TweenDirection.Forward;
            [Tooltip("The easing function to use for the tween.")]
            public FunctionType tweenFunction;
            [Tooltip("The easing type to use for the tween.")]
            public EaseType easeType;
            [Tooltip("The custom easing function to use for the tween.")]
            public AnimationCurve customEase = AnimationCurve.Linear(0, 0, 1, 1);
            
            // Time related
            [Tooltip("The time scale to use for the tween. Scaled time is susceptible to time scale changes, meaning it will slow down or speed up when the game is paused or slowed down. Unscaled time is not affected by time scale changes, meaning it will always run at the same speed regardless of time scale changes.")]
            public TweenTimeScale time = TweenTimeScale.Scaled;
            [Tooltip("The delay before the tween starts.")]
            public ReadOnlyBind<float> delay;
            [Tooltip("The duration of the tween.")]
            public ReadOnlyBind<float> duration = 1f.Bind();
            [Tooltip("The number of times to repeat the tween. A value of 0 means the tween will repeat indefinitely.")]
            public ReadOnlyBind<int> repeatCount = 1.Bind();
        
            [Tooltip("The event to invoke when the animation completes.")]
            public UnityEvent<T> onEaseCompleted;
            [Tooltip("The event to invoke when the animation starts.")]
            public UnityEvent<T> onEaseStarted;
            [Tooltip("The event to invoke when the animation updates.")]
            public UnityEvent<T> onEaseUpdated;
        }
    }
}
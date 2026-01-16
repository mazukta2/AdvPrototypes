using System;
using System.Runtime.CompilerServices;
using Postica.Common;
using UnityEngine;
using UnityEngine.Events;

namespace Postica.BindingSystem.Tweening
{
    /// <summary>
    /// A base modifier to create chase animations modifiers.
    /// </summary>
    [Serializable]
    [HideMember]
    // [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/motion_smooth")]
    [TypeDescription("Animates the value to smoothly follow the input value over time using damped dynamics.")]
    public abstract class DampedTrackerModifier<T> : IReadModifier<T>, IDynamicComponent where T : IEquatable<T>
    {
        private const float ConvergenceTimeReduxRatio = 0.8f;
        
        [Tooltip("Animates this property to the target value over time.")]
        public Data dampedTracker;
        
        [NonSerialized]
        private bool _initialized;
        private T _actualOrigin;
        private T _lastTargetValue;
        
        private T _currentValue;
        private T _currentVelocity;
        private T _currentAcceleration;
        
        private float _startTime;
        private float _functionDuration;
        private float _potentialEndTime;
        private int _remainingRepetitions;
        
        private bool _hasEnded;
        private bool _isAnimating;
        
        ///<inheritdoc/>
        public virtual string Id { get; } = "Value Smoother";

        ///<inheritdoc/>
        public virtual string ShortDataDescription => string.Empty;

        ///<inheritdoc/>
        public BindMode ModifyMode => BindMode.Read;

        ///<inheritdoc/>
        public bool IsDynamic => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Initialize(T initialValue)
        {
            if (_initialized)
            {
                return;
            }
            
            _initialized = true;
            _actualOrigin = dampedTracker.origin.Value;
            _currentValue = initialValue;
            _functionDuration = TweenSystem.GetApproxTrackingTime(
                dampedTracker.speed.Value, 
                dampedTracker.damping.Value,
                tolerance: dampedTracker.targetEpsilon) * ConvergenceTimeReduxRatio;
        }

        protected abstract T Add(T a, T b);
        protected abstract T Subtract(T a, T b);
        protected abstract T Multiply(T a, float b);
        protected virtual bool Equals(T a, T b, float epsilon) => a.Equals(b);

        public void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            _functionDuration = TweenSystem.GetApproxTrackingTime(
                dampedTracker.speed.Value, 
                dampedTracker.damping.Value,
                tolerance: dampedTracker.targetEpsilon) * ConvergenceTimeReduxRatio;
        }

        protected bool TryStartAnimation()
        {
            var time = dampedTracker.time == TweenTimeScale.Scaled ? Time.time : Time.unscaledTime;

            if (_isAnimating) return time >= _startTime;
            
            _startTime = time + dampedTracker.delay.Value;
            _potentialEndTime = _startTime + _functionDuration;
            _remainingRepetitions = dampedTracker.repeatCount.Value;
            if(_remainingRepetitions <= 0)
            {
                _remainingRepetitions = int.MaxValue; // Loop indefinitely
            }
            _isAnimating = true;
            _hasEnded = false;
                
            dampedTracker.onChaseStarted?.Invoke(_currentValue);

            return time >= _startTime;
        }
        
        protected float GetDeltaTime()
        {
            var deltaTime = dampedTracker.time == TweenTimeScale.Scaled ? Time.deltaTime : Time.unscaledDeltaTime;
            return deltaTime;
        }

        public T ModifyRead(in T value)
        {
            if (!_initialized)
            {
                Initialize(value);
            }

            if (!Equals(_lastTargetValue, value, dampedTracker.targetEpsilon))
            {
                _lastTargetValue = value;
                _remainingRepetitions = dampedTracker.repeatCount.Value;
                if (_remainingRepetitions <= 0)
                {
                    _remainingRepetitions = int.MaxValue; // Loop indefinitely
                }
                if(_hasEnded && dampedTracker.originType == TweenOriginType.PreciseOrigin)
                {
                    _currentValue = _actualOrigin;
                }
                _hasEnded = false;
            }
            else if (_hasEnded)
            {
                return _currentValue;
            }
            
            if (!TryStartAnimation())
            {
                return _currentValue;
            }
            
            var dt = GetDeltaTime();

            if (dt == 0)
            {
                return _currentValue;
            }

            var omega = dampedTracker.speed;
            var zeta = dampedTracker.damping;

            // Compute acceleration (critically damped dynamics)
            var displacement = Subtract(_currentValue, value);
            // acceleration  a = −2ζω v − ω² (x − target)
            var acceleration = Subtract(Multiply(_currentVelocity, -2 * omega * zeta), Multiply(displacement, omega * omega));
            
            
            // Semi-implicit Euler integration
            _currentVelocity = Add(_currentVelocity, Multiply(acceleration, dt));
            _currentValue = Add(_currentValue, Multiply(_currentVelocity, dt));
            
            dampedTracker.onChaseUpdated?.Invoke(_currentValue);
            
            if (ComputeTweenEnd(value))
            {
                if (dampedTracker.originType == TweenOriginType.LastCompletedValue)
                {
                    _actualOrigin = value;
                    _currentValue = value; // Reset to the new origin
                }
                else
                {
                    _currentValue = dampedTracker.onTargetReached switch
                    {
                        TweenOnTargetReach.StayAtTarget => value,
                        TweenOnTargetReach.ResetToOrigin => _actualOrigin,
                        _ => _currentValue
                    };
                }

                dampedTracker.onChaseCompleted?.Invoke(value);
                return value;
            }
            
            return _currentValue;
        }
        
        private bool ComputeTweenEnd(T value)
        {
            if (Time.time < _potentialEndTime) return false;
            if (!Equals(_currentValue, value, dampedTracker.targetEpsilon)) return false;
            
            if (--_remainingRepetitions <= 0)
            {
                _hasEnded = true;
                _isAnimating = false;
                    
                return true;
            }

            var delay = dampedTracker.repeatDelay< 0
                ? dampedTracker.delay.Value
                : dampedTracker.repeatDelay.Value;
            var time = dampedTracker.time == TweenTimeScale.Scaled ? Time.time : Time.unscaledTime;
            _startTime = time + delay;
            _potentialEndTime = _startTime + _functionDuration;
                
            _currentValue = _actualOrigin;
            return false;
        }
        
        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => ModifyRead((T)value);

        [Serializable]
        public class Data
        {
            // Origin related
            [Tooltip("The type of origin to use for the tracker.")]
            public TweenOriginType originType = TweenOriginType.LastCompletedValue;
            [Tooltip("What happens when the animation completes.")]
            public TweenOnTargetReach onTargetReached = TweenOnTargetReach.StayAtTarget;
            [Tooltip("The origin value to use for the tracker.")]
            public ReadOnlyBind<T> origin;
            
            // Dampening related
            [Tooltip("The time scale to use for the tracker. Scaled time is susceptible to time scale changes, meaning it will slow down or speed up when the game is paused or slowed down. Unscaled time is not affected by time scale changes, meaning it will always run at the same speed regardless of time scale changes.")]
            public TweenTimeScale time = TweenTimeScale.Scaled;
            [Bind]
            [Min(0)]
            [Tooltip("The delay before the chasing starts.")]
            public ReadOnlyBind<float> delay;
            [Bind]
            [Range(0.5f, 25f)]
            [Tooltip("How fast the value follows the target. Higher = snappier.")]
            public ReadOnlyBind<float> speed = 8f.Bind();
            [Bind]
            [Range(0f, 5f)]
            [Tooltip("How smoothly the value stops. 1 = critical damping (no overshoot).")]
            public ReadOnlyBind<float> damping = 1f.Bind();
            [Tooltip("How many times the animation should repeat. A value of 1 means it will play once, 2 means it will play twice, and so on. A value of 0 means it will loop indefinitely.")]
            public ReadOnlyBind<int> repeatCount = 1.Bind();
            [Tooltip("How much delay to add between each repetition. A value of -1 means 'same as initial delay', 0 means no delay, and any positive value means the delay in seconds.")]
            public ReadOnlyBind<float> repeatDelay = (-1f).Bind();
            [Tooltip("The epsilon value to use for the tracker. This is used to determine when the tracker has reached the target value. A value of 0.001 means that the tracker will stop when it is within 0.001 of the target value.")]
            public float targetEpsilon = 0.001f;
        
            [Tooltip("The event to invoke when the animation completes.")]
            public UnityEvent<T> onChaseCompleted;
            [Tooltip("The event to invoke when the animation starts.")]
            public UnityEvent<T> onChaseStarted;
            [Tooltip("The event to invoke when the animation updates.")]
            public UnityEvent<T> onChaseUpdated;
        }
    }
}
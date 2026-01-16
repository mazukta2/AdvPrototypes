using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    class UIAnimator
    {
        private class AnimationState
        {
            public int index;
            public UIAnimation animation;
            public double startTime;
            public float duration;

            public bool Update(double time)
            {
                if (startTime == 0)
                {
                    startTime = time;
                    animation.Start();
                    //return false; // <-- let a frame pass to assess the rendering
                }
                var progress = (time - startTime) / duration;
                animation.Update((float)progress);
                if (animation.HasFinished || progress > 1)
                {
                    animation.End();
                    return true;
                }
                return false;
            }
        }

        private List<AnimationState> _animationStates = new List<AnimationState>();
        private HashSet<UIAnimation> _animations = new HashSet<UIAnimation>();

        public void Add(UIAnimation animation)
        {
            if (_animations.Contains(animation))
            {
                return;
            }
            _animations.Add(animation);
            _animationStates.Add(new AnimationState()
            {
                index = _animationStates.Count,
                animation = animation,
                startTime = 0,
                duration = animation.duration,
            });
        }

        public void Remove(UIAnimation animation)
        {
            if (_animations.Remove(animation))
            {
                _animationStates.RemoveAll(s => s.animation == animation);
            }
        }

        public void Clear()
        {
            _animationStates.Clear();
            _animations.Clear();
        }

        public void Update()
        {
            if(_animationStates.Count == 0)
            {
                return;
            }

            var time = EditorApplication.timeSinceStartup;
            for (int i = 0; i < _animationStates.Count; i++)
            {
                if (_animationStates[i].Update(time))
                {
                    _animationStates.RemoveAt(i--);
                }
            }
        }
    }

    abstract class UIAnimation
    {
        public readonly VisualElement element;
        public readonly float duration;
        protected readonly Action<VisualElement> _onStart;
        protected readonly Action<VisualElement> _onEnd;

        public UIAnimation(VisualElement element, float duration,
                           Action<VisualElement> onStartCallback,
                           Action<VisualElement> onEndCallback)
        {
            this.element = element;
            _onStart = onStartCallback;
            _onEnd = onEndCallback;
            this.duration = duration;
        }

        public abstract bool HasFinished { get; }

        public abstract void Update(float normalizedValue);

        public virtual void Start()
        {
            _onStart?.Invoke(element);
        }
        public virtual void End()
        {
            _onEnd?.Invoke(element);
        }
    }

    class UISlideAnimation : UIAnimation
    {
        private readonly Vector3 _slideAmount;
        private Vector3 _endPosition;
        private Vector3 _startPosition;

        private VisualElementExtensions.LayoutState _layoutState;

        public UISlideAnimation(VisualElement element, float duration, Vector2 slideAmount,
                                Action<VisualElement> onStartCallback,
                                Action<VisualElement> onEndCallback) : base(element, duration, onStartCallback, onEndCallback)
        {
            _slideAmount = slideAmount;
        }

        public override bool HasFinished => element.style.translate == new Translate(_endPosition.x, _endPosition.y, 0);

        public override void Start()
        {
            _layoutState = element.PushLayout();

            base.Start();

            _startPosition = new Vector3(element.resolvedStyle.translate.x, element.resolvedStyle.translate.y, element.resolvedStyle.translate.z);
            _endPosition = _startPosition + _slideAmount;

            // Make it Absolute in order to move
            element.MakeAbsolute();
        }

        public override void End()
        {
            _layoutState.Restore();
            base.End();
        }

        public override void Update(float normalizedValue)
        {
            element.style.translate = new Translate(Mathf.SmoothStep(_startPosition.x, _endPosition.x, normalizedValue),
                                                     Mathf.SmoothStep(_startPosition.y, _endPosition.y, normalizedValue));
        }
    }

    class UIFadeAnimation : UIAnimation
    {
        private StyleFloat _prevOpacity;
        private float _startOpacity;
        private float _targetOpacity;

        public UIFadeAnimation(VisualElement element, float duration, bool fadeIn,
                                Action<VisualElement> onStartCallback,
                                Action<VisualElement> onEndCallback) : base(element, duration, onStartCallback, onEndCallback)
        {
            if (fadeIn)
            {
                _startOpacity = 0f;
                _targetOpacity = 1f;
            }
            else
            {
                _startOpacity = 1f;
                _targetOpacity = 0f;
            }
        }

        public override bool HasFinished => Mathf.Approximately(_targetOpacity, element.resolvedStyle.opacity);

        public override void Start()
        {
            _prevOpacity = element.style.opacity;
            base.Start();
        }

        public override void End()
        {
            base.End();
            element.style.opacity = _prevOpacity;
        }

        public override void Update(float normalizedValue)
        {
            element.style.opacity = Mathf.SmoothStep(_startOpacity, _targetOpacity, normalizedValue);
        }
    }
}
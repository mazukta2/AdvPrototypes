using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;

namespace Postica.Common
{
    public class LongClickable : Clickable
    {
        public const string ussLongClick = "long-clicking";

        private DateTime _pointerDownTime;
        private DateTime _validPointerUpTime;
        private IVisualElementScheduledItem _classDelayEnabler;
        private float _longClickDuration = 1.0f; // Duration in seconds for a long click
        private Action _clickAction;
        private bool _useSimpleClick;

        public event Action longClicked;

        public LongClickable(Action clickAction, Action longClickAction, float duration = 0.5f)
            : base((Action)null)
        {
            base.clicked += SimpleClick;
            _clickAction = clickAction;
            _longClickDuration = duration;
            if (longClickAction != null)
            {
                longClicked += longClickAction;
            }
        }

        protected override void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            _pointerDownTime = DateTime.Now;
            _validPointerUpTime = _pointerDownTime.AddSeconds(_longClickDuration);
            _classDelayEnabler = target.schedule.Execute(() =>
            {
                target.AddToClassList(ussLongClick);
            });
            _classDelayEnabler.ExecuteLater((long)(_longClickDuration * 1000));

            base.ProcessDownEvent(evt, localPosition, pointerId);
        }

        protected override void ProcessUpEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            _classDelayEnabler?.Pause();
            target.RemoveFromClassList(ussLongClick);

            _useSimpleClick = true;
            if (DateTime.Now > _validPointerUpTime)
            {
                // Short click detected
                _useSimpleClick = false;
                longClicked?.Invoke();
            }
            base.ProcessUpEvent(evt, localPosition, pointerId);
        }

        private void SimpleClick()
        {
            if (_useSimpleClick)
            {
                _clickAction?.Invoke();
            }
        }
    }

}
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Postica.Common
{
    public class PreviewClickable : Clickable
    {
        private EventBase _lastEvent;
        public string previewClassname { get; }

        public PreviewClickable(Action action, string previewClassname)
            : base(action)
        {
            this.previewClassname = previewClassname;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
            target.RegisterCallback<MouseEnterEvent>(OnMouseEnterInternal);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMoveInternal);
            // target.RegisterCallback<KeyDownEvent>(OnKeyDownInternal);
            base.RegisterCallbacksOnTarget();
        }
        
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseLeaveEvent>(OnMouseLeaveEvent);
            target.UnregisterCallback<MouseEnterEvent>(OnMouseEnterInternal);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMoveInternal);
            // target.UnregisterCallback<KeyDownEvent>(OnKeyDownInternal);
            base.UnregisterCallbacksFromTarget();
        }

        private void OnMouseLeaveEvent(MouseLeaveEvent evt)
        {
            // StopUpdatingClass(evt);
            target.RemoveFromClassList(previewClassname);
        }

        private void OnMouseEnterInternal(MouseEnterEvent evt)
        {
            if (AllMatch(evt))
            {
                target.AddToClassList(previewClassname);
            }
            // StartUpdatingClass(evt);
        }
        
        private void OnMouseMoveInternal(MouseMoveEvent evt)
        {
            target.EnableInClassList(previewClassname, AllMatch(evt));
        }

        private void OnKeyDownInternal(KeyDownEvent evt)
        {
            target.EnableInClassList(previewClassname, AllMatch(evt));
        }


        private void StartUpdatingClass(EventBase evt)
        {
            _lastEvent = evt;
            EditorApplication.update -= UpdateClass;
            EditorApplication.update += UpdateClass;
        }

        private void StopUpdatingClass(EventBase evt)
        {
            EditorApplication.update -= UpdateClass;
        }

        private void UpdateClass()
        {
            target.EnableInClassList(previewClassname, AllMatch(_lastEvent));
        }

        private bool AllMatch(EventBase evt)
        {
            if (evt is IMouseEvent me)
            {
                foreach (var activator in activators)
                {
                    if (!MatchModifiers(activator.modifiers, me))
                    {
                        return false;
                    }
                }
            }
            else if (evt is IPointerEvent pe)
            {
                foreach (var activator in activators)
                {
                    if (!MatchModifiers(activator.modifiers, pe))
                    {
                        return false;
                    }
                }
            }
            else if (evt is IKeyboardEvent ke)
            {
                foreach (var activator in activators)
                {
                    if (!MatchModifiers(activator.modifiers, ke))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        private bool MatchModifiers(EventModifiers modifiers, IPointerEvent e)
            => MatchModifiers(modifiers, e.altKey, e.ctrlKey, e.shiftKey, e.commandKey);

        private bool MatchModifiers(EventModifiers modifiers, IMouseEvent e) 
            => MatchModifiers(modifiers, e.altKey, e.ctrlKey, e.shiftKey, e.commandKey);

        private bool MatchModifiers(EventModifiers modifiers, IKeyboardEvent e)
            => MatchModifiers(modifiers, e.altKey, e.ctrlKey, e.shiftKey, e.commandKey);

        private bool MatchModifiers(EventModifiers modifiers, bool alt, bool ctrl, bool shift, bool command)
        {
            if (((modifiers & EventModifiers.Alt) != 0 && !alt) || ((modifiers & EventModifiers.Alt) == 0 && alt))
            {
                return false;
            }

            if (((modifiers & EventModifiers.Control) != 0 && !ctrl) || ((modifiers & EventModifiers.Control) == 0 && ctrl))
            {
                return false;
            }

            if (((modifiers & EventModifiers.Shift) != 0 && !shift) || ((modifiers & EventModifiers.Shift) == 0 && shift))
            {
                return false;
            }

            return ((modifiers & EventModifiers.Command) == 0 || command) && ((modifiers & EventModifiers.Command) != 0 || !command);
        }

    }

}
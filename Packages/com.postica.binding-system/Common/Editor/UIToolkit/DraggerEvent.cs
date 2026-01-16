using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.Common
{
    public class DraggerEvent : MouseManipulator
    {
        public delegate void OnNewPositionDelegate(Vector2 finalValue);
        public delegate Vector2 GetPositionDelegate();
        
        private Vector2 _start;
        private Vector2 _globalStart;
        private bool _active;
        private readonly GetPositionDelegate _getStart;
        private readonly OnNewPositionDelegate _onDrag;

        public DraggerEvent(GetPositionDelegate getStart, OnNewPositionDelegate callback)
        {
            _getStart = getStart;
            _onDrag = callback;
            activators.Add(new ManipulatorActivationFilter
            {
                button = MouseButton.LeftMouse
            });
            _active = false;
        }

        /// <summary>
        ///   <para>Called to register click event callbacks on the target element.</para>
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown));
            target.RegisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove));
            target.RegisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp));
        }

        /// <summary>
        ///   <para>Called to unregister event callbacks from the target element.</para>
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown));
            target.UnregisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove));
            target.UnregisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp));
        }

        /// <summary>
        ///   <para>Called on mouse down event.</para>
        /// </summary>
        /// <param name="e">The event.</param>
        private void OnMouseDown(MouseDownEvent e)
        {
            if (e.target is VisualElement ve && ve.GetFirstAncestorOfType<ListView>() != null)
            {
                return;
            }
            if (_active)
            {
                e.StopImmediatePropagation();
            }
            else if(!e.isPropagationStopped)
            {
                _start = GUIUtility.GUIToScreenPoint(e.mousePosition);
                _globalStart = _getStart();
                _active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        /// <summary>
        ///   <para>Called on mouse move event.</para>
        /// </summary>
        /// <param name="e">The event.</param>
        private void OnMouseMove(MouseMoveEvent e)
        {
            if (!_active)
            {
                return;
            }

            var delta = GUIUtility.GUIToScreenPoint(e.mousePosition) - _start;
            _onDrag?.Invoke(_globalStart + delta);

            e.StopPropagation();
        }

        /// <summary>
        ///   <para>Called on mouse up event.</para>
        /// </summary>
        /// <param name="e">The event.</param>
        private void OnMouseUp(MouseUpEvent e)
        {
            if (_active)
            {
                _active = false;
                target.ReleaseMouse();
                e.StopPropagation();
            }
        }
    }
}
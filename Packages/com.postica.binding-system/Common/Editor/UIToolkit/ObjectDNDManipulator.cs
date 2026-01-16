using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEditor;

namespace Postica.Common
{
    public class ObjectDNDManipulator : Manipulator
    {
        public const string ussInvalid = "dnd-invalid";
        public const string ussValid = "dnd-valid";

        private bool _interactableOnlyOnDragAndDrop;
        private IVisualElementScheduledItem _interactivityUpdater;
        
        public bool AllowSceneObjects { get; set; }
        public Type BaseType { get; set; }

        public bool InteractableOnlyOnDragAndDrop
        {
            get => _interactableOnlyOnDragAndDrop;
            set
            {
                if(_interactableOnlyOnDragAndDrop == value)
                {
                    return;
                }
                
                _interactableOnlyOnDragAndDrop = value;
                EnableInteractivityIfNeeded();
            }
        }

        public event Action<Object> OnDrop;
        public event Action<Object[]> OnDropMultiple;

        private bool _canDrop;
        private bool? _lastValidValue;

        public ObjectDNDManipulator(Action<Object> onDrop, bool allowSceneObjects, Type baseType)
        {
            AllowSceneObjects = allowSceneObjects;
            BaseType = baseType;
            if(onDrop != null)
            {
                OnDrop += onDrop;
            }
        }
        
        public ObjectDNDManipulator(Action<Object[]> onDrop, bool allowSceneObjects, Type baseType = null)
        {
            AllowSceneObjects = allowSceneObjects;
            BaseType = baseType ?? typeof(Object);
            if(onDrop != null)
            {
                OnDropMultiple += onDrop;
            }
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<DragEnterEvent>(OnDragEnter);
            target.RegisterCallback<DragLeaveEvent>(OnDragLeave);
            target.RegisterCallback<DragPerformEvent>(OnDragPerform);
            target.RegisterCallback<DragExitedEvent>(OnDragExit);
            target.RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            
            EnableInteractivityIfNeeded();
        }
        
        private void EnableInteractivityIfNeeded()
        {
            if (_interactivityUpdater != null)
            {
                _interactivityUpdater.Pause();
                _interactivityUpdater = null;
            }
            
            if (target == null)
            {
                return;
            }
            
            if (_interactableOnlyOnDragAndDrop)
            {
                target.pickingMode = PickingMode.Ignore;
                target.panel?.visualTree.RegisterCallback<MouseEnterWindowEvent>(evt => target.pickingMode = EmptyDragAndDrop() ? PickingMode.Ignore : PickingMode.Position);
                _interactivityUpdater = target.schedule.Execute(() =>
                {
                    target.pickingMode = EmptyDragAndDrop() ? PickingMode.Ignore : PickingMode.Position;
                }).Every(33);
            }
            else
            {
                target.pickingMode = PickingMode.Position;
            }
        }

        private void OnDragUpdate(DragUpdatedEvent evt)
        {
            if (EmptyDragAndDrop()) return;
            
            SetValid(_lastValidValue);
            evt.StopPropagation();
        }

        private void OnDragExit(DragExitedEvent evt)
        {
            if (EmptyDragAndDrop()) return;
            
            _canDrop = false;
            SetValid(null);
            evt.StopPropagation();
        }

        private void OnDragEnter(DragEnterEvent evt)
        {
            if (EmptyDragAndDrop()) return;
            
            _canDrop = DragAndDrop.objectReferences?.Length > 0 
                       && IsValid(DragAndDrop.objectReferences[0], out _);
            SetValid(_canDrop);
            
            evt.StopPropagation();
        }

        private void OnDragLeave(DragLeaveEvent evt)
        {
            if (EmptyDragAndDrop()) return;
            
            SetValid(null);
            _canDrop = false;
            evt.StopPropagation();
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            if (EmptyDragAndDrop()) return;

            if (OnDropMultiple != null && IsMultipleValid(out var objects))
            {
                OnDropMultiple(objects);
            }
            else if(OnDrop != null && IsSingleValid(out var correctValue))
            {
                OnDrop(correctValue);
            }
            _canDrop = false;
            DragAndDrop.AcceptDrag();
            DragAndDrop.objectReferences = null;
            SetValid(null);
            evt.StopPropagation();
        }
        
        private bool EmptyDragAndDrop()
        {
            if(DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
            {
                _canDrop = false;
                SetValid(null);
                return true;
            }
            return false;
        }

        private void SetValid(bool? valid)
        {
            target.EnableInClassList(ussValid, valid == true);
            target.EnableInClassList(ussInvalid, valid == false);

            if (valid.HasValue)
            {
                DragAndDrop.visualMode = valid.Value ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.None;
            }

            _lastValidValue = valid;
        }
        
        private bool IsSingleValid(out Object correctValue)
        {
            if (DragAndDrop.objectReferences?.Length != 1 || DragAndDrop.objectReferences[0] == null)
            {
                correctValue = null;
                return false;
            }

            return IsValid(DragAndDrop.objectReferences[0], out correctValue);
        }
        
        private bool IsMultipleValid(out Object[] correctValues)
        {
            if (DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
            {
                correctValues = null;
                return false;
            }

            var correct = new List<Object>();
            foreach (var value in DragAndDrop.objectReferences)
            {
                if (IsValid(value, out var correctValue))
                {
                    correct.Add(correctValue);
                }
            }

            correctValues = correct.ToArray();
            return correctValues.Length > 0;
        }

        private bool IsValid(Object value, out Object correctValue)
        {
            if (value == null)
            {
                correctValue = null;
                return false;
            }
            
            var valueType = value.GetType();

            if(!AllowSceneObjects && CheckIsSceneObject(value))
            {
                correctValue = null;
                return false;
            }

            if (BaseType == null)
            {
                correctValue = value;
                return true;
            }

            if (!typeof(Component).IsAssignableFrom(BaseType)
                && !BaseType.IsAssignableFrom(valueType))
            {
                correctValue = null;
                return false;
            }

            if(BaseType == typeof(GameObject))
            {
                if (value is GameObject go)
                {
                    correctValue = go;
                    return true;
                }
                if (value is Component c)
                {
                    correctValue = c.gameObject;
                    return true;
                }
                correctValue = null;
                return false;
            }

            if (!typeof(Component).IsAssignableFrom(BaseType))
            {
                correctValue = value;
                return BaseType.IsAssignableFrom(valueType);
            }
            
            // Do the same for components
            if (value is GameObject goValue)
            {
                correctValue = goValue.GetComponent(BaseType);
                return correctValue != null;
            }

            if (value is Component cValue)
            {
                correctValue = cValue.GetComponent(BaseType);
                return correctValue != null;
            }

            correctValue = null;
            return false;
        }

        private static bool CheckIsSceneObject(Object obj) => obj is GameObject go 
            ? !string.IsNullOrEmpty(go.scene.name)
            : obj is Component c && !string.IsNullOrEmpty(c.gameObject.scene.name);

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<DragEnterEvent>(OnDragEnter);
            target.UnregisterCallback<DragLeaveEvent>(OnDragLeave);
            target.UnregisterCallback<DragPerformEvent>(OnDragPerform);
            target.UnregisterCallback<DragExitedEvent>(OnDragExit);
            target.UnregisterCallback<DragUpdatedEvent>(OnDragUpdate);
        }
    }

}
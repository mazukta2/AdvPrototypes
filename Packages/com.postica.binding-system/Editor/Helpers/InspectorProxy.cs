using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{

    internal class InspectorProxy
    {
        #region [  STATIC PART  ]

        private const string _propertyEditorFullName = "UnityEditor.PropertyEditor";
        private const string _allPropertyEditorsField = "m_AllPropertyEditors";
        private const string _getTrackerPropertyName = "tracker";

        private static Type _inspectorWindowType;
        private static Type _propertyEditorType;
        private static IList _activePropertyEditors;
        private static int _count;
        private static EditorWindow[] _previousInspectors;
        
        private static Dictionary<EditorWindow, InspectorProxy> _inspectorProxies = new();
        
        public static event Action<InspectorProxy> OnInspectorClosed;
        public static event Action<InspectorProxy> OnInspectorOpened;

        public static void Initialize()
        {
            EditorApplication.update -= TrackInspectors;
            EditorApplication.update += TrackInspectors;
        }
        
        public static Type PropertyEditorType
        {
            get
            {
                if (_propertyEditorType != null) return _propertyEditorType;
                
                EditorApplication.update -= TrackInspectors;
                EditorApplication.update += TrackInspectors;
                _propertyEditorType = typeof(Editor).Assembly.GetType(_propertyEditorFullName);

                return _propertyEditorType;
            }
        }

        public static IList AllPropertyEditors
        {
            get
            {
                if (_activePropertyEditors != null) return _activePropertyEditors;
                
                _activePropertyEditors = GetAllPropertyEditors();
                foreach (EditorWindow editorWindow in _activePropertyEditors)
                {
                    var proxy = new InspectorProxy(editorWindow);
                    OnInspectorOpened?.Invoke(proxy);
                }
                return _activePropertyEditors;
            }
        }

        private static IList GetAllPropertyEditors()
        {
            var allPropertyEditorsField = PropertyEditorType.GetField(_allPropertyEditorsField,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var array = allPropertyEditorsField.GetValue(null); 
            return array as IList;
        }
        
        private static void TrackInspectors()
        {
            foreach (var pair in _inspectorProxies)
            {
                pair.Value.UpdateTracker();
            }
            
            if (_count == AllPropertyEditors.Count)
            {
                return;
            }

            _count = AllPropertyEditors.Count;

            if (_previousInspectors == null)
            {
                _previousInspectors = GetAllPropertyEditors().Cast<EditorWindow>().ToArray();
                return;
            }
            
            var currentInspectors = AllPropertyEditors.Cast<EditorWindow>().ToArray();
            var openedInspectors = currentInspectors.Except(_previousInspectors);
            var closedInspectors = _previousInspectors.Except(currentInspectors);

            if (OnInspectorOpened != null)
            {
                foreach (var inspector in openedInspectors)
                {
                    if (!_inspectorProxies.TryGetValue(inspector, out var proxy))
                    {
                        proxy = new InspectorProxy(inspector);
                    }

                    OnInspectorOpened(proxy);
                }
            }

            if (OnInspectorClosed != null)
            {
                foreach (var inspector in closedInspectors)
                {
                    if (_inspectorProxies.TryGetValue(inspector, out var proxy))
                    {
                        OnInspectorClosed(proxy);
                        _inspectorProxies.Remove(inspector);
                    }
                }
            }
            
            _previousInspectors = currentInspectors;
        }
        
        #endregion
        
        private Action<VisualElement> _onUIToolkitReadyCallback;
        private Func<ActiveEditorTracker> _getTrackerFunc;
        private Editor[] _previousEditors;
        
        public EditorWindow Window { get; }
        public bool IsOpen => Window;
        public bool IsInspector => Window.GetType().Name == "InspectorWindow";
        public VisualElement Root => Window.rootVisualElement;
        
        public ActiveEditorTracker Tracker
        {
            get
            {
                if (_getTrackerFunc == null)
                {
                    const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
                    var trackerProperty = PropertyEditorType.GetProperty(_getTrackerPropertyName, flags);
                    var getMethod = trackerProperty.GetGetMethod(true);
                    _getTrackerFunc = (Func<ActiveEditorTracker>) Delegate.CreateDelegate(typeof(Func<ActiveEditorTracker>), Window, getMethod);
                }
                return _getTrackerFunc();
            }
        }

        public event Action<InspectorProxy, Editor[]> Changed;
        public event Action<InspectorProxy, Editor> EditorOpened;
        public event Action<InspectorProxy, Editor> OnEditorClosed;

        private InspectorProxy(EditorWindow window)
        {
            Window = window;
            _inspectorProxies[window] = this;
        }
        
        private void UpdateTracker()
        {
            if (Tracker == null)
            {
                return;
            }
            
            if (_previousEditors == null)
            {
                _previousEditors = Tracker.activeEditors;
                foreach (var editor in _previousEditors)
                {
                    EditorOpened?.Invoke(this, editor);
                }
                Changed?.Invoke(this, _previousEditors);
                return;
            }
            
            var currentEditors = Tracker.activeEditors;
            var openedEditors = currentEditors.Except(_previousEditors);
            var closedEditors = _previousEditors.Except(currentEditors);
            
            foreach (var editor in openedEditors)
            {
                EditorOpened?.Invoke(this, editor);
            }
            
            foreach (var editor in closedEditors)
            {
                OnEditorClosed?.Invoke(this, editor);
            }
            
            if(openedEditors.Any() || closedEditors.Any())
            {
                Changed?.Invoke(this, currentEditors);
            }
            
            _previousEditors = currentEditors;
        }
        
        public void OnUIToolkitReady(Action<VisualElement> action)
        {
            if (Root.panel != null)
            {
                action(Root);
            }
            else
            {
                _onUIToolkitReadyCallback = action;
                Root.RegisterCallback<AttachToPanelEvent>(AttachedToPanel);
            }
        }

        private void AttachedToPanel(AttachToPanelEvent evt)
        {
            Root.UnregisterCallback<AttachToPanelEvent>(AttachedToPanel);
            
            _onUIToolkitReadyCallback?.Invoke(Root);
        }
    }
}
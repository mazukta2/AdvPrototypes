using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This class is used to reroute fields when using Binding System.
    /// </summary>
    [DefaultExecutionOrder(-32000)]
    [HideMember]
    internal class FieldRoutes : ScriptableObject
    {
        public const string AssetName = "field-routes";
        
        private static bool _initialized;
        private static FieldRoutes _instance;
        private static Action _save;
        
        internal static FieldRoutes Instance => _instance;
        
        internal static void Initialize(Action<Object> setDirty)
        {
            _save = () => setDirty?.Invoke(_instance);
            Initialize();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            
            _instance = Resources.Load<FieldRoutes>(AssetName);
            if (_instance == null)
            {
                _initialized = false;
                return;
            }

            foreach (var route in _instance._routes)
            {
                if (route.type.Get() == null)
                {
                    Debug.LogError(BindSystem.DebugPrefix + $"Type {route.type} not found for field re-routing.");
                    continue;
                }
                
                BindSystem.RerouteBoundField(route.type.Get(), route.from, route.to);
            }
        }

        public static bool IsRouted(Type type, string from)
        {
            return IsInitialized() && _instance._routes.Exists(route => route.type.Get() == type && route.from == from);
        }
        
        public static bool TryGetRoute(Type type, string from, out string to)
        {
            if (!IsInitialized())
            {
                to = null;
                return false;
            }
            var route = _instance._routes?.Find(r => r.type.Get() == type && r.from == from);
            if (route == null)
            {
                to = null;
                return false;
            }
            to = route.to;
            return true;
        }
        
        public static void Add(Type type, string from, string to)
        {
            if (!IsInitialized())
            {
                return;
            }
            _instance.AddRoute(type, from, to);
        }
        
        public static void Remove(Type type, string from)
        {
            if (!IsInitialized())
            {
                return;
            }
            _instance.RemoveRoute(type, from);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsInitialized()
        {
            Initialize();
            return _initialized;
        }
        
        [SerializeField]
        private List<Route> _routes = new();
        
        public List<Route> Routes => _routes;
        
        public void AddRoute(Type type, string from, string to)
        {
            // Find existing route
            var existingRoute = _routes.Find(route => route.type.Get() == type && route.from == from);
            if (existingRoute != null)
            {
                BindSystem.UnrouteBoundField(type, from);
                existingRoute.to = to;
                BindSystem.RerouteBoundField(type, from, to);
                _save?.Invoke();
                return;
            }
            _routes.Add(new Route
            {
                type = type,
                from = from,
                to = to
            });
            BindSystem.RerouteBoundField(type, from, to);
            _save?.Invoke();
        }
        
        public void RemoveRoute(Type type, string from)
        {
            if (_routes.RemoveAll(route => route.type.Get() == type && route.from == from) > 0)
            {
                BindSystem.UnrouteBoundField(type, from);
                _save?.Invoke();
            }
        }

        [Serializable]
        public class Route
        {
            public SerializedType type;
            public string from;
            public string to;
        }
    }
}

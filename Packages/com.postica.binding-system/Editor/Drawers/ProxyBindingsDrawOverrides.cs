using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;

using Object = UnityEngine.Object;
using Postica.Common;

namespace Postica.BindingSystem
{
    internal static class ProxyBindingsDrawOverrides
    {
        public delegate bool TryGetOverrideForDelegate(SerializedProperty property, out Overrides overrides);
        
        private static readonly List<TryGetOverrideForDelegate> _filters = new();
        
        public class Overrides
        {
            public string textPrefix = null;
            public int? shiftX;
            public int? shiftY;
            public bool? usePrefixRect;
            public float? panelShiftX;
            public bool? coverWithBackground;
            public bool? keepFieldHeight;
        }
        
        public static void RegisterFilter(TryGetOverrideForDelegate filter)
        {
            _filters.Add(filter);
        }
        
        public static void UnregisterFilter(TryGetOverrideForDelegate filter)
        {
            _filters.Remove(filter);
        }
        
        public static bool TryGetOverrideFor(SerializedProperty property, out Overrides overrides)
        {
            foreach (var filter in _filters)
            {
                if (filter(property, out overrides))
                {
                    return true;
                }
            }
            overrides = null;
            return false;
        }
    }
}
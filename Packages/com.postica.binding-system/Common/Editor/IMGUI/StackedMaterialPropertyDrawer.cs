using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace Postica.Common
{
    public abstract class StackedMaterialPropertyDrawer : MaterialPropertyDrawer
    {
        private Dictionary<string, MaterialPropertyDrawer> PreviousDrawers { get; } = new();
        
        public void SetDrawer(string propertyName, MaterialPropertyDrawer drawer, bool overwrite = false)
        {
            if(overwrite || !PreviousDrawers.TryGetValue(propertyName, out var prevDrawer) || prevDrawer == null)
            {
                PreviousDrawers[propertyName] = drawer;
            }
        }

        public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
        {
            if(PreviousDrawers.TryGetValue(prop.name, out var drawer) && drawer != null)
            {
                drawer.OnGUI(position, prop, label, editor);
                return;
            }
            editor.DefaultShaderProperty(position, prop, label.text);
        }
        
        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            if(PreviousDrawers.TryGetValue(prop.name, out var drawer) && drawer != null)
            {
                return drawer.GetPropertyHeight(prop, label, editor);
            }
            return MaterialEditor.GetDefaultPropertyHeight(prop);
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            if(PreviousDrawers.TryGetValue(prop.name, out var drawer) && drawer != null)
            {
                drawer.OnGUI(position, prop, label, editor);
                return;
            }
            editor.DefaultShaderProperty(position, prop, label);
        }
        
        public override void Apply(MaterialProperty prop)
        {
            if(PreviousDrawers.TryGetValue(prop.name, out var drawer) && drawer != null)
            {
                drawer.Apply(prop);
            }
            base.Apply(prop);
        }
    }
}
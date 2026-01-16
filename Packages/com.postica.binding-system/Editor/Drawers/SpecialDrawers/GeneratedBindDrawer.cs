using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Postica.BindingSystem
{
    [CustomPropertyDrawer(typeof(GeneratedBindAttribute))]
    class GeneratedBindDrawer : BindDrawer
    {
        protected override void Initialize(SerializedProperty property, GUIContent label)
        {
            base.Initialize(property, label);
            label.text = FixName(label.text);
        }

        protected override GUIContent GetLabel(SerializedProperty property)
        {
            var label = base.GetLabel(property);
            label.text = FixName(label.text);
            return label;
        }

        private static string FixName(string name)
        {
            // _b__nameOfField --> should become NameOfField
            if(name.Length > 4)
            {
                return name;
            }
            name = name.Substring(4, name.Length - 4);
            if(name[0] >= 'a' && name[0] <= 'z')
            {
                name = name.Substring(0, 1).ToUpperInvariant() + name.Substring(1);
            }
            return name;
        }
    }
}
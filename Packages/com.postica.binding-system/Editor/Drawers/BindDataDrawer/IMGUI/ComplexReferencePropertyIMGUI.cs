using Postica.BindingSystem.Accessors;
using Postica.BindingSystem.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal partial class ComplexReferenceProperty
        {
            public float height;

            public ComplexReferenceProperty(float fixedHeight)
            {
                height = fixedHeight;
                properties = Array.Empty<ReferenceProperty>();
            }

            private partial void InitializeIMGUI(SerializedProperty property)
            {
                // Nothing for now
            }

            public void Draw(Rect rect, SerializedProperty property)
            {
                if (properties.Length == 0 || !property.isExpanded) { return; }
                
                EditorGUI.BeginChangeCheck();
                for (int i = 0; i < properties.Length; i++)
                {
                    var innerProperty = property.FindPropertyRelative(properties[i].name);
                    if(innerProperty == null)
                    {
                        continue;
                    }
                    rect.height = properties[i].height;
                    EditorGUI.PropertyField(rect, innerProperty, true);
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    onChanged?.Invoke();
                }
            }

            public float GetHeight(SerializedProperty property)
            {
                if (properties.Length == 0) { return 0f; }

                if (!property.isExpanded) { return 0; }

                height = 0f;
                for (int i = 0; i < properties.Length; i++)
                {
                    var innerProperty = property.FindPropertyRelative(properties[i].name);
                    var propertyHeight = innerProperty != null ? EditorGUI.GetPropertyHeight(innerProperty, true) : 0;
                    properties[i].height = propertyHeight;
                    height += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
                }
                return height;
            }
        }
    }
}
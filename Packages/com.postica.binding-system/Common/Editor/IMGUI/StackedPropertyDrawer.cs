using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

namespace Postica.Common
{
    public abstract class StackedPropertyDrawer : PropertyDrawer
    {
        private PropertyDrawer _nextDrawer;
        private bool _initialized;

        private static readonly GUIContent _incompatibleContent = new GUIContent("No editor available");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!_initialized)
            {
                _initialized = true;
                DrawerSystem.TryGetNextDrawer(this, out _nextDrawer);
            }
            Draw(position, property, label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!_initialized)
            {
                _initialized = true;
                DrawerSystem.TryGetNextDrawer(this, out _nextDrawer);
            }
            return GetHeight(property, label);
        }

        protected void SetDrawProperty(SerializedProperty property)
        {
            if (!_initialized)
            {
                _initialized = true;
                DrawerSystem.TryGetNextDrawer(this, out _nextDrawer);
            }
            if(property == null)
            {
                // Incompatible property
                return;
            }
            DrawerSystem.Inject(_nextDrawer, property.GetFieldInfo());
        }

        private void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property == null)
            {
                EditorGUI.LabelField(position, label, _incompatibleContent);
                return;
            }
            if (_nextDrawer != null)
            {
                _nextDrawer.OnGUI(position, property, label);
            }
            else
            {
                //EditorGUI.BeginProperty(position, label, property);
                if (property.hasMultipleDifferentValues)
                {
                    DrawProperty(position, property, label);
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label, true);
                }
                //EditorGUI.EndProperty();
            }
        }

        private static void DrawProperty(Rect position, SerializedProperty property, GUIContent label)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float: property.floatValue = EditorGUI.FloatField(position, label, property.floatValue); break;
                case SerializedPropertyType.Integer: property.intValue = EditorGUI.IntField(position, label, property.intValue); break;
                case SerializedPropertyType.Boolean: property.boolValue = EditorGUI.Toggle(position, label, property.boolValue); break;
                case SerializedPropertyType.String: property.stringValue = EditorGUI.TextField(position, label, property.stringValue); break;
                case SerializedPropertyType.Color: property.colorValue = EditorGUI.ColorField(position, label, property.colorValue); break;
                case SerializedPropertyType.ObjectReference: property.objectReferenceValue = EditorGUI.ObjectField(position, label, property.objectReferenceValue, property.GetPropertyType() ?? typeof(Object), true); break;
                case SerializedPropertyType.LayerMask: property.intValue = EditorGUI.LayerField(position, label, property.intValue); break;
                case SerializedPropertyType.Vector2: property.vector2Value = EditorGUI.Vector2Field(position, label, property.vector2Value); break;
                case SerializedPropertyType.Vector3: property.vector3Value = EditorGUI.Vector3Field(position, label, property.vector3Value); break;
                case SerializedPropertyType.Vector4: property.vector4Value = EditorGUI.Vector4Field(position, label, property.vector4Value); break;
                case SerializedPropertyType.Rect: property.rectValue = EditorGUI.RectField(position, label, property.rectValue); break;
                case SerializedPropertyType.Character: property.stringValue = EditorGUI.TextField(position, label, property.stringValue); break;
                case SerializedPropertyType.AnimationCurve: property.animationCurveValue = EditorGUI.CurveField(position, label, property.animationCurveValue); break;
                case SerializedPropertyType.Bounds: property.boundsValue = EditorGUI.BoundsField(position, label, property.boundsValue); break;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = EditorGUI.Vector2IntField(position, label, property.vector2IntValue); break;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = EditorGUI.Vector3IntField(position, label, property.vector3IntValue); break;
                case SerializedPropertyType.RectInt: property.rectIntValue = EditorGUI.RectIntField(position, label, property.rectIntValue); break;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = EditorGUI.BoundsIntField(position, label, property.boundsIntValue); break;
                default: EditorGUI.PropertyField(position, property, label); break;
            }
        }

        private float GetHeight(SerializedProperty property, GUIContent label)
        {
            if (_nextDrawer != null)
            {
                return _nextDrawer.GetPropertyHeight(property, label);
            }
            else if(property == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }
            else
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!_initialized)
            {
                _initialized = true;
                DrawerSystem.TryGetNextDrawer(this, out _nextDrawer);
            }
            return _nextDrawer?.CreatePropertyGUI(property);
        }
    }
}
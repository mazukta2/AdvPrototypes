using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace Postica.Common
{
    public static class InputFields
    {
        public static VisualElement GetReadOnlyField(object value)
        {
            return value switch
            {
                bool b => new Toggle(){value = b}.MakeReadonly<Toggle, bool>(),
                string str => new TextField(){value = str, isReadOnly = true},
                float v => new FloatField(){value = v, isReadOnly = true},
                double v => new DoubleField(){value = v, isReadOnly = true},
                int v => new IntegerField(){value = v, isReadOnly = true},
                long v => new LongField(){value = v, isReadOnly = true},
                short v => new IntegerField(){value = v, isReadOnly = true},
                byte v => new IntegerField(){value = v, isReadOnly = true},
                char v => new TextField(){value = v.ToString(), isReadOnly = true},
                ushort v => new IntegerField(){value = v, isReadOnly = true},
                Vector2 v => new Vector2Field(){value = v}.MakeReadonly<Vector2Field, Vector2>(),
                Vector2Int v => new Vector2IntField(){value = v}.MakeReadonly<Vector2IntField, Vector2Int>(),
                Vector3 v => new Vector3Field(){value = v}.MakeReadonly<Vector3Field, Vector3>(),
                Vector3Int v => new Vector3IntField(){value = v}.MakeReadonly<Vector3IntField, Vector3Int>(),
                Vector4 v => new Vector4Field(){value = v}.MakeReadonly<Vector4Field, Vector4>(),
                Quaternion v => new Vector3Field(){value = v.eulerAngles}.MakeReadonly<Vector3Field, Vector3>(),
                Color v => new ColorField(){value = v}.MakeReadonly<ColorField, Color>(),
                Gradient v => new GradientField(){value = v}.MakeReadonly<GradientField, Gradient>(),
                AnimationCurve v => new CurveField(){value = v}.MakeReadonly<CurveField, AnimationCurve>(),
                Object v => new ObjectField(){value = v, objectType = v.GetType(), allowSceneObjects = true}.MakeReadonly<ObjectField, Object>(),
                
                null => new Label("null").WithClass("null-field"),
                _ => new Label(value.ToString())
            };
        }
        
        public static VisualElement GetReadOnlyField(object value, Type type)
        {
            return value switch
            {
                bool b => new Toggle(){value = b}.MakeReadonly<Toggle, bool>(),
                string str => new TextField(){value = str, isReadOnly = true},
                float v => new FloatField(){value = v, isReadOnly = true},
                double v => new DoubleField(){value = v, isReadOnly = true},
                int v => new IntegerField(){value = v, isReadOnly = true},
                long v => new LongField(){value = v, isReadOnly = true},
                short v => new IntegerField(){value = v, isReadOnly = true},
                byte v => new IntegerField(){value = v, isReadOnly = true},
                char v => new TextField(){value = v.ToString(), isReadOnly = true},
                ushort v => new IntegerField(){value = v, isReadOnly = true},
                Vector2 v => new Vector2Field(){value = v}.MakeReadonly<Vector2Field, Vector2>(),
                Vector2Int v => new Vector2IntField(){value = v}.MakeReadonly<Vector2IntField, Vector2Int>(),
                Vector3 v => new Vector3Field(){value = v}.MakeReadonly<Vector3Field, Vector3>(),
                Vector3Int v => new Vector3IntField(){value = v}.MakeReadonly<Vector3IntField, Vector3Int>(),
                Vector4 v => new Vector4Field(){value = v}.MakeReadonly<Vector4Field, Vector4>(),
                Quaternion v => new Vector3Field(){value = v.eulerAngles}.MakeReadonly<Vector3Field, Vector3>(),
                Color v => new ColorField(){value = v}.MakeReadonly<ColorField, Color>(),
                Gradient v => new GradientField(){value = v}.MakeReadonly<GradientField, Gradient>(),
                AnimationCurve v => new CurveField(){value = v}.MakeReadonly<CurveField, AnimationCurve>(),
                Object v => new ObjectField(){value = v, objectType = v.GetType(), allowSceneObjects = true}.MakeReadonly<ObjectField, Object>(),
                
                null => GetReadOnlyField(type),
                _ => new Label(GetStringValue(value, type))
            };
        }

        private static string GetStringValue(object value, Type type)
        {
            var objType = value?.GetType() ?? type;
            var stringValue = value?.ToString();
            if (stringValue == objType.ToString())
            {
                stringValue = objType.UserFriendlyName();
            }
            return stringValue;
        }
        
        public static VisualElement GetReadOnlyField(Type type)
        {
            if (typeof(Object).IsAssignableFrom(type))
            {
                return new ObjectField() { objectType = type, allowSceneObjects = true }
                    .MakeReadonly<ObjectField, Object>();
            }
            return type.Name switch
            {
                nameof(Boolean) => new Toggle().MakeReadonly<Toggle, bool>(),
                nameof(String) => new TextField(){ isReadOnly = true},
                nameof(Single) => new FloatField(){ isReadOnly = true},
                nameof(Double) => new DoubleField(){ isReadOnly = true},
                nameof(Int32) => new IntegerField(){ isReadOnly = true},
                nameof(Int64) => new LongField(){ isReadOnly = true},
                nameof(Int16) => new IntegerField(){ isReadOnly = true},
                nameof(Byte) => new IntegerField(){ isReadOnly = true},
                nameof(Char) => new TextField(){isReadOnly = true},
                nameof(UInt16) => new IntegerField(){ isReadOnly = true},
                nameof(Vector2) => new Vector2Field().MakeReadonly<Vector2Field, Vector2>(),
                nameof(Vector2Int) => new Vector2IntField().MakeReadonly<Vector2IntField, Vector2Int>(),
                nameof(Vector3) => new Vector3Field().MakeReadonly<Vector3Field, Vector3>(),
                nameof(Vector3Int) => new Vector3IntField().MakeReadonly<Vector3IntField, Vector3Int>(),
                nameof(Vector4) => new Vector4Field().MakeReadonly<Vector4Field, Vector4>(),
                nameof(Quaternion) => new Vector3Field().MakeReadonly<Vector3Field, Vector3>(),
                nameof(Color) => new ColorField().MakeReadonly<ColorField, Color>(),
                nameof(Gradient) => new GradientField().MakeReadonly<GradientField, Gradient>(),
                nameof(AnimationCurve) => new CurveField().MakeReadonly<CurveField, AnimationCurve>(),
                
                _ => new Label("undefined").WithClass("null-field"),
            };
        }
    }

}
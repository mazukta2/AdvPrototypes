using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Postica.BindingSystem
{
    internal static class SerializedPropertyExtensions
    {
        public static bool ShouldShiftLabel(this SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Generic ||
                (property.isArray && property.propertyType != SerializedPropertyType.String))
            {
                return true;
            }

            return (property.serializedObject.targetObject, property.propertyPath) switch
            {
                (Rigidbody r, "m_Constraints") => true,
                _ => false
            };
        }
    }
}

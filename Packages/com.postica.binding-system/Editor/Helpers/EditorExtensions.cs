using Postica.BindingSystem.Accessors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Postica.Common;
using UnityEditor;
using UnityEngine;

namespace Postica.BindingSystem
{

    static class EditorExtensions
    {
        public static bool TryGetType(this BindTypeSourceAttribute attr, SerializedProperty property, out Type currentType, 
            bool throwExceptions = true)
        {
            var fieldPath = attr.FieldPath;
            if (string.IsNullOrEmpty(fieldPath))
            {
                throw new ArgumentException($"{nameof(BindTypeSourceAttribute)} has empty filepath in {property.serializedObject.targetObject}");
            }
                
            var foundProperty = property.FindPropertyRelative(fieldPath);
            var parent = property.GetParent();

            while(foundProperty == null && parent != null)
            {
                foundProperty = parent.FindPropertyRelative(fieldPath);
                parent = parent.GetParent();
            }

            currentType = null;

            if (foundProperty == null)
            {
                var propertyResult = TryGetTypeFromProperty(attr, property, out currentType, throwExceptions);
                if (!propertyResult && throwExceptions)
                {
                    throw new ArgumentException($"{nameof(BindTypeSourceAttribute)} in {property.serializedObject.targetObject}: unable to find property at path {fieldPath}");
                }

                return propertyResult;
            }
            if(foundProperty.propertyType != SerializedPropertyType.String)
            {
                if (throwExceptions)
                {
                    throw new ArgumentException($"{nameof(BindTypeSourceAttribute)} in {property.serializedObject.targetObject}: property at path {fieldPath} is not a string type");
                }

                return false;
            }

            var type = Type.GetType(foundProperty.stringValue, false);
            if(type == null)
            {
                if (throwExceptions)
                {
                    throw new ArgumentException($"{nameof(BindTypeSourceAttribute)} in {property.serializedObject.targetObject}: property at path {fieldPath} cannot parse type creation");
                }

                return false;
            }
            
            currentType = type;
            return true;
        }

        private static bool TryGetTypeFromProperty(BindTypeSourceAttribute attr, SerializedProperty property, out Type currentType, bool throwExceptions)
        {
            var fieldInfo = property.GetFieldInfo();
            if (fieldInfo == null)
            {
                if (throwExceptions)
                {
                    throw new ArgumentException($"{nameof(BindTypeSourceAttribute)} in {property.serializedObject.targetObject}: unable to find field info for {property.propertyPath}");
                }

                currentType = null;
                return false;
            }
            
            var declaringType = fieldInfo.DeclaringType;
            var memberInfos = declaringType.GetMember(attr.FieldPath, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (memberInfos.Length == 0)
            {
                if (throwExceptions)
                {
                    throw new ArgumentException($"{nameof(BindTypeSourceAttribute)} in {property.serializedObject.targetObject}: unable to find member info for {attr.FieldPath}");
                }

                currentType = null;
                return false;
            }
            
            var memberInfo = memberInfos[0];
            if (memberInfo.GetMemberType() != typeof(Type))
            {
                if (throwExceptions)
                {
                    throw new ArgumentException($"{nameof(BindTypeSourceAttribute)} in {property.serializedObject.targetObject}: member info for {attr.FieldPath} has type {memberInfo.GetMemberType()} instead of {typeof(Type)}");
                }
            }
            
            object obj = property.serializedObject.targetObject;
            var parentProperty = property.GetParent();
            if (parentProperty?.isArray == true)
            {
                parentProperty = parentProperty.GetParent();
            }
            obj = parentProperty?.GetValue() ?? obj;
            var value = memberInfo.GetValue(obj);
            // if (value == null)
            // {
            //     if (throwExceptions)
            //     {
            //         throw new ArgumentException($"{nameof(BindTypeSourceAttribute)} in {property.serializedObject.targetObject}: member info for {attr.FieldPath} has null value");
            //     }
            //
            //     currentType = null;
            //     return false;
            // }
            
            currentType = (Type) value;
            return true;
        }
    }
}
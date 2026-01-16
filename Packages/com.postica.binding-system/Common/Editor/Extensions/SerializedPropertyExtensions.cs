using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace Postica.Common
{
    public static class SerializedPropertyExtensions
    {
        private static readonly Dictionary<(Type, string), Type> _propertyTypesCache = new();
        private static readonly Func<SerializedProperty, IntPtr> NativePropertyPointerField = ReflectionEditorExtensions.FieldGetter<SerializedProperty, IntPtr>("m_NativePropertyPtr");
        private static readonly Func<SerializedObject, IntPtr> NativeObjectPointerField = ReflectionEditorExtensions.FieldGetter<SerializedObject, IntPtr>("m_NativeObjectPtr");

        public static bool ApplyChanges(this SerializedProperty property)
        {
            var serObj = property.serializedObject;
            using (var clone = new SerializedObject(serObj.targetObjects, serObj.context))
            {
                //var cloneProperty = clone.FindProperty(property.propertyPath);
                //cloneProperty.CopyFrom(property);
                clone.CopyFromSerializedProperty(property);
                return clone.ApplyModifiedProperties();
            }
        }

        public static bool ApplyChangesWithoutUndo(this SerializedProperty property)
        {
            var serObj = property.serializedObject;
            using (var clone = new SerializedObject(serObj.targetObjects, serObj.context))
            {
                clone.CopyFromSerializedProperty(property);
                return clone.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        
        public static string GetViewDataKey(this SerializedProperty property)
        {
            return property.serializedObject.targetObject.GetInstanceID() + '.' + property.propertyPath;
        }

        public static FieldInfo GetFieldInfo(this SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }
            
            var parentProperty = property.GetParent();
            var parentType = parentProperty != null 
                           ? parentProperty.GetPropertyType() 
                           : property.serializedObject.targetObject.GetType();
            var fieldName = property.propertyPath[(property.propertyPath.LastIndexOf('.') + 1)..];
            if (fieldName[^1] == ']') // <-- We have an array element here
            {
                // Need to get the array --> this is the correct fieldInfo
                if (parentProperty != null)
                {
                    var parentFieldInfo = parentProperty.GetFieldInfo();
                    parentProperty.Dispose();
                    return parentFieldInfo;
                }
                // There is most probably an error so we just try "with this"
                var arrayIndex = property.propertyPath.LastIndexOf(".Array", StringComparison.Ordinal);
                fieldName = property.propertyPath[..arrayIndex];
                fieldName = fieldName[(fieldName.LastIndexOf('.') + 1)..];
            }

            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo fieldInfo = GetFieldInTypeHierarchy(parentType, fieldName, flags);

            parentProperty?.Dispose();

            return fieldInfo;
        }

        private static FieldInfo GetFieldInTypeHierarchy(Type type, string fieldName, BindingFlags flags)
        {
            var baseType = type?.BaseType;
            var fieldInfo = type?.GetField(fieldName, flags);

            while(baseType != null && fieldInfo == null)
            {
                fieldInfo = baseType?.GetField(fieldName, flags);
                baseType = baseType.BaseType;
            }

            return fieldInfo;
        }

        public static bool TryGetIndexInArray(this SerializedProperty property, out int index)
        {
            var path = property.propertyPath;
            if (!path.EndsWith("]"))
            {
                index = -1;
                return false;
            }
            var lastBracketIndex = path.LastIndexOf('[') + 1;
            if (lastBracketIndex < 0)
            {
                index = -1;
                return false;
            }
            return int.TryParse(path.Substring(lastBracketIndex, path.Length - lastBracketIndex - 1), out index);
        }

        public static SerializedProperty GetParent(this SerializedProperty property)
        {
            // Handle the prop.Array.data[i] types
            var path = property.propertyPath;
            var lastDotIndex = path.LastIndexOf('.');
            if(lastDotIndex < 0)
            {
                return null;
            }

            var parentPath = path[..lastDotIndex];
            int elemIndex = -1;
            if (parentPath.EndsWith("]"))
            {
                var lastBracketIndex = parentPath.LastIndexOf('[') + 1;
                if(lastBracketIndex < 0)
                {
                    return null;
                }
                elemIndex = int.Parse(parentPath.Substring(lastBracketIndex, parentPath.Length - lastBracketIndex - 1));
                parentPath = parentPath[..(lastBracketIndex - ".data[".Length)];
            }

            while (parentPath.EndsWith(".Array"))
            {
                // Here we have an array --> need to get the parent of this array
                const int arrayLength = 6; //".Array".Length;
                parentPath = parentPath[..^arrayLength];
            }

            var parent = property.serializedObject.FindProperty(parentPath);
            return elemIndex < 0 ? parent : parent.arraySize > elemIndex ? parent.GetArrayElementAtIndex(elemIndex) : null;
        }
        
        public static T GetValue<T>(this SerializedProperty property)
        {
            if (property == null)
            {
                return default;
            }
            var value = property.GetValue();
            if (value is T tValue)
            {
                return tValue;
            }
            if (value == null && typeof(T).IsValueType)
            {
                return default;
            }
            throw new InvalidCastException($"Cannot cast {value?.GetType().Name} to {typeof(T).Name}");
        }

        public static object GetValue(this SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Generic => GetGenericValue(property),
                SerializedPropertyType.Integer => property.intValue,
                SerializedPropertyType.Boolean => property.boolValue,
                SerializedPropertyType.Float => property.floatValue,
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.Color => property.colorValue,
                SerializedPropertyType.ObjectReference => property.objectReferenceValue,
                SerializedPropertyType.LayerMask => property.intValue,
                SerializedPropertyType.Enum => property.enumValueIndex,
                SerializedPropertyType.Vector2 => property.vector2Value,
                SerializedPropertyType.Vector3 => property.vector3Value,
                SerializedPropertyType.Vector4 => property.vector4Value,
                SerializedPropertyType.Rect => property.rectValue,
                SerializedPropertyType.ArraySize => property.intValue,
                SerializedPropertyType.Character => property.stringValue[0],
                SerializedPropertyType.AnimationCurve => property.animationCurveValue,
                SerializedPropertyType.Bounds => property.boundsValue,
                SerializedPropertyType.Gradient => property.gradientValue,
                SerializedPropertyType.Quaternion => property.quaternionValue,
                SerializedPropertyType.ExposedReference => property.exposedReferenceValue,
                SerializedPropertyType.FixedBufferSize => property.fixedBufferSize,
                SerializedPropertyType.Vector2Int => property.vector2IntValue,
                SerializedPropertyType.Vector3Int => property.vector3IntValue,
                SerializedPropertyType.RectInt => property.rectIntValue,
                SerializedPropertyType.BoundsInt => property.boundsIntValue,
                SerializedPropertyType.ManagedReference => property.managedReferenceValue,
                SerializedPropertyType.Hash128 => property.hash128Value,
                _ => property.boxedValue
            };
        }

        public static void SetValue(this SerializedProperty property, object value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic: 
                    SetGenericValue(property, value);
                    break;
                case SerializedPropertyType.Integer: property.intValue = (int)value; break;
                case SerializedPropertyType.Boolean: property.boolValue = (bool)value;  break;
                case SerializedPropertyType.Float: 
                    if(property.numericType == SerializedPropertyNumericType.Double)
                    {
                        property.doubleValue = (double)value;
                    }
                    else
                    {
                        property.floatValue = (float)value;
                    }

                    break;
                case SerializedPropertyType.String: property.stringValue = (string)value;  break;
                case SerializedPropertyType.Color: property.colorValue = (Color)value;  break;
                case SerializedPropertyType.ObjectReference: property.objectReferenceValue = (UnityEngine.Object)value;  break;
                case SerializedPropertyType.LayerMask: property.intValue = (LayerMask)value;  break;
                case SerializedPropertyType.Enum: property.enumValueIndex = (int)value;  break;
                case SerializedPropertyType.Vector2: property.vector2Value = (Vector2)value;  break;
                case SerializedPropertyType.Vector3: property.vector3Value = (Vector3)value;  break;
                case SerializedPropertyType.Vector4: property.vector4Value = (Vector4)value;  break;
                case SerializedPropertyType.Rect: property.rectValue = (Rect)value;  break;
                case SerializedPropertyType.ArraySize: property.intValue = (int)value;  break;
                case SerializedPropertyType.Character: property.stringValue = (string)value; break;
                case SerializedPropertyType.AnimationCurve: property.animationCurveValue = (AnimationCurve)value;  break;
                case SerializedPropertyType.Bounds: property.boundsValue = (Bounds)value;  break;
                case SerializedPropertyType.Gradient: property.gradientValue = (Gradient)value; break;
                case SerializedPropertyType.Quaternion: property.quaternionValue = (Quaternion)value;  break;
                case SerializedPropertyType.ExposedReference: property.exposedReferenceValue = (UnityEngine.Object)value;  break;
                case SerializedPropertyType.FixedBufferSize: break;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = (Vector2Int)value;  break;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = (Vector3Int)value;  break;
                case SerializedPropertyType.RectInt: property.rectIntValue = (RectInt)value;  break;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = (BoundsInt)value;  break;
                case SerializedPropertyType.ManagedReference: property.managedReferenceValue = value; break;
                case SerializedPropertyType.Hash128: property.hash128Value = (Hash128)value;  break;
                default:
                    property.boxedValue = value; break;
            }
        }

        public static void ResetValue(this SerializedProperty property)
        {
            if (property.isArray)
            {
                var size = property.arraySize;
                for (int i = 0; i < size; i++)
                {
                    property.DeleteArrayElementAtIndex(0);
                }
                return;
            }
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:
                    SetGenericValue(property, null);
                    break;
                case SerializedPropertyType.Integer: property.intValue = default; break;
                case SerializedPropertyType.Boolean: property.boolValue = default; break;
                case SerializedPropertyType.Float: property.floatValue = default; break;
                case SerializedPropertyType.String: property.stringValue = default; break;
                case SerializedPropertyType.Color: property.colorValue = default; break;
                case SerializedPropertyType.ObjectReference: property.objectReferenceValue = null; break;
                case SerializedPropertyType.LayerMask: property.intValue = default; break;
                case SerializedPropertyType.Enum: property.enumValueIndex = default; break;
                case SerializedPropertyType.Vector2: property.vector2Value = default; break;
                case SerializedPropertyType.Vector3: property.vector3Value = default; break;
                case SerializedPropertyType.Vector4: property.vector4Value = default; break;
                case SerializedPropertyType.Rect: property.rectValue = default; break;
                case SerializedPropertyType.ArraySize: property.intValue = default; break;
                case SerializedPropertyType.Character: property.stringValue = default; break;
                case SerializedPropertyType.AnimationCurve: property.animationCurveValue = default; break;
                case SerializedPropertyType.Bounds: property.boundsValue = default; break;
                case SerializedPropertyType.Gradient: property.gradientValue = default; break;
                case SerializedPropertyType.Quaternion: property.quaternionValue = default; break;
                case SerializedPropertyType.ExposedReference: property.exposedReferenceValue = null; break;
                case SerializedPropertyType.FixedBufferSize: break;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = default; break;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = default; break;
                case SerializedPropertyType.RectInt: property.rectIntValue = default; break;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = default; break;
                case SerializedPropertyType.ManagedReference: property.managedReferenceValue = null; break;
                case SerializedPropertyType.Hash128: property.hash128Value = default; break;
                default:
                    property.boxedValue = default; break;
            }
        }


        private static void SetGenericValue(SerializedProperty property, object value)
        {
            if (value == null) { return; }
            var fieldInfo = property.GetFieldInfo();
            var parentProperty = property.GetParent();
            var parentValue = parentProperty != null ? parentProperty.GetValue() : property.serializedObject.targetObject;

            if(fieldInfo != null && !fieldInfo.IsInitOnly)
            {
                try
                {
                    int index = -1;
                    if(property.TryGetIndexInArray(out index) != true)
                    {
                        // Not in an array apparently
                        fieldInfo.SetFastValue(parentValue, value);
                        return;
                    }
                    else if (parentValue is Array array)
                    {
                        array.SetValue(value, index);
                        return;
                    }
                    else if (parentValue is IList list)
                    {
                        list[index] = value;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                finally
                {
                    parentProperty?.Dispose();
                }
            };

            using (var nextProperty = property.Copy())
            using (var innerProperty = property.Copy())
            {
                nextProperty.Next(false);
                var enterChildren = true;
                while (innerProperty.Next(enterChildren) && innerProperty.propertyPath != nextProperty.propertyPath)
                {
                    enterChildren = false;
                    var innerFieldInfo = innerProperty.GetFieldInfo();
                    if (innerFieldInfo == null) { continue; }
                    SetValue(innerProperty, innerFieldInfo.GetFastValue(value));
                }
            }
        }

        public static void CopyFrom(this SerializedProperty property, SerializedProperty other)
        {
            if(property.propertyType != other.propertyType || property.type != other.type)
            {
                throw new ArgumentException($"CopyFrom({property.propertyPath}, {other.propertyPath}): Properties are not of the same type");
            }
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:
                    var children = property.CountInProperty() - 1;
                    if(children <= 0)
                    {
                        break;
                    }
                    
                    using (var innerProperty = property.Copy())
                    using (var inner2Property = other.Copy())
                    {
                        innerProperty.Next(true);
                        inner2Property.Next(true);
                        for (int i = 0; i < children; i++)
                        {
                            CopyFrom(innerProperty, inner2Property);
                            innerProperty.Next(false);
                            inner2Property.Next(false);
                        }
                    }
                    // property.boxedValue = other.boxedValue;
                    break;
                case SerializedPropertyType.Integer: property.intValue = other.intValue; break;
                case SerializedPropertyType.Boolean: property.boolValue = other.boolValue; break;
                case SerializedPropertyType.Float: property.floatValue = other.floatValue; break;
                case SerializedPropertyType.String: property.stringValue = other.stringValue; break;
                case SerializedPropertyType.Color: property.colorValue = other.colorValue; break;
                case SerializedPropertyType.ObjectReference: property.objectReferenceValue = other.objectReferenceValue; break;
                case SerializedPropertyType.LayerMask: property.intValue = other.intValue; break;
                case SerializedPropertyType.Enum: property.enumValueIndex = other.enumValueIndex; break;
                case SerializedPropertyType.Vector2: property.vector2Value = other.vector2Value; break;
                case SerializedPropertyType.Vector3: property.vector3Value = other.vector3Value; break;
                case SerializedPropertyType.Vector4: property.vector4Value = other.vector4Value; break;
                case SerializedPropertyType.Rect: property.rectValue = other.rectValue; break;
                case SerializedPropertyType.ArraySize: property.intValue = other.intValue; break;
                case SerializedPropertyType.Character: property.stringValue = other.stringValue; break;
                case SerializedPropertyType.AnimationCurve: property.animationCurveValue = other.animationCurveValue; break;
                case SerializedPropertyType.Bounds: property.boundsValue = other.boundsValue; break;
                case SerializedPropertyType.Gradient: property.gradientValue = other.gradientValue; break;
                case SerializedPropertyType.Quaternion: property.quaternionValue = other.quaternionValue; break;
                case SerializedPropertyType.ExposedReference: property.exposedReferenceValue = other.exposedReferenceValue; break;
                case SerializedPropertyType.FixedBufferSize: break;
                case SerializedPropertyType.Vector2Int: property.vector2IntValue = other.vector2IntValue; break;
                case SerializedPropertyType.Vector3Int: property.vector3IntValue = other.vector3IntValue; break;
                case SerializedPropertyType.RectInt: property.rectIntValue = other.rectIntValue; break;
                case SerializedPropertyType.BoundsInt: property.boundsIntValue = other.boundsIntValue; break;
                case SerializedPropertyType.ManagedReference: property.managedReferenceValue = other.GetGenericValue(); break;
                case SerializedPropertyType.Hash128: property.hash128Value = other.hash128Value; break;
                default:
                    property.boxedValue = other.boxedValue; break;
            }
        }

        private static object GetGenericValue(this SerializedProperty property)
        {
            var fieldInfo = property.GetFieldInfo();
            var parentProperty = property.GetParent();
            var parentValue = parentProperty != null ? parentProperty.GetValue() : property.serializedObject.targetObject;
            try
            {
                int index = -1;
                if (property.TryGetIndexInArray(out index) != true)
                {
                    // Not in an array apparently
                    return fieldInfo?.GetFastValue(parentValue);
                }
                else if (parentValue is Array array)
                {
                    return array.GetValue(index);
                }
                else if (parentValue is IList list && list.Count > index)
                {
                    return list[index];
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                parentProperty?.Dispose();
            }
            return default;
        }

        public static string GetFullPath(this SerializedProperty property)
        {
            return string.Concat(property.serializedObject.targetObject.GetType().FullName, '.', property.propertyPath);
        }

        private static (Type, string) CreateKey(this SerializedProperty property)
        {
            if(property.propertyType == SerializedPropertyType.ManagedReference)
            {
                return (property.serializedObject.targetObject.GetType(), property.propertyPath + property.managedReferenceFullTypename);
            }

            return (property.serializedObject.targetObject.GetType(), property.propertyPath);
        }

        public static Type GetPropertyType(this SerializedProperty property, bool pathMayBeComplex = true)
        {
            if(_propertyTypesCache.TryGetValue(property.CreateKey(), out Type propertyType))
            {
                return propertyType;
            }
            var type = Type.GetType(property.type, false);
            if(type != null)
            {
                _propertyTypesCache[property.CreateKey()] = type;
                return type; 
            }

            if(property.propertyType == SerializedPropertyType.ManagedReference 
                && !string.IsNullOrEmpty(property.managedReferenceFullTypename))
            {
                var splitType = property.managedReferenceFullTypename.Split(' ');
                var assemblyName = splitType[0];
                var typename = splitType[1].Replace('/', '+');
                type = Type.GetType(typename, false);
                if(type == null)
                {
                    var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
                    type = assembly?.GetType(typename, false);
                }
                if(type != null)
                {
                    _propertyTypesCache[property.CreateKey()] = type;
                    return type;
                }
            }

            // find it the exhaustive way, starting from the origin UnityEngine.Object
            var parent = property.GetParent();
            var parentType = parent != null ? parent.GetPropertyType() : property.serializedObject.targetObject.GetType();
            if(property.isArray && property.propertyPath.EndsWith(".Array", StringComparison.Ordinal))
            {
                type = parentType;
            }
            else if (parentType.IsArray && parentType.HasElementType)
            {
                type = parentType.GetElementType();
            }
            else if (/*typeof(IEnumerable).IsAssignableFrom(parentType) || */typeof(IList).IsAssignableFrom(parentType))
            {
                while(parentType?.GetGenericArguments().Length == 0)
                {
                    parentType = parentType.BaseType;
                }
                if (parentType == null)
                {
                    throw new Exception($"Unable to determine type for: {property.propertyPath}");
                }
                type = parentType.GetGenericArguments()[0];
            }
            else
            {
                type = parentType.GetUnityReflectedType(property, !pathMayBeComplex);
                if(type == null)
                {
                    if (property.propertyPath.Contains('.'))
                    {
                        throw new Exception($"Unable to find a field for: {property.propertyPath}");
                    }

                    type = parentType;
                }
            }
            _propertyTypesCache[property.CreateKey()] = type;
            parent?.Dispose();
            return type;
        }

        private static Type GetUnityReflectedType(this Type type, SerializedProperty property, bool considerSOTarget)
        {
            if (type.TryGetUnityObjectProperty(property.propertyPath, out var propertyInfo, out _))
            {
                return propertyInfo.PropertyType;
            }

            if (!considerSOTarget) return type.GetUnityReflectedMemberType(property.name);
            
            var rootType = property.serializedObject.targetObject.GetType();
            if (rootType.TryGetUnityObjectProperty(property.propertyPath, out propertyInfo, out _, useFullPath: true))
            {
                return propertyInfo.PropertyType;
            }

            return type.GetUnityReflectedMemberType(property.name);
        }

        public static Type GetUnityReflectedMemberType(this Type type, string memberName)
        {
            var baseType = type.BaseType;
            var members = type.GetMember(memberName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            while(members?.Length != 1 && type.IsClass && baseType != typeof(object))
            {
                members = baseType.GetMember(memberName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                baseType = baseType.BaseType;
            }
            if(members?.Length > 0)
            {
                return members[0].GetMemberType();
            }

            bool recurse = false;
            // Check if it is a unity property
            if (memberName.StartsWith("m_", StringComparison.Ordinal))
            {
                memberName = memberName[2..];
                recurse = true;
            }
            if (memberName.StartsWith("_", StringComparison.Ordinal))
            {
                memberName = memberName[1..];
                recurse = true;
            }

            if (!recurse)
            {
                return null;
            }
            
            return GetUnityReflectedMemberType(type, memberName) 
                   ?? GetUnityReflectedMemberType(type, InvertFirstLetterCase(memberName));
        }

        private static string InvertFirstLetterCase(string memberName)
        {
            return char.IsUpper(memberName[0]) 
                ? char.ToLower(memberName[0]) + memberName[1..] 
                : char.ToUpper(memberName[0]) + memberName[1..];
        }

        public static bool IsAlive(this SerializedProperty property)
        {
            try
            {
                return property != null
                       && !IntPtr.Zero.Equals(NativePropertyPointerField(property))
                       && property.serializedObject.IsAlive();
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public static bool IsAlive(this SerializedObject serializedObject)
        {
            try
            {
                return serializedObject != null && !IntPtr.Zero.Equals(NativeObjectPointerField(serializedObject));
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public static bool UsesIMGUIDrawer(this SerializedProperty property)
        {
            return property.IsAlive() && DrawerSystem.HasIMGUIDrawer(property);
        }

        private static bool TryFindProperty(Func<string, SerializedProperty> getter, string path, out SerializedProperty nextProperty)
        {
            nextProperty = getter(path);
            if (nextProperty != null)
            {
                return true;
            }

            nextProperty = getter(char.IsUpper(path[0]) 
                         ? char.ToLower(path[0]) + path[1..] 
                         : char.ToUpper(path[0]) + path[1..]);

            if(nextProperty != null)
            {
                return true;
            }

            // Some Unity properties are not prefixed with "m_" so we try to find them without the prefix
            if (path.StartsWith("m_", StringComparison.Ordinal))
            {
                return false;
            }

            if (path.StartsWith("_", StringComparison.Ordinal))
            {
                return false;
            }

            nextProperty = getter("m_" + path);
            if (nextProperty != null)
            {
                return true;
            }

            nextProperty = getter("m_" + char.ToUpper(path[0]) + path[1..]);
            if (nextProperty != null)
            {
                return true;
            }

            nextProperty = getter("m_" + char.ToLower(path[0]) + path[1..]);
            if (nextProperty != null)
            {
                return true;
            }

            nextProperty = getter("_" + path);
            if (nextProperty != null)
            {
                return true;
            }

            nextProperty = getter("_" + char.ToUpper(path[0]) + path[1..]);
            if (nextProperty != null)
            {
                return true;
            }

            nextProperty = getter("_" + char.ToLower(path[0]) + path[1..]);

            return nextProperty != null;
        }

        public static bool TryGetNext(this SerializedProperty property, string path, out SerializedProperty nextProperty)
        {
            return TryFindProperty(property.FindPropertyRelative, path, out nextProperty);
        }

        public static bool TryGetNext(this SerializedObject serObject, string path, out SerializedProperty nextProperty)
        {
            return TryFindProperty(serObject.FindProperty, path, out nextProperty);
        }

        public static bool TryFindLastProperty(this SerializedObject serObject, string path, out SerializedProperty lastProperty)
        {
            var parts = path.Replace('/', '.').Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                lastProperty = null;
                return false;
            }
            if (!serObject.TryGetNext(parts[0], out lastProperty))
            {
                return false;
            }
            for (int i = 1; i < parts.Length; i++)
            {
                if (!lastProperty.TryGetNext(parts[i], out lastProperty))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
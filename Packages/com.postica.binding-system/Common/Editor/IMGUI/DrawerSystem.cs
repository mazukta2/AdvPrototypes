using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Postica.Common
{
    public partial class DrawerSystem
    {
        internal class ReplacesPropertyDrawerAttribute : Attribute
        {
            public Type DrawerType { get; }
            public Type[] FieldTypes { get; }

            public ReplacesPropertyDrawerAttribute(Type drawerType, params Type[] fieldTypes)
            {
                DrawerType = drawerType;
                FieldTypes = fieldTypes;
            }

            public ReplacesPropertyDrawerAttribute(string type)
            {
                DrawerType = Type.GetType(type, false);
            }
        }
        
        private class EmptyType { }
        
        private static readonly Dictionary<Type, Type> _drawerAttributes = new();
        private static readonly Dictionary<FieldInfo, (Type drawerType, PropertyAttribute attr)[]> _fieldDrawers = new Dictionary<FieldInfo, (Type, PropertyAttribute)[]>();
        private static readonly Dictionary<(Type ownerType, string path), bool> _useIMGUIDrawer = new();
        private static Action<PropertyDrawer, FieldInfo> _setFieldInfo;
        private static Action<PropertyDrawer, PropertyAttribute> _attributeSetter;
        
        private static readonly Dictionary<string, object> _originalData = new();

        private static Func<bool> _isIMGUIInspector;
        internal static bool IsIMGUIInspector()
        {
            if (_isIMGUIInspector != null)
            {
                return _isIMGUIInspector();
            }

            var property = typeof(EditorSettings).GetProperty("inspectorUseIMGUIDefaultInspector",
                                                  BindingFlags.Static | BindingFlags.NonPublic);
            if (property == null)
            {
                // Check if is Unity 2022 or newer
#if UNITY_2022_3_OR_NEWER
                _isIMGUIInspector = () => false;
#else
                _isIMGUIInspector = () => true;
#endif
                return true;
            }

            var getMethod = property.GetMethod.CreateDelegate(typeof(Func<bool>), null) as Func<bool>;
#if UNITY_2022_3_OR_NEWER
            _isIMGUIInspector = getMethod ?? (() => false);
#else
            _isIMGUIInspector = getMethod ?? (() => true);
#endif
            return _isIMGUIInspector();
        } 

        internal static void Initialize()
        {
            var getFieldType = FieldGetter<CustomPropertyDrawer, Type>("m_Type");
            var setFieldType = FieldSetter<CustomPropertyDrawer, Type>("m_Type");
            var drawerReplacements = TypeCache.GetTypesWithAttribute<ReplacesPropertyDrawerAttribute>()
                .Select(t => (t, t.GetCustomAttribute<ReplacesPropertyDrawerAttribute>()))
                .Select(p => (p.t, p.Item2.DrawerType, p.Item2.FieldTypes)).ToDictionary(p => p.DrawerType);
            
            foreach (var drawerType in TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>())
            {
                if (!typeof(PropertyDrawer).IsAssignableFrom(drawerType)) continue;

                if (drawerReplacements.TryGetValue(drawerType, out var tuple))
                {
                    foreach (var attribute in drawerType.GetCustomAttributes<CustomPropertyDrawer>())
                    {
                        Type fieldType = null;
                        try
                        {
                            fieldType = getFieldType(attribute);
                            if (tuple.FieldTypes?.Length > 0 && !tuple.FieldTypes.Contains(fieldType))
                            {
                                continue;
                            }

                            setFieldType(attribute, typeof(EmptyType));
                            ScriptAttributeUtility.DrawerStaticTypesCache[fieldType] = tuple.t;
                        }
                        catch(Exception e)
                        {
                            Debug.LogWarning($"Unable to override drawer {tuple.DrawerType} with drawer {tuple.t} for type {fieldType}: {e}");
                        }
                    }

                    continue;
                }
                
                foreach (var attribute in drawerType.GetCustomAttributes<CustomPropertyDrawer>())
                {
                    _drawerAttributes[getFieldType(attribute)] = drawerType;
                }
            }

            _setFieldInfo = FieldSetter<PropertyDrawer, FieldInfo>("m_FieldInfo");
            _attributeSetter = FieldSetter<PropertyDrawer, PropertyAttribute>("m_Attribute");
        }

        public static bool HasIMGUIDrawer(SerializedProperty property)
        {
            if (IsIMGUIInspector())
            {
                return true;
            }
            
            if(_useIMGUIDrawer.TryGetValue((property.serializedObject.targetObject.GetType(), property.propertyPath), out var value))
            {
                return value;
            }
            
            var handler = ScriptAttributeUtility.GetHandler(property);
            using (handler.ApplyNestingContext(0).Instance as IDisposable)
            {
                if (handler.hasPropertyDrawer)
                {
                    var visualElement = handler.propertyDrawer.CreatePropertyGUI(property.Copy());
                    value = visualElement == null;
                }
            }
            
            _useIMGUIDrawer[(property.serializedObject.targetObject.GetType(), property.propertyPath)] = value;
            return value;
        }
        
        internal static PropertyDrawer GetCurrentGlobalDrawer(SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            var handler = ScriptAttributeUtility.GetHandler(property);
            return handler?.propertyDrawer;
        }
        
        internal static PropertyDrawer GetCurrentLocalDrawer(SerializedProperty property)
        {
            if (property == null)
            {
                return null;
            }

            var cache = ScriptAttributeUtility.propertyHandlerCache;
            var handler = cache.GetHandler(property);
            return handler?.propertyDrawer;
        }
        
        internal static void SetCurrentLocalDrawer(SerializedProperty property, PropertyDrawer drawer)
        {
            if (property == null)
            {
                return;
            }

            var cache = ScriptAttributeUtility.propertyHandlerCache;
            if (cache.Instance == ScriptAttributeUtility.GlobalCache.Instance)
            {
                cache.NewInstance();
                ScriptAttributeUtility.propertyHandlerCache = cache;
            }

            var handler = cache.GetHandler(property);
            if (handler == null)
            {
                handler = new PropertyHandlerProxy();
                handler.NewInstance();
                cache.SetHandler(property, handler);
            }
            
            handler.EnsureInitialized().PropertyDrawers.Insert(0, drawer);
        }
        
        internal static void SetGlobalPropertyDrawer(SerializedProperty property, PropertyDrawer drawer)
        {
            if (property == null)
            {
                return;
            }

            var handler = ScriptAttributeUtility.GetHandler(property);
            var propertyDrawers = handler.EnsureInitialized().PropertyDrawers;
            if(propertyDrawers == null)
            {
                propertyDrawers = new List<PropertyDrawer>();
                handler.PropertyDrawers = propertyDrawers;
            }
            propertyDrawers.Remove(drawer);
            propertyDrawers.Insert(0, drawer);
        }
        
        
        public static void RemoveGlobalPropertyDrawer(SerializedProperty property, PropertyDrawer drawer)
        {
            if (!property.IsAlive())
            {
                return;
            }
            var handler = ScriptAttributeUtility.GetHandler(property);
            handler.PropertyDrawers?.RemoveAll(d => d == drawer);
        }

        public static void SetMaterialPropertyDrawer(Material material, MaterialPropertyDrawer drawer)
        {
            // For each visible property in the material, assign the same drawer
            var shader = material.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                var propertyName = shader.GetPropertyName(i);
                var propertyType = shader.GetPropertyType(i);
                if (propertyType == ShaderPropertyType.Texture)
                {
                    SetMaterialPropertyDrawer(material, propertyName + "_ST", drawer);
                }
                SetMaterialPropertyDrawer(material, propertyName, drawer);
            }
        }
        
        public static void RestoreMaterialPropertyDrawer(Material material)
        {
            // For each visible property in the material, assign the same drawer
            var shader = material.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                var propertyName = shader.GetPropertyName(i);
                var propertyType = shader.GetPropertyType(i);
                if (propertyType == ShaderPropertyType.Texture)
                {
                    RestoreMaterialPropertyDrawer(material, propertyName + "_ST");
                }
                RestoreMaterialPropertyDrawer(material, propertyName);
            }
        }

        public static void SetMaterialPropertyDrawer(Material material, string property, MaterialPropertyDrawer drawer)
        {
            var key = MaterialPropertyHandler.GetPropertyString(material.shader, property);
            if (!MaterialPropertyHandler.PropertyHandlers.TryGetValue(key, out var handler) || handler == null)
            {
                handler = new MaterialPropertyHandlerProxy();
                handler.NewInstance();
                MaterialPropertyHandler.PropertyHandlers[key] = handler;
            }
            if(!_originalData.ContainsKey(key))
            {
                _originalData[key] = handler.PropertyDrawer;
            }
            if(drawer is StackedMaterialPropertyDrawer stackedDrawer 
               && _originalData.TryGetValue(key, out var prevDrawer))
            {
                stackedDrawer.SetDrawer(property, prevDrawer as MaterialPropertyDrawer);
            }
            handler.PropertyDrawer = drawer;
        }
        
        public static void RestoreMaterialPropertyDrawer(Material material, string property)
        {
            if (!material)
            {
                return;
            }
            
            var key = MaterialPropertyHandler.GetPropertyString(material.shader, property);
            if(!_originalData.TryGetValue(key, out var data))
            {
                return;
            }
            if (MaterialPropertyHandler.PropertyHandlers.TryGetValue(key, out var handler) && handler != null)
            {
                handler.PropertyDrawer = data as MaterialPropertyDrawer;
            }
        }

        internal static void Inject(PropertyDrawer drawer, FieldInfo fieldInfo)
        {
            if (drawer != null)
            {
                _setFieldInfo(drawer, fieldInfo);
            }
        }

        internal static PropertyDrawer GetDrawerFor(Type type, SerializedProperty property)
        {
            Type objType = type;
            PropertyDrawer drawer = null;
            while (objType != null)
            {
                if (_drawerAttributes.TryGetValue(objType, out var drawerType))
                {
                    drawer = Activator.CreateInstance(drawerType) as PropertyDrawer;
                    break;
                }
                else if(objType.IsGenericType && _drawerAttributes.TryGetValue(objType.GetGenericTypeDefinition(), out drawerType))
                {
                    drawer = Activator.CreateInstance(drawerType) as PropertyDrawer;
                    break;
                }
                objType = objType.BaseType;
            }

            if(drawer != null && property != null)
            {
                var fieldInfo = property.GetFieldInfo();
                _setFieldInfo(drawer, fieldInfo);
            }
            return drawer;
        }

        internal static bool TryGetNextDrawer(PropertyDrawer current, out PropertyDrawer next, bool checkInOrder = true)
        {
            if (!_fieldDrawers.TryGetValue(current.fieldInfo, out (Type type, PropertyAttribute attr)[] drawers))
            {
                List<(Type, PropertyAttribute)> types = new List<(Type, PropertyAttribute)>();
                foreach (var attr in current.fieldInfo.GetCustomAttributes<PropertyAttribute>())
                {
                    if (_drawerAttributes.TryGetValue(attr.GetType(), out Type drawerType))
                    {
                        types.Add((drawerType, attr));
                    }
                }
                if(_drawerAttributes.TryGetValue(current.fieldInfo.FieldType, out Type fieldDrawerType))
                {
                    types.Add((fieldDrawerType, null));
                }
                drawers = types.ToArray();
                _fieldDrawers[current.fieldInfo] = drawers;
            }

            next = null;
            if (drawers.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < drawers.Length; i++)
            {
                if (drawers[i].type == current.GetType())
                {
                    if (i + 1 < drawers.Length)
                    {
                        try
                        {
                            next = Activator.CreateInstance(drawers[i + 1].type) as PropertyDrawer;
                            _setFieldInfo(next, current.fieldInfo);
                            _attributeSetter(next, drawers[i + 1].attr);
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                }
                else if (!checkInOrder)
                {
                    try
                    {
                        next = Activator.CreateInstance(drawers[i].type) as PropertyDrawer;
                        _setFieldInfo(next, current.fieldInfo);
                        _attributeSetter(next, drawers[i].attr);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            return false;
        }

        private static Action<T, S> FieldSetter<T, S>(string fieldName)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fieldInfo = typeof(T).GetField(fieldName, flags);

            var sourceParam = Expression.Parameter(typeof(T));
            var valueParam = Expression.Parameter(typeof(S));
            Expression body = Expression.Assign(Expression.Field(sourceParam, fieldInfo), valueParam);
            var lambda = Expression.Lambda(typeof(Action<T, S>), body, sourceParam, valueParam);
            return (Action<T, S>)lambda.Compile();
        }

        private static Func<T, S> FieldGetter<T, S>(string fieldName)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var fieldInfo = typeof(T).GetField(fieldName, flags);

            var sourceParam = Expression.Parameter(typeof(T));
            Expression body = Expression.Field(sourceParam, fieldInfo);
            var lambda = Expression.Lambda<Func<T, S>>(body, sourceParam);
            return lambda.Compile();
        }
    }
}
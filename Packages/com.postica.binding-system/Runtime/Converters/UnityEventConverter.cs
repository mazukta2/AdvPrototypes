using System;
using System.Linq;
using System.Reflection;
using Postica.Common;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Converters
{
    /// <summary>
    /// This class contains all <see cref="FromUnityEventConverter{T}"/> converters for default types.
    /// </summary>
    public static class UnityEventConverters
    {
        [Serializable] private sealed class FromUnityEventConverter_bool : FromUnityEventConverter<bool>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_byte : FromUnityEventConverter<byte>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_char : FromUnityEventConverter<char>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_short : FromUnityEventConverter<short>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_ushort : FromUnityEventConverter<ushort>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_int : FromUnityEventConverter<int>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_uint : FromUnityEventConverter<uint>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_long : FromUnityEventConverter<long>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_ulong : FromUnityEventConverter<ulong>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_float : FromUnityEventConverter<float>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_double : FromUnityEventConverter<double>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_string : FromUnityEventConverter<string>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Vector2 : FromUnityEventConverter<Vector2>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Vector2Int : FromUnityEventConverter<Vector2Int>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Vector3 : FromUnityEventConverter<Vector3>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Vector3Int : FromUnityEventConverter<Vector3Int>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Vector4 : FromUnityEventConverter<Vector4>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Color : FromUnityEventConverter<Color>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Color32 : FromUnityEventConverter<Color32>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Bounds : FromUnityEventConverter<Bounds>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_BoundsInt : FromUnityEventConverter<BoundsInt>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Rect : FromUnityEventConverter<Rect>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_RectInt : FromUnityEventConverter<RectInt>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_DateTime : FromUnityEventConverter<DateTime>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_TimeSpan : FromUnityEventConverter<TimeSpan>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_object : FromUnityEventConverter<object>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Object : FromUnityEventConverter<UnityEngine.Object>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Transform : FromUnityEventConverter<Transform>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_GameObject : FromUnityEventConverter<GameObject>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Collider : FromUnityEventConverter<Collider>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Material : FromUnityEventConverter<Material>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Renderer : FromUnityEventConverter<Renderer>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Texture : FromUnityEventConverter<Texture>
        {
        }

        [Serializable] private sealed class FromUnityEventConverter_Texture2D : FromUnityEventConverter<Texture2D>
        {
        }
        
        
        
        [Serializable] private sealed class ToUnityEventConverter_bool : ToUnityEventConverter<bool>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_byte : ToUnityEventConverter<byte>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_char : ToUnityEventConverter<char>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_short : ToUnityEventConverter<short>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_ushort : ToUnityEventConverter<ushort>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_int : ToUnityEventConverter<int>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_uint : ToUnityEventConverter<uint>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_long : ToUnityEventConverter<long>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_ulong : ToUnityEventConverter<ulong>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_float : ToUnityEventConverter<float>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_double : ToUnityEventConverter<double>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_string : ToUnityEventConverter<string>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Vector2 : ToUnityEventConverter<Vector2>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Vector2Int : ToUnityEventConverter<Vector2Int>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Vector3 : ToUnityEventConverter<Vector3>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Vector3Int : ToUnityEventConverter<Vector3Int>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Vector4 : ToUnityEventConverter<Vector4>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Color : ToUnityEventConverter<Color>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Color32 : ToUnityEventConverter<Color32>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Bounds : ToUnityEventConverter<Bounds>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_BoundsInt : ToUnityEventConverter<BoundsInt>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Rect : ToUnityEventConverter<Rect>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_RectInt : ToUnityEventConverter<RectInt>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_DateTime : ToUnityEventConverter<DateTime>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_TimeSpan : ToUnityEventConverter<TimeSpan>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_object : ToUnityEventConverter<object>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Object : ToUnityEventConverter<UnityEngine.Object>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Transform : ToUnityEventConverter<Transform>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_GameObject : ToUnityEventConverter<GameObject>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Collider : ToUnityEventConverter<Collider>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Material : ToUnityEventConverter<Material>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Renderer : ToUnityEventConverter<Renderer>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Texture : ToUnityEventConverter<Texture>
        {
        }

        [Serializable] private sealed class ToUnityEventConverter_Texture2D : ToUnityEventConverter<Texture2D>
        {
        }

        /// <summary>
        /// Registers all defined converters
        /// </summary>
        public static void RegisterDefaultTypes()
        {
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_bool>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_byte>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_char>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_short>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_ushort>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_int>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_uint>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_long>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_ulong>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_float>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_double>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_string>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Vector2>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Vector2Int>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Vector3>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Vector3Int>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Vector4>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Color>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Color32>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Bounds>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_BoundsInt>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Rect>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_RectInt>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_object>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Object>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Transform>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_GameObject>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Collider>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Material>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Renderer>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Texture>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_Texture2D>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_DateTime>();
            ConvertersFactory.RegisterTemplate<FromUnityEventConverter_TimeSpan>();
            
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_bool>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_byte>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_char>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_short>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_ushort>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_int>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_uint>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_long>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_ulong>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_float>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_double>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_string>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Vector2>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Vector2Int>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Vector3>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Vector3Int>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Vector4>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Color>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Color32>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Bounds>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_BoundsInt>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Rect>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_RectInt>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_object>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Object>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Transform>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_GameObject>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Collider>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Material>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Renderer>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Texture>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_Texture2D>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_DateTime>();
            ConvertersFactory.RegisterTemplate<ToUnityEventConverter_TimeSpan>();
        }
    }

    /// <summary>
    /// A converter which converts from a UnityEvent of type <typeparamref name="T"/> to a value of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the UnityEvent generic argument</typeparam>
    [Serializable]
    [HideMember]
    public class FromUnityEventConverter<T>
        : IConverter<UnityEvent<T>, T>,
            IConverter<UnityEvent, T>,
            IContextConverter,
            IDynamicComponent
    {
        /// <summary>
        /// This bind value will be used to get the data from the UnityEvent
        /// </summary>
        [Tooltip("UnityEvent cannot return any value, so this bound value will be used instead.\n" +
                 "If this value is in <b>write mode</b> as well, the value will be written to this bound value.")]
        public Bind<T> backingValue = new Bind<T>(default(T)) { IsBound = true };

        [NonSerialized] public UnityEvent<T> unityEvent;
        [NonSerialized] public UnityEvent unityEventGeneric;

        /// <inheritdoc/>
        public string Id => "Unity Event Converter";

        /// <inheritdoc/>
        public string Description => "Converts a unity event to a value or vice-versa.\n" +
                                     "When writing the value, the UnityEvent will be invoked.";

        /// <inheritdoc/>
        public bool IsSafe => false;
        
        public bool IsDynamic => backingValue.IsBound;
        
        /// <inheritdoc/>
        public object Convert(object value)
        {
            if (value is T Tvalue)
            {
                return Convert(Tvalue);
            }

            if (value is UnityEvent<T> unityEvent)
            {
                return Convert(unityEvent);
            }

            return null;
        }

        /// <inheritdoc/>
        public T Convert(UnityEvent<T> value)
        {
            if (value != null)
            {
                unityEvent = value;
            }

            return backingValue.Value;
        }
        
        /// <inheritdoc/>
        public T Convert(UnityEvent value)
        {
            if (value != null)
            {
                unityEventGeneric = value;
            }

            return backingValue.Value;
        }

        public void SetContext(object context, Type contextType, string path)
        {
            try
            {
                var memberInfo = AccessorsFactory.GetMemberAtPath(context?.GetType() ?? contextType, path);
                if (memberInfo == null)
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Member not found.");
                    return;
                }

                if (!Application.isPlaying && (backingValue == null || backingValue.BindData is { IsValid: false }))
                {
                    FindSimilarValue(memberInfo, context, contextType, path);
                    return;
                }

                if (context == null)
                {
                    return;
                }
                
                if (memberInfo.GetValue(context) is UnityEvent<T> unityEvent)
                {
                    this.unityEvent = unityEvent;
                }
                else if (memberInfo.GetValue(context) is UnityEvent unityEventGeneric)
                {
                    this.unityEventGeneric = unityEventGeneric;
                }
                
                else
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Member Value is not a UnityEvent.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{GetType().Name}: Unable to set context. {e}");
            }
        }

        private void FindSimilarValue(MemberInfo memberInfo, object context, Type contextType, string path)
        {
            var partialPath = path.RemoveAtEnd(memberInfo.Name);
            var type = context?.GetType() ?? contextType ?? memberInfo.DeclaringType;
            var allMembers = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m is FieldInfo or PropertyInfo)
                .OrderBy(m => GetNameSimilarityScore(memberInfo.Name, m.Name));

            foreach (var member in allMembers)
            {
                if (member == memberInfo)
                {
                    continue;
                }

                if (member is FieldInfo fieldInfo)
                {
                    if (!typeof(T).IsAssignableFrom(fieldInfo.FieldType))
                    {
                        continue;
                    }

                    if (!fieldInfo.IsPublic && fieldInfo.GetCustomAttribute<SerializeField>() == null)
                    {
                        continue;
                    }

                    backingValue = new Bind<T>(context as Object, partialPath + fieldInfo.Name);
                    return;
                }

                if (member is PropertyInfo propertyInfo && propertyInfo.CanRead && propertyInfo.CanWrite)
                {
                    if (!typeof(T).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        continue;
                    }

                    if (!propertyInfo.GetMethod.IsPublic)
                    {
                        continue;
                    }

                    backingValue = new Bind<T>(context as Object, partialPath + propertyInfo.Name);
                    return;
                }
            }
        }

        private int GetNameSimilarityScore(string target, string candidate)
        {
            // A more advanced and fast method of calculating the similarity score can be used here
            return target.SimilarityDistance(candidate);
        }
    }
    
    /// <summary>
    /// A converter which converts from a UnityEvent of type <typeparamref name="T"/> to a value of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the UnityEvent generic argument</typeparam>
    [Serializable]
    [HideMember]
    public class ToUnityEventConverter<T>
        : IConverter<T, UnityEvent<T>>,
            IConverter<T, UnityEvent>,
            IContextConverter,
            IPeerConverter
    {
        [NonSerialized]
        private UnityEvent<T> _unityEvent;
        [NonSerialized]
        private UnityEvent _unityEventGeneric;

        /// <inheritdoc/>
        public string Id => "Unity Event Converter";

        /// <inheritdoc/>
        public string Description => "Invokes the UnityEvent at path with the passed value." +
                                     "\nTries to set the value to its analogous read converter, if any.";

        /// <inheritdoc/>
        public bool IsSafe => false;
        
        public IConverter OtherConverter { get; set; }

        /// <inheritdoc/>
        public object Convert(object value)
        {
            if (value is T Tvalue)
            {
                return Convert(Tvalue);
            }

            return null;
        }

        /// <inheritdoc/>
        public UnityEvent<T> Convert(T value)
        {
            if (OtherConverter is FromUnityEventConverter<T>
                {
                    backingValue: { CanWrite: true }
                } other)
            {
                other.backingValue.Value = value;
            }

            if (_unityEvent == null && OtherConverter is FromUnityEventConverter<T>
                {
                    unityEvent: not null
                } otherConverter)
            {
                _unityEvent = otherConverter.unityEvent;
            }
            _unityEvent?.Invoke(value);
            return _unityEvent;
        }
        
        UnityEvent IConverter<T, UnityEvent>.Convert(T value)
        {
            if (OtherConverter is FromUnityEventConverter<T>
                {
                    backingValue: { CanWrite: true }
                } other)
            {
                other.backingValue.Value = value;
            }

            if (_unityEvent == null && OtherConverter is FromUnityEventConverter<T>
                {
                    unityEventGeneric: not null
                } otherConverter)
            {
                _unityEventGeneric = otherConverter.unityEventGeneric;
            }
            _unityEventGeneric?.Invoke();
            return _unityEventGeneric;
        }

        public void SetContext(object context, Type contextType, string path)
        {
            try
            {
                var memberInfo = AccessorsFactory.GetMemberAtPath(context?.GetType() ?? contextType, path);
                if (memberInfo == null)
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Member not found.");
                    return;
                }
                
                if (context == null)
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Context is null.");
                    return;
                }

                if (memberInfo.GetValue(context) is UnityEvent<T> unityEvent)
                {
                    _unityEvent = unityEvent;
                }
                else if (memberInfo.GetValue(context) is UnityEvent unityEventGeneric)
                {
                    _unityEventGeneric = unityEventGeneric;
                }
                else
                {
                    Debug.LogWarning($"{GetType().Name}: Unable to set context. Member Value is not a UnityEvent.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{GetType().Name}: Unable to set context. {e}");
            }
        }
    }
}
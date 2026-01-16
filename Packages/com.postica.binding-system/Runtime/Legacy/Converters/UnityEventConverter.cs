using System;
using System.Linq;
using System.Reflection;
using Postica.Common;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This class contains all <see cref="FromUnityEventConverter{T}"/> converters for default types.
    /// </summary>
    internal static class UnityEventConverters
    {
        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_bool : FromUnityEventConverter<bool>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_byte : FromUnityEventConverter<byte>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_char : FromUnityEventConverter<char>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_short : FromUnityEventConverter<short>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_ushort : FromUnityEventConverter<ushort>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_int : FromUnityEventConverter<int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_uint : FromUnityEventConverter<uint>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_long : FromUnityEventConverter<long>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_ulong : FromUnityEventConverter<ulong>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_float : FromUnityEventConverter<float>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_double : FromUnityEventConverter<double>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_string : FromUnityEventConverter<string>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Vector2 : FromUnityEventConverter<Vector2>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Vector2Int : FromUnityEventConverter<Vector2Int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Vector3 : FromUnityEventConverter<Vector3>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Vector3Int : FromUnityEventConverter<Vector3Int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Vector4 : FromUnityEventConverter<Vector4>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Color : FromUnityEventConverter<Color>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Color32 : FromUnityEventConverter<Color32>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Bounds : FromUnityEventConverter<Bounds>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_BoundsInt : FromUnityEventConverter<BoundsInt>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Rect : FromUnityEventConverter<Rect>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_RectInt : FromUnityEventConverter<RectInt>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_DateTime : FromUnityEventConverter<DateTime>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_TimeSpan : FromUnityEventConverter<TimeSpan>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_object : FromUnityEventConverter<object>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Object : FromUnityEventConverter<UnityEngine.Object>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Transform : FromUnityEventConverter<Transform>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_GameObject : FromUnityEventConverter<GameObject>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Collider : FromUnityEventConverter<Collider>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Material : FromUnityEventConverter<Material>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Renderer : FromUnityEventConverter<Renderer>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Texture : FromUnityEventConverter<Texture>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class FromUnityEventConverter_Texture2D : FromUnityEventConverter<Texture2D>
        {
        }


        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_bool : ToUnityEventConverter<bool>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_byte : ToUnityEventConverter<byte>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_char : ToUnityEventConverter<char>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_short : ToUnityEventConverter<short>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_ushort : ToUnityEventConverter<ushort>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_int : ToUnityEventConverter<int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_uint : ToUnityEventConverter<uint>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_long : ToUnityEventConverter<long>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_ulong : ToUnityEventConverter<ulong>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_float : ToUnityEventConverter<float>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_double : ToUnityEventConverter<double>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_string : ToUnityEventConverter<string>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Vector2 : ToUnityEventConverter<Vector2>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Vector2Int : ToUnityEventConverter<Vector2Int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Vector3 : ToUnityEventConverter<Vector3>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Vector3Int : ToUnityEventConverter<Vector3Int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Vector4 : ToUnityEventConverter<Vector4>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Color : ToUnityEventConverter<Color>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Color32 : ToUnityEventConverter<Color32>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Bounds : ToUnityEventConverter<Bounds>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_BoundsInt : ToUnityEventConverter<BoundsInt>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Rect : ToUnityEventConverter<Rect>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_RectInt : ToUnityEventConverter<RectInt>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_DateTime : ToUnityEventConverter<DateTime>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_TimeSpan : ToUnityEventConverter<TimeSpan>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_object : ToUnityEventConverter<object>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Object : ToUnityEventConverter<UnityEngine.Object>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Transform : ToUnityEventConverter<Transform>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_GameObject : ToUnityEventConverter<GameObject>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Collider : ToUnityEventConverter<Collider>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Material : ToUnityEventConverter<Material>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Renderer : ToUnityEventConverter<Renderer>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Texture : ToUnityEventConverter<Texture>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ToUnityEventConverter_Texture2D : ToUnityEventConverter<Texture2D>
        {
        }
    }

    /// <summary>
    /// A converter which converts from a UnityEvent of type <typeparamref name="T"/> to a value of type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the UnityEvent generic argument</typeparam>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
    internal class FromUnityEventConverter<T>
        : IConverter<UnityEvent<T>, T>,
            IConverter<UnityEvent, T>,
            IContextConverter
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
    [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
    internal class ToUnityEventConverter<T>
        : IConverter<T, UnityEvent<T>>,
            IConverter<T, UnityEvent>,
            IContextConverter,
            IPeerConverter
    {
        [NonSerialized] private UnityEvent<T> _unityEvent;
        [NonSerialized] private UnityEvent _unityEventGeneric;

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
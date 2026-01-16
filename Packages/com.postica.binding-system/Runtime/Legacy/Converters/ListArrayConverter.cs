using System;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This class contains all <see cref="ListArrayConverter{T}"/> converters for default types.
    /// </summary>
    internal static class ListArrayConverters
    {
        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_bool : ListArrayConverter<bool>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_byte : ListArrayConverter<byte>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_char : ListArrayConverter<char>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_short : ListArrayConverter<short>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_ushort : ListArrayConverter<ushort>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_int : ListArrayConverter<int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_uint : ListArrayConverter<uint>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_long : ListArrayConverter<long>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_ulong : ListArrayConverter<ulong>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_float : ListArrayConverter<float>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_double : ListArrayConverter<double>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_string : ListArrayConverter<string>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Vector2 : ListArrayConverter<Vector2>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Vector2Int : ListArrayConverter<Vector2Int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Vector3 : ListArrayConverter<Vector3>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Vector3Int : ListArrayConverter<Vector3Int>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Vector4 : ListArrayConverter<Vector4>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Color : ListArrayConverter<Color>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Color32 : ListArrayConverter<Color32>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Bounds : ListArrayConverter<Bounds>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_BoundsInt : ListArrayConverter<BoundsInt>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Rect : ListArrayConverter<Rect>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_RectInt : ListArrayConverter<RectInt>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_DateTime : ListArrayConverter<DateTime>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_TimeSpan : ListArrayConverter<TimeSpan>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_object : ListArrayConverter<object>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Object : ListArrayConverter<UnityEngine.Object>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Transform : ListArrayConverter<Transform>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_GameObject : ListArrayConverter<GameObject>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Collider : ListArrayConverter<Collider>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Material : ListArrayConverter<Material>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Renderer : ListArrayConverter<Renderer>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Texture : ListArrayConverter<Texture>
        {
        }

        [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
        private sealed class ListArrayConverter_Texture2D : ListArrayConverter<Texture2D>
        {
        }
    }

    /// <summary>
    /// A converter which converts from List to Array of type <typeparamref name="T"/> and/or viceversa
    /// </summary>
    /// <typeparam name="T">The type of the element</typeparam>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
    internal class ListArrayConverter<T>
        : IConverter<T[], List<T>>,
            IConverter<List<T>, T[]>
    {
        /// <inheritdoc/>
        public string Id => "List Array Converter";

        /// <inheritdoc/>
        public string Description => "Converts a list to an array or viceversa.";

        /// <inheritdoc/>
        public bool IsSafe => true;

        /// <inheritdoc/>
        public object Convert(object value)
        {
            if (value is T[] array)
            {
                return Convert(array);
            }

            if (value is List<T> list)
            {
                return Convert(list);
            }

            return null;
        }

        /// <inheritdoc/>
        public List<T> Convert(T[] value)
        {
            return value == null ? null : new List<T>(value);
        }

        /// <inheritdoc/>
        public T[] Convert(List<T> value)
        {
            return value == null ? null : value.ToArray();
        }
    }
}
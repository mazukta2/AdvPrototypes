using System;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Converters
{
    /// <summary>
    /// This class contains all <see cref="ListArrayConverter{T}"/> converters for default types.
    /// </summary>
    public static class ListArrayConverters
    {
        [Serializable] private sealed class ListArrayConverter_bool : ListArrayConverter<bool> { }
        [Serializable] private sealed class ListArrayConverter_byte : ListArrayConverter<byte> { }
        [Serializable] private sealed class ListArrayConverter_char : ListArrayConverter<char> { }
        [Serializable] private sealed class ListArrayConverter_short : ListArrayConverter<short> { }
        [Serializable] private sealed class ListArrayConverter_ushort : ListArrayConverter<ushort> { }
        [Serializable] private sealed class ListArrayConverter_int : ListArrayConverter<int> { }
        [Serializable] private sealed class ListArrayConverter_uint : ListArrayConverter<uint> { }
        [Serializable] private sealed class ListArrayConverter_long : ListArrayConverter<long> { }
        [Serializable] private sealed class ListArrayConverter_ulong : ListArrayConverter<ulong> { }
        [Serializable] private sealed class ListArrayConverter_float : ListArrayConverter<float> { }
        [Serializable] private sealed class ListArrayConverter_double : ListArrayConverter<double> { }
        [Serializable] private sealed class ListArrayConverter_string : ListArrayConverter<string> { }
        [Serializable] private sealed class ListArrayConverter_Vector2 : ListArrayConverter<Vector2> { }
        [Serializable] private sealed class ListArrayConverter_Vector2Int : ListArrayConverter<Vector2Int> { }
        [Serializable] private sealed class ListArrayConverter_Vector3 : ListArrayConverter<Vector3> { }
        [Serializable] private sealed class ListArrayConverter_Vector3Int : ListArrayConverter<Vector3Int> { }
        [Serializable] private sealed class ListArrayConverter_Vector4 : ListArrayConverter<Vector4> { }
        [Serializable] private sealed class ListArrayConverter_Color : ListArrayConverter<Color> { }
        [Serializable] private sealed class ListArrayConverter_Gradient : ListArrayConverter<Gradient> { }
        [Serializable] private sealed class ListArrayConverter_Color32 : ListArrayConverter<Color32> { }
        [Serializable] private sealed class ListArrayConverter_Bounds : ListArrayConverter<Bounds> { }
        [Serializable] private sealed class ListArrayConverter_BoundsInt : ListArrayConverter<BoundsInt> { }
        [Serializable] private sealed class ListArrayConverter_Rect : ListArrayConverter<Rect> { }
        [Serializable] private sealed class ListArrayConverter_RectInt : ListArrayConverter<RectInt> { }
        [Serializable] private sealed class ListArrayConverter_DateTime : ListArrayConverter<DateTime> { }
        [Serializable] private sealed class ListArrayConverter_TimeSpan : ListArrayConverter<TimeSpan> { }
        [Serializable] private sealed class ListArrayConverter_object : ListArrayConverter<object> { }
        [Serializable] private sealed class ListArrayConverter_Object : ListArrayConverter<UnityEngine.Object> { }
        [Serializable] private sealed class ListArrayConverter_Transform : ListArrayConverter<Transform> { }
        [Serializable] private sealed class ListArrayConverter_GameObject : ListArrayConverter<GameObject> { }
        [Serializable] private sealed class ListArrayConverter_Collider : ListArrayConverter<Collider> { }
        [Serializable] private sealed class ListArrayConverter_Material : ListArrayConverter<Material> { }
        [Serializable] private sealed class ListArrayConverter_Renderer : ListArrayConverter<Renderer> { }
        [Serializable] private sealed class ListArrayConverter_Texture : ListArrayConverter<Texture> { }
        [Serializable] private sealed class ListArrayConverter_Texture2D : ListArrayConverter<Texture2D> { }

        /// <summary>
        /// Registers all defined converters
        /// </summary>
        public static void RegisterDefaultTypes()
        {
            ConvertersFactory.RegisterTemplate<ListArrayConverter_bool>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_byte>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_char>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_short>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_ushort>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_int>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_uint>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_long>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_ulong>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_float>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_double>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_string>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Vector2>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Vector2Int>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Vector3>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Vector3Int>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Vector4>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Color>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Gradient>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Color32>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Bounds>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_BoundsInt>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Rect>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_RectInt>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_object>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Object>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Transform>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_GameObject>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Collider>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Material>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Renderer>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Texture>();
            ConvertersFactory.RegisterTemplate<ListArrayConverter_Texture2D>();
        }
    }

    /// <summary>
    /// A converter which converts from List to Array of type <typeparamref name="T"/> and/or viceversa
    /// </summary>
    /// <typeparam name="T">The type of the element</typeparam>
    [Serializable]
    [HideMember]
    public class ListArrayConverter<T>
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
            if(value is T[] array) { return Convert(array); }
            if(value is List<T> list) { return Convert(list); }
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

using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    internal class DefaultValueProvider
    {
        public static bool TryGetProviderForType(Type type, out IValueProvider provider)
        {
            if (type == typeof(char)) provider = new CharProvider();
            else if (type == typeof(byte)) provider = new ByteProvider();
            else if (type == typeof(short)) provider = new Int16Provider();
            else if (type == typeof(ushort)) provider = new UInt16Provider();
            else if (type == typeof(int)) provider = new Int32Provider();
            else if (type == typeof(uint)) provider = new UInt32Provider();
            else if (type == typeof(long)) provider = new Int64Provider();
            else if (type == typeof(ulong)) provider = new UInt64Provider();
            else if (type == typeof(float)) provider = new FloatProvider();
            else if (type == typeof(double)) provider = new DoubleProvider();
            else if (type == typeof(string)) provider = new StringProvider();
            else if (type == typeof(Vector2)) provider = new Vector2Provider();
            else if (type == typeof(Vector3)) provider = new Vector3Provider();
            else if (type == typeof(Vector4)) provider = new Vector4Provider();
            else if (type == typeof(Vector2Int)) provider = new Vector2IntProvider();
            else if (type == typeof(Vector3Int)) provider = new Vector3IntProvider();
            else if (type == typeof(Rect)) provider = new RectProvider();
            else if (type == typeof(Bounds)) provider = new BoundsProvider();
            else if (type == typeof(Color)) provider = new ColorProvider();
            else if (type == typeof(Gradient)) provider = new GradientProvider();
            else if (type == typeof(AnimationCurve)) provider = new AnimationCurveProvider();

            else
            {
                provider = null;
                return false;
            }
            return true;
        }
    }

    [Serializable] internal class CharProvider : ReadOnlyBind<char> { }
    [Serializable] internal class ByteProvider : ReadOnlyBind<byte> { }
    [Serializable] internal class Int16Provider : ReadOnlyBind<short> { }
    [Serializable] internal class UInt16Provider : ReadOnlyBind<ushort> { }
    [Serializable] internal class Int32Provider : ReadOnlyBind<int> { }
    [Serializable] internal class UInt32Provider : ReadOnlyBind<uint> { }
    [Serializable] internal class Int64Provider : ReadOnlyBind<long> { }
    [Serializable] internal class UInt64Provider : ReadOnlyBind<ulong> { }
    [Serializable] internal class FloatProvider : ReadOnlyBind<float> { }
    [Serializable] internal class DoubleProvider : ReadOnlyBind<double> { }
    [Serializable] internal class StringProvider : ReadOnlyBind<string> { }
    [Serializable] internal class Vector2Provider : ReadOnlyBind<Vector2> { }
    [Serializable] internal class Vector3Provider : ReadOnlyBind<Vector3> { }
    [Serializable] internal class Vector4Provider : ReadOnlyBind<Vector4> { }
    [Serializable] internal class Vector2IntProvider : ReadOnlyBind<Vector2Int> { }
    [Serializable] internal class Vector3IntProvider : ReadOnlyBind<Vector3Int> { }
    [Serializable] internal class RectProvider : ReadOnlyBind<Rect> { }
    [Serializable] internal class BoundsProvider : ReadOnlyBind<Bounds> { }
    [Serializable] internal class ColorProvider : ReadOnlyBind<Color> { }
    [Serializable] internal class GradientProvider : ReadOnlyBind<Gradient> { }
    [Serializable] internal class AnimationCurveProvider : ReadOnlyBind<AnimationCurve> { }
}

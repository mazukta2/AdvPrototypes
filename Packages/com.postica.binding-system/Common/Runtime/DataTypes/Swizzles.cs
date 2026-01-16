using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Postica.Common
{
    [Serializable]
    public sealed class SwizzleColor
    {
        public enum SwizzleType
        {
            R,
            G,
            B,
            A,
            [InspectorName("1 - R")]
            OneMinusR,
            [InspectorName("1 - G")]
            OneMinusG,
            [InspectorName("1 - B")]
            OneMinusB,
            [InspectorName("1 - A")]
            OneMinusA,
            [InspectorName("0")]
            Zero,
            [InspectorName("1")]
            One
        }

        public SwizzleType swizzleR = SwizzleType.R;
        public SwizzleType swizzleG = SwizzleType.G;
        public SwizzleType swizzleB = SwizzleType.B;
        public SwizzleType swizzleA = SwizzleType.A;
        
        public string ToColoredString()
        {
            return $"{SwizzleTypeToString(swizzleR).RT().Color(Color.red)}, {SwizzleTypeToString(swizzleG).RT().Color(Color.green)}, {SwizzleTypeToString(swizzleB).RT().Color(Color.cyan)}, {SwizzleTypeToString(swizzleA)})";
        }
        
        private static string SwizzleTypeToString(SwizzleType type)
        {
            return type switch
            {
                SwizzleType.R => "R",
                SwizzleType.G => "G",
                SwizzleType.B => "B",
                SwizzleType.A => "A",
                SwizzleType.OneMinusR => "1-R",
                SwizzleType.OneMinusG => "1-G",
                SwizzleType.OneMinusB => "1-B",
                SwizzleType.OneMinusA => "1-A",
                SwizzleType.Zero => "0",
                SwizzleType.One => "1",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public Color Swizzle(Color c)
        {
            return new Color(
                GetComponent(c, swizzleR),
                GetComponent(c, swizzleG),
                GetComponent(c, swizzleB),
                GetComponent(c, swizzleA)
            );
        }
        
        public Color InverseSwizzle(Color c)
        {
            // Note: Inverse swizzle is not always possible, this is a best-effort implementation.
            return new Color(
                InverseGetComponent(c, swizzleR),
                InverseGetComponent(c, swizzleG),
                InverseGetComponent(c, swizzleB),
                InverseGetComponent(c, swizzleA)
            );
        }

        private float InverseGetComponent(Color c, SwizzleType type)
        {
            return type switch
            {
                SwizzleType.R => 1f - c.r,
                SwizzleType.G => 1f - c.g,
                SwizzleType.B => 1f - c.b,
                SwizzleType.A => 1f - c.a,
                SwizzleType.OneMinusR => c.r,
                SwizzleType.OneMinusG => c.g,
                SwizzleType.OneMinusB => c.b,
                SwizzleType.OneMinusA => c.a,
                SwizzleType.Zero => 1f,
                SwizzleType.One => 0f,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float GetComponent(Color c, SwizzleType type)
        {
            return type switch
            {
                SwizzleType.R => c.r,
                SwizzleType.G => c.g,
                SwizzleType.B => c.b,
                SwizzleType.A => c.a,
                SwizzleType.OneMinusR => 1f - c.r,
                SwizzleType.OneMinusG => 1f - c.g,
                SwizzleType.OneMinusB => 1f - c.b,
                SwizzleType.OneMinusA => 1f - c.a,
                SwizzleType.Zero => 0f,
                SwizzleType.One => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
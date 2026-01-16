using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// Provides multiple additional extension functions to numeric types, such as <see cref="float"/>, <see cref="double"/> and <see cref="decimal"/> classes.
    /// </summary>
    internal static class NumericExtensions
    {
        /// <summary>
        /// Returns true if the two floats are approximately equal within a specified tolerance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsApprox(this float a, float b, float tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) < tolerance;
        }
        
        /// <summary>
        /// Returns true if the two doubles are approximately equal within a specified tolerance.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsApprox(this double a, double b, float tolerance = 0.0001f)
        {
            return Math.Abs(a - b) < tolerance;
        }
    }

}

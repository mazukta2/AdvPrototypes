using System;
using System.Collections.Generic;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    public enum FunctionType
    {
        Linear,
        Sine,
        Quad,
        Cubic,
        Quart,
        Quint,
        Expo,
        Circ,
        Back,
        Bounce,
        Elastic,
        Custom
    }

    public enum EaseType
    {
        EaseIn,
        EaseOut,
        EaseInOut
    }

    public enum TweenDirection
    {
        Forward,
        Backward,
        PingPong,
        ReversePingPong,
    }

    public enum TweenTimeScale
    {
        Unscaled,
        Scaled
    }

    public enum TweenOriginType
    {
        PreciseOrigin,
        LastCompletedValue,
    }

    public enum TweenOnTargetReach
    {
        StayAtTarget,
        ResetToOrigin,
    }

    public enum TargetChangeBehavior
    {
        Ignore,
        [InspectorName("Restart Animation Time")]
        RestartAnimation,
        RestartAnimationFromOrigin,

        [InspectorName("Adapt Animation (Unsafe)")]
        AdaptAnimation,
    }

    public static class TweenSystem
    {
        private static readonly Dictionary<(FunctionType tweenFunction, EaseType easeType), Func<float, float>>
            _easeFunctions = new();

        private static readonly Dictionary<(FunctionType tweenFunction, EaseType easeType), Func<float, float>>
            _inverseEaseFunctions = new();

        internal static void RegisterKnownEaseModifiers()
        {
            ModifiersFactory.Register<FloatEaseModifier>();
            ModifiersFactory.Register<IntEaseModifier>();
            ModifiersFactory.Register<DoubleEaseModifier>();
            ModifiersFactory.Register<LongEaseModifier>();
            ModifiersFactory.Register<ByteEaseModifier>();
            ModifiersFactory.Register<ShortEaseModifier>();
            ModifiersFactory.Register<DecimalEaseModifier>();

            ModifiersFactory.Register<Vector2EaseModifier>();
            ModifiersFactory.Register<Vector3EaseModifier>();
            ModifiersFactory.Register<Vector4EaseModifier>();
            ModifiersFactory.Register<Vector2IntEaseModifier>();
            ModifiersFactory.Register<Vector3IntEaseModifier>();

            ModifiersFactory.Register<ColorEaseModifier>();
        }

        internal static void RegisterKnownDampedTrackerModifiers()
        {
            ModifiersFactory.Register<FloatDampedTrackerModifier>();
            ModifiersFactory.Register<IntDampedTrackerModifier>();
            ModifiersFactory.Register<DoubleDampedTrackerModifier>();
            ModifiersFactory.Register<LongDampedTrackerModifier>();
            ModifiersFactory.Register<ByteDampedTrackerModifier>();
            ModifiersFactory.Register<ShortDampedTrackerModifier>();
            ModifiersFactory.Register<DecimalDampedTrackerModifier>();

            ModifiersFactory.Register<Vector2DampedTrackerModifier>();
            ModifiersFactory.Register<Vector3DampedTrackerModifier>();
            ModifiersFactory.Register<Vector4DampedTrackerModifier>();
            ModifiersFactory.Register<Vector2IntDampedTrackerModifier>();
            ModifiersFactory.Register<Vector3IntDampedTrackerModifier>();

            ModifiersFactory.Register<ColorDampedTrackerModifier>();
            
            ModifiersFactory.Register<QuaternionDampedTrackerModifier>();
        }

        public static Func<float, float> GetEaseFunction(FunctionType tweenFunction, EaseType easeType)
        {
            if (_easeFunctions.TryGetValue((tweenFunction, easeType), out var easeFunction))
            {
                return easeFunction;
            }

            Func<float, float> function = (tweenFunction, easeType) switch
            {
                (FunctionType.Linear, _) => x => x,
                (FunctionType.Sine, EaseType.EaseIn) => x => 1 - Mathf.Cos(x * Mathf.PI / 2),
                (FunctionType.Sine, EaseType.EaseOut) => x => Mathf.Sin(x * Mathf.PI / 2),
                (FunctionType.Sine, EaseType.EaseInOut) => x => -0.5f * (Mathf.Cos(Mathf.PI * x) - 1),
                (FunctionType.Quad, EaseType.EaseIn) => x => x * x,
                (FunctionType.Quad, EaseType.EaseOut) => x => 1 - (1 - x) * (1 - x),
                (FunctionType.Quad, EaseType.EaseInOut) => x =>
                    x < 0.5f ? 2 * x * x : x < 0.5 ? 2 * x * x : 1 - (-2 * x + 2) * (-2 * x + 2) / 2,
                (FunctionType.Cubic, EaseType.EaseIn) => x => x * x * x,
                (FunctionType.Cubic, EaseType.EaseOut) => x => 1 - (1 - x) * (1 - x) * (1 - x),
                (FunctionType.Cubic, EaseType.EaseInOut) => x =>
                    x < 0.5f ? 4 * x * x * x : 1 - (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) / 2,
                (FunctionType.Quart, EaseType.EaseIn) => x => x * x * x * x,
                (FunctionType.Quart, EaseType.EaseOut) => x => 1 - (1 - x) * (1 - x) * (1 - x) * (1 - x),
                (FunctionType.Quart, EaseType.EaseInOut) => x =>
                    x < 0.5f ? 8 * x * x * x * x : 1 - (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) / 2,
                (FunctionType.Quint, EaseType.EaseIn) => x => x * x * x * x * x,
                (FunctionType.Quint, EaseType.EaseOut) => x => 1 - (1 - x) * (1 - x) * (1 - x) * (1 - x) * (1 - x),
                (FunctionType.Quint, EaseType.EaseInOut) => x =>
                    x < 0.5f
                        ? 16 * x * x * x * x * x
                        : 1 - (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) * (-2 * x + 2) / 2,
                (FunctionType.Expo, EaseType.EaseIn) => x => x == 0 ? 0 : Mathf.Pow(2, 10 * (x - 1)),
                (FunctionType.Expo, EaseType.EaseOut) => x => Mathf.Approximately(x, 1) ? 1 : 1 - Mathf.Pow(2, -10 * x),
                (FunctionType.Expo, EaseType.EaseInOut) => x =>
                    x == 0 ? 0 :
                    Mathf.Approximately(x, 1) ? 1 :
                    x < 0.5f ? Mathf.Pow(2, 10 * (2 * x - 1)) / 2 : (2 - Mathf.Pow(2, -10 * (2 * x - 1))) / 2,
                (FunctionType.Circ, EaseType.EaseIn) => x => 1 - Mathf.Sqrt(1 - x * x),
                (FunctionType.Circ, EaseType.EaseOut) => x => Mathf.Sqrt(1 - (x - 1) * (x - 1)),
                (FunctionType.Circ, EaseType.EaseInOut) => x =>
                    x < 0.5f
                        ? (1 - Mathf.Sqrt(1 - 2 * x * (2 * x))) / 2
                        : (Mathf.Sqrt(1 - Mathf.Pow(-2 * x + 2, 2)) + 1) / 2,
                (FunctionType.Back, EaseType.EaseIn) => x => 2.70158f * x * x * x - 1.70158f * x * x,
                (FunctionType.Back, EaseType.EaseOut) => x =>
                    1 + 2.70158f * Mathf.Pow(x - 1, 3) + 1.70158f * Mathf.Pow(x - 1, 2),
                (FunctionType.Back, EaseType.EaseInOut) => x =>
                    x < 0.5f
                        ? (Mathf.Pow(2 * x, 2) * ((2.70158f + 1) * 2 * x - 2.70158f)) / 2
                        : (Mathf.Pow(2 * x - 2, 2) * ((2.70158f + 1) * (x * 2 - 2) + 2.70158f) + 2) / 2,
                (FunctionType.Bounce, EaseType.EaseIn) => x => 1 - GetBounceEase(1 - x),
                (FunctionType.Bounce, EaseType.EaseOut) => GetBounceEase,
                (FunctionType.Bounce, EaseType.EaseInOut) => x =>
                    x < 0.5f ? (1 - GetBounceEase(1 - 2 * x)) / 2 : (GetBounceEase(2 * x - 1) + 1) / 2,
                (FunctionType.Elastic, EaseType.EaseIn) => x =>
                    x == 0 ? 0 :
                    x == 1 ? 1 : -Mathf.Pow(2, 10 * x - 10) * Mathf.Sin((x * 10 - 10.75f) * (2 * Mathf.PI) / 3),
                (FunctionType.Elastic, EaseType.EaseOut) => x =>
                    x == 0 ? 0 :
                    x == 1 ? 1 : Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * (2 * Mathf.PI) / 3) + 1,
                (FunctionType.Elastic, EaseType.EaseInOut) => x => x == 0
                    ? 0
                    : x == 1
                        ? 1
                        : x < 0.5
                            ? -(Mathf.Pow(2, 20 * x - 10) * Mathf.Sin((20 * x - 11.125f) * (2 * Mathf.PI) / 4.5f)) / 2
                            : (Mathf.Pow(2, -20 * x + 10) * Mathf.Sin((20 * x - 11.125f) * (2 * Mathf.PI) / 4.5f)) / 2 +
                              1,
                _ => null
            };

            _easeFunctions[(tweenFunction, easeType)] = function;
            return function;
        }

        public static Func<float, float> GetIterativeInverse(Func<float, float> function, int iterations = 20)
        {
            return y =>
            {
                if (y <= 0f) return 0f;
                if (y >= 1f) return 1f;

                float lo = 0f, hi = 1f, mid = 0f;
                for (int i = 0; i < iterations; ++i)
                {
                    mid = (lo + hi) * 0.5f;
                    if (function(mid) < y) lo = mid;
                    else hi = mid;
                }

                return mid;
            };
        }

        public static Func<float, float> GetInverseEaseFunction(FunctionType tweenFunction, EaseType easeType)
        {
            if (_inverseEaseFunctions.TryGetValue((tweenFunction, easeType), out var inverseFunction))
            {
                return inverseFunction;
            }

            inverseFunction = ComputeInverseEaseFunction(tweenFunction, easeType);
            _inverseEaseFunctions[(tweenFunction, easeType)] = inverseFunction;
            return inverseFunction;
        }

        private static Func<float, float> ComputeInverseEaseFunction(FunctionType fn, EaseType et)
        {
            /* local helpers ------------------------------------------------- */

            const float LN2 = 0.693147180559945309417f; // natural log(2)

            float Log2(float v) => Mathf.Log(v) / LN2; // Mathf.Log is ln
            float Cbrt(float v) => Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 1f / 3f);

            /* --------------------------------------------------------------- */
            /*  analytic inverses                                              */
            /* --------------------------------------------------------------- */
            switch (fn, et)
            {
                /* ----- Linear ---------------------------------------------- */
                case (FunctionType.Linear, _):
                    return y => y;

                /* ----- Sine ------------------------------------------------- */
                case (FunctionType.Sine, EaseType.EaseIn):
                    return y => (2f / Mathf.PI) * Mathf.Acos(1f - y);

                case (FunctionType.Sine, EaseType.EaseOut):
                    return y => (2f / Mathf.PI) * Mathf.Asin(y);

                case (FunctionType.Sine, EaseType.EaseInOut):
                    return y => Mathf.Acos(1f - 2f * y) / Mathf.PI;

                /* ----- Quad ------------------------------------------------- */
                case (FunctionType.Quad, EaseType.EaseIn):
                    return y => Mathf.Sqrt(y);

                case (FunctionType.Quad, EaseType.EaseOut):
                    return y => 1f - Mathf.Sqrt(1f - y);

                case (FunctionType.Quad, EaseType.EaseInOut):
                    return y => y < 0.5f
                        ? Mathf.Sqrt(0.5f * y)
                        : 1f - Mathf.Sqrt(0.5f * (1f - y));

                /* ----- Cubic ------------------------------------------------ */
                case (FunctionType.Cubic, EaseType.EaseIn):
                    return y => Cbrt(y);

                case (FunctionType.Cubic, EaseType.EaseOut):
                    return y => 1f - Cbrt(1f - y);

                case (FunctionType.Cubic, EaseType.EaseInOut):
                    return y => y < 0.5f
                        ? Cbrt(0.25f * y)
                        : 1f - Cbrt(0.5f * (1f - y));

                /* ----- Quart ----------------------------------------------- */
                case (FunctionType.Quart, EaseType.EaseIn):
                    return y => Mathf.Pow(y, 0.25f);

                case (FunctionType.Quart, EaseType.EaseOut):
                    return y => 1f - Mathf.Pow(1f - y, 0.25f);

                case (FunctionType.Quart, EaseType.EaseInOut):
                    return y => y < 0.5f
                        ? Mathf.Pow(0.125f * y, 0.25f)
                        : 1f - Mathf.Pow(0.5f * (1f - y), 0.25f);

                /* ----- Quint ----------------------------------------------- */
                case (FunctionType.Quint, EaseType.EaseIn):
                    return y => Mathf.Pow(y, 0.2f);

                case (FunctionType.Quint, EaseType.EaseOut):
                    return y => 1f - Mathf.Pow(1f - y, 0.2f);

                case (FunctionType.Quint, EaseType.EaseInOut):
                    return y => y < 0.5f
                        ? Mathf.Pow(0.0625f * y, 0.2f)
                        : 1f - Mathf.Pow(0.5f * (1f - y), 0.2f);

                /* ----- Exponential ----------------------------------------- */
                case (FunctionType.Expo, EaseType.EaseIn):
                    return y => Mathf.Approximately(y, 0f)
                        ? 0f
                        : 1f + Log2(y) / 10f;

                case (FunctionType.Expo, EaseType.EaseOut):
                    return y => Mathf.Approximately(y, 1f)
                        ? 1f
                        : -Log2(1f - y) / 10f;

                case (FunctionType.Expo, EaseType.EaseInOut):
                    return y =>
                    {
                        if (Mathf.Approximately(y, 0f)) return 0f;
                        if (Mathf.Approximately(y, 1f)) return 1f;

                        if (y < 0.5f) // first half
                        {
                            float v = y * 2f; // v = 2y
                            return (Log2(v) / 10f + 1f) * 0.5f;
                        }
                        else // second half
                        {
                            float w = 2f * (1f - y); // w = 2(1-y)
                            return (1f - Log2(w) / 10f) * 0.5f;
                        }
                    };

                /* ----- Circular -------------------------------------------- */
                case (FunctionType.Circ, EaseType.EaseIn):
                    return y => Mathf.Sqrt(1f - Mathf.Pow(1f - y, 2f));

                case (FunctionType.Circ, EaseType.EaseOut):
                    return y => 1f - Mathf.Sqrt(1f - y * y);

                case (FunctionType.Circ, EaseType.EaseInOut):
                    return y => y < 0.5f
                        ? 0.5f * Mathf.Sqrt(1f - Mathf.Pow(1f - 2f * y, 2f))
                        : 1f - 0.5f * Mathf.Sqrt(1f - Mathf.Pow(2f * y - 1f, 2f));

                /* ----- everything else (Back / Bounce / Elastic) ----------- */
                /*     analytic inverses are messy / non-unique, so we do      */
                /*     a quick monotone root search.                           */
                case (FunctionType.Back, _):
                case (FunctionType.Bounce, _):
                case (FunctionType.Elastic, _):
                {
                    // we need the *forward* easing function first:
                    Func<float, float> fwd = GetEaseFunction(fn, et); // <-- see below
                    return GetIterativeInverse(fwd);
                }

                /* ----- unknown --------------------------------------------- */
                default:
                    return null;
            }
        }

        private static float GetBounceEase(float value)
        {
            switch (value)
            {
                case < 1 / 2.75f:
                    return 7.5625f * value * value;
                case < 2 / 2.75f:
                    value -= 1.5f / 2.75f;
                    return 7.5625f * value * value + .75f;
                default:
                {
                    if (value < 2.5 / 2.75)
                    {
                        value -= 2.25f / 2.75f;
                        return 7.5625f * value * value + .9375f;
                    }

                    value -= 2.625f / 2.75f;
                    return 7.5625f * value * value + .984375f;
                }
            }
        }

        public static class DampedHarmonic
        {
            const float EPS = 1e-4f; // tolerance for “ζ == 1”

            public static float MotionToTarget
            (
                float t,
                float y0, float v0,
                float yTarget,
                float omega, float zeta
            )
            {
                return Motion(t, y0 - yTarget, v0, omega, zeta) + yTarget;
            }
            
            //------------------------------------------------------------------
            // PUBLIC WRAPPER --------------------------------------------------
            //------------------------------------------------------------------
            // Returns y(t).  Overload with "out v" also returns y'(t).
            public static float Motion
            (
                float t,
                float y0, float v0,
                float omega, float zeta
            )
            {
                float y, v;
                Motion(t, y0, v0, omega, zeta, out y, out v);
                return y;
            }

            public static void Motion
            (
                float t,
                float y0, float v0,
                float omega, float zeta,
                out float y, out float v
            )
            {
                if (zeta < 1f - EPS)
                    Underdamped(t, y0, v0, omega, zeta, out y, out v);
                else if (zeta > 1f + EPS)
                    Overdamped(t, y0, v0, omega, zeta, out y, out v);
                else
                    Critical(t, y0, v0, omega, out y, out v);
            }

            //------------------------------------------------------------------
            // ζ < 1  :  e^{-ζωt}[A cos(ω_d t)+B sin(ω_d t)]
            //------------------------------------------------------------------
            static void Underdamped
            (
                float t,
                float y0, float v0,
                float omega, float zeta,
                out float y, out float v
            )
            {
                float wd = omega * Mathf.Sqrt(1f - zeta * zeta);
                float A = y0;
                float B = (v0 + zeta * omega * y0) / wd;

                float exp = Mathf.Exp(-zeta * omega * t);
                float cos = Mathf.Cos(wd * t);
                float sin = Mathf.Sin(wd * t);

                y = exp * (A * cos + B * sin);

                // derivative
                v = exp * (-zeta * omega * (A * cos + B * sin) + (-A * wd * sin + B * wd * cos));
            }

            //------------------------------------------------------------------
            // ζ == 1 :  (A + B t) e^{-ωt}
            //------------------------------------------------------------------
            static void Critical
            (
                float t,
                float y0, float v0,
                float omega,
                out float y, out float v
            )
            {
                float A = y0;
                float B = v0 + omega * y0;

                float exp = Mathf.Exp(-omega * t);
                y = (A + B * t) * exp;
                v = (B - omega * (A + B * t)) * exp;
            }

            //------------------------------------------------------------------
            // ζ > 1  :  A e^{r1 t} + B e^{r2 t}
            //------------------------------------------------------------------
            static void Overdamped /**/
            (
                float t,
                float y0, float v0,
                float omega, float zeta,
                out float y, out float v
            )
            {
                float s = Mathf.Sqrt(zeta * zeta - 1f);
                float r1 = -omega * (zeta - s);
                float r2 = -omega * (zeta + s);

                float A = (v0 - r2 * y0) / (r1 - r2);
                float B = y0 - A;

                float e1 = Mathf.Exp(r1 * t);
                float e2 = Mathf.Exp(r2 * t);

                y = A * e1 + B * e2;
                v = A * r1 * e1 + B * r2 * e2;
            }
        }

        /// <summary>
        /// Settling-time estimate for   y'' + 2ζω y' + ω² y = 0 .
        /// </summary>
        /// <param name="zeta">Damping ratio ζ  (can be ≥ 0).</param>
        /// <param name="omega">Natural frequency ω  [rad/s] (must be > 0).</param>
        /// <param name="initialValue">Initial displacement  y(0).</param>
        /// <param name="initialVelocity">Initial velocity      y'(0).</param>
        /// <param name="tolerance">Desired absolute tolerance |y| &lt; tol (tol > 0).</param>
        /// <returns>Settling time t_set [s], or +∞ if the system is unstable.</returns>
        public static float GetApproxTrackingTime(
            float omega,
            float zeta,
            float initialValue = 1.0f,
            float initialVelocity = 0.0f,
            float tolerance = 1e-3f)
        {
            if (omega <= 0.0f) throw new ArgumentOutOfRangeException(nameof(omega), "ω must be positive.");
            if (tolerance <= 0.0f) throw new ArgumentOutOfRangeException(nameof(tolerance), "tolerance must be positive.");

            // Unstable if negative damping.
            if (zeta < 0.0f) return float.PositiveInfinity;

            const float EPS = 1e-12f; // small number to detect critical damping
            float A, lambda; // envelope amplitude and real pole

            if (zeta < 1.0f - EPS) // -------- under-damped (ζ < 1) ----------
            {
                float wd = omega * Mathf.Sqrt(1.0f - zeta * zeta); // damped ω
                float c = (initialVelocity + zeta * omega * initialValue) / wd;
                A = Mathf.Sqrt(initialValue * initialValue + c * c); // |C| in envelope
                lambda = zeta * omega; // decay rate
            }
            else if (Mathf.Abs(zeta - 1.0f) <= EPS) // --- critically damped (ζ ≈ 1) ---
            {
                A = Mathf.Sqrt(initialValue * initialValue + (initialVelocity / omega) * (initialVelocity / omega));
                lambda = omega;
            }
            else // ---- over-damped (ζ > 1) --------
            {
                float sq = Mathf.Sqrt(zeta * zeta - 1.0f);
                float lam1 = -omega * (zeta - sq); // slow pole (closest to 0)
                float lam2 = -omega * (zeta + sq); // fast pole

                float c2 = (initialVelocity - lam1 * initialValue) / (lam2 - lam1);
                float c1 = initialValue - c2;

                A = Mathf.Abs(c1) + Mathf.Abs(c2); // simple envelope bound
                lambda = -lam1; // positive decay rate
            }

            return (A <= tolerance)
                ? 0f
                : Mathf.Log(A / tolerance) / lambda;
        }
    }
}
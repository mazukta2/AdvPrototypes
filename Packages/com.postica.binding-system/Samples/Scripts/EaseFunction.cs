using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class EaseFunction : MonoBehaviour
    {
        const int plotResolution = 20;

        public enum EaseType
        {
            // Shapes
            Linear = 1,
            Quadratic = 2,
            Cubic = 3,
            Quartic = 4,
            Quintic = 5,
        }

        public ReadOnlyBind<float> input;
        [Bind(BindMode.Write)]
        public Bind<float> output;

        [Space]
        public bool analyticVersion = true;
        public EasePart easeIn = new EasePart() { type = EaseType.Linear };
        public EasePart easeOut = new EasePart() { type = EaseType.Linear };

        [Space]
        public bool useMixin = true;
        [Bind]
        [Range(0f, 1f)]
        public ReadOnlyBind<float> mixin = (ReadOnlyBind<float>)0.5f;

        [Space]
        public AnimationCurve curve;

        Func<float, float> functionIn;
        Func<float, float> functionOut;

        private void OnValidate()
        {
            BuildFunction();
        }

        // Start is called before the first frame update
        void Start()
        {
            // Build the function...
            BuildFunction();
        }

        private void BuildFunction()
        {
            if (!analyticVersion)
            {
                if(curve.keys?.Length >= plotResolution)
                {
                    curve.keys = new Keyframe[0];
                    curve.AddKey(0, 0);
                    curve.AddKey(1, 1);
                }
                return;
            }

            // Function in
            Func<float, float> fin = null;
            Func<float, float> fin2 = null;
            switch (easeIn.type)
            {
                case EaseType.Linear: fin = Functions.Linear; break;
                case EaseType.Quadratic: fin = Functions.Quadratic; break;
                case EaseType.Cubic: fin = Functions.Cubic; break;
                case EaseType.Quartic: fin = Functions.Quartic; break;
                case EaseType.Quintic: fin = Functions.Quintic; break;
                default: fin = x => 1; break;
            }

            if (easeIn.invert)
            {
                fin2 = x => 1 - fin(x);
            }
            else
            {
                fin2 = fin;
            }

            if (easeIn.absolute)
            {
                var a = easeIn.offset;
                functionIn = x => Mathf.Abs(a + fin2(x));
            }
            else
            {
                var a = easeIn.offset;
                functionIn = x => a + fin2(x);
            }

            Func<float, float> fout = null;
            Func<float, float> fout2 = null;
            switch (easeOut.type)
            {
                case EaseType.Linear: fout = Functions.Linear; break;
                case EaseType.Quadratic: fout = Functions.Quadratic; break;
                case EaseType.Cubic: fout = Functions.Cubic; break;
                case EaseType.Quartic: fout = Functions.Quartic; break;
                case EaseType.Quintic: fout = Functions.Quintic; break;
                default: fout = x => 1; break;
            }

            if (easeIn.invert)
            {
                fout2 = x => 1 - fout(x);
            }
            else
            {
                fout2 = fout;
            }

            if (easeIn.absolute)
            {
                var a = easeIn.offset;
                functionOut = x => Functions.Flip(Mathf.Abs(a + fout2(Functions.Flip(x))));
            }
            else
            {
                var a = easeIn.offset;
                functionOut = x => Functions.Flip(a + fout2(Functions.Flip(x)));
            }

            curve.keys = new Keyframe[0];

            float prevY = 0;
            float y = Evaluate(0);
            const float dx = 1 / (float)plotResolution;
            for (int i = 0; i < plotResolution; i++)
            {
                var x = i / (float)plotResolution;
                var nextX = (i + 1) / (float)plotResolution;
                var nextY = Evaluate(nextX);

                curve.AddKey(new Keyframe(x, y, (y - prevY) / dx, (nextY - y) / dx));
                prevY = y;
                y = nextY;
            }
        }

        void Update()
        {
            output.Value = Evaluate(input.Value);
        }

        private float Evaluate(float x)
        {
            if(functionIn == null || functionOut == null)
            {
                return 0;
            }
            
            float result = 0;
            if (useMixin)
            {
                var mix = Mathf.Clamp01(mixin.Value);
                result = (1 - mix) * functionIn(x) + mix * functionOut(x);
            }
            else
            {
                result = (1 - x) * functionIn(x) + x * functionOut(x);
            }
            return result;
        }

        [Serializable]
        public struct EasePart
        {
            public EaseType type;
            public bool invert;
            public bool absolute;
            public float offset;
        }

        [Serializable]
        public struct Range
        {
            public ReadOnlyBind<float> min;
            public ReadOnlyBind<float> max;

            public Range(float min, float max)
            {
                this.min = (ReadOnlyBind<float>)min;
                this.max = (ReadOnlyBind<float>)max;
            }

            public float length => (max - min);
            public float Normalize(float x) => Mathf.Clamp01((x - min) / (max - min));
            public float Denormalize(float x) => x * (max - min) + min;
        }

        private static class Functions
        {
            public static float Absolute(float x) => x < 0 ? -x : x;
            public static float Flip(float x) => 1 - x;
            public static float Linear(float x) => x;
            public static float Quadratic(float x) => x * x;
            public static float Cubic(float x) => x * x * x;
            public static float Quartic(float x) => x * x * x * x;
            public static float Quintic(float x) => x * x * x * x * x;
        }
    }
}

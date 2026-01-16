using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class FloatPIDModifier : PIDModifier<float>
    {
        public FloatPIDModifier()
        {
            P_I_D = new Data()
            {
                useLimits = true.Bind(),
                min = (-1f).Bind(),
                max = 1f.Bind(),
            };
        }
        protected override float Add(float a, float b) => a + b;
        protected override float Subtract(float a, float b) => a - b;
        protected override float Multiply(float a, float b) => a * b;
        protected override float Divide(float a, float b) => a / b;
        protected override float Clamp(float value, float min, float max) => Mathf.Clamp(value, min, max);
        protected override float Normalize(float value, float min, float max) => Mathf.InverseLerp(min, max, value);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class DoublePIDModifier : PIDModifier<double>
    {
        public DoublePIDModifier()
        {
            P_I_D = new Data()
            {
                useLimits = true.Bind(),
                min = (-1d).Bind(),
                max = 1d.Bind(),
            };
        }
        protected override double Add(double a, double b) => a + b;
        protected override double Subtract(double a, double b) => a - b;
        protected override double Multiply(double a, float b) => a * b;
        protected override double Divide(double a, float b) => a / b;
        protected override double Clamp(double value, double min, double max) => Mathf.Clamp((float)value, (float)min, (float)max);
        protected override double Normalize(double value, double min, double max) => Mathf.InverseLerp((float)min, (float)max, (float)value);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class IntPIDModifier : PIDModifier<int>
    {
        protected override int Add(int a, int b) => a + b;
        protected override int Subtract(int a, int b) => a - b;
        protected override int Multiply(int a, float b) => Mathf.RoundToInt(a * b);
        protected override int Divide(int a, float b) => Mathf.RoundToInt(a / b);
        protected override int Clamp(int value, int min, int max) => Mathf.Clamp(value, min, max);
        protected override int Normalize(int value, int min, int max) => Mathf.RoundToInt(Mathf.InverseLerp(min, max, value));
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class LongPIDModifier : PIDModifier<long>
    {
        protected override long Add(long a, long b) => a + b;
        protected override long Subtract(long a, long b) => a - b;
        protected override long Multiply(long a, float b) => (long)(a * b);
        protected override long Divide(long a, float b) => (long)(a / b);
        protected override long Clamp(long value, long min, long max) => Mathf.Clamp((int)value, (int)min, (int)max);
        protected override long Normalize(long value, long min, long max) => Mathf.RoundToInt(Mathf.InverseLerp(min, max, (int)value));
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class ShortPIDModifier : PIDModifier<short>
    {
        protected override short Add(short a, short b) => (short)(a + b);
        protected override short Subtract(short a, short b) => (short)(a - b);
        protected override short Multiply(short a, float b) => (short)(a * b);
        protected override short Divide(short a, float b) => (short)(a / b);
        protected override short Clamp(short value, short min, short max) => (short)Mathf.Clamp(value, min, max);
        protected override short Normalize(short value, short min, short max) => (short)Mathf.RoundToInt(Mathf.InverseLerp(min, max, value));
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class DecimalPIDModifier : PIDModifier<decimal>
    {
        public DecimalPIDModifier()
        {
            P_I_D = new Data()
            {
                useLimits = true.Bind(),
                min = ((decimal)-1f).Bind(),
                max = ((decimal)1f).Bind(),
            };
        }
        protected override decimal Add(decimal a, decimal b) => a + b;
        protected override decimal Subtract(decimal a, decimal b) => a - b;
        protected override decimal Multiply(decimal a, float b) => a * (decimal)b;
        protected override decimal Divide(decimal a, float b) => a / (decimal)b;
        protected override decimal Clamp(decimal value, decimal min, decimal max) => (decimal)Mathf.Clamp((float)value, (float)min, (float)max);
        protected override decimal Normalize(decimal value, decimal min, decimal max) => (decimal)Mathf.InverseLerp((float)min, (float)max, (float)value);
    }
}
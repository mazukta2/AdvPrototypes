using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class FloatDampedTrackerModifier : DampedTrackerModifier<float>
    {
        protected override float Add(float a, float b) => a + b;
        protected override float Subtract(float a, float b) => a - b;
        protected override float Multiply(float a, float b) => a * b;
        protected override bool Equals(float a, float b, float epsilon) => a.IsApprox(b, epsilon);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class DoubleDampedTrackerModifier : DampedTrackerModifier<double>
    {
        protected override double Add(double a, double b) => a + b;
        protected override double Subtract(double a, double b) => a - b;
        protected override double Multiply(double a, float b) => a * b;
        protected override bool Equals(double a, double b, float epsilon) => a.IsApprox(b, epsilon);
    }
    
    // Add other numeric types as needed
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class IntDampedTrackerModifier : DampedTrackerModifier<int>
    {
        protected override int Add(int a, int b) => a + b;
        protected override int Subtract(int a, int b) => a - b;
        protected override int Multiply(int a, float b) => (int)(a * b);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class LongDampedTrackerModifier : DampedTrackerModifier<long>
    {
        protected override long Add(long a, long b) => a + b;
        protected override long Subtract(long a, long b) => a - b;
        protected override long Multiply(long a, float b) => (long)(a * b);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class ByteDampedTrackerModifier : DampedTrackerModifier<byte>
    {
        protected override byte Add(byte a, byte b) => (byte)(a + b);
        protected override byte Subtract(byte a, byte b) => (byte)(a - b);
        protected override byte Multiply(byte a, float b) => (byte)(a * b);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class ShortDampedTrackerModifier : DampedTrackerModifier<short>
    {
        protected override short Add(short a, short b) => (short)(a + b);
        protected override short Subtract(short a, short b) => (short)(a - b);
        protected override short Multiply(short a, float b) => (short)(a * b);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class UIntDampedTrackerModifier : DampedTrackerModifier<uint>
    {
        protected override uint Add(uint a, uint b) => a + b;
        protected override uint Subtract(uint a, uint b) => a - b;
        protected override uint Multiply(uint a, float b) => (uint)(a * b);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class ULongDampedTrackerModifier : DampedTrackerModifier<ulong>
    {
        protected override ulong Add(ulong a, ulong b) => a + b;
        protected override ulong Subtract(ulong a, ulong b) => a - b;
        protected override ulong Multiply(ulong a, float b) => (ulong)(a * b);
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public sealed class DecimalDampedTrackerModifier : DampedTrackerModifier<decimal>
    {
        protected override decimal Add(decimal a, decimal b) => a + b;
        protected override decimal Subtract(decimal a, decimal b) => a - b;
        protected override decimal Multiply(decimal a, float b) => (decimal)((float)a * b);
    }
}
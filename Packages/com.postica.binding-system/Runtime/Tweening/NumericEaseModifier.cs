using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class FloatEaseModifier : EaseModifier<float>
    {
        protected override float Lerp(float from, float to, float progress)
        {
            return Mathf.Lerp(from, to, progress);
        }
        
        protected override float GetLerpPoint(float actual, float from, float to)
        {
            return to != from ? (actual - from) / (to - from) : 1;
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class DoubleEaseModifier : EaseModifier<double>
    {
        protected override double Lerp(double from, double to, float progress)
        {
            return Mathf.Lerp((float)from, (float)to, progress);
        }
        
        protected override float GetLerpPoint(double actual, double from, double to)
        {
            return to != from ? (float)(actual - from) / (float)(to - from) : 1;
        }
    }
    
    // Add other numeric types as needed
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class IntEaseModifier : EaseModifier<int>
    {
        protected override int Lerp(int from, int to, float progress)
        {
            return Mathf.RoundToInt(Mathf.Lerp(from, to, progress));
        }
        
        protected override float GetLerpPoint(int actual, int from, int to)
        {
            return to != from ? (actual - from) / (to - from) : 1;
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class LongEaseModifier : EaseModifier<long>
    {
        protected override long Lerp(long from, long to, float progress)
        {
            return (long)Mathf.Lerp(from, to, progress);
        }
        
        protected override float GetLerpPoint(long actual, long from, long to)
        {
            return to != from ? (float)(actual - from) / (float)(to - from) : 1;
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class ByteEaseModifier : EaseModifier<byte>
    {
        protected override byte Lerp(byte from, byte to, float progress)
        {
            return (byte)Mathf.Lerp(from, to, progress);
        }
        
        protected override float GetLerpPoint(byte actual, byte from, byte to)
        {
            return to != from ? (actual - from) / (to - from) : 1;
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class ShortEaseModifier : EaseModifier<short>
    {
        protected override short Lerp(short from, short to, float progress)
        {
            return (short)Mathf.Lerp(from, to, progress);
        }
        
        protected override float GetLerpPoint(short actual, short from, short to)
        {
            return to != from ? (actual - from) / (to - from) : 1;
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class UIntEaseModifier : EaseModifier<uint>
    {
        protected override uint Lerp(uint from, uint to, float progress)
        {
            return (uint)Mathf.Lerp(from, to, progress);
        }
        
        protected override float GetLerpPoint(uint actual, uint from, uint to)
        {
            return to != from ? (actual - from) / (to - from) : 1;
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class ULongEaseModifier : EaseModifier<ulong>
    {
        protected override ulong Lerp(ulong from, ulong to, float progress)
        {
            return (ulong)Mathf.Lerp(from, to, progress);
        }
        
        protected override float GetLerpPoint(ulong actual, ulong from, ulong to)
        {
            return to != from ? (float)(actual - from) / (float)(to - from) : 1;
        }
    }
    
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class DecimalEaseModifier : EaseModifier<decimal>
    {
        protected override decimal Lerp(decimal from, decimal to, float progress)
        {
            return (decimal)Mathf.Lerp((float)from, (float)to, progress);
        }
        
        protected override float GetLerpPoint(decimal actual, decimal from, decimal to)
        {
            return to != from ? (float)(actual - from) / (float)(to - from) : 1;
        }
    }
}
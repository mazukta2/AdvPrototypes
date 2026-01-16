using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// Utility to calculate a moving average over a series of values.
    /// </summary>
    public class MovingAverage : IFormattable
    {
        private readonly double[] _values;
        
        private int _index;
        private double _average;
        
        public int SampleCount => _values.Length;
        public double Value => _average;
        
        public void Reset()
        {
            _index = 0;
            _average = 0;
            Array.Fill(_values, 0);
        }
        
        public MovingAverage(int sampleCount)
        {
            _index = 0;
            _values = new double[sampleCount];
            _average = 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AddSample(double value)
        {
            _values[_index] = value;
            _index = (_index + 1) % _values.Length;

            _average += (value - _values[_index]) / _values.Length;
            return _average;
        }
        
        public static implicit operator double(MovingAverage avg)
        {
            return avg._average;
        }

        public static double operator +(MovingAverage avg, double value)
        {
            return avg.AddSample(value);
        }
        
        public static float operator +(MovingAverage avg, float value)
        {
            return (float)avg.AddSample(value);
        }
        
        public override string ToString()
        {
            return _average.ToString();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return _average.ToString(format, formatProvider);
        }
    }
}

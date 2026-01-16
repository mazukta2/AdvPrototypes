using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Converts a number into a boolean value, if the number is greater or equal to the threshold, it returns true.
    /// </summary>
    [HideMember]
    [Serializable]
    [Obsolete("Use the same name converter from Postica.BindingSystem.Runtime.dll")]
    internal class NumericToBoolConverter : 
        IConverter<float, bool>,
        IConverter<int, bool>,
        IConverter<double, bool>,
        IConverter<long, bool>,
        IConverter<short, bool>,
        IConverter<byte, bool>,
        IConverter<sbyte, bool>,
        IConverter<uint, bool>,
        IConverter<ulong, bool>,
        IConverter<ushort, bool>,
        IConverter<IValueProvider<float>, bool>,
        IConverter<IValueProvider<int>, bool>,
        IConverter<IValueProvider<double>, bool>,
        IConverter<IValueProvider<long>, bool>,
        IConverter<IValueProvider<short>, bool>,
        IConverter<IValueProvider<byte>, bool>,
        IConverter<IValueProvider<sbyte>, bool>,
        IConverter<IValueProvider<uint>, bool>,
        IConverter<IValueProvider<ulong>, bool>,
        IConverter<IValueProvider<ushort>, bool>
    {
        [Tooltip("The threshold value to convert the float to a bool, the value from zero to this threshold is converted to false, the rest to true")]
        public Bind<float> threshold = 0.5f.Bind();
        
        public string Id => "Numeric to Bool Converter";

        public string Description => "Converts a numeric value to a bool value, if the value is greater or equal to the threshold, it returns true";

        public bool IsSafe => true;

        public bool Convert(float value) => value >= threshold;
        public bool Convert(int value) => value >= threshold;
        public bool Convert(double value) => value >= threshold;
        public bool Convert(long value) => value >= threshold;
        public bool Convert(short value) => value >= threshold;
        public bool Convert(byte value) => value >= threshold;
        public bool Convert(sbyte value) => value >= threshold;
        public bool Convert(uint value) => value >= threshold;
        public bool Convert(ulong value) => value >= threshold;
        public bool Convert(ushort value) => value >= threshold;
        
        public bool Convert(IValueProvider<float> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<int> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<double> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<long> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<short> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<byte> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<sbyte> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<uint> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<ulong> value) => value.Value >= threshold;
        public bool Convert(IValueProvider<ushort> value) => value.Value >= threshold;

        public object Convert(object value)
        {
            return Convert((float)value);
        }
    }
}
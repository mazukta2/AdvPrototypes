using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Converters
{
    /// <summary>
    /// Converts a number into a boolean value, if the number is greater or equal to the threshold, it returns true.
    /// </summary>
    [HideMember]
    [Serializable]
    public class NumericToBoolConverter : 
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
        IConverter<IValueProvider<ushort>, bool>,
        IRequiresValidation,
        IDynamicComponent
    {
        [SerializeField]
        [Tooltip("Compare the value with the threshold, based on selected comparison type")]
        private BindComparison<float> _trueIfValue;
        
        [SerializeField]
        [HideInInspector]
        private Bind<float> threshold;
        private bool _validated;

        public BindComparison<float> Value
        {
            get
            {
                if (_validated)
                {
                    return _trueIfValue;
                }

                Validate(out _);
                return _trueIfValue;
            }
        }
        
        public string Id => "Numeric to Bool Converter";

        public string Description => "Converts a numeric value to a bool value, if the value is greater or equal to the threshold, it returns true";

        public bool IsSafe => true;
        
        public bool IsDynamic => _trueIfValue.value.IsBound;

        public bool Convert(float value) => Value.Compare(value);
        public bool Convert(int value) => Value.Compare(value);
        public bool Convert(double value) => Value.Compare((float)value);
        public bool Convert(long value) => Value.Compare(value);
        public bool Convert(short value) => Value.Compare(value);
        public bool Convert(byte value) => Value.Compare(value);
        public bool Convert(sbyte value) => Value.Compare(value);
        public bool Convert(uint value) => Value.Compare(value);
        public bool Convert(ulong value) => Value.Compare(value);
        public bool Convert(ushort value) => Value.Compare(value);
        
        public bool Convert(IValueProvider<float> value) => Value.Compare(value.Value);
        public bool Convert(IValueProvider<int> value) => Value.Compare(value.Value);
        public bool Convert(IValueProvider<double> value) => Value.Compare((float)value.Value);
        public bool Convert(IValueProvider<long> value) => Value.Compare(value.Value);
        public bool Convert(IValueProvider<short> value) => Value.Compare(value.Value);
        public bool Convert(IValueProvider<byte> value) => Value.Compare(value.Value);
        public bool Convert(IValueProvider<sbyte> value) => Value.Compare(value.Value);
        public bool Convert(IValueProvider<uint> value) => Value.Compare(value.Value);
        public bool Convert(IValueProvider<ulong> value) => Value.Compare(value.Value);
        public bool Convert(IValueProvider<ushort> value) => Value.Compare(value.Value);

        public object Convert(object value)
        {
            return Convert(System.Convert.ToSingle(value));
        }

        public void Validate(out bool hasChanged)
        {
            if (threshold.Value != 0 && _trueIfValue.value == 0 && _trueIfValue.comparisonType == BindComparison<float>.ComparisonType.Equals)
            {
                _trueIfValue.value = threshold;
                _trueIfValue.comparisonType = BindComparison<float>.ComparisonType.GreaterThanOrEquals;
                hasChanged = true;
            }
            else
            {
                hasChanged = false;
            }
            _validated = true;
        }
    }
}
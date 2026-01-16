using Postica.Common;
using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Starting point for custom modifiers for numeric data types.
    /// </summary>
    public abstract class NumericModifier : 
        IReadWriteModifier<double>,
        IReadWriteModifier<short>, 
        IReadWriteModifier<int>, 
        IReadWriteModifier<long>, 
        IReadWriteModifier<float>
    {
        [SerializeField]
        [HideInInspector]
        protected BindMode _mode;

        [NonSerialized]
        private string _defaultId;


        public NumericModifier()
        {
            _defaultId = StringUtility.NicifyName(GetType().Name.Replace("Modifier", ""));
        }

        ///<inheritdoc/>
        public virtual string Id => _defaultId;
        ///<inheritdoc/>
        public virtual string ShortDataDescription => string.Empty;
        ///<inheritdoc/>
        public BindMode ModifyMode { get => _mode; protected set => _mode = value; }

        /// <summary>
        /// Format the string as a variable.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string VarFormat(string value) => value.RT().Bold().Color(BindColors.Primary);
        
        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) 
            => mode == BindMode.Write && ModifyMode == BindMode.ReadWrite 
                ? InverseModify((double)value) : Modify((double)value);

        protected abstract double Modify(double value);
        protected virtual double InverseModify(double output) => output;

        protected virtual long Modify(long value) => (long)Modify((double)value);
        protected virtual long InverseModify(long output) => output;

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private double Read(double value) => _mode.CanRead() ? Modify(value) : InverseModify(value);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private double Write(double value) => _mode.CanRead() ? InverseModify(value) : Modify(value);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private long Read(long value) => _mode.CanRead() ? Modify(value) : InverseModify(value);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private long Write(long value) => _mode.CanRead() ? InverseModify(value) : Modify(value);


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        int IReadModifier<int>.ModifyRead(in int value) => (int)Read(value);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        short IReadModifier<short>.ModifyRead(in short value) => (short)Read(value);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        long IReadModifier<long>.ModifyRead(in long value) => (long)Read(value);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        double IReadModifier<double>.ModifyRead(in double value) => Read(value);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        float IReadModifier<float>.ModifyRead(in float value) => (float)Read(value);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        short IWriteModifier<short>.ModifyWrite(in short output) => (short)Write(output);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        int IWriteModifier<int>.ModifyWrite(in int output) => (int)Write(output);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        long IWriteModifier<long>.ModifyWrite(in long output) => (long)Write(output);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        float IWriteModifier<float>.ModifyWrite(in float output) => (float)Write(output);
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        double IWriteModifier<double>.ModifyWrite(in double output) => Write(output);
    }
}
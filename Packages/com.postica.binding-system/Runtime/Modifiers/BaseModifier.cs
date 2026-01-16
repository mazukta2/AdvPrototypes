using Postica.Common;
using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Starting point for custom modifiers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public abstract class BaseModifier<T> : IReadWriteModifier<T>
    {
        // TODO: Consider adding the IDynamicComponent interface here and having a virtual IsDynamic property.
        // By default it should return true for custom modifiers, and false for built-in ones. This is to avoid
        // breaking the functionality of existing custom modifiers.
        [SerializeField] [HideInInspector] protected BindMode _mode;

        [NonSerialized] private string _defaultId;


        protected BaseModifier()
        {
            _defaultId = StringUtility.NicifyName(GetType().Name.Replace("Modifier", ""));
        }

        ///<inheritdoc/>
        public virtual string Id => _defaultId;

        ///<inheritdoc/>
        public virtual string ShortDataDescription => string.Empty;

        ///<inheritdoc/>
        public BindMode ModifyMode
        {
            get => _mode;
            protected set => _mode = value;
        }

        /// <summary>
        /// Modify the value. This method will be called when the value is read, or if the bind mode is write-only, during write.
        /// </summary>
        /// <param name="value">The input value to be modified</param>
        /// <returns>A modified value of the same type as <paramref name="value"/></returns>
        protected abstract T Modify(T value);
        
        /// <summary>
        /// Modify the value in the inverse direction. <br/>
        /// This method will be called when the value is written, but only if bind mode is read-write. <br/>
        /// For any other mode, the other <see cref="Modify(T)"/> method will be called.
        /// </summary>
        /// <param name="output">The output value to pass to input</param>
        /// <returns>The modified output value</returns>
        protected virtual T InverseModify(T output) => output;

        /// <summary>
        /// Format the string as a variable.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static string VarFormat(string value) => value.RT().Bold().Color(BindColors.Primary);

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        T IReadModifier<T>.ModifyRead(in T value) => _mode.CanRead() ? Modify(value) : InverseModify(value);

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        T IWriteModifier<T>.ModifyWrite(in T value) => _mode.CanRead() ? InverseModify(value) : Modify(value);

        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => ModifyMode == BindMode.ReadWrite && mode == BindMode.Write
            ? InverseModify((T)value)
            : Modify((T)value);
    }
}
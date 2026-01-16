using System;
using System.Collections.Generic;
using UnityEngine;
using Postica.BindingSystem.Accessors;

using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using Postica.Common;
using UnityEngine.Serialization;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This class allows binding a value to another value directly from the inspector.
    /// This class provides <b>only read-only values</b>.<br/><br/>
    /// <b>This class is similar to <see cref="ReadOnlyBind{T}"/> but it does not allow parameters.</b>
    /// <para/>
    /// Visually this object operates in two modes, one bound to other object's value, the other not. 
    /// The first mode allows the user to select a bind source and a path, and the value
    /// returned by this object will be the value found at the source following the selected path.
    /// The second mode behaves as a normal value field where user inserts the value directly.
    /// <para/>
    /// This object is designed to operate with minimum overhead possible and reach an execution performance 
    /// close to direct access of the members while generating near-zero memory allocations (in most cases it is zero).
    /// <para/>
    /// This object implicitly converts to <typeparamref name="T"/>. 
    /// To convert a value of type <typeparamref name="T"/> to this object, use <see cref="BindExtensions.Bind{T}(T)"/>
    /// </summary>
    /// <typeparam name="T">The type to bind</typeparam>
    [Serializable]
    // [HideMember(HideMemberAttribute.Hide.InternalsOnly)]
    public class ReadOnlyBindLite<T> : IBind<T>, IBindData<BindDataLite>, IValueProvider<T>, IDataRefresher, IDisposable, IBindAccessor
    {
        [SerializeField]
        [HideMember]
        [BindType]
        [BindOverride(BindMode.Read)]
        [BindValuesOnChange(nameof(ResetBind))]
        private BindDataLite _bindData;
        [SerializeField]
        [HideMember]
        [FormerlySerializedAs("_isBinded")]
        private bool _isBound;
        [SerializeField]
        [HideMember]
        private T _value;

        private IAccessor<T> _accessor;
        private ModifyDelegate<T> _readModifier;
        [NonSerialized]
        private bool _initialized;

        [NonSerialized]
        private bool _wasEnabled;
        [NonSerialized]
        private bool _updateOnEnable;

        private T _prevValue;

        private ValueChanged<T> _valueChanged;
        
        /// <summary>
        /// If true, the bind will update the value on enable if the context is a <see cref="Behaviour"/> and it is enabled.
        /// </summary>
        [HideMember]
        public bool UpdateOnEnable
        {
            get => _updateOnEnable;
            set => _updateOnEnable = value;
        }
        
        /// <summary>
        /// This event will be raised each time the bound value has changed. <br/>
        /// Beware that this event will be raised at the closest Unity Player Loop stage.
        /// </summary>
        public event ValueChanged<T> ValueChanged
        {
            add
            {
                if (_valueChanged == null)
                {
                    BindingEngineInternal.RegisterDataRefresher(this);
                }
                _valueChanged += value;
            }
            remove
            {
                _valueChanged -= value;
                if (_valueChanged == null)
                {
                    BindingEngineInternal.UnregisterDataRefresher(this);
                }
            }
        }

        private void Initialize()
        {
            _initialized = true;
            BuildAccessor(_bindData.Source);
            var modifier = _bindData.Modifiers.Length > 1
                         ? new CompoundModifier<T>(this, _bindData.Modifiers)
                         : _bindData.Modifiers.Length > 0 ? _bindData.Modifiers[0] : null;
            
            if(modifier is IRequiresAutoUpdate { ShouldAutoUpdate: true } requiresAutoUpdate)
            {
                _updateOnEnable |= requiresAutoUpdate.UpdateOnEnable;
            }
            
            _readModifier = modifier.GetReadFunc<T>(this, SetSmartModifierValue);
        }
        
        private void SetSmartModifierValue(ISmartModifier modifier, T value)
        {
            if (!_isBound)
            {
                return;
            }

            if (_accessor is IAccessor { CanWrite: true })
            {
                _accessor.SetValue(_bindData.Source, value);
                if (_valueChanged != null && !Equals(_prevValue, value))
                {
                    _valueChanged(_prevValue, value);
                }
                _prevValue = value;
            }
        }

        private void BuildAccessor(object source)
        {
            if (source == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_bindData.Path))
            {
                _accessor = AccessorsFactory.GetAccessor<T>(source,
                                                            _bindData.Path,
                                                            this,
                                                            _bindData.PreModifiers,
                                                            _bindData.ReadConverter,
                                                            _bindData.WriteConverter);
                return;
            }

            // We do not allow self references in read mode
            if (_bindData.Mode != BindMode.Read)
            {
                throw new InvalidOperationException($"{GetType().Name}<{typeof(T).Name}> cannot operate on self reference, the Bind Mode should be read-only");
            }

            if (!(source is T)
                && !ConvertersFactory.HasConversion(source.GetType(), typeof(T), out _))
            {
                throw new InvalidOperationException($"{GetType().Name}<{typeof(T).Name}> cannot operate on self reference, the bind source is not compatible with value type");
            }

            _accessor = typeof(T) == typeof(bool) && source is Object
                ? new UnityObjectToBoolAccessor(this) as IAccessor<T>
                      : new SourceAccessor<T>(this);
        }

        /// <summary>
        /// Gets the <see cref="BindDataLite"/> if the field is bound to, null otherwise.
        /// </summary>
        [HideMember]
        public BindDataLite? BindData => _isBound ? _bindData : (BindDataLite?)null;

        /// <summary>
        /// Gets the <see cref="IAccessor"/> associated with this bind. <br/>
        /// The accessor typically handles all the heavy lifting for value read.
        /// </summary>
        [HideMember]
        public IAccessor<T> Accessor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_accessor == null && _bindData.IsValid)
                {
                    Initialize();
                }
                return _accessor;
            }
        }

        [HideMember]
        object IBindAccessor.RawAccessor => Accessor;

        /// <summary>
        /// Whether this object should be bound to another object value or not
        /// </summary>
        public bool IsBound
        {
            get => _isBound;
            set => _isBound = value;
        }

        /// <summary>
        /// Gets or sets the source for the bind. Setting the value requires this bind object to have <see cref="IsBound"/> to be true first.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if the new source value is not compatible with the existing bind path</exception>
        public Object Source
        {
            get => _bindData.Source;
            set
            {
                if (!_isBound)
                {
                    return;
                }
                if (_bindData.Source == value)
                {
                    return;
                }
                if (value is null || !_bindData.IsValid || !_initialized)
                {
                    _bindData.Source = value;
                    ResetBind();
                    return;
                }
                try
                {
                    BuildAccessor(value);
                    _bindData.Source = value;
                }
                catch (ArgumentException)
                {
                    throw new InvalidOperationException($"The ReadOnlyBindLite<{typeof(T).Name}> with path {_bindData.Path} cannot change its bound source to {value} because it is not compatible");
                }
            }
        }

        /// <summary>
        /// Gets the value of this binder.
        /// </summary>
        public T Value
        {
            get
            {
                if (_isBound)
                {
                    return GetValue(_bindData.Source);
                }
                return _value;
            }
        }

        [Obsolete("Use UnboundValue instead")]
        public T FallbackValue
        {
            get => UnboundValue;
            set => UnboundValue = value;
        }
        
        /// <summary>
        /// Gets or sets the unbound value of this bind
        /// </summary>
        public T UnboundValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _value = value;
        }

        /// <inheritdoc/>
        [HideMember]
        public object UnsafeValue => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(object target)
        {
            if (!_initialized)
            {
                Initialize();
            }
            if (_readModifier != null)
            {
                return _readModifier(_accessor.GetValue(target));
            }
            else
            {
                return _accessor.GetValue(target);
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T PipeValue(T value)
        {
            return value;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T StoreValueTo(ref T field)
        {
            var value = Value;
            field = value;
            return value;
        }

        private void ResetBind()
        {
            _accessor = null;
            _initialized = false;
            _prevValue = default;
        }

        public override string ToString()
        {
            if (Application.isPlaying)
            {
                return (_isBound ? $"{_bindData.FullPath()}: " : "") + Value;
            }
            return _isBound ? _bindData.FullPath() : _value?.ToString();
        }
        
        /// <inheritdoc/>
        (Object owner, string path) IDataRefresher.RefreshId => (_bindData.Context, _bindData.Id);
        
        /// <inheritdoc/>
        public bool CanRefresh() => _valueChanged != null || (_isBound && _bindData.IsAutoUpdated && _bindData.IsValid);

        /// <inheritdoc/>
        public void Refresh()
        {
            if(_valueChanged == null) { return; }

            if (_bindData.Context is Behaviour { isActiveAndEnabled: false })
            {
                _wasEnabled = false;
                return;
            }

            if (!_wasEnabled && _updateOnEnable)
            {
                var value = Value;
                _valueChanged?.Invoke(_prevValue, value);
            }
            _wasEnabled = true;
            
            var newValue = Value;
            if (Equals(newValue, _prevValue)) return;
            var lastValue = _prevValue;
            _prevValue = newValue;
            _valueChanged?.Invoke(lastValue, newValue);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            BindingEngineInternal.UnregisterDataRefresher(this);
        }

        /// <summary>
        /// Constructor. Creates a bind field for <paramref name="target"/> at specified <paramref name="path"/>
        /// </summary>
        /// <param name="target">The object to bind to</param>
        /// <param name="path">The path to bind at</param>
        public ReadOnlyBindLite(Object target, string path)
        {
            _bindData = new BindDataLite(target, path);
            _value = default;
            _accessor = default;
            _isBound = _bindData.IsValid;
        }

        internal ReadOnlyBindLite(in BindDataLite data)
        {
            _bindData = data;
            _isBound = _bindData.IsValid && (_bindData.Mode == BindMode.Read || _bindData.Mode == BindMode.ReadWrite);
            _accessor = default;
        }

        /// <summary>
        /// Constructor. Creates a read-only bind field with a default value of type <typeparamref name="T"/>
        /// </summary>
        public ReadOnlyBindLite() { }

        /// <summary>
        /// Constructor. Creates an unbound field with specified direct <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value to set</param>
        public ReadOnlyBindLite(in T value) { _value = value; }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);
        public static implicit operator T(ReadOnlyBindLite<T> binder) => binder.Value;
        public static explicit operator ReadOnlyBindLite<T>(T value) => new ReadOnlyBindLite<T>(value);
        public static implicit operator ReadOnlyBindLite<T>(Bind<T> binder)
        {
            var bindData = binder.BindData;
            if (bindData == null)
            {
                return new ReadOnlyBindLite<T>(binder.UnboundValue);
            }
            else
            {
                return new ReadOnlyBindLite<T>(bindData.Value.Source, bindData.Value.Path) { _value = binder.UnboundValue };
            }
        }

        public static implicit operator ReadOnlyBindLite<T>(BindExtensions.NewBind<T> newBind) => new ReadOnlyBindLite<T>(newBind.value);
    }
}

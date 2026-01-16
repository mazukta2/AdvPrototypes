using System;
using System.Collections.Generic;
using UnityEngine;
using Postica.BindingSystem.Accessors;

using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using UnityEngine.Serialization;
using Postica.Common;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This class allows binding a value to another value directly from the inspector. 
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
    public class Bind<T> : IBind<T>, IBindData<BindData<T>>, IValueProvider<T>, IDataRefresher, IBindAccessor, IDisposable, ISerializationCallbackReceiver, IFormattable
    {
        [SerializeField]
        [HideMember]
        [BindType]
        [BindValuesOnChange(nameof(ResetBind))]
        private BindData<T> _bindData;
        [SerializeField]
        [FormerlySerializedAs("_isBinded")]
        [HideMember]
        private bool _isBound;
        [SerializeField]
        [HideMember]
        private T _value;

        private IAccessor<T> _accessor;
        private ModifyDelegate<T> _readValue;
        private ModifyDelegate<T> _writeValue;
        [NonSerialized]
        private bool _writeInProgress;
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
                if(_valueChanged == null)
                {
                    BindingEngineInternal.RegisterDataRefresher(this);
                }
                _valueChanged += value;
            }
            remove
            {
                _valueChanged -= value;
                if(_valueChanged == null)
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

            if (modifier == null)
            {
                return;
            }
            
            if(modifier is IRequiresAutoUpdate { ShouldAutoUpdate: true } requiresAutoUpdate)
            {
                _updateOnEnable |= requiresAutoUpdate.UpdateOnEnable;
            }
            
            (_readValue, _writeValue) = modifier.GetBothFunc<T>(this, SetSmartModifierValue);
        }

        private void SetSmartModifierValue(ISmartModifier modifier, T value)
        {
            if (!_isBound)
            {
                return;
            }

            if (_accessor is not IAccessor { CanWrite: true })
            {
                return;
            }
            _accessor.SetValue(_bindData.Source, value);
            if (_valueChanged != null && !Equals(_prevValue, value))
            {
                _valueChanged(_prevValue, value);
            }
            _prevValue = value;
        }

        private void BuildAccessor(object source)
        {
            if(source == null)
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
                                                            _bindData.WriteConverter,
                                                            _bindData.Parameters.GetValues(),
                                                            _bindData.MainParameterIndex);
                return;
            }

            // We do not allow self references in write mode
            if(_bindData.Mode != BindMode.Read)
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
        /// Gets the <see cref="BindData"/> if the field is bound to, null otherwise.
        /// </summary>
        [HideMember]
        public BindData<T>? BindData => _isBound ? _bindData : (BindData<T>?)null;

        /// <summary>
        /// Gets the <see cref="IAccessor"/> associated with this bind. <br/>
        /// The accessor typically handles all the heavy lifting for value read/write.
        /// </summary>
        [HideMember]
        public IAccessor<T> Accessor {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if(_accessor == null && _bindData.IsValid)
                {
                    Initialize();
                }
                return _accessor;
            }
        }

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
        /// Whether this object can read its value or not
        /// </summary>
        public bool CanRead => !_isBound || _bindData.Mode.CanRead();
        /// <summary>
        /// Whether this object can write its value or not
        /// </summary>
        public bool CanWrite => !_isBound || _bindData.Mode.CanWrite();

        /// <summary>
        /// Gets or sets the source for the bind. <br/>
        /// Setting the value requires this bind object to have <i><see cref="IsBound"/></i> <b>to be true first</b>.
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
                if(_bindData.Source == value)
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
                catch(ArgumentException)
                {
                    throw new InvalidOperationException($"The Bind<{typeof(T).Name}> with path {_bindData.Path} cannot change its bound source to {value} because it is not compatible");
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of this binder.
        /// </summary>
        public T Value {
            get {
                if (_isBound)
                {
                    return GetValue(_bindData.Source);
                }
                return _value;
            }
            set {
                if (_isBound)
                {
                    // This is to avoid recursive looping while setting the value
                    if (!_writeInProgress)
                    {
                        _writeInProgress = true;
                        SetValue(_bindData.Source, value);
                        _writeInProgress = false;
                    }
                }
                else
                {
                    _value = value;
                    if (_valueChanged != null && !Equals(_prevValue, value))
                    {
                        var prevValue = _prevValue;
                        _valueChanged(prevValue, value);
                    }

                    _prevValue = value;
                }
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
            if(_bindData.Mode == BindMode.Write)
            {
                if (!((IAccessor)_accessor).CanRead)
                {
                    throw new InvalidOperationException($"The Bind<{typeof(T).Name}> at path {_bindData.Path} is not read enabled");
                }

                return _prevValue;
            }
            
            if(_readValue != null)
            {
                return _prevValue = _readValue(_accessor.GetValue(target));
            }
            
            return _prevValue = _accessor.GetValue(target);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValueUnsafe(object target)
        {
            if (!_isBound)
            {
                return _value;
            }
            
            if (!_initialized)
            {
                Initialize();
            }
            
            if (!((IAccessor)_accessor).CanRead)
            {
                return _prevValue;
            }
            
            if(_readValue != null)
            {
                return _readValue(_accessor.GetValue(target));
            }
            
            return _accessor.GetValue(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(object target, in T value)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_bindData.Mode == BindMode.Read)
            {
                if (((IAccessor)_accessor).CanWrite)
                {
                    if (Application.isEditor && _bindData.IsLiveDebug)
                    {
                        _bindData.DebugValue = value;
                    }

                    _prevValue = value;
                    return;
                }
                
                throw new InvalidOperationException(
                    $"The Bind<{typeof(T).Name}> at path {_bindData.Path} is not write enabled");
            }

            if (Application.isEditor && _bindData.IsLiveDebug)
            {
                _bindData.DebugValue = value;
            }
            if(_writeValue != null)
            {
                _accessor.SetValue(target, _writeValue(value));
            }
            else
            {
                _accessor.SetValue(target, value);
            }
            
            if (_valueChanged != null && !Equals(_prevValue, value))
            {
                _valueChanged(_prevValue, value);
            }
            _prevValue = value;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T PipeValue(T value)
        {
            Value = value;
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

        /// <inheritdoc/>
        (Object owner, string path) IDataRefresher.RefreshId => (_bindData.Context, _bindData.Id);
        
        /// <inheritdoc/>
        public bool CanRefresh() => _valueChanged != null || (_isBound && _bindData.IsAutoUpdated && _bindData.IsValid);

        /// <inheritdoc/>
        public void Refresh()
        {
            if(_valueChanged == null && !_bindData.IsAutoUpdated) { return; }

            if (_bindData.Context is Behaviour { isActiveAndEnabled: false })
            {
                _wasEnabled = false;
                return;
            }

            if (!_wasEnabled && _updateOnEnable)
            {
                T value = _bindData.Mode.CanRead() ? GetValueUnsafe(_bindData.Source) : _prevValue;
                _valueChanged?.Invoke(_prevValue, value);
                if (_bindData.Mode.CanWrite())
                {
                    Value = value;
                }
            }
            _wasEnabled = true;
            
            if (_isBound && _bindData.Mode == BindMode.Write)
            {
                if (!_initialized)
                {
                    Initialize();
                }
                BindingEngineInternal.UnregisterDataRefresher(this);
                return;
            }
            
            var newValue = _isBound ? GetValueUnsafe(_bindData.Source) : _value;
            if (Equals(newValue, _prevValue)) return;
            var lastValue = _prevValue;
            _prevValue = newValue;
            _valueChanged?.Invoke(lastValue, newValue);
        }

        public override string ToString()
        {
            if (Application.isPlaying)
            {
                return (_isBound ? $"{_bindData.FullPath()}: " : "") + GetValueUnsafe(_bindData.Source);
            }
            return _isBound ? _bindData.FullPath() : _value?.ToString();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            var value = Value;
            if(value is IFormattable formattable)
            {
                return formattable.ToString(format, formatProvider);
            }
            return value?.ToString();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            BindingEngineInternal.UnregisterDataRefresher(this);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Do nothing
        }
        
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (!_isBound)
            {
                return;
            }
        
            AutoRegister();
        }

        private void AutoRegister()
        {
            if (_bindData is { IsValueChangedEnabled: true, HasPersistentEvents: true })
            {
                ValueChanged -= InvokePersistentEvents;
                ValueChanged += InvokePersistentEvents;
                return;
            }

            if (_bindData.IsAutoUpdated)
            {
                BindingEngineInternal.RegisterDataRefresher(this);
            }
        }

        private void InvokePersistentEvents(T oldValue, T newValue)
        {
            if (!_isBound)
            {
                return;
            }

            if (_bindData.IsValueChangedEnabled)
            {
                _bindData.OnValueChanged.Invoke(newValue);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

        /// <summary>
        /// Constructor. Creates a bind field for <paramref name="target"/> at specified <paramref name="path"/>
        /// </summary>
        /// <param name="target">The object to bind to</param>
        /// <param name="path">The path to bind at</param>
        /// <param name="parameters">Parameters for the specified path. Can be either direct values or <see cref="IValueProvider"/>s</param>
        public Bind(Object target, string path, params object[] parameters)
        {
            _bindData = new BindData<T>(target, path, parameters.ToValueProviders(), 0);
            _value = default;
            _accessor = default;
            _isBound = _bindData.IsValid;
            _writeInProgress = false;
        }

        internal Bind(in BindData data)
        {
            _bindData = data;
            _isBound = _bindData.IsValid;
            _writeInProgress = false;
            _accessor = default;
        }

        /// <summary>
        /// Constructor. Creates an unbound field with specified direct <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value to set</param>
        public Bind(in T value) 
        {
            _value = value;
        }

        public static implicit operator T(Bind<T> binder) => binder.Value;
        public static explicit operator Bind<T>(T value) => new Bind<T>(value);
        public static explicit operator Bind<T>(ReadOnlyBind<T> binder)
        {
            var bindData = binder.BindData;
            if (bindData == null)
            {
                return new Bind<T>(binder.UnboundValue);
            }
            else
            {
                return new Bind<T>(bindData.Value.Source, bindData.Value.Path) { _value = binder.UnboundValue };
            }
        }

        public static implicit operator Bind<T>(BindExtensions.NewBind<T> newBind) => new Bind<T>(newBind.value);
    }
}

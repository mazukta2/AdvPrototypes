using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Postica.BindingSystem.Accessors;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This class allows binding a value to another value directly from the inspector. 
    /// <para/>
    /// This class allows the user to select a bind source and a path, and the value
    /// returned by this object will be the value found at the source following the selected path.
    /// <para/>
    /// This object is designed to operate with minimum overhead possible and reach an execution performance 
    /// close to direct access of the members while generating near-zero memory allocations (in some cases it is zero).
    /// <para/>
    /// This object implicitly converts to <typeparamref name="T"/>. 
    /// To convert a value of type <typeparamref name="T"/> to this object, use <see cref="BindExtensions.Bind{T}(T)"/>
    /// </summary>
    /// <typeparam name="T">The type to bind</typeparam>
    [Serializable]
    public class BindDataFor<T> : IBind<T>, IValueProvider<T>, IDataRefresher, IDisposable, IBindAccessor, ISerializationCallbackReceiver
    {
        [SerializeField]
        [HideMember]
        [BindType]
        [BindValuesOnChange(nameof(ResetBind))]
        private BindData<T> _bindData;

        private IAccessor<T> _accessor;
        private ModifyDelegate<T> _readModifier;
        private ModifyDelegate<T> _writeModifier;
        
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

        /// <summary>
        /// Gets the <see cref="BindData"/> if the field is bound to, null otherwise.
        /// </summary>
        [HideMember]
        public BindData BindData => _bindData;

        /// <summary>
        /// Gets the <see cref="IAccessor"/> associated with this bind. <br/>
        /// The accessor typically handles all the heavy lifting for value read/write.
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
        /// Gets or sets the source for the bind.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if the new source value is not compatible with the existing bind path</exception>
        public Object Source
        {
            get => _bindData.Source;
            set
            {
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
                    throw new InvalidOperationException($"The BindDataFor<{typeof(T).Name}> at path {_bindData.Path} cannot change its bound source to {value} because it is not compatible");
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of this binder.
        /// </summary>
        public T Value
        {
            get
            {
                if (!_initialized)
                {
                    Initialize();
                }
                if (_readModifier == null)
                {
                    return _accessor.GetValue(_bindData.Source);
                }

                return _readModifier(_accessor.GetValue(_bindData.Source));
            }
            set
            {
                if (_writeInProgress)
                {
                    return;
                }
                if (!_initialized)
                {
                    Initialize();
                }
                if (_bindData.Mode == BindMode.Read)
                {
                    throw new InvalidOperationException($"The BindDataFor<{typeof(T).Name}> at path {_bindData.Path} is not write enabled");
                }
                if (Application.isEditor && _bindData.IsLiveDebug)
                {
                    _bindData.DebugValue = value;
                }

                _writeInProgress = true;
                if (_writeModifier == null)
                {
                    _accessor.SetValue(_bindData.Source, value);
                }
                else
                {
                    _accessor.SetValue(_bindData.Source, _writeModifier(value));
                }
                _writeInProgress = false;
            }
        }

        /// <inheritdoc/>
        [HideMember]
        public object UnsafeValue => Value;

        bool IBind.IsBound { get => true; set { } }

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
            
            (_readModifier, _writeModifier) = modifier.GetBothFunc<T>(this, SetSmartModifierValue);
        }

        private void SetSmartModifierValue(ISmartModifier modifier, T value)
        {
            if (_accessor is not IAccessor { CanWrite: false })
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
                                                            _bindData.WriteConverter,
                                                            _bindData.Parameters.GetValues(),
                                                            _bindData.MainParameterIndex);
                return;
            }

            // We do not allow self references in read mode
            if (_bindData.Mode != BindMode.Read)
            {
                throw new InvalidOperationException($"{GetType().Name}<{typeof(T).Name}> cannot operate on self reference, the Bind Mode should be read-only");
            }

            if (!typeof(T).IsAssignableFrom(source.GetType())
                && !ConvertersFactory.HasConversion(source.GetType(), typeof(T), out _))
            {
                throw new InvalidOperationException($"{GetType().Name}<{typeof(T).Name}> cannot operate on self reference, the bind source is not compatible with value type");
            }

            _accessor = typeof(T) == typeof(bool) && typeof(Object).IsAssignableFrom(source.GetType())
                      ? new UnityObjectToBoolAccessor(this) as IAccessor<T>
                      : new SourceAccessor<T>(this);
        }

        private void ResetBind()
        {
            _accessor = null;
            _initialized = false;
        }

        public override string ToString()
        {
            if (Application.isPlaying)
            {
                return $"{_bindData.FullPath()}: {Value}";
            }

            return _bindData.FullPath();
        }
        
        /// <inheritdoc/>
        (Object owner, string path) IDataRefresher.RefreshId => (_bindData.Context, _bindData.Id);

        /// <inheritdoc/>
        public bool CanRefresh() => _valueChanged != null || (_bindData.IsAutoUpdated && _bindData.IsValid);

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
                T value = _bindData.Mode.CanRead() ? Value : _prevValue;
                _valueChanged?.Invoke(_prevValue, value);
                if (_bindData.Mode.CanWrite())
                {
                    Value = value;
                }
            }
            _wasEnabled = true;
            
            if (_bindData.Mode == BindMode.Write)
            {
                if (!_initialized)
                {
                    Initialize();
                }
                BindingEngineInternal.UnregisterDataRefresher(this);
                return;
            }
            
            var newValue = Value;
            if (Equals(newValue, _prevValue)) return;
            var lastValue = _prevValue;
            _prevValue = newValue;
            _valueChanged?.Invoke(lastValue, newValue);
        }

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
            AutoRegister();
        }

        private void AutoRegister()
        {
            if (_bindData.IsValueChangedEnabled && _bindData.HasPersistentEvents)
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
            if (_bindData.IsValueChangedEnabled)
            {
                _bindData.OnValueChanged.Invoke(newValue);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

        public static implicit operator T(BindDataFor<T> binder) => binder.Value;
        public static implicit operator Bind<T>(BindDataFor<T> binder) => new Bind<T>(binder._bindData);
        public static implicit operator ReadOnlyBind<T>(BindDataFor<T> binder) => new ReadOnlyBind<T>(binder._bindData);
        public static implicit operator BindDataFor<T>(Bind<T> binder) => new BindDataFor<T> { _bindData = binder.BindData ?? default };
        public static implicit operator BindDataFor<T>(ReadOnlyBind<T> binder) => new BindDataFor<T> { _bindData = binder.BindData ?? default };
    }
}

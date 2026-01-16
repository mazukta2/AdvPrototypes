using System;
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
    /// This class provides <b> write-only values </b>.
    /// <para/>
    /// Visually this object operates in two modes, one bound to other object's value, the other not. 
    /// The first mode allows the user to select a bind source and a path, and the value
    /// returned by this object will be the value found at the source following the selected path.
    /// The second mode behaves as a normal value field where user checks (for debug purposes) the value directly.
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
    public class WriteOnlyBind<T> : IBind<T>, IBindData<BindData>, IBindAccessor, IDataRefresher
    {
        [SerializeField]
        [HideMember]
        [BindType]
        [BindOverride(BindMode.Write)]
        [BindValuesOnChange(nameof(ResetBind))]
        private BindData<T> _bindData;
        [SerializeField]
        [HideMember]
        [FormerlySerializedAs("_isBinded")]
        private bool _isBound;
        [SerializeField]
        [HideMember]
        private T _value;

        private IAccessor<T> _accessor;
        private ModifyDelegate<T> _writeModifier;
        [NonSerialized]
        private bool _writeInProgress;
        [NonSerialized]
        private bool _initialized;
        [NonSerialized]
        private bool _wasEnabled;
        [NonSerialized]
        private bool _updateOnEnable;

        [NonSerialized]
        private T _prevValue;
        
        /// <summary>
        /// If true, the bind will update the value on enable if the context is a <see cref="Behaviour"/> and it is enabled.
        /// </summary>
        [HideMember]
        public bool UpdateOnEnable
        {
            get => _updateOnEnable;
            set => _updateOnEnable = value;
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
            
            _writeModifier = modifier.GetWriteFunc<T>(this, SetSmartModifierValue);
        }
        
        private void SetSmartModifierValue(ISmartModifier modifier, T value)
        {
            if (!_isBound)
            {
                return;
            }

            _accessor.SetValue(_bindData.Source, value);
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

            if (!typeof(T).IsAssignableFrom(source.GetType()) 
                && !ConvertersFactory.HasConversion(source.GetType(), typeof(T), out _))
            {
                throw new InvalidOperationException($"{GetType().Name}<{typeof(T).Name}> cannot operate on self reference, the bind source is not compatible with value type");
            }


            _accessor = typeof(T) == typeof(bool) && typeof(Object).IsAssignableFrom(source.GetType()) 
                      ? new UnityObjectToBoolAccessor(this) as IAccessor<T>
                      : new SourceAccessor<T>(this);
        }

        /// <summary>
        /// Gets the <see cref="BindData"/> if the field is bound to, null otherwise.
        /// </summary>
        [HideMember]
        public BindData? BindData => _isBound ? _bindData : (BindData?)null;

        /// <summary>
        /// Gets the <see cref="IAccessor"/> associated with this bind. 
        /// The accessor typically handles all the heavy lifting for value writing.
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
        /// Gets or sets the source for the bind. <br/>
        /// Setting the value requires this bind object to have <see cref="IsBound"/> <b>to be true first</b>.
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
        /// Sets the value of this binder.
        /// </summary>
        public T Value {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(object target, in T value)
        {
            if (!_initialized)
            {
                Initialize();
            }

            _prevValue = value;
            
            if (Application.isEditor && _bindData.IsLiveDebug)
            {
                _bindData.DebugValue = value;
            }
            if(_writeModifier != null)
            {
                _accessor.SetValue(target, _writeModifier(value));
            }
            else
            {
                _accessor.SetValue(target, value);
            }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T PipeValue(T value)
        {
            Value = value;
            return value;
        }

        private void ResetBind()
        {
            _accessor = null;
            _initialized = false;
        }
        
        /// <inheritdoc/>
        (Object owner, string path) IDataRefresher.RefreshId => (_bindData.Context, _bindData.Id);
        
        /// <inheritdoc/>
        public bool CanRefresh() => _isBound && _bindData.IsAutoUpdated && _bindData.IsValid;

        /// <inheritdoc/>
        public void Refresh()
        {
            if(!_bindData.IsAutoUpdated) { return; }

            if (!_isBound) return;
            
            if (_bindData.Context is Behaviour { isActiveAndEnabled: false })
            {
                _wasEnabled = false;
                return;
            }

            if (!_wasEnabled && _updateOnEnable)
            {
                Value = _prevValue;
            }
            _wasEnabled = true;
            
            if (!_initialized)
            {
                Initialize();
            }
            BindingEngineInternal.UnregisterDataRefresher(this);
        }
        
        public override string ToString()
        {
            if (Application.isPlaying)
            {
                return _isBound ? $"{_bindData.FullPath()}" : _value?.ToString();
            }
            return _isBound ? _bindData.FullPath() : _value?.ToString();
        }

        /// <summary>
        /// Constructor. Creates a bind field for <paramref name="target"/> at specified <paramref name="path"/>
        /// </summary>
        /// <param name="target">The object to bind to</param>
        /// <param name="path">The path to bind at</param>
        /// <param name="parameters">Parameters for the specified path. Can be either direct values or <see cref="IValueProvider"/>s</param>
        public WriteOnlyBind(Object target, string path, params object[] parameters)
        {
            _bindData = new BindData(target, path, parameters.ToValueProviders(), 0);
            _value = default;
            _accessor = default;
            _isBound = _bindData.IsValid;
            _writeInProgress = false;
        }

        internal WriteOnlyBind(in BindData data)
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
        public WriteOnlyBind(in T value) 
        {
            _value = value;
        }

        public static explicit operator WriteOnlyBind<T>(T value) => new WriteOnlyBind<T>(value);
        public static explicit operator WriteOnlyBind<T>(ReadOnlyBind<T> binder)
        {
            var bindData = binder.BindData;
            if (bindData == null)
            {
                return new WriteOnlyBind<T>(binder.UnboundValue);
            }
            else
            {
                return new WriteOnlyBind<T>(bindData.Value.Source, bindData.Value.Path) { _value = binder.UnboundValue };
            }
        }

        public static implicit operator WriteOnlyBind<T>(BindExtensions.NewBind<T> newBind) => new WriteOnlyBind<T>(newBind.value);
    }
}

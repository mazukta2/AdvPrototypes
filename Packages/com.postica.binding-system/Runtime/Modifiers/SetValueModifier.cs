using System;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Modifiers
{

    /// <summary>
    /// A modifier which sets a value up the chain, disregarding the value it receives.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/set_value")]
    [TypeDescription("Sets a value up the chain, disregarding the value it receives.")]
    public class SetValueModifier<T> : IWriteModifier<T>, ISmartValueModifier<T>, IRequiresAutoUpdate, IDynamicComponent where T : struct
    {
        [Tooltip("The value to set, ignoring any receiving value.")]
        public ReadOnlyBind<T> setValue;

        [SerializeField]
        [Tooltip("If true, the value will be set when the context object becomes enabled.")]
        protected bool _setOnEnable = true;

        [NonSerialized] private bool _isInitialized;
        
        private IBind _owner;
        private Action<ISmartModifier, T> _setValue;
        
        public T ModifyWrite(in T value)
        {
            return setValue.Value;
        }

        ///<inheritdoc/>
        public virtual string Id => "Set Value";

        ///<inheritdoc/>
        public virtual string ShortDataDescription => setValue.IsBound ? setValue.ToString().RT().Color(BindColors.Primary) : setValue.Value.ToString();

        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => setValue.Value;
        ///<inheritdoc/>
        public BindMode ModifyMode => BindMode.Write;

        private void InitializeIfNeeded()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            
            setValue.UpdateOnEnable = _setOnEnable;

            setValue.ValueChanged -= SetValueChanged;
            setValue.ValueChanged += SetValueChanged;
        }

        private void SetValueChanged(T _, T newValue)
        {
            SetValue?.Invoke(this, newValue);
        }

        public void OnValidate()
        {
            setValue.UpdateOnEnable = _setOnEnable;
        }

        public IBind BindOwner
        {
            get => _owner;
            set
            {
                if (_owner == value)
                {
                    return;
                }

                _owner = value;
                InitializeIfNeeded();
            }
        }
        
        void ISmartModifier.SetSetValueCallback<S>(Action<ISmartModifier, S> setValue)
        {
            if (!typeof(T).IsAssignableFrom(typeof(S)))
            {
                return;
            }
            
            SetValue = (modifier, value) => setValue(modifier, value is S sValue ? sValue : default);
        }

        public Action<ISmartModifier, T> SetValue
        {
            get => _setValue;
            set
            {
                if (_setValue == value)
                {
                    return;
                }
                
                _setValue = value;
                InitializeIfNeeded();
            }
        }
        
        public bool ShouldAutoUpdate => true;

        public bool UpdateOnEnable
        {
            get => _setOnEnable;
            set => _setOnEnable = value;
        }

        public bool IsDynamic => setValue.IsBound;
    }
    
    /// <summary>
    /// A modifier which sets a value up the chain, disregarding the value it receives.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [ModifierOptions(BindMode.Write)]
    [TypeIcon("_bsicons/modifiers/set_value")]
    public class SetObjectValueModifier<T> : IObjectModifier<T>, ISmartModifier, IRequiresAutoUpdate, IDynamicComponent where T : class
    {
        [Tooltip("The value to set, ignoring any receiving value.")]
        [BindTypeSource(nameof(TargetType))]
        public ReadOnlyBind<T> setValue;

        [SerializeField]
        [Tooltip("If true, the value will be set when the context object becomes enabled.")]
        protected bool _setOnEnable = true;

        
        [NonSerialized] private bool _isInitialized;
        
        private IBind _owner;
        private Action<ISmartModifier, T> _setValue;
        
        [SerializeField]
        [HideInInspector]
        protected SerializedType _targetType;
        public virtual Type TargetType
        {
            get => _targetType?.Get() ?? typeof(T);
            set => _targetType = value;
        }

        ///<inheritdoc/>
        public virtual string Id => "Set Value";

        ///<inheritdoc/>
        public virtual string ShortDataDescription => setValue.IsBound ? setValue.ToString().RT().Color(BindColors.Primary) : setValue.Value?.ToString();

        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => setValue.Value;
        ///<inheritdoc/>
        public BindMode ModifyMode => BindMode.Write;

        private void InitializeIfNeeded()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;
            
            setValue.UpdateOnEnable = _setOnEnable;

            setValue.ValueChanged -= SetValueChanged;
            setValue.ValueChanged += SetValueChanged;
        }

        public void OnValidate()
        {
            setValue.UpdateOnEnable = _setOnEnable;
        }

        private void SetValueChanged(T _, T newValue)
        {
            SetValue?.Invoke(this, newValue);
        }

        public IBind BindOwner
        {
            get => _owner;
            set
            {
                if (_owner == value)
                {
                    return;
                }

                _owner = value;
                InitializeIfNeeded();
            }
        }

        void ISmartModifier.SetSetValueCallback<S>(Action<ISmartModifier, S> setValue)
        {
            if (!typeof(T).IsAssignableFrom(typeof(S)))
            {
                return;
            }
            
            SetValue = (modifier, value) => setValue(modifier, value is S sValue ? sValue : default);
        }
        
        public Action<ISmartModifier, T> SetValue
        {
            get => _setValue;
            set
            {
                if (_setValue == value)
                {
                    return;
                }
                
                _setValue = value;
                InitializeIfNeeded();
            }
        }

        public bool ShouldAutoUpdate => true;

        public bool UpdateOnEnable
        {
            get => _setOnEnable;
            set => _setOnEnable = value;
        }

        public bool IsDynamic => setValue.IsBound;
    }

    #region [  Specialized Implementations  ]
    
    public static class SetValueModifiers
    {
        /// <summary>
        /// Registers all the specialized versions of SetValueModifier to ModifiersFactory.
        /// </summary>
        public static void RegisterAll()
        {
            ModifiersFactory.Register<SetValueFloatModifier>();
            ModifiersFactory.Register<SetValueIntModifier>();
            ModifiersFactory.Register<SetValueBoolModifier>();
            ModifiersFactory.Register<SetValueDoubleModifier>();
            ModifiersFactory.Register<SetValueLongModifier>();
            ModifiersFactory.Register<SetValueShortModifier>();
            ModifiersFactory.Register<SetValueByteModifier>();
            ModifiersFactory.Register<SetValueCharModifier>();
            ModifiersFactory.Register<SetValueDecimalModifier>();
            ModifiersFactory.Register<SetValueSbyteModifier>();
            ModifiersFactory.Register<SetValueUintModifier>();
            ModifiersFactory.Register<SetValueUlongModifier>();
            ModifiersFactory.Register<SetValueUshortModifier>();
            ModifiersFactory.Register<SetValueDateTimeModifier>();
            ModifiersFactory.Register<SetValueStringModifier>();
            ModifiersFactory.Register<SetValueVector2Modifier>();
            ModifiersFactory.Register<SetValueVector3Modifier>();
            ModifiersFactory.Register<SetValueVector4Modifier>();
            ModifiersFactory.Register<SetValueColorModifier>();
            ModifiersFactory.Register<SetValueQuaternionModifier>();
            ModifiersFactory.Register<SetValueRectModifier>();
            ModifiersFactory.Register<SetValueVector2IntModifier>();
            ModifiersFactory.Register<SetValueVector3IntModifier>();
            ModifiersFactory.Register<SetValueBoundsModifier>();
            ModifiersFactory.Register<SetValueBoundsIntModifier>();
            ModifiersFactory.Register<SetValueMatrix4x4Modifier>();
            ModifiersFactory.Register<SetValueObjectModifier>();
            ModifiersFactory.Register<SetValueUnityObjectModifier>();
            ModifiersFactory.Register<SetValueGradientModifier>();
            ModifiersFactory.Register<SetValueAnimationCurveModifier>();
            ModifiersFactory.Register<SetValueRectOffsetModifier>();
        }
    }
    
    [Serializable] public sealed class SetValueFloatModifier : SetValueModifier<float>{ }
    [Serializable] public sealed class SetValueIntModifier : SetValueModifier<int>{ }
    [Serializable] public sealed class SetValueBoolModifier : SetValueModifier<bool>{ }
    [Serializable] public sealed class SetValueDoubleModifier : SetValueModifier<double>{ }
    [Serializable] public sealed class SetValueLongModifier : SetValueModifier<long>{ }
    [Serializable] public sealed class SetValueShortModifier : SetValueModifier<short>{ }
    [Serializable] public sealed class SetValueByteModifier : SetValueModifier<byte>{ }
    [Serializable] public sealed class SetValueCharModifier : SetValueModifier<char>{ }
    [Serializable] public sealed class SetValueDecimalModifier : SetValueModifier<decimal>{ }
    [Serializable] public sealed class SetValueSbyteModifier : SetValueModifier<sbyte>{ }
    [Serializable] public sealed class SetValueUintModifier : SetValueModifier<uint>{ }
    [Serializable] public sealed class SetValueUlongModifier : SetValueModifier<ulong>{ }
    [Serializable] public sealed class SetValueUshortModifier : SetValueModifier<ushort>{ }
    [Serializable] public sealed class SetValueDateTimeModifier : SetValueModifier<DateTime>{ }
    [Serializable] public sealed class SetValueVector2Modifier : SetValueModifier<Vector2>{ }
    [Serializable] public sealed class SetValueVector3Modifier : SetValueModifier<Vector3>{ }
    [Serializable] public sealed class SetValueVector4Modifier : SetValueModifier<Vector4>{ }
    [Serializable] public sealed class SetValueColorModifier : SetValueModifier<Color>{ }
    [Serializable] public sealed class SetValueQuaternionModifier : SetValueModifier<Quaternion>{ }
    [Serializable] public sealed class SetValueRectModifier : SetValueModifier<Rect>{ }
    [Serializable] public sealed class SetValueVector2IntModifier : SetValueModifier<Vector2Int>{ }
    [Serializable] public sealed class SetValueVector3IntModifier : SetValueModifier<Vector3Int>{ }
    [Serializable] public sealed class SetValueBoundsModifier : SetValueModifier<Bounds>{ }
    [Serializable] public sealed class SetValueBoundsIntModifier : SetValueModifier<BoundsInt>{ }
    [Serializable] public sealed class SetValueMatrix4x4Modifier : SetValueModifier<Matrix4x4>{ }
    [Serializable] public sealed class SetValueStringModifier : SetObjectValueModifier<string>{ }
    [Serializable] public sealed class SetValueObjectModifier : SetObjectValueModifier<object>{ }
    [Serializable] public sealed class SetValueUnityObjectModifier : SetObjectValueModifier<Object>{ }
    // Other classes for Gradient, AnimationCurve and RectOffset
    [Serializable] public sealed class SetValueGradientModifier : SetObjectValueModifier<Gradient>{ }
    [Serializable] public sealed class SetValueAnimationCurveModifier : SetObjectValueModifier<AnimationCurve>{ }
    [Serializable] public sealed class SetValueRectOffsetModifier : SetObjectValueModifier<RectOffset>{ }
    
    #endregion
}
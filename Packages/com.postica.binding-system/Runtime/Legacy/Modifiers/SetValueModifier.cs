using System;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{

    /// <summary>
    /// A modifier which sets a value up the chain, disregarding the value it receives.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/set_value")]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal class SetValueModifier<T> : IWriteModifier<T>, ISmartValueModifier<T> where T : struct
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
    }
    
    /// <summary>
    /// A modifier which sets a value up the chain, disregarding the value it receives.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [ModifierOptions(BindMode.Write)]
    [TypeIcon("_bsicons/set_value")]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal class SetObjectValueModifier<T> : IObjectModifier<T>, ISmartModifier where T : class
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
    }

    #region [  Specialized Implementations  ]
    
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueFloatModifier : SetValueModifier<float>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueIntModifier : SetValueModifier<int>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueBoolModifier : SetValueModifier<bool>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueDoubleModifier : SetValueModifier<double>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueLongModifier : SetValueModifier<long>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueShortModifier : SetValueModifier<short>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueByteModifier : SetValueModifier<byte>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueCharModifier : SetValueModifier<char>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueDecimalModifier : SetValueModifier<decimal>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueSbyteModifier : SetValueModifier<sbyte>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueUintModifier : SetValueModifier<uint>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueUlongModifier : SetValueModifier<ulong>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueUshortModifier : SetValueModifier<ushort>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueDateTimeModifier : SetValueModifier<DateTime>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueVector2Modifier : SetValueModifier<Vector2>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueVector3Modifier : SetValueModifier<Vector3>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueVector4Modifier : SetValueModifier<Vector4>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueColorModifier : SetValueModifier<Color>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueQuaternionModifier : SetValueModifier<Quaternion>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueRectModifier : SetValueModifier<Rect>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueVector2IntModifier : SetValueModifier<Vector2Int>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueVector3IntModifier : SetValueModifier<Vector3Int>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueBoundsModifier : SetValueModifier<Bounds>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueBoundsIntModifier : SetValueModifier<BoundsInt>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueMatrix4x4Modifier : SetValueModifier<Matrix4x4>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueStringModifier : SetObjectValueModifier<string>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueObjectModifier : SetObjectValueModifier<object>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueUnityObjectModifier : SetObjectValueModifier<Object>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueGradientModifier : SetObjectValueModifier<Gradient>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueAnimationCurveModifier : SetObjectValueModifier<AnimationCurve>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")][Serializable] internal sealed class SetValueRectOffsetModifier : SetObjectValueModifier<RectOffset>{ }
    
    #endregion
}
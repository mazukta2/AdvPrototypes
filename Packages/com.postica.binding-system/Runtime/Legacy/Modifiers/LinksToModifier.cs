using System;
using System.Collections.Generic;
using Postica.BindingSystem.Accessors;
using Postica.Common;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{

    /// <summary>
    /// A modifier which links to another bindable value.
    /// This modifier allows to have multiple bindable values linked to a single value.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/link_color")]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal class LinksToModifier<T> : IReadWriteModifier<T>, ISmartValueModifier<T> where T : struct
    {
        [Tooltip("Links this bindable to another bindable value.")]
        public BindDataFor<T> linkTo;

        [NonSerialized] private bool _isInitialized;
        [NonSerialized] private bool _isUpdatingValue;
        [NonSerialized] private T _lastValue;

        private IBind _owner;
        private Action<ISmartModifier, T> _setValue;

        ///<inheritdoc/>
        public T ModifyRead(in T value)
        {
            if (linkTo.BindData.IsValid 
                && linkTo.BindData.Mode.CanWrite() 
                && linkTo.Accessor is IAccessor { CanWrite: true } 
                && !EqualityComparer<T>.Default.Equals(_lastValue, value))
            {
                _isUpdatingValue = true;
                linkTo.Value = value;
                _isUpdatingValue = false;
                _lastValue = value;
            }

            return value;
        }

        ///<inheritdoc/>
        public T ModifyWrite(in T value)
        {
            if (linkTo.BindData.IsValid && linkTo.BindData.Mode.CanWrite() && linkTo.Accessor is IAccessor { CanWrite: true })
            {
                _isUpdatingValue = true;
                linkTo.Value = value;
                _isUpdatingValue = false;
            }

            return value;
        }

        private void InitializeIfNeeded()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            linkTo.ValueChanged -= LinkToValueChanged;
            linkTo.ValueChanged += LinkToValueChanged;
        }

        private void LinkToValueChanged(T oldvalue, T newvalue)
        {
            if (_isUpdatingValue)
            {
                return;
            }

            if (!linkTo.BindData.IsValid || !linkTo.BindData.Mode.CanRead())
            {
                // This link cannot read from, so no need to update the value
                return;
            }

            SetValue?.Invoke(this, newvalue);
        }

        ///<inheritdoc/>
        public virtual string Id => "Link To Bindable";

        ///<inheritdoc/>
        public virtual string ShortDataDescription 
        {
            get
            {
                var source = linkTo.Source
                    ? $"[{linkTo.Source.GetType().UserFriendlyName()}]."
                    : "";
                var value = linkTo.BindData.IsValid ? source + linkTo.BindData.Path : "NOT SET";

                return linkTo.BindData.IsValid ? value : value.RT().Color(Color.red.Green(0.5f)).Bold();
            }
        }

        ///<inheritdoc/>
        public BindMode ModifyMode => BindMode.ReadWrite;

        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => ModifyMode == BindMode.Write ? ModifyWrite((T)value) : ModifyRead((T)value);

        ///<inheritdoc/>
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

        ///<inheritdoc/>
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
    /// A modifier which links to another bindable value.
    /// This modifier allows to have multiple bindable values linked to a single value.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [ModifierOptions(BindMode.ReadWrite)]
    [TypeIcon("_bsicons/link_color")]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal class LinksToObjectModifier<T> : IObjectModifier<T>, ISmartModifier where T : class
    {
        [Tooltip("Links this bindable to another bindable value.")]
        [BindTypeSource(nameof(TargetType))]
        public BindDataFor<T> linkTo;

        [NonSerialized] private bool _isInitialized;
        [NonSerialized] private bool _isUpdatingValue;
        [NonSerialized] private T _lastValue;

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
        public T ModifyRead(in T value)
        {
            if (linkTo.BindData.IsValid && linkTo.BindData.Mode.CanWrite() && linkTo.Accessor is IAccessor { CanWrite: true } &&
                !EqualityComparer<T>.Default.Equals(_lastValue, value))
            {
                _isUpdatingValue = true;
                linkTo.Value = value;
                _isUpdatingValue = false;
                _lastValue = value;
            }

            return value;
        }

        ///<inheritdoc/>
        public T ModifyWrite(in T value)
        {
            if (linkTo.BindData.IsValid && linkTo.BindData.Mode.CanWrite() && linkTo.Accessor is IAccessor { CanWrite: true })
            {
                _isUpdatingValue = true;
                linkTo.Value = value;
                _isUpdatingValue = false;
            }

            return value;
        }

        private void InitializeIfNeeded()
        {
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            linkTo.ValueChanged -= LinkToValueChanged;
            linkTo.ValueChanged += LinkToValueChanged;
        }

        private void LinkToValueChanged(T oldvalue, T newvalue)
        {
            if (_isUpdatingValue)
            {
                return;
            }
            
            if (!linkTo.BindData.IsValid || !linkTo.BindData.Mode.CanRead())
            {
                // This link cannot read from, so no need to update the value
                return;
            }

            SetValue?.Invoke(this, newvalue);
        }

        ///<inheritdoc/>
        public virtual string Id => "Link To Bindable";

        ///<inheritdoc/>
        public virtual string ShortDataDescription 
        {
            get
            {
                var source = linkTo.Source
                    ? $"[{linkTo.Source.GetType().UserFriendlyName()}]."
                    : "";
                var value = linkTo.BindData.IsValid ? source + linkTo.BindData.Path : "NOT SET";

                return linkTo.BindData.IsValid ? value : value.RT().Color(Color.red.Green(0.5f)).Bold();
            }
        }

        ///<inheritdoc/>
        public BindMode ModifyMode => BindMode.ReadWrite;

        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => ModifyMode == BindMode.Write ? ModifyWrite((T)value) : ModifyRead((T)value);

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
    
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToFloatModifier : LinksToModifier<float>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToIntModifier : LinksToModifier<int>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToBoolModifier : LinksToModifier<bool>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToDoubleModifier : LinksToModifier<double>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToLongModifier : LinksToModifier<long>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToShortModifier : LinksToModifier<short>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToByteModifier : LinksToModifier<byte>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToCharModifier : LinksToModifier<char>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToDecimalModifier : LinksToModifier<decimal>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToSbyteModifier : LinksToModifier<sbyte>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToUintModifier : LinksToModifier<uint>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToUlongModifier : LinksToModifier<ulong>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToUshortModifier : LinksToModifier<ushort>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToDateTimeModifier : LinksToModifier<DateTime>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToStringModifier : LinksToObjectModifier<string>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToVector2Modifier : LinksToModifier<Vector2>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToVector3Modifier : LinksToModifier<Vector3>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToVector4Modifier : LinksToModifier<Vector4>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToColorModifier : LinksToModifier<Color>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToQuaternionModifier : LinksToModifier<Quaternion>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToRectModifier : LinksToModifier<Rect>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToVector2IntModifier : LinksToModifier<Vector2Int>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToVector3IntModifier : LinksToModifier<Vector3Int>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToBoundsModifier : LinksToModifier<Bounds>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToBoundsIntModifier : LinksToModifier<BoundsInt>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToMatrix4x4Modifier : LinksToModifier<Matrix4x4>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToObjectModifier : LinksToObjectModifier<object>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToUnityObjectModifier : LinksToObjectModifier<Object>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToGradientModifier : LinksToObjectModifier<Gradient>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToAnimationCurveModifier : LinksToObjectModifier<AnimationCurve>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class LinksToRectOffsetModifier : LinksToObjectModifier<RectOffset>{ }
    
    #endregion
}
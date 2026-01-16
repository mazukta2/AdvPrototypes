using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Postica.BindingSystem.Accessors;
using Postica.Common;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Modifiers
{

    /// <summary>
    /// A modifier which links to another bindable value.
    /// This modifier allows to have multiple bindable values linked to a single value.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/link_color")]
    [TypeDescription("Links this bindable to another bindable value. \n" +
                     "This makes it possible to have multiple bindable values linked to a single value.")]
    public class LinksToModifier<T> : IReadWriteModifier<T>, ISmartValueModifier<T>, IDynamicComponent where T : struct
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
                && !Equals(_lastValue, value))
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

        public bool IsDynamic => true;
    }
    
    /// <summary>
    /// A modifier which links to another bindable value.
    /// This modifier allows to have multiple bindable values linked to a single value.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [ModifierOptions(BindMode.ReadWrite)]
    [TypeIcon("_bsicons/modifiers/link_color")]
    public class LinksToObjectModifier<T> : IObjectModifier<T>, ISmartModifier where T : class
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
                !Equals(_lastValue, value))
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);
    }

    #region [  Specialized Implementations  ]
    
    public static class LinkModifiers
    {
        /// <summary>
        /// Registers all the specialized versions of LinksToModifier to ModifiersFactory.
        /// </summary>
        public static void RegisterAll()
        {
            ModifiersFactory.Register<LinksToFloatModifier>();
            ModifiersFactory.Register<LinksToIntModifier>();
            ModifiersFactory.Register<LinksToBoolModifier>();
            ModifiersFactory.Register<LinksToDoubleModifier>();
            ModifiersFactory.Register<LinksToLongModifier>();
            ModifiersFactory.Register<LinksToShortModifier>();
            ModifiersFactory.Register<LinksToByteModifier>();
            ModifiersFactory.Register<LinksToCharModifier>();
            ModifiersFactory.Register<LinksToDecimalModifier>();
            ModifiersFactory.Register<LinksToSbyteModifier>();
            ModifiersFactory.Register<LinksToUintModifier>();
            ModifiersFactory.Register<LinksToUlongModifier>();
            ModifiersFactory.Register<LinksToUshortModifier>();
            ModifiersFactory.Register<LinksToDateTimeModifier>();
            ModifiersFactory.Register<LinksToVector2Modifier>();
            ModifiersFactory.Register<LinksToVector3Modifier>();
            ModifiersFactory.Register<LinksToVector4Modifier>();
            ModifiersFactory.Register<LinksToColorModifier>();
            ModifiersFactory.Register<LinksToQuaternionModifier>();
            ModifiersFactory.Register<LinksToRectModifier>();
            ModifiersFactory.Register<LinksToVector2IntModifier>();
            ModifiersFactory.Register<LinksToVector3IntModifier>();
            ModifiersFactory.Register<LinksToBoundsModifier>();
            ModifiersFactory.Register<LinksToBoundsIntModifier>();
            ModifiersFactory.Register<LinksToMatrix4x4Modifier>();
            
            ModifiersFactory.Register<LinksToStringModifier>();
            ModifiersFactory.Register<LinksToObjectModifier>();
            ModifiersFactory.Register<LinksToUnityObjectModifier>();
            ModifiersFactory.Register<LinksToGradientModifier>();
            ModifiersFactory.Register<LinksToAnimationCurveModifier>();
            ModifiersFactory.Register<LinksToRectOffsetModifier>();
        }
    }
    
    [Serializable] public sealed class LinksToFloatModifier : LinksToModifier<float>{ }
    [Serializable] public sealed class LinksToIntModifier : LinksToModifier<int>{ }
    [Serializable] public sealed class LinksToBoolModifier : LinksToModifier<bool>{ }
    [Serializable] public sealed class LinksToDoubleModifier : LinksToModifier<double>{ }
    [Serializable] public sealed class LinksToLongModifier : LinksToModifier<long>{ }
    [Serializable] public sealed class LinksToShortModifier : LinksToModifier<short>{ }
    [Serializable] public sealed class LinksToByteModifier : LinksToModifier<byte>{ }
    [Serializable] public sealed class LinksToCharModifier : LinksToModifier<char>{ }
    [Serializable] public sealed class LinksToDecimalModifier : LinksToModifier<decimal>{ }
    [Serializable] public sealed class LinksToSbyteModifier : LinksToModifier<sbyte>{ }
    [Serializable] public sealed class LinksToUintModifier : LinksToModifier<uint>{ }
    [Serializable] public sealed class LinksToUlongModifier : LinksToModifier<ulong>{ }
    [Serializable] public sealed class LinksToUshortModifier : LinksToModifier<ushort>{ }
    [Serializable] public sealed class LinksToDateTimeModifier : LinksToModifier<DateTime>{ }
    [Serializable] public sealed class LinksToStringModifier : LinksToObjectModifier<string>{ }
    [Serializable] public sealed class LinksToVector2Modifier : LinksToModifier<Vector2>{ }
    [Serializable] public sealed class LinksToVector3Modifier : LinksToModifier<Vector3>{ }
    [Serializable] public sealed class LinksToVector4Modifier : LinksToModifier<Vector4>{ }
    [Serializable] public sealed class LinksToColorModifier : LinksToModifier<Color>{ }
    [Serializable] public sealed class LinksToQuaternionModifier : LinksToModifier<Quaternion>{ }
    [Serializable] public sealed class LinksToRectModifier : LinksToModifier<Rect>{ }
    [Serializable] public sealed class LinksToVector2IntModifier : LinksToModifier<Vector2Int>{ }
    [Serializable] public sealed class LinksToVector3IntModifier : LinksToModifier<Vector3Int>{ }
    [Serializable] public sealed class LinksToBoundsModifier : LinksToModifier<Bounds>{ }
    [Serializable] public sealed class LinksToBoundsIntModifier : LinksToModifier<BoundsInt>{ }
    [Serializable] public sealed class LinksToMatrix4x4Modifier : LinksToModifier<Matrix4x4>{ }
    [Serializable] public sealed class LinksToObjectModifier : LinksToObjectModifier<object>{ }
    [Serializable] public sealed class LinksToUnityObjectModifier : LinksToObjectModifier<Object>{ }
    [Serializable] public sealed class LinksToGradientModifier : LinksToObjectModifier<Gradient>{ }
    [Serializable] public sealed class LinksToAnimationCurveModifier : LinksToObjectModifier<AnimationCurve>{ }
    [Serializable] public sealed class LinksToRectOffsetModifier : LinksToObjectModifier<RectOffset>{ }
    
    #endregion
}
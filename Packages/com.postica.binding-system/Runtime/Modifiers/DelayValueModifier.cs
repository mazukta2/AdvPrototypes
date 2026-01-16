using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Postica.Common;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Tweening
{
    /// <summary>
    /// A base modifier to create delay value modifiers.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/clock")]
    [TypeDescription("Delays the value by a specified amount of time.")]
    public class DelayValueModifier<T> : IReadModifier<T>, IDynamicComponent
    {
        [Tooltip("How much to delay the value (in seconds)")]
        [Bind]
        [Min(0)]
        public ReadOnlyBind<float> delay = 1f.Bind();
        [Tooltip("This value will be used until delay is over, after that the value will be from the delayed ones")]
        public ReadOnlyBind<T> initialValue;
        
        [NonSerialized]
        private Queue<(T value, float time)> _delayedValues = new();

        private bool _lastValueSet;
        private T _lastValue;
        
        ///<inheritdoc/>
        public virtual string Id { get; } = "Value Delayer";

        ///<inheritdoc/>
        public virtual string ShortDataDescription => string.Empty;

        ///<inheritdoc/>
        public BindMode ModifyMode => BindMode.Read;

        public T ModifyRead(in T value)
        {
            _delayedValues.Enqueue((value, Time.time + delay));

            if (_delayedValues.Peek().time <= Time.time)
            {
                _lastValue = _delayedValues.Dequeue().value;
            }
            else if (!_lastValueSet)
            {
                _lastValue = initialValue.Value;
                _lastValueSet = true;
            }
            return _lastValue;
        }
        
        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => ModifyRead((T)value);

        public bool IsDynamic => delay.IsBound || delay.UnboundValue > 0;
    }

    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/clock")]
    public class DelayInstanceValueModifier<T> : DelayValueModifier<T>, IObjectModifier<T> where T : class
    {
        [SerializeField]
        [HideInInspector]
        protected SerializedType _targetType;
        public virtual Type TargetType
        {
            get => _targetType?.Get() ?? typeof(T);
            set => _targetType = value;
        }
    }
    
    #region [  Specialized Implementations  ]
    
    public static class DelayValueModifiers
    {
        /// <summary>
        /// Registers all the specialized versions of DelayValueModifier to ModifiersFactory.
        /// </summary>
        public static void RegisterAll()
        {
            ModifiersFactory.Register<DelayValueFloatModifier>();
            ModifiersFactory.Register<DelayValueIntModifier>();
            ModifiersFactory.Register<DelayValueBoolModifier>();
            ModifiersFactory.Register<DelayValueDoubleModifier>();
            ModifiersFactory.Register<DelayValueLongModifier>();
            ModifiersFactory.Register<DelayValueShortModifier>();
            ModifiersFactory.Register<DelayValueByteModifier>();
            ModifiersFactory.Register<DelayValueCharModifier>();
            ModifiersFactory.Register<DelayValueDecimalModifier>();
            ModifiersFactory.Register<DelayValueSbyteModifier>();
            ModifiersFactory.Register<DelayValueUintModifier>();
            ModifiersFactory.Register<DelayValueUlongModifier>();
            ModifiersFactory.Register<DelayValueUshortModifier>();
            ModifiersFactory.Register<DelayValueDateTimeModifier>();
            ModifiersFactory.Register<DelayValueStringModifier>();
            ModifiersFactory.Register<DelayValueVector2Modifier>();
            ModifiersFactory.Register<DelayValueVector3Modifier>();
            ModifiersFactory.Register<DelayValueVector4Modifier>();
            ModifiersFactory.Register<DelayValueColorModifier>();
            ModifiersFactory.Register<DelayValueQuaternionModifier>();
            ModifiersFactory.Register<DelayValueRectModifier>();
            ModifiersFactory.Register<DelayValueVector2IntModifier>();
            ModifiersFactory.Register<DelayValueVector3IntModifier>();
            ModifiersFactory.Register<DelayValueBoundsModifier>();
            ModifiersFactory.Register<DelayValueBoundsIntModifier>();
            ModifiersFactory.Register<DelayValueMatrix4x4Modifier>();
            ModifiersFactory.Register<DelayValueObjectModifier>();
            ModifiersFactory.Register<DelayValueUnityObjectModifier>();
            ModifiersFactory.Register<DelayValueGradientModifier>();
            ModifiersFactory.Register<DelayValueAnimationCurveModifier>();
            ModifiersFactory.Register<DelayValueRectOffsetModifier>();
        }
    }
    
    [Serializable] public sealed class DelayValueFloatModifier : DelayValueModifier<float>{ }
    [Serializable] public sealed class DelayValueIntModifier : DelayValueModifier<int>{ }
    [Serializable] public sealed class DelayValueBoolModifier : DelayValueModifier<bool>{ }
    [Serializable] public sealed class DelayValueDoubleModifier : DelayValueModifier<double>{ }
    [Serializable] public sealed class DelayValueLongModifier : DelayValueModifier<long>{ }
    [Serializable] public sealed class DelayValueShortModifier : DelayValueModifier<short>{ }
    [Serializable] public sealed class DelayValueByteModifier : DelayValueModifier<byte>{ }
    [Serializable] public sealed class DelayValueCharModifier : DelayValueModifier<char>{ }
    [Serializable] public sealed class DelayValueDecimalModifier : DelayValueModifier<decimal>{ }
    [Serializable] public sealed class DelayValueSbyteModifier : DelayValueModifier<sbyte>{ }
    [Serializable] public sealed class DelayValueUintModifier : DelayValueModifier<uint>{ }
    [Serializable] public sealed class DelayValueUlongModifier : DelayValueModifier<ulong>{ }
    [Serializable] public sealed class DelayValueUshortModifier : DelayValueModifier<ushort>{ }
    [Serializable] public sealed class DelayValueDateTimeModifier : DelayValueModifier<DateTime>{ }
    [Serializable] public sealed class DelayValueVector2Modifier : DelayValueModifier<Vector2>{ }
    [Serializable] public sealed class DelayValueVector3Modifier : DelayValueModifier<Vector3>{ }
    [Serializable] public sealed class DelayValueVector4Modifier : DelayValueModifier<Vector4>{ }
    [Serializable] public sealed class DelayValueColorModifier : DelayValueModifier<Color>{ }
    [Serializable] public sealed class DelayValueQuaternionModifier : DelayValueModifier<Quaternion>{ }
    [Serializable] public sealed class DelayValueRectModifier : DelayValueModifier<Rect>{ }
    [Serializable] public sealed class DelayValueVector2IntModifier : DelayValueModifier<Vector2Int>{ }
    [Serializable] public sealed class DelayValueVector3IntModifier : DelayValueModifier<Vector3Int>{ }
    [Serializable] public sealed class DelayValueBoundsModifier : DelayValueModifier<Bounds>{ }
    [Serializable] public sealed class DelayValueBoundsIntModifier : DelayValueModifier<BoundsInt>{ }
    [Serializable] public sealed class DelayValueMatrix4x4Modifier : DelayValueModifier<Matrix4x4>{ }
    [Serializable] public sealed class DelayValueStringModifier : DelayInstanceValueModifier<string>{ }
    [Serializable] public sealed class DelayValueObjectModifier : DelayInstanceValueModifier<object>{ }
    [Serializable] public sealed class DelayValueUnityObjectModifier : DelayInstanceValueModifier<Object>{ }
    // Other classes for Gradient, AnimationCurve and RectOffset
    [Serializable] public sealed class DelayValueGradientModifier : DelayInstanceValueModifier<Gradient>{ }
    [Serializable] public sealed class DelayValueAnimationCurveModifier : DelayInstanceValueModifier<AnimationCurve>{ }
    [Serializable] public sealed class DelayValueRectOffsetModifier : DelayInstanceValueModifier<RectOffset>{ }
    
    #endregion
}
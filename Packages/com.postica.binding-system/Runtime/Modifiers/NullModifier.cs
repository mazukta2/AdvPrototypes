using System;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Modifiers
{

    /// <summary>
    /// A modifier which checks if the value is null and returns a fallback value if it is.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [ModifierOptions(BindMode.ReadWrite)]
    [TypeIcon("_bsicons/modifiers/null")]
    [TypeDescription("Checks if the value is null and returns a fallback value if it is.")]
    public class NullModifier<S> : IObjectModifier<S>, IDynamicComponent where S : class
    {
        [Tooltip("This value will be passed along if the original is null.")]
        [BindTypeSource(nameof(TargetType))]
        public ReadOnlyBind<S> fallbackValue;

        [SerializeField]
        [HideInInspector]
        private BindMode _mode;
        
        [SerializeField]
        [HideInInspector]
        protected SerializedType _targetType;
        public virtual Type TargetType
        {
            get => _targetType?.Get() ?? typeof(S);
            set => _targetType = value;
        }
        
        public virtual object Modify(BindMode mode, object value)
        {
            return value ?? fallbackValue.Value;
        }

        ///<inheritdoc/>
        public virtual string Id => "Null Check Modifier";

        ///<inheritdoc/>
        public string ShortDataDescription => fallbackValue.IsBound ? fallbackValue.ToString().RT().Color(BindColors.Primary) : fallbackValue.Value?.ToString();

        ///<inheritdoc/>
        public BindMode ModifyMode
        {
            get => _mode;
            set => _mode = value;
        }

        public bool IsDynamic => fallbackValue.IsBound;
    }

    #region [  Specialized Implementations  ]
    
    public static class NullModifiers
    {
        /// <summary>
        /// Registers all the specialized versions of NullModifier to ModifiersFactory.
        /// </summary>
        public static void RegisterAll()
        {
            ModifiersFactory.Register<NullStringModifier>();
            ModifiersFactory.Register<NullModifier>();
            ModifiersFactory.Register<NullUnityObjectModifier>();
            ModifiersFactory.Register<NullGradientModifier>();
            ModifiersFactory.Register<NullAnimationCurveModifier>();
            ModifiersFactory.Register<NullRectOffsetModifier>();
        }
    }

    [Serializable]
    public sealed class NullStringModifier : NullModifier<string>
    {
        public override string Id => "Null Or Empty Check Modifier";

        public override object Modify(BindMode mode, object value)
        {
            return value is string s && !string.IsNullOrEmpty(s) ? s : fallbackValue.Value;
        }
    }
    
    [Serializable] public sealed class NullModifier : NullModifier<object>{ }
    [Serializable] public sealed class NullUnityObjectModifier : NullModifier<Object>{ }
    [Serializable] public sealed class NullGradientModifier : NullModifier<Gradient>{ }
    [Serializable] public sealed class NullAnimationCurveModifier : NullModifier<AnimationCurve>{ }
    [Serializable] public sealed class NullRectOffsetModifier : NullModifier<RectOffset>{ }
    
    
    #endregion
}
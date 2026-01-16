using System;
using Postica.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{

    /// <summary>
    /// A modifier which checks if the value is null and returns a fallback value if it is.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [ModifierOptions(BindMode.ReadWrite)]
    [TypeIcon("_bsicons/null")]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal class NullModifier<S> : IObjectModifier<S> where S : class
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
    }

    #region [  Specialized Implementations  ]

    [Serializable]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class NullStringModifier : NullModifier<string>
    {
        public override string Id => "Null Or Empty Check Modifier";

        public override object Modify(BindMode mode, object value)
        {
            return value is string s && !string.IsNullOrEmpty(s) ? s : fallbackValue.Value;
        }
    }
    
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class NullModifier : NullModifier<object>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class NullUnityObjectModifier : NullModifier<Object>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class NullGradientModifier : NullModifier<Gradient>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class NullAnimationCurveModifier : NullModifier<AnimationCurve>{ }
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    [Serializable] internal sealed class NullRectOffsetModifier : NullModifier<RectOffset>{ }
    
    
    #endregion
}
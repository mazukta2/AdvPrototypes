using System;
using Postica.Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Tweening
{
    public enum PIDValueType
    {
        
        [InspectorName("Value \u2192 Set Point in Closed Loop")]
        ClosedLoop,
        [InspectorName("Value \u2192 Set Point (SP)")]
        AsSetPoint,
        [InspectorName("Value \u2192 Process Variable (PV)")]
        AsProcessVariable,
    }
    
    internal static class PIDModifier
    {
        internal const float MAX_P_COEFFICIENT = 2f;
        internal const float MAX_I_COEFFICIENT = 1f;
        internal const float MAX_D_COEFFICIENT = 0.2f;
        
        public static void RegisterKnownControllers()
        {
            ModifiersFactory.Register<FloatPIDModifier>();
            ModifiersFactory.Register<DoublePIDModifier>();
            ModifiersFactory.Register<IntPIDModifier>();
            ModifiersFactory.Register<LongPIDModifier>();
            ModifiersFactory.Register<ShortPIDModifier>();
            ModifiersFactory.Register<Vector3PIDModifier>();
            ModifiersFactory.Register<Vector2PIDModifier>();
            ModifiersFactory.Register<Vector4PIDModifier>();
        }
    }
    
    /// <summary>
    /// A base modifier to create PID (Proportional - Integral - Derivative) Controllers.
    /// </summary>
    [Serializable]
    [HideMember]
    // [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/motion")]
    public abstract class PIDModifier<T> : IReadModifier<T> where T : IEquatable<T>
    {
        [Tooltip("The PID (Proportional, Integral, Derivative) controller settings.")]
        public Data P_I_D;

        private bool _initialized;
        private T _lastError;
        private T _integral;
        private T _output;
        
        ///<inheritdoc/>
        public virtual string Id { get; } = "PID Controller";

        ///<inheritdoc/>
        public virtual string ShortDataDescription => string.Empty;

        ///<inheritdoc/>
        public BindMode ModifyMode => BindMode.Read;

        protected abstract T Add(T a, T b);
        protected abstract T Subtract(T a, T b);
        protected abstract T Multiply(T a, float b);
        protected abstract T Divide(T a, float b);
        protected abstract T Clamp(T value, T min, T max);
        protected abstract T Normalize(T value, T min, T max);

        private T Compute(T processVariable, T setPoint, float dt)
        {
            var error = Subtract(setPoint, processVariable);
            var der = Divide(Subtract(error, _lastError), dt);
            _integral = Add(_integral, Multiply(error, dt));
            _lastError = error;
            var proportional = Multiply(error, P_I_D.Kp);
            var integral = Multiply(_integral, P_I_D.Ki);
            var derivative = Multiply(der, P_I_D.Kd);
            var result = Add(proportional, Add(integral, derivative));
            if (P_I_D.useLimits)
            {
                result = Clamp(result, P_I_D.min, P_I_D.max);
            }
            return result;
        }
        
        public T ModifyRead(in T value)
        {
            if (!_initialized)
            {
                _output = value;
                _initialized = true;
                return value;
            }
            
            var dt = P_I_D.unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var (processVariable, setPoint) = P_I_D.valueType switch
            {
                PIDValueType.ClosedLoop => (_output, value),
                PIDValueType.AsProcessVariable => (value, P_I_D.otherVariable),
                PIDValueType.AsSetPoint => (P_I_D.otherVariable, value),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            _output = Add(_output, Compute(processVariable, setPoint, dt));
            
            return _output;
        }
        
        ///<inheritdoc/>
        public object Modify(BindMode mode, object value) => ModifyRead((T)value);

        [Serializable]
        public class Data
        {
            [Tooltip("Proportional constant (counters current error)")]
            [Bind]
            [Range(-PIDModifier.MAX_P_COEFFICIENT, PIDModifier.MAX_P_COEFFICIENT)]
            public ReadOnlyBind<float> Kp = 0.2f.Bind();
            [Tooltip("Integral constant (counters cumulated error)")]
            [Bind]
            [Range(-PIDModifier.MAX_I_COEFFICIENT, PIDModifier.MAX_I_COEFFICIENT)]
            public ReadOnlyBind<float> Ki = 0.05f.Bind();
            [Tooltip("Derivative constant (fights oscillation)")]
            [Bind]
            [Range(-PIDModifier.MAX_D_COEFFICIENT, PIDModifier.MAX_D_COEFFICIENT)]
            public ReadOnlyBind<float> Kd = 1f.Bind();
            [Tooltip("If true, the passed value to modifier will act as input, otherwise it will act as desired target value.")]
            public PIDValueType valueType = PIDValueType.ClosedLoop;
            [Tooltip("The Set Point (SP) AKA desired target output of the PID controller.")]
            public ReadOnlyBind<T> otherVariable;

            [Tooltip("If true, the unscaled time will be used for the PID controller.")]
            public bool unscaledTime;
            [Tooltip("Whether to limit the output or not.")]
            public ReadOnlyBind<bool> useLimits;
            [Tooltip("The minimum value of the output.")]
            public ReadOnlyBind<T> min;
            [Tooltip("The maximum value of the output.")]
            public ReadOnlyBind<T> max;
        }
    }
}
using System;
using Postica.BindingSystem.Utility;
using Postica.Common;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// Returns the evaluation of a boolean expression where input acts as one of the variables.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/function")]
    [TypeDescription("Evaluates a boolean expression where the input value acts as one of the variables.")]
    public sealed class BoolExpressionModifier : IReadModifier<bool>, IDynamicComponent
    {
        public BoolExpressionValue expression = new("Expression", "x") { expression = "x and true" };

        private bool? _isDynamic;
        
        public bool ModifyRead(in bool value) => expression.Evaluate(value);

        ///<inheritdoc/>
        public string Id => "Bool Expression";

        ///<inheritdoc/>
        public string ShortDataDescription => expression.expression;

        public object Modify(BindMode mode, object value)
        {
            if (value is bool b && mode.CanRead())
            {
                return ModifyRead(b);
            }

            return false;
        }

        public BindMode ModifyMode => BindMode.Read;

        public bool IsDynamic
        {
            get
            {
                _isDynamic ??= expression.HasDynamicVariables;
                return _isDynamic.Value;
            }
        }
    }
}
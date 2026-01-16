using System;
using Postica.BindingSystem.Utility;
using Postica.Common;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// Returns the evaluation of a math expression where input acts as one of the variables.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeIcon("_bsicons/modifiers/function")]
    [TypeDescription("Evaluates a math expression where the input value acts as one of the variables.")]
    public sealed class MathExpressionModifier : NumericModifier, IDynamicComponent
    {
        public MathExpressionValue expression = new("Expression", "x") { expression = "x + 1" };

        ///<inheritdoc/>
        public override string Id => "Math Expression";

        ///<inheritdoc/>
        public override string ShortDataDescription => expression.expression;

        protected override double Modify(double value) => expression.EvaluateFast(value);
        protected override long Modify(long value) => (long)expression.EvaluateFast(value);

        public bool IsDynamic => expression.HasDynamicVariables;
    }
}
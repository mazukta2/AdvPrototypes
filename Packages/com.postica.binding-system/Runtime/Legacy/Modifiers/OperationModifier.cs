using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Performs math operations on input numeric value.
    /// </summary>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class OperationModifier : NumericModifier
    {
        public enum OperatorType
        {
            Add,
            Subtract,
            Multiply,
            Divide,
            Modulus,
            Power,
            Absolute,
        }

        [SerializeField]
        private OperatorType _operator;
        [SerializeField]
        private ReadOnlyBind<float> _operand = 0f.Bind();

        ///<inheritdoc/>
        public override string ShortDataDescription
        {
            get
            {
                string operandA = VarFormat("x");
                string operandB = _operand.BindData.HasValue ? VarFormat("y") : _operand.Value.ToString();
                switch (_operator)
                {
                    case OperatorType.Add: return $"({operandA} + {operandB})";
                    case OperatorType.Subtract: return $"({operandA} - {operandB})";
                    case OperatorType.Multiply: return $"({operandA} * {operandB})";
                    case OperatorType.Divide: return $"({operandA} / {operandB})";
                    case OperatorType.Modulus: return $"({operandA} % {operandB})";
                    case OperatorType.Power: return $"({operandA} ^ {operandB})";
                    case OperatorType.Absolute: return $"(|{operandA}|)";
                }

                return "[null]".RT().Bold().Color(BindColors.Error);
            }
        }

        protected override double Modify(double value)
        {
            switch (_operator)
            {
                case OperatorType.Add: return value + _operand;
                case OperatorType.Subtract: return value - _operand;
                case OperatorType.Multiply: return value * _operand;
                case OperatorType.Divide: return value / _operand;
                case OperatorType.Modulus: return value % _operand;
                case OperatorType.Power: return _operand.Value == 2 ? value * value : Math.Pow(value, _operand);
                case OperatorType.Absolute: return value < 0 ? -value : value;
            }
            return value;
        }
    }
}
using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Performs a logic operation change on input boolean value.
    /// </summary>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class LogicOperationModifier : BaseModifier<bool>
    {
        public enum OperatorType
        {
            And,
            Or,
            Xor,
            Not,
        }

        [SerializeField]
        private OperatorType _operator;
        [SerializeField]
        [ReadOnlyBind]
        private Bind<bool> _operand;

        public OperatorType Operator { get => _operator; set => _operator = value; }
        public bool Operand { get => _operand; set => _operand.Value = value; }

        ///<inheritdoc/>
        public override string Id => "Logic Operation";

        ///<inheritdoc/>
        public override string ShortDataDescription
        {
            get
            {
                string operandA = VarFormat("x");
                string operandB = _operand.BindData.HasValue ? VarFormat("y") : _operand.Value.ToString();
                
                switch (_operator)
                {
                    case OperatorType.And: return $"({operandA} AND {operandB})";
                    case OperatorType.Or: return $"({operandA} OR {operandB})";
                    case OperatorType.Xor: return $"({operandA} XOR {operandB})";
                    case OperatorType.Not: return $"(NOT {operandA})";
                }

                return "[undefined]";
            }
        }

        protected override bool Modify(bool value)
        {
            switch (_operator)
            {
                case OperatorType.And: return value && _operand.Value;
                case OperatorType.Or: return value || _operand.Value;
                case OperatorType.Xor: return value ^ _operand.Value;
                case OperatorType.Not: return !value;
                default: return value;
            }
        }

        protected override bool InverseModify(bool value)
        {
            switch (_operator)
            {
                case OperatorType.And: return value && _operand.Value;
                case OperatorType.Or: return value || _operand.Value;
                case OperatorType.Xor: return value ^ _operand.Value;
                case OperatorType.Not: return !value;
                default: return value;
            }
        }
    }
}
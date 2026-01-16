using System;
using Postica.BindingSystem.Utility;
using Postica.Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// Returns the evaluation of a math expression where input acts as a Vector4.
    /// </summary>
    [Serializable]
    [HideMember]
    [TypeIcon("_bsicons/modifiers/function")]
    [TypeDescription("Evaluates a math expression where the input vector acts as the variables.")]
    public sealed class Vector2MathExpressionModifier : IReadWriteModifier<Vector2>, IDynamicComponent
    {
        public MathExpressionValue expressionX = new("<color=#FF3A3A><b>X</b></color>", "x", "y") { expression = "x" };
        public MathExpressionValue expressionY = new("<color=green><b>Y</b></color>", "x", "y") { expression = "y" };

        private readonly double[] _inputs = new double[2];
        
        private bool? _isDynamic;
        
        public string Id  => "Vector Expression";
        public string ShortDataDescription => " (" + expressionX.expression.RT().Bold().Color("#FF3A3A")+ ", " 
                                            + expressionY.expression.RT().Bold().Color(Color.green) + ")";
        
        private double[] GetInputs(Vector2 input)
        {
            _inputs[0] = input.x;
            _inputs[1] = input.y;
            
            return _inputs;
        }


        public Vector2 ModifyRead(in Vector2 value)
        {
            var output = value;
            output.x = (float)expressionX.EvaluateFast(GetInputs(value));
            output.y = (float)expressionY.EvaluateFast(GetInputs(value));
            return output;
        }

        public Vector2 ModifyWrite(in Vector2 value)
        {
            // TODO: To be developed when Expression has a way to invert the function
            return value;
        }
        
        public object Modify(BindMode mode, object value)
        {
            if(value is Vector2 vector)
            {
                return mode switch
                {
                    BindMode.Read => ModifyRead(vector),
                    BindMode.Write => ModifyWrite(vector),
                    _ => value
                };
            }
            return value;
        }

        public BindMode ModifyMode => BindMode.ReadWrite;

        public bool IsDynamic => _isDynamic ??= (expressionX.HasDynamicVariables || expressionY.HasDynamicVariables);
    }
    
    /// <summary>
    /// Returns the evaluation of a math expression where input acts as a Vector4.
    /// </summary>
    [Serializable]
    [HideMember]
    [TypeIcon("_bsicons/modifiers/function")]
    [TypeDescription("Evaluates a math expression where the input vector acts as the variables.")]
    public sealed class Vector3MathExpressionModifier : IReadWriteModifier<Vector3>
    {
        public MathExpressionValue expressionX = new("<color=#FF3A3A><b>X</b></color>", "x", "y", "z") { expression = "x" };
        public MathExpressionValue expressionY = new("<color=green><b>Y</b></color>", "x", "y", "z") { expression = "y" };
        public MathExpressionValue expressionZ = new("<color=#00AAFF><b>Z</b></color>", "x", "y", "z") { expression = "z" };

        private readonly double[] _inputs = new double[3];
        
        public string Id  => "Vector Expression";
        public string ShortDataDescription => "(" + expressionX.expression.RT().Bold().Color("#FF3A3A")+ ", " 
                                            + expressionY.expression.RT().Bold().Color(Color.green) + ", " 
                                            + expressionZ.expression.RT().Bold().Color("#00AAFF") + ")";
        
        private double[] GetInputs(Vector3 input)
        {
            _inputs[0] = input.x;
            _inputs[1] = input.y;
            _inputs[2] = input.z;
            
            return _inputs;
        }


        public Vector3 ModifyRead(in Vector3 value)
        {
            var output = value;
            output.x = (float)expressionX.EvaluateFast(GetInputs(value));
            output.y = (float)expressionY.EvaluateFast(GetInputs(value));
            output.z = (float)expressionZ.EvaluateFast(GetInputs(value));
            return output;
        }

        public Vector3 ModifyWrite(in Vector3 value)
        {
            // TODO: To be developed when Expression has a way to invert the function
            return value;
        }
        
        public object Modify(BindMode mode, object value)
        {
            if(value is Vector3 vector)
            {
                return mode switch
                {
                    BindMode.Read => ModifyRead(vector),
                    BindMode.Write => ModifyWrite(vector),
                    _ => value
                };
            }
            return value;
        }

        public BindMode ModifyMode => BindMode.ReadWrite;
    }
    
    /// <summary>
    /// Returns the evaluation of a math expression where input acts as a Vector4.
    /// </summary>
    [Serializable]
    [HideMember]
    [TypeIcon("_bsicons/modifiers/function")]
    [TypeDescription("Evaluates a math expression where the input vector acts as the variables.")]
    public sealed class Vector4MathExpressionModifier : IReadWriteModifier<Vector4>
    {
        public MathExpressionValue expressionX = new("<color=#FF3A3A><b>X</b></color>", "x", "y", "z", "w") { expression = "x" };
        public MathExpressionValue expressionY = new("<color=green><b>Y</b></color>", "x", "y", "z", "w") { expression = "y" };
        public MathExpressionValue expressionZ = new("<color=#00AAFF><b>Z</b></color>", "x", "y", "z", "w") { expression = "z" };
        public MathExpressionValue expressionW = new("<b>W</b>", "x", "y", "z", "w") { expression = "w" };

        private readonly double[] _inputs = new double[4];
        
        public string Id  => "Vector Expression";
        public string ShortDataDescription => "(" + expressionX.expression.RT().Bold().Color("#FF3A3A")+ ", " 
                                            + expressionY.expression.RT().Bold().Color(Color.green) + ", " 
                                            + expressionZ.expression.RT().Bold().Color("#00AAFF") + ", " 
                                            + expressionW.expression.RT().Bold() + ")";
        
        private double[] GetInputs(Vector4 input)
        {
            _inputs[0] = input.x;
            _inputs[1] = input.y;
            _inputs[2] = input.z;
            _inputs[3] = input.w;
            
            return _inputs;
        }


        public Vector4 ModifyRead(in Vector4 value)
        {
            var output = value;
            output.x = (float)expressionX.EvaluateFast(GetInputs(value));
            output.z = (float)expressionZ.EvaluateFast(GetInputs(value));
            output.w = (float)expressionW.EvaluateFast(GetInputs(value));
            output.y = (float)expressionY.EvaluateFast(GetInputs(value));
            return output;
        }

        public Vector4 ModifyWrite(in Vector4 value)
        {
            // TODO: To be developed when Expression has a way to invert the function
            return value;
        }
        
        public object Modify(BindMode mode, object value)
        {
            if(value is Vector4 vector)
            {
                return mode switch
                {
                    BindMode.Read => ModifyRead(vector),
                    BindMode.Write => ModifyWrite(vector),
                    _ => value
                };
            }
            return value;
        }

        public BindMode ModifyMode => BindMode.ReadWrite;
    }
}
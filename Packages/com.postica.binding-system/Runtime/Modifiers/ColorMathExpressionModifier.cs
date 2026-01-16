using System;
using Postica.BindingSystem.Utility;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// Returns the evaluation of a math expression where input acts as a set of 4 channels of the color.
    /// </summary>
    [Serializable]
    [HideMember]
    [TypeIcon("_bsicons/modifiers/function")]
    [TypeDescription("Evaluates a math expression where input value acts as a set of 4 channels of the color.")]
    public sealed class ColorMathExpressionModifier : IReadWriteModifier<Color>, IDynamicComponent
    {
        public MathExpressionValue expressionRed = new("<color=#FF3A3A><b>red</b></color>", "r", "g", "b", "a") { expression = "r" };
        public MathExpressionValue expressionGreen = new("<color=green><b>green</b></color>", "r", "g", "b", "a") { expression = "g" };
        public MathExpressionValue expressionBlue = new("<color=#00AAFF><b>blue</b></color>", "r", "g", "b", "a") { expression = "b" };
        public MathExpressionValue expressionAlpha = new("<b>alpha</b>", "r", "g", "b", "a") { expression = "a" };

        private readonly double[] _inputs = new double[4];
        
        private bool? _isDynamic;
        
        public string Id  => "Color Expression";
        public string ShortDataDescription => " " + expressionRed.expression.RT().Bold().Color("#FF3A3A")+ " | ".RT().Color(Color.gray) 
                                            + expressionGreen.expression.RT().Bold().Color(Color.green) + " | ".RT().Color(Color.gray)
                                            + expressionBlue.expression.RT().Bold().Color("#00AAFF") + " | ".RT().Color(Color.gray)
                                            + expressionAlpha.expression.RT().Bold().Color(Color.white);
        
        public bool IsDynamic
        {
            get
            {
                _isDynamic ??= expressionRed.HasDynamicVariables || expressionGreen.HasDynamicVariables ||
                              expressionBlue.HasDynamicVariables || expressionAlpha.HasDynamicVariables;
                return _isDynamic.Value;
            }
        }
        
        private double[] GetInputs(Color color)
        {
            _inputs[0] = color.r;
            _inputs[1] = color.g;
            _inputs[2] = color.b;
            _inputs[3] = color.a;
            
            return _inputs;
        }


        public Color ModifyRead(in Color value)
        {
            var output = value;
            output.r = (float)expressionRed.EvaluateFast(GetInputs(value));
            output.g = (float)expressionGreen.EvaluateFast(GetInputs(value));
            output.b = (float)expressionBlue.EvaluateFast(GetInputs(value));
            output.a = (float)expressionAlpha.EvaluateFast(GetInputs(value));
            return output;
        }

        public Color ModifyWrite(in Color value)
        {
            // TODO: To be developed when Expression has a way to invert the function
            return value;
        }
        
        public object Modify(BindMode mode, object value)
        {
            if(value is Color color)
            {
                return mode switch
                {
                    BindMode.Read => ModifyRead(color),
                    BindMode.Write => ModifyWrite(color),
                    _ => value
                };
            }
            return value;
        }

        public BindMode ModifyMode => BindMode.ReadWrite;
    }
}
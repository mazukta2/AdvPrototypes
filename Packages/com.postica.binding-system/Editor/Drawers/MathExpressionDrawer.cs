using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Postica.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ColorUtility = UnityEngine.ColorUtility;

namespace Postica.BindingSystem.Utility
{
    [CustomPropertyDrawer(typeof(MathExpressionValue), true)]
    class MathExpressionDrawer : PropertyDrawer
    {
        private const string kConstantColor = "#11FF11";
        private const string kSystemVariableColor = "#11FFAA"; // aqua green
        private const string kFunctionColor = "#FFF555";
        private const string kVariableColor = "#FFA500"; // orange
        private const string kInputValuesColor = "#7fdbef"; // orange
        private const string kNumberColor = "yellow";
        private const string kErrorColor = "red";
        
        private static readonly string kExpressionInfo = @$"-----------------------------------------
Use the following to build an expression:
- <color={kConstantColor}>Constants</color> are predefined values like <color={kConstantColor}><b>pi</b></color> or <color={kConstantColor}><b>e</b></color>.
- <color={kSystemVariableColor}>System Variables</color> are values provided by the system to be used at runtime. Use one of the following:
     <color={kSystemVariableColor}><b>t</b> - time, <b>dt</b> - delta time, <b>fdt</b> - fixed delta time
     <b>frame</b> - frame count, <b>fps</b> - frames per second, 
     <b>rand</b> - random number</color>
- <color={kFunctionColor}>Functions</color> are predefined operations, use one of the following:
     <color={kFunctionColor}><b>sin</b>, <b>cos</b>, <b>tan</b>, <b>asin</b>, <b>acos</b>, <b>atan</b>, <b>sqrt</b>, 
     <b>abs</b>, <b>log</b>, <b>mod</b>, <b>clamp</b>, <b>max</b>, <b>min</b>, <b>lerp</b>, <b>round</b>, 
     <b>floor</b>, <b>ceil</b>, <b>pow</b>, <b>exp</b>, <b>sign</b>, <b>rand</b></color>
- <color={kVariableColor}>Variables</color> are values that can change at runtime.";
        
        private class Elements
        {
            public EnhancedFoldout foldout;
            public TextField expressionField;
            public Image expressionInfo;
            public Label expressionLabel;
            public VisualElement variablesContainer;
            public List<string> inputVariables;
            public bool isSteady;
        }
        
        private Dictionary<string, Elements> elements = new();
        private string _finalTooltip;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if(!elements.TryGetValue(property.propertyPath, out var element))
            {
                element = new Elements();
                elements[property.propertyPath] = element;
            }
            
            var labelProperty = property.FindPropertyRelative("_label");
            var label = labelProperty.stringValue;
            
            element.foldout = new EnhancedFoldout().MakeAsField();
            element.foldout.text = string.IsNullOrEmpty(label) ? property.displayName : label;
            
            var expressionField = CreateExpressionField(element, property);
            element.foldout.restOfHeader.Add(expressionField);
            
            element.expressionInfo = new Image()
            {
                tooltip = GetTooltip(property),
            }.WithClass("math-expression__info");
            
            element.foldout.startOfHeader.Add(element.expressionInfo);
            
            // Add this class to avoid adding the replacement zones
            element.foldout.AddToClassList("ignore-target-replacement");
            element.foldout.viewDataKey = property.propertyPath;
            
            element.foldout.RegisterCallback<DetachFromPanelEvent>(_ => element.isSteady = false);
            
            // foldout.bindingPath = property.propertyPath;
            
            return element.foldout;
        }

        private string GetTooltip(SerializedProperty property)
        {
            if (!string.IsNullOrEmpty(_finalTooltip)) return _finalTooltip;
            
            var inputVariables = property.FindPropertyRelative("_inputVariables");
            if(inputVariables.arraySize == 0)
            {
                _finalTooltip = kExpressionInfo;
                return _finalTooltip;
            }
            
            var finalTooltip = new StringBuilder();
            finalTooltip.Append($"<size=14><color={kInputValuesColor}><b>Input variables</b></color> available: ");

            if (inputVariables.arraySize > 1)
            {
                for (int i = 0; i < inputVariables.arraySize - 1; i++)
                {
                    finalTooltip.Append($"<color={kInputValuesColor}><b>")
                        .Append(inputVariables.GetArrayElementAtIndex(i).stringValue)
                        .Append("</b></color>, ");
                }

                finalTooltip.Length -= 2;
                finalTooltip.Append($" and <color={kInputValuesColor}><b>")
                        .Append(inputVariables.GetArrayElementAtIndex(inputVariables.arraySize - 1).stringValue)
                        .Append("</b></color>.");
            }
            else
            {
                finalTooltip.Append($"<color={kInputValuesColor}><b>")
                    .Append(inputVariables.GetArrayElementAtIndex(inputVariables.arraySize - 1).stringValue)
                    .Append("</b></color>.");
            }

            finalTooltip.Append("</size>\n\n").Append(kExpressionInfo);
            _finalTooltip = finalTooltip.ToString();

            return _finalTooltip;
        }

        private VisualElement CreateExpressionField(Elements element, SerializedProperty property)
        {
            var inputVariables = property.FindPropertyRelative("_inputVariables");
            element.inputVariables = new List<string>();
            for (int i = 0; i < inputVariables.arraySize; i++)
            {
                element.inputVariables.Add(inputVariables.GetArrayElementAtIndex(i).stringValue);
            }
            
            var expressionProp = property.FindPropertyRelative(nameof(MathExpressionValue.expression));
            element.expressionField = new TextField()
            {
                value = expressionProp.stringValue,
            }.WithClass("math-expression__field");

            element.expressionLabel = new Label()
            {
                enableRichText = true, 
                text = DecoratedText(element, expressionProp.stringValue, out var isExpressionValid),
                pickingMode = PickingMode.Ignore,
            };
            element.expressionLabel.AddToClassList("math-expression__label");
            
            element.foldout.EnableInClassList("math-expression--error", !isExpressionValid);
            
            var labelParent = new VisualElement(){pickingMode = PickingMode.Ignore}
                .WithClass("math-expression__label-parent")
                .WithChildren(element.expressionLabel);
            
            var textElement = element.expressionField.Q<TextElement>();
            
            element.expressionLabel.ApplyTranslation(textElement);

            element.expressionField.OnAttachToPanel(f =>
            {
                element.foldout.restOfHeader.Add(labelParent);
                element.expressionLabel.ApplyTranslation(textElement);
                UpdateVariables(element.expressionField.value, property);
            });

            element.expressionField.RegisterValueChangedCallback(evt =>
            {
                expressionProp.stringValue = evt.newValue;
                expressionProp.serializedObject.ApplyModifiedProperties();
                element.expressionLabel.text = DecoratedText(element, evt.newValue, out isExpressionValid);
                element.expressionLabel.ApplyTranslation(textElement);
                element.foldout.EnableInClassList("math-expression--error", !isExpressionValid);

                // element.foldout.schedule.Execute(() => UpdateVariables(evt.newValue, property)).ExecuteLater(2000);
            });
            
            element.expressionField.RegisterCallback<KeyUpEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return)
                {
                    UpdateVariables(element.expressionField.value, property);
                }
            });
            
            element.expressionField.RegisterCallback<BlurEvent>(evt =>
            {
                UpdateVariables(element.expressionField.value, property);
            });
            
            textElement.schedule.Execute(() =>
            {
                element.expressionLabel.ApplyTranslation(textElement);
            }).Every(20);
            
            return element.expressionField;
        }

        private void UpdateVariables(string expression, SerializedProperty property)
        {
            if(!elements.TryGetValue(property.propertyPath, out var element))
            {
                return;
            }
            
            var variablesProperty = property.FindPropertyRelative(nameof(MathExpressionValue.variables));
            
            if (string.IsNullOrEmpty(expression))
            {
                variablesProperty.ClearArray();
                variablesProperty.serializedObject.ApplyModifiedProperties();
                element.foldout.Clear();
                return;
            }
            
            try
            {
                var parser = new Parser(expression);
                parser.Parse(out var variables);
                foreach (var inputVar in element.inputVariables)
                {
                    variables.Remove(inputVar);
                    variables.Remove(inputVar.ToUpper());
                    variables.Remove(inputVar.ToLower());
                }
                variables.RemoveAll(v => v.Equals("input", StringComparison.OrdinalIgnoreCase));
                var variablesNames = element.foldout.Query<PropertyField>(null, "math-expression__variable-field")
                    .ToList().Select(f => f.label).ToList();

                if (Application.isPlaying && property.GetValue() is MathExpressionValue runtimeValue)
                {
                    runtimeValue.Invalidate();
                }
                
                if (variables.SequenceEqual(variablesNames, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }
                
                element.foldout.Clear();
                if (!element.isSteady)
                {
                    element.foldout.schedule.Execute(() => element.isSteady = true).ExecuteLater(20);
                }
                if (variablesNames.Count > 0 || element.isSteady)
                {
                    var expressionValue = property.GetValue() as MathExpressionValue;
                    var oldValues = expressionValue.variables;
                    expressionValue.variables = new List<Bind<float>>();
                    foreach (var variable in variables)
                    {
                        var currentIndex = variablesNames.IndexOf(variable);
                        if (currentIndex < 0)
                        {
                            expressionValue.variables.Add(0f.Bind());
                            continue;
                        }
                        
                        expressionValue.variables.Add(oldValues[currentIndex]);
                    }

                    variablesProperty.serializedObject.Update();
                }

                for (int i = 0; i < variablesProperty.arraySize; i++)
                {
                    var variable = variables[i];
                    var newVariable = variablesProperty.GetArrayElementAtIndex(i);
                    var field = new PropertyField(newVariable, variable)
                    {
                        bindingPath = $"{property.propertyPath}.{nameof(MathExpressionValue.variables)}",
                    }.WithClass("math-expression__variable-field");
                    field.BindProperty(newVariable);
                    element.foldout.Add(field);
                }
            }
            catch (ParsingException)
            {
                // No changes to variables
                return;
            }
        }

        private string DecoratedText(Elements element, string expression, out bool isValid)
        {
            if (string.IsNullOrEmpty(expression))
            {
                isValid = true;
                return string.Empty;
            }

            var parser = new Parser(expression);
            try
            {
                var expr = parser.Parse(out var variables);
                expr.Evaluate(variables.ToDictionary(v => v, v => 0.0));
            }
            catch (ParsingException ex)
            {
                isValid = false;
                return $"{expression[..ex.Position]}<color={kErrorColor}>{expression[ex.Position..]}</color>";
            }
            catch (Exception)
            {
                isValid = false;
                return expression;
            }

            var tokenizer = parser.Tokenizer;
            var tokens = tokenizer.Tokens;
            var label = expression;
            var offset = 0;
            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.Identifier:
                        var color = Parser.Constants.ContainsKey(token.Text) ? kConstantColor 
                                        : Parser.SystemVariables.ContainsKey(token.Text) ? kSystemVariableColor
                                        : FunctionRegistry.Functions.ContainsKey(token.Text) ? kFunctionColor
                                        : kVariableColor;
                        var finalPosition = token.Position + offset;
                        // Remove the token from the label
                        label = label.Remove(finalPosition, token.Text.Length);
                        // Insert the token with color
                        if(token.Text is "input")
                        {
                            color = kInputValuesColor;
                        }
                        else
                        {
                            foreach (var inputVar in element.inputVariables)
                            {
                                if(token.Text.Equals(inputVar, StringComparison.OrdinalIgnoreCase))
                                {
                                    color = kInputValuesColor;
                                    break;
                                }
                            }
                        }
                        var finalText = $"<color={color}>{token.Text}</color>";
                        // if (!FunctionRegistry.Functions.ContainsKey(token.Text))
                        // {
                        //     finalText = $"<b>{finalText}</b>";
                        // }
                        if(token.Text is "x" or "X")
                        {
                            finalText = $"<b>{finalText}</b>";
                        }
                        label = label.Insert(finalPosition, finalText);
                        offset += finalText.Length - token.Text.Length;
                        break;
                    // case TokenType.Number:
                    //     finalPosition = token.Position + offset;
                    //     // Remove the token from the label
                    //     label = label.Remove(finalPosition, token.Text.Length);
                    //     // Insert the token with color
                    //     finalText = $"<color={kNumberColor}>{token.Text}</color>";
                    //     label = label.Insert(finalPosition, finalText);
                    //     offset += finalText.Length - token.Text.Length;
                    //     break;
                }
            }
            
            isValid = true;
            return label;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Postica.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem.Utility
{
    [CustomPropertyDrawer(typeof(BoolExpressionValue), true)]
    class BoolExpressionDrawer : PropertyDrawer
    {
        private const string kConstantColor = "#11FF11";
        private const string kSystemVariableColor = "#11FFAA"; // aqua green
        private const string kOperatorColor = "#FFF555";
        private const string kVariableColor = "#FFA500"; // orange
        private const string kInputValuesColor = "#7fdbef";
        private const string kErrorColor = "red";

        private static readonly string kExpressionInfo = @$"-----------------------------------------
Use the following to build a boolean expression:
- <color={kConstantColor}>Boolean Literals</color> are <color={kConstantColor}><b>true</b></color> and <color={kConstantColor}><b>false</b></color>.
- <color={kSystemVariableColor}>System Variables</color> are runtime values available at execution.
- <color={kOperatorColor}>Operators</color> can be entered as symbols (<b>&&</b>, <b>||</b>, <b>!</b>, <b>==</b>, <b>!=</b>) or as words (<b>and</b>, <b>or</b>, <b>not</b>, <b>equals</b>, <b>notequals</b>).
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
            if (!elements.TryGetValue(property.propertyPath, out var element))
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
            }.WithClass("bool-expression__info");

            element.foldout.startOfHeader.Add(element.expressionInfo);

            // Add this class to avoid adding the replacement zones
            element.foldout.AddToClassList("ignore-target-replacement");
            element.foldout.viewDataKey = property.propertyPath;

            element.foldout.RegisterCallback<DetachFromPanelEvent>(evt => element.isSteady = false);

            return element.foldout;
        }

        private string GetTooltip(SerializedProperty property)
        {
            if (!string.IsNullOrEmpty(_finalTooltip)) return _finalTooltip;

            var inputVariables = property.FindPropertyRelative("_inputVariables");
            if (inputVariables.arraySize == 0)
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
                    .Append(inputVariables.GetArrayElementAtIndex(0).stringValue)
                    .Append("</b></color>.");
            }

            finalTooltip.Append("</size>\n\n").Append(kExpressionInfo);
            _finalTooltip = finalTooltip.ToString();

            return _finalTooltip;
        }

        private VisualElement CreateExpressionField(Elements element, SerializedProperty property)
        {
            var inputVariablesProp = property.FindPropertyRelative("_inputVariables");
            element.inputVariables = new List<string>();
            for (int i = 0; i < inputVariablesProp.arraySize; i++)
            {
                element.inputVariables.Add(inputVariablesProp.GetArrayElementAtIndex(i).stringValue);
            }

            var expressionProp = property.FindPropertyRelative("expression");
            element.expressionField = new TextField()
            {
                value = expressionProp.stringValue,
            }.WithClass("bool-expression__field");

            element.expressionLabel = new Label()
            {
                enableRichText = true,
                text = DecoratedText(element, expressionProp.stringValue, out var isExpressionValid),
                pickingMode = PickingMode.Ignore,
            };
            element.expressionLabel.AddToClassList("bool-expression__label");

            element.foldout.EnableInClassList("bool-expression--error", !isExpressionValid);

            var labelParent = new VisualElement() { pickingMode = PickingMode.Ignore }
                .WithClass("bool-expression__label-parent")
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
                element.foldout.EnableInClassList("bool-expression--error", !isExpressionValid);
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
            if (!elements.TryGetValue(property.propertyPath, out var element))
            {
                return;
            }

            var variablesProperty = property.FindPropertyRelative("variables");

            if (string.IsNullOrEmpty(expression))
            {
                variablesProperty.ClearArray();
                variablesProperty.serializedObject.ApplyModifiedProperties();
                element.foldout.Clear();
                return;
            }

            try
            {
                var parser = new BoolParser(expression);
                parser.Parse(out var variables);
                foreach (var inputVar in element.inputVariables)
                {
                    variables.Remove(inputVar);
                    variables.Remove(inputVar.ToUpper());
                    variables.Remove(inputVar.ToLower());
                }
                variables.RemoveAll(v => v.Equals("input", StringComparison.OrdinalIgnoreCase));
                var variablesNames = element.foldout.Query<PropertyField>(null, "bool-expression__variable-field")
                    .ToList().Select(f => f.label).ToList();

                if (Application.isPlaying && property.GetValue() is BoolExpressionValue runtimeValue)
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
                    var expressionValue = property.GetValue() as BoolExpressionValue;
                    var oldValues = expressionValue.variables;
                    expressionValue.variables = new List<Bind<bool>>();
                    foreach (var variable in variables)
                    {
                        var currentIndex = variablesNames.IndexOf(variable);
                        if (currentIndex < 0)
                        {
                            expressionValue.variables.Add(false.Bind());
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
                        bindingPath = $"{property.propertyPath}.variables",
                    }.WithClass("bool-expression__variable-field");
                    field.BindProperty(newVariable);
                    element.foldout.Add(field);
                }
            }
            catch (ParsingException)
            {
                // In case of a parsing error, do not update the variables
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

            var parser = new BoolParser(expression);
            try
            {
                var expr = parser.Parse(out var variables);
                // Evaluate using a default dictionary (all variables false)
                expr.Evaluate(variables.ToDictionary(v => v, v => false));
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

            // Decorate tokens with colors
            // (Assumes BoolParser exposes its tokenizer via a public Tokenizer property)
            var tokenizer = parser.Tokenizer;
            var tokens = tokenizer.Tokens;
            var label = expression;
            var offset = 0;
            foreach (var token in tokens)
            {
                string color = kVariableColor;
                switch (token.Type)
                {
                    case BoolTokenType.Identifier:
                        // Use input color if the identifier is an input variable
                        foreach (var inputVar in element.inputVariables)
                        {
                            if (token.Text.Equals(inputVar, StringComparison.OrdinalIgnoreCase))
                            {
                                color = kInputValuesColor;
                                break;
                            }
                        }
                        if (token.Text.Equals("input", StringComparison.OrdinalIgnoreCase))
                            color = kInputValuesColor;
                        break;
                    case BoolTokenType.BooleanLiteral:
                        color = kConstantColor;
                        break;
                    case BoolTokenType.And:
                    case BoolTokenType.Or:
                    case BoolTokenType.Not:
                    case BoolTokenType.Equals:
                    case BoolTokenType.NotEquals:
                        color = kOperatorColor;
                        break;
                }
                var finalPosition = token.Position + offset;
                label = label.Remove(finalPosition, token.Text.Length);
                var finalText = $"<color={color}>{token.Text}</color>";
                label = label.Insert(finalPosition, finalText);
                offset += finalText.Length - token.Text.Length;
            }
            isValid = true;
            return label;
        }
    }
}

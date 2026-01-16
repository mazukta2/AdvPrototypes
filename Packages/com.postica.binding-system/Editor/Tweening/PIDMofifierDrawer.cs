using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using Postica.Common;
using UnityEditor.UIElements;

namespace Postica.BindingSystem.Tweening
{
    [CustomPropertyDrawer(typeof(PIDModifier<>.Data), true)]
    public class PIDModifierDataDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!property.IsAlive())
            {
                return new Label("Error: Property is corrupted or not found.");
            }

            // Properties
            var kpProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.Kp));
            var kiProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.Ki));
            var kdProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.Kd));

            var valueTypeProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.valueType));
            var otherVariableProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.otherVariable));

            var useUnscaledTimeProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.unscaledTime));

            var useLimitsProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.useLimits));
            var useLimitsValueProp = useLimitsProp.FindPropertyRelative("_value");
            var useLimitsIsBoundProp = useLimitsProp.FindPropertyRelative("_isBound");
            var minProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.min));
            var maxProp = property.FindPropertyRelative(nameof(PIDModifier<int>.Data.max));

            var view = new EnhancedFoldout().AddTweensStyle().WithClass("pid-data");
            view.text = "PID";

            view.restOfHeader.Add(new PropertyField { label = "", focusable = false }.EnsureBind(valueTypeProp)
                .WithClass("pid-data-popup"));

            var otherValueField = new PropertyField(otherVariableProp).WithClass("pid-data__property", "pid-data__property--desired")
                .VisibleWhen(() => valueTypeProp?.enumValueIndex != (int)PIDValueType.ClosedLoop);
            view.Add(otherValueField);

            var graphView = new PIDGraphView(kpProp.GetValue() as IValueProvider<float>, kiProp.GetValue() as IValueProvider<float>, kdProp.GetValue() as IValueProvider<float>);
            view.Add(graphView);
            
            view.Add(new PropertyField(kpProp).WithClass("pid-data__property", "pid-data__property--kp").DoOnValueChange(v => graphView.Regenerate()));
            view.Add(new PropertyField(kiProp).WithClass("pid-data__property", "pid-data__property--ki").DoOnValueChange(v => graphView.Regenerate()));
            view.Add(new PropertyField(kdProp).WithClass("pid-data__property", "pid-data__property--kd").DoOnValueChange(v => graphView.Regenerate()));

            view.Add(new PropertyField(useUnscaledTimeProp).WithClass("pid-data__property",
                "pid-data__property--unscaled"));

            view.Add(new PropertyField(useLimitsProp).WithClass("pid-data__property", "pid-data__property--limits"));
            view.Add(new PropertyField(minProp).WithClass("pid-data__property", "pid-data__property--min")
                .VisibleWhen(() => useLimitsValueProp?.boolValue == true || useLimitsIsBoundProp?.boolValue == true));
            view.Add(new PropertyField(maxProp).WithClass("pid-data__property", "pid-data__property--max")
                .VisibleWhen(() => useLimitsValueProp?.boolValue == true || useLimitsIsBoundProp?.boolValue == true));
            
            var processor = new VisualElement().WithStyle(s => s.position = Position.Absolute);
            view.Add(processor);

            processor.schedule.Execute(() =>
            {
                var label = processor.Q<Label>(null, "bind-field__label", "unity-property-field__label");
                if(label != null){
                    label.text = valueTypeProp?.enumValueIndex == (int)PIDValueType.AsSetPoint ? "Process Variable" : "Set Point";
                }
            }).Every(100);
            
            return view;
        }

        private class PIDGraphView : VisualElement
        {
            public static readonly string ussClassName = "pid-graph";
            public static readonly string ussLabelClassName = $"{ussClassName}__label";
            public static readonly string ussGridClassName = $"{ussClassName}__grid";
            public static readonly string ussInputClassName = $"{ussClassName}__input";

            private static readonly CustomStyleProperty<float> s_CurveThicknessProperty =
                new("--curve-thickness");

            private static readonly CustomStyleProperty<float> s_GridLineWidthProperty =
                new("--grid-line-width");

            private static readonly CustomStyleProperty<int> s_CurvePointsCountProperty =
                new("--curve-points-count");

            private static readonly CustomStyleProperty<Color> s_GridColorProperty =
                new("--grid-color");

            private static readonly CustomStyleProperty<Color> s_TargetColorProperty =
                new("--target-color");

            private static readonly CustomStyleProperty<Color> s_ValueColorProperty =
                new("--value-color");

            private GridView grid;
            private Label label;

            private Color targetColor = Color.cyan;
            private Color valueColor = Color.red;

            private IValueProvider<float> kp;
            private IValueProvider<float> ki;
            private IValueProvider<float> kd;
            
            private float simulatedInput = 0.6f;

            public PIDGraphView(IValueProvider<float> kp, IValueProvider<float> ki, IValueProvider<float> kd)
            {
                focusable = false;

                this.kp = kp;
                this.ki = ki;
                this.kd = kd;

                AddToClassList(ussClassName);
                
                grid = new GridView(this);
                Add(grid);
                
                var simulatedInputField = new FloatField("TEST INPUT")
                {
                    value = simulatedInput,
                }.WithClass(ussInputClassName);
                simulatedInputField.RegisterValueChangedCallback(evt =>
                {
                    simulatedInput = evt.newValue;
                    grid.Regenerate();
                });
                Add(simulatedInputField);
                
                label = new Label("SIMULATION").WithClass(ussLabelClassName);
                label.usageHints = UsageHints.DynamicColor;
                Add(label);

                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            }
            
            public void Regenerate()
            {
                grid.Regenerate();
            }

            private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
            {
                if (evt.customStyle.TryGetValue(s_TargetColorProperty, out var targetColorProperty))
                {
                    targetColor = targetColorProperty;
                }

                if (evt.customStyle.TryGetValue(s_ValueColorProperty, out var valueColorProperty))
                {
                    valueColor = valueColorProperty;
                }
            }

            private class GridView : VisualElement
            {
                private readonly PIDGraphView owner;
                public Vector2[] curvePoints;
                public Vector2[] targetPoints;

                public float curveThickness = 2f;
                public float gridLineWidth = 1f;
                public Color gridColor = new(0.3f, 0.3f, 0.3f);
                public int curvePointsCount = 100;
                public float step = 1f;

                public GridView(PIDGraphView owner)
                {
                    this.owner = owner;

                    AddToClassList(ussGridClassName);
                    GenerateCurvePoints();
                    GenerateTargetPoints();
                    RegisterCallback<CustomStyleResolvedEvent>(OnGridCustomStyleResolved);
                    generateVisualContent += PaintCellGrid;
                }
                
                public void Regenerate()
                {
                    GenerateCurvePoints();
                    GenerateTargetPoints();
                    MarkDirtyRepaint();
                }
                
                private void GenerateCurvePoints()
                {
                    float kp = owner.kp?.Value ?? 0;
                    float ki = owner.ki?.Value ?? 0;
                    float kd = owner.kd?.Value ?? 0;

                    float? lastError = null;
                    float integral = 0;
                    float output = 0;

                    float max = contentRect.height;
                    
                    float Compute(float input, float targetOutput, float dt)
                    {
                        var error = targetOutput - input;
                        float derivative = lastError.HasValue ? (error - lastError.Value) / dt : 0;
                        integral += error * dt;

                        integral = Mathf.Clamp(integral, -1, 1);
                        
                        lastError = error;
                        
                        var result = kp * error + ki * integral + kd * derivative;
                        // if (owner.min != null && owner.max != null)
                        {
                            result = Mathf.Clamp(result, -1, 1);
                        }
                        return result;
                    }

                    float dt = step / (curvePointsCount);
                    curvePoints = new Vector2[curvePointsCount];
                    for (int i = 0; i < curvePointsCount; i++)
                    {
                        float t = i / (float)(curvePointsCount - 1);
                        output += Compute(output, i < curvePointsCount / 10 ? 0 : owner.simulatedInput, dt);
                        curvePoints[i] = new Vector2(t, Mathf.Clamp(output, -1, 1));
                    }
                }

                private void GenerateTargetPoints()
                {
                    targetPoints = new Vector2[curvePointsCount];
                    for (int i = 0; i < curvePointsCount; i++)
                    {
                        float t = i / (float)(curvePointsCount - 1);
                        float value = i < curvePointsCount / 10 ? 0 : owner.simulatedInput;
                        targetPoints[i] = new Vector2(t, Mathf.Clamp(value, -1, 1));
                    }
                }

                private void OnGridCustomStyleResolved(CustomStyleResolvedEvent evt)
                {
                    if (evt.customStyle.TryGetValue(s_CurveThicknessProperty, out var curveThicknessLength))
                    {
                        curveThickness = curveThicknessLength;
                    }

                    if (evt.customStyle.TryGetValue(s_GridLineWidthProperty, out var gridLineWidthLength))
                    {
                        gridLineWidth = gridLineWidthLength;
                    }

                    if (evt.customStyle.TryGetValue(s_CurvePointsCountProperty, out var curvePointsCountValue))
                    {
                        curvePointsCount = curvePointsCountValue;
                        Regenerate();
                    }

                    if (evt.customStyle.TryGetValue(s_GridColorProperty, out var gridColorValue))
                    {
                        gridColor = gridColorValue;
                    }
                }

                private void PaintCellGrid(MeshGenerationContext ctx)
                {
                    var painter = ctx.painter2D;
                    var rect = contentRect;

                    // Draw grid
                    painter.strokeColor = gridColor;
                    painter.lineWidth = gridLineWidth;

                    // Vertical lines
                    painter.BeginPath();
                    for (int i = 0; i <= 4; i++)
                    {
                        float x = rect.x + (rect.width * i / 4);
                        painter.MoveTo(new Vector2(x, rect.y));
                        painter.LineTo(new Vector2(x, rect.y + rect.height));
                    }

                    painter.Stroke();

                    // Horizontal lines
                    painter.BeginPath();
                    for (int i = 0; i <= 4; i++)
                    {
                        float y = rect.y + (rect.height * i / 4);
                        painter.MoveTo(new Vector2(rect.x, y));
                        painter.LineTo(new Vector2(rect.x + rect.width, y));
                    }

                    painter.Stroke();

                    // Draw curve
                    DrawCurve(rect, painter, targetPoints, owner.targetColor, curveThickness);
                    DrawCurve(rect, painter, curvePoints, owner.valueColor.WithAlpha(0.6f), curveThickness);
                }
                
                private static void DrawCurve(Rect rect, Painter2D painter, Vector2[] points, Color color, float curveThickness)
                {
                    painter.lineWidth = curveThickness;
                    var faintColor = color.WithAlpha(0.3f);

                    painter.BeginPath();
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        Vector2 p1 = new Vector2(
                            rect.x + points[i].x * rect.width,
                            rect.y + (0.5f - points[i].y) * rect.height
                        );
                        Vector2 p2 = new Vector2(
                            rect.x + points[i + 1].x * rect.width,
                            rect.y + (0.5f - points[i + 1].y / 2) * rect.height
                        );

                        var y1 = points[i].y;
                        var y2 = points[i + 1].y;
                        bool InRange(float x) => x > -1 && x < 1;
                        painter.strokeColor = InRange(y2) ? color : faintColor;
                        
                        if (i == 0)
                        {
                            painter.MoveTo(p1);
                        }

                        painter.LineTo(p2);
                    }

                    painter.Stroke();
                }
            }
        }
    }
}
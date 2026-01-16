using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using Postica.Common;

namespace Postica.BindingSystem.Tweening
{
    public class EasingFunctionCell : Button
    {
        public new static readonly string ussClassName = "tween-cell";
        public static readonly string ussLabelClassName = $"{ussClassName}__label";
        public static readonly string ussGridClassName = $"{ussClassName}__grid";
        public static readonly string ussDotViewClassName = $"{ussClassName}__dot-view";
        public static readonly string ussDotClassName = $"{ussDotViewClassName}__dot";

        private static readonly CustomStyleProperty<float> s_CurveThicknessProperty =
            new("--curve-thickness");

        private static readonly CustomStyleProperty<float> s_GridLineWidthProperty =
            new("--grid-line-width");

        private static readonly CustomStyleProperty<float> s_AnimationDurationProperty =
            new("--animation-duration");

        private static readonly CustomStyleProperty<int> s_CurvePointsCountProperty =
            new("--curve-points-count");

        private static readonly CustomStyleProperty<Color> s_GridColorProperty =
            new("--grid-color");

        private static readonly CustomStyleProperty<Color> s_StartColorProperty =
            new("--start-color");

        private static readonly CustomStyleProperty<Color> s_EndColorProperty =
            new("--end-color");

        private EasingFunction function;
        private EasingFunction ghostFunction;
        private float animationTime;
        private DateTime lastAnimationTime;
        private bool isAnimating;
        private bool restartAnimation;
        private bool autoAdjustSize;
        private bool showXAxisLabels;
        private float xMin, xMax, yMin, yMax;
        private string measurementUnit = "";

        private GridView grid;
        private VisualElement dotView;
        private Image dot;
        private Label label;
        
        private Label minXLabel;
        private Label maxXLabel;

        private float? animationDuration;
        private Func<float> getAnimationDuration;
        private Color startColor = Color.cyan;
        private Color endColor = Color.red;
        private Gradient gradient;

        public float XAxisStart { get; set; } = 0f;
        public float XAxisEnd { get; set; } = 1f;
        public float YAxisStart { get; set; } = 0f;
        public float YAxisEnd { get; set; } = 1f;
        
        public float ComputedXAxisStart => AutoAdjust ? xMin : XAxisStart;
        public float ComputedXAxisEnd => AutoAdjust ? xMax : XAxisEnd;
        
        public Func<float> GetConvergenceThreshold { get; set; }
        
        public int CurvePointsCount
        {
            get => grid.curvePointsCount;
            set
            {
                if (grid.curvePointsCount == value)
                {
                    return;
                }
                
                grid.curvePointsCount = Mathf.Max(1, value);
                grid.GenerateCurvePoints();
                grid.MarkDirtyRepaint();
            }
        }

        public bool AutoAdjust
        {
            get => autoAdjustSize;
            set
            {
                if(autoAdjustSize == value)
                {
                    return;
                }
                
                EnableInClassList("tween-cell--auto-adjust", value);
                autoAdjustSize = value;
                Rebuild();
            }
        }
        
        public string MeasurementUnit
        {
            get => measurementUnit;
            set
            {
                if (measurementUnit == value)
                {
                    return;
                }
                
                measurementUnit = value;
                minXLabel.text = $"{XAxisStart.ToString("F2")}{measurementUnit}";
                maxXLabel.text = $"{XAxisEnd.ToString("F2")}{measurementUnit}";
            }
        }
        
        public bool ShowXAxisLabels
        {
            get => showXAxisLabels;
            set
            {
                if (showXAxisLabels == value)
                {
                    return;
                }
                
                showXAxisLabels = value;
                EnableInClassList("tween-cell--show-x-labels", value);
            }
        }

        public EasingFunction Function
        {
            get => function;
            set 
            {
                if (function == value)
                {
                    return;
                } 
                function = value;
                label.text = function.Name;
                Rebuild();
            }
        }

        public EasingFunction GhostFunction
        {
            get => ghostFunction;
            set
            {
                if (ghostFunction == value)
                {
                    return;
                }

                ghostFunction = value;
                Rebuild();
            }
        }

        public int GridHorizontalLines { get; set; } = 2;
        public int GridVerticalLines { get; set; } = 2;

        public EasingFunctionCell(EasingFunction function, Func<float> getAnimationDuration, bool restartAnimation = false, EasingFunction ghostFunction = null)
        {
            this.function = function;
            this.restartAnimation = restartAnimation;
            this.getAnimationDuration = getAnimationDuration;
            this.ghostFunction = ghostFunction;

            focusable = false;

            AddToClassList(ussClassName);

            label = new Label(function.Name).WithClass(ussLabelClassName);
            label.usageHints = UsageHints.DynamicColor;
            Add(label);
            
            minXLabel = new Label(XAxisStart.ToString("F2")).WithClass("tween-cell__x-label", "tween-cell__x-label--min");
            maxXLabel = new Label(XAxisEnd.ToString("F2")).WithClass("tween-cell__x-label", "tween-cell__x-label--max");
            minXLabel.usageHints = UsageHints.DynamicColor;
            maxXLabel.usageHints = UsageHints.DynamicColor;
            Add(minXLabel);
            Add(maxXLabel);

            grid = new GridView(this);
            Add(grid);
            
            dotView = new VisualElement().WithClass(ussDotViewClassName);
            dot = new Image().WithClass(ussDotClassName);
            dot.usageHints = UsageHints.DynamicTransform | UsageHints.DynamicColor;
            dotView.Add(dot);
            Add(dotView);
            
            ComputeGradient();

            RegisterCallback<MouseEnterEvent>(evt => EnableAnimation(true));
            RegisterCallback<MouseLeaveEvent>(evt => EnableAnimation(false));
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            EditorApplication.update += OnEditorUpdate;
            
            Add(new Label("CURRENT").WithClass("tween-cell__current-label"));
        }
        
        public void Rebuild()
        {
            ComputeGradient();
            grid.GenerateCurvePoints();
            grid.MarkDirtyRepaint();
            MarkDirtyRepaint();
        }

        private void ComputeGradient()
        {
            gradient = new Gradient();
            var colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(startColor, 0f);
            colorKeys[1] = new GradientColorKey(endColor, 1f);
            gradient.SetKeys(colorKeys, Array.Empty<GradientAlphaKey>());
        }

        private void EnableAnimation(bool enable)
        {
            EnableInClassList("tween-cell--animating", enable);
            if (isAnimating == enable)
            {
                return;
            }

            isAnimating = enable;
            if (isAnimating)
            {
                animationTime = 0f;
            }
            else
            {
                MarkDirtyRepaint();
            }
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_StartColorProperty, out var startColorProperty))
            {
                startColor = startColorProperty;
                ComputeGradient();
            }
            
            if (evt.customStyle.TryGetValue(s_EndColorProperty, out var endColorProperty))
            {
                endColor = endColorProperty;
                ComputeGradient();
            }
            
            if (evt.customStyle.TryGetValue(s_AnimationDurationProperty, out var animationDurationLength))
            {
                animationDuration = animationDurationLength;
            }
        }

        private void OnEditorUpdate()
        {
            if (isAnimating)
            {
                var duration = animationDuration ?? getAnimationDuration?.Invoke() ?? xMax - xMin;
                if (duration <= 0f)
                {
                    return;
                }
                
                var deltaTime = (float)(DateTime.Now - lastAnimationTime).TotalSeconds;
                animationTime = Mathf.Min(duration, animationTime + deltaTime);
                if (restartAnimation && animationTime >= duration)
                {
                    animationTime = 0f;
                }

                float normalizedTime = AutoAdjust ? animationTime : Mathf.Clamp01(animationTime / duration);
                float value = function.Function(normalizedTime);

                var color = Color.LerpUnclamped(startColor, endColor, value);
            
                dot.style.backgroundColor = color;
                dot.style.bottom = new StyleLength(new Length(value * layout.height / (yMax - yMin), LengthUnit.Percent));
                label.style.backgroundColor = color;
                
                MarkDirtyRepaint();
            }
            else
            {
                label.style.backgroundColor = new StyleColor(StyleKeyword.Null);
            }
            
            lastAnimationTime = DateTime.Now;
        }

        public void Dispose()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private class GridView : VisualElement
        {
            private readonly EasingFunctionCell owner;
            public Vector2[] curvePoints;
            public Vector2[] ghostCurvePoints;

            public float curveThickness = 2f;
            public float gridLineWidth = 1f;
            public Color gridColor = new(0.3f, 0.3f, 0.3f);
            public Color ghostLineColor = new(0.5f, 0.5f, 0.5f);
            public int curvePointsCount = 50;

            public GridView(EasingFunctionCell owner)
            {
                this.owner = owner;

                AddToClassList(ussGridClassName);
                GenerateCurvePoints();
                RegisterCallback<CustomStyleResolvedEvent>(OnGridCustomStyleResolved);
                generateVisualContent += PaintCellGrid;
            }

            public void GenerateCurvePoints()
            {
                const float testScale = 20f;
                
                var (xMin, xMax) = (owner.XAxisStart, owner.XAxisEnd);
                var (yMin, yMax) = (owner.YAxisStart, owner.YAxisEnd);
                var step = (xMax - xMin) / (curvePointsCount - 1);
                
                if (owner.AutoAdjust)
                {
                    var threshold = owner.GetConvergenceThreshold?.Invoke() ?? 0;
                    xMax = owner.function.GetConvergencePoint(xMin, xMax * testScale, step / testScale, threshold);
                    step = (xMax - xMin) / (curvePointsCount - 1);
                    (yMin, yMax) = owner.function.GetMinMax(xMin, xMax, step);

                    if (owner.ghostFunction != null)
                    {
                        var ghostXMax = owner.ghostFunction.GetConvergencePoint(owner.XAxisStart, owner.XAxisEnd * testScale, step / testScale, threshold);
                        if (ghostXMax > xMax)
                        {
                            xMax = ghostXMax;
                            step = (xMax - xMin) / (curvePointsCount - 1);
                        }
                        var ghostMinMax = owner.ghostFunction.GetMinMax(xMin, xMax, step);
                        if (ghostMinMax.min < yMin)
                        {
                            yMin = ghostMinMax.min;
                        }
                        if (ghostMinMax.max > yMax)
                        {
                            yMax = ghostMinMax.max;
                        }
                    }
                    
                    owner.minXLabel.text = xMin.ToString("F2") + owner.MeasurementUnit;
                    owner.maxXLabel.text = xMax.ToString("F2") + owner.MeasurementUnit;
                }
                
                owner.xMin = xMin;
                owner.xMax = xMax;
                owner.yMin = yMin;
                owner.yMax = yMax;
                
                curvePoints = new Vector2[curvePointsCount];
                for (int i = 0; i < curvePointsCount; i++)
                {
                    float t = i * step;
                    float value = owner.function.Function(t);
                    curvePoints[i] = new Vector2(t / (xMax - xMin), value / (yMax - yMin));
                }
                
                if (owner.ghostFunction != null)
                {
                    ghostCurvePoints = new Vector2[curvePointsCount];
                    for (int i = 0; i < curvePointsCount; i++)
                    {
                        float t = i * step;
                        float value = owner.ghostFunction.Function(t);
                        ghostCurvePoints[i] = new Vector2(t / (xMax - xMin), value / (yMax - yMin));
                    }
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
                    GenerateCurvePoints();
                }

                if (evt.customStyle.TryGetValue(s_GridColorProperty, out var gridColorValue))
                {
                    gridColor = gridColorValue;
                }
                
                if (evt.customStyle.TryGetValue(s_StartColorProperty, out var ghostLineColorValue))
                {
                    ghostLineColor = ghostLineColorValue;
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
                var verticalLinesCount = owner.GridVerticalLines * (owner.xMax - owner.xMin);
                painter.BeginPath();
                for (int i = 0; i <= verticalLinesCount; i++)
                {
                    float x = rect.x + (rect.width * i / verticalLinesCount);
                    painter.MoveTo(new Vector2(x, rect.y));
                    painter.LineTo(new Vector2(x, rect.y + rect.height));
                }

                painter.Stroke();

                // Horizontal lines
                var horizontalLinesCount = owner.GridHorizontalLines * (owner.yMax - owner.yMin);
                painter.BeginPath();
                for (int i = 0; i <= horizontalLinesCount; i++)
                {
                    float y = rect.y + (rect.height * i / horizontalLinesCount);
                    painter.MoveTo(new Vector2(rect.x, y));
                    painter.LineTo(new Vector2(rect.x + rect.width, y));
                }

                painter.Stroke();
                
                // Draw ghost curve if exists
                if (owner.ghostFunction != null && ghostCurvePoints != null)
                {
                    painter.strokeColor = ghostLineColor;
                    painter.lineWidth = curveThickness * 0.5f;

                    painter.BeginPath();
                    for (int i = 0; i < ghostCurvePoints.Length - 1; i++)
                    {
                        Vector2 p1 = new Vector2(
                            rect.x + ghostCurvePoints[i].x * rect.width,
                            rect.y + (1 - ghostCurvePoints[i].y) * rect.height
                        );
                        Vector2 p2 = new Vector2(
                            rect.x + ghostCurvePoints[i + 1].x * rect.width,
                            rect.y + (1 - ghostCurvePoints[i + 1].y) * rect.height
                        );

                        if (i == 0)
                        {
                            painter.MoveTo(p1);
                        }

                        painter.LineTo(p2);
                    }

                    painter.Stroke();
                }

                // Draw curve
                painter.strokeGradient = owner.gradient;
                painter.lineWidth = curveThickness;

                painter.BeginPath();
                for (int i = 0; i < curvePoints.Length - 1; i++)
                {
                    Vector2 p1 = new Vector2(
                        rect.x + curvePoints[i].x * rect.width,
                        rect.y + (1 - curvePoints[i].y) * rect.height
                    );
                    Vector2 p2 = new Vector2(
                        rect.x + curvePoints[i + 1].x * rect.width,
                        rect.y + (1 - curvePoints[i + 1].y) * rect.height
                    );

                    if (i == 0)
                    {
                        painter.MoveTo(p1);
                    }

                    painter.LineTo(p2);
                }

                painter.Stroke();
            }
        }

        public EasingFunctionCell Clone(Action onClick)
        {
            var clone = new EasingFunctionCell(function, getAnimationDuration, restartAnimation);
            clone.clicked += onClick;

            foreach (var @class in GetClasses())
            {
                clone.AddToClassList(@class);
            }

            return clone;
        }
    }
}
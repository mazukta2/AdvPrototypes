using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Postica.Common
{
    public class AlignInInspectorManipulator : Manipulator
    {
        public delegate VisualElement GetFieldLabelDelegate(VisualElement target);


        public static readonly string alignedFieldUssClassName = "field__aligned";
        private static readonly string inspectorFieldUssClassName = "field__inspector-field";
        private static CustomStyleProperty<float> s_LabelWidthRatioProperty =
            new("--unity-property-field-label-width-ratio");
        private static CustomStyleProperty<float> s_LabelExtraPaddingProperty =
            new("--unity-property-field-label-extra-padding");
        private static CustomStyleProperty<float> s_LabelBaseMinWidthProperty =
            new("--unity-property-field-label-base-min-width");
        private static CustomStyleProperty<float> s_LabelExtraContextWidthProperty =
            new("--unity-base-field-extra-context-width");

        private float _labelWidthRatio;
        private float _labelExtraPadding;
        private float _labelBaseMinWidth;
        private float _labelExtraContextWidth;
        private VisualElement _cachedContextWidthElement;
        private VisualElement _cachedInspectorElement;

        private Func<bool> _condition;

        public VisualElement fieldLabel{ get; set; }

        public GetFieldLabelDelegate fieldLabelGetter { get; set; }

        public AlignInInspectorManipulator(VisualElement fieldLabel, Func<bool> condition = null)
        {
            this.fieldLabel = fieldLabel;
            this._condition = condition;
        }

        public AlignInInspectorManipulator(GetFieldLabelDelegate fieldLabelGetter, Func<bool> condition = null)
        {
            this.fieldLabelGetter = fieldLabelGetter;
            this._condition = condition;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.AddToClassList(alignedFieldUssClassName);
            target.RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            if(target.panel != null)
            {
                PrepareAlignment();
            }
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.RemoveFromClassList(alignedFieldUssClassName);
            target.UnregisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            target.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            target.RegisterCallback<GeometryChangedEvent>(OnInspectorFieldGeometryChanged);
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            PrepareAlignment();
        }

        private void PrepareAlignment()
        {
            for (VisualElement visualElement = target.parent; visualElement != null; visualElement = visualElement.parent)
            {
                if (visualElement.ClassListContains("unity-inspector-element"))
                {
                    _cachedInspectorElement = visualElement;
                }

                if (visualElement.ClassListContains("unity-inspector-main-container"))
                {
                    _cachedContextWidthElement = visualElement;
                    break;
                }
            }

            if (_cachedInspectorElement != null)
            {
                _labelWidthRatio = 0.45f;
                _labelExtraPadding = 37f;
                _labelBaseMinWidth = 123f;
                _labelExtraContextWidth = 1f;
                target.RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
                target.AddToClassList(inspectorFieldUssClassName);
                target.RegisterCallback<GeometryChangedEvent>(OnInspectorFieldGeometryChanged);
            }
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var shouldAlign = false;
            if (evt.customStyle.TryGetValue(s_LabelWidthRatioProperty, out var labelWidthRatio)
                && Mathf.Abs(_labelWidthRatio - labelWidthRatio) > Mathf.Epsilon)
            {
                _labelWidthRatio = labelWidthRatio;
                shouldAlign = true;
            }

            if (evt.customStyle.TryGetValue(s_LabelExtraPaddingProperty, out var labelExtraPadding)
                && Mathf.Abs(_labelExtraPadding - labelExtraPadding) > Mathf.Epsilon)
            {
                _labelExtraPadding = labelExtraPadding;
                shouldAlign = true;
            }

            if (evt.customStyle.TryGetValue(s_LabelBaseMinWidthProperty, out var labelBaseMinWidth)
                && Mathf.Abs(_labelBaseMinWidth - labelBaseMinWidth) > Mathf.Epsilon)
            {
                _labelBaseMinWidth = labelBaseMinWidth;
                shouldAlign = true;
            }

            if (evt.customStyle.TryGetValue(s_LabelExtraContextWidthProperty, out var labelExtraContextWidth)
                && Mathf.Abs(_labelExtraContextWidth - labelExtraContextWidth) > Mathf.Epsilon)
            {
                _labelExtraContextWidth = labelExtraContextWidth;
                shouldAlign = true;
            }

            if (shouldAlign)
            {
                AlignLabel();
            }
        }

        private void OnInspectorFieldGeometryChanged(GeometryChangedEvent e)
        {
            AlignLabel();
        }

        private void AlignLabel()
        {   
            var label = fieldLabel ?? fieldLabelGetter?.Invoke(target);
            if (label != null && target.ClassListContains(alignedFieldUssClassName))
            {
                if (_condition != null && !_condition())
                {
                    label.style.minWidth = new StyleLength(StyleKeyword.Null);
                    label.style.width = new StyleLength(StyleKeyword.Null);
                    return;
                }
                float labelExtraPadding = _labelExtraPadding;
                float num = target.worldBound.x - _cachedInspectorElement.worldBound.x - _cachedInspectorElement.resolvedStyle.paddingLeft;
                labelExtraPadding += num;
                labelExtraPadding += target.resolvedStyle.paddingLeft;
                float a = _labelBaseMinWidth - num - target.resolvedStyle.paddingLeft;
                VisualElement visualElement = _cachedContextWidthElement ?? _cachedInspectorElement;
                label.style.minWidth = Mathf.Max(a, 0f);
                float num2 = (visualElement.resolvedStyle.width + _labelExtraContextWidth) * _labelWidthRatio - labelExtraPadding;
                if (Mathf.Abs(label.resolvedStyle.width - num2) > 1E-30f)
                {
                    label.style.width = Mathf.Max(0f, num2);
                }
            }
        }
    }

}
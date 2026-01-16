using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using Postica.Common;
using UnityEditor.UIElements;

namespace Postica.BindingSystem.Tweening
{
    [CustomPropertyDrawer(typeof(DampedTrackerModifier<>.Data), true)]
    public class DampedTrackerModifierDataDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!property.IsAlive())
            {
                return new Label("Error: Property is corrupted or not found.");
            }
            
            // Properties
            var originTypeProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.originType));
            var targetReachedProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.onTargetReached));
            var originProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.origin));
            
            // Time related
            var timeScaleProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.time));
            var delayProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.delay));
            var omegaProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.speed));
            var dampControlProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.damping));
            var repeatCountProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.repeatCount));
            var repeatDelayProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.repeatDelay));
            var epsilonProp = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.targetEpsilon));
            
            // Events
            var onTweenStarted = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.onChaseStarted));
            var onTweenUpdated = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.onChaseUpdated));
            var onTweenCompleted = property.FindPropertyRelative(nameof(DampedTrackerModifier<int>.Data.onChaseCompleted));
            
            if(omegaProp.GetValue() is not IValueProvider<float> omegaProvider 
               || dampControlProp.GetValue() is not IValueProvider<float> dampControlProvider
               || delayProp.GetValue() is not IValueProvider<float> delayProvider)
            {
                return null;
            }
            
            var view = new EnhancedFoldout().AddTweensStyle().WithClass("tween-data", "tween-data--damped-tracker");
            view.text = "Smooth Value";
            view.value = false;
            view.viewDataKey = "tween-data-" + property.propertyPath;
            
            // Add some basic information in the header
            var convergenceLabel = new Label("⇥ ")
            {
                tooltip = "<b>Approximated Convergence Time</b>\nThe time it takes for the tracker to converge to the target value, in seconds.",
            }.WithClass("tween-data__header-label", "tween-data__header-label--convergence");
            
            var omegaLabel = new Label("Ω ")
            {
                tooltip = "<b>Omega - Follow Speed</b>\nThe angular frequency of the damped harmonic motion, in radians per second." +
                          "\nThe higher the value, the faster the tracker converges to the target value.",
            }.WithClass("tween-data__header-label", "tween-data__header-label--omega");
            
            var zetaLabel = new Label("ζ ")
            {
                tooltip = "<b>Zeta - Damping power</b>\nThe damping ratio of the damped harmonic motion, which determines the type of damping: \n < 1 \u2192 underdamped (Springy)\n ~ 1 \u2192 critically damped (Firm)\n > 1 \u2192 overdamped (Slow Convergence)",
            }.WithClass("tween-data__header-label", "tween-data__header-label--damping");
            
            view.restOfHeader.Add(convergenceLabel);
            view.restOfHeader.Add(omegaLabel);
            view.restOfHeader.Add(zetaLabel);

            var previewFunction = BuildFunctionPreview(omegaProp, dampControlProp, delayProp);

            var omegaField = new PropertyField(omegaProp).WithClass("tween-data__property", "tween-data__omega");
            omegaField.RegisterValueChangeCallback(_ => UpdateValues());
            
            var zetaField = new PropertyField(dampControlProp).WithClass("tween-data__property", "tween-data__damp-control");
            zetaField.RegisterValueChangeCallback(_ => UpdateValues());
            
            var delayPropField = new PropertyField(delayProp).WithClass("tween-data__property", "tween-data__delay");
            delayPropField.RegisterValueChangeCallback(_ => UpdateValues());
            
            view.Add(omegaField);
            view.Add(zetaField);
            view.Add(delayPropField);
            
            view.Add(previewFunction);
            
                
            var advancedFoldout = new Foldout
            {
                text = "Advanced",
                value = false,
                viewDataKey = "tween-advanced-" + property.propertyPath
            }.WithClass("tween-data__advanced");
            
            advancedFoldout.Add(new PropertyField(timeScaleProp).WithClass("tween-data__property", "tween-data__time-scale"));
            advancedFoldout.Add(new PropertyField(epsilonProp).WithClass("tween-data__property", "tween-data__epsilon"));
            advancedFoldout.Add(new VisualElement().WithClass("tween-data__separator"));
            advancedFoldout.Add(new PropertyField(originTypeProp).WithClass("tween-data__property", "tween-data__origin-type"));
            advancedFoldout.Add(new PropertyField(originProp).WithClass("tween-data__property", "tween-data__origin").VisibleWhen(() => originTypeProp.IsAlive() && originTypeProp.enumValueIndex == (int)TweenOriginType.PreciseOrigin));
            advancedFoldout.Add(new PropertyField(targetReachedProp).WithClass("tween-data__property", "tween-data__target-reached").VisibleWhen(() => originTypeProp.IsAlive() && originTypeProp.enumValueIndex == (int)TweenOriginType.PreciseOrigin));
            advancedFoldout.Add(new VisualElement().WithClass("tween-data__separator"));
            advancedFoldout.Add(new PropertyField(repeatCountProp).WithClass("tween-data__property", "tween-data__repeat-count"));
            advancedFoldout.Add(new PropertyField(repeatDelayProp).WithClass("tween-data__property", "tween-data__repeat-delay"));
            advancedFoldout.Add(new VisualElement().WithClass("tween-data__separator"));
            advancedFoldout.Add(new PropertyField(onTweenStarted).WithClass("tween-data__property", "tween-data__on-tween-started"));
            advancedFoldout.Add(new PropertyField(onTweenUpdated).WithClass("tween-data__property", "tween-data__on-tween-updated"));
            advancedFoldout.Add(new PropertyField(onTweenCompleted).WithClass("tween-data__property", "tween-data__on-tween-completed"));
            
            view.Add(advancedFoldout);
            
            return view;

            void UpdateValues()
            {
                previewFunction.Rebuild();
                convergenceLabel.text = $"⇢ {previewFunction.ComputedXAxisEnd:F2}s";
                omegaLabel.text = $"Ω {omegaProvider.Value:F2} rad/s";
                zetaLabel.text = $"ζ {dampControlProvider.Value:F2}";
                
            }
        }

        private static EasingFunctionCell BuildFunctionPreview(SerializedProperty omegaProp, SerializedProperty dampControlProp, SerializedProperty delayProp)
        {
            if(omegaProp.GetValue() is not IValueProvider<float> omegaProvider 
               || dampControlProp.GetValue() is not IValueProvider<float> dampControlProvider
               || delayProp.GetValue() is not IValueProvider<float> delayProvider)
            {
                return null;
            }
                
            var initialPosition = 0f;
            var initialVelocity = 0f;
            var targetPosition = 1f;

            var targetFunction = new EasingFunction("Target", v => v <= delayProvider.Value ? initialPosition : targetPosition);
            var function = new EasingFunction("Tracker Preview", v => v <= delayProvider.Value ? initialPosition : TweenSystem.DampedHarmonic.MotionToTarget(v - delayProvider.Value, initialPosition, initialVelocity, targetPosition, omegaProvider.Value, dampControlProvider.Value));
            // var function = new EasingFunction("Tracker Preview", v => omegaProvider.Value * v);
            var easeFunctionCell = new EasingFunctionCell(function, null, false, targetFunction);
            easeFunctionCell.AddToClassList("tween-data__preview");
            easeFunctionCell.pickingMode = PickingMode.Ignore;
            easeFunctionCell.AutoAdjust = true;
            easeFunctionCell.MeasurementUnit = "s";
            easeFunctionCell.ShowXAxisLabels = true;
            easeFunctionCell.CurvePointsCount = 150;

            return easeFunctionCell;
        }
    }
}
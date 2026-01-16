using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using Postica.Common;
using UnityEditor.UIElements;

namespace Postica.BindingSystem.Tweening
{
    [CustomPropertyDrawer(typeof(EaseModifier<>.Data), true)]
    public class TweenModifierDataDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!property.IsAlive())
            {
                return new Label("Error: Property is corrupted or not found.");
            }
            
            // Properties
            var onTargetChangeProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.onTargetChange));
            var originTypeProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.originType));
            var originProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.origin));
            
            // Tween related
            var directionProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.direction));
            var tweenFunctionProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.tweenFunction));
            var easeTypeProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.easeType));
            var customEaseProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.customEase));
            
            // Time related
            var timeScaleProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.time));
            var delayProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.delay));
            var durationProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.duration));
            var repeatCountProp = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.repeatCount));
            
            // Events
            var onTweenStarted = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.onEaseStarted));
            var onTweenUpdated = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.onEaseUpdated));
            var onTweenCompleted = property.FindPropertyRelative(nameof(EaseModifier<int>.Data.onEaseCompleted));
            
            var view = new EnhancedFoldout().AddTweensStyle().WithClass("tween-data");
            view.text = "Tween";
            view.value = false;
            view.viewDataKey = "tween-data-" + property.propertyPath;

            var button = new DropdownButton()
            {
                value = ((FunctionType)tweenFunctionProp.enumValueIndex, (EaseType)easeTypeProp.enumValueIndex) switch
                {
                    (FunctionType.Linear, _) => "Linear",
                    (FunctionType.Custom, _) => "Custom",
                    _ => Enum.GetName(typeof(EaseType), easeTypeProp.enumValueIndex) + Enum.GetName(typeof(FunctionType), tweenFunctionProp.enumValueIndex),
                },
                focusable = false,
            }.WithClass("tween-data__button", "tween-data__button--select-ease");
            button.clicked += () => EasingFunctionsWindow.Show(GetRectForEaseWindow(button),
                f =>
                {
                    tweenFunctionProp.enumValueIndex = (int)f.FunctionType;
                    easeTypeProp.enumValueIndex = (int)f.EaseType;

                    button.value = f.Name;
                    tweenFunctionProp.serializedObject.ApplyModifiedProperties();
                },
                ((FunctionType)tweenFunctionProp.enumValueIndex, (EaseType)easeTypeProp.enumValueIndex),
                customEaseProp.animationCurveValue.Evaluate);
            view.restOfHeader.Add(button);
            
            // view.restOfHeader.Add(new PropertyField{ label = "", focusable = false}.EnsureBind(easeTypeProp).WithClass("tween-data__ease-popup"));
            // view.restOfHeader.Add(new PropertyField{ label = "", focusable = false}.EnsureBind(tweenFunctionProp).WithClass("tween-data__ease-popup"));
            view.restOfHeader.Add(new PropertyField{ label = "", focusable = false}.EnsureBind(directionProp).WithClass("tween-data__ease-popup"));
            
            view.Add(new PropertyField(customEaseProp).WithClass("tween-data__property", "tween-data__custom-ease").VisibleWhen(() => tweenFunctionProp.IsAlive() && tweenFunctionProp.enumValueIndex == (int)FunctionType.Custom));
            view.Add(new PropertyField(delayProp).WithClass("tween-data__property", "tween-data__delay"));
            view.Add(new PropertyField(durationProp).WithClass("tween-data__property", "tween-data__duration"));
            view.Add(new PropertyField(repeatCountProp).WithClass("tween-data__property", "tween-data__repeat-count"));
                
            var advancedFoldout = new Foldout
            {
                text = "Advanced",
                value = false,
                viewDataKey = "tween-advanced-" + property.propertyPath
            }.WithClass("tween-data__advanced");
            advancedFoldout.Add(new PropertyField(timeScaleProp).WithClass("tween-data__property", "tween-data__time-scale"));
            advancedFoldout.Add(new PropertyField(onTargetChangeProp).WithClass("tween-data__property", "tween-data__on-target-change"));
            advancedFoldout.Add(new PropertyField(originTypeProp).WithClass("tween-data__property", "tween-data__origin-type"));
            advancedFoldout.Add(new PropertyField(originProp).WithClass("tween-data__property", "tween-data__origin").VisibleWhen(() => originTypeProp.IsAlive() && originTypeProp.enumValueIndex == (int)TweenOriginType.PreciseOrigin));
            advancedFoldout.Add(new VisualElement().WithClass("tween-data__separator"));
            advancedFoldout.Add(new PropertyField(onTweenStarted).WithClass("tween-data__property", "tween-data__on-tween-started"));
            advancedFoldout.Add(new PropertyField(onTweenUpdated).WithClass("tween-data__property", "tween-data__on-tween-updated"));
            advancedFoldout.Add(new PropertyField(onTweenCompleted).WithClass("tween-data__property", "tween-data__on-tween-completed"));
            
            view.Add(advancedFoldout);
            
            return view;
            
            Rect GetRectForEaseWindow(VisualElement element)
            {
                var rect = GUIUtility.GUIToScreenRect(element.worldBound);
                var viewRect = GUIUtility.GUIToScreenRect(view.worldBound);
                rect.x = viewRect.x;
                rect.width = viewRect.width;
                return rect;
            }
        }
    }
}
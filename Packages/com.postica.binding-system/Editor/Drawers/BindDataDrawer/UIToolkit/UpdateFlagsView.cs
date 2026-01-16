using System;
using System.Collections.Generic;
using Postica.Common;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal class UpdateFlagsView : VisualElement, SmartDropdown.IBuildingBlock
        {
            private BindData.BitFlags _flags;
            private readonly Action<BindData.BitFlags, float> _onChange;
            
            private float _interval;
            private VisualElement _intervalControl;
            
            private enum UpdateType
            {
                Frames,
                Seconds
            }

            public SearchTags SearchTags { get; }
            
            public UpdateFlagsView(BindData.BitFlags flags, Action<BindData.BitFlags, float> onChange, float interval)
            {
                _flags = flags;
                _onChange = onChange;
                _interval = interval;
                this.WithClass("bind-flags").AddBSStyle();
            }
            
            public VisualElement GetSearchDrawer(string searchValue, Action closeWindow, bool darkMode)
            {
                return null;
            }

            public void OnPathResolved(SmartDropdown.IPathNode node)
            {
                // Nothing for now
            }

            public VisualElement GetDrawer(Action closeWindow, bool darkMode)
            {
                return BuildUI();
            }

            internal VisualElement BuildUI()
            {
                var container = new VisualElement().WithClass("bind-flags__container");
                var pointsContainer = new VisualElement().WithClass("bind-flags__container__points");
                
                var updateEditor = CreateFlagCell("EDITOR",BindData.BitFlags.UpdateInEditor);
                var updateOnChange = CreateFlagCell("ON CHANGE",BindData.BitFlags.UpdateOnChange, "C");
                var updateOnUpdate = CreateFlagCell("UPDATE", BindData.BitFlags.UpdateOnUpdate);
                var updateOnLateUpdate = CreateFlagCell("LATE UPDATE",BindData.BitFlags.UpdateOnLateUpdate);
                var updateOnFixedUpdate = CreateFlagCell("FIXED UPDATE",BindData.BitFlags.UpdateOnFixedUpdate);
                var updateOnRenderUpdate = CreateFlagCell("RENDER",BindData.BitFlags.UpdateOnPrePostRender, "R");
                var updateOnEnableUpdate = CreateFlagCell("ON ENABLE",BindData.BitFlags.UpdateOnEnable, "✓");
                
                pointsContainer.Add(updateEditor);
                pointsContainer.Add(CreateSeparator());
                pointsContainer.Add(updateOnUpdate);
                pointsContainer.Add(updateOnLateUpdate);
                pointsContainer.Add(updateOnRenderUpdate);
                pointsContainer.Add(updateOnFixedUpdate);
                pointsContainer.Add(CreateSeparator());
                pointsContainer.Add(updateOnChange);
                
                container.Add(pointsContainer);
                
                var secondRowContainer = new VisualElement().WithClass("bind-flags__container__second-row");
                container.Add(secondRowContainer);
                secondRowContainer.Add(updateOnEnableUpdate);
                
                var placeholderLabel = new Label("Selecting multiple update points may hurt performance").WithClass("bind-flags__controls-placeholder");
                secondRowContainer.Add(placeholderLabel);
                
                var noneSelectedLabel = new Label("This bound field won't be updated automatically").WithClass("bind-flags__none-selected-label");
                secondRowContainer.Add(noneSelectedLabel);
                
                var controlsContainer = new VisualElement().WithClass("bind-flags__controls");
                secondRowContainer.Add(controlsContainer);
                

                var optimizedToggle = new Toggle("Optimized Update")
                {
                    tooltip = "If enabled, the update will be optimized to only run when bound <b>source is active</b> and underlying <b>data changes</b> (depending on bind mode).\n" +
                              "This is useful for performance-sensitive bindings but in very rare occasions may skip updates. Use with care.",
                    value = _flags.HasFlag(BindData.BitFlags.OptimizeUpdate),
                }.WithClass("bind-flags__control", "bind-flags__field-toggle", "bind-flags__field-toggle--optimized");
                
                optimizedToggle.RegisterValueChangedCallback(e =>
                {
                    _flags = e.newValue ? _flags | BindData.BitFlags.OptimizeUpdate : _flags & ~BindData.BitFlags.OptimizeUpdate;
                    _onChange?.Invoke(_flags, _interval);
                });
                
                controlsContainer.Add(optimizedToggle);
                
                controlsContainer.Add(CreateIntervalControl());
                
                RefreshUpdateFields();
                
                return this.WithChildren(container);
            }

            private VisualElement CreateIntervalControl()
            {
                _intervalControl = new VisualElement()
                    .WithClass("bind-flags__interval-control");

                var label = new Label("Update Every")
                    .WithClass("bind-flags__interval-control__label");
                _intervalControl.Add(label);
                
                var timeIntervalField = new FloatField
                    {
                        value = _interval,
                        tooltip = "The interval (in seconds) at which the update will be called."
                    }.WithClass("bind-flags__interval-control__field", "bind-flags__interval-control__field--time");

                var frameIntervalField = new IntegerField
                    {
                        value = (int)_interval,
                        tooltip = "The number of frames between updates."
                    }.WithClass("bind-flags__interval-control__field", "bind-flags__interval-control__field--frames");

                var typeField = new PopupField<UpdateType>(new List<UpdateType>
                {
                    UpdateType.Frames,
                    UpdateType.Seconds,
                }, 
                    _flags.HasFlag(BindData.BitFlags.UseTimeUpdateInterval) ? 1 : 0,
                    s => GetIntervalValue() == 1 ? s.ToString()[..^1] : s.ToString(),
                    s => GetIntervalValue() == 1 ? s.ToString()[..^1] : s.ToString())
                {
                    tooltip = "Select the type of interval for updates.",
                }.WithClass("bind-flags__interval-control__type");;
                
                
                timeIntervalField.RegisterValueChangedCallback(e =>
                {
                    _interval = Mathf.Max(e.newValue, 0);
                    timeIntervalField.SetValueWithoutNotify(_interval);
                    typeField.SetValueWithoutNotify(UpdateType.Frames);
                    typeField.SetValueWithoutNotify(UpdateType.Seconds);
                    _onChange?.Invoke(_flags, _interval);
                });
                
                frameIntervalField.RegisterValueChangedCallback(e =>
                {
                    _interval = Mathf.Max(Mathf.CeilToInt(e.newValue), 0);
                    frameIntervalField.SetValueWithoutNotify((int)_interval);
                    typeField.SetValueWithoutNotify(UpdateType.Seconds);
                    typeField.SetValueWithoutNotify(UpdateType.Frames);
                    _onChange?.Invoke(_flags, _interval);
                });
                
                typeField.RegisterValueChangedCallback(e =>
                {
                    _flags = e.newValue == UpdateType.Seconds 
                        ? _flags | BindData.BitFlags.UseTimeUpdateInterval 
                        : _flags & ~BindData.BitFlags.UseTimeUpdateInterval;
                    
                    _interval = e.newValue == UpdateType.Frames
                        ? frameIntervalField.value 
                        : timeIntervalField.value; // Reset to 1 second if switching to frame interval
                    
                    UpdateCorrectClasses();
                    _onChange?.Invoke(_flags, _interval);
                });
                
                _intervalControl.Add(timeIntervalField);
                _intervalControl.Add(frameIntervalField);
                _intervalControl.Add(typeField);
                
                UpdateCorrectClasses();
                
                return _intervalControl;

                void UpdateCorrectClasses()
                {
                    _intervalControl.ClearClassList();
                    _intervalControl.AddToClassList("bind-flags__control");
                    _intervalControl.AddToClassList("bind-flags__interval-control");
                    _intervalControl.AddToClassList(_flags.HasFlag(BindData.BitFlags.UseTimeUpdateInterval) 
                        ? "bind-flags__interval-control--time" 
                        : "bind-flags__interval-control--frames");
                }
                
                float GetIntervalValue()
                {
                    return _flags.HasFlag(BindData.BitFlags.UseTimeUpdateInterval) 
                        ? timeIntervalField.value 
                        : frameIntervalField.value;
                }
            }

            private VisualElement CreateSeparator()
            {
                return new VisualElement().WithClass("bind-flags__separator");
            }

            private Toggle CreateFlagCell(string label, BindData.BitFlags flag, string initial = null)
            {
                var toggle = new Toggle(label)
                {
                    focusable = false,
                    value = _flags.HasFlag(flag),
                    tooltip = flag.GetAttribute<TooltipAttribute>()?.tooltip
                }
                    .WithClass("bind-flags__toggle")
                    .WithChildren(new Label(initial ?? label[0..1]).WithClass("bind-flags__toggle__initial"));

                toggle.RegisterValueChangedCallback(e =>
                {
                    _flags = e.newValue ? _flags | flag : _flags & ~flag;
                    _onChange?.Invoke(_flags, _interval);
                    RefreshUpdateFields();
                });
                return toggle;
            }

            private void RefreshUpdateFields()
            {
                EnableInClassList("bind-flags--full-control", 
                    _flags.HasFlag(BindData.BitFlags.UpdateOnUpdate) ||
                    _flags.HasFlag(BindData.BitFlags.UpdateOnLateUpdate) ||
                    _flags.HasFlag(BindData.BitFlags.UpdateOnFixedUpdate) ||
                    _flags.HasFlag(BindData.BitFlags.UpdateOnPrePostRender) || 
                    _flags.HasFlag(BindData.BitFlags.UpdateOnEnable) ||
                    _flags.HasFlag(BindData.BitFlags.UpdateInEditor));
                EnableInClassList("bind-flags--none",
                    !_flags.HasFlag(BindData.BitFlags.UpdateInEditor) &&
                    !_flags.HasFlag(BindData.BitFlags.UpdateOnChange) &&
                    !_flags.HasFlag(BindData.BitFlags.UpdateOnUpdate) &&
                    !_flags.HasFlag(BindData.BitFlags.UpdateOnLateUpdate) &&
                    !_flags.HasFlag(BindData.BitFlags.UpdateOnFixedUpdate) &&
                    !_flags.HasFlag(BindData.BitFlags.UpdateOnPrePostRender) &&
                    !_flags.HasFlag(BindData.BitFlags.UpdateOnEnable));
                EnableInClassList("bind-flags--with-interval",
                    _flags.HasFlag(BindData.BitFlags.UpdateOnUpdate) ||
                    _flags.HasFlag(BindData.BitFlags.UpdateOnLateUpdate) ||
                    _flags.HasFlag(BindData.BitFlags.UpdateOnFixedUpdate) ||
                    _flags.HasFlag(BindData.BitFlags.UpdateOnPrePostRender));
                
                _intervalControl.SetEnabled(ClassListContains("bind-flags--with-interval"));
            }
        }
    }
}
using Postica.BindingSystem.Accessors;
using Postica.BindingSystem.Reflection;
using Postica.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Postica.BindingSystem.PinningLogic;
using Postica.Common.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        private static readonly Dictionary<(Type bindType, BindMode bindMode), MemberItemsCache> _membersCache = new();
        private const string _lastUsedKey = "Key_LastUsedSources";

        public static void ClearCache()
        {
            _membersCache.Clear();
        }
        
        public static void RegisterLastUsedSource(Object context)
        {
            if(context is Component component)
            {
                context = component.gameObject;
            }

            var lastUsedSources = BindSystem.MetaValues.GetList(_lastUsedKey);
            lastUsedSources.Remove(context);
            lastUsedSources.Insert(0, context);
            while (lastUsedSources.Count > 3)
            {
                lastUsedSources.RemoveAt(lastUsedSources.Count - 1);
            }
        }
        
        public static IEnumerable<Object> GetLastUsedSources()
        {
            return BindSystem.MetaValues.GetSanitizedList(_lastUsedKey);
        }

        private class MemberItemsCache
        {
            private readonly Dictionary<Key, MemberLink> _cache = new();

            public MemberLink GetLink(Type bindType, MemberItem item, BindMode mode)
            {
                var key = new Key(item);
                if (!_cache.TryGetValue(key, out var cache))
                {
                    Profiler.BeginSample("BindData.GetLink");
                    cache = new MemberLink(this, bindType, item, mode);
                    _cache.Add(key, cache);
                    Profiler.EndSample();
                }

                return cache;
            }

            public readonly struct Key : IEquatable<Key>
            {
                private readonly Type type;
                private readonly bool canWrite;
                private readonly bool canRead;

                public Key(MemberItem item)
                {
                    type = item.type;
                    canWrite = item.canWrite;
                    canRead = item.canRead;
                }

                public static implicit operator Key(MemberItem item) => new Key(item);

                public bool Equals(Key other)
                {
                    return type == other.type && canWrite == other.canWrite && canRead == other.canRead;
                }

                public override bool Equals(object obj)
                {
                    return obj is Key other && Equals(other);
                }

                public override int GetHashCode()
                {
                    return HashCode.Combine(type, canWrite, canRead);
                }
            }

            public class MemberLink
            {
                public readonly bool isValid;
                public readonly bool hasConverter;
                public readonly bool hasSafeConverter;

                private readonly Type _bindType;
                private readonly BindMode _mode;
                private readonly MemberItemsCache _owner;
                private readonly IEnumerable<MemberItem> _children;

                private bool? _hasValidChildren;
                private bool _isCheckingChildren;

                public MemberLink(MemberItemsCache owner, Type bindType, MemberItem item, BindMode mode)
                {
                    _bindType = bindType;
                    _children = item.children;
                    _mode = mode;
                    _owner = owner;

                    if (mode == BindMode.ReadWrite && item.canRead && item.canWrite)
                    {
                        bool safeRead = false;
                        bool safeWrite = false;
                        if ((bindType.IsAssignableFrom(item.type) || (hasConverter =
                                ConvertersFactory.HasConversion(item.type, bindType, out safeRead)))
                            && (item.type.IsAssignableFrom(bindType) || (hasConverter |=
                                ConvertersFactory.HasConversion(bindType, item.type, out safeWrite))))
                        {
                            isValid = true;
                            hasSafeConverter = safeRead || safeWrite;
                        }
                    }
                    else if (mode == BindMode.Read && item.canRead)
                    {
                        if (bindType.IsAssignableFrom(item.type))
                        {
                            isValid = true;
                            hasConverter = false;
                            hasSafeConverter = false;
                        }
                        else if (ConvertersFactory.HasConversion(item.type, bindType, out var safe))
                        {
                            isValid = true;
                            hasConverter = true;
                            hasSafeConverter = safe;
                        }
                    }
                    else if (mode == BindMode.Write && item.canWrite)
                    {
                        if (item.type.IsAssignableFrom(bindType))
                        {
                            isValid = true;
                            hasConverter |= false;
                            hasSafeConverter |= false;
                        }
                        else if (ConvertersFactory.HasConversion(bindType, item.type, out var safe))
                        {
                            isValid = true;
                            hasConverter = true;
                            hasSafeConverter |= safe;
                        }
                    }
                }

                public bool HasValidChildren()
                {
                    try
                    {
                        Profiler.BeginSample("BindData.LinkHasValidChildren");
                        if (_hasValidChildren.HasValue)
                        {
                            return _hasValidChildren.Value;
                        }

                        _isCheckingChildren = true;
                        foreach (var child in _children)
                        {
                            Key childKey = child;
                            if (!_owner._cache.TryGetValue(childKey, out MemberLink link))
                            {
                                link = new MemberLink(_owner, _bindType, child, _mode);
                                _owner._cache.Add(childKey, link);
                            }

                            if (link._isCheckingChildren)
                            {
                                continue;
                            }

                            if (link.isValid || link.HasValidChildren())
                            {
                                _hasValidChildren = true;
                                _isCheckingChildren = false;
                                return true;
                            }
                        }

                        _hasValidChildren = false;
                        _isCheckingChildren = false;
                        return false;
                    }
                    finally
                    {
                        _isCheckingChildren = false;
                        Profiler.EndSample();
                    }
                }
            }
        }

        private SmartDropdown BuildOptionsMenu(PropertyData data, Action onSelect = null, Action onReset = null)
        {
            var currentWindow = EditorWindow.focusedWindow;
            var menu = new SmartDropdown(false, "Bind Menu");
            if (data.hasCustomUpdates)
            {
                menu.AddSeparator("Update Points");
                menu.Add("Update On", new UpdateFlagsView((BindData.BitFlags)data.properties.flags.intValue, (flags, interval) =>
                {
                    data.properties.flags.intValue = (int)flags;
                    data.properties.updateInterval.floatValue = interval;
                    data.Refresh();
                    ApplyChanges(data);
                }, data.properties.updateInterval.floatValue));
            }

            menu.AddSeparator("Settings");
            menu.Add("Live Debug",
                BindData.BitFlags.LiveDebug.IsFlagOf(data.properties.flags.intValue),
                v =>
                {
                    if (_isUIToolkit)
                    {
                        data.properties.flags.intValue =
                            BindData.BitFlags.LiveDebug.EnableFlagIn(v, data.properties.flags.intValue);
                        data.Refresh();
                        ApplyChanges(data);
                    }
                    else
                    {
                        data.preRenderAction = () =>
                        {
                            data.properties.flags.intValue =
                                BindData.BitFlags.LiveDebug.EnableFlagIn(v, data.properties.flags.intValue);
                            data.Refresh();
                            currentWindow.Repaint();
                        };
                        GUITools.InspectorWindow.Repaint();
                    }
                }
            );
            if (!data.hasCustomUpdates)
            {
                menu.Add("Auto Update",
                    BindData.BitFlags.AutoUpdate.IsFlagOf(data.properties.flags.intValue),
                    v =>
                    {
                        if (_isUIToolkit)
                        {
                            data.properties.flags.intValue =
                                BindData.BitFlags.AutoUpdate.EnableFlagIn(v, data.properties.flags.intValue);
                            data.Refresh();
                            ApplyChanges(data);
                        }
                        else
                        {
                            data.preRenderAction = () =>
                            {
                                data.properties.flags.intValue =
                                    BindData.BitFlags.AutoUpdate.EnableFlagIn(v, data.properties.flags.intValue);
                                data.Refresh();
                                currentWindow.Repaint();
                            };
                            GUITools.InspectorWindow.Repaint();
                        }
                    }
                );
            }
            // else
            // {
            //     
            //     menu.Add("Keep Original Value", () => { });
            // }

            if (data.properties.valueChangedEvent != null)
            {
                menu.Add("Value Changed Event",
                    data.enableEvents,
                    v =>
                    {
                        if (_isUIToolkit)
                        {
                            data.enableEvents = v;
                            data.Refresh();
                            ApplyChanges(data);
                        }
                        else
                        {
                            data.preRenderAction = () =>
                            {
                                data.enableEvents = v;
                                data.Refresh();
                                currentWindow.Repaint();
                            };
                            GUITools.InspectorWindow.Repaint();
                        }
                    }
                );
            }

            if (data.reroute.isValid)
            {
                AddRerouteOption(data, onSelect, menu);
            }

            menu.Add("Reset Values", "", ObjectIcon.EditorIcons.Refresh, () => ExecuteAction(data, () =>
            {
                data.properties.flags.ResetValue();
                data.properties.mainParameterIndex.ResetValue();
                data.properties.valueChangedEvent?.ResetValue();
                data.properties.mode.ResetValue();
                data.properties.modifiers.ResetValue();
                data.properties.parameters?.ResetValue();
                data.properties.path.ResetValue();
                data.properties.pPath.ResetValue();
                data.properties.readConverter.ResetValue();
                data.properties.target.ResetValue();
                data.properties.writeConverter.ResetValue();
                data.isPathPreview = false;
                data.canPathPreview = false;
                data.pathPreviewIsEditing = false;

                if (data.hasCustomUpdates)
                {
                    data.properties.flags.intValue = (int)BindData.BitFlags.UpdateOnUpdate;
                }

                data.Invalidate();

                if (onReset != null)
                {
                    onReset();
                }
                else
                {
                    onSelect?.Invoke();
                }
            }));

            var (fromType, toType) = GetTypeMapping(data);
            var hasPreConversionModifiers = TryAddPreConversionModifiers(data, toType, fromType, menu, onSelect);
            
            var modifierTemplates = ModifiersFactory.GetTemplatesFor(data.bindType);
            if (!(modifierTemplates?.Count > 0)) return menu;

            var addedPaths = new Dictionary<string, ModifiersFactory.IModifierTemplate>();
            var mode = (BindMode)data.properties.mode.enumValueIndex;
            if (!hasPreConversionModifiers)
            {
                menu.AddSeparator("Add Modifier", data.isMultipleTargets ? $"Add Modifier To ALL Objects" : $"Add Modifier");
            }
            else
            {
                menu.AddSeparator("Add Modifier", data.isMultipleTargets ? $"Add <b>POST-Conversion</b> Modifier To ALL Objects" : $"Add <b>POST-Conversion</b> Modifier",
                    (new Image
                    {
                        image = ObjectIcon.GetFor(data.bindType),
                        tooltip = data.bindType.FullName,
                    }.WithClass("sd-separator-icon"), false));
            }
            
            foreach (var mt in modifierTemplates)
            {
                if (!mt.TryGetBindModeFor(data.bindType, out var modifierMode) || !modifierMode.IsCompatibleWith(mode))
                {
                    continue;
                }

                var setMode = mode;
                var icon = ObjectIcon.GetFor(mt.OriginalType);
                if (!icon)
                {
                    icon = ObjectIcon.EditorIcons.CSharpScript;
                }

                var typename = "";
                var path = mt.ModifierId;
                if (addedPaths.TryGetValue(mt.ModifierId, out var otherMT))
                {
                    if (!otherMT.AllowSimilarModifiers)
                    {
                        continue;
                    }

                    typename = mt.OriginalType.UserFriendlyName();
                    var index = 2;
                    while (addedPaths.ContainsKey(path + ' ' + index))
                    {
                        index++;
                    }

                    path = path + ' ' + index;
                    addedPaths.Add(path, mt);
                }
                else
                {
                    addedPaths.Add(path, mt);
                }

                mt.OriginalType.TryGetDescription(out var description);
                menu.Add(path, typename, icon, description, () => ExecuteAction(data, () =>
                {
                    if (data.isMultipleTargets)
                    {
                        var propertyPath = data.properties.property.propertyPath;
                        foreach (var t in data.serializedObject.targetObjects)
                        {
                            using (var so = new SerializedObject(t))
                            {
                                var soData = new PropertyData(so.FindProperty(propertyPath));
                                var soSetMode = (BindMode)soData.properties.mode.enumValueIndex;
                                AddModifierToProperty(soData, mt, soSetMode, data.bindType, false);
                                so.ApplyModifiedProperties();
                            }
                        }

                        data.serializedObject.Update();
                        data.commonModifiers.Update();
                    }
                    else
                    {
                        AddModifierToProperty(data, mt, setMode, data.bindType);
                    }

                    //NotifyChanges(data);

                    onSelect?.Invoke();
                }));
            }

            return menu;
        }

        private bool TryAddPreConversionModifiers(PropertyData data, Type toType, Type fromType, SmartDropdown menu, Action onSelect)
        {
            if (toType?.IsAssignableFrom(fromType) == true) return false;
            
            var preModifierTemplates = ModifiersFactory.GetTemplatesFor(fromType);
            if (!(preModifierTemplates?.Count > 0)) return false;
            
            var addedPaths = new Dictionary<string, ModifiersFactory.IModifierTemplate>();
            var mode = (BindMode)data.properties.mode.enumValueIndex;
            
            menu.AddSeparator("Add Pre-Modifier", data.isMultipleTargets ? $"Add <b>PRE-Conversion</b> Modifier To ALL Objects" : $"Add <b>PRE-Conversion</b> Modifier",
                (new Image
                {
                    image = ObjectIcon.GetFor(fromType),
                    tooltip = fromType.FullName,
                }.WithClass("sd-separator-icon"), false));
            foreach (var mt in preModifierTemplates)
            {
                if (!mt.TryGetBindModeFor(fromType, out var modifierMode) || !modifierMode.IsCompatibleWith(mode))
                {
                    continue;
                }

                var setMode = mode;
                var icon = ObjectIcon.GetFor(mt.OriginalType);
                if (!icon)
                {
                    icon = ObjectIcon.EditorIcons.CSharpScript;
                }

                var typename = "";
                var path = mt.ModifierId + " (Pre)";
                if (addedPaths.TryGetValue(mt.ModifierId, out var otherMT))
                {
                    if (!otherMT.AllowSimilarModifiers)
                    {
                        continue;
                    }

                    typename = mt.OriginalType.UserFriendlyName();
                    var index = 2;
                    while (addedPaths.ContainsKey(path + ' ' + index))
                    {
                        index++;
                    }

                    path = path + ' ' + index;
                    addedPaths.Add(path, mt);
                }
                else
                {
                    addedPaths.Add(path, mt);
                }

                mt.OriginalType.TryGetDescription(out var description);
                menu.Add(path, typename, icon, description, () => ExecuteAction(data, () =>
                {
                    if (data.isMultipleTargets)
                    {
                        var propertyPath = data.properties.property.propertyPath;
                        foreach (var t in data.serializedObject.targetObjects)
                        {
                            using (var so = new SerializedObject(t))
                            {
                                var soData = new PropertyData(so.FindProperty(propertyPath));
                                var soSetMode = (BindMode)soData.properties.mode.enumValueIndex;
                                AddPreModifierToProperty(soData, mt, soSetMode, fromType, false);
                                so.ApplyModifiedProperties();
                            }
                        }

                        data.serializedObject.Update();
                        data.commonModifiers.Update();
                    }
                    else
                    {
                        AddPreModifierToProperty(data, mt, setMode, data.bindType);
                    }

                    //NotifyChanges(data);

                    onSelect?.Invoke();
                }));
            }
            
            return true;
        }

        private static void AddRerouteOption(PropertyData data, Action onSelect, SmartDropdown menu)
        {
            var fieldName = data.reroute.from;
            var fieldType = data.reroute.fieldType ?? data.bindType;
            if (fieldType == null)
            {
                return;
            }
            
            var type = data.reroute.type;
            var to = data.reroute.to;
            
            var group = menu.GetGroupAt($"Reroute {fieldName}");
            group.Icon = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "_bsicons/route" : "_bsicons/route_lite");
            group.Description = to;
            group.Name = "Reroute " + fieldName.RT().Color(BindColors.Primary);
            group.AllowDuplicates = true;
            if (!string.IsNullOrEmpty(to) && to != fieldName)
            {
                group.Add("Remove Reroute", new DropdownItem("Remove Reroute", "", () =>
                    {
                        FieldRoutes.Remove(type, fieldName);
                        data.reroute.To(null);
                        onSelect?.Invoke();
                    }, canAlterGroup: false, icon: Resources.Load<Texture2D>("_bsicons/remove"))
                    .OnPreRender(v => v.Q<Label>(null, "sd-label-main").style.color = Color.red.Green(0.25f).Blue(0.25f)));
            }

            var members = ReflectionFactory.GetGroupFor(type)
                .GetRootChildren();
            var separatorAdded = false;
            var bestCandidateSet = false;
            
            foreach (var member in members.OrderBy(m => m.name.AdvancedSimilarityDistance(fieldName)))
            {
                if (!fieldType.IsAssignableFrom(member.type))
                {
                    continue;
                }

                var memberName = member.memberInfo?.Name ?? member.name;
                if(memberName == fieldName)
                {
                    continue;
                }

                if (!separatorAdded)
                {
                    separatorAdded = true;
                    group.Add("__candidates__", new SeparatorItem("Candidates"));
                }
                
                var memberKind = member.memberInfo switch
                {
                    FieldInfo => "Field",
                    PropertyInfo => "Property",
                    MethodInfo => "Method",
                    _ => "Member"
                };

                var item = new DropdownItem(memberName,
                    memberKind, () =>
                    {
                        FieldRoutes.Add(type, fieldName, memberName);
                        data.reroute.To(memberName);
                        onSelect?.Invoke();
                    }, ObjectIcon.GetFor(member.type), canAlterGroup: false);

                if (!bestCandidateSet)
                {
                    bestCandidateSet = true;
                    if (member.name.AdvancedSimilarityDistance(fieldName) < 5f)
                    {
                        item.SecondLabel = "Best".RT().Color(BindColors.Primary) + " - " + item.SecondLabel;
                        item.OnPreRender(v => v.AddToClassList("sd-best-match"));
                    }
                }
                
                group.Add(memberName, item, isSelected: to == memberName);
            }
            
            if(group.Children.Length == 0)
            {
                menu.Remove(group.Path);
            }
        }

        private static void AddModifierToProperty(PropertyData data, ModifiersFactory.IModifierTemplate mt,
            BindMode setMode, Type bindType, bool refreshData = true)
        {
            var modifiersProp = data.properties.modifiers;
            var elementIndex = modifiersProp.arraySize;
            modifiersProp.InsertArrayElementAtIndex(elementIndex);
            var modifier = mt.Create(setMode);
            if (modifier is IObjectModifier objectModifier)
            {
                objectModifier.TargetType = bindType;
            }

            modifiersProp.GetArrayElementAtIndex(elementIndex).managedReferenceValue = modifier;
            if (refreshData)
            {
                data.Refresh();
            }
        }
        
        private static void AddPreModifierToProperty(PropertyData data, ModifiersFactory.IModifierTemplate mt,
            BindMode setMode, Type bindType, bool refreshData = true)
        {
            var modifiersProp = data.properties.preModifiers;
            var elementIndex = modifiersProp.arraySize;
            modifiersProp.InsertArrayElementAtIndex(elementIndex);
            var modifier = mt.Create(setMode);
            if (modifier is IObjectModifier objectModifier)
            {
                objectModifier.TargetType = bindType;
            }

            modifiersProp.GetArrayElementAtIndex(elementIndex).managedReferenceValue = modifier;
            if (refreshData)
            {
                data.Refresh();
            }
        }

        private SmartDropdown HandleShowPopup(PropertyData data, string currentPath, SerializedProperty property)
        {
            if (data.isMultipleTargets && data.commonSource.commonType == null)
            {
                return new SmartDropdown(false,
                    data.bindType != null ? $"Bind a {GetFriendlyName(data.bindType)}" : "Bind Source");
            }

            if (data.isMultipleTargets
                && data.commonSource.commonValue == null
                && data.commonSource.commonType != null)
            {
                if (typeof(GameObject) == data.commonSource.commonType ||
                    typeof(Component).IsAssignableFrom(data.commonSource.commonType))
                {
                    var targets = new List<GameObject>();
                    data.commonSource.ForEach((t, p, v) =>
                    {
                        if (TryGetGameObject(v, out var go))
                        {
                            targets.Add(go);
                        }
                    });

                    return HandleShowPopup(data, targets.ToArray(), property);
                }
            }

            var target = data.isMultipleTargets && data.commonSource.commonValue
                ? data.commonSource.commonValue
                : data.properties.target.objectReferenceValue;

            currentPath = InverseTransformPath(target, currentPath);

            if (TryGetGameObject(target, out var go))
            {
                return HandleShowPopup(data, go, target, currentPath, property);
            }

            // Generic Unity object logic
            var menuTitle = data.bindType != null ? $" Bind <b>{data.bindType.UserFriendlyName()}</b> " : " Bind ";
            if (target)
            {
                menuTitle += $"from <b>{target.name}</b>";
            }

            var dropDown = new SmartDropdown(true, menuTitle);
            dropDown.GetGroupAt("").Icon = ObjectIcon.GetFor(data.bindType);
            if (data.isMultipleTargets && data.commonType == null)
            {
                return dropDown;
            }

            if (target != null)
            {
                var prefixPath = target.GetType().Name;
                AppendToMenu(dropDown, target, null, prefixPath, prefixPath, currentPath, property);
            }

            AppendLastUsedSources(dropDown, property, target);

            AppendPinnedPaths(dropDown, property);
            
            AppendProvidersPaths(dropDown, target, currentPath, property);
            
            return dropDown;
        }

        private SmartDropdown HandleShowPopup(PropertyData data, GameObject[] targets, SerializedProperty property)
        {
            var menuTitle = data.bindType != null ? $" Bind <b>{data.bindType.UserFriendlyName()}</b> " : " Bind ";
            menuTitle += $"from <b>{targets.Length} targets</b>";
            var menu = new SmartDropdown(true, menuTitle);
            menu.GetGroupAt("").Icon = ObjectIcon.GetFor(data.bindType);

            var components = targets[0].GetComponents<Component>().ToList();

            for (int i = 1; i < targets.Length; i++)
            {
                var newComponents = targets[i].GetComponents<Component>();
                for (int j = 0; j < components.Count; j++)
                {
                    var componentType = components[j].GetType();
                    if (newComponents.Any(c => componentType.IsAssignableFrom(c.GetType())))
                    {
                        continue;
                    }

                    var baseComponent = newComponents.FirstOrDefault(c => c.GetType().IsAssignableFrom(componentType));
                    if (baseComponent != null)
                    {
                        components[j] = baseComponent;
                    }
                    else
                    {
                        components.RemoveAt(j--);
                    }
                }
            }

            AppendToMenu(menu, null, typeof(GameObject), "GameObject", "GameObject", "", property);

            menu.AddSeparator("Components");
            foreach (var component in components)
            {
                var prefixPath = component.GetType().Name;
                AppendToMenu(menu, component, null, prefixPath, prefixPath, "", property);
            }
            
            AppendLastUsedSources(menu, property, targets[0]);

            AppendPinnedPaths(menu, property);
            
            // AppendProvidersPaths(menu, originalTarget, "", property);

            return menu;
        }

        private SmartDropdown HandleShowPopup(PropertyData data, GameObject target, Object originalTarget,
            string currentPath, SerializedProperty property)
        {
            var menuTitle = data.bindType != null ? $" Bind <b>{data.bindType.UserFriendlyName()}</b> " : " Bind ";
            if (target)
            {
                menuTitle += $"from <b>{target.name}</b>";
            }

            var menu = new SmartDropdown(true, menuTitle);
            menu.GetGroupAt("").Icon = ObjectIcon.GetFor(data.bindType);

            var components = target.GetComponents<Component>();

            AppendToMenu(menu, target, null, "GameObject", "GameObject", originalTarget == target ? currentPath : "",
                property);

            menu.AddSeparator("Components");
            foreach (var component in components)
            {
                var prefixPath = component.GetType().Name;
                AppendToMenu(menu, component, null, prefixPath, prefixPath,
                    originalTarget == component ? currentPath : "", property);
            }
            
            AppendLastUsedSources(menu, property, target);
            
            AppendPinnedPaths(menu, property);

            AppendProvidersPaths(menu, originalTarget, currentPath, property);

            return menu;
        }
        
        
        private void AppendLastUsedSources(SmartDropdown dropDown, SerializedProperty property, Object target)
        {
            var canShowLastUsed = BindingSettings.Current.ShowLastUsedSources;
            if (!canShowLastUsed && target)
            {
                return;
            }
            
            var lastUsedSource = GetLastUsedSources();
            if (lastUsedSource?.Any() == true)
            {
                dropDown.AddSeparator("Last Used", null, out var removeSeparator);
                var validSources = 0;

                foreach (var lastUsed in GetLastUsedSources())
                {
                    if (lastUsed == target || (target is Component c && c.gameObject == lastUsed))
                    {
                        continue;
                    }
                    
                    validSources++;
                    var prefixPath = lastUsed.name + "   "; // <-- this is to avoid conflicts with other items
                    var prefixSetPath = lastUsed.GetType().Name;
                    
                    var group = dropDown.GetGroupAt(prefixPath);

                    if (lastUsed is not GameObject go)
                    {
                        AppendToMenu(dropDown, lastUsed, null, prefixPath, prefixSetPath, "", property);
                        AddGroupPreview(group, lastUsed);
                        continue;
                    }
                    
                    group.Icon = ObjectIcon.GetFor(go);
                    group.Description = "GameObject";
                    AppendToMenu(dropDown, go, null, prefixPath + SmartDropdown.Separator + "GameObject", prefixSetPath, "", property);
                    
                    AddGroupPreview(group, lastUsed);
                    
                    var components = go.GetComponents<Component>();
                    
                    if (components.Length <= 0) continue;

                    var validComponentsCount = 0;
                    dropDown.AddSeparator(prefixPath + SmartDropdown.Separator + "Components", "Components", out var removeComponentsSeparator);
                    foreach (var component in components)
                    {
                        var componentType = component.GetType();
                        var prefixComponentPath = prefixPath + SmartDropdown.Separator + componentType.Name;
                        var prefixComponentSetPath = componentType.Name;
                        if (AppendToMenu(dropDown, component, null, prefixComponentPath, prefixComponentSetPath, "",
                                property))
                        {
                            validComponentsCount++;
                        }
                    }
                    
                    if (validComponentsCount > 0)
                    {
                        group.Description = validComponentsCount + " Components";
                    }
                    else
                    {
                        removeComponentsSeparator?.Invoke();
                    }
                }
                
                if (validSources <= 0)
                {
                    removeSeparator();
                }
            }

            void AddGroupPreview(SmartDropdown.IPathGroup group, Object lastUsed)
            {
                if (!target)
                {
                    group.UIElementFactory =
                        new DropdownPreviewGroup(lastUsed.GetType(), lastUsed, false).CreateGroupUI;
                }
            }
        }

        private void AppendProvidersPaths(SmartDropdown menu, Object originalTarget, string currentPath,
            SerializedProperty property)
        {
            var separatorAdded = false;
            var accessorProviders = BindTypesCache.GetAllAccessorProviders();
            var isGameObject = TryGetGameObject(originalTarget, out var gameObject);

            var propertyPath = property.propertyPath;
            var data = GetData(property);
            var bindType = data.bindType;
            var mode = data.properties.BindMode;
            var currentId = data.properties.path.stringValue;
            var editedParentsPaths = new HashSet<string>();

            foreach (var provider in accessorProviders)
            {
                var paths = provider.Value.GetAvailablePaths(originalTarget);
                if (!paths.Any())
                {
                    continue;
                }

                if (!separatorAdded)
                {
                    menu.AddSeparator("Other Providers");
                    separatorAdded = true;
                }

                editedParentsPaths.Clear();

                var icon = provider.Value is Accessors.IComponentAccessorProvider cp
                    ? ObjectIcon.GetFor(cp.GetComponent(gameObject))
                    : ObjectIcon.GetFor(provider.Value);

                menu.EditGroup(provider.Key, provider.Value.Id, icon, "Provider");
                editedParentsPaths.Add(provider.Key);

                var target = isGameObject && provider.Value is Accessors.IComponentAccessorProvider cProvider
                    ? cProvider.GetComponent(gameObject)
                    : originalTarget;

                var prefixPath = provider.Value.Id + '/';
                var currentTargetPath = currentPath;
                // if (currentPath.StartsWith(target.GetType().Name))
                // {
                //     currentTargetPath = currentPath.Substring(target.GetType().Name.Length + 1);
                // }

                foreach (var path in paths)
                {
                    var pathMode = path.BindMode;
                    if (!mode.IsCompatibleWith(pathMode))
                    {
                        continue;
                    }

                    var menuPath = prefixPath + path.MenuPath;

                    if (menu.ContainsPath(menuPath))
                    {
                        continue;
                    }

                    bool IsTypeCompatible(AccessorPath path)
                    {
                        var isTypeCompatible = data.bindType?.IsAssignableFrom(path.Type) == true;

                        if (bindType != null && !bindType.IsAssignableFrom(path.Type))
                        {
                            if (mode.CanWrite() && ConvertersFactory.HasConversion(bindType, path.Type, out _))
                            {
                                isTypeCompatible = true;
                            }
                            else if (mode.CanRead() && ConvertersFactory.HasConversion(path.Type, bindType, out _))
                            {
                                isTypeCompatible = true;
                            }
                        }

                        return isTypeCompatible;
                    }

                    var isTypeCompatible = IsTypeCompatible(path);
                    if (path.IsSealed && !isTypeCompatible)
                    {
                        continue;
                    }

                    editedParentsPaths.Add(menuPath);

                    bool hasChildren = false;
                    if (!path.IsSealed && path.Type != null)
                    {
                        hasChildren = AppendToMenu(menu, target, path.Object?.GetType() ?? path.Type, menuPath, path.Id,
                            currentTargetPath, property);
                    }

                    if (!hasChildren && !isTypeCompatible)
                    {
                        continue;
                    }

                    SmartDropdown.IBuildingBlock item = null;
                    if (DropdownPreviewItem.CanPreview(path.Object))
                    {
                        item = new DropdownPreviewItem(StringUtility.NicifyName(path.Name), () =>
                        {
                            if (_isUIToolkit)
                            {
                                SetNewValues(propertyPath, target, path.Id, true);
                            }
                            else
                            {
                                data.preRenderAction = () => SetNewValues(propertyPath, target, path.Id, false);
                            }

                            data.accessorsPaths = ToDictionaryNotUnique(paths, p => p.Id);
                        }, path.Object, false);
                    }
                    else
                    {
                        item = new DropdownItem(StringUtility.NicifyName(path.Name), path.Type.Name, () =>
                        {
                            if (_isUIToolkit)
                            {
                                SetNewValues(propertyPath, target, path.Id, true);
                            }
                            else
                            {
                                data.preRenderAction = () => SetNewValues(propertyPath, target, path.Id, false);
                            }

                            data.accessorsPaths = ToDictionaryNotUnique(paths, p => p.Id);
                        }, ObjectIcon.GetFor(path.Type));
                    }

                    if (path.Children.Count > 0 || hasChildren)
                    {
                        var groupUI = new DropdownGroupItem(path.Description ?? path.Type?.UserFriendlyName(), item);
                        menu.EditGroup(menuPath, null, groupUI.CreateGroupUI, item);
                    }
                    else
                    {
                        menu.Add(menuPath, item, currentId == path.Id);
                    }

                    var parent = path.Parent;
                    while (parent != null && !editedParentsPaths.Contains(parent.MenuPath))
                    {
                        editedParentsPaths.Add(parent.MenuPath);

                        if (DropdownPreviewItem.CanPreview(parent.Object))
                        {
                            menu.EditGroup(prefixPath + parent.MenuPath,
                                null,
                                new DropdownPreviewGroup(parent.Type, parent.Object).CreateGroupUI);
                        }
                        else
                        {
                            menu.EditGroup(prefixPath + parent.MenuPath, null, ObjectIcon.GetFor(parent.Type),
                                parent.Type?.GetAliasName());
                        }

                        parent = parent.Parent;
                    }
                }
            }
        }

        private bool AppendToMenu(SmartDropdown menu,
            Object obj,
            Type prefixType,
            string prefixPath,
            string prefixSetPath,
            string currentPath,
            SerializedProperty property,
            Action<Object, string> onSelect = null)
        {
            var data = GetData(property);
            var bindType = data.bindType;
            if (bindType == null)
            {
                return false;
            }

            if (!obj.IsAssignableTo(data.properties.context.objectReferenceValue))
            {
                return false;
            }
            
            var objType = prefixType ?? obj.GetType();
            if (objType.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.Completely)
            {
                return false;
            }

            var propertyPath = property.propertyPath;
            
            var mode = data.properties.BindMode;

            if (!_membersCache.TryGetValue((bindType, mode), out var membersCache))
            {
                membersCache = new MemberItemsCache();
                _membersCache.Add((bindType, mode), membersCache);
            }

            #region [  INNER FUNCTIONS  ]

            Action SetValueOnSelectCallback(string pathToSet)
            {
                return _isUIToolkit
                    ? () =>
                    {
                        onSelect?.Invoke(obj, pathToSet);
                        SetNewValues(propertyPath, obj, pathToSet, true);
                    }
                    : () => GetData(propertyPath).preRenderAction =
                        () =>
                        {
                            onSelect?.Invoke(obj, pathToSet);
                            SetNewValues(propertyPath, obj, pathToSet, false);
                        };
            }

            MemberItemsCache.MemberLink GetCached(MemberItem m) => membersCache.GetLink(bindType, m, mode);

            (bool isValid, bool hasConverter, bool isSafe) GetTypeValidation(Type type, BindMode mode)
            {
                try
                {
                    Profiler.BeginSample("BindData.GetTypeValidation");

                    if (bindType.IsAssignableFrom(type))
                    {
                        return (true, false, true);
                    }

                    var isSafe = false;
                    if (!bindType.IsAssignableFrom(type))
                    {
                        if (mode.CanRead())
                        {
                            if (!ConvertersFactory.HasConversion(type, bindType, out var safe))
                            {
                                return (false, false, false);
                            }

                            isSafe = safe;
                        }

                        if (mode.CanWrite())
                        {
                            if (!ConvertersFactory.HasConversion(bindType, type, out var safe))
                            {
                                return (false, false, false);
                            }

                            isSafe = safe;
                        }
                    }

                    return (true, true, isSafe);
                }
                finally
                {
                    Profiler.EndSample();
                }
            }

            #endregion

            Profiler.BeginSample("BindData.GetMembersForType");
            var reflectionGroup = ReflectionFactory.GetGroupFor(objType);
            Profiler.EndSample();

            var icon = AssetPreview.GetMiniThumbnail(obj) ?? ObjectIcon.GetFor(objType);

            var group = menu.GetGroupAt(prefixPath);
            group.Name = obj is Component ? objType.Name : obj.name;
            group.Icon = icon;
            group.Description = objType.GetAliasName();
            group.AllowDuplicates = true;

            if (!prefixPath.EndsWith('/'))
            {
                prefixPath += '/';
            }

            if (!prefixSetPath.EndsWith('/'))
            {
                prefixSetPath += '/';
            }

            var result = AddToMenu(reflectionGroup.root, group, mode, currentPath, prefixPath, prefixSetPath,
                GetCached, SetValueOnSelectCallback, previewSource: obj);

            if (!string.IsNullOrEmpty(currentPath))
            {
                menu.SetCurrentlySelected(currentPath);
            }

            if (prefixType != null) return result;

            var description = obj is MonoBehaviour ? "Script"
                : obj is Component ? "Component"
                : obj is ScriptableObject ? "ScriptableObject"
                : obj.GetType().Name;

            var validation = GetTypeValidation(objType, mode);
            var path = objType.Name;

            group.Description = description;

            if (!validation.isValid)
            {
                return result;
            }

            var item = new EnhancedDropdownItem(objType.UserFriendlyName(), null,
                SetValueOnSelectCallback(""),
                icon,
                validation.hasConverter,
                validation.isSafe);

            if (group.Children.Length > 0)
            {
                var groupUI = new DropdownGroupItem(description, item);
                group.UIElementFactory = groupUI.CreateGroupUI;
                group.UISearchElementFactory = groupUI.CreateSearchGroupUI;
                group.GroupSearchElements.Add(item);
            }
            else
            {
                group.Add(path, item, currentPath == path);
            }

            return result;
        }

        private bool AddToMenu(MemberItem root,
            SmartDropdown.IPathGroup group,
            BindMode mode,
            string currentPath,
            string prefixPath,
            string prefixSetPath,
            Func<MemberItem, MemberItemsCache.MemberLink> getCached,
            Func<string, Action> selectCallback,
            int previewDepth = 0,
            Object previewSource = null)
        {
            var success = false;
            Profiler.BeginSample("BindData.AppendToMenuLoop");

            bool IsCompatible(MemberItem memberItem, MemberItemsCache.MemberLink link)
            {
                return link.isValid && mode.IsCompatibleWith(memberItem.canRead, memberItem.canWrite);
            }

            bool HasCompatibleChildren(MemberItem memberItem)
            {
                var canWrite = mode.CanWrite();
                // if (memberItem.type.IsValueType && !memberItem.canWrite)
                // {
                //     return false;
                // }

                foreach (var child in memberItem.children)
                {
                    if (canWrite && child.type.IsValueType && !child.canWrite)
                    {
                        continue;
                    }

                    var childLink = getCached(child);
                    if (IsCompatible(child, childLink) || HasCompatibleChildren(child))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool GroupIsAtCurrentPath()
            {
                if (string.IsNullOrEmpty(currentPath))
                {
                    return false;
                }

                var groupPath = group.Path.Trim(SmartDropdown.Separator) + SmartDropdown.Separator;
                if (!currentPath.StartsWith(groupPath))
                {
                    return false;
                }
                
                return currentPath.IndexOf(SmartDropdown.Separator, groupPath.Length) < 0;
            }
            
#if BS_DEBUG
            Debug.Log($"{currentPath} - {group.Path} ==> {GroupIsAtCurrentPath()}");
#endif
            var canPreview = previewDepth > 0 || GroupIsAtCurrentPath();

            foreach (var member in root.children)
            {
                var link = getCached(member);

                var isValid = IsCompatible(member, link);
                var hasValidChildren = member.children.Count > 0 && HasCompatibleChildren(member);
                if (!isValid && !hasValidChildren)
                {
                    continue;
                }

                var aliasedTypename = member.type.GetAliasName();
                SmartDropdown.IBuildingBlock item = null;
                if (isValid)
                {
                    if (canPreview && DropdownPreviewItem.CanPreview(member.type))
                    {
                        try
                        {
                            var fullPath = prefixSetPath.RemoveAtStart(previewSource.GetType().Name) + member.path;
                            fullPath = fullPath.TrimStart(SmartDropdown.Separator);
                            var accessor = AccessorsFactory.GetAccessor(previewSource, fullPath);
                            var previewValue = accessor.GetValue(previewSource);
                            item = new DropdownPreviewItem(member.name, member.type, selectCallback(prefixSetPath + member.path),
                                previewValue, canAlterGroup: false);
                        }
#if BS_DEBUG
                        catch (Exception ex)
                        {
                            Debug.LogError($"EXCEPTION @ Path: {member.path}");
                            Debug.LogException(ex, previewSource);
                        }
#endif
                        catch
                        {
                            // ignored
                        }
                    }

                    item ??= new EnhancedDropdownItem(member.name, hasValidChildren ? null : aliasedTypename,
                        selectCallback(prefixSetPath + member.path),
                        ObjectIcon.GetFor(member.type),
                        link.hasConverter,
                        link.hasSafeConverter);
                }

                if (hasValidChildren)
                {
                    var innerGroup = group.GetGroup(member.path);
                    innerGroup.Name = member.name;
                    innerGroup.Icon = ObjectIcon.GetFor(member.type);
                    innerGroup.Description = aliasedTypename;
                    innerGroup.OnGroupBuild = g =>
                    {
                        AddToMenu(member, innerGroup, mode, currentPath, prefixPath, prefixSetPath, getCached,
                            selectCallback, previewDepth - 1, previewSource);
                    };

                    if (item != null)
                    {
                        if (item is DropdownPreviewItem previewItem)
                        {
                            previewItem.SecondLabel = null;
                        }
                        var groupUI = new DropdownGroupItem(aliasedTypename, item);
                        innerGroup.UIElementFactory = groupUI.CreateGroupUI;
                        innerGroup.UISearchElementFactory = groupUI.CreateSearchGroupUI;
                        innerGroup.GroupSearchElements.Add(item);
                    }
                    else if (canPreview && DropdownPreviewItem.CanPreview(member.type))
                    {
                        try
                        {
                            var fullPath = prefixSetPath.RemoveAtStart(previewSource.GetType().Name) + member.path;
                            fullPath = fullPath.TrimStart(SmartDropdown.Separator);
                            var accessor = AccessorsFactory.GetAccessor(previewSource, fullPath);
                            var previewValue = accessor.GetValue(previewSource) ?? member.type;
                            innerGroup.UIElementFactory = new DropdownPreviewGroup(null, previewValue, oneLine: true)
                                .CreateGroupUI;
                        }
#if BS_DEBUG
                        catch (Exception ex)
                        {
                            Debug.LogError($"EXCEPTION @ Path: {member.path}");
                            Debug.LogException(ex, previewSource);
                        }
#endif
                        catch
                        {
                            // ignored
                        }
                    }
                }
                else
                {
                    group.Add(prefixPath + member.path, item, currentPath == (prefixSetPath + member.path));
                }

                success = true;
            }

            Profiler.EndSample();

            return success;
        }

        private void AppendPinnedPaths(SmartDropdown menu, SerializedProperty property)
        {
            var targets = _serializedObject.targetObjects;
            var paths = PinningSystem.GetAllPinnedPaths(targets);

            if (!paths.Any())
            {
                return;
            }
            
            menu.AddSeparator("Pinned");

            Object NormalizeContext(Object context)
            {
                if (context is Component c && c)
                {
                    return c.gameObject;
                }

                return context;
            }

            var groupsByContext = paths.GroupBy(g => NormalizeContext(g.context)).ToList();

            const int maxVisibleGroups = 5;
            const int maxPreviewGroups = 15;
            
            var showAsPreviewGroups = groupsByContext.Count <= maxPreviewGroups;
            
            var data = GetData(property);
            
            if(groupsByContext.Count < maxVisibleGroups || !data.sourceTarget)
            {
                foreach (var group in groupsByContext)
                {
                    AddPinnedGroup(menu, property, group, "", showAsPreviewGroups);
                }

                return;
            }
            
            var canShowLastPins = BindingSettings.Current.ShowLastUsedPins;
            var lastUsedContexts = PinningSystem.GetLastUsedContexts(targets[0]).Where(c => c).Select(NormalizeContext).Distinct().Take(4);
            var lastUsedGroups = groupsByContext.Where(g => lastUsedContexts.Contains(g.Key)).ToList();

            if (canShowLastPins)
            {
                foreach (var group in lastUsedGroups)
                {
                    AddPinnedGroup(menu, property, group, "", false);
                }
            }

            var pinnedPrefix = "All Pinned";
            var pinnedGroup = menu.GetGroupAt(pinnedPrefix);
            if(pinnedPrefix != "")
            {
                pinnedGroup.Icon = ObjectIcon.GetFor<PinnedPath>();
                pinnedGroup.Description = "Pinned Paths";
            }
            
            foreach (var group in groupsByContext)
            {
                AddPinnedGroup(menu, property, group, pinnedPrefix, showAsPreviewGroups);
            }
        }

        private void AddPinnedGroup(SmartDropdown menu, SerializedProperty property, IGrouping<Object, PinnedPath> group, string pinnedPrefix, bool asPreviewGroup)
        {
            if (!group.Key)
            {
                return;
            }
                
            var groupName = group.Key.name;
            var groupPath = pinnedPrefix + SmartDropdown.Separator + groupName;
            var groupMenu = menu.GetGroupAt(groupPath);
            groupMenu.Name = groupName;
            groupMenu.AllowDuplicates = true;
            
            DropdownPreviewGroup previewGroup = null;
            if(asPreviewGroup)
            {
                previewGroup = new DropdownPreviewGroup(groupName, group.Key?.GetType(), group.Key);
                groupMenu.UIElementFactory = previewGroup.CreateGroupUI;
            }
                
            var rootPaths = group.Key is GameObject 
                ? group.Where(p => p.IsRootPath).ToList() 
                : new List<PinnedPath>();
            var nonRootPaths = group.Except(rootPaths);
                
            var groupsByType = nonRootPaths.GroupBy(g => g.context.GetType()).ToList();
                
            if (groupsByType.Count > 1
                || rootPaths.Count > 1
                || (groupsByType.Count > 0 && rootPaths.Count > 0 && groupsByType[0].Key != rootPaths[0].type.Get()))
            {
                groupMenu.Description = "Diverse Pins";
                groupMenu.Icon = ObjectIcon.GetFor(group.Key);
            }
            else
            {
                var mainType = groupsByType.Count > 0
                    ? groupsByType[0].Key
                    : rootPaths.Count > 0
                        ? rootPaths[0].type.Get()
                        : group.Key.GetType();
                groupMenu.Description = mainType.GetAliasName();
                var mainContext = groupsByType.Count > 0 ? groupsByType[0].First().context : group.Key;
                groupMenu.Icon = AssetPreview.GetMiniThumbnail(mainContext) ?? ObjectIcon.GetFor(mainType);
                if (previewGroup != null)
                {
                    previewGroup.PreviewValue = mainContext;
                }
            }

            foreach (var typedGroup in groupsByType)
            {
                var separatorId = groupPath + SmartDropdown.Separator + typedGroup.Key.Name;
                menu.AddSeparator(separatorId, "Pins in " + typedGroup.Key.Name.RT().Bold());

                var canAddChildSeparator = false;
                var success = false;
                foreach (var path in typedGroup)
                {
                    if (path.flags.HasFlag(PinnedPath.BitFlags.PinChildren) 
                        && TryGetChildren(path.context, path.rawPath, out var children))
                    {
                        if (canAddChildSeparator)
                        {
                            menu.AddSeparator(groupPath + SmartDropdown.Separator + path.path.NiceName());
                        }
                        
                        canAddChildSeparator = true;

                        foreach (var childPath in children)
                        {
                            var groupId = path.type.Get()?.FullName;
                            groupId = groupId.Replace(SmartDropdown.Separator.ToString(), "");
                            var prefixSetPath = childPath.context == path.context
                                ? childPath.path
                                : path.path + SmartDropdown.Separator + childPath.path;
                            success |= AppendPinnedToMenu(menu, groupMenu, childPath, property, UpdateLastUsed, 
                                prefixSetPath: prefixSetPath,
                                context: path.context,
                                groupId: groupId);
                        }
                    }
                    else
                    {
                        success |= AppendPinnedToMenu(menu, groupMenu, path, property, UpdateLastUsed);
                    }
                }

                if (!success)
                {
                    menu.Remove(separatorId);
                }
            }
                
            if (group.Key is not GameObject keyGameObject)
            {
                return;
            }
                
            var mainPath = rootPaths.FirstOrDefault(p => p.context == group.Key);
            List<Component> components = null;

            if (mainPath.IsValid)
            {
                components = keyGameObject.GetComponents<Component>().ToList();
                    
                var mainTypename = mainPath.context.GetType().Name;
                var prefixPath = groupPath + SmartDropdown.Separator + mainTypename;
                menu.AddSeparator(prefixPath, null, out var removeSeparator);
                var result = AppendToMenu(menu, mainPath.context, null, prefixPath, mainTypename,
                    "",
                    property,
                    UpdateLastUsed);

                if (!result)
                {
                    removeSeparator();
                }
            }

            components ??= rootPaths.Where(p => p.context is Component)
                .Select(p => p.context as Component).ToList();
                
            var hasSuccessfulComponents = false;
            var componentsPrefix = groupPath + SmartDropdown.Separator + "Components";
            menu.AddSeparator(componentsPrefix, null, out var removeComponentsSeparator);
            foreach (var component in components)
            {
                var componentTypename = component.GetType().Name;
                var prefixPath = groupPath + SmartDropdown.Separator + componentTypename;
                hasSuccessfulComponents |= AppendToMenu(menu, component, null, prefixPath, componentTypename,
                    "", property, UpdateLastUsed);
            }
                
            if (!hasSuccessfulComponents)
            {
                removeComponentsSeparator();
            }

            return;

            bool TryGetChildren(Object context, string path, out List<PinnedPath> children, bool enterChildren = true)
            {
                using (var serObj = new SerializedObject(context))
                {
                    var property = serObj.FindProperty(path);
                    if (property == null)
                    {
                        children = null;
                        return false;
                    }
                    
                    if(path == "m_Name")
                    {
                        property.Next(false);
                    }

                    if (property.propertyPath == "m_EditorClassIdentifier")
                    {
                        property.Next(false);
                    }

                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        return TryGetChildren(property.objectReferenceValue, "m_Name", out children, false);
                    }

                    SerializedProperty nextProperty;
                    if (enterChildren)
                    {
                        nextProperty = property.Copy();
                        nextProperty.Next(false);
                        property.Next(property.propertyType != SerializedPropertyType.String);
                    }
                    else
                    {
                        nextProperty = null;
                    }

                    children = new();

                    do 
                    {
                        try
                        {
                            children.Add(new PinnedPath(context, property.propertyPath, property.GetPropertyType()));
                        }
                        catch
                        {
                            // ignored
                        }
                    }
                    while (property.NextVisible(false) && property.propertyPath != nextProperty?.propertyPath);
                }
                
                return children.Count > 0;
            }

            void UpdateLastUsed(Object obj, string _)
            {
                PinningSystem.UseContext(obj);
            }
        }

        private bool AppendPinnedToMenu(SmartDropdown menu,
            SmartDropdown.IPathGroup root,
            PinnedPath path,
            SerializedProperty property,
            Action<Object, string> onSelect = null,
            string prefixSetPath = null,
            Object context = null,
            string groupId = null)
        {
            var data = GetData(property);
            var bindType = data.bindType;
            if (bindType == null)
            {
                return false;
            }

            if (!context.IsAssignableTo(data.properties.context.objectReferenceValue))
            {
                return false;
            }

            var propertyPath = property.propertyPath;
            var mode = data.properties.BindMode;
            var actualContext = context ? context : path.context;

            if (!_membersCache.TryGetValue((bindType, mode), out var membersCache))
            {
                membersCache = new MemberItemsCache();
                _membersCache.Add((bindType, mode), membersCache);
            }

            #region [  INNER FUNCTIONS  ]

            string NormalizePath(string path)
            {
                return path.Replace('.', SmartDropdown.Separator).Replace('/', SmartDropdown.Separator);
            }
            
            Action SetValueOnSelectCallback(string pathToSet)
            {
                return _isUIToolkit
                    ? () =>
                    {
                        onSelect?.Invoke(actualContext, pathToSet);
                        SetNewValues(propertyPath, actualContext, pathToSet, true);
                    }
                    : () => GetData(propertyPath).preRenderAction =
                        () =>
                        {
                            onSelect?.Invoke(actualContext, pathToSet);
                            SetNewValues(propertyPath, actualContext, pathToSet, false);
                        };
            }

            MemberItemsCache.MemberLink GetCached(MemberItem m) => membersCache.GetLink(bindType, m, mode);

            (bool isValid, bool hasConverter, bool isSafe) GetTypeValidation(Type type, BindMode mode)
            {
                try
                {
                    Profiler.BeginSample("BindData.GetTypeValidation");

                    if (bindType.IsAssignableFrom(type))
                    {
                        return (true, false, true);
                    }

                    var isSafe = false;
                    if (!bindType.IsAssignableFrom(type))
                    {
                        if (mode.CanRead())
                        {
                            if (!ConvertersFactory.HasConversion(type, bindType, out var safe))
                            {
                                return (false, false, false);
                            }

                            isSafe = safe;
                        }

                        if (mode.CanWrite())
                        {
                            if (!ConvertersFactory.HasConversion(bindType, type, out var safe))
                            {
                                return (false, false, false);
                            }

                            isSafe = safe;
                        }
                    }

                    return (true, true, isSafe);
                }
                finally
                {
                    Profiler.EndSample();
                }
            }

            SmartDropdown.IBuildingBlock CreateItem(string name, PinnedPath path, string setPath, Texture2D icon,
                bool hasConverter, bool isSafe)
            {
                var type = path.type.Get();
                var actualPathToSet = setPath ?? path.path;
                actualPathToSet = NormalizePath(actualPathToSet);
                if (DropdownPreviewItem.CanPreview(type))
                {
                    try
                    {
                        var accessor = AccessorsFactory.GetAccessor(path.context, path.path);
                        var previewValue = accessor.GetValue(path.context) ?? type;
                        return new DropdownPreviewItem(name, type, SetValueOnSelectCallback(actualPathToSet),
                            previewValue, canAlterGroup: false);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                return new EnhancedDropdownItem(name, type.GetAliasName(),
                    SetValueOnSelectCallback(actualPathToSet),
                    icon,
                    hasConverter,
                    isSafe);
            }

            #endregion

            Profiler.BeginSample("BindData.GetMembersForType");
            var pathType = path.type.Get();
            if (pathType == null)
            {
                Profiler.EndSample();
                return false;
            }
            var reflectionGroup = ReflectionFactory.GetGroupFor(pathType);
            Profiler.EndSample();
            
            var icon = ObjectIcon.GetFor(pathType);
            var description = path.type.Get().GetAliasName();
            var normalizedPath = NormalizePath(path.path).TrimStart(SmartDropdown.Separator);
            var name = normalizedPath[(normalizedPath.LastIndexOf(SmartDropdown.Separator) + 1)..].NiceName();

            if(reflectionGroup.root.children.Count == 0)
            {
                var validItem = GetTypeValidation(path.type, mode);
                if (!validItem.isValid)
                {
                    return false;
                }
                
                var menuItem = CreateItem(name, path, null, icon, validItem.hasConverter, validItem.isSafe);
                
                root.Add(path.path, menuItem);
                return true;
            }

            var group = root;
            if (string.IsNullOrEmpty(name))
            {
                name = group.Name;
            }
            
            if (!string.IsNullOrEmpty(normalizedPath))
            {
                var groupPathAndId = normalizedPath + "_" + (groupId ?? description);
                group = root.GetGroup(groupPathAndId);
                if (group.Name == name && group.Description != description)
                {
                    // Here we have a different group with the same name, need to create a new one
                    group = root.NewGroup(groupId);
                }
                group.Name = name;
                group.Icon = icon;
                group.Description = description;
            }

            group.AllowDuplicates = true;

            var prefixPath = group.Path + SmartDropdown.Separator;
            prefixSetPath ??= path.path;
            prefixSetPath = NormalizePath(prefixSetPath).TrimEnd(SmartDropdown.Separator) + SmartDropdown.Separator;
            var previewDepth = DropdownPreviewItem.CanPreview(path.type.Get()) ? 2 : 0;

            var result = AddToMenu(reflectionGroup.root, group, mode, "", prefixPath, prefixSetPath,
                    GetCached, SetValueOnSelectCallback, previewDepth, actualContext);

            var validation = GetTypeValidation(path.type, mode);

            if (!validation.isValid)
            {
                AddGroupPreview();
                return result;
            }

            var item = CreateItem(name, path, prefixSetPath.TrimEnd(SmartDropdown.Separator), icon, validation.hasConverter, validation.isSafe);

            if (group.Children.Length > 0 && item != null)
            {
                if (item is DropdownPreviewItem previewItem)
                {
                    previewItem.SecondLabel = null;
                }
                else if (item is DropdownItem simpleItem)
                {
                    simpleItem.SecondLabel = null;
                }
                var groupUI = new DropdownGroupItem(description, item);
                group.UIElementFactory = groupUI.CreateGroupUI;
                group.UISearchElementFactory = groupUI.CreateSearchGroupUI;
                group.GroupSearchElements.Add(item);
            }
            else if (item == null && AddGroupPreview())
            {
                // We have a preview item
            }
            else
            {
                group.Add(prefixPath, item, index: 0);
            }

            return result;

            bool AddGroupPreview()
            {
                if (!DropdownPreviewItem.CanPreview(path.type.Get()))
                {
                    return false;
                }
                try
                {
                    var accessor = AccessorsFactory.GetAccessor(path.context, path.path);
                    var previewValue = accessor.GetValue(path.context) ?? path.type.Get();
                    group.UIElementFactory =
                        new DropdownPreviewGroup(null, previewValue, oneLine: true)
                            .CreateGroupUI;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        partial void UpdatePreviewIMGUI(PropertyData data);
        partial void UpdatePreviewUIToolkit(PropertyData data);

        private void SetNewValues(string propertyPath, Object source, string path, bool applyChanges)
        {
            RegisterLastUsedSource(source);
            
            var data = GetData(propertyPath);
            var purePath = TransformPath(source, path);

            SetNewSource(source, data, purePath);

            data.prevPath = purePath;

            data.UpdateCommonValues();

            data.formattedValue = null;

            // Reset the converters
            var (fromType, toType) = GetTypeMapping(data);
            if (fromType != data.readConverter.fromType || toType != data.readConverter.toType)
            {
                data.properties.readConverter.managedReferenceValue = null;
                data.readConverter = default;
                data.properties.preModifiers.ClearArray();
                data.preModifiers = default;
            }

            if (fromType != data.writeConverter.toType || toType != data.writeConverter.fromType)
            {
                data.properties.writeConverter.managedReferenceValue = null;
                data.writeConverter = default;
                data.properties.preModifiers.ClearArray();
                data.preModifiers = default;
            }

            data.properties.parameters?.ResetValue();

            // Generate the correct parameters value providers
            UpdateParameters(source, data, purePath);

            // Here we most probably have a different property type
            if ((toType ?? fromType) != data.prevType)
            {
                // Reset the modifiers
                data.modifiers = default;
            }

            if (data.onChanged != null)
            {
                data.preRenderAction = data.onChanged;
            }

            UpdatePathPreview(data);

            data.Refresh();

            if (applyChanges)
            {
                ApplyChanges(data);
            }
        }

        private void UpdatePathPreview(PropertyData data)
        {
            if (_isUIToolkit)
            {
                UpdatePreviewUIToolkit(data);
            }
            else
            {
                UpdatePreviewIMGUI(data);
            }
        }

        private void UpdateParameters(Object source, PropertyData data, string purePath)
        {
            if(!source || string.IsNullOrEmpty(purePath) || data.commonPath.isMixedValue == true)
            {
                data.parameters = default;
                return;
            }

            var lastIndexOf = purePath.LastIndexOf(']');
            if(lastIndexOf < 0)
            {
                lastIndexOf = purePath.LastIndexOf(')');
            }
            
            if (lastIndexOf < 5)
            {
                // Avoid the case of Providers or Arrays
                data.parameters = default;
                return;
            }
            
            var path = purePath.Substring(0, lastIndexOf + 1);
            
            var memberInfo = AccessorsFactory.GetMemberAtPath(source.GetType(), path);
            ParameterInfo[] paramsInfos = null;
            Type[] paramsTypes = null;
            Type memberType = null;
            switch (memberInfo)
            {
                case FieldInfo info:
                    memberType = info.FieldType;
                    break;
                case PropertyInfo info:
                    memberType = info.PropertyType;
                    paramsInfos = info.GetIndexParameters();
                    break;
                case MethodInfo info:
                    memberType = info.ReturnType;
                    paramsInfos = info.GetParameters();
                    break;
            }

            if (paramsInfos?.Length > 0)
            {
                paramsTypes = paramsInfos.Select(p => p.ParameterType).ToArray();
            }
            else if (memberType?.IsArray == true)
            {
                paramsTypes = new Type[memberType.GetArrayRank()];
                for (int i = 0; i < paramsTypes.Length; i++)
                {
                    paramsTypes[i] = typeof(int);
                }
            }

            if (paramsTypes != null && data.properties.parameters != null)
            {
                var parametersProp = data.properties.parameters;
                var parametersPropPath = parametersProp.propertyPath;

                if (data.commonPath.isMixedValue == false)
                {
                    data.commonPath.ForEach((t, p, v) =>
                    {
                        var parProp = p.serializedObject.FindProperty(parametersPropPath);
                        AddParameters(parProp, paramsTypes);
                        p.serializedObject.ApplyModifiedProperties();
                    });
                }
                else
                {
                    AddParameters(parametersProp, paramsTypes);
                }

                void AddParameters(SerializedProperty parametersProp, Type[] paramTypes)
                {
                    parametersProp.arraySize = paramsTypes.Length;
                    for (int i = 0; i < paramsTypes.Length; i++)
                    {
                        if (typeof(Object).IsAssignableFrom(paramTypes[i]))
                        {
                            // We have a UnityObject as value here
                            parametersProp.GetArrayElementAtIndex(i).FindPropertyRelative("_typename").stringValue =
                                paramTypes[i].AssemblyQualifiedName;
                            continue;
                        }

                        // We have a generic value here
                        parametersProp.GetArrayElementAtIndex(i).FindPropertyRelative("_typename").stringValue =
                            null;
                        if (DefaultValueProvider.TryGetProviderForType(paramsTypes[i], out var defaultProvider))
                        {
                            parametersProp.GetArrayElementAtIndex(i).FindPropertyRelative("_value")
                                .managedReferenceValue = defaultProvider;
                            continue;
                        }

                        var providerTypes = BindTypesCache.GetProvidersFor(paramsTypes[i]);
                        var providerType = providerTypes?.FirstOrDefault();
                        if (providerType == null)
                        {
                            providerType = paramsTypes[i];
                        }

                        var provider = CreateDefaultValue(providerType);
                        parametersProp.GetArrayElementAtIndex(i).FindPropertyRelative("_value")
                            .managedReferenceValue = provider;
                    }
                }
            }

        }

        private static void SetNewSource(Object source, PropertyData data, string purePath)
        {
            if (source != null && data.commonSource.isMixedValue == true)
            {
                // Here we need to update each target
                data.commonSource.ForEach((t, p, v) =>
                {
                    if (v == null || source.GetType().IsAssignableFrom(v.GetType()))
                    {
                        p.objectReferenceValue = v ? v : source;
                        p.serializedObject.ApplyModifiedProperties();

                        data.commonPath[t] = purePath;
                        data.commonType[t] = v.GetType().AssemblyQualifiedName;
                        return;
                    }

                    if (source is not Component && source is not GameObject)
                    {
                        // Do nothing, type mismatch
                        Debug.LogError(
                            $"{t.GetType()}: Unable to set path {purePath} because the source type {source.GetType()} is not compatible",
                            t);
                        return;
                    }

                    var go = v is Component c ? c.gameObject : v as GameObject;

                    if (go == null)
                    {
                        // Do nothing, type mismatch
                        Debug.LogError(
                            $"{t.GetType()}: Unable to set path {purePath} because the source type {source.GetType()} is not compatible",
                            t);
                        return;
                    }

                    Object vc = source is GameObject ? go : go.GetComponent(source.GetType());
                    if (vc == null)
                    {
                        Debug.LogError(
                            $"{t.GetType()}: Unable to set path {purePath} because the component {source.GetType()} is not present in {go}",
                            t);
                        return;
                    }

                    p.objectReferenceValue = vc;
                    p.serializedObject.ApplyModifiedProperties();

                    data.commonPath[t] = purePath;
                    data.commonType[t] = source.GetType().AssemblyQualifiedName;
                });

                data.serializedObject.Update();
            }
            else
            {
                data.properties.target.objectReferenceValue = source;
                data.properties.path.stringValue = purePath;
                data.sourcePersistedType = source?.GetType();
            }
        }

        private static object CreateDefaultValue(Type providerType)
        {
            if (providerType == typeof(string))
            {
                return string.Empty;
            }

            if (providerType.IsInterface)
            {
                return null;
            }

            if (providerType.IsAbstract)
            {
                return null;
            }

            if (!providerType.IsValueType && !providerType.GetConstructors().Any(c => c.GetParameters().Length == 0))
            {
                // No value type and without parameterless constructors
                foreach (var constructor in providerType.GetConstructors().OrderBy(c => c.GetParameters().Length))
                {
                    try
                    {
                        var pars = constructor.GetParameters();
                        var valuePars = pars.Select(p => CreateDefaultValue(p.ParameterType)).ToArray();
                        return Activator.CreateInstance(providerType, valuePars);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return Activator.CreateInstance(providerType, true);
        }
    }
}
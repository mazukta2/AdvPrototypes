using Postica.Common;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Postica.BindingSystem.BindDependencies;
using Postica.BindingSystem.Dependencies;
using Postica.BindingSystem.PinningLogic;
using Postica.BindingSystem.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    class BindingSettingsProvider : SettingsProvider
    {
        public BindingSettingsProvider(string path, SettingsScope scope)
            : base(path, scope, new string[] { "Binding", "DataBind" }) { }

        [SettingsProvider]
        public static SettingsProvider CreateBindingSettingsProvider()
        {
            return new BindingSettingsProvider("Project/Binding System", SettingsScope.Project);
        }

        private VisualElement _root;
        private VisualElement _header;
        private VisualElement _visualizationPart;
        private VisualElement _optimizationPart;
        private VisualElement _experimentalPart;
        private VisualElement _configurationPart;
        private VisualElement _toolsPart;

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            var settings = BindingSettings.Current;

            _root = new ScrollView() { name = "root" };

            _root.styleSheets.Add(LoadStyle("SettingsStyles"));
            if (!EditorGUIUtility.isProSkin)
            {
                _root.styleSheets.Add(LoadStyle("SettingsStyles_lite"));
            }

            rootElement.Add(_root);

            _header = BuildHeader(settings);
            _visualizationPart = BuildVisualizationSection(settings);
            _optimizationPart = BuildOptimizationSection(settings);
            _experimentalPart = BuildExperimentalSection(settings);
            _configurationPart = BuildConfigurationSection(settings);
            _toolsPart = BuildToolsSection(settings);

            _root.Add(_header);
            _root.Add(_visualizationPart);
            _root.Add(_optimizationPart);
            _root.Add(_toolsPart);
            _root.Add(_configurationPart);
            _root.Add(_experimentalPart);
            
            _root.StretchToParentSize();
        }
        
        private VisualElement BuildHeader(BindingSettings settings)
        {
            var header = new VisualElement() { name = "header" }.WithClass("horizontal-container");
            var title = new VisualElement().WithClass("inflate");
            title.Add(new VisualElement().WithClass("title-header").WithChildren(
                    new Label("Binding System") { name = "title" },
                    new Label("v" + BindSystem.Version) { name = "version" }));
            if (string.CompareOrdinal(Application.unityVersion, "2022.3") >= 0)
            {
                title.Add(new Label("For detailed information about these settings, please refer to the <a href=\"https://postica.gitbook.io/binding-system-2\">documentation</a>.") { enableRichText = true }.WithClass("description"));
            }
            else
            {
                title.Add(new Label("For detailed information about these settings, please refer to the documentation.") { enableRichText = true }.WithClass("description"));
                title.Add(new Button(() => Application.OpenURL("https://postica.gitbook.io/binding-system-2")) { text = "See Documentation" }.WithClass("hyperlink"));
            }
            header.Add(new Image() { name = "titleIcon", image = EditorGUIUtility.isProSkin ? Icons.BindIcon_Dark_On : Icons.BindIcon_Lite_On });
            header.Add(title);

            return header;
        }

        private VisualElement BuildVisualizationSection(BindingSettings settings)
        {
            var container = new VisualElement().WithClass("container", "container--visualization");
            var containerTitle = new Label("Visualization").WithClass("h2", "container-title");

            container.Add(containerTitle);
            
            container.WithChildren(
                new Label("Field Options").WithClass("field-header"),
                new Toggle().BindTo(settings, s => s.ShowTargetGroupReplacement),
                new Toggle().BindTo(settings, s => s.RealtimeDebug),
                new Label("Dropdown Options").WithClass("field-header"),
                new Toggle().BindTo(settings, s => s.ShowLastUsedSources),
                new Toggle().BindTo(settings, s => s.ShowLastUsedPins)
            );

            return container;
        }

        private static void RefreshInspector()
        {
            if (Selection.objects?.Length > 0)
            {
                var activeObjects = Selection.objects;
                Selection.activeObject = null;
                EditorApplication.delayCall += () => Selection.objects = activeObjects;
                EditorWindow.focusedWindow?.Repaint();
            }
        }

        private VisualElement BuildOptimizationSection(BindingSettings settings)
        {
            var container = new VisualElement().WithClass("container");
            var containerTitle = new Label("Optimization").WithClass("h2", "container-title");
            container.Add(containerTitle);
            
            BuildImportantField<SliderInt, int>(container, settings,
                s => s.MaxBindPathDepth,
                "Max Bind Path Depth",
                "This feature allows you to set the maximum depth of the bind path. " +
                "This can help prevent infinite loops and improve performance. " +
                "A low depth value makes binding less flexible but avoids issues with very complex types.\n" +
                "Reduce this value if the editor hangs when opening the binding menu.".RT().Color(BindColors.Debug),
                "Editor Only")
                .showInputField = true;
            
#if BS_LEGACY_SUPPORT
            BuildImportantField<Toggle, bool>(container, settings, 
                s => s.AutoFixSerializationUpgrade,
                "Auto Conversion",
                "This feature automatically detects type changes between standard fields* and bind types and copies their values, ensuring that no data is lost during conversion.\n" +
                "<b>Backing up your project is strongly recommended before enabling this feature</b>, as an extra precaution.\n",
                "*[SerializeReference] Not Supported Yet");
#endif
            
            BuildImportantField<Toggle, bool>(container, settings, 
                s => s.UsePhasedBinding,
                "Phased Bindings",
                "This feature switches all inspector-set bindings to use phased bindings instead of normal ones.\n" +
                "Phased bindings can improve performance by up to 30% for complex bindings setups, at the cost of a slightly bigger build size.",
                "Experimental");

            return container;
        }
        
        private TField BuildImportantField<TField, TProperty>(VisualElement container, 
            BindingSettings settings, 
            Expression<Func<BindingSettings, TProperty>> property, 
            string labelText, string description, string tag = null)
        where TField : BaseField<TProperty>, new()
        {
            var propertyContainer = new VisualElement().WithClass("important-property__container");
            var descriptionLabel = new Label(description)
                { pickingMode = PickingMode.Ignore }.WithClass("important-property__description");
            var field = new TField()
                {
                    label = labelText,
                }
                .AlignField()
                .WithClass("important-property__container__field");
                
            field.BindTo(settings, property, string.IsNullOrEmpty(labelText));
            
            var fullContainer = new VisualElement()
                .WithClass("important-property");

            propertyContainer.Add(field);
            fullContainer.Add(propertyContainer);
            fullContainer.Add(descriptionLabel);

            if (!string.IsNullOrEmpty(tag))
            {
                var hintLabel = new Label(tag).WithClass("important-property__container__tag");
                propertyContainer.Add(hintLabel);
            }

            container.Add(fullContainer);
            
            return field;
        }
        
        private VisualElement BuildToolsSection(BindingSettings settings)
        {
            var container = new VisualElement().WithClass("container");
            var containerTitle = new Label("Tools").WithClass("h2", "container-title");
            container.Add(containerTitle);

            BuildToolLine(container, "Bindings Monitor",
                "This tool shows all active bindings in the scene, allowing you to monitor their status and performance, and debug issues.",
                BindMonitorWindow.Open);
            
            BuildToolLine(container, "Bindings Dependencies Analyzer",
                "This tool analyzes and visualizes the dependencies between bindings in your project, helping you to understand and manage complex binding relationships.",
                DependenciesWindow.ShowDependenciesWindow);
            
            return container;
        }
        
        private void BuildToolLine(VisualElement container, string toolName, string description, Action onClick, string actionName = "↗ Open")
        {
            var manager = new ConfigManagerView(toolName, false);
            manager.SetAction(actionName, onClick);
            container.Add(manager);
            
            var descLabel = new Label(description) { pickingMode = PickingMode.Ignore }.WithClass("h5", "tool-description", "space-below");
            manager.content.Add(descLabel);
        }
        
        private VisualElement BuildConfigurationSection(BindingSettings settings)
        {
            var container = new VisualElement().WithClass("container");
            var containerTitle = new Label("Configuration").WithClass("h2", "container-title");
            container.Add(containerTitle);

            BuildProxyBindingsManager(settings, container);
            BuildPinningManager(settings, container);
            BuildFieldsRerouting(settings, container);
            BuildRefactoringManager(settings, container);
            BuildDependenciesManager(settings, container);
            
            return container;
        }

        private void BuildRefactoringManager(BindingSettings settings, VisualElement container)
        {
            var manager = new ConfigManagerView("Refactor Manager", true);
            
            manager.content.WithChildren(
                new Toggle().WithClass("aligned-field").BindTo(settings, s => s.EnableRefactoring),
                new Toggle().WithClass("aligned-field").BindTo(settings, s => s.PreferRenamingAutoFix),
                new Toggle().WithClass("aligned-field").BindTo(settings, s => s.EnableUnityClassesRefactoring)
            );
            
            container.Add(manager);
        }
        
        private void BuildDependenciesManager(BindingSettings settings, VisualElement container)
        {
            var manager = new ConfigManagerView("Dependencies Window", true);
            
            manager.content.WithChildren(
                new Toggle("Search in Children").WithClass("aligned-field").BindTo(settings, s => s.AddChildrenToDependenciesSearch, updateLabel: false)
            );
            
            container.Add(manager);
        }

        private void BuildProxyBindingsManager(BindingSettings settings, VisualElement container)
        {
            var manager = new ConfigManagerView("Proxy Bindings System", true);
            manager.SetAction("\u21bb Sanitize Scene(s)", CleanupProxies, "Removes all empty proxy bindings from currently opened scene(s)");
            manager.actionButton.SetEnabled(!Application.isPlaying);

            var flagsViewer = new BindDataDrawer.UpdateFlagsView(settings.DefaultBindData.Flags, (flags, interval) =>
            {
                var data = settings.DefaultBindData;
                data.SetFlags(flags);
                data.UpdateInterval = interval;
                settings.DefaultBindData = data;
            }, settings.DefaultBindData.UpdateInterval).BuildUI();
            
            manager.content.WithChildren(
                new Toggle().WithClass("aligned-field").BindTo(settings, s => s.ShowProxyBindings),
                new VisualElement().WithClass("aligned-field", "flags-viewer-container", BaseField<int>.ussClassName).WithChildren(
                    new Label("Default Bind Data")
                    {
                        tooltip = "The default bind data that will be used when creating a new binding."
                    }.WithClass("flags-viewer-label", BaseField<int>.labelUssClassName),
                    flagsViewer,
                    new Label("This is a template to be used when creating new bindings, it shouldn't affect the existing ones.").WithClass("flags-viewer-hint"))
            );
            
            container.Add(manager);
            void CleanupProxies()
            {
                for (int i = 0; i < SceneManager.loadedSceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var rootObject in rootObjects)
                    {
                        var proxies = rootObject.GetComponentsInChildren<ProxyBindings>(true);
                        foreach (var proxy in proxies)
                        {
                            if (proxy.IsEmpty)
                            {
                                Object.DestroyImmediate(proxy);
                            }
                        }
                    }
                }
            }
        }

        private void BuildPinningManager(BindingSettings settings, VisualElement container)
        {
            var manager = new ConfigManagerView("Pinning Manager", true);
            manager.SetAction("\u21bb Refresh", RebuildPinningManager, executeOnExpand: true);
            container.Add(manager);
            
            RebuildPinningManager();

            void RebuildPinningManager()
            {
                manager.content.Clear();
                var pinnedStorages = PinningSystem.GetAllPinnedStorages();
                foreach (var storage in pinnedStorages)
                {
                    var storageContainer = new VisualElement().WithClass("pin-container");
                    manager.content.Add(storageContainer);
                    var storagePaths = new Foldout()
                    {
                        text = "Pinned Properties in " + storage.Id, 
                        viewDataKey = "PinnedPaths_of_" + storage.Id,
                        focusable = false,
                        value = false,
                    }.WithClass("pin-paths","h3", "pin-title");
                    storageContainer.Add(storagePaths);
                    storagePaths.RegisterValueChangedCallback(e =>
                    {
                        storagePaths.Clear();
                        if (e.newValue)
                        {
                            BuildPinnedPaths(storage, storagePaths);
                        }
                    });
                    storagePaths.value = true;
                }
            }
        }

        private static void BuildPinnedPaths(IPinnedStorage storage, VisualElement storagePaths)
        {
            var paths = storage.AllPaths.OrderBy(p => p.context ? p.context.ToString() : "").ToList();
            foreach (var path in paths)
            {
                var pathContainer = new VisualElement().WithClass("pin-path");
                var contextField = new ObjectField()
                {
                    objectType = typeof(Object),
                    focusable = false, 
                    value = path.context, 
                    allowSceneObjects = true
                }.WithClass("pin-path-context");
                var pathLabel = new Label(path.path).WithClass("pin-path-label");
                var pathButton = new Button(() =>
                {
                    if (storage is Object obj && obj)
                    {
                        Undo.RegisterCompleteObjectUndo(obj, "Remove Path");
                    }

                    storage.RemovePath(path);
                    storagePaths.Remove(pathContainer);
                }) { text = "\u232b"}.WithClass("pin-path-button");
                    
                pathContainer.Add(contextField);
                pathContainer.Add(pathLabel);
                if (path.flags.HasFlag(PinnedPath.BitFlags.PinChildren))
                {
                    var pinChildrenBubble = new Label("Children"){tooltip = "Pinning System will show the children of this path, instead of the path itself"}.WithClass("pin-children-bubble");
                    pathContainer.Add(pinChildrenBubble);
                }
                pathContainer.Add(pathButton);
                storagePaths.Add(pathContainer);
            }
        }
        
        private void BuildFieldsRerouting(BindingSettings settings, VisualElement container)
        {
            var manager = new ConfigManagerView("Fields Routes", true);
            manager.SetAction("\u21bb Refresh", RebuildFieldsRoutes, executeOnExpand: true);
            container.Add(manager);
            
            RebuildFieldsRoutes();

            void RebuildFieldsRoutes()
            {
                manager.content.Clear();

                var instance = FieldRoutes.Instance;
                foreach (var routeGroup in instance.Routes.GroupBy(r => r.type))
                {
                    foreach (var route in routeGroup)
                    {
                        var routeContainer = BuildRoute(instance, route, () =>
                        {
                            instance.RemoveRoute(route.type, route.from);
                            RebuildFieldsRoutes();
                        });
                        manager.content.Add(routeContainer);
                    }
                }
            }
        }

        
        private static VisualElement BuildRoute(FieldRoutes instance, FieldRoutes.Route route, Action onRemove)
        {
            var routeContainer = new VisualElement().WithClass("route");
            var typeField = new VisualElement().WithClass("route__type").WithChildren(new Image()
            {
                image = ObjectIcon.GetFor(route.type.Get()),
                scaleMode = ScaleMode.ScaleToFit
            }.WithClass("route__type__icon"), new Label(route.type.Get().Name).WithClass("route__type__name"));
            
            var fromField = new TextField() { isReadOnly = true, value = route.from }.WithClass("route__field");
            var toField = new TextField() { value = route.to }.WithClass("route__field").DoOnValueChange<TextField, string>(v => instance.AddRoute(route.type, route.from, v));
            var removeButton = new Button(onRemove) { text = "\u232b" }.WithClass("route__remove-button");

            routeContainer.Add(typeField);
            routeContainer.Add(fromField);
            routeContainer.Add(new Label("\u2192").WithClass("route__arrow"));
            routeContainer.Add(toField);
            routeContainer.Add(removeButton);

            return routeContainer;
        }

        private VisualElement BuildExperimentalSection(BindingSettings settings)
        {
            // Nothing experimental for now
            var container = new VisualElement();

            return container;
        }

        private StyleSheet LoadStyle(string name)
        {
            var correctedPath = BindingSystemIO.GetAssetPath("Editor", "Settings", "Styles", $"{name}.uss");
            var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>(correctedPath);
            return styles;
        }
        
        private class ConfigManagerView : VisualElement
        {
            public readonly VisualElement header;
            public readonly VisualElement content;
            public readonly Button actionButton;
            
            private Action _executeOnExpand;
            private readonly string _sessionKey;
            
            public string Title
            {
                get => header.Q<Label>().text;
                set => header.Q<Label>().text = value;
            }
            
            public bool IsCollapsed
            {
                get => !content.IsDisplayed();
                set
                {
                    _executeOnExpand?.Invoke();
                    header.EnableInClassList("collapsed", value);
                    content.SetVisibility(!value);
                    SessionState.SetBool(_sessionKey + ":collapsed", true);
                }
            }
            
            public void SetAction(string actionName, Action action, string actionTooltip = null, bool executeOnExpand = false)
            {
                actionButton.text = actionName;
                actionButton.clicked += action;
                actionButton.SetVisibility(true);
                _executeOnExpand = executeOnExpand ? action : null;
                if (actionTooltip != null)
                {
                    actionButton.tooltip = actionTooltip;
                }
            }
            
            public ConfigManagerView(string title, bool collapsable)
            {
                AddToClassList("config-manager");
                header = new VisualElement().WithClass("config-manager__header");
                var managerTitle = new Label(title).WithClass("h3", "config-manager__header__title");
                header.Add(managerTitle);

                actionButton = new Button().WithClass(
                        "config-manager__header__refresh-button").SetVisibility(false);
                header.Add(actionButton);

                Add(header);
                content = new VisualElement().WithClass("config-manager__container");
                Add(content);
                
                _sessionKey = "config-manager-" + title;

                if (collapsable)
                {
                    header.AddToClassList("collapsable");
                    header.Insert(0, new Image().WithClass("collapse-icon"));
                    header.WhenClicked(() => IsCollapsed = !IsCollapsed);
                    IsCollapsed = SessionState.GetBool(_sessionKey + ":collapsed", true);
                }
            }
        }
    }
}
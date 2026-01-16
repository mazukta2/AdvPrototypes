using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Postica.BindingSystem; // for BindingSystemIO
using Postica.BindingSystem.Dependencies; // for IBindFinder, BindDependency, BindDependencyGroup
using Postica.Common; // for SerializedPropertyExtensions
#if UNITY_2020_1_OR_NEWER
using UnityEditor.Experimental.GraphView;
#endif

namespace Postica.BindingSystem.BindDependencies
{
    // A lightweight graph window for visualizing direct dependencies between properties.
    // Nodes represent objects, with ports per dependent property. Edges represent dependencies.
    // Read/Write/Both are styled differently. Clicking a property highlights the owner node.
    internal class DependenciesGraphWindow : EditorWindow
    {
        // [MenuItem("Window/Analysis/Bindings Dependency Graph")] 
        public static void Open() { GetWindow<DependenciesGraphWindow>(); }

        private Toggle _searchScenes;
        private Toggle _searchPrefabs;
        private Toggle _searchAssets;
        private ObjectField _filterByObject;
        private Button _rebuildBtn;
        private Label _status;
        private VisualElement _toolbar;
        private GraphView _graph;

        private readonly BindFindInLoadedScenes _scenesFinder = new();
        private readonly BindFindInLoadedPrefabs _prefabsFinder = new();
        private readonly BindFindInLoadedAssets _assetsFinder = new();

        private readonly Dictionary<int, ObjectNode> _nodes = new();
        private readonly Dictionary<(int instanceId, string path), Port> _outPorts = new();
        private readonly Dictionary<(int instanceId, string path), Port> _inPorts = new();

        public void CreateGUI()
        {
            titleContent = new GUIContent("Bindings Dependency Graph");
            minSize = new Vector2(900, 600);

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;

            // Load styles similar to other Binding System windows
            var ussPath = BindingSystemIO.BuildLocalPath("Editor", "BindDependencies", "DependenciesGraph.uss");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (styleSheet) root.styleSheets.Add(styleSheet);
            if (!EditorGUIUtility.isProSkin)
            {
                ussPath = BindingSystemIO.BuildLocalPath("Editor", "BindDependencies", "DependenciesGraphLite.uss");
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                if (styleSheet) root.styleSheets.Add(styleSheet);
            }

            BuildToolbar(root);
            BuildGraph(root);

            // Auto-rebuild after UI is ready
            EditorApplication.delayCall += RebuildGraph;
        }

        private void BuildToolbar(VisualElement root)
        {
            _toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4 } };

            _searchScenes = new Toggle("Loaded Scenes") { value = true };
            _searchPrefabs = new Toggle("Prefabs") { value = true };
            _searchAssets = new Toggle("Assets") { value = false };
            _filterByObject = new ObjectField("Filter (optional)") { objectType = typeof(Object) };
            _rebuildBtn = new Button(RebuildGraph) { text = "Rebuild" };
            _status = new Label("") { style = { unityTextAlign = TextAnchor.MiddleRight, flexGrow = 1 } };

            _toolbar.Add(_searchScenes);
            _toolbar.Add(_searchPrefabs);
            _toolbar.Add(_searchAssets);
            _toolbar.Add(_filterByObject);
            _toolbar.Add(_status);
            _toolbar.Add(_rebuildBtn);
            root.Add(_toolbar);

            // Trigger rebuild on changes
            _searchScenes.RegisterValueChangedCallback(_ => RebuildGraph());
            _searchPrefabs.RegisterValueChangedCallback(_ => RebuildGraph());
            _searchAssets.RegisterValueChangedCallback(_ => RebuildGraph());
            _filterByObject.RegisterValueChangedCallback(_ => RebuildGraph());
        }

        private void BuildGraph(VisualElement root)
        {
#if UNITY_2020_1_OR_NEWER
            _graph = new DependenciesGraphView();
            _graph.AddToClassList("bs-deps-graph");
            // _graph.StretchToParentSize(); // removed to avoid overlaying the toolbar
            _graph.style.flexGrow = 1f; // let it fill remaining space under the toolbar
            _graph.style.position = Position.Relative;
            root.Add(_graph);
#else
            var warn = new HelpBox("GraphView requires Unity 2020.1 or newer.", HelpBoxMessageType.Warning);
            root.Add(warn);
#endif
        }

        private async void RebuildGraph()
        {
#if !UNITY_2020_1_OR_NEWER
            return;
#else
            _status.text = "Scanning...";
            _rebuildBtn.SetEnabled(false);
            (_graph as DependenciesGraphView)?.ClearGraph();
            _nodes.Clear();
            _outPorts.Clear();
            _inPorts.Clear();

            // Collect dependency groups using existing finders
            var groups = new List<BindDependencyGroup>();
            var targets = CollectRoots();
            if (targets.Count == 0)
            {
                // Default to all proxies and binds in loaded scenes
                targets = CollectAllCandidateRoots();
            }

            float total = Mathf.Max(1f, targets.Count);
            float done = 0f;

            async System.Threading.Tasks.Task AddFromFinder(IBindFinder finder)
            {
                foreach (var t in targets)
                {
                    var found = await finder.FindDependencies(t.root, t.path, p =>
                    {
                        _status.text = $"Scanning {finder.Name} {(int)(100f * ((done + p) / total))}%";
                    }, new System.Threading.CancellationToken());

                    if (found != null) groups.AddRange(found);
                    done += 1f;
                }
            }

            var tasks = new List<System.Threading.Tasks.Task>();
            if (_searchScenes.value) tasks.Add(AddFromFinder(_scenesFinder));
            if (_searchPrefabs.value) tasks.Add(AddFromFinder(_prefabsFinder));
            if (_searchAssets.value) tasks.Add(AddFromFinder(_assetsFinder));

            foreach (var t in tasks) await t;

            // Build a flat list of dependencies
            var deps = new List<BindDependency>();
            foreach (var g in groups)
            {
                Collect(g);
            }

            // Create nodes and edges
            foreach (var d in deps)
            {
                TryCreateNode(d.Source);
                TryCreateNode(d.Target);

                var mode = ResolveMode(d, out var hasReadConv, out var hasWriteConv, out var hasMods);

                var outPort = GetOutPort(d.Source, d.SourcePath);
                var inPort = GetInPort(d.Target, d.TargetPath);

                var edge = outPort.ConnectTo(inPort);
                var modeClass = mode switch
                {
                    BindMode.Read => "dep-read",
                    BindMode.Write => "dep-write",
                    BindMode.ReadWrite => "dep-both",
                    _ => "dep-read"
                };
                edge.AddToClassList(modeClass);

                // Badges: Mode + Converters/Modifiers
                var badge = new Label("") { pickingMode = PickingMode.Ignore };
                badge.text = mode switch
                {
                    BindMode.Read => "R",
                    BindMode.Write => "W",
                    _ => "RW"
                };
                if (hasReadConv || hasWriteConv) badge.text += " •C";
                if (hasMods) badge.text += " •M";
                edge.Add(badge);

                _graph.AddElement(edge);
            }

            // Auto layout: simple grid
            ( _graph as DependenciesGraphView)?.AutoLayout();

            _status.text = $"Graph: { _nodes.Count } objects, { deps.Count } dependencies";
            _rebuildBtn.SetEnabled(true);
            return;

            void Collect(BindDependencyGroup group)
            {
                foreach (var d in group.Dependencies) deps.Add(d);
                foreach (var sub in group.SubGroups) Collect(sub);
            }
#endif
        }

        private List<(Object root, string path)> CollectRoots()
        {
            var list = new List<(Object, string)>();
            if (_filterByObject.value)
            {
                // If an object is specified, add all its serialized bind paths we can find as roots
                var go = _filterByObject.value as GameObject;
                if (go)
                {
                    foreach (var comp in go.GetComponents<Component>())
                    {
                        foreach (var p in EnumerateBindRoots(comp)) list.Add(p);
                    }

                    // Include proxies that use this GameObject or any of its components as Source
                    foreach (var pb in Resources.FindObjectsOfTypeAll<ProxyBindings>())
                    {
                        if (!pb) continue;
                        foreach (var proxy in pb.Bindings)
                        {
                            if (!proxy.IsBound || proxy.BindData == null) continue;
                            var bd = proxy.BindData.Value;
                            if (!bd.Source || string.IsNullOrEmpty(bd.Path)) continue;
                            if (bd.Source == go || (bd.Source is Component sc && sc.gameObject == go))
                            {
                                list.Add((bd.Source, bd.Path.Replace('.', '/')));
                            }
                        }
                    }
                }
                else
                {
                    foreach (var p in EnumerateBindRoots(_filterByObject.value)) list.Add(p);

                    // Include proxies that use this object as Source
                    foreach (var pb in Resources.FindObjectsOfTypeAll<ProxyBindings>())
                    {
                        if (!pb) continue;
                        foreach (var proxy in pb.Bindings)
                        {
                            if (!proxy.IsBound || proxy.BindData == null) continue;
                            var bd = proxy.BindData.Value;
                            if (bd.Source == _filterByObject.value && !string.IsNullOrEmpty(bd.Path))
                            {
                                list.Add((bd.Source, bd.Path.Replace('.', '/')));
                            }
                        }
                    }
                }
            }
            return list;
        }

        private List<(Object root, string path)> CollectAllCandidateRoots()
        {
            var list = new List<(Object, string)>();
            // Look into proxies first
            foreach (var pb in Resources.FindObjectsOfTypeAll<ProxyBindings>())
            {
                if (!pb) continue;
                foreach (var proxy in pb.Bindings)
                {
                    if (!proxy.IsBound || proxy.BindData == null) continue;
                    var bd = proxy.BindData.Value;
                    if (bd.Source && !string.IsNullOrEmpty(bd.Path)) list.Add((bd.Source, bd.Path.Replace('.', '/')));
                }
            }

            // Look into components with Bind<T> fields
            foreach (var comp in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (!comp) continue;
                foreach (var p in EnumerateBindRoots(comp)) list.Add(p);
            }
            return list.Distinct().ToList();
        }

        private IEnumerable<(Object root, string path)> EnumerateBindRoots(Object target)
        {
            // Only MonoBehaviours are expected to contain Bind<T> fields
            if (target is not MonoBehaviour)
            {
                yield break;
            }

            using var so = new SerializedObject(target);
            var sp = so.FindProperty("m_Script");
            if (sp == null)
            {
                yield break;
            }
            while (sp.NextVisible(sp.propertyType == SerializedPropertyType.Generic))
            {
                if (sp.propertyPath.EndsWith("bindData.Path", StringComparison.OrdinalIgnoreCase))
                {
                    var bindProp = sp.GetParent();
                    if (bindProp == null)
                    {
                        continue;
                    }
                    var srcProp = bindProp.FindPropertyRelative("Source");
                    var pathProp = bindProp.FindPropertyRelative("Path");
                    if (srcProp == null || pathProp == null)
                    {
                        continue;
                    }
                    var src = srcProp.objectReferenceValue;
                    var path = pathProp.stringValue;
                    var isBoundPath = sp.propertyPath.Replace(".bindData.Path", "._isBound");
                    var isBound = so.FindProperty(isBoundPath);
                    if (isBound is { propertyType: SerializedPropertyType.Boolean, boolValue: true } && src && !string.IsNullOrEmpty(path))
                    {
                        yield return (src, path.Replace('.', '/'));
                    }
                }
            }
        }

        private void TryCreateNode(Object obj)
        {
            if (!obj) return;
            var id = obj.GetInstanceID();
            if (_nodes.ContainsKey(id)) return;

            var node = new ObjectNode(obj, OnPropertyClicked);
            _nodes[id] = node;
            _graph.AddElement(node);
            ( _graph as DependenciesGraphView )?.RegisterNode(node);
        }

        private Port GetOutPort(Object obj, string path)
        {
            var key = (obj.GetInstanceID(), path);
            if (_outPorts.TryGetValue(key, out var p)) return p;
            var node = _nodes[obj.GetInstanceID()];
            p = node.GetOrCreateOutPort(path);
            _outPorts[key] = p;
            return p;
        }

        private Port GetInPort(Object obj, string path)
        {
            var key = (obj.GetInstanceID(), path);
            if (_inPorts.TryGetValue(key, out var p)) return p;
            var node = _nodes[obj.GetInstanceID()];
            p = node.GetOrCreateInPort(path);
            _inPorts[key] = p;
            return p;
        }

        private void OnPropertyClicked(Object owner)
        {
            foreach (var n in _nodes.Values) n.SetHighlighted(n.Owner == owner);
            Selection.activeObject = owner;
            EditorGUIUtility.PingObject(owner);
        }

        private BindMode ResolveMode(BindDependency dep, out bool hasReadConverter, out bool hasWriteConverter, out bool hasModifiers)
        {
            hasReadConverter = hasWriteConverter = hasModifiers = false;

            // First try proxy-based resolution
            if (dep.Target is Component c)
            {
                var provider = c.GetComponent<ProxyBindings>();
                if (provider != null && provider.TryGetProxy(dep.Source, dep.SourcePath.Replace('.', '/'), out var proxy, out _))
                {
                    if (proxy.BindData.HasValue)
                    {
                        var bd = proxy.BindData.Value;
                        hasReadConverter = bd.ReadConverter != null;
                        hasWriteConverter = bd.WriteConverter != null;
                        hasModifiers = bd.Modifiers != null && bd.Modifiers.Length > 0;
                        return bd.Mode;
                    }
                }
            }

            // Fallback: try to inspect a Bind<T> field on the target component
            if (dep.Target is MonoBehaviour mb)
            {
                using var so = new SerializedObject(mb);
                var sp = so.FindProperty("m_Script");
                while (sp.NextVisible(sp.propertyType == SerializedPropertyType.Generic))
                {
                    if (sp.propertyPath.EndsWith("bindData.Path", StringComparison.OrdinalIgnoreCase))
                    {
                        var bindProp = sp.GetParent();
                        var src = bindProp.FindPropertyRelative("Source")?.objectReferenceValue;
                        var path = bindProp.FindPropertyRelative("Path")?.stringValue;
                        var isBoundPath = sp.propertyPath.Replace(".bindData.Path", "._isBound");
                        var isBound = so.FindProperty(isBoundPath);
                        if (isBound is { propertyType: SerializedPropertyType.Boolean, boolValue: true }
                            && src == dep.Source && IsSimilarPath(path, dep.SourcePath))
                        {
                            var modeProp = bindProp.FindPropertyRelative("_mode");
                            var readConvProp = bindProp.FindPropertyRelative("_readConverter");
                            var writeConvProp = bindProp.FindPropertyRelative("_writeConverter");
                            var modsProp = bindProp.FindPropertyRelative("_modifiers");

                            hasReadConverter = readConvProp != null && !string.IsNullOrEmpty(readConvProp.managedReferenceFullTypename);
                            hasWriteConverter = writeConvProp != null && !string.IsNullOrEmpty(writeConvProp.managedReferenceFullTypename);
                            hasModifiers = modsProp != null && modsProp.isArray && modsProp.arraySize > 0;

                            return (BindMode)(modeProp?.enumValueIndex ?? (int)BindMode.ReadWrite);
                        }
                    }
                }
            }

            return BindMode.ReadWrite;
        }

        private static bool IsSimilarPath(string pathValue, string path)
        {
            pathValue = pathValue.Replace('.', '/');
            if (pathValue == path) return true;
            if (string.IsNullOrEmpty(pathValue)) return false;
            if (pathValue.StartsWith("_"))
            {
                return pathValue.Substring(1) == path
                       || (char.IsLower(pathValue[1]) && char.ToUpper(pathValue[1]) + pathValue.Substring(2) == path)
                       || (char.IsUpper(pathValue[1]) && char.ToLower(pathValue[1]) + pathValue.Substring(2) == path);
            }
            if (pathValue.StartsWith("m_"))
            {
                return pathValue.Substring(2) == path
                       || (char.IsLower(pathValue[2]) && char.ToUpper(pathValue[2]) + pathValue.Substring(3) == path)
                       || (char.IsUpper(pathValue[2]) && char.ToLower(pathValue[2]) + pathValue.Substring(3) == path);
            }
            if ("m_" + pathValue == path) return true;
            if ("_" + pathValue == path) return true;
            if (char.IsUpper(pathValue[0]) && "_" + char.ToLower(pathValue[0]) + pathValue.Substring(1) == path) return true;
            if (char.IsLower(pathValue[0]) && "_" + char.ToUpper(pathValue[0]) + pathValue.Substring(1) == path) return true;
            if (char.IsUpper(pathValue[0]) && "m_" + char.ToLower(pathValue[0]) + pathValue.Substring(1) == path) return true;
            if (char.IsLower(pathValue[0]) && "m_" + char.ToUpper(pathValue[0]) + pathValue.Substring(1) == path) return true;
            return false;
        }
    }

#if UNITY_2020_1_OR_NEWER
    internal class DependenciesGraphView : GraphView
    {
        private readonly List<ObjectNode> _nodes = new();
        public DependenciesGraphView()
        {
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            // Styles can be provided via USS if desired.
        }

        public void ClearGraph()
        {
            DeleteElements(graphElements.ToList());
            _nodes.Clear();
        }

        public void RegisterNode(ObjectNode node) => _nodes.Add(node);

        public void AutoLayout()
        {
            // Simple grid layout
            const float padding = 40f;
            const float width = 300f;
            const float height = 220f;

            int cols = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, _nodes.Count)));
            for (int i = 0; i < _nodes.Count; i++)
            {
                int r = i / cols;
                int c = i % cols;
                var pos = new Rect(padding + c * (width + padding), padding + r * (height + padding), width, height);
                _nodes[i].SetPosition(pos);
            }
        }
    }

    internal class ObjectNode : Node
    {
        public Object Owner { get; }
        private readonly Action<Object> _onPropertyClick;
        private readonly Dictionary<string, (Port inPort, Port outPort, Label label)> _props = new();

        public ObjectNode(Object owner, Action<Object> onPropertyClick)
        {
            Owner = owner;
            _onPropertyClick = onPropertyClick;

            // Enable dragging and interaction
            capabilities = Capabilities.Movable | Capabilities.Selectable | Capabilities.Collapsible | Capabilities.Renamable | Capabilities.Resizable;
            style.position = Position.Absolute;

            title = owner ? owner.name + $" ({owner.GetType().Name})" : "<null>";
            var icon = EditorGUIUtility.ObjectContent(owner, owner ? owner.GetType() : typeof(Object)).image;
            if (icon) titleContainer.Insert(0, new Image { image = icon, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore, style = { width = 16, height = 16, marginRight = 4 } });
        }

        public void SetHighlighted(bool on)
        {
            this.EnableInClassList("node-highlight", on);
            // Inline visual feedback in case no USS is present
            var c = on ? new Color(0.2f, 0.6f, 1f, 0.25f) : Color.clear;
            titleContainer.style.backgroundColor = c;
            titleContainer.style.unityBackgroundImageTintColor = on ? new Color(0.8f, 0.9f, 1f, 0.9f) : Color.white;
        }

        public Port GetOrCreateOutPort(string path)
        {
            var p = GetOrCreate(path);
            return p.outPort;
        }

        public Port GetOrCreateInPort(string path)
        {
            var p = GetOrCreate(path);
            return p.inPort;
        }

        private (Port inPort, Port outPort, Label label) GetOrCreate(string path)
        {
            if (_props.TryGetValue(path, out var ports)) return ports;

            var container = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } };
            var inPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(object));
            var outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(object));
            var lbl = new Label(path) { tooltip = path };

            lbl.RegisterCallback<MouseUpEvent>(_ => _onPropertyClick?.Invoke(Owner));

            container.Add(inPort);
            container.Add(lbl);
            container.Add(outPort);

            extensionContainer.Add(container);
            RefreshExpandedState();
            RefreshPorts();

            _props[path] = (inPort, outPort, lbl);
            return _props[path];
        }

        private GraphView GetGraphView()
        {
            var p = this.parent;
            while (p != null && p is not GraphView) p = p.parent;
            return p as GraphView;
        }
    }
#endif
}

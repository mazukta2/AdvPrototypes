using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.Common
{
    public class SmartDropdown
    {
        public const char Separator = '/';
        public const string SeparatorString = "/";
        private static readonly char[] _separators = { ' ', Separator };

        public const string ussSearchLabelPath = "sd-search-label-path";
        public const string ussVisible = "sd-visible";

        public enum CloseReason
        {
            Default,
            ItemWasSelected,
        }

        public delegate VisualElement CreateGroupUIDelegate(IPathGroup group, Action onClick, Action closeWindow, bool isDarkSkin);
        public delegate VisualElement CreateSearchGroupUIDelegate(string searchValue, IPathGroup group, Action onClick, Action closeWindow, bool isDarkSkin);
        public delegate void CloseWindowDelegate(CloseReason reason);
        public delegate void OnGroupBuildDelegate(IPathGroup group);

        public interface ISearchElement
        {
            SearchTags SearchTags { get; }
            VisualElement GetSearchDrawer(string searchValue, Action closeWindow, bool darkMode);
        }
        
        public interface IBuildingBlock : ISearchElement
        {
            void OnPathResolved(IPathNode node);
            VisualElement GetDrawer(Action closeWindow, bool darkMode);
        }


        public interface IPathNode
        {
            IPathNode Parent { get; }
            string Path { get; }
            string Name { get; set; }
            string Description { get; set; }
            Texture2D Icon { get; set; }
            int SiblingIndex { get; }
            void OnPreBuildView(Action<IBuildingBlock> callback);
        }

        public interface IPathGroup : IPathNode
        {
            bool AllowDuplicates { get; set; }
            IEnumerable<IBuildingBlock> Blocks { get; }
            List<ISearchElement> GroupSearchElements { get; }
            IPathNode[] Children { get; }
            CreateGroupUIDelegate UIElementFactory { get; set; }
            CreateSearchGroupUIDelegate UISearchElementFactory { get; set; }
            OnGroupBuildDelegate OnGroupBuild { get; set; }
            bool IsSelected { get; set; }
            IPathGroup Add(string path, IBuildingBlock elem, bool isSelected = false, int index = -1);
            IPathGroup GetGroup(string name);
            IPathGroup NewGroup(string normalizedPath);
            void SortChildren(Comparison<IPathNode> comparison);
        }

        #region [  STATIC PART  ]

        private static IValueRef _pendingDropdown;

        /// <summary>
        /// Builds a dropdown out of specified elements and shows it at specified position
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="position">The position to show this dropdown at</param>
        /// <param name="elements"></param>
        /// <param name="pathGetter"></param>
        /// <param name="onSelectCallback"></param>
        /// <param name="selected"></param>
        /// <param name="nameGetter"></param>
        /// <param name="iconGetter"></param>
        /// <param name="searchTagsGetter"></param>
        public static void Show<T>(Rect position,
                                   IEnumerable<T> elements,
                                   Func<T, string> pathGetter,
                                   Action<T> onSelectCallback,
                                   T selected = default,
                                   Func<T, string> nameGetter = null,
                                   Func<T, Texture2D> iconGetter = null,
                                   Func<T, SearchTags> searchTagsGetter = null)
        {
            new SmartDropdown(true)
                .AddValues(elements, pathGetter, iconGetter, onSelectCallback, selected, nameGetter)
                .Show(position);
        }

        /// <summary>
        /// Shows a dropdown list of all specified values to select from
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rect"></param>
        /// <param name="items"></param>
        /// <param name="selected"></param>
        /// <param name="nameGetter"></param>
        /// <param name="intent"></param>
        /// <returns></returns>
        public static T AsPopup<T>(Rect rect,
                                IEnumerable<T> items,
                                T selected = default,
                                Func<T, string> nameGetter = null,
                                string intent = null,
                                GUIStyle style = null)
        {
            var label = selected != null ? nameGetter?.Invoke(selected) ?? selected.ToString() : "Nothing";
            var buttonStyle = style ?? EditorStyles.popup;
            bool buttonIsPressed = GUI.Button(rect, label, buttonStyle);
            if(_pendingDropdown != null)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    buttonStyle.Draw(rect, label, false, true, true, false);
                }
            }
            if ((_pendingDropdown == null && GUI.Button(rect, label, style)) 
                || (_pendingDropdown != null && _pendingDropdown.Rect != rect))
            {
                Func<T, string> pathGetter = nameGetter ?? (i => i?.ToString() ?? string.Empty);
                var valueRef = new ValueRef<T>(rect, selected);
                Action<T> onSelectInner = t => valueRef.Value = t;
                var dropdown = new SmartDropdown(false, intent)
                                .AddValues(items, pathGetter, null, t => valueRef.Value = t, selected, nameGetter);

                dropdown._noScrollView = true;
                dropdown.Show(rect);

                _pendingDropdown = valueRef;
                dropdown._onDismiss = () => _pendingDropdown?.Invalidate();

                return selected;
            }

            if (!_pendingDropdown.HasChanged)
            {
                return selected;
            }

            var value = (_pendingDropdown as IValueRef<T>).Value;
            _pendingDropdown = null;
            return value;
        }

        public static void Show<T>(Rect rect,
                                IEnumerable<T> items,
                                Action<T> onSelect,
                                T selected = default,
                                Func<T, string> nameGetter = null)
        {
            _pendingDropdown?.Invalidate(); // <-- invalidate any remaining dropdown

            Func<T, string> pathGetter = nameGetter ?? (i => i?.ToString() ?? string.Empty);
            var valueRef = new ValueRef<T>(rect, selected);
            var dropdown = new SmartDropdown(false)
                            .AddValues(items, pathGetter, null, onSelect, selected, nameGetter);

            dropdown.Show(rect);
        }

        private static string GetNameFromPath(string path)
        {
            var index = path.LastIndexOf(Separator);
            var name = index < 0 || index == path.Length - 1 ? path : path[(index + 1)..];
            return name;
        }

        private static StyleSheet _defaultStyles;
        private static StyleSheet _defaultLiteStyles;
        private static void LoadStyles(VisualElement element)
        {
            if (!_defaultStyles)
            {
                var pathSuffix = Path.Combine("SmartDropdown", "Styles");
                var stylesPath = Directory.GetFiles("Packages", "SDStyles.uss", SearchOption.AllDirectories)
                                          .FirstOrDefault(f => Path.GetDirectoryName(f).EndsWith(pathSuffix));
                var relativePath = stylesPath.Replace('\\', '/').Replace(Application.dataPath, "Assets");
                _defaultStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(relativePath);

                if (!_defaultLiteStyles)
                {
                    stylesPath = Directory.GetFiles("Packages", "SDStyles_lite.uss", SearchOption.AllDirectories)
                                          .FirstOrDefault(f => Path.GetDirectoryName(f).EndsWith(pathSuffix));
                    relativePath = stylesPath.Replace('\\', '/').Replace(Application.dataPath, "Assets");
                    _defaultLiteStyles = AssetDatabase.LoadAssetAtPath<StyleSheet>(relativePath);
                }
            }

            element.styleSheets.Add(_defaultStyles);
            if (_defaultLiteStyles && !EditorGUIUtility.isProSkin)
            {
                element.styleSheets.Add(_defaultLiteStyles);
            }
        }

#if INTERNAL_TEST
        [MenuItem("SmartDropdown/ShowTest Window")]
#endif
        private static void TestShow()
        {
            var sd = PrepareTestDropdown();

            sd.ShowAsWindow(new Rect(200, 200, 300, 800));
        }

#if INTERNAL_TEST
        [MenuItem("SmartDropdown/ShowTest Dropdown")]
#endif
        private static void TestShowDropdown()
        {
            var sd = PrepareTestDropdown();

            sd.Show(new Rect(200, 200, 300, 500));
        }

        private static SmartDropdown PrepareTestDropdown()
        {
            return new SmartDropdown(true)
                                 .Add("Click me!", () => Debug.Log("You clicked me!"))
                                 .Add("Materials/Red", new DropdownPreviewItem("Red", typeof(Material), () => Debug.Log("Selected Red"), Resources.Load<Material>("Red"), "Hello"))
                                 .Add("Materials/Yellow", new DropdownPreviewItem(null, typeof(Material), () => Debug.Log("Selected Yellow"), Resources.Load<Material>("Yellow")))
                                 .Add("First Row/Click me! 2", "Hero", () => Debug.Log("You clicked me! 2"))
                                 .Add("First Row/Toggle me!", false, v => Debug.Log("Now I am: " + v))
                                 .Add("First Row/Or Click me! 2", () => Debug.Log("You clicked me! 2"))
                                 .Add("First Row/Second Row/Click me!", () => Debug.Log("You clicked me!"))
                                 .Add("First Row/Second Row/Or Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Or event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Click me!", () => Debug.Log("You clicked me!"))
                                 .Add("First Row/Second Row/Third row/Final row/Click me!", () => Debug.Log("You clicked me!"))
                                 .Add("First Row/Second Row/Third row/Final row/Or Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Or event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or1 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or2 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or3 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or4 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or5 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or6 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or7 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or8 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or9 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or10 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or11 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or12 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or13 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or14 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or15 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or16 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or17 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or18 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or19 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or20 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or21 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or22 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or23 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or24 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or25 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or26 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or27 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or28 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or29 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or30 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or31 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or32 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or33 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or34 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or35 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or36 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or37 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or38 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or39 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or40 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or41 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or42 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or43 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or44 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or45 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or46 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or47 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or48 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or49 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or50 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or51 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or52 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or53 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or54 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or55 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or56 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or57 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or58 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))
                                 .Add("First Row/Second Row/Third row/Final row/Fifth row/Or or59 event Click me!", new DropdownItem("Click me!", () => Debug.Log("You clicked me!")))

                                 //.Add("First Row/Other Row/Click me!", "Villain", Icons.Bind, () => Debug.Log("You clicked me!"))
                                 .AddSeparator("Strange stuff")
                                 .Add("Hi there", new DropdownItem("Heyyyyy", () => Debug.Log("You clicked me!")))
                                 .Add("Hello there", new DropdownItem("Whats up", () => Debug.Log("You clicked me!")))
                                 .Add("Hello there again", new DropdownItem("", () => Debug.Log("You clicked me!")))
                                 .AddSeparator("Even Stranger things")
                                 .Add("Yahooo", new DropdownItem("", () => Debug.Log("You clicked me!")))
                                 .Add("Yahooo 2", new DropdownItem("", () => Debug.Log("You clicked me!")))
                                 .Add("Yahooo 3", new DropdownItem("", () => Debug.Log("You clicked me!")))
                                 .Add("Yahooo 4", new DropdownItem("", () => Debug.Log("You clicked me!")))
                                 .Add("Yahooo 5", new DropdownItem("", () => Debug.Log("You clicked me!")))
                                 .Add("Yahooo 6", new DropdownItem("", () => Debug.Log("You clicked me!")))
                                 .Add("Yahooo 7", new DropdownItem("", () => Debug.Log("You clicked me!")))
                                 .EditGroup("Materials", null, new DropdownPreviewGroup(typeof(Material), Resources.Load<Material>("Yellow")).CreateGroupUI);
        }

#endregion

        #region [  Tree Data Structures  ]

        private interface IValueRef
        {
            Rect Rect { get; }
            bool HasChanged { get; }
            bool HasValue { get; }

            void Invalidate();
        }

        private interface IValueRef<out T> : IValueRef
        {
            T Value { get; }
        }

        private class ValueRef<T> : IValueRef<T>
        {
            private T _value;

            public bool HasValue { get; private set; }
            public bool HasChanged { get; private set; }
            public T Value 
            { 
                get => _value;
                set
                {
                    _value = value;
                    HasValue = true;
                    HasChanged = true;
                }
            }

            public ValueRef(Rect rect, T originalValue)
            {
                Rect = rect;
                _value = originalValue;
            }

            public Rect Rect { get; private set; }

            public void Invalidate() => HasChanged = true;
        }

        private class GroupOverrides
        {
            public CreateGroupUIDelegate uiFactory;
            public IBuildingBlock[] searchableBlocks;
            public Texture2D icon;
            public string nameLabel;
            public string descriptionLabel;
            public OnGroupBuildDelegate onGroupBuild;
        }

        private abstract class SDNode : IPathNode
        {
            protected string _nameOverride;
            protected readonly string _name;
            protected readonly string _path;

            public SDNode parent;
            public VisualElement visualElement;
            public VisualElement searchVisualElement;

            public SDNode(string path, string name = null)
            {
                _path = path;
                var index = path.LastIndexOf(Separator);
                _name = name ?? (index < 0 || index == path.Length - 1 ? path : path.Substring(index + 1));
            }

            public string rawName => _name;

            public IPathNode Parent => parent;
            
            public Action<IBuildingBlock> OnPreBuildViewCallback { get; set; }
            
            public string Name
            {
                get => _nameOverride ?? _name;
                set => _nameOverride = value;
            }

            public string Path => _path;

            public string Description { get; set; }
            public Texture2D Icon { get; set; }
            public int SiblingIndex { get; internal set; } = -1;
            public virtual void OnPreBuildView(Action<IBuildingBlock> callback)
            {
                OnPreBuildViewCallback += callback;
            }
        }

        private class SDNodeGroup : SDNode, IPathGroup
        {
            private bool _groupBuilt = false;
            private SDRoot _root;
            private readonly HashSet<IBuildingBlock> _blocks = new();
            public readonly List<SDNode> children = new();
            public VisualElement panel;
            
            public bool AllowDuplicates { get; set; }
            public bool IsSelected { get; set; }

            public SDNodeGroup(string path, SDRoot root) : base(path)
            {
                _root = root;
            }

            public SDNodeGroup(string path, string name, SDRoot root) : base(path, name)
            {
                _root = root;
            }

            public List<ISearchElement> GroupSearchElements { get; } = new();
            public IPathNode[] Children => children.ToArray();

            public CreateGroupUIDelegate UIElementFactory { get; set; }
            public CreateSearchGroupUIDelegate UISearchElementFactory { get; set; }
            public OnGroupBuildDelegate OnGroupBuild { get; set; }

            public IEnumerable<IBuildingBlock> Blocks => _blocks;
            public bool IsEmpty => children.Count == 0 && OnGroupBuild == null;

            public IPathGroup Add(string path, IBuildingBlock elem, bool isSelected = false, int index = -1)
            {   
                var name = GetNameFromPath(path).Trim(Separator);
                if(!AllowDuplicates && children.Any(n => n.Name == name))
                {
                    throw new InvalidOperationException($"A node with name {name} already exists in group {Path}");
                }
                
                var nodeBlock = new SDNodeBlock(path, elem, isSelected);
                if(index < 0)
                {
                    children.Add(nodeBlock);
                    index = children.Count - 1;
                }
                else
                {
                    children.Insert(index, nodeBlock);
                }
                
                _blocks.Add(elem);
                
                nodeBlock.parent = this;
                nodeBlock.SiblingIndex = index;
                elem.OnPathResolved(nodeBlock);
                return this;
            }

            public IPathGroup GetGroup(string path)
            {
                var index = path.LastIndexOf(Separator);
                var name = index < 0 ? path : path[(index + 1)..];
                return _root.GetOrCreateGroup(_path + Separator + name);
            }
            
            public IPathGroup NewGroup(string path)
            {
                var index = path.LastIndexOf(Separator);
                var name = index < 0 ? path : path[(index + 1)..];
                return _root.CreateGroup(_path + Separator + name);
            }

            public void SortChildren(Comparison<IPathNode> comparison)
            {
                children.Sort(comparison);
            }

            public void RemoveEmptyGroups()
            {
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    if (children[i] is SDNodeGroup group)
                    {
                        group.RemoveEmptyGroups();
                        if (group.IsEmpty)
                        {
                            children.RemoveAt(i);
                        }
                    }
                }
            }

            public SDNodeGroup EnsureIsBuilt()
            {
                if (!_groupBuilt)
                {
                    _groupBuilt = true;
                    OnGroupBuild?.Invoke(this);
                }
                return this;
            }

            public void ApplyOverrides(in GroupOverrides groupOverrides)
            {
                UIElementFactory = groupOverrides.uiFactory;
                foreach(var block in groupOverrides.searchableBlocks)
                {
                    _blocks.Add(block);
                }
                Icon = groupOverrides.icon;
                Description = groupOverrides.descriptionLabel;
                _nameOverride = groupOverrides.nameLabel;
                if (groupOverrides.onGroupBuild != null)
                {
                    OnGroupBuild = groupOverrides.onGroupBuild;
                }
            }

            public virtual void Add(SDNode child)
            {
                children.Add(child);

                child.parent = this;
                child.SiblingIndex = children.Count - 1;
                if (child is SDNodeBlock blockNode)
                {
                    blockNode.block?.OnPathResolved(child);
                }
            }

            internal void Search(string searchValue, List<VisualElement> foundNodex)
            {
                foreach(var child in children)
                {
                    if(child is SDNodeBlock block && block.block.SearchTags.Contains(searchValue))
                    {
                        foundNodex.Add(block.visualElement);
                    }
                    else if(child is SDNodeGroup group)
                    {
                        foreach(var groupBlock in Blocks)
                        {
                            if (groupBlock.SearchTags.Contains(searchValue))
                            {
                                foundNodex.Add(group.visualElement);
                            }
                        }
                        group.Search(searchValue, foundNodex);
                    }
                }
            }

            internal void Search(string searchValue, List<SDNode> foundNodes, bool includePath, int maxDepth)
            {
                EnsureIsBuilt();
                foreach (var child in children)
                {
                    if (child is SDNodeBlock block)
                    {
                        if (block.block.SearchTags.Contains(searchValue))
                        {
                            foundNodes.Add(child);
                        }
                        else if (includePath && block.Path.ToLower().Contains(searchValue))
                        {
                            foundNodes.Add(child);
                        }
                    }
                    else if (child is SDNodeGroup group)
                    {
                        foreach (var searchElement in group.GroupSearchElements)
                        {
                            if (searchElement.SearchTags.Contains(searchValue))
                            {
                                foundNodes.Add(group);
                                break;
                            }
                        }

                        if (maxDepth > 0)
                        {
                            group.Search(searchValue, foundNodes, includePath, maxDepth - 1);
                        }
                    }
                }
            }

            internal SDNode GetFirstChecked()
            {
                foreach(var child in children)
                {
                    if(child is SDNodeBlock { isChecked: true })
                    {
                        return child;
                    }
                    if(child is SDNodeGroup group)
                    {
                        var checkedChild = group.GetFirstChecked();
                        if(checkedChild != null)
                        {
                            return checkedChild;
                        }

                        if (group.IsSelected)
                        {
                            return group;
                        }
                    }
                }
                return null;
            }
        }

        private class SDRoot : SDNodeGroup
        {
            private readonly Dictionary<string, SDNode> _nodes = new(StringComparer.Ordinal);
            private readonly Dictionary<string, SDNodeGroup> _groups = new(StringComparer.Ordinal);
            private SDNodeGroup _lastUsedGroup;

            public SDRoot(string name) : base("", name, null)
            {
                
            }

            public override void Add(SDNode child)
            {
                base.Add(child);
                if(child is SDNodeGroup group)
                {
                    _groups[group.Path.Trim(Separator)] = group;
                }
                _nodes[child.Path] = child;
            }

            public void Add(string path, SDNode node)
            {
                var index = path.LastIndexOf(Separator);
                if (index < 0)
                {
                    Add(node);
                    return;
                }

                var groupPath = path.Substring(0, index).Trim(Separator);
                var group = GetOrCreateGroup(groupPath);

                _lastUsedGroup = group ?? throw new InvalidOperationException($"{nameof(SmartDropdown)}: Unable to retrieve groups for path {path}.");
                _nodes[path] = node;
                group.Add(node);
            }

            public bool Remove(SDNode node)
            {
                _nodes.Remove(node.Path);

                if(node is IPathGroup nodeGroup)
                {
                    foreach(var child in nodeGroup.Children)
                    {
                        if (child is SDNode childNode)
                        {
                            Remove(childNode);
                        }
                    }
                }

                if (children.Remove(node))
                {
                    return true;
                }

                if(node.parent is SDNodeGroup sdNodeGroup)
                {
                    return sdNodeGroup.children.Remove(node);
                }

                return false;
            }

            public SDNodeGroup GetOrCreateGroup(string path)
            {
                if (path == _path)
                {
                    return this;
                }
                
                if(_lastUsedGroup != null && _lastUsedGroup.Path.Length == path.Length && _lastUsedGroup.Path == path)
                {
                    return _lastUsedGroup;
                }

                if (_groups.TryGetValue(path, out var group))
                {
                    return group;
                }

                group = new SDNodeGroup(path, this);
                _groups[path] = group;

                var index = path.LastIndexOf(Separator);
                if (index < 0)
                {
                    Add(group);
                    return group;
                }

                var groupPath = path.Substring(0, index);
                GetOrCreateGroup(groupPath).Add(group);

                return group;
            }
            
            public SDNodeGroup CreateGroup(string path)
            {
                var group = new SDNodeGroup(path, this);
                _groups[path] = group;

                var index = path.LastIndexOf(Separator);
                if (index < 0)
                {
                    Add(group);
                    return group;
                }

                var groupPath = path.Substring(0, index);
                GetOrCreateGroup(groupPath).Add(group);

                return group;
            }

            public bool TryGetGroup(string path, out SDNodeGroup group) => _groups.TryGetValue(path.Trim(Separator), out group);

            public bool TryGetNodeAtPath<T>(string path, out T node) where T : SDNode
            {
                if(_nodes.TryGetValue(path, out var n) && n is T tn)
                {
                    node = tn;
                    return true;
                }
                node = null;
                return false;
            }
        }

        private class SDNodeBlock : SDNode
        {
            public readonly IBuildingBlock block;
            public readonly Func<IBuildingBlock> factory;
            public readonly bool isChecked;

            public SDNodeBlock(string path, IBuildingBlock block, bool isChecked = false) : base(path) 
            {
                this.block = block;
                this.isChecked = isChecked;
            }
            
            public SDNodeBlock(string path, Func<IBuildingBlock> factory, bool isChecked = false) : base(path) 
            {
                this.factory = factory;
                this.isChecked = isChecked;
            }
        }

#endregion
        
        private class SDWindow : EditorWindow
        {
            private enum PanelAnimation
            {
                None, 
                SlideRight,
                SlideLeft,
                Fade
            }

            private const string SearchGroupPath = "__search__";

#pragma warning disable CS0414 // Remove unused private members
            private bool _isEnabled = false;
#pragma warning restore CS0414 // Remove unused private members
            private bool _hasToFit;
            private bool _noScrollView;
            private float _sizeDelta = 0;

            private UIAnimator _animator = new UIAnimator();
            private float _animationDuration = 0.2f;

            private SmartDropdown _dropdown;
            private Stack<SDNodeGroup> _groupStack = new Stack<SDNodeGroup>();
            private SDNodeGroup _searchRoot;

            private VisualElement _rootPanel;
            private VisualElement _body;
            private VisualElement _header;
            private VisualElement _footer;
            private VisualElement _currentPanel;
            private VisualElement _searchGroup;
            private Label _emptyLabel;
            private TextField _searchText;

            [NonSerialized]
            private bool _hasSelection = false;

            private bool _isGlobalSearch;

            private SDNodeGroup CurrentGroup => _groupStack.Count > 0 ? _groupStack.Peek() : null;

            public Rect TargetPosition { get; internal set; }

            public event Action<CloseReason> OnCloseWindow;

            private void OnEnable()
            {
                _isEnabled = true;
            }

            private void OnDisable()
            {
                if (_dropdown != null)
                {
                    _dropdown.isEnabled = _isEnabled = false;
                    _dropdown._onDismiss?.Invoke();
                }
            }

            private void Update()
            {
                _animator.Update();
            }

            private void OnGUI()
            {
                if (_hasToFit)
                {
                    FitToContent();
                    ReadjustOnScreen();
                    _hasToFit = false;
                }
            }

            private void OnDestroy()
            {
                OnCloseWindow?.Invoke(_hasSelection ? CloseReason.ItemWasSelected : CloseReason.Default);
            }

            private void CloseOnSelect()
            {
                _hasSelection = true;
                Close();
            }

            private VisualElement CreateGroup(string name, string description, Texture2D icon, Action onClick)
            {
                var ve = new VisualElement();
                ve.AddToClassList("sd-clickable");
                ve.AddToClassList("sd-group");

                var image = new Image() { image = icon };
                image.AddToClassList("sd-icon");
                image.AddToClassList("sd-image");
                image.EnableInClassList("sd-has-value", icon);
                var border = new VisualElement();
                border.AddToClassList("sd-border");
                image.Add(border);
                ve.Add(image);

                var label = new Label(name);
                label.AddToClassList("sd-label");
                label.AddToClassList("sd-label-main");
                ve.Add(label);

                if (!string.IsNullOrEmpty(description))
                {
                    var descriptionLabel = new Label(description);
                    descriptionLabel.AddToClassList("sd-label");
                    descriptionLabel.AddToClassList("sd-label-description");
                    ve.Add(descriptionLabel);
                }

                var rightArrow = new Label(@"›");
                rightArrow.AddToClassList("sd-right-arrow");
                ve.Add(rightArrow);

                ve.AddManipulator(new Clickable(onClick));
                return ve;
            }

            public void Build(SmartDropdown dropdown)
            {
                _dropdown = dropdown;
                dropdown.isEnabled = true;


                _rootPanel = new VisualElement() { name = "sdRoot" };
                LoadStyles(_rootPanel);
                _rootPanel.EnableInClassList("sd-as-dropdown", dropdown._showAsDropdown);

                rootVisualElement.Add(_rootPanel);


                _header = new VisualElement() { name = "sdHeader" };
                _rootPanel.Add(_header);
                if (dropdown._searchBarRequested)
                {
                    _searchGroup = new VisualElement() { name = "sdSearch" };
                    _searchText = new TextField();
                    _searchText.AddToClassList("sd-search");
                    _searchText.AddToClassList("sd-search-text");
                    _searchText.RegisterValueChangedCallback(SearchValueChanged);
                    var searchPlaceholder = new Label("Search...");
                    searchPlaceholder.AddToClassList("sd-search");
                    searchPlaceholder.AddToClassList("sd-search-placeholder");
                    searchPlaceholder.pickingMode = PickingMode.Ignore;
                    _searchText.Add(searchPlaceholder);
                    _searchGroup.Add(_searchText);
                    var searchIcon = new Image();
                    searchIcon.AddToClassList("sd-search");
                    searchIcon.AddToClassList("sd-search-icon");
                    searchIcon.pickingMode = PickingMode.Ignore;
                    _searchGroup.Add(searchIcon);
                    var searchClearButton = new Button(() => SetSearchValue("")) { text = @"⌫", tooltip = "Clear Search" };
                    searchClearButton.AddToClassList("sd-search");
                    searchClearButton.AddToClassList("sd-search-clear");
                    _searchGroup.Add(searchClearButton);
                    _header.Add(_searchGroup);

                }
                
                _body = new VisualElement() { name = "sdBody" };
                _rootPanel.Add(_body);

                _footer = new VisualElement() { name = "sdFooter" };
                _rootPanel.Add(_footer);

                var pathLabel = new Label();
                pathLabel.AddToClassList("sd-label");
                pathLabel.AddToClassList("sd-path-label");
                _footer.Add(pathLabel);

                bool requiresNavigation = dropdown._searchBarRequested || dropdown._root.children.Any(c => c is SDNodeGroup);
                _noScrollView = dropdown._root.children.Count < 15 && (!requiresNavigation || _dropdown._noScrollView);

                LoadGroup(dropdown._root,
                          PanelAnimation.None,
                          requiresNavigation);

                _hasToFit = !requiresNavigation;
                _rootPanel.visible = !_hasToFit;

                if (!requiresNavigation && !string.IsNullOrEmpty(dropdown._intent))
                {
                    var intentTitle = new Label(dropdown._intent) { name = "sdTitle" };
                    _header.Add(intentTitle);
                }

                if(dropdown._root.children.Count == 0)
                {
                    _emptyLabel = new Label("No Data Available").WithClass("sd-empty-label");
                    dropdown._root.panel.Add(_emptyLabel);
                }
            }

            public void NavigateTo(string path)
            {
                var split = path.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
                var group = _dropdown._root as SDNodeGroup;
                var count = split.Length;
                foreach(var part in split)
                {
                    count--;
                    var prevGroup = group;
                    foreach (var child in group.children)
                    {
                        if (child.rawName != part) continue;
                        
                        if (count <= 0)
                        {
                            child.visualElement?.AddToClassList("sd-checked");
                            return;
                        }

                        if (child is SDNodeGroup nextGroup)
                        {
                            group = nextGroup;
                        }
                        break;
                    }

                    if (group == prevGroup)
                    {
                        LoadPreviousGroup(_dropdown._root, PanelAnimation.None);
                        break;
                    }
                    LoadGroup(group, PanelAnimation.None);
                }
            }

            public void NavigateTo(SDNode node)
            {
                List<SDNodeGroup> groups = new List<SDNodeGroup>();
                var parent = node.parent;
                while (parent != null)
                {
                    while(!(parent is SDNodeGroup) && parent != null)
                    {
                        parent = parent.parent;
                    }

                    if (parent is SDNodeGroup group)
                    {
                        groups.Insert(0, group);
                    }
                    parent = parent?.parent;
                }

                foreach(var group in groups)
                {
                    if (!_groupStack.Contains(group))
                    {
                        _groupStack.Push(group);
                    }
                }

                if (node is SDNodeGroup groupNode)
                {
                    _groupStack.Push(groupNode);
                }

                LoadGroup(_groupStack.Pop(), PanelAnimation.None);
                if (node is not SDNodeGroup && node.visualElement != null)
                {
                    _groupStack.Peek().panel.Q<ScrollView>()?.ScrollTo(node.visualElement);
                }
            }

            private void SearchValueChanged(ChangeEvent<string> evt)
            {
                SetSearchValue(evt.newValue);
            }

            private void SetSearchValue(string searchValue)
            {
                if (string.IsNullOrEmpty(searchValue))
                {
                    ClearSearch(true);
                    return;
                }
                
                _searchText.SetValueWithoutNotify(searchValue);
                _searchGroup.EnableInClassList("sd-has-value", !string.IsNullOrEmpty(searchValue));

                var lowerValue = searchValue.ToLower();
                var foundNodes = new List<SDNode>();

                _searchRoot ??= _groupStack.Peek();

                var actualSearchRoot = _isGlobalSearch ? _dropdown._root : _searchRoot;
                // _dropdown._root.Search(lowerValue, foundNodes, lowerValue.Contains(SeparatorString), 4);
                actualSearchRoot.Search(lowerValue, foundNodes, lowerValue.Contains(SeparatorString), 4);
                
                foundNodes.Sort((a, b) => a.Path.Length.CompareTo(b.Path.Length));
                LoadSearchGroup(searchValue, foundNodes);
            }

            private void ClearSearch(bool animate)
            {
                _searchText.SetValueWithoutNotify("");
                _searchGroup.EnableInClassList("sd-has-value", false);
                
                while(_groupStack.Peek().Path.Contains(SearchGroupPath))
                {
                    _groupStack.Pop();
                }
                LoadPreviousGroup(_groupStack.Peek(), animate ? PanelAnimation.SlideRight : PanelAnimation.None);
                _searchRoot = null;
            }

            private void FitToContent()
            {
                var lastElement = _currentPanel.Query(className: "sd-element").Last();
                var startY = _rootPanel.worldBound.yMin;
                var endY = lastElement != null ? lastElement.worldBound.yMax : _rootPanel.worldBound.yMax;
                var height = endY - startY;

                _sizeDelta = position.height - height;

                maxSize = new Vector2(maxSize.x, height);

                _rootPanel.visible = true;
            }

            private void ReadjustOnScreen()
            {
                var rect = GUIUtility.GUIToScreenPoint(position.position);
                if (TargetPosition.y <= rect.y || _sizeDelta <= 10)
                {
                    // Correct position
                    return;
                }

                var newPosition = new Vector2(position.x, position.y + _sizeDelta);
                position = new Rect(GUIUtility.GUIToScreenPoint(newPosition), position.size);
            }

            private VisualElement GetVisualElementFor(SDNode node, bool createNew)
            {
                if(!createNew && node.visualElement != null)
                {
                    return node.visualElement;
                }
                switch (node)
                {
                    case SDNodeBlock block:
                        node.OnPreBuildViewCallback?.Invoke(block.block);
                        var elem = block.block.GetDrawer(CloseOnSelect, EditorGUIUtility.isProSkin);
                        if(_dropdown._overrideStyles.TryGetValue(block.block.GetType(), out StyleSheet style))
                        {
                            elem.styleSheets.Add(style);
                        }
                        block.visualElement = elem;
                        elem.AddToClassList("sd-element");
                        elem.EnableInClassList("sd-checked", block.isChecked);
                        return elem;
                    case SDNodeGroup group:
                        group.EnsureIsBuilt();
                        var groupVE = group.UIElementFactory?.Invoke(group, 
                            () => LoadGroup(group, PanelAnimation.SlideLeft), CloseOnSelect, EditorGUIUtility.isProSkin) 
                                   ?? CreateGroup(group.Name, group.Description, group.Icon, () => LoadGroup(group, PanelAnimation.SlideLeft));
                        group.visualElement = groupVE;
                        groupVE.AddToClassList("sd-element");
                        return groupVE;
                    default: 
                        throw new InvalidOperationException($"Cannot identify the type of {nameof(SmartDropdown)} Tree Node. " +
                            $"Received {node.GetType()}");
                }
            }

            private VisualElement GetSearchVisualElementFor(SDNode node, string searchValue)
            {
                switch (node)
                {
                    case SDNodeBlock block:
                        var elem = block.block.GetSearchDrawer(searchValue, CloseOnSelect, EditorGUIUtility.isProSkin);
                        if (_dropdown._overrideStyles.TryGetValue(block.block.GetType(), out StyleSheet style))
                        {
                            elem.styleSheets.Add(style);
                        }
                        elem.AddToClassList("sd-element");
                        elem.EnableInClassList("sd-checked", block.isChecked);
                        return elem;
                    case SDNodeGroup group:
                        group.EnsureIsBuilt();
                        VisualElement groupVE;
                        if(group.UISearchElementFactory != null)
                        {
                            groupVE = group.UISearchElementFactory(searchValue, group,
                                () => LoadGroup(group, PanelAnimation.SlideLeft), CloseOnSelect,
                                EditorGUIUtility.isProSkin);
                        }
                        else if(group.UIElementFactory != null)
                        {
                            groupVE = group.UIElementFactory?.Invoke(group,
                                () => LoadGroup(group, PanelAnimation.SlideLeft), CloseOnSelect,
                                EditorGUIUtility.isProSkin);
                        }
                        else
                        {
                            groupVE = CreateGroup(group.Name, group.Description, group.Icon,
                                () => LoadGroup(group, PanelAnimation.SlideLeft));
                        }
                        groupVE.AddToClassList("sd-element");
                        return groupVE;
                    default:
                        throw new InvalidOperationException($"Cannot identify the type of {nameof(SmartDropdown)} Tree Node. " +
                            $"Received {node.GetType()}");
                }
            }

            private void LoadGroup(SDNodeGroup group, PanelAnimation panelAnimation, bool showHeader = true)
            {
                _footer.EnableInClassList(ussVisible, group.Path.StartsWith(SearchGroupPath));

                var panel = group.panel;
                if (panel == null)
                {
                    panel = new VisualElement();
                    panel.AddToClassList("sd-panel");
                    panel.usageHints = UsageHints.DynamicTransform;

                    if (showHeader)
                    {
                        var panelHeader = new VisualElement() { name = "sdPanelHeader" };
                        panel.Add(panelHeader); 
                        var parentToAdd = panelHeader;
                        if (_groupStack.Count > 0)
                        {
                            foreach (var groupInStack in _groupStack.Reverse())
                            {
                                panelHeader.AddToClassList("sd-has-back");
                                var backButton = new VisualElement().WithClass("sd-back-button").WithChildren(
                                    new Image { image = groupInStack.Icon }.WithClass("sd-back-icon")
                                        .WithClassEnabled("has-value", groupInStack.Icon),
                                    new Label(groupInStack.Name).WithClass("sd-back-label"),
                                    new Label("\u2190 NAVIGATE").WithClass("navigate-hint"));
                                backButton.AddManipulator(new Clickable(() => LoadPreviousGroup(groupInStack)));
                                var backButtonParent = new VisualElement().WithClass("sd-back-parent")
                                    .WithChildren(backButton);
                                
                                parentToAdd.Add(backButtonParent);
                                
                                parentToAdd = backButtonParent;
                            }
                        }
                        var groupName = new VisualElement().WithChildren(
                            new Image { image = group.Icon }.WithClass("sd-back-icon").WithClassEnabled("has-value", group.Icon),
                            new Label(group.Name) { name = "sdPanelName" })
                            .WithClass("sd-label", "sd-header-title");
                        parentToAdd.Add(groupName);
                    }

                    var nodesList = new List<SDNode>();
                    foreach (var child in group.children)
                    {
                        if (child is SDNodeGroup { IsEmpty: true })
                        {
                            continue;
                        }
                        nodesList.Add(child);
                    }

                    VisualElement list = null;
                    if (group.children.Count < 50)
                    {
                        list = _noScrollView ? new VisualElement() : new ScrollView() { horizontalScrollerVisibility = ScrollerVisibility.Hidden};
                        foreach (var node in nodesList)
                        {
                            list.Add(GetVisualElementFor(node, false));
                        }

                        nodesList.FirstOrDefault()?.visualElement?.AddToClassList("sd-first");
                        nodesList.LastOrDefault()?.visualElement?.AddToClassList("sd-last");
                    }
                    else
                    {
                        Action<VisualElement, int> bindItem = (v, i) =>
                        {
                            v.Clear();
                            var ve = GetVisualElementFor(nodesList[i], false);
                            
                            ve.EnableInClassList("sd-first", i == 0);
                            ve.EnableInClassList("sd-last", i == nodesList.Count - 1);

                            v.Add(ve);
                        };
                        var listView = new ListView(nodesList, 24, () => new VisualElement(), bindItem);
                        listView.delegatesFocus = false;
                        listView.reorderable = false;
                        listView.selectionType = SelectionType.None;
                        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
                        listView.style.flexGrow = 1.0f;

                        list = listView;
                    }

                    list.AddToClassList("sd-list");
                    panel.Add(list);
                    group.panel = panel;
                }

                _groupStack.Push(group);
                AnimateMoveToPanel(_currentPanel, panel, panelAnimation);
                _currentPanel = panel;
            }

            private void LoadSearchGroup(string searchValue, IEnumerable<SDNode> nodesToShow)
            {
                _footer.EnableInClassList(ussVisible, true);

                SDNodeGroup searchGroup = null;

                while (_groupStack.Peek().Path.Contains(SearchGroupPath))
                {
                    searchGroup = _groupStack.Peek();
                    _groupStack.Pop();
                }

                if (searchGroup?.Path.StartsWith(SearchGroupPath) != true || searchGroup.Path.Substring(SearchGroupPath.Length) != searchValue)
                {
                    searchGroup = new SDNodeGroup(SearchGroupPath + searchValue, "Search", _dropdown._root);
                }

                var panel = searchGroup.panel;

                if (panel == null)
                {
                    panel = new VisualElement();
                    panel.AddToClassList("sd-panel");
                    panel.usageHints = UsageHints.DynamicTransform;

                    var panelHeader = new VisualElement() { name = "sdSearchPanelHeader" };
                    panel.Add(panelHeader);

                    // Clear any search groups
                    if (_groupStack.Count > 0)
                    {
                        panelHeader.AddToClassList("sd-has-back");
                        var prevGroup = _groupStack.Peek();
                        var backButton = new Button(() => SetSearchValue(""))
                        {
                            text = "Back",
                        };
                        backButton.AddToClassList("sd-back-button");
                        var backLabel = new Label(@"‹");
                        backLabel.AddToClassList("sd-back-label");
                        backButton.Add(backLabel);
                        panelHeader.Add(backButton);
                    }
                    var groupName = new Label("Search Results") { name = "sdPanelName" };
                    groupName.style.textOverflow = TextOverflow.Ellipsis;
                    groupName.AddToClassList("sd-label");
                    panelHeader.Add(groupName);
                    
                    var globalSearch = new Toggle()
                    {
                        value = _isGlobalSearch, 
                        focusable = false,
                        tooltip = "<b>Global Search</b>\nWhen enabled, search scope is global. \nWhen disabled, search is limited to the current group.",
                    }.WithClass("sd-search-type");
                    globalSearch.RegisterValueChangedCallback(e =>
                    {
                        _isGlobalSearch = e.newValue;
                        ClearSearch(false);
                        SetSearchValue(searchValue);
                    });
                    panelHeader.Add(globalSearch);

                    var nodesList = nodesToShow.ToList();

                    var pathLabel = _footer.Q<Label>(className: "sd-path-label");

                    VisualElement list = null;
                    if (nodesList.Count < 50)
                    {
                        list = new ScrollView();
                        foreach (var node in nodesList)
                        {
                            var ve = GetSearchVisualElementFor(node, searchValue);
                            ve.RegisterCallback<MouseEnterEvent>(e => pathLabel.text = node.Path.Replace('/', '→'));
                            ve.AddToClassList("sd-search-elem");
                            node.searchVisualElement = ve;
                            ve.RemoveFromClassList("sd-first");
                            ve.RemoveFromClassList("sd-last");
                            list.Add(ve);
                        }

                        nodesList.FirstOrDefault()?.visualElement?.AddToClassList("sd-first");
                        nodesList.LastOrDefault()?.visualElement?.AddToClassList("sd-last");
                    }
                    else
                    {
                        Action<VisualElement, int> bindItem = (v, i) =>
                        {
                            v.Clear();
                            var node = nodesList[i];
                            var ve = GetSearchVisualElementFor(node, searchValue);
                            ve.RegisterCallback<MouseEnterEvent>(e => pathLabel.text = node.Path.Replace('/', '→'));
                            ve.AddToClassList("sd-search-elem");
                            node.searchVisualElement = ve;

                            ve.EnableInClassList("sd-first", i == 0);
                            ve.EnableInClassList("sd-last", i == nodesList.Count - 1);

                            v.Add(ve);
                        };
                        var listView = new ListView(nodesList, 36, () => new VisualElement(), bindItem);
                        listView.delegatesFocus = false;
                        listView.unbindItem = (v, i) => v.Clear();
                        listView.reorderable = false;
                        listView.selectionType = SelectionType.None;
                        listView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
                        listView.style.flexGrow = 1.0f;

                        list = listView;
                    }

                    list.AddToClassList("sd-list");
                    panel.Add(list);
                    searchGroup.panel = panel;
                }

                _groupStack.Push(searchGroup);
                AnimateMoveToPanel(_currentPanel, panel, PanelAnimation.None);
                _currentPanel = panel;
            }

            private void LoadPreviousGroup()
            {
                if(_groupStack.Count > 1)
                {
                    _groupStack.Pop();
                    var previousGroup = _groupStack.Pop();
                    LoadGroup(previousGroup, PanelAnimation.SlideRight);
                }
            }

            private void LoadPreviousGroup(SDNodeGroup group, PanelAnimation panelAnimation = PanelAnimation.SlideRight)
            {
                while (_groupStack.Count > 1 && _groupStack.Pop() != group)
                {
                    if (_groupStack.Peek().Path.StartsWith(SearchGroupPath))
                    {
                        _searchText.SetValueWithoutNotify("");
                        _searchGroup.RemoveFromClassList("sd-has-value");
                    }
                }
                if (_groupStack.Count > 0 && _groupStack.Peek() == group)
                {
                    _groupStack.Pop();
                }
                LoadGroup(group, panelAnimation);
            }

            private void AnimateMoveToPanel(VisualElement fromPanel, VisualElement toPanel, PanelAnimation panelAnimation)
            {
                switch (panelAnimation)
                {
                    case PanelAnimation.SlideRight:
                        fromPanel.MakeAbsolute();
                        _body.Add(toPanel);
                        var amountToMove = fromPanel.resolvedStyle.width;
                        _animator.Add(new UISlideAnimation(fromPanel, _animationDuration,
                                            Vector2.right * amountToMove,
                                            null,
                                            e => { e.RemoveFromHierarchy(); e.ResetLayoutStyle(); }));
                        _animator.Add(new UISlideAnimation(toPanel, _animationDuration,
                                            Vector2.right * amountToMove,
                                            e =>
                                            {
                                                e.MakeAbsolute();
                                                e.CopyLayoutFrom(fromPanel);
                                                e.MoveAbsolutePosition(Vector2.left * amountToMove);
                                            },
                                            e =>
                                            {
                                                e.style.position = Position.Relative;
                                                e.ResetLayoutStyle();
                                            }));
                        break;
                    case PanelAnimation.SlideLeft:
                        fromPanel.MakeAbsolute();
                        _body.Add(toPanel);
                        amountToMove = fromPanel.resolvedStyle.width;
                        _animator.Add(new UISlideAnimation(fromPanel, _animationDuration,
                                            Vector2.left * amountToMove,
                                            null,
                                            e => { e.RemoveFromHierarchy(); e.ResetLayoutStyle(); }));
                        _animator.Add(new UISlideAnimation(toPanel, _animationDuration,
                                            Vector2.left * amountToMove,
                                            e =>
                                            {
                                                e.MakeAbsolute();
                                                e.CopyLayoutFrom(fromPanel);
                                                e.MoveAbsolutePosition(Vector2.right * amountToMove);
                                            },
                                            e =>
                                            {
                                                e.style.position = Position.Relative;
                                                e.ResetLayoutStyle();
                                            }));
                        break;
                    case PanelAnimation.Fade: // TODO: panel animation fade
                        toPanel.CopyLayoutFrom(fromPanel);
                        fromPanel.MakeAbsolute();
                        _body.Add(toPanel);
                        _animator.Add(new UIFadeAnimation(fromPanel, _animationDuration,
                                            false,
                                            null,
                                            e => e.RemoveFromHierarchy()));
                        _animator.Add(new UIFadeAnimation(toPanel, _animationDuration,
                                            true,
                                            e =>
                                            {
                                                e.MakeAbsolute();
                                            },
                                            e =>
                                            {
                                                e.style.position = Position.Relative;
                                                e.ResetLayoutStyle();
                                            }));
                        break;
                    case PanelAnimation.None:
                        fromPanel?.RemoveFromHierarchy();
                        _body.Add(toPanel);
                        break;
                }
            }

        }

        internal bool ContainsPath(string menuPath)
        {
            return _buildingBlocks.ContainsKey(menuPath);
        }

        private readonly Dictionary<string, SDNodeBlock> _buildingBlocks = new Dictionary<string, SDNodeBlock>();
        private readonly Dictionary<Type, StyleSheet> _overrideStyles = new Dictionary<Type, StyleSheet>();
        private readonly Dictionary<string, GroupOverrides> _groupUIsOverrides = new Dictionary<string, GroupOverrides>();

        private readonly string _intent;
        private readonly SDRoot _root;
        private readonly bool _searchBarRequested;
        private string _currentPath;

        private bool _showAsDropdown;
        private bool _noScrollView;
        private Action _onDismiss;

        public bool isEnabled { get; private set; }
        
        public int ItemsCount => _buildingBlocks.Count;
        public IPathGroup Root => _root;

        public SmartDropdown(bool withSearch, string intent = null)
        {
            _root = new SDRoot(intent ?? "Menu");
            _intent = intent;
            _searchBarRequested = withSearch;
        }

        public IPathGroup GetGroupAt(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return _root;
            }
            var group = _root.GetOrCreateGroup(path);
            return group;
        }
        
        public void SetCurrentlySelected(string path)
        {
            _currentPath = path;
        }

        public SmartDropdown Add(string path, IBuildingBlock elem, bool isSelected = false)
        {
            Profiler.BeginSample("SmartDropdown.Add");
            ThrowIfPathAlreadyExists(path);
            var nodeBlock = new SDNodeBlock(path, elem, isSelected);
            _buildingBlocks.Add(path, nodeBlock);
            _root.Add(path.Trim(_separators), nodeBlock);
            Profiler.EndSample();
            return this;
        }

        public SmartDropdown EditGroup(string path, string label, CreateGroupUIDelegate uiFactory, params IBuildingBlock[] searcheableBlocks)
        {
            Profiler.BeginSample("SmartDropdown.EditGroupWithFactory");
            if(!_groupUIsOverrides.TryGetValue(path, out var overrides))
            {
                overrides = new GroupOverrides();
                _groupUIsOverrides[path] = overrides;
            }
            overrides.nameLabel = label;
            overrides.uiFactory = uiFactory;
            overrides.searchableBlocks = searcheableBlocks;
            overrides.onGroupBuild = null;
            Profiler.EndSample();
            return this;
        }
        
        public SmartDropdown EditGroup(string path, string label, CreateGroupUIDelegate uiFactory, OnGroupBuildDelegate onBuildCallback, params IBuildingBlock[] searcheableBlocks)
        {
            Profiler.BeginSample("SmartDropdown.EditGroupWithFactory");
            if(!_groupUIsOverrides.TryGetValue(path, out var overrides))
            {
                overrides = new GroupOverrides();
                _groupUIsOverrides[path] = overrides;
            }
            overrides.nameLabel = label;
            overrides.uiFactory = uiFactory;
            overrides.searchableBlocks = searcheableBlocks;
            overrides.onGroupBuild = onBuildCallback;
            Profiler.EndSample();
            return this;
        }

        public SmartDropdown EditGroup(string path, string label, params IBuildingBlock[] searcheableBlocks)
        {
            if (!_groupUIsOverrides.TryGetValue(path, out var overrides))
            {
                overrides = new GroupOverrides();
                _groupUIsOverrides[path] = overrides;
            }
            overrides.nameLabel = label;
            overrides.searchableBlocks = searcheableBlocks;

            return this;
        }

        public SmartDropdown EditGroup(string path, string label, Texture2D icon, string descriptionLabel, params IBuildingBlock[] searcheableBlocks)
        {
            Profiler.BeginSample("SmartDropdown.EditGroup");
            if (!_groupUIsOverrides.TryGetValue(path, out var overrides))
            {
                overrides = new GroupOverrides();
                _groupUIsOverrides[path] = overrides;
            }
            overrides.nameLabel = label;
            overrides.icon = icon;
            overrides.descriptionLabel = descriptionLabel;
            overrides.searchableBlocks = searcheableBlocks;
            Profiler.EndSample();
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SmartDropdown Add(string path, Action onSelect, string name = null, bool isSelected = false)
            => Add(path, null, null, onSelect, name, isSelected);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SmartDropdown Add(string path, string type, Action onSelect, string name = null, bool isSelected = false)
            => Add(path, type, null, onSelect, name, isSelected);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SmartDropdown Add(string path, string type, Texture2D icon, Action onSelect, string name = null, bool isSelected = false)
        {
            Add(path, new DropdownItem(name, type, onSelect, icon), isSelected);
            return this;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SmartDropdown Add(string path, string type, Texture2D icon, string description,  Action onSelect, string name = null, bool isSelected = false)
        {
            Add(path, new DropdownItem(name, type, onSelect, icon, description), isSelected);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SmartDropdown Add(string path, bool value, Func<bool, bool> onToggle, string type = null, string description = null)
        {
            Add(path, new DropdownToggle(null, value, onToggle, type, description), false);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SmartDropdown Add(string path, bool value, Action<bool> onToggle, string type = null, string description = null)
        {
            Add(path, new DropdownToggle(null, value, v => { onToggle?.Invoke(v); return v; }, type, description), false);
            return this;
        }

        public SmartDropdown AddSeparator(string path = "", string name = null) => AddSeparator(path, name, null);
        public SmartDropdown AddSeparator(string path = "", string name = null, params (VisualElement element, bool applySeparatorStyle)[] additionalElements)
        {
            if (path == null)
            {
                path = "/";
            }

            Add(path, new SeparatorItem(name, additionalElements));

            return this;
        }
        
        public SmartDropdown AddSeparator(string path, string name, out Action removeAction)
        {
            if (path == null)
            {
                path = "/";
            }

            if (_buildingBlocks.ContainsKey(path))
            {
                removeAction = null;
                return this;
            }
            
            Add(path, new SeparatorItem(name));
            
            removeAction = () => Remove(path);

            return this;
        }

        private void ThrowIfPathAlreadyExists(string path)
        {
            if (_buildingBlocks.ContainsKey(path))
            {
                throw new ArgumentException($"Path {path} already added", nameof(path));
            }
        }

        public SmartDropdown AddValues<T>(IEnumerable<T> elements,
                                          Func<T, string> pathGetter,
                                          Func<T, Texture2D> iconGetter,
                                          Action<T> onSelectCallback,
                                          T selected,
                                          Func<T, string> nameGetter = null,
                                          Func<T, string> descriptionGetter = null)
        {
            if (elements == null)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            if (pathGetter == null)
            {
                throw new ArgumentNullException(nameof(pathGetter));
            }

            if (onSelectCallback == null)
            {
                throw new ArgumentNullException(nameof(onSelectCallback));
            }

            foreach (var elem in elements)
            {
                var path = pathGetter(elem);
                var name = nameGetter?.Invoke(elem) ?? GetNameFromPath(path);
                var description = descriptionGetter?.Invoke(elem);
                var icon = iconGetter?.Invoke(elem);
                Action onSelect = () => onSelectCallback(elem);
                Add(path, new DropdownItem(name, null, onSelect, icon), elem.Equals(selected));
            }
            return this;
        }

        public SmartDropdown Remove(string path)
        {
            if (_buildingBlocks.Remove(path, out SDNodeBlock node))
            {
                _root.Remove(node);
            }
            return this;
        }
        
        public SmartDropdown RemoveEmptyGroups()
        {
            _root.RemoveEmptyGroups();
            return this;
        }

        public void OverrideStyle<T>(StyleSheet style)
        {
            _overrideStyles[typeof(T)] = style;
        }

        private void ApplyGroupUIOverrides()
        {
            foreach(var pair in _groupUIsOverrides)
            {
                ApplyGroupUIOverride(pair.Key, pair.Value, false);
            }
            
            _groupUIsOverrides.Clear();
        }
        
        private void ApplyGroupUIOverride(string path, GroupOverrides overrides, bool removeFromCache = true)
        {
            if (_root.TryGetGroup(path, out var nodeGroup))
            {
                nodeGroup.ApplyOverrides(overrides);
            }
            else // <-- Create it
            {
                var group = new SDNodeGroup(path, _root);
                group.ApplyOverrides(overrides);
                _root.Add(path, group);

                //Debug.LogError("Unable to find a group at specified path: " + pair.Key);
            }
            
            if(removeFromCache)
            {
                _groupUIsOverrides.Remove(path);
            }
        }

        public void Show(Rect position,
                         Vector2? size = null,
                         bool startFromSelected = true,
                         bool isScreenPosition = false,
                         Action<CloseReason> onClose = null)
        {
            _showAsDropdown = true;
            ApplyGroupUIOverrides();
            var window = ScriptableObject.CreateInstance<SDWindow>();
            window.Build(this);
            if (!string.IsNullOrEmpty(_currentPath))
            {
                window.NavigateTo(_currentPath);
            }
            else if (startFromSelected)
            {
                var firstSelectedNode = _root.GetFirstChecked();
                if(firstSelectedNode != null)
                {
                    window.NavigateTo(firstSelectedNode);
                }
            }
            size = new Vector2(Mathf.Max(size?.x ?? position.width, 120), Mathf.Max(size?.y ?? position.height, 400));
            var screenPosition = isScreenPosition ? position : GUIUtility.GUIToScreenRect(position);
            window.TargetPosition = screenPosition;
            window.OnCloseWindow += onClose;
            window.ShowAsDropDown(screenPosition, size.Value);
            // window.position = screenPosition;
            // window.minSize = size.Value;
            // window.Show();
        }

        internal void ShowAsWindow(Rect position, Vector2? size = null, bool startFromSelected = true)
        {
            _showAsDropdown = false;
            ApplyGroupUIOverrides();
            var window = ScriptableObject.CreateInstance<SDWindow>();
            window.Build(this);
            if (!string.IsNullOrEmpty(_currentPath))
            {
                window.NavigateTo(_currentPath);
            }
            else if (startFromSelected)
            {
                var firstSelectedNode = _root.GetFirstChecked();
                if (firstSelectedNode != null)
                {
                    window.NavigateTo(firstSelectedNode);
                }
            }
            size = new Vector2(Mathf.Max(size?.x ?? position.width, 120), Mathf.Max(size?.y ?? position.height, 400));
            window.position = position;
            window.minSize = size.Value;
            window.Show();
        }
    }
}
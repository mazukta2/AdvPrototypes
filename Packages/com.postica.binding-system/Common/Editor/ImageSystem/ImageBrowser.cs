using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Postica.Common
{
    class ImageBrowser : EditorWindow
    {
        private readonly GUIContent _tagLabel = new GUIContent();

        [NonSerialized]
        private GUIStyle _tagLabelStyle;
        [NonSerialized]
        private GUIStyle _boxStyle;
        [NonSerialized]
        private GUIStyle _itemTitleStyle;

        private Vector2 _scrollOffset;

        private bool _shouldUpdateDB;
        private int _selectedNamespace;
        private string[] _namespacesNames;
        private NamespaceNode[] _namespaces;
        private BrowseItem _expandedItem;

        private string[] _effectsNames;
        private EffectCallbacks[] _effectCallbacks = new[]
        {
            new EffectCallbacks("None", 0, v => v, null),
            new EffectCallbacks("Multiply", 1f, v => EditorGUILayout.ColorField(v is Color c ? c : Color.white), (i, v) => i.MultiplyBy((Color)v)),
            new EffectCallbacks("Add", 1f, v => EditorGUILayout.ColorField(v is Color c ? c : Color.white), (i, v) => i.AddColor((Color)v)),
            new EffectCallbacks("Invert", 0f, v => GUILayout.Toggle(v is bool b && b, "Invert Alpha"), (i, v) => i.Invert((bool)v)),
        };

        private class EffectCallbacks
        {
            public readonly string name;
            public readonly float applyDelay;
            public readonly Func<object, object> drawPayload;
            public readonly Func<CodedImage, object, CodedImage> generator;

            public EffectCallbacks(string name, float applyDelay, Func<object, object> drawer, Func<CodedImage, object, CodedImage> generator)
            {
                this.name = name;
                drawPayload = drawer;
                this.generator = generator;
                this.applyDelay = applyDelay;
            }
        }

        private class Effect
        {
            private object _payload;
            private CodedImage _effecImage;
            private readonly CodedImage _original;
            private DateTime _nextChangeTime;
            private Func<CodedImage, object, CodedImage> _applyFunc;

            public CodedImage image => _effecImage ?? _original;
            public int index { get; set; }

            public Effect(CodedImage original)
            {
                _original = original;
            }

            public void DrawLayout(EffectCallbacks callbacks)
            {
                if(callbacks.generator == null)
                {
                    _effecImage = null;
                    return;
                }
                var payload = callbacks.drawPayload(_payload);
                if(!Equals(payload, _payload))
                {
                    _payload = payload;
                    if (callbacks.applyDelay > 0)
                    {
                        _applyFunc = callbacks.generator;
                        _nextChangeTime = DateTime.Now.AddSeconds(callbacks.applyDelay);
                    }
                    else
                    {
                        _effecImage = callbacks.generator(_original, _payload);
                        _applyFunc = null;
                    }
                }
            }

            public bool ApplyChanges()
            {
                if(_applyFunc == null)
                {
                    return true;
                }
                if(DateTime.Now > _nextChangeTime)
                {
                    _effecImage = _applyFunc(_original, _payload);
                    _applyFunc = null;
                    return true;
                }
                return false;
            }
        }

        private class NamespaceNode
        {
            public readonly string name;
            public readonly ClassNode[] children;

            public NamespaceNode(string name, Type[] types)
            {
                this.name = string.IsNullOrEmpty(name) ? "No Namespace" : name;

                children = types.Where(HasCodedImages).Select(t => new ClassNode(t)).ToArray();
            }
        }

        private class ClassNode
        {
            public readonly ClassNode[] children;

            public readonly Type type;
            public readonly string name;
            public readonly BrowseItem[] items;

            public bool isExpanded;

            public ClassNode(Type type)
            {
                this.type = type;
                name = type.Name;
                var codedImageType = typeof(CodedImage);
                items = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
                            .Where(IsStaticCodedImage)
                            .Select(p => new BrowseItem(p.GetValue(null) as CodedImage, p))
                            .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
                                .Where(IsStaticCodedImage)
                                .Select(f => new BrowseItem(f.GetValue(null) as CodedImage, f)))
                            .ToArray();

                children = type.GetNestedTypes(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public)
                               .Where(HasCodedImages).Select(t => new ClassNode(t)).ToArray();
            }
        }

        class BrowseItem
        {
            public readonly CodedImage source;
            public readonly string format;
            public readonly string size;
            public readonly bool isPng;
            public readonly string dimensions;
            public readonly string identifier;
            public readonly string filepath;
            public readonly string type;
            public readonly bool isReadOnly;
            public readonly bool isStatic;
            public readonly Effect effect;

            public BrowseItem(CodedImage image, MemberInfo memberInfo)
            {
                source = image;
                effect = new Effect(image);
                isPng = image.IsPNGorJPEG;
                identifier = image.Identifier;
                dimensions = $"{image.Size.width} x {image.Size.height}";
                switch (memberInfo)
                {
                    case PropertyInfo p:
                        type = "Property";
                        isReadOnly = !p.CanWrite;
                        isStatic = p.GetMethod.IsStatic;
                        break;
                    case FieldInfo f:
                        type = "Field";
                        isReadOnly = f.IsInitOnly;
                        isStatic = f.IsStatic;
                        break;
                }
                var bytes = image.Bytes.Length;
                if (bytes > 1024 * 1024)
                {
                    size = $"{bytes / (1024f * 1024):0.00} MB";
                }
                else
                {
                    size = $"{bytes / (1024f):0.00} KB";
                }
                format = image.Format.ToString();
            }

            public override int GetHashCode()
            {
                return source.GetHashCode() ^ identifier.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is BrowseItem other && other.source == source;
            }
        }

#if INTERNAL_TEST
        [MenuItem("Binding/Image Browser")]
#endif
        static void ShowConverterWindow()
        {
            GetWindow<ImageBrowser>();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Image Browser");
            _namespaces = TypeCache.GetTypesDerivedFrom<object>()
                                   .GroupBy(t => t.Namespace)
                                   .Select(g => new NamespaceNode(g.Key, g.ToArray()))
                                   .Where(n => n.children?.Length > 0)
                                   .ToArray();
            _namespacesNames = _namespaces.Select(n => n.name).ToArray();
            _selectedNamespace = _namespacesNames?.Length > 0 ? 0 : -1;
            _effectsNames = _effectCallbacks.Select(e => e.name).ToArray();
            _shouldUpdateDB = false;
        }

        private void OnGUI()
        {
            if (_tagLabelStyle == null)
            {
                InitializeStyles();
            }

            _selectedNamespace = EditorGUILayout.Popup("Namespace", _selectedNamespace, _namespacesNames);

            GUILayout.Space(10);

            GUILayout.Label(GUITools.Content("Browse Classes"), EditorStyles.centeredGreyMiniLabel);
            _scrollOffset = EditorGUILayout.BeginScrollView(_scrollOffset);
            {
                if (_selectedNamespace >= 0)
                {
                    foreach (var child in _namespaces[_selectedNamespace].children)
                    {
                        DrawClassNode(child, EditorGUI.indentLevel);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (_shouldUpdateDB)
            {
                EditorGUILayout.BeginHorizontal(_boxStyle);
                GUILayout.FlexibleSpace();
                if(GUILayout.Button("Refresh Assets"))
                {
                    AssetDatabase.Refresh();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawClassNode(ClassNode classNode, int indentLevel)
        {
            var prevIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indentLevel;
            classNode.isExpanded = EditorGUILayout.Foldout(classNode.isExpanded, classNode.name, true);
            if (!classNode.isExpanded)
            {
                return;
            }
            foreach (var child in classNode.children)
            {
                DrawClassNode(child, indentLevel + 1);
            }
            foreach (var item in classNode.items)
            {
                DrawBrowsableItem(item);
            }
            EditorGUI.indentLevel = prevIndentLevel;
        }

        private void DrawBrowsableItem(BrowseItem item)
        {
            var orange = new Color(1, 0.5f, 0);
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal(_boxStyle, GUILayout.ExpandHeight(false));
            {
                var removeButtonRect = GUILayoutUtility.GetRect(20, 20);
                using (GUITools.PushState())
                {
                    GUI.backgroundColor = Color.red;
                    if(GUI.Button(removeButtonRect, "-"))
                    {
                        RemoveItem(item);
                    }
                }
                GUILayout.Space(2);
                var side = item == _expandedItem ? 100 : 34;
                var texRect = GUILayoutUtility.GetRect(side, side, _boxStyle, GUILayout.Width(side));
                EditorGUI.DrawTextureTransparent(texRect, item.effect.image);
                if(Event.current.type == EventType.MouseUp && texRect.Contains(Event.current.mousePosition))
                {
                    _expandedItem = item == _expandedItem ? null : item;
                    Repaint();
                }
                EditorGUILayout.BeginVertical();
                {
                    //GUILayout.FlexibleSpace();
                    GUILayout.Label(item.identifier, _itemTitleStyle);
                    GUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal(/*GUILayout.Width(90)*/);
                    {
                        if (item.isStatic)
                        {
                            DrawTagLayout("Static", bgColor: Color.gray, fgColor: Color.green, rectWidth: 70);
                            GUILayout.Space(4);
                        }
                        if (item.isReadOnly)
                        {
                            DrawTagLayout("ReadOnly", bgColor: Color.gray, fgColor: Color.yellow, rectWidth: 70);
                            GUILayout.Space(4);
                        }
                        DrawTagLayout(item.type, bgColor: Color.gray, fgColor: orange, rectWidth: 70);
                        GUILayout.Space(4);
                    }
                    EditorGUILayout.EndHorizontal();
                    if(_expandedItem == item)
                    {
                        // Draw effects part
                        EditorGUILayout.BeginHorizontal();
                        var labelWidth = EditorGUIUtility.labelWidth;
                        EditorGUIUtility.labelWidth = 80;
                        item.effect.index = EditorGUILayout.Popup("Apply Effect", item.effect.index, _effectsNames);
                        EditorGUIUtility.labelWidth = labelWidth;
                        item.effect.DrawLayout(_effectCallbacks[item.effect.index]);
                        EditorGUILayout.EndHorizontal();
                        if (!item.effect.ApplyChanges())
                        {
                            Repaint();
                        }
                    }
                    //GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                GUILayout.BeginVertical();
                { 
                    //GUILayout.FlexibleSpace();
                    EditorGUILayout.BeginHorizontal();
                    {
                        DrawTagLayout(item.dimensions, bgColor: Color.gray, fgColor: Color.green, rectWidth: 70);
                        GUILayout.Space(4);
                        DrawTagLayout(item.isPng ? "PNG File" : "Raw File", bgColor: Color.gray, fgColor: Color.cyan, rectWidth: 70);
                    }
                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    {
                        DrawTagLayout(item.format, bgColor: Color.gray, fgColor: Color.yellow, rectWidth: 70);
                        GUILayout.Space(4);
                        DrawTagLayout(item.size, bgColor: Color.gray, fgColor: orange, rectWidth: 70);
                    }
                    EditorGUILayout.EndHorizontal();
                    //GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RemoveItem(BrowseItem item)
        {
            if (string.IsNullOrEmpty(item.filepath))
            {
                // Find the item
                return; // <-- return for now
            }
            var fileContents = File.ReadAllText(item.filepath);
            var length = fileContents.Length;
            var startIndex = fileContents.IndexOf("// Start " + item.identifier);
            var endIndex = fileContents.IndexOf("// End " + item.identifier);
            var firstPart = fileContents.Substring(0, startIndex).TrimEnd();
            var lastPart = fileContents.Substring(endIndex, length - endIndex);

            File.WriteAllText(item.filepath, firstPart + lastPart);
            _shouldUpdateDB = true;
        }

        private void InitializeStyles()
        {
            _tagLabelStyle = new GUIStyle("Box")
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                //margin = new RectOffset(4, 4, 0, 0),
            };
            var color = Color.white;
            color.a = 0.6f;
            _tagLabelStyle.normal.textColor = color;

            _boxStyle = new GUIStyle("Box");

            _itemTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
            };
        }

        private void DrawTag(Rect rect, string label, Color? bgColor = null, Color? fgColor = null)
        {
            if (Event.current.type == EventType.Repaint)
            {
                using (GUITools.PushState())
                {
                    GUI.backgroundColor = bgColor ?? GUI.backgroundColor;
                    GUI.contentColor = fgColor ?? GUI.contentColor;
                    _tagLabel.text = label;
                    _tagLabelStyle.Draw(rect, _tagLabel, false, false, false, false);
                }
            }
        }

        private void DrawTagLayout(string label, Color? bgColor = null, Color? fgColor = null, float? rectWidth = null)
        {
            _tagLabel.text = label;
            var width = rectWidth ?? _tagLabelStyle.CalcSize(_tagLabel).x;
            var rect = GUILayoutUtility.GetRect(width, EditorGUIUtility.singleLineHeight, GUILayout.Width(width));
            if (Event.current.type == EventType.Repaint)
            {
                using (GUITools.PushState())
                {
                    GUI.backgroundColor = bgColor ?? GUI.backgroundColor;
                    GUI.contentColor = fgColor ?? GUI.contentColor;
                    _tagLabelStyle.Draw(rect, _tagLabel, false, false, false, false);
                }
            }
        }

        private static bool HasCodedImages(Type t)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static;
            return t.GetProperties(flags).Any(IsStaticCodedImage) || t.GetFields(flags).Any(IsStaticCodedImage);
        }

        private static bool IsCodedImage(Type t) => t == typeof(CodedImage) || t.IsSubclassOf(typeof(CodedImage));
        private static bool IsStaticCodedImage(PropertyInfo p)
            => p.GetMethod?.IsStatic == true && (p.PropertyType == typeof(CodedImage) || p.PropertyType.IsSubclassOf(typeof(CodedImage)));
        private static bool IsStaticCodedImage(FieldInfo f)
            => f.IsStatic && (f.FieldType == typeof(CodedImage) || f.FieldType.IsSubclassOf(typeof(CodedImage)));
    }
}
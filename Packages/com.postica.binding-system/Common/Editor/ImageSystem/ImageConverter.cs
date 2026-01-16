using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Postica.Common
{
    public class ImageConverter : EditorWindow
    {
        private readonly GUIContent _assetLabel = new GUIContent("Texture/Folder");
        private readonly GUIContent _classLabel = new GUIContent("Class Name");
        private readonly GUIContent _namespaceLabel = new GUIContent("Namespace");
        private readonly GUIContent _filepathLabel = new GUIContent("Path");
        private readonly GUIContent _tagLabel = new GUIContent();

        [NonSerialized]
        private GUIStyle _tagLabelStyle;
        [NonSerialized]
        private GUIStyle _boxStyle;
        [NonSerialized]
        private GUIStyle _placeholderStyle;

        private string _namespace;
        private string _className;
        private string _filePath;
        private Vector2 _scrollOffset;
        private List<Object> _assetsToConvert = new List<Object>();

        private bool _globalApplyAll;
        private bool _globalSaveAsPNG = true;
        private int _globalWidth = 256;
        private int _globalHeight = 256;


        public int GlobalWidth
        {
            get { return _globalWidth; }
            set 
            {
                if (_globalWidth != value)
                {
                    _globalWidth = value;
                    foreach(var list in _conversionItems.Values)
                    {
                        foreach (var item in list)
                        {
                            if(_globalApplyAll || !item.changed)
                            {
                                item.width = value;
                            }
                        }
                    }
                }
            }
        }


        public int GlobalHeight
        {
            get { return _globalHeight; }
            set
            {
                if (_globalHeight != value)
                {
                    _globalHeight = value;
                    foreach (var list in _conversionItems.Values)
                    {
                        foreach (var item in list)
                        {
                            if (_globalApplyAll || !item.changed)
                            {
                                item.height = value;
                            }
                        }
                    }
                }
            }
        }


        public bool GlobalSaveAsPNG
        {
            get { return _globalSaveAsPNG; }
            set
            {
                if (_globalSaveAsPNG != value)
                {
                    _globalSaveAsPNG = value;
                    foreach (var list in _conversionItems.Values)
                    {
                        foreach (var item in list)
                        {
                            if (_globalApplyAll || !item.changed)
                            {
                                item.saveAsPNG = value;
                            }
                        }
                    }
                }
            }
        }



        private Dictionary<Object, List<ConversionItem>> _conversionItems = new Dictionary<Object, List<ConversionItem>>();

        private class ConversionItem
        {
            private int _width;
            private int _height;
            private bool _saveAsPng;
            private bool _changed;

            public readonly Texture2D source;
            public readonly string format;
            public readonly string size;
            public readonly string filePath;
            public readonly (int width, int heigh) originalSize;
            public readonly bool isPng;
            public readonly float aspectRatio;
            public readonly bool isFromCode;

            public string identifier;
            public bool convert;
            public bool lockAspectRatio;

            public bool saveAsPNG
            {
                get => _saveAsPng;
                set
                {
                    if(_saveAsPng != value)
                    {
                        _saveAsPng = value;
                        UpdateChanged();
                    }
                }
            }

            public int width
            {
                get => _width;
                set
                {
                    if (_width != value)
                    {
                        _width = value;
                        if (lockAspectRatio)
                        {
                            _height = Mathf.CeilToInt(_width / aspectRatio);
                        }
                        UpdateChanged();
                    }
                }
            }

            public int height
            {
                get => _height;
                set
                {
                    if(_height != value)
                    {
                        _height = value;
                        if (lockAspectRatio)
                        {
                            _width = Mathf.CeilToInt(_height * aspectRatio);
                        }
                        UpdateChanged();
                    }
                }
            }

            public bool changed => _changed;

            public bool willResample => originalSize.width != width || originalSize.heigh != height;

            public ConversionItem(Texture2D texture, string file)
            {
                source = texture;
                filePath = file;
                isPng = Path.GetExtension(file).ToLower().Contains("png");
                originalSize = (texture.width, texture.height);
                aspectRatio = texture.width / (float)texture.height;

                if(texture is Texture2D tex2D)
                {
                    var bytes = tex2D.GetRawTextureData().Length;
                    if (bytes > 1024 * 1024)
                    {
                        size = $"{bytes / (1024f * 1024):0.00} MB";
                    }
                    else
                    {
                        size = $"{bytes / (1024f):0.00} KB";
                    }
                    format = tex2D.format.ToString();
                }
                else
                {
                    size = "-- KB";
                    format = "Not 2D";
                }
                Reset();
            }

            public void Reset()
            {
                identifier = source.name;
                _width = source.width;
                _height = source.height;
                _changed = false;
                convert = true;
                saveAsPNG = true;
                lockAspectRatio = true;
            }

            private void UpdateChanged()
            {
                _changed = _width != source.width || _height != source.height || !_saveAsPng;
            }

            public override int GetHashCode()
            {
                return source.GetHashCode() ^ identifier.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return obj is ConversionItem other && other.source == source;
            }
        }

        private class Generator
        {
            private const int BYTES_PER_LINE = 20;

            private const string GENERATE_FILE_TEMPLATE = @"
/***************************************************
*                  AUTO-GENERATED                  *
*   Please do not modify this file as it may be    *
*     overwritten by the ImageConverter tool       *
***************************************************/

using Postica.BindingSystem;
using UnityEngine;

namespace %NAMESPACE%
{
    public class %CLASSNAME%
    {
        %IMAGES%
        // END OF IMAGES
    }
}";

            private const string GENERATE_IMAGE_TEMPLATE = @"
        // Start %IMAGE_NAME% Bytes
        public static readonly CodedImage %IMAGE_NAME% = new GenericImage(nameof(%IMAGE_NAME%), %WIDTH%, %HEIGHT%, %IS_PNG%, TextureFormat.%FORMAT%, new byte[]{ %BYTES%
        });
        // End %IMAGE_NAME% Bytes";

            public string Namespace { get; }
            public string ClassName { get; }
            public List<ConversionItem> Items { get; }

            public Generator(string ns, string classname, IEnumerable<ConversionItem> items)
            {
                Namespace = ns;
                ClassName = classname;
                Items = new List<ConversionItem>(items);
            }

            public void Generate(string filepath, bool append, bool overwrite = true)
            {
                var itemsToProcess = Items.Where(i => i.convert);

                // Check if there are already existing items in the code
                if(append && File.Exists(filepath))
                {
                    var fileText = File.ReadAllText(filepath);
                    if (overwrite)
                    {
                        var existingItems = itemsToProcess.Where(i => fileText.Contains($"// Start {i.identifier} Bytes"));
                        if (existingItems.Any())
                        {
                            foreach (var item in existingItems)
                            {
                                fileText = DeleteItemInText(fileText, item);
                            }

                            File.WriteAllText(filepath, fileText);
                        }
                    }
                    else
                    {
                        var existingItem = itemsToProcess.FirstOrDefault(i => fileText.Contains(string.Concat("// Start ", i.identifier)));
                        if (existingItem != null)
                        {
                            throw new InvalidOperationException($"Image with identifier {existingItem.identifier} already exists in file");
                        }
                    }
                }

                // TODO: Make async and use Progress
                StringBuilder sb = new StringBuilder();
                StringBuilder bstring = new StringBuilder();
                foreach(var item in itemsToProcess)
                {
                    bstring.Clear();
                    var bytes = ComputeBytes(item);
                    var bytesLength = bytes.Length;
                    for (int i = 0; i < bytesLength; i += BYTES_PER_LINE)
                    {
                        bstring.AppendLine().Append(' ', 12); // space
                        var remainingBytes = Mathf.Min(bytesLength - i, BYTES_PER_LINE);
                        for (int j = 0; j < remainingBytes; j++)
                        {
                            bstring.Append('0').Append('x').Append(bytes[i + j].ToString("X2")).Append(',').Append(' ');
                        }
                    }
                    bstring.Length -= 2; // remove the last comma and space

                    sb.Append(GENERATE_IMAGE_TEMPLATE.Replace("%IMAGE_NAME%", item.identifier)
                                                     .Replace("%WIDTH%", item.width.ToString())
                                                     .Replace("%HEIGHT%", item.height.ToString())
                                                     .Replace("%IS_PNG%", item.saveAsPNG ? "true" : "false")
                                                     .Replace("%BYTES%", bstring.ToString()))
                                                     .Replace("%FORMAT%", item.saveAsPNG ? TextureFormat.ARGB32.ToString() : item.format)
                      .AppendLine();
                }
                
                var directory = Path.GetDirectoryName(filepath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string fileContents;
                if (append && File.Exists(filepath))
                {
                    sb.AppendLine().Append("        // END OF IMAGES");
                    fileContents = File.ReadAllText(filepath).Replace("// END OF IMAGES", sb.ToString());
                }
                else
                {
                    fileContents = GENERATE_FILE_TEMPLATE.Replace("%NAMESPACE%", Namespace)
                                                            .Replace("%CLASSNAME%", ClassName)
                                                            .Replace("%IMAGES%", sb.ToString());
                }
                File.WriteAllText(filepath, fileContents);

                AssetDatabase.ImportAsset(filepath.Replace(Application.dataPath, "Assets").Replace('\\', '/'));
            }

            private static string DeleteItemInText(string fileText, ConversionItem item)
            {
                var length = fileText.Length;
                var startText = $"// Start {item.identifier} Bytes";
                var endText = $"// End {item.identifier} Bytes";
                var startIndex = fileText.IndexOf(startText);
                var endIndex = fileText.IndexOf(endText) + endText.Length;
                var firstPart = fileText.Substring(0, startIndex).TrimEnd();
                var lastPart = fileText.Substring(endIndex, length - endIndex).TrimStart();

                return firstPart + lastPart;
            }

            private byte[] ComputeBytes(ConversionItem item)
            {
                using (SetTextureImporterFormat(item.source, true, item.saveAsPNG))
                {
                    Texture2D texture = null;
                    Texture2D source = item.source;
                    if (item.willResample)
                    {
                        texture = new Texture2D(item.width, item.height, TextureFormat.BGRA32, false);
                        var halfPixelWidth = 0.5f / item.width;
                        var halfPixelHeight = 0.5f / item.height;
                        for (int y = 0; y < item.height; y++)
                        {
                            var v = y / (float)item.height + halfPixelHeight;
                            for (int x = 0; x < item.width; x++)
                            {
                                var u = x / (float)item.width + halfPixelWidth;
                                var pixel = source.GetPixelBilinear(u, v);
                                texture.SetPixel(x, y, pixel);
                            }
                        }
                        texture.Apply();
                    }
                    else
                    {
                        texture = new Texture2D(item.source.width, item.source.height, item.source.format, false);
                        SetTextureImporterFormat(texture, true, item.saveAsPNG);
                        texture.LoadRawTextureData(item.source.GetRawTextureData());
                        texture.Apply();
                    }
                    if (item.saveAsPNG)
                    {
                        var bytes = texture.EncodeToPNG();
                        if (bytes?.Length > 0)
                        {
                            return bytes;
                        }
                        return texture.GetRawTextureData();
                    }
                    else
                    {
                        return texture.GetRawTextureData();
                    }
                }
            }

            public static TemporaryData SetTextureImporterFormat(Texture2D texture, bool isReadable, bool forPng)
            {
                if (null == texture || texture.isReadable == isReadable) return default;

                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (AssetImporter.GetAtPath(assetPath) is TextureImporter tImporter && tImporter)
                {
                    var textureType = tImporter.textureType;
                    var wasReadable = tImporter.isReadable;
                    var compression = tImporter.textureCompression;

                    tImporter.textureType = TextureImporterType.Default;
                    tImporter.isReadable = isReadable;
                    if (forPng)
                    {
                        tImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    }

                    AssetDatabase.ImportAsset(assetPath);
                    AssetDatabase.Refresh();

                    return new TemporaryData(() =>
                    {
                        tImporter.textureCompression = compression;
                        tImporter.isReadable = wasReadable;
                        tImporter.textureType = textureType;

                        AssetDatabase.ImportAsset(assetPath);
                        AssetDatabase.Refresh();
                    });
                }

                return default;
            }
        }

#if INTERNAL_TEST
        [MenuItem("Binding/Image Converter")]
#endif
        static void ShowConverterWindow()
        {
            GetWindow<ImageConverter>();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Image Converter");
        }

        private void OnGUI()
        {
            if (_tagLabelStyle == null)
            {
                InitializeStyles();
            }

            for (int i = 0; i < _assetsToConvert.Count; i++)
            {
                var asset = _assetsToConvert[i];
                if (DrawAssetToConvert(asset, out Object changedAsset))
                {
                    _conversionItems.Remove(asset);
                    if (changedAsset)
                    {
                        if (!_assetsToConvert.Contains(changedAsset))
                        {
                            _assetsToConvert[i] = changedAsset;
                        }
                    }
                    else
                    {
                        _assetsToConvert.RemoveAt(i--);
                    }
                }
            }

            Object newAsset = null;
            EditorGUILayout.BeginHorizontal();
            {
                newAsset = EditorGUILayout.ObjectField(_assetLabel, null, typeof(Object), false);
                DrawTagLayout("new", bgColor: Color.black, fgColor: Color.cyan, rectWidth: 60);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            _namespace = EditorGUILayout.TextField(_namespaceLabel, _namespace);
            _className = EditorGUILayout.TextField(_classLabel, _className);

            EditorGUILayout.BeginVertical(_boxStyle);
            {
                GUILayout.Label(GUITools.Content("Overrides"), EditorStyles.centeredGreyMiniLabel);
                _globalApplyAll = EditorGUILayout.Toggle("Apply to all", _globalApplyAll);
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Size", GUILayout.Width(40));
                    GUILayout.Space(10);
                    GlobalWidth = EditorGUILayout.IntField(GlobalWidth, GUILayout.Width(40));
                    GUILayout.Label(GUITools.Content(" x"), GUILayout.Width(20));
                    GlobalHeight = EditorGUILayout.IntField(GlobalHeight, GUILayout.Width(40));
                    GUILayout.Space(10);
                    GlobalSaveAsPNG = GUILayout.Toggle(GlobalSaveAsPNG, GUITools.Content("Save as PNG"));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Reset"))
                    {
                        foreach (var list in _conversionItems.Values)
                        {
                            foreach (var item in list)
                            {
                                if (_globalApplyAll || !item.changed)
                                {
                                    item.Reset();
                                }
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Label(GUITools.Content("Conversion Items"), EditorStyles.centeredGreyMiniLabel);
            _scrollOffset = EditorGUILayout.BeginScrollView(_scrollOffset);
            {
                foreach (var asset in _assetsToConvert)
                {
                    DrawConversionItems(asset);
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal(_boxStyle);
            {
                GUILayout.Label(_filepathLabel, GUILayout.Width(40));
                var pathRect = GUILayoutUtility.GetRect(300, EditorGUIUtility.singleLineHeight);
                _filePath = EditorGUI.TextField(pathRect, _filePath);
                GUI.Label(pathRect, "Drag Folder Here", _placeholderStyle);
                pathRect = new Rect(pathRect.x - 2, pathRect.y - 2, pathRect.width + 4, pathRect.height + 4);
                if (DragAndDrop.objectReferences.Any(r => r is DefaultAsset) && pathRect.Contains(Event.current.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    EditorGUI.DrawRect(pathRect, Color.cyan.WithAlpha(0.2f));
                    if(Event.current.type == EventType.MouseUp || Event.current.type == EventType.DragPerform)
                    {
                        var firstFolder = DragAndDrop.objectReferences.FirstOrDefault(a => a is DefaultAsset);
                        var path = AssetDatabase.GetAssetPath(firstFolder);
                        _filePath = path;
                        DragAndDrop.AcceptDrag();
                    }
                }
                var localPath = string.Concat("/", _className, ".cs");
                GUILayout.Label(localPath);
                GUILayout.FlexibleSpace();
                using (GUITools.PushState())
                {
                    GUI.backgroundColor = Color.cyan;
                    if (GUILayout.Button("Create"))
                    {
                        var generator = new Generator(_namespace, _className, _conversionItems.Values.SelectMany(l => l).Distinct());
                        generator.Generate(Path.Combine(_filePath, _className + ".cs"), false);
                    }
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("Modify"))
                    {
                        var generator = new Generator(_namespace, _className, _conversionItems.Values.SelectMany(l => l).Distinct());
                        generator.Generate(Path.Combine(_filePath, _className + ".cs"), true);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            // Leave at the end to avoid pre-emptive modifications
            if (newAsset is Texture2D || newAsset is DefaultAsset)
            {
                if(_assetsToConvert.Count == 0 && string.IsNullOrEmpty(_className))
                {
                    if (string.IsNullOrEmpty(_namespace))
                    {
                        _namespace = EditorSettings.projectGenerationRootNamespace;
                    }
                    if(newAsset is Texture2D)
                    {
                        var filepath = AssetDatabase.GetAssetPath(newAsset);
                        _className = Path.GetDirectoryName(filepath);
                    }
                    else
                    {
                        _className = newAsset.name.Replace(" ", string.Empty);
                    }
                }
                _assetsToConvert.Add(newAsset);
            }
        }

        private void DrawConversionItems(Object asset)
        {
            if(!_conversionItems.TryGetValue(asset, out List<ConversionItem> items))
            {
                items = new List<ConversionItem>();
                if(asset is Texture2D texture)
                {
                    items.Add(new ConversionItem(texture, AssetDatabase.GetAssetPath(texture)));
                }
                else
                {
                    var folderPath = AssetDatabase.GetAssetPath(asset);
                    foreach(var file in Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories))
                    {
                        if (Path.GetExtension(file).Contains("meta"))
                        {
                            continue;
                        }
                        var textureInFolder = AssetDatabase.LoadAssetAtPath<Texture2D>(file.Replace('\\', '/').Replace(Application.dataPath, "Assets"));
                        if (textureInFolder)
                        {
                            items.Add(new ConversionItem(textureInFolder, file));
                        }
                    }
                }
                _conversionItems[asset] = items;
            }

            foreach(var item in items)
            {
                DrawConversionItem(item);
            }
        }

        private void DrawConversionItem(ConversionItem item)
        {
            var space = 20;
            GUILayout.Space(4);
            var bgColor = GUI.backgroundColor;
            if (item.changed)
            {
                GUI.backgroundColor = new Color(0, 1f, 0.5f, 0.9f);
            }
            EditorGUILayout.BeginHorizontal(_boxStyle);
            GUI.backgroundColor = bgColor;
            {
                item.convert = GUILayout.Toggle(item.convert, GUIContent.none, GUILayout.Width(20));
                using (new EditorGUI.DisabledScope(!item.convert))
                {
                    var texRect = GUILayoutUtility.GetRect(52, 52, _boxStyle, GUILayout.Width(52));
                    EditorGUI.DrawTextureTransparent(texRect, item.source);
                    GUILayout.Space(space);
                    EditorGUILayout.BeginVertical();
                    {
                        item.identifier = EditorGUILayout.TextField(item.identifier);
                        EditorGUILayout.BeginHorizontal(/*GUILayout.Width(90)*/);
                        {
                            item.lockAspectRatio = GUILayout.Toggle(item.lockAspectRatio, GUITools.Content("Lock"), EditorStyles.miniButton);
                            GUILayout.Space(10);
                            item.width = EditorGUILayout.IntField(item.width, GUILayout.Width(40));
                            GUILayout.Label(GUITools.Content(" x"), GUILayout.Width(20));
                            item.height = EditorGUILayout.IntField(item.height, GUILayout.Width(40));
                            //if (item.isPng)
                            {
                                GUILayout.FlexibleSpace();
                                item.saveAsPNG = GUILayout.Toggle(item.saveAsPNG, GUITools.Content("Save as PNG"));
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        {
                            DrawTagLayout(item.isPng ? "PNG File" : "Raw File", bgColor: Color.gray, fgColor: Color.cyan, rectWidth: 70);
                            GUILayout.Space(4);
                            DrawTagLayout(item.format, bgColor: Color.gray, fgColor: Color.yellow, rectWidth: 70);
                            GUILayout.Space(4);
                            DrawTagLayout(item.size, bgColor: Color.gray, fgColor: Color.green, rectWidth: 70);
                            if (item.willResample)
                            {
                                GUILayout.Space(4);
                                DrawTagLayout("Will Resample", bgColor: Color.gray, fgColor: Color.red, rectWidth: 90);
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();

                    GUILayout.Space(space);

                }
                if (GUILayout.Button(GUITools.Content("Reset"), EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    item.Reset();
                }
            }
            EditorGUILayout.EndHorizontal();
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

            _placeholderStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };
            _placeholderStyle.normal.textColor = _placeholderStyle.normal.textColor.WithAlpha(0.5f);
        }

        private bool DrawAssetToConvert(Object asset, out Object newAsset)
        {
            EditorGUILayout.BeginHorizontal();
            newAsset = EditorGUILayout.ObjectField(_assetLabel, asset, typeof(Object), false);
            var tagRect = GUILayoutUtility.GetRect(60, EditorGUIUtility.singleLineHeight, GUILayout.Width(60));
            if (Event.current.type == EventType.Repaint)
            {
                if (newAsset is DefaultAsset)
                {
                    DrawTag(tagRect, "folder", bgColor: Color.gray, fgColor: Color.yellow);
                }
                else if (newAsset is Texture2D)
                {
                    DrawTag(tagRect, "texture", bgColor: Color.gray, fgColor: Color.green);
                }
                else
                {
                    DrawTag(tagRect, "new", bgColor: Color.gray, fgColor: Color.cyan);
                }
            }
            EditorGUILayout.EndHorizontal();
            if(newAsset != asset)
            {
                return true;
            }
            return false;
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
    }
}
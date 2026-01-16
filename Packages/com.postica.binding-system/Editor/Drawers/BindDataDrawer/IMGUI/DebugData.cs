using Postica.Common;
using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using PopupWindow = Postica.Common.PopupWindow;
namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal class DebugData
        {
            private readonly DrawData[] _draws = new DrawData[16];
            private int _drawsCount = 0;
            private readonly Object _context;

            public DebugData(Object context)
            {
                _context = context;
            }

            public float GetHeight() => 16f;

            public void StoreDraw(Rect rect, Func<object, object> readFunc, Func<object, object> writeFunc)
            {
                _draws[_drawsCount++] = new DrawData
                {
                    rect = rect,
                    readFunction = readFunc,
                    writeFunction = writeFunc,
                };
            }

            public void CommitDraw(ref PropertyData data, object firstReadValue, object firstWriteValue, Styles styles, Contents contents)
            {
                if(_drawsCount == 0) { return; }

                var read = default(object);
                var write = default(object);

                try
                {
                    read = (firstReadValue is IBindDataDebug bindDebug ? bindDebug.GetRawData() : firstReadValue) ?? _draws[0].readFunction?.Invoke(null);
                }
                catch(Exception ex)
                {
                    if(firstReadValue is IBindDataDebug bindDebug && bindDebug.Source)
                    {
                        ex.Source = BuildSourceInfo(bindDebug);
                    }
                    _draws[0].readException = ex;
                    read = null;
                    Debug.LogException(ex, _context);
                }

                try
                {
                    write = firstWriteValue ?? _draws[_drawsCount - 1].writeFunction?.Invoke(null);
                }
                catch(Exception ex)
                {
                    _draws[_drawsCount - 1].writeException = ex;
                    write = null;
                    Debug.LogException(ex, _context);
                }

                for (int i = 0; i < _drawsCount; i++)
                {
                    try
                    {
                        read = _draws[i].readFunction?.Invoke(read) ?? read;
                    }
                    catch(Exception ex)
                    {
                        read = null;
                        _draws[i].readException = ex;
                        Debug.LogException(ex, _context);
                    }
                    _draws[i].readValue = read;
                    _draws[_drawsCount - i - 1].writeValue = write;
                    try
                    {
                        write = _draws[_drawsCount - i - 1].writeFunction?.Invoke(write) ?? write;
                    }
                    catch(Exception ex)
                    {
                        write = null;
                        _draws[_drawsCount - i - 1].writeException = ex;
                        Debug.LogException(ex, _context);
                    }
                }

                for (int i = 0; i < _drawsCount; i++)
                {
                    Draw(_draws[i], styles, contents);
                }

                _drawsCount = 0;
                GUITools.InspectorWindow.Repaint();
            }

            private string BuildSourceInfo(IBindDataDebug bindDebug)
            {
                return $"{bindDebug.Source}.{Accessors.AccessorPath.CleanPath(bindDebug.Path.Replace("Array.data", ""))}";
            }

            private void Draw(in DrawData data, Styles styles, Contents contents)
            {
                var labelRect = new Rect(data.rect.x, data.rect.y, 50, data.rect.height);
                GUI.Label(labelRect, "Value: ", styles.debugLabel);
                var dataRect = data.rect;
                dataRect.x += labelRect.width;
                dataRect.width -= labelRect.width;
                var drawWidth = data.readFunction != null && data.writeFunction != null ? dataRect.width * 0.5f : dataRect.width;
                var arrowRect = new Rect(dataRect.x, dataRect.y, dataRect.height, dataRect.height);
                var valueRect = new Rect(dataRect.x + arrowRect.width, dataRect.y, drawWidth - arrowRect.width, 16);
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                if(data.readFunction != null)
                {
                    GUI.DrawTexture(arrowRect, Icons.LiveDebug_ArrowDown);
                    DrawControl(valueRect, data.readException ?? data.readValue, styles);
                    var skipWidth = valueRect.width + arrowRect.width;
                    valueRect.x += skipWidth;
                    arrowRect.x += skipWidth;
                }
                if (data.writeFunction != null)
                {
                    GUI.DrawTexture(arrowRect, Icons.LiveDebug_ArrowUp);
                    DrawControl(valueRect, data.writeException ?? data.writeValue, styles);
                }
                EditorGUI.indentLevel = indent;
            }

            private static void DrawControl(Rect rect, object value, Styles styles)
            {
                switch (value)
                {
                    case Color v:
                        if(Event.current.type == EventType.Repaint)
                        {
                            styles.debugTextBox.Draw(rect, false, false, false, false);
                        }
                        EditorGUI.ColorField(rect, v);
                        break;
                    case Vector2Int v:
                        EditorGUI.SelectableLabel(rect, $"({v.x}, {v.y})", styles.debugTextBox);
                        break;
                    case Vector3Int v:
                        EditorGUI.SelectableLabel(rect, $"({v.x}, {v.y}, {v.z})", styles.debugTextBox);
                        break;
                    case Vector2 v:
                        EditorGUI.SelectableLabel(rect, $"({v.x}, {v.y})", styles.debugTextBox);
                        break;
                    case Vector3 v:
                        EditorGUI.SelectableLabel(rect, $"({v.x}, {v.y}, {v.z})", styles.debugTextBox);
                        break;
                    case Vector4 v:
                        EditorGUI.SelectableLabel(rect, $"({v.x}, {v.y}, {v.z}, {v.w})", styles.debugTextBox);
                        break;
                    case Quaternion v:
                        var euler = v.eulerAngles;
                        EditorGUI.SelectableLabel(rect, $"Euler({euler.x}, {euler.y}, {euler.z})", styles.debugTextBox);
                        break;
                    case Exception ex:
                        var e = Event.current;
                        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Zoom);
                        if (e.button == 0 && e.type == EventType.MouseUp && rect.Contains(e.mousePosition))
                        {
                            PopupWindow.Show(GUIUtility.GUIToScreenRect(rect), 
                                             new Vector2(Mathf.Max(300, rect.width), 300), 
                                             CreateExceptionInfo(ex));
                        }
                        EditorGUI.LabelField(rect, $"{ex.GetType().Name}", styles.debugErrorBox);
                        break;
                    default:
                        EditorGUI.SelectableLabel(rect, value?.ToString(), styles.debugTextBox);
                        break;
                }
            }

            private static VisualElement CreateExceptionInfo(Exception ex, float paddingShift = 0)
            {
                var container = new VisualElement();
                const float padding = 8;
                container.style.paddingBottom = padding;
                container.style.paddingTop = padding;
                container.style.paddingLeft = padding + paddingShift;
                container.style.paddingRight = padding;

                const float border = 1;
                container.style.borderBottomWidth = border;
                container.style.borderTopWidth = border;
                container.style.borderLeftWidth = border;
                container.style.borderRightWidth = border;

                var borderColor = Color.red;
                container.style.borderBottomColor = borderColor;
                container.style.borderTopColor = borderColor;
                container.style.borderLeftColor = borderColor;
                container.style.borderRightColor = borderColor;

                var type = new Label(ex.GetType().Name);
                type.style.fontSize = 12;
                type.style.color = Color.red;
                type.style.unityFontStyleAndWeight = FontStyle.Bold;

                if(ex.InnerException != null)
                {
                    container.Add(CreateExceptionInfo(ex.InnerException, 8));
                    return container;
                }

                var message = new Label(ex.Message);
                message.style.fontSize = 11;
                message.style.color = Color.red.Green(0.4f).Blue(0.4f);
                message.style.whiteSpace = WhiteSpace.Normal;

                var source = new Label("Source: " + ex.Source);
                source.style.fontSize = 9;
                source.style.whiteSpace = WhiteSpace.Normal;

                var sb = new StringBuilder();
                var method = ex.TargetSite;
                if(method != null)
                {
                    sb.AppendLine($"{method.ReflectedType.Name}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})");
                }

                var targetSite = new Label("[For Devs]: " + sb.ToString());
                targetSite.style.fontSize = 9;
                targetSite.style.marginTop = 12;
                targetSite.style.whiteSpace = WhiteSpace.Normal;

                container.Add(type);
                container.Add(message); 
                container.Add(source);
                container.Add(targetSite);
                return container;
            }

            private struct DrawData
            {
                public Rect rect;
                public Func<object, object> readFunction;
                public Func<object, object> writeFunction;
                public object readValue;
                public object writeValue;
                public Exception readException;
                public Exception writeException;
            }
        }
    }
}
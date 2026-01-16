using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    static class GUITools
    {
        private static readonly GUIContent _tempContent = new GUIContent();

        private static EditorWindow _inspector;

        public static EditorWindow InspectorWindow
        {
            get
            {
                if (!_inspector)
                {
                    var inspectorWindowType = TypeCache.GetTypesDerivedFrom<EditorWindow>().FirstOrDefault(e => e.FullName == "UnityEditor.InspectorWindow");
                    _inspector = EditorWindow.GetWindow(inspectorWindowType);
                }
                return _inspector;
            }
        }

        public struct FoldoutState
        {
            public readonly bool originalValue;
            public readonly SerializedProperty property;
            public bool value;

            public FoldoutState(SerializedProperty property)
            {
                this.property = property;
                originalValue = property.isExpanded;
                value = originalValue;
            }

            public bool Apply()
            {
                property.isExpanded = value;
                return value != originalValue;
            }
            public void Reset() => property.isExpanded = originalValue;
        }

        public struct State : IDisposable
        {
            private Matrix4x4 _matrix;
            private Color _color;
            private Color _bgColor;
            private Color _fgColor;
            private float _labelWidth;
            private bool _enabled;

            internal State(bool _)
            {
                _color = GUI.color;
                _matrix = GUI.matrix;
                _bgColor = GUI.backgroundColor;
                _fgColor = GUI.contentColor;
                _labelWidth = EditorGUIUtility.labelWidth;
                _enabled = GUI.enabled;
            }

            public void Dispose()
            {
                GUI.contentColor = _fgColor;
                GUI.backgroundColor = _bgColor;
                GUI.color = _color;
                GUI.matrix = _matrix;
                EditorGUIUtility.labelWidth = _labelWidth;
                GUI.enabled = _enabled;
            }

            public void RestoreLabelWidth() => EditorGUIUtility.labelWidth = _labelWidth;
            public void RestoreColors()
            {
                GUI.contentColor = _fgColor;
                GUI.backgroundColor = _bgColor;
                GUI.color = _color;
            }
            public void RestoreMatrix() => GUI.matrix = _matrix;
            public void RestoreEnabledState() => GUI.enabled = _enabled;
        }

        public struct LabelWidthState : IDisposable
        {
            private float _value;

            internal LabelWidthState(float newValue)
            {
                _value = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = newValue;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = _value;
            }
        }

        public struct PipeContent
        {
            public GUIContent Content => _tempContent;

            private PipeContent(string text)
            {
                _tempContent.text = text;
                _tempContent.image = null;
                _tempContent.tooltip = null;
            }

            private PipeContent(Texture image)
            {
                _tempContent.text = null;
                _tempContent.image = image;
                _tempContent.tooltip = null;
            }

            public static implicit operator GUIContent(PipeContent pipeContent) => pipeContent.Content;
            public static implicit operator PipeContent(string text) => new PipeContent(text);
            public static implicit operator PipeContent(Texture icon) => new PipeContent(icon);
        }

        public static State PushState() => new State(false);
        public static LabelWidthState LabelWidth(float width) => new LabelWidthState(width);

        public static GUIContent Content(string text, string tooltip = null)
        {
            _tempContent.text = text;
            _tempContent.image = null;
            _tempContent.tooltip = tooltip;

            return _tempContent;
        }

        public static GUIContent Content(Texture image, string tooltip = null)
        {
            _tempContent.text = null;
            _tempContent.image = image;
            _tempContent.tooltip = tooltip;

            return _tempContent;
        }

        public static GUIContent Content(string text, Texture image, string tooltip = null)
        {
            _tempContent.text = text;
            _tempContent.image = image;
            _tempContent.tooltip = tooltip;

            return _tempContent;
        }
    }
}
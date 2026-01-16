using System;
using UnityEditor;
using UnityEngine;
using Postica.Common;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal partial class PropertyData
        {
            public Action<Rect, bool> previewDraw;
            public Func<float> getPreviewHeight;
            public Rect lastPreviewRect;
            public int previewIndentLevel;

            public float changeEventHeight;
        }

        partial void UpdatePreviewIMGUI(PropertyData data)
        {
            var source = data.properties.target.objectReferenceValue;
            data.previewDraw = null;
            data.getPreviewHeight = null;
            data.canPathPreview = false;
            data.pathPreviewIsEditing = false;

            if (!source)
            {
                return;
            }
            
            var path = data.properties.path.stringValue;
            // Prepare path, convert / to . and if the path ends with ] then remove the last part
            path = path.Replace('/', '.');
            if (path.EndsWith("]"))
            {
                var lastDot = path.LastIndexOf('.');
                if (lastDot != -1)
                {
                    path = path.Substring(0, lastDot);
                }
            }

            if(OnTryPrepareDataPreview != null)
            {
                try
                {
                    if(OnTryPrepareDataPreview(data, source, path))
                    {
                        return;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            var serObject = new SerializedObject(source);
            if(!serObject.TryFindLastProperty(path, out var prop))
            {
                serObject.Dispose();
                return;
            }
            
            prop.isExpanded = true;
            data.previewDraw = (rect, save) =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, prop, true);
                if (EditorGUI.EndChangeCheck() && save)
                {
                    serObject.ApplyModifiedProperties();
                }
            };
            data.getPreviewHeight = () =>
            {
                return EditorGUI.GetPropertyHeight(prop, true);
            };

            data.canPathPreview = true;
        }
    }
}
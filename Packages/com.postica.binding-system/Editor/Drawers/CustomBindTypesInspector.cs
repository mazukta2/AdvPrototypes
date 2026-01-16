using Postica.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    [CustomEditor(typeof(CustomBindTypesAsset))]
    class CustomBindTypesInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var converters = serializedObject.FindProperty(nameof(CustomBindTypesAsset.customConverters));
            var modifiers = serializedObject.FindProperty(nameof(CustomBindTypesAsset.customModifiers));
            var accessorProviders = serializedObject.FindProperty(nameof(CustomBindTypesAsset.customAccessorProviders));
            var container = new VisualElement().WithChildren(
                new Label("This file" + " contains all user defined converters, modifiers and accessor providers in the project.\n" +
                          "It contains as well some hidden metadata, mostly required for type registration process.".RT().Color(BindColors.Primary) +
                          "\n\nDo not delete".RT().Color(BindColors.Error) + " this file as it will be recreated and reset all custom, user defined types to be registered by default into the system.")
                {
                    enableRichText = true,
                }.WithStyle(s =>
                {
                    s.paddingBottom = 8;
                    s.paddingTop = 8;
                    s.paddingLeft = 8;
                    s.paddingRight = 8;
                    s.fontSize = 12;
                    s.backgroundColor = Color.black.WithAlpha(0.25f);
                    s.whiteSpace = WhiteSpace.Normal;
                    s.borderBottomLeftRadius = 8;
                    s.borderBottomRightRadius = 8;
                    s.borderTopLeftRadius = 8;
                    s.borderTopRightRadius = 8;
                    s.marginBottom = 12;
                    s.marginTop = 12;
                    // s.unityTextAlign = TextAnchor.MiddleCenter;
                }),
                new PropertyField().EnsureBind(converters),
                new PropertyField().EnsureBind(modifiers),
                new PropertyField().EnsureBind(accessorProviders)
            );
            
            return container;
        }
    }
}
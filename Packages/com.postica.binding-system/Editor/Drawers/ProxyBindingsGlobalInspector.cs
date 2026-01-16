using Postica.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    [CustomEditor(typeof(ProxyBindingsAsset))]
    class ProxyBindingsGlobalInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var proxies = serializedObject.FindProperty("bindings");
            var container = new VisualElement().WithChildren(
                new Label("This file" + " contains all bindings linked to assets in the project.\n" +
                          "Prefabs and scenes have their own, embedded bindings.".RT().Color(BindColors.Primary) +
                          "\n\nDo not delete".RT().Color(BindColors.Error) + " this file as it will remove all bindings from the project assets.")
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
                }).SetVisibility(target.name == "global-bindings"),
                new PropertyField().EnsureBind(proxies)
            );
            return container;
        }
    }
}
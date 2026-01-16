using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    // [CustomEditor(typeof(ProxyBindings))]
    class ProxyBindingsInspector : Editor
    {
        protected override void OnHeaderGUI()
        {
            //base.OnHeaderGUI();
        }

        public override VisualElement CreateInspectorGUI()
        {
            target.hideFlags = HideFlags.None;
            var emptyView = new VisualElement();
            // emptyView.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            return emptyView;
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var root = evt.destinationPanel.visualTree;
            var proxies = serializedObject.FindProperty("bindings");
            for (int i = 0; i < proxies.arraySize; i++)
            {
                var proxy = proxies.GetArrayElementAtIndex(i);
                var proxyView = new BindProxyDrawer.BindProxyView(root, proxy, null, false);
                proxyView.AttachToRoot(root);
            }
            
        }
    }
}
using Postica.Common;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    [CustomEditor(typeof(FieldRoutes))]
    class FieldRoutesInspector : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var routes = serializedObject.FindProperty("_routes");
            var container = new VisualElement().WithChildren(
                new Label("This file" + " contains all fields' rerouting in the whole project.\n" +
                          "\n\nDo not delete".RT().Color(BindColors.Error) + " this file as it will remove all field routes from the project.")
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
                new PropertyField().EnsureBind(routes)
            );
            return container;
        }
    }
}
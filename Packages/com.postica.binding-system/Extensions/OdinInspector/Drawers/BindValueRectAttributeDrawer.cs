using UnityEngine;
using Sirenix.OdinInspector.Editor;

namespace Postica.BindingSystem.Odin
{
    class BindValueRectAttributeDrawer : OdinAttributeDrawer<BindValueRectAttribute>
    {
        private Rect _rect;
        public Rect Rect => _rect;

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var rect = GUILayoutUtility.GetRect(0, 0);
            CallNextDrawer(label);
            var lastRect = Property.LastDrawnValueRect;
            if(Event.current.type == EventType.Repaint)
            {
                _rect = new Rect(lastRect.x + 3, rect.y + 1, lastRect.width, lastRect.yMax - rect.y);
            }
        }
    }
}
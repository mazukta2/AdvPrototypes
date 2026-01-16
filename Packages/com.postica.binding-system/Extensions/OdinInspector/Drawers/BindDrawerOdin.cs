using UnityEngine;
using System;
using System.Collections;
using Sirenix.OdinInspector.Editor;

namespace Postica.BindingSystem.Odin
{

    class BindDrawerOdin<T> : OdinValueDrawer<T>, IExposedOdinDrawer where T : IBind
    {
        private BindDrawer _drawer;

        public override bool CanDrawTypeFilter(Type type)
        {
            return !type.IsArray && !typeof(IEnumerable).IsAssignableFrom(type) && base.CanDrawTypeFilter(type);
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            _drawer = new BindDrawer(this, Property);
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            _drawer ??= new BindDrawer(this, Property);
            _drawer.DrawPropertyLayout(label);
        }
        
        bool IExposedOdinDrawer.CallNextDrawer(GUIContent label) => CallNextDrawer(label);
    }
}
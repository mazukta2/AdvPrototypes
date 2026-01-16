using Postica.BindingSystem.Accessors;
using Postica.BindingSystem.Reflection;
using Postica.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        private class EnhancedDropdownItem : DropdownItem
        {
            private readonly bool _usesConverter;
            private readonly bool _unsafeConverter;

            public EnhancedDropdownItem(string name,
                                        string type,
                                        Action onSelectCallback,
                                        Texture2D icon,
                                        bool hasConverter,
                                        bool isSafe) 
                : base(name, type, onSelectCallback, icon, null, false)
            {
                _usesConverter = hasConverter;
                _unsafeConverter = hasConverter && !isSafe;
            }

            public override VisualElement GetDrawer(Action closeWindow, bool darkMode)
            {
                var ve = base.GetDrawer(closeWindow, darkMode);
                if (!_usesConverter)
                {
                    return ve;
                }

                var image = new VisualElement();
                image.style.backgroundImage = new StyleBackground(_unsafeConverter ? Icons.ConvertUnsafeIcon : Icons.ConvertIcon);
                image.style.width = 14;
                image.style.height = 14;
                image.style.alignSelf = Align.Center;
                image.style.flexShrink = 0;
                image.style.marginLeft = 4;
                image.tooltip = _unsafeConverter ? "Uses a Value Conversion that may fail" : "Uses Value Conversion";

                if (_unsafeConverter)
                {
                    image.style.unityBackgroundImageTintColor = _unsafeColor.WithAlpha(0.5f);
                }
                else
                {
                    image.style.unityBackgroundImageTintColor = EditorGUIUtility.isProSkin 
                                                              ? Color.white.WithAlpha(0.5f)
                                                              : Color.black.WithAlpha(0.5f);
                }

                var typeLabel = ve.Q<Label>(className: "sd-label-description");
                if (typeLabel == null)
                {
                    return ve;
                }

                typeLabel.parent.Insert(typeLabel.parent.IndexOf(typeLabel), image);

                return ve;
            }
        }
    }
}
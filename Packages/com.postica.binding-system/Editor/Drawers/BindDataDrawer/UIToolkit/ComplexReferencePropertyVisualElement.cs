using System;
using Postica.Common;
using UnityEditor;
using UnityEditor.UIElements;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal partial class ComplexReferenceProperty
        {
            private partial void InitializeVisualElements(SerializedProperty property)
            {
                // For each property create its property field
                for (int i = 0; i < Length; i++)
                {
                    properties[i].view = new PropertyField().EnsureBind(properties[i].property);
                    properties[i].view.RegisterValueChangeCallback(evt => onChanged?.Invoke());
                }

            }

            public void Refresh()
            {
                for (int i = 0; i < Length; i++)
                {
                    ref var view = ref properties[i].view;
                    ref var property = ref properties[i].property;

                    if(view == null)
                    {
                        continue;
                    }

                    if(property == null)
                    {
                        continue;
                    }

                    try
                    {
                        if (view.childCount == 0 || property.propertyPath != view.bindingPath)
                        {
                            //view.Clear();
                            //view.binding = null;
                            view.BindProperty(property);
                        }
                    }
                    catch (ObjectDisposedException) when (property.IsAlive())
                    {
                        view.BindProperty(property);
                    }
                }
            }
        }
    }
}
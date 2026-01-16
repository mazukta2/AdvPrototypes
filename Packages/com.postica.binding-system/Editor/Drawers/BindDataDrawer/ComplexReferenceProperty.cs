using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        internal partial class ComplexReferenceProperty
        {
            public readonly SerializedProperty mainProperty;
            public readonly ReferenceProperty[] properties;

            public int Length => properties?.Length ?? 0;

            public Action onChanged;

            public ComplexReferenceProperty(SerializedProperty property, string ignoreList = null, bool isUIToolkit = false)
            {
                mainProperty = property.Copy();
                properties = BuildInnerProperties(property, ignoreList);
                if (isUIToolkit)
                {
                    InitializeVisualElements(property);
                }
                else
                {
                    InitializeIMGUI(property);
                }
            }

            private partial void InitializeIMGUI(SerializedProperty property);
            private partial void InitializeVisualElements(SerializedProperty property);

            private static ReferenceProperty[] BuildInnerProperties(SerializedProperty property, string ignoreList)
            {
                var nextProperty = property.Copy();
                var iterator = property.Copy();
                nextProperty.NextVisible(false);
                iterator.Next(true);

                if (!iterator.propertyPath.Contains(property.propertyPath))
                {
                    return Array.Empty<ReferenceProperty>();
                }

                if (ignoreList?.Contains(iterator.name + ";") == true)
                {
                    iterator.NextVisible(false);
                }

                if (iterator.propertyPath == nextProperty.propertyPath)
                {
                    return Array.Empty<ReferenceProperty>();
                }

                List<ReferenceProperty> properties = new List<ReferenceProperty>
                {
                    new ReferenceProperty() 
                    {
                        name = iterator.name,
                        property = iterator.Copy(),
                    }
                };
                while (iterator.NextVisible(false) && iterator.propertyPath != nextProperty.propertyPath)
                {
                    properties.Add(new ReferenceProperty()
                    {
                        name = iterator.name,
                        property = iterator.Copy(),
                    });
                }
                return properties.ToArray();
            }
        }

        internal partial struct ReferenceProperty
        {
            public string name;
            public SerializedProperty property;
            public PropertyField view;
            public float height;

            internal PropertyField GetView()
            {
                view ??= new PropertyField(property);
                return view;
            }
        }
    }
}
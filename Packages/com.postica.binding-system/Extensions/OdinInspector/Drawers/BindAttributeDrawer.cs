using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;

using Object = UnityEngine.Object;
using UnityEditor.UIElements;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Linq;
using Sirenix.OdinInspector.Editor.Drawers;

namespace Postica.BindingSystem.Odin
{
    class BindAttributeDrawer : OdinAttributeDrawer<BindAttribute>, IExposedOdinDrawer
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

            // Get the value property
            var valueProperty = Property.Children["_value"];
            if (valueProperty == null)
            {
                // Most probably it is a bind without a value
                return;
            }

            var transferedAttributes = new HashSet<Type>();

            var activeDrawerChain = Property.GetActiveDrawerChain();

            valueProperty.CleanForCachedReuse();

            // Transfer the attributes to the value property using reflection
            var attributesField = typeof(InspectorProperty).GetField("processedAttributes", BindingFlags.NonPublic | BindingFlags.Instance);
            var immutableAttributesField = typeof(InspectorProperty).GetField("processedAttributesImmutable", BindingFlags.NonPublic | BindingFlags.Instance);
            
            var valueAttributes = attributesField.GetValue(valueProperty) as List<Attribute>;
            // Nullify immutable attributes for the value property
            immutableAttributesField.SetValue(valueProperty, null);

            var thisNamespace = typeof(BindAttribute).Namespace;
            var canCopyAttribute = false;

            foreach (var attribute in Property.Attributes)
            {
                if(attribute is BindAttribute)
                {
                    canCopyAttribute = true;
                    continue;
                }

                // Avoid our attributes
                if (attribute.GetType().Namespace?.StartsWith(thisNamespace) == true)
                { 
                    continue; 
                }

                if (!canCopyAttribute)
                {
                    continue;
                }

                // Avoid duplicates
                if (valueAttributes.Any(a => a.GetType() == attribute.GetType()))
                {
                    continue;
                }
                
                transferedAttributes.Add(attribute.GetType());
                valueAttributes.Add(attribute);
            }

            valueAttributes.Add(new BindValueRectAttribute());

            // Do the same to InspectorPropertyInfo
            attributesField = typeof(InspectorPropertyInfo).GetField("attributes", BindingFlags.NonPublic | BindingFlags.Instance);
            immutableAttributesField = typeof(InspectorPropertyInfo).GetField("attributesImmutable", BindingFlags.NonPublic | BindingFlags.Instance);

            valueAttributes = attributesField.GetValue(valueProperty.Info) as List<Attribute>;
            // Nullify immutable attributes for the value property
            immutableAttributesField.SetValue(valueProperty.Info, null);

            canCopyAttribute = false;

            foreach (var attribute in Property.Info.Attributes)
            {
                if(attribute is BindAttribute)
                {
                    canCopyAttribute = true;
                    continue;
                }

                // Avoid our attributes
                if (attribute.GetType().Namespace?.StartsWith(thisNamespace) == true)
                {
                    continue;
                }

                if (!canCopyAttribute)
                {
                    continue;
                }

                // Avoid duplicates
                if (valueAttributes.Any(a => a.GetType() == attribute.GetType()))
                {
                    continue;
                }

                transferedAttributes.Add(attribute.GetType());
                valueAttributes.Add(attribute);
            }

            valueAttributes.Add(new BindValueRectAttribute());

            var canDisableDrawer = false;

            // Disable each drawer in the chain, but only if drawer is for attributes
            foreach (var drawer in activeDrawerChain.BakedDrawerArray)
            {
                if (drawer == this)
                {
                    canDisableDrawer = true;
                    continue;
                }

                if (TryGetInvalidAttribute(drawer.GetType(), out var attributeType) 
                    && transferedAttributes.Contains(attributeType))
                {
                    drawer.SkipWhenDrawing = true;
                    continue;
                }

                if (!canDisableDrawer)
                {
                    continue;
                }

                if (OdinExtensions.IsUsingUIToolkit && drawer is HeaderAttributeDrawer)
                {
                    drawer.SkipWhenDrawing = true;
                }
                else 
                if (IsOdinInvalidAttributeDrawer(drawer.GetType()))
                {
                    drawer.SkipWhenDrawing = true;
                }
                else if (IsOdinAttributeDrawer(drawer.GetType()))
                {
                    drawer.SkipWhenDrawing = true;
                }
            }

            // Get the private field initialized for each drawer in the chain using reflection and reset it
            var initializedField = typeof(OdinDrawer).GetField("initialized", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach(var drawer in valueProperty.GetActiveDrawerChain().BakedDrawerArray)
            {
                initializedField.SetValue(drawer, false);
                drawer.Initialize(valueProperty);
            }
        }

        // Utility method to get if the drawer is subtype of generic OdinAttributeDrawer
        private static bool IsOdinAttributeDrawer(Type type)
        {
            return type.IsSubclassOf(typeof(OdinAttributeDrawer<>)) || type.IsSubclassOf(typeof(OdinAttributeDrawer<,>));
        }

        private static bool IsOdinValueOrGroupDrawer(Type type)
        {
            return type.IsSubclassOf(typeof(OdinValueDrawer<>)) || type.IsSubclassOf(typeof(OdinGroupDrawer<>));
        }

        private static bool IsOdinInvalidAttributeDrawer(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(InvalidAttributeNotificationDrawer<>);
        }

        private static bool TryGetInvalidAttribute(Type type, out Type attributeType)
        {
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(InvalidAttributeNotificationDrawer<>))
            {
                attributeType = type.GetGenericArguments()[0];
                return true;
            }
            attributeType = null;
            return false;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            _drawer?.DrawPropertyLayout(label);
        }

        bool IExposedOdinDrawer.CallNextDrawer(GUIContent label) => CallNextDrawer(label);
    }
}
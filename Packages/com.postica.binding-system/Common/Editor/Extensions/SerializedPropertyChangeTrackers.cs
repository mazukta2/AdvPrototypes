using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.Common
{
    public interface ISerializedPropertyChangeTracker
    {
        string PropertyPath { get; }
        bool HasChanged();
        void Update();
        void ApplyTo(SerializedProperty property);
    }

    public static class SerializedPropertyChangeTrackerExtensions
    {
        public static void UpdateAll(this IEnumerable<ISerializedPropertyChangeTracker> trackers)
        {
            HashSet<SerializedObject> updatedSerializedObjects = new HashSet<SerializedObject>();
            foreach (var tracker in trackers)
            {
                if (tracker is PropertyChangeTracker standardTracker
                    && updatedSerializedObjects.Add(standardTracker.property.serializedObject))
                {
                    standardTracker.property.serializedObject.Update();
                }

                tracker.Update();
            }
        }

        public static ISerializedPropertyChangeTracker CreateChangeTracker(this SerializedProperty property)
        {
            if (property.isArray)
            {
                return new ArrayPropertyChangeTracker(property);
            }

            return property.propertyType switch
            {
                SerializedPropertyType.Boolean => new BoolPropertyChangeTracker(property),
                SerializedPropertyType.Float => new FloatPropertyChangeTracker(property),
                SerializedPropertyType.ArraySize => new IntPropertyChangeTracker(property),
                SerializedPropertyType.Integer => new IntPropertyChangeTracker(property),
                SerializedPropertyType.String => new StringPropertyChangeTracker(property),
                SerializedPropertyType.Enum => new EnumPropertyChangeTracker(property),
                SerializedPropertyType.Vector2 => new Vector2PropertyChangeTracker(property),
                SerializedPropertyType.Vector3 => new Vector3PropertyChangeTracker(property),
                SerializedPropertyType.Vector4 => new Vector4PropertyChangeTracker(property),
                SerializedPropertyType.Color => new ColorPropertyChangeTracker(property),
                SerializedPropertyType.Bounds => new BoundsPropertyChangeTracker(property),
                SerializedPropertyType.Rect => new RectPropertyChangeTracker(property),
                SerializedPropertyType.Quaternion => new QuaternionPropertyChangeTracker(property),
                SerializedPropertyType.AnimationCurve => new AnimationCurvePropertyChangeTracker(property),
#if UNITY_2022_3_OR_NEWER
                SerializedPropertyType.Gradient => new GradientPropertyChangeTracker(property),
#endif
                SerializedPropertyType.ObjectReference => new ObjectReferencePropertyChangeTracker(property),
                SerializedPropertyType.ExposedReference => new ExposedReferencePropertyChangeTracker(property),
                SerializedPropertyType.LayerMask => new LayerMaskPropertyChangeTracker(property),
                SerializedPropertyType.ManagedReference => new ManagedReferencePropertyChangeTracker(property),
                SerializedPropertyType.Generic => new GenericPropertyChangeTracker(property),
#if UNITY_2022_3_OR_NEWER
                _ => new FallbackPropertyChangeTracker(property),
#endif
            };
        }

        private abstract class PropertyChangeTracker : ISerializedPropertyChangeTracker, IDisposable
        {
            public SerializedProperty property;
            public string PropertyPath { get; set; }

            public PropertyChangeTracker(SerializedProperty property)
            {
                PropertyPath = property.propertyPath;
                this.property = property.Copy();
                Update();
            }

            public abstract void ApplyTo(SerializedProperty property);
            public abstract bool HasChanged();
            public abstract void Update();

            public virtual void Dispose()
            {
                property?.Dispose();
                property = null;
            }
        }

#if UNITY_2022_3_OR_NEWER
        private sealed class FallbackPropertyChangeTracker : PropertyChangeTracker
        {
            private object _value;

            public FallbackPropertyChangeTracker(SerializedProperty prop) : base(prop) { }

            public override bool HasChanged()
            {
                return Equals(_value, property.boxedValue);
            }

            public override void Update()
            {
                _value = property.boxedValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.boxedValue = _value;
            }
        }
#endif

        private sealed class ArrayPropertyChangeTracker : PropertyChangeTracker
        {
            private List<ISerializedPropertyChangeTracker> _array = new();

            public ArrayPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _array.Count != property.arraySize
                       || _array.Any(e => e.HasChanged());
            }

            public override void Update()
            {
                if (_array.Count == property.arraySize)
                {
                    for (int i = 0; i < _array.Count; i++)
                    {
                        _array[i].Update();
                    }

                    return;
                }

                // Dispose all the elements first
                foreach (var element in _array)
                {
                    if (element is IDisposable disposableElement)
                    {
                        disposableElement.Dispose();
                    }
                }

                _array.Clear();
                for (int i = 0; i < property.arraySize; i++)
                {
                    var element = property.GetArrayElementAtIndex(i);
                    _array.Add(element.CreateChangeTracker());
                }
            }

            public override void ApplyTo(SerializedProperty property)
            {
                if (property.arraySize != _array.Count)
                {
                    property.arraySize = _array.Count;
                }

                for (int i = 0; i < _array.Count; i++)
                {
                    var element = property.GetArrayElementAtIndex(i);
                    _array[i].ApplyTo(element);
                }
            }
        }

        private sealed class GenericPropertyChangeTracker : PropertyChangeTracker
        {
            private List<ISerializedPropertyChangeTracker> children = new();

            public GenericPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
                var iterator = prop.Copy();
                if (iterator.Next(true))
                {
                    do
                    {
                        children.Add(iterator.CreateChangeTracker());
                    } while (iterator.Next(iterator.propertyType == SerializedPropertyType.Generic));
                }
            }

            public override bool HasChanged()
            {
                return children.Any(c => c.HasChanged());
            }

            public override void Update()
            {
                foreach (var child in children)
                {
                    child.Update();
                }
            }

            public override void ApplyTo(SerializedProperty property)
            {
                foreach (var child in children)
                {
                    child.ApplyTo(property);
                }
            }
        }

        // For each property type, create a class which handles the type of that property and extends PropertyChangeTracker
        private sealed class IntPropertyChangeTracker : PropertyChangeTracker
        {
            private int _value;

            public IntPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.intValue;
            }

            public override void Update()
            {
                _value = property.intValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.intValue = _value;
            }
        }

        private class FloatPropertyChangeTracker : PropertyChangeTracker
        {
            private float _value;

            public FloatPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return Mathf.Abs(_value - property.floatValue) > 0.000001f;
            }

            public override void Update()
            {
                _value = property.floatValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.floatValue = _value;
            }
        }

        private class StringPropertyChangeTracker : PropertyChangeTracker
        {
            private string _value;

            public StringPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.stringValue;
            }

            public override void Update()
            {
                _value = property.stringValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.stringValue = _value;
            }
        }

        private class DoublePropertyChangeTracker : PropertyChangeTracker
        {
            private double _value;

            public DoublePropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return Math.Abs(_value - property.floatValue) > 0.00000001;
            }

            public override void Update()
            {
                _value = property.doubleValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.doubleValue = _value;
            }
        }

        private class BoolPropertyChangeTracker : PropertyChangeTracker
        {
            private bool _value;

            public BoolPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.boolValue;
            }

            public override void Update()
            {
                _value = property.boolValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.boolValue = _value;
            }
        }

        private class EnumPropertyChangeTracker : PropertyChangeTracker
        {
            private int _value;

            public EnumPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.enumValueIndex;
            }

            public override void Update()
            {
                _value = property.enumValueIndex;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.enumValueIndex = _value;
            }
        }

        private class Vector2PropertyChangeTracker : PropertyChangeTracker
        {
            private Vector2 _value;

            public Vector2PropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.vector2Value;
            }

            public override void Update()
            {
                _value = property.vector2Value;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.vector2Value = _value;
            }
        }

        private class Vector3PropertyChangeTracker : PropertyChangeTracker
        {
            private Vector3 _value;

            public Vector3PropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.vector3Value;
            }

            public override void Update()
            {
                _value = property.vector3Value;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.vector3Value = _value;
            }
        }

        private class Vector4PropertyChangeTracker : PropertyChangeTracker
        {
            private Vector4 _value;

            public Vector4PropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.vector4Value;
            }

            public override void Update()
            {
                _value = property.vector4Value;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.vector4Value = _value;
            }
        }

        private class ColorPropertyChangeTracker : PropertyChangeTracker
        {
            private Color _value;

            public ColorPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.colorValue;
            }

            public override void Update()
            {
                _value = property.colorValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.colorValue = _value;
            }
        }

        private class BoundsPropertyChangeTracker : PropertyChangeTracker
        {
            private Bounds _value;

            public BoundsPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.boundsValue;
            }

            public override void Update()
            {
                _value = property.boundsValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.boundsValue = _value;
            }
        }

        private class RectPropertyChangeTracker : PropertyChangeTracker
        {
            private Rect _value;

            public RectPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.rectValue;
            }

            public override void Update()
            {
                _value = property.rectValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.rectValue = _value;
            }
        }

        private class QuaternionPropertyChangeTracker : PropertyChangeTracker
        {
            private Quaternion _value;

            public QuaternionPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.quaternionValue;
            }

            public override void Update()
            {
                _value = property.quaternionValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.quaternionValue = _value;
            }
        }

        private class AnimationCurvePropertyChangeTracker : PropertyChangeTracker
        {
            private AnimationCurve _value;

            public AnimationCurvePropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value.Equals(property.animationCurveValue);
            }

            public override void Update()
            {
                _value = property.animationCurveValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.animationCurveValue = _value;
            }
        }

#if UNITY_2022_3_OR_NEWER
        private class GradientPropertyChangeTracker : PropertyChangeTracker
        {
            private Gradient _value;

            public GradientPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value.Equals(property.gradientValue);
            }

            public override void Update()
            {
                _value = property.gradientValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.gradientValue = _value;
            }
        }
#endif

        private class ObjectReferencePropertyChangeTracker : PropertyChangeTracker
        {
            private Object _value;

            public ObjectReferencePropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return property.objectReferenceValue == _value;
            }

            public override void Update()
            {
                _value = property.objectReferenceValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.objectReferenceValue = _value;
            }
        }

        private class ExposedReferencePropertyChangeTracker : PropertyChangeTracker
        {
            private Object _value;

            public ExposedReferencePropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return property.exposedReferenceValue == _value;
            }

            public override void Update()
            {
                _value = property.exposedReferenceValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.exposedReferenceValue = _value;
            }
        }

        private class LayerMaskPropertyChangeTracker : PropertyChangeTracker
        {
            private LayerMask _value;

            public LayerMaskPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value.value != property.intValue;
            }

            public override void Update()
            {
                _value = property.intValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.intValue = _value.value;
            }
        }

        private class EnumFlagsPropertyChangeTracker : PropertyChangeTracker
        {
            private int _value;

            public EnumFlagsPropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value != property.intValue;
            }

            public override void Update()
            {
                _value = property.intValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.intValue = _value;
            }
        }

        private class ManagedReferencePropertyChangeTracker : PropertyChangeTracker
        {
            private object _value;

            public ManagedReferencePropertyChangeTracker(SerializedProperty prop) : base(prop)
            {
            }

            public override bool HasChanged()
            {
                return _value == property.managedReferenceValue;
            }

            public override void Update()
            {
                _value = property.managedReferenceValue;
            }

            public override void ApplyTo(SerializedProperty property)
            {
                property.managedReferenceValue = _value;
            }
        }
    }
}
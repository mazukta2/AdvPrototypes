using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Postica.BindingSystem.Accessors;
using Postica.Common;
using Postica.Common.Reflection;
#if ENABLE_PERFORMANCE_TESTS
using Unity.PerformanceTesting;
#endif
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem {

    public class AccessorsFactoryTests
    {
        private const int k_PerfIterations = 100;
        private const int k_PerfMeasurements = 30;
        
        private DummyObject _dummy;
        private DerivedDummyObject _derivedDummy;

        private class ValueProvider<T> : IValueProvider<T>
        {
            public T Value { get; set; }
            public object UnsafeValue => Value;

            public static implicit operator ValueProvider<T>(T value) => new ValueProvider<T>() { Value = value };
        }

        [Flags]
        public enum WhatToTest
        {
            ShortFields = 1 << 0,
            LongFields = 1 << 1,
            ShortProperties = 1 << 2,
            Compound = 1 << 3,
            Everything = ShortFields | LongFields | ShortProperties | Compound,
        }

        [SetUp]
        public void CreateDummy()
        {
            _dummy = new GameObject("Dummy_For_Tests").AddComponent<DummyObject>();
            _derivedDummy = new GameObject("DerivedDummy_For_Tests").AddComponent<DerivedDummyObject>();
        }

        [TearDown]
        public void DestroyDummy()
        {
            if (_dummy)
            {
                Object.Destroy(_dummy.gameObject);
            }
            if (_derivedDummy)
            {
                Object.Destroy(_derivedDummy.gameObject);
            }
            AccessorsFactory.Reset(hardReset: false);
        }

        private static string BuildPath(MemberInfo memberInfo)
        {
            switch (memberInfo)
            {
                case PropertyInfo info:
                    return info.GetIndexParameters()?.Length > 0 ?
                        $"{info.Name}[{string.Join(", ", info.GetIndexParameters().Select(p => p.ParameterType.Name))}]" :
                        info.Name;
                    case FieldInfo info: return info.Name;
                case MethodInfo info:
                    return $"{info.Name}({string.Join(", ", info.GetParameters().Select(p => p.ParameterType.Name))})";
            }
            return null;
        }

        [Test]
        public void Get_Accessor_For_Vector3_Passes()
        {
            var accessor = AccessorsFactory.GetAccessor(_dummy, "Vector3.x");
            Assert.IsNotNull(accessor);
        }

        [Test]
        public void Get_Accessor_For_NonExisting_Property_Fails()
        {
            Assert.Throws<ArgumentException>(() => AccessorsFactory.GetAccessor(_dummy, "Vector3.random"));
        }

        [Test]
        public void Set_Vecto3_As_String_Fails()
        {
            var accessor = AccessorsFactory.GetAccessor(_dummy, "Vector3");
            Assert.Throws<InvalidCastException>(() => accessor.SetValue(_dummy, "Hello"));
        }

        [Test]
        public void Set_Vecto3_As_Int_Fails()
        {
            var accessor = AccessorsFactory.GetAccessor(_dummy, "Vector3");
            Assert.Throws<InvalidCastException>(() => accessor.SetValue(_dummy, 3));
        }

        [Test]
        public void Set_Float_As_FloatString_Passes()
        {
            var floatValue = 3.5f;
            var accessor = AccessorsFactory.GetAccessor(_dummy, "publicFloat");
            var floatString = floatValue.ToString();
            accessor.SetValue(_dummy, floatString);

            Assert.AreEqual(floatValue, _dummy.publicFloat);
        }

        [Test]
        public void GetSimplePath_Vector3_Passes()
        {
            var vec3 = new Vector3(3, 0, 0);
            _dummy.Vector3 = vec3;
            var accessor = AccessorsFactory.GetAccessor(_dummy, "Vector3");

            Assert.AreEqual(vec3, accessor.GetValue(_dummy));
        }

        [Test]
        public void GetLongerPath_X_From_Vector3_Passes()
        {
            _dummy.Vector3 = new Vector3(3, 0, 0);
            var accessor = AccessorsFactory.GetAccessor(_dummy, "Vector3.x");

            Assert.AreEqual(3, accessor.GetValue(_dummy));
        }
        
        [Test]
        public void GetLongerPath_X_From_PublicVector3_Passes()
        {
            _dummy.publicVector = new Vector3(3, 0, 0);
            var accessor = AccessorsFactory.GetAccessor(_dummy, "publicVector.x");

            Assert.AreEqual(3, accessor.GetValue(_dummy));
        }

        [Test]
        public void SetSimplePath_Vector3_Passes()
        {
            var vec3 = new Vector3(3, 0, 0);
            var accessor = AccessorsFactory.GetAccessor(_dummy, "Vector3");
            accessor.SetValue(_dummy, vec3);
            Assert.AreEqual(vec3, _dummy.Vector3);
        }

        [Test]
        public void SetSimplePath_SpecializedVector3_Passes()
        {
            var vec3 = new Vector3(3, 0, 0);
            var accessor = AccessorsFactory.GetAccessor<Vector3>(_dummy, "Vector3");
            accessor.SetValue(_dummy, vec3);
            Assert.AreEqual(vec3, _dummy.Vector3);
        }

        [Test]
        public void SetSimplePath_Float_From_Int_Passes()
        {
            var accessor = AccessorsFactory.GetAccessor(_dummy, "publicFloat");
            accessor.SetValue(_dummy, 3);
            Assert.AreEqual(3, _dummy.publicFloat);
        }

        [Test]
        public void SetLongerPath_X_From_Vector3_Passes()
        {
            var accessor = AccessorsFactory.GetAccessor(_dummy, "Vector3.x");
            accessor.SetValue(_dummy, 3f);
            Assert.AreEqual(3, _dummy.Vector3.x);
        }

        [Test]
        public void SetLongerPath_Vec2_From_SubStruct_Passes()
        {
            var vec2 = new Vector2(2, 3);
            var accessor = AccessorsFactory.GetAccessor(_dummy, "SubStruct.vec2");
            accessor.SetValue(_dummy, vec2);
            Assert.AreEqual(vec2, _dummy.SubStruct.vec2);
        }

        [Test]
        public void SetDerived_FloatValue_Passes()
        {
            var accessor = AccessorsFactory.GetAccessor(_derivedDummy, "FloatProperty");
            accessor.SetValue(_derivedDummy, 3f);
            Assert.AreEqual(3f + 2f, _derivedDummy.FloatProperty);
        }

        [Test]
        public void SetTwiceLongerPath_Vec2_X_From_SubClass_Passes()
        {
            // Arrange
            var expectedX = 42;
            var expectedY = 69;
            var newTransform = new GameObject("Test").transform;
            newTransform.position = new Vector3(0, expectedY, 43);
            var subClass = new DummyObject.InnerClass() { transform = newTransform, vec2 = new Vector2(0, 69) };

            // Act
            _dummy.SubClass = new DummyObject.InnerClass() { transform = _dummy.transform };
            var accessor1 = AccessorsFactory.GetAccessor(_dummy, "SubClass.transform.position.x");
            var accessor2 = AccessorsFactory.GetAccessor(_dummy, "SubClass");

            accessor1.SetValue(_dummy, 3);
            accessor2.SetValue(_dummy, subClass);
            accessor1.SetValue(_dummy, expectedX);

            // Assert
            Assert.AreEqual(expectedX, _dummy.SubClass.transform.position.x);
            Assert.AreEqual(expectedY, _dummy.SubClass.transform.position.y);
            
            // Cleanup
            Object.Destroy(newTransform.gameObject);
        }

        [Test]
        public void MaterialProperty_SetColor_Passes()
        {
            // Arrange
            var expectedColor = Color.red;
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer = cube.GetComponent<Renderer>();
            renderer.material.SetColor("_Color", Color.white);
            
            // Act
            var accessor = AccessorsFactory.GetAccessor<Color>(renderer, "material._Color-color");

            accessor.SetValue(renderer, Color.red);

            // Assert
            Assert.AreEqual(expectedColor, renderer.material.GetColor("_Color"));
            
            // Cleanup
            Object.Destroy(cube);
        }
        
        [Test]
        public void MaterialProperty_GetColor_Passes()
        {
            // Arrange
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer = cube.GetComponent<Renderer>();
            renderer.material.SetColor("_Color", Color.red);
            
            // Act
            var accessor = AccessorsFactory.GetAccessor<Color>(renderer, "material._Color-color");

            var color = accessor.GetValue(renderer);

            // Assert
            Assert.AreEqual(renderer.material.GetColor("_Color"), color);
            
            // Cleanup
            Object.Destroy(cube);
        }

        [Test]
        public void SetIndexerPath_Passes()
        {
            // Arrange
            var addition = 4;
            var multiplier = 3;
            var startValue = 2;
            var expectedSetter = startValue * multiplier + addition;
            var expectedGetter = (expectedSetter + addition) * multiplier;
            var indexerPath = BuildPath(typeof(DummyObject).GetProperty("Item"));

            // Act
            _dummy.IndexerAmount = startValue;

            var accessor = AccessorsFactory.GetAccessor<int>(_dummy,
                                                             indexerPath, null, null,
                                                             new ValueProvider<int>[] { addition, multiplier });
            accessor.SetValue(_dummy, startValue);
            //var value = accessor.GetValue(_dummy);

            // Assert
            //Assert.AreEqual(startValue, _dummy.IndexerAmount);
            Assert.AreEqual(expectedGetter, _dummy[addition, multiplier]);
        }

        [Test]
        public void SetIndexerPath_ValueType_Passes()
        {
            // Arrange
            var addition = 4;
            var multiplier = 3;
            var startValue = 2;
            var expectedSetter = startValue * multiplier + addition;
            var expectedGettter = (expectedSetter + addition) * multiplier;
            var indexerPath = nameof(DummyObject.SubStruct) + "." + BuildPath(typeof(DummyObject).GetProperty("Item"));

            // Act
            _dummy.IndexerAmount = startValue;

            var accessor = AccessorsFactory.GetAccessor<int>(_dummy,
                                                             indexerPath, null, null,
                                                             new ValueProvider<int>[] { addition, multiplier });
            accessor.SetValue(_dummy, startValue);
            //var value = accessor.GetValue(_dummy);

            // Assert
            //Assert.AreEqual(startValue, _dummy.IndexerAmount);
            Assert.AreEqual(expectedGettter, _dummy.SubStruct[addition, multiplier]);
        }

        [Test]
        public void SetArrayElement_Passes()
        {
            // Arrange
            var value = 4;
            var path = $"{nameof(DummyObject.arrayFloats)}." + Accessors.AccessorPath.ArrayPrefix;
            _dummy.arrayFloats[2] = 0;

            // Act
            var accessor = AccessorsFactory.GetAccessor<int>(_dummy,
                                                             path, null, null,
                                                             new ValueProvider<int>[] { 2 });
            accessor.SetValue(_dummy, value);
            //var value = accessor.GetValue(_dummy);

            // Assert
            //Assert.AreEqual(startValue, _dummy.IndexerAmount);
            Assert.AreEqual(value, _dummy.arrayFloats[2]);
        }

        [Test]
        public void SetArrayElement_ValueType_Passes()
        {
            // Arrange
            var value = 4;
            var path = $"{nameof(DummyObject.SubStruct)}.{nameof(DummyObject.arrayFloats)}." + Accessors.AccessorPath.ArrayPrefix;
            _dummy.SubStruct = new DummyObject.InnerStruct() { arrayFloats = new float[] { 0, 0, 0, 0 } };

            // Act
            var accessor = AccessorsFactory.GetAccessor<int>(_dummy,
                                                             path, null, null,
                                                             new ValueProvider<int>[] { 2 });
            accessor.SetValue(_dummy, value);
            //var value = accessor.GetValue(_dummy);

            // Assert
            //Assert.AreEqual(startValue, _dummy.IndexerAmount);
            Assert.AreEqual(value, _dummy.SubStruct.arrayFloats[2]);
        }


        [Test]
        public void SetIndexerPath_AOT_Passes()
        {
            // Arrange
            var target = 16f;
            var body = new GameObject().AddComponent<Rigidbody>();
            body.mass = 4;
            var path = $"{nameof(DummyObject.SubStruct)}.{nameof(DummyObject.InnerStruct.dfloats)}.Item[float]";
            _dummy.SubStruct = new DummyObject.InnerStruct() { dfloats = new Dictionary<float, float>() { { 0, 2f }, { 4f, target } } };

            // Act
            var accessor = AccessorsFactory.GetAccessor<int>(_dummy,
                                                             path, null, null,
                                                             new object[] { new ReadOnlyBind<float>(body, "mass") });
            var value = accessor.GetValue(_dummy);
            //var value = accessor.GetValue(_dummy);

            // Assert
            //Assert.AreEqual(startValue, _dummy.IndexerAmount);
            Assert.AreEqual(target, value);
            Assert.AreEqual(_dummy.SubStruct.dfloats[body.mass], value);
            
            // Cleanup
            Object.Destroy(body.gameObject);
        }

        [Test]
        public void SetIndexerPath_SimpleUnity_AOT_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(Transform.transform)}.{nameof(Transform.position)}.Item[int]";
            var transform = new GameObject("Test Object").transform;

            transform.position = new Vector3(0, target, 0);

            // Act
            var accessor = AccessorsFactory.GetAccessor<int>(transform,
                                                             path, null, null,
                                                             new object[] { index });
            var value = accessor.GetValue(transform);
            //var value = accessor.GetValue(_dummy);

            // Assert
            //Assert.AreEqual(startValue, _dummy.IndexerAmount);
            Assert.AreEqual(target, value);
            Assert.AreEqual(transform.position[index], value);
            
            // Cleanup
            Object.Destroy(transform.gameObject);
        }

        [Test]
        public void SetArrayPath_Field_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(DummyObject.arrayFloats)}[{index}]";

            _dummy.arrayFloats = new float[] { 0, 0, 0, 0 };
            var expected = new float[] { 0, 0, 0, 0 };
            expected[index] = target;

            // Act
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, path);
            accessor.SetValue(_dummy, target);

            // Assert
            CollectionAssert.AreEquivalent(expected, _dummy.arrayFloats);
        }
        
        [Test]
        public void GetArrayPath_Field_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(DummyObject.arrayFloats)}[{index}]";

            _dummy.arrayFloats = new float[] { 0, 0, 0, 0 };
            _dummy.arrayFloats[index] = target;

            // Act
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, path);
            var value = accessor.GetValue(_dummy);

            // Assert
            Assert.AreEqual(target, value);
        }
        
        [Test]
        public void SetArrayPath_Property_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(DummyObject.ArrayFloats)}[{index}]";

            _dummy.ArrayFloats = new float[] { 0, 0, 0, 0 };
            var expected = new float[] { 0, 0, 0, 0 };
            expected[index] = target;

            // Act
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, path);
            accessor.SetValue(_dummy, target);

            // Assert
            CollectionAssert.AreEquivalent(expected, _dummy.ArrayFloats);
        }
        
        [Test]
        public void GetArrayPath_Property_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(DummyObject.ArrayFloats)}[{index}]";

            _dummy.ArrayFloats = new float[] { 0, 0, 0, 0 };
            _dummy.ArrayFloats[index] = target;

            // Act
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, path);
            var value = accessor.GetValue(_dummy);

            // Assert
            Assert.AreEqual(target, value);
        }
        
        [Test]
        public void SetListPath_Field_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(DummyObject.floats)}[{index}]";

            _dummy.floats = new() { 0, 0, 0, 0 };
            var expected = new List<float> { 0, 0, 0, 0 };
            expected[index] = target;

            // Act
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, path);
            accessor.SetValue(_dummy, target);

            // Assert
            CollectionAssert.AreEquivalent(expected, _dummy.floats);
        }
        
        [Test]
        public void GetListPath_Field_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(DummyObject.floats)}[{index}]";

            _dummy.floats = new () { 0, 0, 0, 0 };
            _dummy.floats[index] = target;

            // Act
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, path);
            var value = accessor.GetValue(_dummy);

            // Assert
            Assert.AreEqual(target, value);
        }
        
        [Test]
        public void SetListPath_Property_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(DummyObject.Floats)}[{index}]";

            _dummy.Floats = new() { 0, 0, 0, 0 };
            var expected = new List<float> { 0, 0, 0, 0 };
            expected[index] = target;

            // Act
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, path);
            accessor.SetValue(_dummy, target);

            // Assert
            CollectionAssert.AreEquivalent(expected, _dummy.Floats);
        }
        
        [Test]
        public void GetListPath_Property_Passes()
        {
            // Arrange
            var target = 16f;
            var index = 1;
            var path = $"{nameof(DummyObject.Floats)}[{index}]";

            _dummy.Floats = new () { 0, 0, 0, 0 };
            _dummy.Floats[index] = target;

            // Act
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, path);
            var value = accessor.GetValue(_dummy);

            // Assert
            Assert.AreEqual(target, value);
        }

#if ENABLE_PERFORMANCE_TESTS
        
        [Test, Performance]
        [TestCase(k_PerfIterations, k_PerfMeasurements, true, WhatToTest.Everything)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.Everything)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.ShortFields | WhatToTest.LongFields)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.ShortProperties | WhatToTest.Compound)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.ShortFields | WhatToTest.ShortProperties)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.LongFields | WhatToTest.Compound)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.ShortFields)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.LongFields)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.ShortProperties)]
        [TestCase(k_PerfIterations, k_PerfMeasurements, false, WhatToTest.Compound)]
        public void PerformanceTest(int iterations, int measurements, bool withReflection, WhatToTest what)
        {
            // Define values
            object[] values =
            {
                42f, 
                Color.green, 
                Color.red, 
                69f, 
                new Vector3(0, 90, 0), 
                new Vector3(3, 0, 0),
                "Hello Struct", 
                "Hello Class", 
                0.5f, 
                42f, 
                "Hello again Struct", 
                new Vector2(3, 9),
                new Vector2(4, 16),
                new Vector3(3, 3, 0),
                new Vector3(3, 5, 0),
                new Vector3(3, 7, 9),
            };

            var directResult = new object[values.Length];
            var bindResult = new object[values.Length];
            var reflectionResult = new object[values.Length];

            object[] accessors =
            {
                AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.publicFloat)),
                AccessorsFactory.GetAccessor<Color>(_dummy, nameof(DummyObject.publicColor)),
                AccessorsFactory.GetAccessor<Color>(_dummy, nameof(DummyObject.Color)),
                AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.FloatProperty)),
                AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.Vector3)),
                AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.Transform) + "." + nameof(Transform.localEulerAngles)),
                // Long Fields
                AccessorsFactory.GetAccessor<DummyObject.InnerStruct>(_dummy, $"{nameof(DummyObject.SubStruct)}"),
                AccessorsFactory.GetAccessor<string>(_dummy, $"{nameof(DummyObject.SubClass)}.{nameof(DummyObject.SubClass.str)}"),
                AccessorsFactory.GetAccessor<float>(_dummy, $"{nameof(DummyObject.publicColor)}.{nameof(Color.a)}"),
                AccessorsFactory.GetAccessor<float>(_dummy, $"{nameof(DummyObject.Vector3)}.{nameof(Vector3.z)}"),
                AccessorsFactory.GetAccessor<string>(_dummy, $"{nameof(DummyObject.SubStruct)}.{nameof(DummyObject.SubStruct.str)}"),
                AccessorsFactory.GetAccessor<Vector2>(_dummy, $"{nameof(DummyObject.SubStruct)}.{nameof(DummyObject.SubStruct.vec2)}"),
                AccessorsFactory.GetAccessor<Vector2>(_dummy, $"{nameof(DummyObject.SubClass)}.{nameof(DummyObject.SubClass.vec2)}"),
                //
                AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.publicVector)),
                AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.Transform) + "." + nameof(Transform.position)),
                AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.Transform) + "." + nameof(Transform.root) + "." + nameof(Transform.localScale)),
            };
            
            Debug.Log("Accessors: \n" + string.Join("\n", accessors.Select((a, i) => $"[{i}] - {a.GetType().UserFriendlyName()}").ToArray()));

            (Action<object, object> setter, Func<object, object> getter)[] reflection =
            {
                GetAccessors<DummyObject>(nameof(DummyObject.publicFloat)),
                GetAccessors<DummyObject>(nameof(DummyObject.publicColor)),
                GetAccessors<DummyObject>(nameof(DummyObject.Color)),
                GetAccessors<DummyObject>(nameof(DummyObject.FloatProperty)),
                GetAccessors<DummyObject>(nameof(DummyObject.Vector3)),
                GetAccessors<Transform>(nameof(Transform.localEulerAngles)),
                GetAccessors<DummyObject>(nameof(DummyObject.SubStruct)),
                GetAccessors<DummyObject.InnerClass>(nameof(DummyObject.SubClass.str)),
                GetAccessors<Color>(nameof(Color.a)),
                GetAccessors<Vector3>(nameof(Vector3.z)),
                GetAccessors<DummyObject.InnerStruct>(nameof(DummyObject.SubStruct.str)),
                GetAccessors<DummyObject.InnerStruct>(nameof(DummyObject.SubStruct.vec2)),
                GetAccessors<DummyObject.InnerClass>(nameof(DummyObject.SubClass.vec2)),
                GetAccessors<DummyObject>(nameof(DummyObject.publicVector)),
                GetAccessors<Transform>(nameof(Transform.position)),
                GetAccessors<Transform>(nameof(Transform.localScale)),
            };

            (Delegate getter, Delegate setter) Compile<T>(string path, IAccessor<T> accessor)
            {
                var compilingAccessor = accessor as ICompiledAccessor<T>;
                return (compilingAccessor.CompileGetter(), compilingAccessor.CompileSetter());
            }
            
            (Delegate getter, Delegate setter)[] compiled =
            {
                Compile(nameof(DummyObject.publicFloat), AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.publicFloat))),
                Compile(nameof(DummyObject.publicColor), AccessorsFactory.GetAccessor<Color>(_dummy, nameof(DummyObject.publicColor))),
                Compile(nameof(DummyObject.Color), AccessorsFactory.GetAccessor<Color>(_dummy, nameof(DummyObject.Color))),
                Compile(nameof(DummyObject.FloatProperty), AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.FloatProperty))),
                Compile(nameof(DummyObject.Vector3), AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.Vector3))),
                Compile(nameof(DummyObject.Transform) + "." + nameof(Transform.localEulerAngles), AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.Transform) + "." + nameof(Transform.localEulerAngles))),
                // Long Fields
                Compile($"{nameof(DummyObject.SubStruct)}", AccessorsFactory.GetAccessor<DummyObject.InnerStruct>(_dummy, $"{nameof(DummyObject.SubStruct)}")),
                Compile($"{nameof(DummyObject.SubClass)}.{nameof(DummyObject.SubClass.str)}", AccessorsFactory.GetAccessor<string>(_dummy, $"{nameof(DummyObject.SubClass)}.{nameof(DummyObject.SubClass.str)}")),
                Compile($"{nameof(DummyObject.publicColor)}.{nameof(Color.a)}", AccessorsFactory.GetAccessor<float>(_dummy, $"{nameof(DummyObject.publicColor)}.{nameof(Color.a)}")),
                Compile($"{nameof(DummyObject.Vector3)}.{nameof(Vector3.z)}", AccessorsFactory.GetAccessor<float>(_dummy, $"{nameof(DummyObject.Vector3)}.{nameof(Vector3.z)}")),
                Compile($"{nameof(DummyObject.SubStruct)}.{nameof(DummyObject.SubStruct.str)}", AccessorsFactory.GetAccessor<string>(_dummy, $"{nameof(DummyObject.SubStruct)}.{nameof(DummyObject.SubStruct.str)}")),
                Compile($"{nameof(DummyObject.SubStruct)}.{nameof(DummyObject.SubStruct.vec2)}", AccessorsFactory.GetAccessor<Vector2>(_dummy, $"{nameof(DummyObject.SubStruct)}.{nameof(DummyObject.SubStruct.vec2)}")),
                Compile($"{nameof(DummyObject.SubClass)}.{nameof(DummyObject.SubClass.vec2)}", AccessorsFactory.GetAccessor<Vector2>(_dummy, $"{nameof(DummyObject.SubClass)}.{nameof(DummyObject.SubClass.vec2)}")),
                //
                Compile(nameof(DummyObject.publicVector), AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.publicVector))),
                Compile(nameof(DummyObject.Transform) + "." + nameof(Transform.position), AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.Transform) + "." + nameof(Transform.position))),
                Compile(nameof(DummyObject.Transform) + "." + nameof(Transform.root) + "." + nameof(Transform.localScale), AccessorsFactory.GetAccessor<Vector3>(_dummy, nameof(DummyObject.Transform) + "." + nameof(Transform.root) + "." + nameof(Transform.localScale))),
            };

            SampleGroup samplegroup = new SampleGroup("TotalAllocatedMemory", SampleUnit.Kilobyte, false);
            SampleGroup methodsGroup = new SampleGroup("Methods Group", SampleUnit.Undefined, false);

            // Perform the logic
            long preRunAllocatedMem = 0;
            long allocatedMemory = 0;

            if (withReflection)
            {
                // Reflection
                preRunAllocatedMem = Profiler.GetTotalAllocatedMemoryLong();
                Measure.Method(ReflectionSetters).WarmupCount(5).IterationsPerMeasurement(iterations).MeasurementCount(measurements).GC().Run();
                allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - preRunAllocatedMem;
                Measure.Custom(samplegroup, allocatedMemory / 1024f);

                preRunAllocatedMem = Profiler.GetTotalAllocatedMemoryLong();
                Measure.Method(ReflectionGetters).WarmupCount(5).IterationsPerMeasurement(iterations).MeasurementCount(measurements).GC().Run();
                allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - preRunAllocatedMem;
                Measure.Custom(samplegroup, allocatedMemory / 1024f);
                Measure.Custom(methodsGroup, 0);
            }

            preRunAllocatedMem = Profiler.GetTotalAllocatedMemoryLong();
            Measure.Method(BindSetters).WarmupCount(5).IterationsPerMeasurement(iterations).MeasurementCount(measurements).GC().Run();
            allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - preRunAllocatedMem;
            Measure.Custom(samplegroup, allocatedMemory / 1024f);

            preRunAllocatedMem = Profiler.GetTotalAllocatedMemoryLong();
            Measure.Method(BindGetters).WarmupCount(5).IterationsPerMeasurement(iterations).MeasurementCount(measurements).GC().Run();
            allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - preRunAllocatedMem;
            Measure.Custom(samplegroup, allocatedMemory / 1024f);
            Measure.Custom(methodsGroup, 1);

            
            preRunAllocatedMem = Profiler.GetTotalAllocatedMemoryLong();
            Measure.Method(CompiledSetters).WarmupCount(5).IterationsPerMeasurement(iterations).MeasurementCount(measurements).GC().Run();
            allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - preRunAllocatedMem;
            Measure.Custom(samplegroup, allocatedMemory / 1024f);
            
            preRunAllocatedMem = Profiler.GetTotalAllocatedMemoryLong();
            Measure.Method(CompiledGetters).WarmupCount(5).IterationsPerMeasurement(iterations).MeasurementCount(measurements).GC().Run();
            allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - preRunAllocatedMem;
            Measure.Custom(samplegroup, allocatedMemory / 1024f);
            Measure.Custom(methodsGroup, 2);
            
            preRunAllocatedMem = Profiler.GetTotalAllocatedMemoryLong();
            Measure.Method(DirectSetters).WarmupCount(5).IterationsPerMeasurement(iterations).MeasurementCount(measurements).GC().Run();
            allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - preRunAllocatedMem;
            Measure.Custom(samplegroup, allocatedMemory / 1024f);

            preRunAllocatedMem = Profiler.GetTotalAllocatedMemoryLong();
            Measure.Method(DirectGetters).WarmupCount(5).IterationsPerMeasurement(iterations).MeasurementCount(measurements).GC().Run();
            allocatedMemory = Profiler.GetTotalAllocatedMemoryLong() - preRunAllocatedMem;
            Measure.Custom(samplegroup, allocatedMemory / 1024f);
            Measure.Custom(methodsGroup, 3);

            // Definitions 
            void DirectSetters()
            {
                // Simple calls
                if (what.HasFlag(WhatToTest.ShortFields))
                {
                    _dummy.publicFloat = (float)values[0];
                    _dummy.publicColor = (Color)values[1];
                    _dummy.publicVector = (Vector3)values[13];
                }
                
                if (what.HasFlag(WhatToTest.ShortProperties))
                {
                    _dummy.Color = (Color)values[2];
                    _dummy.FloatProperty = (float)values[3];
                    _dummy.Vector3 = (Vector3)values[4];
                }
                
                if (what.HasFlag(WhatToTest.LongFields))
                {
                    // Complex calls
                    _dummy.publicColor.a = (float)values[8];
                }

                if (what.HasFlag(WhatToTest.Compound))
                {
                    _dummy.Transform.localEulerAngles = (Vector3)values[5];
                    _dummy.Transform.position = (Vector3)values[14];
                    _dummy.Transform.root.localScale = (Vector3)values[15];
                    
                    _dummy.SubStruct = new DummyObject.InnerStruct() { str = (string)values[6] };
                    _dummy.SubClass.str = (string)values[7];
                    
                    var vector = _dummy.Vector3;
                    vector.z = (float)values[9];
                    _dummy.Vector3 = vector;
                    
                    var subStruct = _dummy.SubStruct;
                    subStruct.str = (string)values[10];
                    _dummy.SubStruct = subStruct;
                    subStruct = _dummy.SubStruct;
                    subStruct.vec2 = (Vector2)values[11];
                    _dummy.SubStruct = subStruct;

                    _dummy.SubClass.vec2 = (Vector2)values[12];
                }
            }

            void BindSetters()
            {
                if (what.HasFlag(WhatToTest.ShortFields))
                {
                    ((Accessors.IAccessor<float>)accessors[0]).SetValue(_dummy, (float)values[0]);
                    ((Accessors.IAccessor<Color>)accessors[1]).SetValue(_dummy, (Color)values[1]);
                    ((Accessors.IAccessor<Vector3>)accessors[13]).SetValue(_dummy, (Vector3)values[13]);
                }
                if (what.HasFlag(WhatToTest.ShortProperties))
                {
                    ((Accessors.IAccessor<Color>)accessors[2]).SetValue(_dummy, (Color)values[2]);
                    ((Accessors.IAccessor<float>)accessors[3]).SetValue(_dummy, (float)values[3]);
                    ((Accessors.IAccessor<Vector3>)accessors[4]).SetValue(_dummy, (Vector3)values[4]);
                }
                if (what.HasFlag(WhatToTest.LongFields))
                {
                    ((Accessors.IAccessor<float>)accessors[8]).SetValue(_dummy, (float)values[8]);
                }
                if (what.HasFlag(WhatToTest.Compound))
                {
                    ((Accessors.IAccessor<Vector3>)accessors[5]).SetValue(_dummy, (Vector3)values[5]);
                    ((PropertyObjectTypeAccessor<DummyObject, DummyObject.InnerStruct>)accessors[6]).SetValue(ref _dummy, new DummyObject.InnerStruct() { str = (string)values[6] });
                    ((Accessors.IAccessor<string>)accessors[7]).SetValue(_dummy, (string)values[7]);
                    ((Accessors.IAccessor<float>)accessors[9]).SetValue(_dummy, (float)values[9]);
                    ((Accessors.IAccessor<string>)accessors[10]).SetValue(_dummy, (string)values[10]);
                    ((Accessors.IAccessor<Vector2>)accessors[11]).SetValue(_dummy, (Vector2)values[11]);
                    ((Accessors.IAccessor<Vector2>)accessors[12]).SetValue(_dummy, (Vector2)values[12]);
                    ((Accessors.IAccessor<Vector3>)accessors[14]).SetValue(_dummy, (Vector3)values[14]);
                    ((Accessors.IAccessor<Vector3>)accessors[15]).SetValue(_dummy, (Vector3)values[15]);
                }
            }
            
            void CompiledSetters()
            {
                if (what.HasFlag(WhatToTest.ShortFields))
                {
                    ((RefSetterDelegate<float>)compiled[0].setter)(_dummy, (float)values[0]);
                    ((RefSetterDelegate<Color>)compiled[1].setter)(_dummy, (Color)values[1]);
                    ((RefSetterDelegate<Vector3>)compiled[13].setter)(_dummy, (Vector3)values[13]);
                }
                if (what.HasFlag(WhatToTest.ShortProperties))
                {
                    ((RefSetterDelegate<Color>)compiled[2].setter)(_dummy, (Color)values[2]);
                    ((RefSetterDelegate<float>)compiled[3].setter)(_dummy, (float)values[3]);
                    ((RefSetterDelegate<Vector3>)compiled[4].setter)(_dummy, (Vector3)values[4]);
                }
                if (what.HasFlag(WhatToTest.LongFields))
                {
                    ((RefSetterDelegate<float>)compiled[8].setter)(_dummy, (float)values[8]);
                }
                if (what.HasFlag(WhatToTest.Compound))
                {
                    ((RefSetterDelegate<DummyObject.InnerStruct>)compiled[6].setter)(_dummy, new DummyObject.InnerStruct() { str = (string)values[6]});
                    ((RefSetterDelegate<string>)compiled[7].setter)(_dummy, (string)values[7]);
                    ((RefSetterDelegate<float>)compiled[9].setter)(_dummy, (float)values[9]);
                    ((RefSetterDelegate<string>)compiled[10].setter)(_dummy, (string)values[10]);
                    ((RefSetterDelegate<Vector2>)compiled[11].setter)(_dummy, (Vector2)values[11]);
                    ((RefSetterDelegate<Vector2>)compiled[12].setter)(_dummy, (Vector2)values[12]);
                    ((RefSetterDelegate<Vector3>)compiled[5].setter)(_dummy, (Vector3)values[5]);
                    ((RefSetterDelegate<Vector3>)compiled[14].setter)(_dummy, (Vector3)values[14]);
                    ((RefSetterDelegate<Vector3>)compiled[15].setter)(_dummy, (Vector3)values[15]);
                }
            }

            void ReflectionSetters()
            {
                if (what.HasFlag(WhatToTest.ShortFields))
                {
                    reflection[0].setter(_dummy, (float)values[0]);
                    reflection[1].setter(_dummy, (Color)values[1]);
                    reflection[13].setter(_dummy, (Vector3)values[13]);
                }
                if (what.HasFlag(WhatToTest.ShortProperties))
                {
                    reflection[2].setter(_dummy, (Color)values[2]);
                    reflection[3].setter(_dummy, (float)values[3]);
                    reflection[4].setter(_dummy, (Vector3)values[4]);
                }
                if (what.HasFlag(WhatToTest.LongFields))
                {
                    reflection[8].setter(_dummy.publicColor, (float)values[8]);
                }
                if (what.HasFlag(WhatToTest.Compound))
                {
                    reflection[6].setter(_dummy, new DummyObject.InnerStruct() { str = (string)values[6] });
                    reflection[7].setter(_dummy.SubClass, (string)values[7]);
                    reflection[9].setter(_dummy.Vector3, (float)values[9]);
                    reflection[10].setter(_dummy.SubStruct, (string)values[10]);
                    reflection[11].setter(_dummy.SubStruct, (Vector2)values[11]);
                    reflection[12].setter(_dummy.SubClass, (Vector2)values[12]);
                    reflection[5].setter(_dummy.Transform, (Vector3)values[5]);
                    reflection[14].setter(_dummy.Transform, (Vector3)values[14]);
                    reflection[15].setter(_dummy.Transform, (Vector3)values[15]);
                }
            }

            void DirectGetters()
            {
                if (what.HasFlag(WhatToTest.ShortFields))
                {
                    directResult[0] = _dummy.publicFloat;
                    directResult[1] = _dummy.publicColor;
                    directResult[13] = _dummy.publicVector;
                }
                if (what.HasFlag(WhatToTest.ShortProperties))
                {
                    directResult[2] = _dummy.Color;
                    directResult[3] = _dummy.FloatProperty;
                    directResult[4] = _dummy.Vector3;
                }
                if (what.HasFlag(WhatToTest.LongFields))
                {
                    directResult[8] = _dummy.publicColor.a;
                }
                if (what.HasFlag(WhatToTest.Compound))
                {
                    directResult[5] = _dummy.Transform.localEulerAngles;
                    directResult[6] = _dummy.SubStruct.str;
                    directResult[7] = _dummy.SubClass.str;
                    directResult[9] = _dummy.Vector3.z;
                    directResult[10] = _dummy.SubStruct.str;
                    directResult[11] = _dummy.SubStruct.vec2;
                    directResult[12] = _dummy.SubClass.vec2;
                    directResult[14] = _dummy.Transform.position;
                    directResult[15] = _dummy.Transform.root.localScale;
                }
            }

            void BindGetters()
            {
                if (what.HasFlag(WhatToTest.ShortFields))
                {
                    bindResult[0] = ((Accessors.IAccessor<float>)accessors[0]).GetValue(_dummy);
                    bindResult[1] = ((Accessors.IAccessor<Color>)accessors[1]).GetValue(_dummy);
                    bindResult[13] = ((Accessors.IAccessor<Vector3>)accessors[13]).GetValue(_dummy);
                }
                if (what.HasFlag(WhatToTest.ShortProperties))
                {
                    bindResult[2] = ((Accessors.IAccessor<Color>)accessors[2]).GetValue(_dummy);
                    bindResult[3] = ((Accessors.IAccessor<float>)accessors[3]).GetValue(_dummy);
                    bindResult[4] = ((Accessors.IAccessor<Vector3>)accessors[4]).GetValue(_dummy);
                }

                if (what.HasFlag(WhatToTest.LongFields))
                {
                    bindResult[8] = ((Accessors.IAccessor<float>)accessors[8]).GetValue(_dummy);
                }

                if (what.HasFlag(WhatToTest.Compound))
                {
                    bindResult[5] = ((Accessors.IAccessor<Vector3>)accessors[4]).GetValue(_dummy);
                    bindResult[6] = ((Accessors.IAccessor<DummyObject.InnerStruct>)accessors[6]).GetValue(_dummy).str;
                    bindResult[7] = ((Accessors.IAccessor<string>)accessors[7]).GetValue(_dummy);
                    bindResult[14] = ((Accessors.IAccessor<Vector3>)accessors[14]).GetValue(_dummy);
                    bindResult[15] = ((Accessors.IAccessor<Vector3>)accessors[15]).GetValue(_dummy);
                    bindResult[9] = ((Accessors.IAccessor<float>)accessors[9]).GetValue(_dummy);
                    bindResult[10] = ((Accessors.IAccessor<string>)accessors[10]).GetValue(_dummy);
                    bindResult[11] = ((Accessors.IAccessor<Vector2>)accessors[11]).GetValue(_dummy);
                    bindResult[12] = ((Accessors.IAccessor<Vector2>)accessors[12]).GetValue(_dummy);
                }
            }
            
            void CompiledGetters()
            {
                if (what.HasFlag(WhatToTest.ShortFields))
                {
                    bindResult[0] = ((RefGetterDelegate<float>)compiled[0].getter)(_dummy);
                    bindResult[1] = ((RefGetterDelegate<Color>)compiled[1].getter)(_dummy);
                    bindResult[13] = ((RefGetterDelegate<Vector3>)compiled[13].getter)(_dummy);
                }
                if (what.HasFlag(WhatToTest.ShortProperties))
                {
                    bindResult[2] = ((RefGetterDelegate<Color>)compiled[2].getter)(_dummy);
                    bindResult[3] = ((RefGetterDelegate<float>)compiled[3].getter)(_dummy);
                    bindResult[4] = ((RefGetterDelegate<Vector3>)compiled[4].getter)(_dummy);
                }
                if (what.HasFlag(WhatToTest.LongFields))
                {
                    bindResult[8] = ((RefGetterDelegate<float>)compiled[8].getter)(_dummy);
                }
                if (what.HasFlag(WhatToTest.Compound))
                {
                    bindResult[5] = ((RefGetterDelegate<Vector3>)compiled[4].getter)(_dummy);
                    bindResult[6] = ((RefGetterDelegate<DummyObject.InnerStruct>)compiled[6].getter)(_dummy).str;
                    bindResult[7] = ((RefGetterDelegate<string>)compiled[7].getter)(_dummy);
                    bindResult[9] = ((RefGetterDelegate<float>)compiled[9].getter)(_dummy);
                    bindResult[10] = ((RefGetterDelegate<string>)compiled[10].getter)(_dummy);
                    bindResult[11] = ((RefGetterDelegate<Vector2>)compiled[11].getter)(_dummy);
                    bindResult[12] = ((RefGetterDelegate<Vector2>)compiled[12].getter)(_dummy);
                    bindResult[14] = ((RefGetterDelegate<Vector3>)compiled[14].getter)(_dummy);
                    bindResult[15] = ((RefGetterDelegate<Vector3>)compiled[15].getter)(_dummy);
                }
            }

            void ReflectionGetters()
            {
                if (what.HasFlag(WhatToTest.ShortFields))
                {
                    reflectionResult[0] = reflection[0].getter(_dummy);
                    reflectionResult[1] = reflection[1].getter(_dummy);
                    reflectionResult[13] = reflection[13].getter(_dummy);
                }
                if (what.HasFlag(WhatToTest.ShortProperties))
                {
                    reflectionResult[2] = reflection[2].getter(_dummy);
                    reflectionResult[3] = reflection[3].getter(_dummy);
                    reflectionResult[4] = reflection[4].getter(_dummy);
                }
                if (what.HasFlag(WhatToTest.LongFields))
                {
                    reflectionResult[8] = reflection[8].getter(_dummy.publicColor);
                }
                if (what.HasFlag(WhatToTest.Compound))
                {
                    reflectionResult[5] = reflection[5].getter(_dummy.Transform);
                    reflectionResult[6] = reflection[6].getter(_dummy);
                    reflectionResult[7] = reflection[7].getter(_dummy.SubClass);
                    reflectionResult[9] = reflection[9].getter(_dummy.Vector3);
                    reflectionResult[10] = reflection[10].getter(_dummy.SubStruct);
                    reflectionResult[11] = reflection[11].getter(_dummy.SubStruct);
                    reflectionResult[12] = reflection[12].getter(_dummy.SubClass);
                    reflectionResult[14] = reflection[14].getter(_dummy.Transform);
                    reflectionResult[15] = reflection[15].getter(_dummy.Transform);
                }
            }

            (Action<object, object> setter, Func<object, object> getter) GetAccessors<T>(string name)
            {
                var fieldInfo = typeof(T).GetField(name);
                if (fieldInfo != null)
                {
                    return (fieldInfo.SetValue, fieldInfo.GetValue);
                }
                var property = typeof(T).GetProperty(name);
                if(property != null)
                {
                    return (property.SetValue, property.GetValue);
                }
                return default;
            }
        }
        
        [Test, Performance]
        [TestCase("Direct", "FieldAccessor", "PreciseAccessor", "Accessor", "Lambda")]
        public void Field_Set_Performance(string a, string b, string c, string d, string e)
        {
            var fieldAccessor = Reflect.From<DummyObject>.Get<float>(nameof(DummyObject.publicFloat));
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.publicFloat));
            var value = 42f;
            var specializedAccessor = (ClassFieldValueAccessor<DummyObject, float>)accessor;
            Action<object, float> setter = (target, val) => specializedAccessor.SetValue(target, val);
            
            Measure.Method(() =>
            {
                _dummy.publicFloat = value;
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                fieldAccessor.SetValue(_dummy, value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                specializedAccessor.SetValue(ref _dummy, value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                accessor.SetValue(_dummy, value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                setter(_dummy, value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
        
        [Test, Performance]
        [TestCase("Direct", "FieldAccessor", "PreciseAccessor", "Accessor", "Lambda")]
        public void Field_Get_Performance(string a, string b, string c, string d, string e)
        {
            var fieldAccessor = Reflect.From<DummyObject>.Get<float>(nameof(DummyObject.publicFloat));
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.publicFloat));
            var specializedAccessor = (ClassFieldValueAccessor<DummyObject, float>)accessor;
            
            Func<DummyObject, float> getter = target => specializedAccessor.GetValue(target);
            
            Measure.Method(() =>
            {
                var value = _dummy.publicFloat;
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                var value = fieldAccessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                var value = specializedAccessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                var value = accessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                var value = getter(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
        
        [Test, Performance]
        [TestCase("Direct", "FieldAccessor", "PreciseAccessor", "Accessor", "Lambda")]
        public void LongField_Set_Performance(string a, string b, string c, string d, string e)
        {
            var fieldAccessor = Reflect.From<DummyObject>.Get<float>(nameof(DummyObject.publicColor), nameof(Color.a));
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.publicColor) + "." + nameof(Color.a));
            var value = 42f;
            var specializedAccessor = (ClassFieldValueAccessor<DummyObject, float>)accessor;
            Action<object, float> setter = (target, val) => specializedAccessor.SetValue(target, val);
            
            Measure.Method(() =>
            {
                _dummy.publicFloat = value;
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                fieldAccessor.SetValue(_dummy, value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                specializedAccessor.SetValue(ref _dummy, value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                accessor.SetValue(_dummy, value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                setter(_dummy, value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
        
        [Test, Performance]
        [TestCase("Direct", "FieldAccessor", "PreciseAccessor", "Accessor", "Lambda")]
        public void LongField_Get_Performance(string a, string b, string c, string d, string e)
        {
            var fieldAccessor = Reflect.From<DummyObject>.Get<float>(nameof(DummyObject.publicColor), nameof(Color.a));
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.publicColor) + "." + nameof(Color.a));
            var specializedAccessor = (ClassFieldValueAccessor<DummyObject, float>)accessor;
            
            Func<DummyObject, float> getter = target => specializedAccessor.GetValue(target);
            
            Measure.Method(() =>
            {
                var value = _dummy.publicFloat;
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                var value = fieldAccessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                var value = specializedAccessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                var value = accessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            
            Measure.Method(() =>
            {
                var value = getter(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
        
#endif
    }
}
using NUnit.Framework;
#if ENABLE_PERFORMANCE_TESTS
using Unity.PerformanceTesting;
#endif
using UnityEngine;
using Object = UnityEngine.Object;
using Reflect = Postica.Common.Reflection.Reflect;

namespace Postica.BindingSystem
{
    public class ReflectTests
    {
        private DummyObject _dummy;
        private DerivedDummyObject _derivedDummy;

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

        // Make the same tests for other publicFields of DummyObject
        [Test]
        public void SetFieldFast_Bool_Passes()
        {
            var expectedValue = true;
            var accessor = Reflect.From<DummyObject>.Get<bool>(nameof(DummyObject.publicBool));
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicBool);
        }

        [Test]
        public void GetFieldFast_Bool_Passes()
        {
            var expectedValue = true;
            var accessor = Reflect.From<DummyObject>.Get<bool>(nameof(DummyObject.publicBool));
            _dummy.publicBool = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void SetFieldFast_Byte_Passes()
        {
            var expectedValue = (byte)42;
            var accessor = Reflect.From<DummyObject>.Get<byte>(nameof(DummyObject.publicByte));
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicByte);
        }

        [Test]
        public void GetFieldFast_Byte_Passes()
        {
            var expectedValue = (byte)42;
            var accessor = Reflect.From<DummyObject>.Get<byte>(nameof(DummyObject.publicByte));
            _dummy.publicByte = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void SetFieldFast_Short_Passes()
        {
            var expectedValue = (short)42;
            var accessor = Reflect.From<DummyObject>.Get<short>(nameof(DummyObject.publicShort));
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicShort);
        }

        [Test]
        public void GetFieldFast_Short_Passes()
        {
            var expectedValue = (short)42;
            var accessor = Reflect.From<DummyObject>.Get<short>(nameof(DummyObject.publicShort));
            _dummy.publicShort = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void SetFieldFast_Long_Passes()
        {
            var expectedValue = 42L;
            var accessor = Reflect.From<DummyObject>.Get<long>(nameof(DummyObject.publicLong));
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicLong);
        }

        [Test]
        public void GetFieldFast_Long_Passes()
        {
            var expectedValue = 42L;
            var accessor = Reflect.From<DummyObject>.Get<long>(nameof(DummyObject.publicLong));
            _dummy.publicLong = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void SetFieldFast_Double_Passes()
        {
            var expectedValue = 42.0;
            var accessor = Reflect.From<DummyObject>.Get<double>(nameof(DummyObject.publicDouble));
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicDouble);
        }

        [Test]
        public void GetFieldFast_Double_Passes()
        {
            var expectedValue = 42.0;
            var accessor = Reflect.From<DummyObject>.Get<double>(nameof(DummyObject.publicDouble));
            _dummy.publicDouble = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void SetFieldFast_Vector3_Passes()
        {
            var expectedValue = Vector3.one * 42;
            var accessor = Reflect.From<DummyObject>.Get<Vector3>(nameof(DummyObject.publicVector));
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicVector);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        public void SetFieldFast_Vector3_Performance()
        {
            var accessor = Reflect.From<DummyObject>.Get<Vector3>(nameof(DummyObject.publicVector));
            Measure.Method(() => { accessor.SetValue(_dummy, Vector3.one); }).WarmupCount(10)
                .IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() => { _dummy.publicVector = Vector3.one; }).WarmupCount(10)
                .IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
#endif

        [Test]
        public void GetFieldFast_Vector3_Passes()
        {
            var expectedValue = Vector3.one * 42;
            var accessor = Reflect.From<DummyObject>.Get<Vector3>(nameof(DummyObject.publicVector));
            _dummy.publicVector = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        public void GetFieldFast_Vector3_Performance()
        {
            var accessor = Reflect.From<DummyObject>.Get<Vector3>(nameof(DummyObject.publicVector));
            Measure.Method(() =>
            {
                _dummy.publicVector = Vector3.one;
                var actual = accessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() =>
            {
                _dummy.publicVector = Vector3.one;
                var actual = _dummy.publicVector;
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }

#endif

        [Test]
        public void SetFieldFast_Vector3_y_Passes()
        {
            var expectedValue = 42;
            var accessor = Reflect.From<DummyObject>.Get<float>(nameof(DummyObject.publicVector), nameof(Vector3.y));
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicVector.y);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        [TestCase(false)]
        [TestCase(true)]
        public void SetFieldFast_Vector3_y_Performance(bool assert)
        {
            var accessor = Reflect.From<DummyObject>.Get<float>(nameof(DummyObject.publicVector), nameof(Vector3.y));
            Measure.Method(() =>
            {
                accessor.SetValue(_dummy, 42);
                if (assert)
                {
                    Assert.AreEqual(42, _dummy.publicVector.y);
                }
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() =>
            {
                _dummy.publicVector.y = 42;
                if (assert)
                {
                    Assert.AreEqual(42, _dummy.publicVector.y);
                }
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }

#endif

        [Test]
        public void GetFieldFast_Vector3_y_Passes()
        {
            var expectedValue = 42;
            var accessor = Reflect.From<DummyObject>.Get<float>(nameof(DummyObject.publicVector), nameof(Vector3.y));
            _dummy.publicVector.y = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        [TestCase(false)]
        [TestCase(true)]
        public void GetFieldFast_Vector3_y_Performance(bool assert)
        {
            var accessor = Reflect.From<DummyObject>.Get<float>(nameof(DummyObject.publicVector), nameof(Vector3.y));
            Measure.Method(() =>
            {
                _dummy.publicVector.y = 42;
                var actual = accessor.GetValue(_dummy);
                if (assert)
                {
                    Assert.AreEqual(42, actual);
                }
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() =>
            {
                _dummy.publicVector.y = 42;
                var actual = _dummy.publicVector.y;
                if (assert)
                {
                    Assert.AreEqual(42, actual);
                }
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
#endif

        [Test]
        public void SetFieldFast_Split_Vector3_y_Passes()
        {
            var expectedValue = 42;
            var accessor = Reflect.FromStruct<Vector3>.Get<float>(nameof(Vector3.y));
            accessor.SetValue(ref _dummy.publicVector, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicVector.y);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        [TestCase(false)]
        [TestCase(true)]
        public void SetFieldFast_Split_Vector3_y_Performance(bool assert)
        {
            var accessor = Reflect.FromStruct<Vector3>.Get<float>(nameof(Vector3.y));
            Measure.Method(() =>
            {
                accessor.SetValue(ref _dummy.publicVector, 42f);
                if (assert)
                {
                    Assert.AreEqual(42, _dummy.publicVector.y);
                }
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() =>
            {
                _dummy.publicVector.y = 42;
                if (assert)
                {
                    Assert.AreEqual(42, _dummy.publicVector.y);
                }
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
#endif

        [Test]
        public void GetFieldFast_Split_Vector3_y_Passes()
        {
            var expectedValue = 42;
            var accessor = Reflect.FromStruct<Vector3>.Get<float>(nameof(Vector3.y));
            _dummy.publicVector.y = expectedValue;
            var actualValue = accessor.GetValue(ref _dummy.publicVector);
            Assert.AreEqual(expectedValue, actualValue);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        [TestCase(false)]
        [TestCase(true)]
        public void GetFieldFast_Split_Vector3_y_Performance(bool assert)
        {
            var accessor = Reflect.FromStruct<Vector3>.Get<float>(nameof(Vector3.y));
            Measure.Method(() =>
            {
                _dummy.publicVector.y = 42;
                var actual = accessor.GetValue(ref _dummy.publicVector);
                if (assert)
                {
                    Assert.AreEqual(42, actual);
                }
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() =>
            {
                _dummy.publicVector.y = 42;
                var actual = _dummy.publicVector.y;
                if (assert)
                {
                    Assert.AreEqual(42, actual);
                }
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
#endif

        [Test]
        public void SetFieldFast_Color_Passes()
        {
            var expectedValue = Color.red;
            var accessor = Reflect.From<DummyObject>.Get<Color>(nameof(DummyObject.publicColor));
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.publicColor);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        public void SetFieldFast_Color_Performance()
        {
            var accessor = Reflect.From<DummyObject>.Get<Color>(nameof(DummyObject.publicColor));
            Measure.Method(() => { accessor.SetValue(_dummy, Color.red); }).WarmupCount(10)
                .IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() => { _dummy.publicColor = Color.red; }).WarmupCount(10).IterationsPerMeasurement(100000)
                .MeasurementCount(20).GC().Run();
        }
#endif

        [Test]
        public void GetFieldFast_Color_Passes()
        {
            var expectedValue = Color.red;
            var accessor = Reflect.From<DummyObject>.Get<Color>(nameof(DummyObject.publicColor));
            _dummy.publicColor = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        public void GetFieldFast_Color_Performance()
        {
            var accessor = Reflect.From<DummyObject>.Get<Color>(nameof(DummyObject.publicColor));
            Measure.Method(() =>
            {
                _dummy.publicColor = Color.red;
                var actual = accessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() =>
            {
                _dummy.publicColor = Color.red;
                var actual = _dummy.publicColor;
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
#endif

        [Test]
        public void SetFieldFast_InnerClass_Passes()
        {
            var expectedValue = new DummyObject.InnerClass() { str = "Hello" };
            var accessor = Reflect.From<DummyObject>.GetRef<DummyObject.InnerClass>("_innerClass");
            accessor.SetValue(_dummy, expectedValue);
            Assert.AreEqual(expectedValue, _dummy.SubClass);
            Assert.AreEqual("Hello", _dummy.SubClass.str);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        public void SetFieldFast_InnerClass_Performance()
        {
            var expectedValue = new DummyObject.InnerClass() { str = "Hello" };
            var accessor = Reflect.From<DummyObject>.GetRef<DummyObject.InnerClass>("_innerClass");
            Measure.Method(() => { accessor.SetValue(_dummy, expectedValue); }).WarmupCount(10)
                .IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() => { _dummy.SubClass = expectedValue; }).WarmupCount(10).IterationsPerMeasurement(100000)
                .MeasurementCount(20).GC().Run();
        }
#endif

        [Test]
        public void GetFieldFast_InnerClass_Passes()
        {
            var expectedValue = new DummyObject.InnerClass() { str = "Hello" };
            var accessor = Reflect.From<DummyObject>.GetRef<DummyObject.InnerClass>("_innerClass");
            _dummy.SubClass = expectedValue;
            var actualValue = accessor.GetValue(_dummy);
            Assert.AreEqual(expectedValue, actualValue);
            Assert.AreEqual("Hello", actualValue.str);
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        public void GetFieldFast_InnerClass_Performance()
        {
            var expectedValue = new DummyObject.InnerClass() { str = "Hello" };
            var accessor = Reflect.From<DummyObject>.GetRef<DummyObject.InnerClass>("_innerClass");
            Measure.Method(() =>
            {
                _dummy.SubClass = expectedValue;
                var actual = accessor.GetValue(_dummy);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
            Measure.Method(() =>
            {
                _dummy.SubClass = expectedValue;
                var actual = _dummy.SubClass;
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
#endif

        [Test]
        public void IsBoxed_Passes()
        {
            var value = 1;
            Assert.IsFalse(Reflect.IsBoxed(value));
            object boxedValue = value;
            Assert.IsTrue(Reflect.IsBoxed(boxedValue));
        }

#if ENABLE_PERFORMANCE_TESTS
        [Test, Performance]
        public void IsNotBoxed_Performance()
        {
            Measure.Method(() =>
            {
                var value = 1;
                Reflect.IsBoxed(value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }

        [Test, Performance]
        public void IsBoxed_Performance()
        {
            Measure.Method(() =>
            {
                object value = 1;
                Reflect.IsBoxed(value);
            }).WarmupCount(10).IterationsPerMeasurement(100000).MeasurementCount(20).GC().Run();
        }
#endif
    }
}
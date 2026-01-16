using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Postica.BindingSystem.Converters;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = NUnit.Framework.Is;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem {

    public class ConvertersTests
    {
        private DummyObject _dummy;
        private DerivedDummyObject _derivedDummy;

        private class ValueProvider<T> : IValueProvider<T>
        {
            public T Value { get; set; }
            public object UnsafeValue => Value;

            public static implicit operator ValueProvider<T>(T value) => new ValueProvider<T>() { Value = value };
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

        
        #region [  ENUM CONVERTER TESTS  ]
        
        [Test]
        public void EnumConverter_Pure_Conversion_Passes()
        {
            var converterTemplate = new EnumConverter<int>()
            {
                EnumType = typeof(DummyObject.TestEnum),
                choices = new EnumConverter<int>.Choices
                {
                    values = new List<ReadOnlyBind<int>>()
                    {
                        1.Bind(),
                        2.Bind(),
                        3.Bind()
                    },
                    fallback = 0.Bind()
                }
            };

            var converter = converterTemplate.Compile(converterTemplate.EnumType, typeof(int)) as IConverter<DummyObject.TestEnum, int>;
            Assert.IsNotNull(converter, "Converter should not be null");
            Assert.AreEqual(1, converter.Convert(DummyObject.TestEnum.First));
            Assert.AreEqual(2, converter.Convert(DummyObject.TestEnum.Second));
            Assert.AreEqual(3, converter.Convert(DummyObject.TestEnum.Third));
        }
        
        [Test]
        public void EnumConverter_Accessor_Conversion_Passes()
        {
            var converterTemplate = new EnumConverter<float>()
            {
                EnumType = typeof(DummyObject.TestEnum),
                choices = new EnumConverter<float>.Choices
                {
                    values = new List<ReadOnlyBind<float>>()
                    {
                        1.5f.Bind(),
                        2.8f.Bind(),
                        3.2f.Bind()
                    },
                    fallback = 0f.Bind()
                }
            };
            
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.publicEnum), converterTemplate, null);
            Assert.IsNotNull(accessor, "Accessor should not be null");
            
            _dummy.publicEnum = DummyObject.TestEnum.First;
            Assert.AreEqual(1.5f, accessor.GetValue(_dummy));
            
            _dummy.publicEnum = DummyObject.TestEnum.Second;
            Assert.AreEqual(2.8f, accessor.GetValue(_dummy));
            
            _dummy.publicEnum = DummyObject.TestEnum.Third;
            Assert.AreEqual(3.2f, accessor.GetValue(_dummy));
        }
        
        [Test]
        public void EnumConverter_Accessor_NoMemoryAllocations()
        {
            var converterTemplate = new EnumConverter<float>()
            {
                EnumType = typeof(DummyObject.TestEnum),
                choices = new EnumConverter<float>.Choices
                {
                    values = new List<ReadOnlyBind<float>>()
                    {
                        1.5f.Bind(),
                        2.8f.Bind(),
                        3.2f.Bind()
                    },
                    fallback = 0f.Bind()
                }
            };
            
            var accessor = AccessorsFactory.GetAccessor<float>(_dummy, nameof(DummyObject.publicEnum), converterTemplate, null);
            Assert.IsNotNull(accessor, "Accessor should not be null");

            Assert.That(() =>
            {
                _dummy.publicEnum = DummyObject.TestEnum.First;
                var value = accessor.GetValue(_dummy);

                _dummy.publicEnum = DummyObject.TestEnum.Second;
                value = accessor.GetValue(_dummy);

                _dummy.publicEnum = DummyObject.TestEnum.Third;
                value = accessor.GetValue(_dummy);
            }, Is.Not.AllocatingGCMemory());
        }
        
        [Test]
        public void EnumUnityObjectConverter_Pure_Conversion_Passes()
        {
            var testTransform = new GameObject("TestTransform").transform;
            var converterTemplate = new EnumUnityObjectConverter()
            {
                EnumType = typeof(DummyObject.TestEnum),
                choices = new EnumUnityObjectConverter.Choices
                {
                    values = new List<ReadOnlyBind<Object>>()
                    {
                        ((Object)_dummy.transform).Bind(),
                        ((Object)testTransform).Bind(),
                        (null as Object).Bind()
                    },
                    fallback = (null as Object).Bind()
                }
            };

            var converter = converterTemplate.Compile(converterTemplate.EnumType, typeof(Transform)) as IConverter<DummyObject.TestEnum, Transform>;
            Assert.IsNotNull(converter, "Converter should not be null");
            Assert.AreEqual(_dummy.transform, converter.Convert(DummyObject.TestEnum.First));
            Assert.AreEqual(testTransform, converter.Convert(DummyObject.TestEnum.Second));
            Assert.AreEqual(null, converter.Convert(DummyObject.TestEnum.Third));
            
            Object.Destroy(testTransform.gameObject); // Clean up the test transform
        }
        
        [Test]
        public void EnumUnityObjectConverter_Accessor_Conversion_Passes()
        {
            var testTransform = new GameObject("TestTransform").transform;
            var converterTemplate = new EnumUnityObjectConverter()
            {
                EnumType = typeof(DummyObject.TestEnum),
                choices = new EnumUnityObjectConverter.Choices
                {
                    values = new List<ReadOnlyBind<Object>>()
                    {
                        ((Object)_dummy.transform).Bind(),
                        ((Object)testTransform).Bind(),
                        (null as Object).Bind()
                    },
                    fallback = (null as Object).Bind()
                }
            };
            
            var accessor = AccessorsFactory.GetAccessor<Transform>(_dummy, nameof(DummyObject.publicEnum), converterTemplate, null);
            Assert.IsNotNull(accessor, "Accessor should not be null");
            
            _dummy.publicEnum = DummyObject.TestEnum.First;
            Assert.AreEqual(_dummy.transform, accessor.GetValue(_dummy));
            
            _dummy.publicEnum = DummyObject.TestEnum.Second;
            Assert.AreEqual(testTransform, accessor.GetValue(_dummy));
            
            _dummy.publicEnum = DummyObject.TestEnum.Third;
            Assert.AreEqual(null, accessor.GetValue(_dummy));
            
            Object.Destroy(testTransform.gameObject); // Clean up the test transform
        }
        
        [Test]
        public void EnumUnityObjectConverter_Accessor_NoMemoryAllocations()
        {
            var testTransform = new GameObject("TestTransform").transform;
            var converterTemplate = new EnumUnityObjectConverter()
            {
                EnumType = typeof(DummyObject.TestEnum),
                choices = new EnumUnityObjectConverter.Choices
                {
                    values = new List<ReadOnlyBind<Object>>()
                    {
                        ((Object)_dummy.transform).Bind(),
                        ((Object)testTransform).Bind(),
                        (null as Object).Bind()
                    },
                    fallback = (null as Object).Bind()
                }
            };
            
            var accessor = AccessorsFactory.GetAccessor<Transform>(_dummy, nameof(DummyObject.publicEnum), converterTemplate, null);
            Assert.IsNotNull(accessor, "Accessor should not be null");

            Assert.That(() =>
            {
                _dummy.publicEnum = DummyObject.TestEnum.First;
                var value = accessor.GetValue(_dummy);

                _dummy.publicEnum = DummyObject.TestEnum.Second;
                value = accessor.GetValue(_dummy);

                _dummy.publicEnum = DummyObject.TestEnum.Third;
                value = accessor.GetValue(_dummy);
            }, Is.Not.AllocatingGCMemory());
            
            Object.Destroy(testTransform.gameObject); // Clean up the test transform
        }
        #endregion
    }
}
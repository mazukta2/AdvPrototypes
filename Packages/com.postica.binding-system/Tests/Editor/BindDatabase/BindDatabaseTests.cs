using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Postica.BindingSystem.Serialization;
using UnityEngine;
using UnityEngine.TestTools;

namespace Postica.BindingSystem.Tests
{
    public class BindDatabaseTests
    {
        [Serializable]
        public class TypeTest_WithBind
        {
            public Bind<float> vs;
            [SerializeField]
            private Bind<Bla> bla;

            public List<Bind<float>> list = new List<Bind<float>>() { 3f.Bind(), 33f.Bind(), 333f.Bind(), 444f.Bind() };
            public Bind<List<Bind<float>>> listOfFloats;
            public List<Bind<Bla>> binds;

            [Serializable]
            public struct Bla
            {
                public float x;
                public V v;
                public Bind<Vector2> y;
            }

            [Serializable]
            public struct V
            {
                public float x;
                public float y;
            }
        }

        [Serializable]
        public class TypeTest_NoBind
        {
            public float vs;
            [SerializeField]
            public Bla bla;

            public List<float> list = new List<float>() { 3f, 33f, 333f, 444f };
            public Bind<List<float>> listOfFloats;
            public List<Bla> binds;

            [Serializable]
            public struct Bla
            {
                public float x;
                public Vector2 y;
            }
        }

        [Serializable]
        private class TypeJson
        {
            public BindDatabase.BindType type;
        }

        private static string ToJson(BindDatabase.BindType type) => JsonUtility.ToJson(new TypeJson() { type = type }, true);
        private static BindDatabase.BindType FromJson(string json) => JsonUtility.FromJson<TypeJson>(json).type;

        // A Test behaves as an ordinary method
        [Test]
        public void BuildType_NoBind_Passes()
        {
            // Use the Assert class to test conditions
            var guid = "1E1F9D605A154256A2D6646D410A8484";
            var fileId = "12345678901";
            var expectedOutput = @"{
    ""type"": {
        ""type"": ""Postica.BindingSystem.Tests.BindDatabaseTests+TypeTest_NoBind, BindingSystem.Editor.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",
        ""guid"": ""1E1F9D605A154256A2D6646D410A8484"",
        ""localId"": ""12345678901"",
        ""fields"": [
            {
                ""id"": ""vs"",
                ""path"": ""vs"",
                ""depth"": 0,
                ""flags"": 0
            },
            {
                ""id"": ""bla"",
                ""path"": ""bla"",
                ""depth"": 0,
                ""flags"": 4
            },
            {
                ""id"": ""bla.x"",
                ""path"": ""bla.x"",
                ""depth"": 1,
                ""flags"": 0
            },
            {
                ""id"": ""bla.y"",
                ""path"": ""bla.y"",
                ""depth"": 1,
                ""flags"": 0
            },
            {
                ""id"": ""list"",
                ""path"": ""list"",
                ""depth"": 0,
                ""flags"": 2
            },
            {
                ""id"": ""list.Array[#]"",
                ""path"": ""list.Array[#]"",
                ""depth"": 1,
                ""flags"": 0
            },
            {
                ""id"": ""listOfFloats"",
                ""path"": ""listOfFloats._value"",
                ""depth"": 0,
                ""flags"": 3
            },
            {
                ""id"": ""listOfFloats.Array[#]"",
                ""path"": ""listOfFloats._value.Array[#]"",
                ""depth"": 1,
                ""flags"": 0
            },
            {
                ""id"": ""binds"",
                ""path"": ""binds"",
                ""depth"": 0,
                ""flags"": 2
            },
            {
                ""id"": ""binds.Array[#]"",
                ""path"": ""binds.Array[#]"",
                ""depth"": 1,
                ""flags"": 4
            },
            {
                ""id"": ""binds.Array[#].x"",
                ""path"": ""binds.Array[#].x"",
                ""depth"": 2,
                ""flags"": 0
            },
            {
                ""id"": ""binds.Array[#].y"",
                ""path"": ""binds.Array[#].y"",
                ""depth"": 2,
                ""flags"": 0
            }
        ]
    }
}".Replace("\r", "");

            var type = BindDatabase.BuildType(typeof(TypeTest_NoBind), guid, fileId, new Dictionary<Type, List<string>>());
            var output = ToJson(type);

            Debug.Log("Output:\n" + output);
            Assert.IsNotNull(type);
            Assert.AreEqual(expectedOutput, output);
        }

        [Test]
        public void BuildType_WithBind_Passes()
        {
            // Use the Assert class to test conditions
            var guid = "AD1F9D605A154256A2D6646D410A8CCC";
            var fileId = "10987654321";
            var expectedOutput = @"{
    ""type"": {
        ""type"": ""Postica.BindingSystem.Tests.BindDatabaseTests+TypeTest_WithBind, BindingSystem.Editor.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"",
        ""guid"": ""AD1F9D605A154256A2D6646D410A8CCC"",
        ""localId"": ""10987654321"",
        ""fields"": [
            {
                ""id"": ""vs"",
                ""path"": ""vs._value"",
                ""depth"": 0,
                ""flags"": 1
            },
            {
                ""id"": ""bla"",
                ""path"": ""bla._value"",
                ""depth"": 0,
                ""flags"": 1
            },
            {
                ""id"": ""bla.x"",
                ""path"": ""bla._value.x"",
                ""depth"": 1,
                ""flags"": 0
            },
            {
                ""id"": ""bla.v"",
                ""path"": ""bla._value.v"",
                ""depth"": 1,
                ""flags"": 4
            },
            {
                ""id"": ""bla.v.x"",
                ""path"": ""bla._value.v.x"",
                ""depth"": 2,
                ""flags"": 0
            },
            {
                ""id"": ""bla.v.y"",
                ""path"": ""bla._value.v.y"",
                ""depth"": 2,
                ""flags"": 0
            },
            {
                ""id"": ""bla.y"",
                ""path"": ""bla._value.y._value"",
                ""depth"": 1,
                ""flags"": 1
            },
            {
                ""id"": ""list"",
                ""path"": ""list"",
                ""depth"": 0,
                ""flags"": 2
            },
            {
                ""id"": ""list.Array[#]"",
                ""path"": ""list.Array[#]._value"",
                ""depth"": 1,
                ""flags"": 1
            },
            {
                ""id"": ""listOfFloats"",
                ""path"": ""listOfFloats._value"",
                ""depth"": 0,
                ""flags"": 3
            },
            {
                ""id"": ""listOfFloats.Array[#]"",
                ""path"": ""listOfFloats._value.Array[#]._value"",
                ""depth"": 1,
                ""flags"": 1
            },
            {
                ""id"": ""binds"",
                ""path"": ""binds"",
                ""depth"": 0,
                ""flags"": 2
            },
            {
                ""id"": ""binds.Array[#]"",
                ""path"": ""binds.Array[#]._value"",
                ""depth"": 1,
                ""flags"": 1
            },
            {
                ""id"": ""binds.Array[#].x"",
                ""path"": ""binds.Array[#]._value.x"",
                ""depth"": 2,
                ""flags"": 0
            },
            {
                ""id"": ""binds.Array[#].v"",
                ""path"": ""binds.Array[#]._value.v"",
                ""depth"": 2,
                ""flags"": 4
            },
            {
                ""id"": ""binds.Array[#].v.x"",
                ""path"": ""binds.Array[#]._value.v.x"",
                ""depth"": 3,
                ""flags"": 0
            },
            {
                ""id"": ""binds.Array[#].v.y"",
                ""path"": ""binds.Array[#]._value.v.y"",
                ""depth"": 3,
                ""flags"": 0
            },
            {
                ""id"": ""binds.Array[#].y"",
                ""path"": ""binds.Array[#]._value.y._value"",
                ""depth"": 2,
                ""flags"": 1
            }
        ]
    }
}".Replace("\r", "");

            var type = BindDatabase.BuildType(typeof(TypeTest_WithBind), guid, fileId, new Dictionary<Type, List<string>>());
            var output = ToJson(type);

            Debug.Log("Output:\n" + output);
            Assert.IsNotNull(type);
            Assert.AreEqual(expectedOutput, output);
        }
    }
}
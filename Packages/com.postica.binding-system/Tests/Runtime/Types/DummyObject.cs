using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    public class DummyObject : MonoBehaviour
    {
        public enum TestEnum
        {
            First,
            Second,
            Third
        }
        
        [Range(0, 2)]
        public float publicFloat;
        public float publicInt;
        public TestEnum publicEnum;
        public Color publicColor;
        public Vector3 publicVector;
        [SerializeField]
        private int privateInt;
        private bool unserializedBool;

        public bool publicBool;
        public short publicShort;
        public long publicLong;
        public double publicDouble;
        public byte publicByte;

        private int _indexerAmount; 

        public List<float> floats = new List<float>();
        public float[] arrayFloats = new float[4];
        public Dictionary<float, float> dictFloats = new Dictionary<float, float>();
        public float[] ArrayFloats { get => arrayFloats; set => arrayFloats = value; }
        public List<float> Floats { get => floats; set => floats = value; }

        [SerializeField]
        private Vector3 _vec3;

        public Vector3 Vector3 {
            get => _vec3;
            set {
                _vec3 = value;
            }
        }

        public Color Color { get; set; }

        public int this[int addition, int multiplication]
        {
            get => (_indexerAmount + addition) * multiplication;
            set => _indexerAmount = value * multiplication + addition;
        }

        public int IndexerAmount { get => _indexerAmount; set => _indexerAmount = value; }

        public int GetIndexerAmount() => _indexerAmount;
        public int AddIndexerAmount(int addition) => _indexerAmount + addition;
        public void SetIndexerValue(int value, int addition, double multiply = 1) => _indexerAmount = value + addition * (int)multiply;

        [SerializeField]
        private InnerStruct _innerStruct;
        [SerializeField]
        private InnerClass _innerClass;

        public InnerStruct SubStruct {
            get => _innerStruct;
            set => _innerStruct = value;
        }

        public InnerClass SubClass
        {
            get => _innerClass;
            set => _innerClass = value;
        }

        public Transform Transform => transform;

        public virtual float FloatProperty { get; set; }

        [Serializable]
        public struct InnerStruct
        {
            private Vector2 _vec2;

            public Transform transform;
            public string str;
            public Vector2 vec2P { get => _vec2; set => _vec2 = value; }
            public Vector2 vec2 { get; set; }


            public List<float> floats;
            public float[] arrayFloats;


            public Dictionary<float, float> dfloats;

            private int _indexerAmount;

            public int this[int addition, int multiplication]
            {
                get => (_indexerAmount + addition) * multiplication;
                set => _indexerAmount = value * multiplication + addition;
            }

            public int IndexerAmount { get => _indexerAmount; set => _indexerAmount = value; }

            public int GetIndexerAmount() => _indexerAmount;
            public int AddIndexerAmount(int addition) => _indexerAmount + addition;
            public void SetIndexerValue(int value, int addition, double multiply = 1) => _indexerAmount = value + addition * (int)multiply;

        }

        [Serializable]
        public class InnerClass
        {
            public Transform transform;
            public string str;
            public Vector2 vec2 { get; set; }
        }

        private void Awake()
        {
            if(_innerClass == null)
            {
                _innerClass = new InnerClass() { transform = transform };
            }
            if(_innerStruct.transform == null)
            {
                _innerStruct.transform = transform;
            }
        }
    }

    public class DerivedDummyObject : DummyObject
    {
        public override float FloatProperty { get => base.FloatProperty; set => base.FloatProperty = value + 2; }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// This class is a dictionary that can be serialized in Unity.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField][HideInInspector] private List<TKey> keys = new();
        [SerializeField][HideInInspector] private List<TValue> values = new();
        
        [SerializeField] private List<Pair> pairs = new();

        [Serializable]
        private struct Pair
        {
            public TKey Key;
            public TValue Value;
        }
        
        public void OnBeforeSerialize()
        {
            pairs.Clear();
            foreach (var pair in this)
            {
                pairs.Add(new Pair
                {
                    Key = pair.Key,
                    Value = pair.Value
                });
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            foreach (var pair in pairs)
            {
                Add(pair.Key, pair.Value);
            }
            for (int i = 0; i != Math.Min(keys.Count, values.Count); i++)
            {
                Add(keys[i], values[i]);
            }
            keys.Clear();
            values.Clear();
        }
    }
    
    /// <summary>
    /// This class is a dictionary that can be serialized in Unity. The values are serialized by reference.
    /// </summary>
    [Serializable]
    public class SerializedValueDictionary : Dictionary<string, object>, ISerializationCallbackReceiver
    {
        [SerializeReference] private List<Pair> pairs = new();

        [Serializable]
        private abstract class Pair
        {
            public string Key;
            
            public abstract object ValueRaw { get; set; }
        }
        
        [Serializable]
        private class Pair<T> : Pair
        {
            public T Value;
            public override object ValueRaw
            {
                get => Value;
                set => Value = (T)value;
            }
        }
        
        public void OnBeforeSerialize()
        {
            pairs.Clear();
            foreach (var pair in this)
            {
                var pairInstance = pair.Value switch
                {
                    bool => new PairBoolean(),
                    int => new PairInt(),
                    long => new PairLong(),
                    short => new PairShort(),
                    byte => new PairByte(),
                    char => new PairChar(),
                    float => new PairFloat(),
                    double => new PairDouble(),
                    string => new PairString(),
                    UnityEngine.Object => new PairObject(),
                    _ => (Pair)Activator.CreateInstance(typeof(Pair<>).MakeGenericType(pair.Value?.GetType()))
                } ;
                pairInstance.Key = pair.Key;
                pairInstance.ValueRaw = pair.Value;
                pairs.Add(pairInstance);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            foreach (var pair in pairs)
            {
                Add(pair.Key, pair.ValueRaw);
            }
        }
        
        [Serializable] private class PairBoolean : Pair<bool>{}
        [Serializable] private class PairInt : Pair<int>{}
        [Serializable] private class PairLong : Pair<long>{}
        [Serializable] private class PairShort : Pair<short>{}
        [Serializable] private class PairByte : Pair<byte>{}
        [Serializable] private class PairChar : Pair<char>{}
        [Serializable] private class PairFloat : Pair<float>{}
        [Serializable] private class PairDouble : Pair<double>{}
        [Serializable] private class PairString : Pair<string>{}
        [Serializable] private class PairObject : Pair<UnityEngine.Object>{}
        // Do for remaining types as needed
    }
}
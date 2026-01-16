using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// A wrapper for a List that can be serialized in Unity.
    /// </summary>
    /// <remarks>The lists are serialized by default in Unity, but any inherited class from it won't be serialized.
    /// This class is to avoid this issue</remarks>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class SerializedList<T> : IList<T>
    {
        [SerializeField] private List<T> values = new();

        public void Add(T value)
        {
            values.Add(value);
        }

        public void CopyTo(T[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);

        bool ICollection<T>.Remove(T item) => values.Remove(item);

        public int Count => values.Count;
        public bool IsReadOnly => false;

        public bool Remove(T value) => values.Remove(value);
        public void Clear() => values.Clear();
        public bool Contains(T value) => values.Contains(value);
        public IEnumerator<T> GetEnumerator() => values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

        public int IndexOf(T item) => values.IndexOf(item);

        public void Insert(int index, T item) => values.Insert(index, item);

        public void RemoveAt(int index) => values.RemoveAt(index);
        public int RemoveAll(Predicate<T> match) => values.RemoveAll(match);

        public T this[int index]
        {
            get => values[index];
            set => values[index] = value;
        }
    }
}
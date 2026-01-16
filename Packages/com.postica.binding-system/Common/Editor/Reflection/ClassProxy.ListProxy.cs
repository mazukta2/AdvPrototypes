using System.Collections;
using System.Collections.Generic;

namespace Postica.Common.Reflection
{
    public partial class ClassProxy
    {
        public class ListProxy<S> : ClassProxy<IList>, IList<S> where S : ClassProxy
        {
            // Implement all methods of List<T> here
            public void Add(S item)
            {
                Instance.Add(item.Instance);
            }
            
            public void Remove(S item)
            {
                Instance.Remove(item.Instance);
            }

            public int Count => Instance.Count;
            public bool IsReadOnly => Instance.IsReadOnly;

            public void Clear()
            {
                Instance.Clear();
            }
            
            public bool Contains(S item)
            {
                return Instance.Contains(item.Instance);
            }
            
            public void CopyTo(S[] array, int arrayIndex)
            {
                for (int i = 0; i < Instance.Count; i++)
                {
                    array[i] = To<S>(Instance[i]);
                }
            }

            bool ICollection<S>.Remove(S item)
            {
                if (!Instance.Contains(item.Instance)) return false;
                
                Instance.Remove(item.Instance);
                return true;
            }

            public int IndexOf(S item)
            {
                return Instance.IndexOf(item.Instance);
            }
            
            public void Insert(int index, S item)
            {
                Instance.Insert(index, item.Instance);
            }

            public void RemoveAt(int index)
            {
                Instance.RemoveAt(index);
            }

            public S this[int index]
            {
                get => To<S>(Instance[index]);
                set => Instance[index] = value.Instance;
            }

            public IEnumerator<S> GetEnumerator()
            {
                return new Enumerator(Instance);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
            
            private class Enumerator : IEnumerator<S>
            {
                private IEnumerator _enumerator;
                
                public Enumerator(IList list)
                {
                    _enumerator = list.GetEnumerator();
                }
                
                public bool MoveNext()
                {
                    return _enumerator.MoveNext();
                }

                public void Reset()
                {
                    _enumerator.Reset();
                }

                public S Current => To<S>(_enumerator.Current);

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    _enumerator = null;
                }
            }
        }
    }
}
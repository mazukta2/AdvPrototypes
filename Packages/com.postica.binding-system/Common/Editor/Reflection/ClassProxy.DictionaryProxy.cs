using System;
using System.Collections;
using System.Collections.Generic;

namespace Postica.Common.Reflection
{
    public partial class ClassProxy
    {
        public class DictionaryProxy<SKey, SValue> : ClassProxy<IDictionary>, IDictionary<SKey, SValue>
        {
            public IEnumerator<KeyValuePair<SKey, SValue>> GetEnumerator()
            {
                return new Enumerator(Instance);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(KeyValuePair<SKey, SValue> item)
            {
                Instance.Add(From<SKey>(item.Key), From<SValue>(item.Value));
            }

            public void Clear()
            {
                Instance.Clear();
            }

            public bool Contains(KeyValuePair<SKey, SValue> item)
            {
                return Instance.Contains(FromPair(item));
            }

            public void CopyTo(KeyValuePair<SKey, SValue>[] array, int arrayIndex)
            {
                var array2 = new KeyValuePair<object, object>[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    array2[i] = FromPair(array[i]);
                }
                
                Instance.CopyTo(array2, arrayIndex);
            }

            public bool Remove(KeyValuePair<SKey, SValue> item)
            {
                Instance.Remove(FromPair(item));
                return true;
            }
            
            private static KeyValuePair<object, object> FromPair(KeyValuePair<SKey, SValue> pair)
            {
                return new KeyValuePair<object, object>(From<SKey>(pair.Key), From<SValue>(pair.Value));
            }
            
            private static KeyValuePair<SKey, SValue> ToPair(KeyValuePair<SKey, SValue> pair)
            {
                return new KeyValuePair<SKey, SValue>(To<SKey>(pair.Key), To<SValue>(pair.Value));
            }
            
            private static KeyValuePair<SKey, SValue> ToPair(object pair)
            {
                var specialPair = (KeyValuePair<object, object>) pair;
                return new KeyValuePair<SKey, SValue>(To<SKey>(specialPair.Key), To<SValue>(specialPair.Value));
            }

            public int Count => Instance.Count;
            public bool IsReadOnly => Instance.IsReadOnly;
            
            public void Add(SKey key, SValue value)
            {
                Instance.Add(From<SKey>(key), From<SValue>(value));
            }

            public bool ContainsKey(SKey key)
            {
                return Instance.Contains(From<SKey>(key));
            }

            public bool Remove(SKey key)
            {
                var key2 = From<SKey>(key);
                if (!Instance.Contains(key2)) return false;
                Instance.Remove(key2);
                return true;
            }

            public bool TryGetValue(SKey key, out SValue value)
            {
                if (Instance.Contains(From<SKey>(key)))
                {
                    value = this[key];
                    return true;
                }

                value = default;
                return false;
            }

            public SValue this[SKey key]
            {
                get => To<SValue>(Instance[From<SKey>(key)]);
                set => Instance[From<SKey>(key)] = From<SValue>(value);
            }

            public ICollection<SKey> Keys
            {
                get
                {
                    var list = new List<SKey>();
                    foreach (var key in Instance.Keys)
                    {
                        list.Add(To<SKey>(key));
                    }

                    return list;
                }
            }

            public ICollection<SValue> Values
            {
                get
                {
                    var list = new List<SValue>();
                    foreach (var value in Instance.Values)
                    {
                        list.Add(To<SValue>(value));
                    }

                    return list;
                }
            }

            private class Enumerator : IEnumerator<KeyValuePair<SKey, SValue>>
            {
                private IEnumerator _enumerator;
                
                public Enumerator(IDictionary dictionary)
                {
                    _enumerator = dictionary.GetEnumerator();
                }

                public bool MoveNext() => _enumerator.MoveNext();

                public void Reset() => _enumerator.Reset();

                public KeyValuePair<SKey, SValue> Current => ToPair(_enumerator.Current);

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    if (_enumerator is IDisposable e)
                    {
                        e.Dispose();
                    }
                }
            }
        }
    }
}
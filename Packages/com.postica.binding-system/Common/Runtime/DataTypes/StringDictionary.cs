using System.Collections.Generic;

namespace Postica.Common
{
    /// <summary>
    /// A dictionary that uses string keys and is case-sensitive. <br/>
    /// This dictionary has an increased performance compared to the default dictionary when using string keys.
    /// </summary>
    /// <typeparam name="V">The type of the value</typeparam>
    internal class StringDictionary<V> : Dictionary<string, V>
    {
        private class FastStringComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => string.Equals(x, y, System.StringComparison.Ordinal);

            public int GetHashCode(string obj) => obj.GetHashCode();
        }

        public StringDictionary() : base(new FastStringComparer())
        {
        }

        public StringDictionary(IDictionary<string, V> dictionary) : base(dictionary, new FastStringComparer())
        {
        }

        public StringDictionary(int capacity) : base(capacity, new FastStringComparer())
        {
        }

        public StringDictionary(IEnumerable<KeyValuePair<string, V>> collection) : base(collection, new FastStringComparer())
        {
        }
    }
}

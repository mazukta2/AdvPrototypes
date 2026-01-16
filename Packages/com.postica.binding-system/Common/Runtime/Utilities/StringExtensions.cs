using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// Class containing extension methods for strings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// A utility wrapper struct to format strings with rich text tags.
        /// </summary>
        public struct StringRichTextFormatter
        {
            private bool? _isBold;
            private bool? _isItalic;
            private Color? _color;
            private float? _size;
            private readonly bool _ignoreRichText;

            public readonly string original;

            public StringRichTextFormatter(string original)
            {
                this.original = original;
                _isBold = null;
                _isItalic = null;
                _color = null;
                _size = null;
                _ignoreRichText = false;
            }
            
            internal StringRichTextFormatter(string original, bool ignoreRichText) : this(original)
            {
                _ignoreRichText = ignoreRichText;
            }

            /// <summary>
            /// Make the text bold.
            /// </summary>
            /// <returns></returns>
            public StringRichTextFormatter Bold()
            {
                _isBold = true;
                return this;
            }

            /// <summary>
            /// Make the text italic.
            /// </summary>
            /// <returns></returns>
            public StringRichTextFormatter Italic()
            {
                _isItalic = true;
                return this;
            }

            /// <summary>
            /// Change the color of the text.
            /// </summary>
            /// <param name="color"></param>
            /// <returns></returns>
            public StringRichTextFormatter Color(Color color)
            {
                _color = color;
                return this;
            }

            /// <summary>
            /// Change the color of the text.
            /// </summary>
            /// <param name="htmlColor">The color in HTML format: e.g. #aa55ff</param>
            /// <returns></returns>
            public StringRichTextFormatter Color(string htmlColor)
            {
                _color = UnityEngine.ColorUtility.TryParseHtmlString(htmlColor, out var color) ? color : null;
                return this;
            }

            /// <summary>
            /// Change the size of the text.
            /// </summary>
            /// <param name="size"></param>
            /// <returns></returns>
            public StringRichTextFormatter Size(float size)
            {
                _size = size;
                return this;
            }

            /// <summary>
            /// Converts the formatter to a string.
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                if (_ignoreRichText)
                {
                    return original;
                }
                
                var sb = new StringBuilder();
                if (_size.HasValue)
                {
                    sb.Append($"<size={_size.Value}>");
                }

                if (_color.HasValue)
                {
                    sb.Append($"<color=#{UnityEngine.ColorUtility.ToHtmlStringRGBA(_color.Value)}>");
                }

                if (_isBold == true)
                {
                    sb.Append("<b>");
                }

                if (_isItalic == true)
                {
                    sb.Append("<i>");
                }

                sb.Append(original);
                if (_isItalic == true)
                {
                    sb.Append("</i>");
                }

                if (_isBold == true)
                {
                    sb.Append("</b>");
                }

                if (_color.HasValue)
                {
                    sb.Append("</color>");
                }

                if (_size.HasValue)
                {
                    sb.Append("</size>");
                }

                return sb.ToString();
            }

            public static implicit operator string(StringRichTextFormatter stringFormat)
            {
                return stringFormat.ToString();
            }

            public static implicit operator StringRichTextFormatter(string str)
            {
                return new StringRichTextFormatter(str);
            }
        }

        /// <summary>
        /// Format a string with rich text tags.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static StringRichTextFormatter AsRichText(this string str)
        {
#if UNITY_2022_3_OR_NEWER
            return new StringRichTextFormatter(str);
#else
            return new StringRichTextFormatter(str, true);
#endif
        }
        
        /// <summary>
        /// Format a string with rich text tags.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static StringRichTextFormatter RT(this string str)
        {
            return new StringRichTextFormatter(str);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string NiceName(this string name) => StringUtility.NicifyName(name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool FastEquals(this string a, string other) => string.Equals(a, other, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string FastReplace(this string a, string oldValue, string newValue) =>
            a.Replace(oldValue, newValue, StringComparison.Ordinal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string ReplaceAtStart(this string a, string oldValue, string newValue)
        {
            var index = a.IndexOf(oldValue, StringComparison.Ordinal);
            if (index < 0)
            {
                return a;
            }

            return newValue + a.Remove(0, oldValue.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string ReplaceAtEnd(this string a, string oldValue, string newValue)
        {
            var index = a.LastIndexOf(oldValue, StringComparison.Ordinal);
            if (index < 0)
            {
                return a;
            }

            return a.Remove(index, oldValue.Length) + newValue;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string RemoveAtStart(this string a, string value)
        {
            var index = a.IndexOf(value, StringComparison.Ordinal);
            return index != 0 ? a : a.Remove(0, value.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string RemoveAtEnd(this string a, string value)
        {
            var index = a.LastIndexOf(value, StringComparison.Ordinal);
            if (index < 0)
            {
                return a;
            }

            return a.Remove(index, value.Length);
        }

        internal static string IfNullOrEmpty(this string s, string alternative)
        {
            return string.IsNullOrEmpty(s) ? alternative : s;
        }

        /// <summary>
        /// Convert a hexadecimal string to a byte array
        /// </summary>
        public static byte[] ToByteArray(this string hexString)
        {
            byte[] retval = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                retval[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }

            return retval;
        }

        internal static int IndexOf(this StringBuilder sb, string value, int startIndex, int searchLength = -1,
            bool ignoreCase = false)
        {
            int index;
            int length = value.Length;
            int maxSearchLength = searchLength < 0
                ? (sb.Length - length) + 1
                : Min(startIndex + searchLength, (sb.Length - length) + 1);

            if (ignoreCase)
            {
                for (int i = startIndex; i < maxSearchLength; ++i)
                {
                    if (char.ToLower(sb[i]) == char.ToLower(value[0]))
                    {
                        index = 1;
                        while ((index < length) && (char.ToLower(sb[i + index]) == char.ToLower(value[index])))
                        {
                            ++index;
                        }

                        if (index == length)
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }

            for (int i = startIndex; i < maxSearchLength; ++i)
            {
                if (sb[i] == value[0])
                {
                    index = 1;
                    while ((index < length) && (sb[i + index] == value[index]))
                    {
                        ++index;
                    }

                    if (index == length)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Min(int a, int b) => a < b ? a : b;

        /// <summary>
        /// Compute the similarity between two strings, based on the Levenshtein distance.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static int SimilarityDistance(this string source, string target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            int n = source.Length;
            int m = target.Length;

            if (n == 0) return m;
            if (m == 0) return n;

            int[][] distance = new int[n + 1][];
            for (int index = 0; index < n + 1; index++)
            {
                distance[index] = new int[m + 1];
            }

            for (int i = 0; i <= n; i++)
                distance[i][0] = i;
            for (int j = 0; j <= m; j++)
                distance[0][j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    distance[i][j] = Mathf.Min(
                        Mathf.Min(distance[i - 1][j] + 1, distance[i][j - 1] + 1),
                        distance[i - 1][j - 1] + cost);
                }
            }

            return distance[n][m];
        }
        
        /// <summary>
        /// Computes the similarity between two strings, based on the Levenshtein distance, with an additional check for transpositions.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static int AdvancedSimilarityDistance(this string source, string target)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            int n = source.Length;
            int m = target.Length;

            if (n == 0) return m;
            if (m == 0) return n;

            int[,] distance = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
                distance[i, 0] = i;
            for (int j = 0; j <= m; j++)
                distance[0, j] = j;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);

                    if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                    {
                        distance[i, j] = Math.Min(distance[i, j], distance[i - 2, j - 2] + cost);
                    }
                }
            }

            return distance[n, m];
        }
    }

    internal static class StringUtility
    {
        public static string NicifyName(string name)
        {
            if (string.IsNullOrEmpty(name)) { return string.Empty; }
            if (name.StartsWith("m_"))
            {
                name = name.Substring(2);
            }
            else if (name.StartsWith("_"))
            {
                name = name.TrimStart('_');
            }
            return name.Substring(0, 1).ToUpperInvariant() + SplitUpperLetters(name.Substring(1));
        }

        private static string SplitUpperLetters(string name)
        {
            if (name.Length == 0) { return string.Empty; }
            StringBuilder sb = new StringBuilder();
            sb.Append(name[0]);
            bool wasLower = !((name[0] >= 'A' && name[0] <= 'Z') || (name[0] >= '0' && name[0] <= '9'));
            int squareBrackets = 0;
            int roundBrackets = 0;
            for (int i = 1; i < name.Length; i++)
            {
                if (name[i] == '[')
                {
                    squareBrackets++;
                }
                else if (name[i] == ']')
                {
                    squareBrackets--;
                }
                else if (name[i] == '(')
                {
                    roundBrackets++;
                }
                else if (name[i] == ')')
                {
                    roundBrackets--;
                }
                if (squareBrackets <= 0 && roundBrackets <= 0)
                {
                    bool isLower = true;
                    if ((name[i] >= 'A' && name[i] <= 'Z') || (name[i] >= '0' && name[i] <= '9'))
                    {
                        isLower = false;
                        if (wasLower)
                        {
                            sb.Append(' ');
                        }
                    }
                    wasLower = isLower;
                }
                sb.Append(name[i]);
            }
            return sb.ToString();
        }
    }
}

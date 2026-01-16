using System;

namespace Postica.Common
{
    public readonly struct SearchTags
    {
        public readonly string[] tags;
        private readonly string[] _trimmedTags;

        public SearchTags(params string[] tags)
        {
            this.tags = tags ?? new string[tags.Length];
            _trimmedTags = new string[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                this.tags[i] = tags[i] ?? string.Empty;
                _trimmedTags[i] = this.tags[i].Replace(" ", "", StringComparison.Ordinal)
                                              .Replace("_", "", StringComparison.Ordinal)
                                              .Replace("-", "", StringComparison.Ordinal);
            }
        }

        internal bool Contains(string value)
        {
            if(tags.Length == 0)
            {
                return false;
            }
            if(tags.Length == 1)
            {
                return tags[0].Contains(value, StringComparison.OrdinalIgnoreCase) || _trimmedTags[0].Contains(value, StringComparison.OrdinalIgnoreCase);
            }
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i].Contains(value, StringComparison.OrdinalIgnoreCase) || _trimmedTags[i].Contains(value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public float SimilarityDistance(string value)
        {
            var maxDistance = float.MaxValue;
            for (int i = 0; i < tags.Length; i++)
            {
                var distance = tags[i].SimilarityDistance(value);
                if (distance < maxDistance)
                {
                    maxDistance = distance;
                }
                distance = _trimmedTags[i].SimilarityDistance(value);
                if (distance < maxDistance)
                {
                    maxDistance = distance;
                }
            }
            return maxDistance;
        }

        private static readonly SearchTags _empty = new SearchTags(Array.Empty<string>());
        public static SearchTags None => _empty;

        public static implicit operator SearchTags(string[] tags) => new SearchTags(tags);
        public static implicit operator SearchTags(string tag) => new SearchTags(tag);
        public static implicit operator SearchTags(int tag) => new SearchTags(tag.ToString());
        public static implicit operator SearchTags(float tag) => new SearchTags(tag.ToString());
        public static implicit operator SearchTags(bool tag) => new SearchTags(tag.ToString());
        public static implicit operator SearchTags(Enum tag) => new SearchTags(tag.ToString());
        public static implicit operator SearchTags(UnityEngine.Object tag) => new SearchTags(tag.name);
    }
}
using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier that trims a string.
    /// </summary>
    public class TrimModifier : BaseModifier<string>
    {
        private static readonly char[] _defaultTrimChars = { ' ' };

        public enum Type
        {
            StartAndEnd,
            Start,
            End,
        }

        [SerializeField]
        [Tooltip("The characters to trim")]
        private Bind<string> _characters;
        [SerializeField]
        [Tooltip("How to trim the string")]
        private Type _at;

        public override string ShortDataDescription => _characters.ToString("chars") + " at " + _at;

        protected override string Modify(string value)
        {
            var chars = string.IsNullOrEmpty(_characters) ? _defaultTrimChars : _characters.Value.ToCharArray();
            return _at switch
            {
                Type.StartAndEnd => value.Trim(chars),
                Type.Start => value.TrimStart(chars),
                Type.End => value.TrimEnd(chars),
                _ => value.Trim(chars),
            };
        }
    }
}
using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier that returns a substring of a string.
    /// </summary>
    [Serializable]
    [OneLineModifier]
    [HideMember]
    [TypeDescription("Returns a substring of the string.")]
    public class SubstringModifier : BaseModifier<string>, IDynamicComponent
    {
        [SerializeField]
        [Tooltip("The index of the first character of the substring.")]
        private Bind<int> _startIndex;
        [SerializeField]
        [Tooltip("The length of the substring. If length is 0, the substring will go to the end of the string. " +
            "Negative values are accepted and will count the number of characters from the end")]
        private Bind<int> _length;
        
        private string _prevInput;
        private string _cacheOutput;

        public override string ShortDataDescription => $"({_startIndex.ToString("x")}, {_length.ToString("y")})";

        protected override string Modify(string value)
        {
            if(_startIndex.IsBound || _length.IsBound)
            {
                // Do not cache if any of the parameters is bound, because it can change
                return ModifyPure(value);
            }
            
            if (_prevInput?.Equals(value, StringComparison.Ordinal) == true)
            {
                return _cacheOutput;
            }
            _prevInput = value;
            return _cacheOutput =  ModifyPure(value);
        }

        private string ModifyPure(string value)
        {
            var length = _length.Value;
            return length switch
            {
                0 => value[_startIndex.Value..],
                < 0 => value[_startIndex.Value..^(-length)],
                _ => value.Substring(_startIndex.Value, length),
            };
        }

        public bool IsDynamic => _startIndex.IsBound || _length.IsBound;
    }
}
using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier that changes the casing of a string.
    /// </summary>
    [OneLineModifier]
    [HideMember]
    [TypeDescription("Changes the casing of the string.")]
    public class StringCasingModifier : BaseModifier<string>
    {
        public enum Casing
        {
            ToLower,
            ToLowerInvariant,
            ToUpper,
            ToUpperInvariant,
        }

        [SerializeField]
        [Tooltip("What casing to use")]
        private Casing _casing;
        
        private string _prevInput;
        private string _cacheOutput;

        public override string Id => "Change Casing";
        public override string ShortDataDescription => _casing.ToString();

        protected override string Modify(string value)
        {
            if(_prevInput?.Equals(value, StringComparison.Ordinal) == true)
            {
                return _cacheOutput;
            }
            _prevInput = value;
            
            return _cacheOutput = _casing switch
            {
                Casing.ToLower => value.ToLower(),
                Casing.ToLowerInvariant => value.ToLowerInvariant(),
                Casing.ToUpper => value.ToUpper(),
                Casing.ToUpperInvariant => value.ToUpperInvariant(),
                _ => value
            };
        }
    }
}
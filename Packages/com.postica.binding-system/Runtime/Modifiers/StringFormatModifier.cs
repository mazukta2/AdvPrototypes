using System;
using System.Text.RegularExpressions;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// Formats the input string.
    /// </summary>
    [Serializable]
    [HideMember]
    [OneLineModifier]
    [TypeDescription("Formats the input string.")]
    public sealed class StringFormatModifier : BaseModifier<string>, IDynamicComponent
    {
        [SerializeField]
        [HideInInspector]
        private string _format = "{0}";
        
        [Bind]
        [Multiline(3)]
        public ReadOnlyBind<string> format = "{0}".Bind();

        private Regex _reverseFormatRegex;
        
        private string _prevInput;
        [NonSerialized]
        private string _cacheOutput;

        ///<inheritdoc/>
        public override string ShortDataDescription
        {
            get
            {
                if(_format != "{0}" && format.Value == "{0}")
                {
                    format.UnboundValue = _format;
                }

                var formatValue = format.Value;
                return formatValue == "{0}" ? "Not Set".RT().Bold().Color(BindColors.Primary) : $"[{formatValue}]";
            }
        }

        protected override string Modify(string value)
        {
            if (format.IsBound)
            {
                // Do not cache if format is bound, because it can change
                return string.IsNullOrEmpty(format) ? value : string.Format(format, value);
            }
            
            if (_prevInput?.Equals(value, StringComparison.Ordinal) == true && _cacheOutput != null)
            {
                return _cacheOutput;
            }
            _prevInput = value;
            return _cacheOutput = string.IsNullOrEmpty(format) ? value : string.Format(format, value);
        }

        protected override string InverseModify(string output)
        {
            if (!format.IsBound && _cacheOutput?.Equals(output, StringComparison.Ordinal) == true)
            {
                return _prevInput;
            }
            
            if(_reverseFormatRegex == null)
            {
                var pattern = GetPattern(format);
                _reverseFormatRegex = new Regex(pattern);
            }

            var match = _reverseFormatRegex.Match(output);
            return match.Success ? match.Groups[1].Value : output;
        }

        private static string GetPattern(string format)
        {
            return Regex.Replace(format, @"\{0.*\}", "(.*)");
        }

        public bool IsDynamic => format.IsBound;
    }
}
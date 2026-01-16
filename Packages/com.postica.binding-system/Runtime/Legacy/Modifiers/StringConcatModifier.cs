using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Concatenates the input string with the other parameter strings.
    /// </summary>
    [Serializable]
    [HideMember]
    [Obsolete("Use the same name modifier from Postica.BindingSystem.Runtime.dll")]
    internal sealed class StringConcatModifier : BaseModifier<string>
    {
        [SerializeField]
        private ReadOnlyBind<int> _inputIndex;
        [SerializeField]
        private ReadOnlyBind<string>[] _pieces = Array.Empty<ReadOnlyBind<string>>();

        private StringBuilder _descriptionSb;
        private StringBuilder _sb;
        private string _prevInput;

        ///<inheritdoc/>
        public override string ShortDataDescription 
        {
            get
            {
                if(_descriptionSb == null)
                {
                    _descriptionSb = new StringBuilder();
                }
                else
                {
                    _descriptionSb.Clear();
                }
                if(_inputIndex.IsBound)
                {
                    _descriptionSb.Append("(").Append(_pieces.Length).Append(" pieces)");
                    return _descriptionSb.ToString();
                }
                _descriptionSb.Append('(');
                var mainIndex = _inputIndex.Value;
                if(_pieces.Length == 0)
                {
                    return string.Empty;
                }
                for (int i = 0; i < _pieces.Length; i++)
                {
                    if(i == mainIndex)
                    {
                        _descriptionSb.Append(VarFormat("x")).Append(" + ");
                    }
                    if (_pieces[i].IsBound)
                    {
                        _descriptionSb.Append(VarFormat($"p{i}")).Append(" + ");
                    }
                    else
                    {
                        _descriptionSb.Append('\'').Append(_pieces[i].Value).Append('\'').Append(" + ");
                    }

                    if(_descriptionSb.Length > 100)
                    {
                        _descriptionSb.Append("...");
                        return _descriptionSb.ToString();
                    }
                }

                if (mainIndex >= _pieces.Length)
                {
                    _descriptionSb.Append(VarFormat("x"));
                }
                else
                {
                    _descriptionSb.Length -= " + ".Length;
                }

                _descriptionSb.Append(')');
                return _descriptionSb.ToString();
            }
        }

        protected override string Modify(string value)
        {
            _prevInput = value;

            if(_sb == null)
            {
                _sb = new StringBuilder();
            }
            else
            {
                _sb.Clear();
            }

            var mainIndex = _inputIndex.Value;
            for (int i = 0; i < _pieces.Length; i++)
            {
                if (i == mainIndex)
                {
                    _sb.Append(value);
                }
                _sb.Append(_pieces[i].Value);
            }

            if (mainIndex >= _pieces.Length)
            {
                _sb.Append(value);
            }

            return _sb.ToString();
        }

        protected override string InverseModify(string output)
        {
            if (output is null)
            {
                return null;
            }

            var str = output;

            var mainIndex = _inputIndex.Value;

            if (mainIndex >= _pieces.Length)
            {
                str = TrimEnd(str, _prevInput);
            }

            for (int i = _pieces.Length - 1; i >= 0; i--)
            {
                if (i == mainIndex && _prevInput != null && str.EndsWith(_prevInput))
                {
                    str = TrimEnd(str, _prevInput);
                }
                if (str.EndsWith(_pieces[i]))
                {
                    str = TrimEnd(str, _pieces[i].Value);
                }
                else
                {
                    return output;
                }
            }

            return str;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string TrimEnd(string str, string end) => str.Substring(0, str.Length - end.Length);
    }
}
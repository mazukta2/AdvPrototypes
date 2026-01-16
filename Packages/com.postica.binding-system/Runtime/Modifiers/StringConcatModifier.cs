using System;
using System.Runtime.CompilerServices;
using System.Text;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Modifiers
{
    /// <summary>
    /// Concatenates the input string with the other parameter strings.
    /// </summary>
    [Serializable]
    [HideMember]
    [TypeDescription("Concatenates the input string with the other parameter strings.")]
    public sealed class StringConcatModifier : BaseModifier<string>, IDynamicComponent
    {
        [SerializeField]
        private ReadOnlyBind<int> _inputIndex;
        [SerializeField]
        private ReadOnlyBind<string>[] _pieces = Array.Empty<ReadOnlyBind<string>>();

        private StringBuilder _descriptionSb;
        private StringBuilder _sb;
        private bool? _canCache;
        private string _prevInput;
        private string _cacheOutput;

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
            InitializeCaching();
            
            if (_canCache == true && _prevInput?.Equals(value, StringComparison.Ordinal) == true)
            {
                return _cacheOutput;
            }
            
            _prevInput = value;

            return _canCache == true ? _cacheOutput = ModifyPure(value) : ModifyPure(value);
        }

        private void InitializeCaching()
        {
            if (_canCache == null)
            {
                _canCache = !_inputIndex.IsBound;
                if (_canCache == true)
                {
                    foreach (var piece in _pieces)
                    {
                        if (piece.IsBound)
                        {
                            _canCache = false;
                            break;
                        }
                    }
                }
            }
        }

        private string ModifyPure(string value)
        {
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
            
            InitializeCaching();
            if (_canCache == true && _cacheOutput?.Equals(output, StringComparison.Ordinal) == true)
            {
                return _prevInput;
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

        public bool IsDynamic => _inputIndex.IsBound || Array.Exists(_pieces, p => p.IsBound);
    }
}
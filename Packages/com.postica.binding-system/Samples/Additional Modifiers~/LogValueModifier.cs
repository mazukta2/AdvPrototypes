using System;
using UnityEngine;
using Postica.Common;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A value modifier that logs the value to the console.
    /// </summary>
    [OneLineModifier]
    public class LogValueModifier : IReadWriteModifier<object>
    {
        [SerializeField]
        [Tooltip("The format to use to log the value.")]
        private Bind<string> _format = "Value is {0}".Bind();

        public string ShortDataDescription => $"with format " + PrintBindValue();

        public string Id => "Log Value";

        public BindMode ModifyMode => BindMode.ReadWrite;

        private string PrintBindValue()
        {
            return !_format.IsBound ? _format.Value : _format.ToString().AsRichText().Bold().Color(BindColors.Primary);
        }

        public object Modify(BindMode mode, object value)
        {
            Debug.LogFormat(_format, value);
            return value;
        }

        public object ModifyRead(in object value) => Modify(BindMode.Read, value);

        public object ModifyWrite(in object value) => Modify(BindMode.Write, value);
    }
}
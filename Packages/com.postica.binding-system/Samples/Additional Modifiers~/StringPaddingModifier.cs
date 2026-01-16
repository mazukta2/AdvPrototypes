using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier that adds padding to a string.
    /// </summary>
    public class StringPadding : BaseModifier<string>
    {
        public enum Side
        {
            Left,
            Right,
            BothSides,
        }

        [SerializeField]
        [Tooltip("The number of characters to pad")]
        private Bind<int> _amount;
        [SerializeField]
        [Tooltip("The character to pad")]
        private Bind<char> _character;
        [SerializeField]
        [Tooltip("The side to pad")]
        private Side _side;

        public override string Id => "Add Padding";

        public override string ShortDataDescription => $"{_side} {_amount.ToString("n")} '{_character.ToString("x")}'";

        protected override string Modify(string value)
        {
            return _side switch
            {
                Side.Left => value.PadLeft(_amount, _character),
                Side.Right => value.PadRight(_amount, _character),
                Side.BothSides => value.PadLeft(_amount, _character).PadRight(_amount, _character),
                _ => value
            };
        }
    }
}
using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier that rounds a float to the nearest integer.
    /// </summary>
    public class RoundFloorModifier : NumericModifier
    {
        public override string Id => "Round";

        protected override double Modify(double value)
        {
            return Mathf.Round((float)value);
        }
    }
}
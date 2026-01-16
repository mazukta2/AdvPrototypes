using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier that rounds a float to the nearest higher integer.
    /// </summary>
    public class CeilFloatModifier : NumericModifier
    {
        public override string Id => "Ceil";
        protected override double Modify(double value)
        {
            return Mathf.Ceil((float)value);
        }
    }
}
using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// A modifier that rounds a float to the nearest lower integer.
    /// </summary>
    public class FloorFloatModifier : NumericModifier
    {
        public override string Id => "Floor";
        protected override double Modify(double value)
        {
            return Mathf.Floor((float)value);
        }
    }
}
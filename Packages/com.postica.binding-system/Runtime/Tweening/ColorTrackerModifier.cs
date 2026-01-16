using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class ColorDampedTrackerModifier : DampedTrackerModifier<Color>
    {
        protected override Color Add(Color a, Color b)
        {
            return a + b;
        }

        protected override Color Subtract(Color a, Color b)
        {
            return a - b;
        }

        protected override Color Multiply(Color a, float b)
        {
            return a * b;
        }
    }
}
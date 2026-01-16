using System;
using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem.Tweening
{
    [Serializable]
    [HideMember]
    [OneLineModifier]
    public class ColorEaseModifier : EaseModifier<Color>
    {
        protected override Color Lerp(Color from, Color to, float progress)
        {
            return Color.LerpUnclamped(from, to, progress);
        }
    }
}
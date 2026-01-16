using System;
using System.Linq;
using System.Reflection;
using Postica.Common;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Converters
{
    /// <summary>
    /// A converter which converts from a <see cref="Gradient"/> to a <see cref="Color"/> at a specific time.
    /// </summary>
    [Serializable]
    [HideMember]
    public class GradientToColorConverter : IConverter<Gradient, Color>, IDynamicComponent
    {
        [Tooltip("A value between 0 and 1 representing the position on the gradient to get the color from.")]
        [Bind]
        [Range(0, 1)]
        public ReadOnlyBind<float> samplePoint = 0f.Bind();
        
        public string Id => "Gradient To Color";
        public string Description => "Converts a Gradient to a Color";
        public bool IsSafe => true;
        public Color Convert(Gradient value)
        {
            var normalizedIndex = Mathf.Clamp01(samplePoint.Value);
            return value.Evaluate(normalizedIndex);
        }

        public bool IsDynamic => samplePoint.IsBound;
    }
}
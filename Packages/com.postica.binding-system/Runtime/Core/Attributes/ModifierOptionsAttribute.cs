using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Stores options for the modifier, required for drawing the modifier in the inspector
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class ModifierOptionsAttribute : PropertyAttribute
    {
        /// <summary>
        /// When to engage the modifier
        /// </summary>
        public BindMode? ModifierMode { get; set; }
        
        /// <summary>
        /// If true, other similar modifiers for base types will be allowed
        /// </summary>
        public bool AllowSimilarTypes { get; set; } = false;

        /// <summary>
        /// Allow this modifier for derived types
        /// </summary>
        public bool AllowForDerivedTypes { get; set; } = true;

        
        /// <summary>
        /// Stores options for the modifier, required for drawing the modifier in the inspector
        /// </summary>
        /// <param name="modifierMode"></param>
        public ModifierOptionsAttribute(BindMode modifierMode)
        {
            ModifierMode = modifierMode;
        }
        
        /// <summary>
        /// Stores options for the modifier, required for drawing the modifier in the inspector
        /// </summary>
        public ModifierOptionsAttribute()
        {
            ModifierMode = BindMode.ReadWrite;
        }
    }
}

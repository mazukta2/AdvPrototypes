using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Marks the field as bound, and forces a <see cref="BindMode"/> on that field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BindAttribute : PropertyAttribute
    {
        /// <summary>
        /// The mode to bind
        /// </summary>
        public BindMode BindMode { get; private set; }

        /// <summary>
        /// Forces the bind field into the specified <paramref name="bindMode"/>
        /// </summary>
        /// <param name="bindMode">The mode this bind should operate in</param>
        public BindAttribute(BindMode bindMode)
        {
            BindMode = bindMode;
        }

        /// <summary>
        /// Marks this bind field as a bind type. This is required when multiple draw attributes are used, 
        /// it will order them into a new draw pipeline.
        /// </summary>
        public BindAttribute()
        {
            BindMode = BindMode.ReadWrite;
        }
    }
}

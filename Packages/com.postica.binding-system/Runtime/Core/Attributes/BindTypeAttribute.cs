using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Registers the value type for generic binds
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BindTypeAttribute : PropertyAttribute
    {
        /// <summary>
        /// The type of the value this bind should allow
        /// </summary>
        public Type BindType { get; private set; }

        /// <summary>
        /// Forces the bind field to operate only on <paramref name="bindType"/> types
        /// </summary>
        /// <param name="bindType">Type of the value for bind field</param>
        public BindTypeAttribute(Type bindType)
        {
            BindType = bindType;
        }

        internal BindTypeAttribute() // <-- This is for internal use only
        {

        }
    }
}

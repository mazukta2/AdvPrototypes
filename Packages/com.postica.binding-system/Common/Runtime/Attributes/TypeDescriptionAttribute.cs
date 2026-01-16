using System;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// Sets the description for a type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class TypeDescriptionAttribute : Attribute
    {
        /// <summary>
        /// The description for this type.
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Sets the description for this type.
        /// </summary>
        /// <param name="description"></param>
        public TypeDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}

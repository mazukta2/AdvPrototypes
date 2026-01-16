using System;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// Sets the icon path for this type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class TypeIconAttribute : PropertyAttribute
    {
        /// <summary>
        /// The path to the icon resource.
        /// </summary>
        public string ResourcePath { get; }
        
        /// <summary>
        /// Sets the icon resource path for this type.
        /// </summary>
        /// <param name="resourcePath"></param>
        public TypeIconAttribute(string resourcePath)
        {
            ResourcePath = resourcePath;
        }
    }
}

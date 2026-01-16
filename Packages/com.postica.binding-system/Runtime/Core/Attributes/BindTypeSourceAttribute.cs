using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Stores the information where to get the source for the bind
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class BindTypeSourceAttribute : PropertyAttribute
    {
        /// <summary>
        /// The relative path to source for the bind
        /// </summary>
        public string FieldPath { get; private set; }

        /// <summary>
        /// Stores the information where to get the source for the bind. Only for expert users
        /// </summary>
        /// <param name="fieldPath">The relative path to the source for the bind</param>
        public BindTypeSourceAttribute(string fieldPath)
        {
            FieldPath = fieldPath;
        }
    }
}

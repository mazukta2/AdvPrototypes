using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Marks this field or property as being able to update multiple times per frame.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class MultiUpdateAttribute : PropertyAttribute
    {
    }
}

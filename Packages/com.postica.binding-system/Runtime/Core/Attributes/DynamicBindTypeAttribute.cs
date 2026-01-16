using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Marks this field or property as having a dynamic bind type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DynamicBindTypeAttribute : PropertyAttribute
    {
    }
}

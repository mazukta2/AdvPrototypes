using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Marks the bind field as being generated automatically by the system
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class GeneratedBindAttribute : PropertyAttribute
    {
        
    }
}

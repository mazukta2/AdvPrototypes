using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Marks this object as having custom accessors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RegistersCustomAccessorsAttribute : PropertyAttribute
    {
    }
}

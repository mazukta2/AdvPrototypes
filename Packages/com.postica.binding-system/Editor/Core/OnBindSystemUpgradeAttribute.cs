using System;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This attribute is used to mark a method that should be called when the Binding System is upgraded.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class OnBindSystemUpgradeAttribute : Attribute
    {
        
    }
}
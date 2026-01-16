using System;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Marks the bind field as read-only. This will let the bind access only members which are read-enabled
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ReadOnlyBindAttribute : BindAttribute
    {
        /// <summary>
        /// Marks the bind field as read-only. This will let the bind access only members which are read-enabled
        /// </summary>
        public ReadOnlyBindAttribute() : base(BindMode.Read) { }
    }
}

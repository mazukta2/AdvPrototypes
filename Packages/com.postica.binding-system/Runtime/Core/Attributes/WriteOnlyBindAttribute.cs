using System;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Marks the bind field as write-only. This will let the bind access only members which are write-enabled
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class WriteOnlyBindAttribute : BindAttribute
    {
        /// <summary>
        /// Marks the bind field as write-only. This will let the bind access only members which are write-enabled
        /// </summary>
        public WriteOnlyBindAttribute() : base(BindMode.Write) { }
    }
}

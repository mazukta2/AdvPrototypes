using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// Use this attribute to hide members from binding system. 
    /// When hidded, the member won't appear in the bind menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field 
                  | AttributeTargets.Property 
                  | AttributeTargets.Method 
                  | AttributeTargets.Class 
                  | AttributeTargets.Struct)]
    public class HideMemberAttribute : PropertyAttribute
    {
        /// <summary>
        /// Type of hide for the member
        /// </summary>
        public enum Hide
        {
            /// <summary>
            /// The member will be hidden completely
            /// </summary>
            Completely,
            /// <summary>
            /// The member itself will be visible, but won't allow further navigation into its own members
            /// </summary>
            InternalsOnly,
            /// <summary>
            /// The member will appear only once per type in the bind menu. 
            /// This means another member of the same type from other sources won't be visible anymore in the bind menu
            /// </summary>
            ShowOnlyOnce,
        }

        /// <summary>
        /// How to hide the member
        /// </summary>
        public Hide HowToHide { get; private set; }

        /// <summary>
        /// Hides the member from binding system.
        /// </summary>
        /// <param name="howToHide">How to hide the member. Default is <see cref="Hide.Completely"/></param>
        public HideMemberAttribute(Hide howToHide = Hide.Completely)
        {
            HowToHide = howToHide;
        }
    }
}

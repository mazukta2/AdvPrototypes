using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class BindOverrideAttribute : Attribute
    {
        public BindMode BindMode { get; private set; }

        public BindOverrideAttribute(BindMode bindMode)
        {
            BindMode = bindMode;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
    public class BindModeAttribute : Attribute
    {
        public BindMode BindMode { get; private set; }

        public BindModeAttribute(BindMode bindMode)
        {
            BindMode = bindMode;
        }
    }
}

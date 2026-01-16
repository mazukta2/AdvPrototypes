using System;
using UnityEngine;

namespace Postica.BindingSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class BindValuesOnChangeAttribute : PropertyAttribute
    {
        public string MethodName { get; private set; }

        public BindValuesOnChangeAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}

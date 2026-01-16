using System;

namespace Postica.Common.Reflection
{
    public partial class ClassProxy
    {
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate)]
        public class ForAttribute : Attribute
        {
            public Type Type { get; }
            
            public ForAttribute(Type sameAssemblyType, string type)
            {
                Type = sameAssemblyType.Assembly.GetType(type);
            }
        }
    }
}
using System;
using System.Reflection;

namespace Postica.Common.Reflection
{
    public partial class ClassProxy
    {
        public class DelegateConverter<T> where T : Delegate
        {
            private Type _type; 

            public DelegateConverter()
            {
                var forAttribute = typeof(T).GetCustomAttribute<ForAttribute>();
                if (forAttribute == null)
                {
                    throw new Exception($"Delegate type {typeof(T)} must have a ForAttribute.");
                }

                _type = forAttribute.Type;
            }

            public object FromT(T value) => Cast(value, _type);
            public T ToT(object value) => value != null ? (T)Cast((Delegate)value, typeof(T)) : null;
            
            private static Delegate Cast(Delegate source, Type type)
            {
                if (source == null) return null;
                 
                var delegates = source.GetInvocationList();
                if (delegates.Length == 1)
                {
                    return Delegate.CreateDelegate(type, delegates[0].Target, delegates[0].Method);
                }

                var delegatesDest = new Delegate[delegates.Length];
                for (int i = 0; i < delegates.Length; i++)
                {
                    delegatesDest[i] = Delegate.CreateDelegate(type, delegates[i].Target, delegates[i].Method);
                }

                return Delegate.Combine(delegatesDest);
            }
        }
    }
}
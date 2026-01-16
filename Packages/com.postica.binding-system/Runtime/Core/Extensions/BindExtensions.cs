using Postica.Common;
using UnityEngine;

namespace Postica.BindingSystem
{
    public static class BindExtensions
    {
        public class NewBind<T>
        {
            internal readonly T value;

            private NewBind() { }

            internal NewBind(in T value)
            {
                this.value = value;
            }
        }

        /// <summary>
        /// Creates a new bound value and returns it. Used mostly for initializing serialized fields.
        /// </summary>
        /// <remarks>Please note that in case of assignment the newly created bind <b>will overwrite</b> the asignee bind</remarks>
        /// <typeparam name="T">The type of the value to be bound</typeparam>
        /// <param name="value">The value to be bound</param>
        /// <returns>A newly created bind</returns>
        public static NewBind<T> Bind<T>(this T value)
        {
            return new NewBind<T>(value);
        }
        
        /// <summary>
        /// Returns a string representing the direct value or <paramref name="varName"/> if the value is bound.
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="bind">The bind object</param>
        /// <param name="varName">The name to use if the value is bound and not known at compile time</param>
        /// <param name="richTextFormatting">If true, a special formatting will be used</param>
        /// <returns></returns>
        public static string ToString<T>(this Bind<T> bind, string varName, bool richTextFormatting = true)
        {
            if (bind.IsBound)
            {
                return richTextFormatting ? varName.RT().Bold().Color(BindColors.Primary) : $"[{varName}]";
            }
            return bind.Value?.ToString();
        }

        /// <summary>
        /// Returns a string representing the direct value or <paramref name="varName"/> if the value is bound.
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="bind">The bind object</param>
        /// <param name="varName">The name to use if the value is bound and not known at compile time</param>
        /// <param name="richTextFormatting">If true, a special formatting will be used</param>
        /// <returns></returns>
        public static string ToString<T>(this ReadOnlyBind<T> bind, string varName, bool richTextFormatting = true)
        {
            if (bind.IsBound)
            {
                return richTextFormatting ? varName.RT().Bold().Color(BindColors.Primary) : $"[{varName}]";
            }
            return bind.Value?.ToString();
        }

        private class DirectValue<T> : IValueProvider<T>
        {
            public DirectValue(object value)
            {
                if(value is T tvalue)
                {
                    Value = tvalue;
                }
                UnsafeValue = value;
            }

            public T Value { get; }

            public object UnsafeValue { get; }
        }

        private class DirectValue : IValueProvider
        {
            public DirectValue(object value)
            {
                UnsafeValue = value;
            }

            public object UnsafeValue { get; }
        }

        internal static string ToShortString(this Object obj, char separator = '/')
        {
            if (!obj)
            {
                return "null";
            }
            if (obj is Component c)
            {
                return obj.name + separator + c.GetType().Name;
            }
            return obj.name;
        }

        internal static string FullPath(this in BindData data)
        {
            return data.Source.ToShortString('.') + '.' + data.Path.WithDots();
        }
        
        internal static string FullPath(this in BindDataLite data)
        {
            return data.Source.ToShortString('.') + '.' + data.Path.WithDots();
        }
        
        internal static string FullPath<T>(this in BindData<T> data)
        {
            return data.Source.ToShortString('.') + '.' + data.Path.WithDots();
        }
        
        internal static string WithDots(this string str)
        {
            return str.Replace('/', '.');
        }

        internal static IValueProvider[] ToValueProviders(this object[] values)
        {
            IValueProvider[] pars = null;
            if (values?.Length > 0)
            {
                pars = new IValueProvider[values.Length];
                for (int i = 0; i < pars.Length; i++)
                {
                    if (values[i] is IValueProvider valueProvider)
                    {
                        pars[i] = valueProvider;
                    }
                    else
                    {
                        pars[i] = new DirectValue(values[i]);
                    }
                }
            }
            return pars;
        }
    }
}

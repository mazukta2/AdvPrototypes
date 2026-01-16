using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Postica.Common
{
    /// <summary>
    /// This class contains extension methods for the <see cref="Type"/> class.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Gets the user-friendly name of the type. <br/>
        /// e.g. Single -> float, Int32 -> int, List`1 -> List&lt;int&gt;
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetAliasName(this Type type)
        {
            if (type == typeof(bool)) return "bool";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(sbyte)) return "sbyte";
            if (type == typeof(char)) return "char";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(int)) return "int";
            if (type == typeof(uint)) return "uint";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";
            if (type == typeof(short)) return "short";
            if (type == typeof(ushort)) return "ushort";
            if (type == typeof(string)) return "string";

            return UserFriendlyName(type, false, false, false);
        }

        /// <summary>
        /// Gets the user-friendly name of the type. <br/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string UserFriendlyName(this Type type)
        {
            return UserFriendlyName(type, false, true, false);
        }

        /// <summary>
        /// Gets the user-friendly name of the type. <br/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="useAliasNames">Whether to use alias names even for argument types</param>
        /// <returns></returns>
        public static string UserFriendlyName(this Type type, bool useAliasNames)
        {
            return UserFriendlyName(type, false, useAliasNames, true);
        }

        /// <summary>
        /// Gets the user-friendly name of the type with its namespace. <br/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string UserFriendlyFullName(this Type type)
        {
            return UserFriendlyName(type, true, false, false);
        }

        /// <summary>
        /// Gets the code-friendly name of the type with its namespace. <br/>
        /// A code-friendly name is a name that can be used in code without any issues. <br/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="codeFriendly"></param>
        /// <returns></returns>
        public static string UserFriendlyFullName(this Type type, bool codeFriendly)
        {
            return UserFriendlyName(type, true, false, codeFriendly);
        }

        private static string UserFriendlyName(Type type, bool fullName, bool useAlias, bool codeFriendly)
        {
            if (type == null)
            {
                return "No Type";
            }
            var name = fullName ? type.FullName : (useAlias ? GetAliasName(type) : type.Name);
            if (name.StartsWith("Nullable`", StringComparison.Ordinal))
            {
                return UserFriendlyName(type.GetGenericArguments()[0], fullName, useAlias, codeFriendly) + '?';
            }
            if (type.GetGenericArguments()?.Length > 0)
            {
                var indexOfQuote = name.IndexOf('`', StringComparison.Ordinal);
                if (indexOfQuote < 0)
                {
                    return name;
                }
                var sb = new StringBuilder();
                sb.Append(name.Substring(0, indexOfQuote)).Append('<');
                foreach (var arg in type.GetGenericArguments())
                {
                    sb.Append(UserFriendlyName(arg, fullName, useAlias, codeFriendly)).Append(',').Append(' ');
                }
                sb.Length -= 2;
                sb.Append('>');
                return sb.ToString().MakeCodeFriendly(codeFriendly);
            }
            else if (type.IsArray)
            {
                return UserFriendlyName(type.GetElementType(), fullName, useAlias, codeFriendly) + "[]";
            }
            return name.MakeCodeFriendly(codeFriendly);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string MakeCodeFriendly(this string str, bool codeFriendly)
            => codeFriendly ? str.Replace('+', '.') : str;
        
        /// <summary>
        /// Gets the generic arguments of <paramref name="type"/> if this type is assignable from it.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="type">The generic type to check against</param>
        /// <returns>The array of arguments type, or empty if not supported</returns>
        public static bool TryGetGenericArguments(this Type t, Type type, out Type[] args)
        {
            if (type.IsInterface)
            {
                foreach (var iface in t.GetInterfaces())
                {
                    if (iface.IsGenericType && iface.GetGenericTypeDefinition() == type)
                    {
                        args = iface.GetGenericArguments();
                        return args.Length > 0;
                    }
                }

                args = null;
                return false;
            }

            if (!type.IsGenericType)
            {
                args = null;
                return false;
            }
            
            while (t != null)
            {
                if (!t.IsGenericType || t.GetGenericTypeDefinition() != type)
                {
                    t = t.BaseType;
                    continue;
                }
                    
                args = t.GetGenericArguments();
                return args.Length > 0;
            }
            
            args = null;
            return false;
        }
        
        /// <summary>
        /// Gets whether the type is an interface with the specified generic arguments or not.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="interface"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool IsInterfaceWithArgs(this Type t, Type @interface, params Type[] args)
        {
            if (!@interface.IsInterface)
            {
                return false;
            }

            foreach (var iface in t.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == @interface)
                {
                    return iface.GetGenericArguments().SequenceEqual(args);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets whether the type is an interface with the specified partial generic arguments or not.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="interface"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static bool IsInterfaceWithPartialArgs(this Type t, Type @interface, params Type[] args)
        {
            if (!@interface.IsInterface)
            {
                return false;
            }

            foreach (var iface in t.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == @interface)
                {
                    var ifaceArgs = iface.GetGenericArguments();
                    if (ifaceArgs.Length < args.Length)
                    {
                        continue;
                    }
                    
                    var shouldContinue = false;
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (ifaceArgs[i] != args[i])
                        {
                            shouldContinue = true;
                            break;
                        }
                    }
                    
                    if (shouldContinue)
                    {
                        continue;
                    }
                    
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Gets the depth of the type in the type hierarchy.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetTypeHierarchyDepth(this Type type)
        {
            return type?.BaseType != null ? type.BaseType.GetTypeHierarchyDepth() + 1 : 0;
        }
        
        /// <summary>
        /// Gets the attribute of the specified type from the enum value.
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetAttribute<T>(this Enum value) where T : Attribute
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            var field = type.GetField(name);
            return (T)Attribute.GetCustomAttribute(field, typeof(T));
        }
        
        /// <summary>
        /// Gets whether the type is a Unity type or not.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static bool IsUnityType(this Type type)
        {
            return type is { Namespace: not null } && type.Namespace.StartsWith("UnityEngine", StringComparison.Ordinal);
        }
        
        /// <summary>
        /// Gets the signature of the type.
        /// </summary>
        /// <remarks>The signature of a type may be different from its name. E.g. List`1 has a signature List&lt;int&gt;</remarks>
        /// <param name="type"></param>
        /// <param name="withNamespace">Whether the signature should contain the namespace or not</param>
        /// <returns></returns>
        public static string ToSignatureString(this Type type, bool withNamespace = true)
        {
            if(type == null) { return "null"; }
            var sb = new StringBuilder();
            var genericArgs = type.GetGenericArguments();
            BuildTypeSignature(type, sb, genericArgs, genericArgs?.Length ?? 0, withNamespace, withNamespace);
            return sb.ToString();
        }

        private static void BuildTypeSignature(Type type, 
                                               StringBuilder sb, 
                                               Type[] genericArguments, 
                                               int genericArgsLength, 
                                               bool withNamespace,
                                               bool genericArgsUseNamespace)
        {
            if (withNamespace && !string.IsNullOrEmpty(type.Namespace))
            {
                sb.Append(type.Namespace).Append('.');
            }

            var genericArgsIndex = type.Name.IndexOf('`');
            var localGenericArgsAmount = -1;

            if (type.DeclaringType != null)
            {
                localGenericArgsAmount = genericArgsIndex < 0 ? 0 : int.Parse(type.Name.Substring(genericArgsIndex + 1, 1));
                BuildTypeSignature(type.DeclaringType, sb, genericArguments, genericArgsLength - localGenericArgsAmount, false, genericArgsUseNamespace);
                sb.Append('.');
            }

            if(genericArgsIndex > 0)
            {
                sb.Append(type.Name.Substring(0, genericArgsIndex));
                if (localGenericArgsAmount < 0) { localGenericArgsAmount = int.Parse(type.Name.Substring(genericArgsIndex + 1, 1)); }
                if(localGenericArgsAmount >= 0)
                {
                    var startIndex = genericArgsLength - localGenericArgsAmount;
                    var endIndex = genericArgsLength;
                    sb.Append('<');
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        var genericArgType = genericArguments[i];
                        var paramGenericArgTypes = genericArgType.GetGenericArguments();
                        BuildTypeSignature(genericArgType, sb, paramGenericArgTypes, paramGenericArgTypes?.Length ?? 0, genericArgsUseNamespace, genericArgsUseNamespace);
                        sb.Append(',').Append(' ');
                    }
                    sb.Length -= 2; // Remove the last ", " part of the last element
                    sb.Append('>');
                }
            }
            else
            {
                sb.Append(type.Name);
            }
        }

        /// <summary>
        /// Gets the number of generic arguments of the type. If the type is not generic, it returns 0.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int GetGenericArgumentsCount(this Type type)
        {
            var genericArgsIndex = type.Name.IndexOf('`');
            return genericArgsIndex < 0 ? 0 : int.Parse(type.Name.Substring(genericArgsIndex + 1, 1));
        }

        /// <summary>
        /// Gets the list of base types of the type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetTypeHierarchy(this Type type)
        {
            if(type.IsValueType)
            {
                return new Type[] { type };
            }
            List<Type> hierarchy = new List<Type>();
            while(type != null && type != typeof(object))
            {
                hierarchy.Add(type);
                type = type.BaseType;
            }
            return hierarchy;
        }

        /// <summary>
        /// Checks if the type is a type of the specified generic type.
        /// </summary>
        /// <param name="type"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>True if the type is descendant of a generic type</returns>
        public static bool IsGenericTypeOf<T>(this Type type) 
        {
            return IsGenericTypeOf(type, typeof(T));
        }
        
        /// <summary>
        /// Checks if the type is a type of the specified generic type.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="testType">The generic type to test against</param>
        /// <returns>True if the type is descendant of a generic type</returns>
        public static bool IsGenericTypeOf(this Type type, Type testType)
        {
            while(type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == testType)
                {
                    return true;
                }
                type = type.BaseType;
            }

            return false;
        }
        
        /// <summary>
        /// Checks if the type implements the specified generic interface.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="testType">The generic interface to test against</param>
        /// <returns>True if the type implements the generic interface</returns>
        public static bool ImplementsInterfaceOf(this Type type, Type testType, Type genericArg)
        {
            var interfaces = type.GetInterfaces();
            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == testType)
                {
                    var args = iface.GetGenericArguments();
                    if (args.Length > 0 && args[0] == genericArg)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

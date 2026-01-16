using Postica.BindingSystem.Accessors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Postica.BindingSystem
{

    public static class BindTypesCache
    {
        private readonly static Dictionary<Type, List<Type>> _providerTypes = new Dictionary<Type, List<Type>>();
        private readonly static List<Type> _allProviderTypes = new List<Type>();
        private static Dictionary<string, IAccessorProvider> _accessorProviders;

        public static IReadOnlyList<Type> GetAllProviderTypes()
        {
            if(_allProviderTypes.Count == 0)
            {
                foreach (var type in TypeCache.GetTypesDerivedFrom<object>())
                {
                    if (type.IsGenericType)
                    {
                        continue;
                    }
                    if(TryGetStronglyTypedProvider(type, out var providerTypes))
                    {
                        _allProviderTypes.Add(type);
                        foreach(var providerType in providerTypes)
                        {
                            var elemType = providerType.GenericTypeArguments[0];
                            if (!_providerTypes.TryGetValue(elemType, out var list))
                            {
                                list = new List<Type>() { type };
                                _providerTypes.Add(elemType, list);
                            }
                            else if(!list.Contains(type))
                            {
                                list.Add(type);
                            }
                        }
                    }
                    else if (typeof(IValueProvider).IsAssignableFrom(type))
                    {
                        _allProviderTypes.Add(type);
                    }
                }
            }
            return _allProviderTypes;
        }

        private static bool TryGetStronglyTypedProvider(Type sourceType, out Type[] providerTypes)
        {
            providerTypes = sourceType.GetInterfaces().Where(i => i.IsGenericType && typeof(IValueProvider<>) == i.GetGenericTypeDefinition()).ToArray();
            return providerTypes?.Length > 0;
        }

        public static IReadOnlyList<Type> GetProvidersFor(Type valueType)
        {
            if(_allProviderTypes.Count == 0)
            {
                GetAllProviderTypes();
            }
            if(_providerTypes.TryGetValue(valueType, out var list))
            {
                return list;
            }
            return null;
        }

        public static IReadOnlyDictionary<string, IAccessorProvider> GetAllAccessorProviders()
        {
            if (_accessorProviders == null)
            {
                _accessorProviders = new Dictionary<string, IAccessorProvider>();
                foreach(var providerType in TypeCache.GetTypesDerivedFrom<IAccessorProvider>())
                {
                    try
                    {
                        if(providerType.IsGenericTypeDefinition || providerType.IsAbstract || providerType.IsInterface)
                        {
                            continue;
                        }
                        var provider = Activator.CreateInstance(providerType) as IAccessorProvider;
                        _accessorProviders[provider.Id] = provider;
                    }
                    catch(Exception ex)
                    {
                        Debug.LogWarning($"Unable to instantiate an accessor provider of type {providerType.Name}: {ex.Message}");
                    }
                }
            }
            return _accessorProviders;
        }
    }
}
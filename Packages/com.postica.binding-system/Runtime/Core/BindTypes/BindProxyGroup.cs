using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Postica.BindingSystem.Accessors;
using UnityEngine;
using Object = UnityEngine.Object;
using Postica.Common;
using UnityEngine.Serialization;

namespace Postica.BindingSystem
{
    [Serializable]
    internal class BindProxyGroup
    {
        public Object context;
        public List<BindProxy> bindProxies;
        
        public bool TryGetBindProxy(string path, out BindProxy bindProxy)
        {
            bindProxy = bindProxies.Find(proxy => proxy.Path == path);
            return bindProxy != null;
        }
        
        public bool IsValid()
        {
            return context && bindProxies != null;
        }
        
        public void Cleanup()
        {
            bindProxies.RemoveAll(proxy => proxy == null);
        }
        
        public void OnValidate()
        {
            Cleanup();
            foreach (var bindProxy in bindProxies)
            {
                bindProxy?.OnValidate();
            }
        }
    }

    internal static class BindProxyGroupExtensions
    {
        public static Dictionary<Object, List<BindProxy>> ToDictionary(this IList<BindProxyGroup> groups,
            bool removeInvalid = false)
        {
            var proxies = new Dictionary<Object, List<BindProxy>>();
            for(var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group?.IsValid() == true)
                {
                    if (!proxies.TryGetValue(group.context, out var list))
                    {
                        list = new List<BindProxy>();
                        proxies[group.context] = list;
                    }
                    list.AddRange(group.bindProxies);
                }
                else if (removeInvalid)
                {
                    groups.RemoveAt(i--);
                }
            }

            return proxies;
        }
        
        public static Dictionary<Object, List<BindProxy>> ToDictionary(this IEnumerable<BindProxyGroup> groups)
        {
            var proxies = new Dictionary<Object, List<BindProxy>>();
            foreach (var group in groups)
            {
                if (group?.IsValid() != true) continue;
                
                if (!proxies.TryGetValue(group.context, out var list))
                {
                    list = new List<BindProxy>();
                    proxies[group.context] = list;
                }
                list.AddRange(group.bindProxies);
            }

            return proxies;
        }
        
        public static void OnValidate(this IEnumerable<BindProxyGroup> groups)
        {
            foreach (var group in groups)
            {
                group?.OnValidate();
            }
        }
        
        public static void OnValidate(this IList<BindProxyGroup> groups, bool removeInvalid = false)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                groups[i]?.OnValidate();
                if (removeInvalid && !groups[i].IsValid())
                {
                    groups.RemoveAt(i--);
                }
            }
        }
        
        public static void Cleanup(this IEnumerable<BindProxyGroup> groups)
        {
            foreach (var group in groups.ToList())
            {
                group?.Cleanup();
            }
        }
    }
}

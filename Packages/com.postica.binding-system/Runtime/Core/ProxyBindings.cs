using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    [HideMember]
    internal class ProxyBindings : MonoBehaviour, IBindProxyProvider, ISerializationCallbackReceiver, IBindProxyContainer
    {
        internal static Action<IBindProxyProvider> MakeDirty;
        
        private readonly Dictionary<Type, string> _typeMap = new();
        private Dictionary<string, BindProxy> _proxyMap;
        
        [SerializeField]
        private List<BindProxy> bindings = new();

        public List<BindProxy> Bindings => bindings;
        
        
        #region [ IBindProxyContainer Implementation ]
        IReadOnlyList<IBindProxy> IBindProxyContainer.Bindings => bindings;
        
        void IBindProxyContainer.RemoveProxy(IBindProxy proxy)
        {
            if (proxy is BindProxy bindProxy)
            {
                RemoveProxy(bindProxy);
            }
        }

        void IBindProxyContainer.RemoveProxyAt(int index)
        {
            var proxy = bindings[index];
            if (proxy != null)
            {
                RemoveProxy(proxy);
            }
        }

        #endregion
        
        private Dictionary<string, BindProxy> ProxyMap => _proxyMap ??= bindings.ToDictionary(GetProxyId, p => p);

        IEnumerable<BindProxy> IBindProxyProvider.GetAllProxies() => bindings;
        
        public bool UpdateProxyAt(int index)
        {
            if(index < 0 || index >= bindings.Count)
            {
                return false;
            }
            
            bindings[index].Update();
            return false;
        }
        
        public void UpdateProxy(string id)
        {
            if(ProxyMap.TryGetValue(id, out var proxy))
            {
                proxy.Update();
            }
        }

        public string GetProxyId(BindProxy proxy)
        {
            var typename = proxy.SourceType?.FullName ?? proxy.Source.GetType().FullName;
            return proxy.Path + "@" + typename;
        }

        public bool TryGetProxy(string id, out BindProxy proxy, out int index)
        {
            if(ProxyMap.TryGetValue(id, out proxy))
            {
                index = bindings.IndexOf(proxy);
                return true;
            }
            
            index = -1;
            return false;
        }

        public bool IsEmpty => bindings.Count == 0;

        private void OnValidate()
        {
            if(Bindings.Count == 0) return;
            
            var markDirty = false;
            for(var i = 0; i < Bindings.Count; i++)
            {
                var bindProxy = Bindings[i];
                if (bindProxy == null)
                {
                    bindings.RemoveAt(i--);
                    markDirty = true;
                    continue;
                }
                
                bindProxy.Provider = this;
                bindProxy.OnValidate();
            }
            
            if (markDirty)
            {
                MakeDirty?.Invoke(this);
            }
        }

        private void OnEnable()
        {
            if(Bindings.Count == 0) return;

            foreach (var bindProxy in Bindings)
            {
                if (bindProxy == null)
                {
                    continue;
                }
                
                bindProxy.Provider = this;
                bindProxy.RegisterForUpdates();
            }
        }
        
        private void OnDisable()
        {
            foreach (var bindProxy in Bindings)
            {
                if (bindProxy.OptionsValue.HasFlag(BindProxy.Options.NeedsToBeAlive))
                {
                    continue;
                }
                bindProxy?.UnregisterForUpdates();
            }
        }

        private void OnDestroy()
        {
            foreach (var bindProxy in Bindings)
            {
                bindProxy?.OnDestroy();
            }
        }

        public bool TryGetProxy(Object source, string path, out BindProxy proxy, out int index)
        {
            index = bindings.FindIndex(p => p.Source == source && p.Path == path);
            proxy = index >= 0 ? bindings[index] : null;
            return proxy != null;
        }

        public bool TryGetProxiesInTree(Object source, string rootPath, out List<(BindProxy proxy, int index)> proxies)
        {
            proxies = new List<(BindProxy, int)>();
            for (var i = 0; i < bindings.Count; i++)
            {
                var bindProxy = bindings[i];
                if (bindProxy.Source == source && bindProxy.Path.StartsWith(rootPath, StringComparison.Ordinal))
                {
                    proxies.Add((bindProxy, i));
                }
            }
            
            return proxies.Count > 0;
        }

        public List<BindProxy> GetProxies(Object source)
        {
            if (source is GameObject go && go)
            {
                // Get all proxies from all components
                var proxies = new List<BindProxy>();
                foreach (var component in go.GetComponents<Component>())
                {
                    proxies.AddRange(GetProxies(component));
                }
                return proxies;
            }
            return bindings.FindAll(p => p.Source == source);
        }

        public bool RemoveProxy(Object source, string path)
        {
            if (bindings.RemoveAll(p => p.Source == source && p.Path == path) <= 0) return false;
            
            _proxyMap = null;
            MakeDirty?.Invoke(this);
            return true;
        }

        public bool RemoveProxy(BindProxy proxy)
        {
            if (!bindings.Remove(proxy)) return false;
            
            _proxyMap = null;
            MakeDirty?.Invoke(this);
            return true;
        }

        public bool AddProxy(BindProxy proxy)
        {
            if(bindings.Any(p => p.Source == proxy.Source && p.Path == proxy.Path))
            {
                return false;
            }
            
            bindings.Add(proxy);
            _proxyMap = null;
            MakeDirty?.Invoke(this);
            return true;
        }

        public bool RemoveProxies(Object source)
        {
            if(source is GameObject go)
            {
                // Remove all proxies from all components
                var removed = false;
                foreach (var component in go.GetComponents<Component>())
                {
                    removed |= RemoveProxies(component);
                }
                
                if(removed)
                {
                    _proxyMap = null;
                    MakeDirty?.Invoke(this);
                    return true;
                }

                return false;
            }

            if (bindings.RemoveAll(p => p.Source == source) > 0)
            {
                _proxyMap = null;
                MakeDirty?.Invoke(this);
                return true;
            }

            return false;
        }

        public void OnBeforeSerialize()
        {
            // Nothing to do here, serialization is handled by Unity
        }

        public void OnAfterDeserialize()
        {
            // Register this proxy to the engine
            foreach (var bindProxy in bindings)
            {
                if (bindProxy == null)
                {
                    continue;
                }

                if (bindProxy.OptionsValue.HasFlag(BindProxy.Options.NeedsToBeAlive))
                {
                    // Register for updates if the proxy needs to be alive
                    bindProxy.Provider = this;
                    BindingEngineInternal.RegisterDisabledProxy(bindProxy);
                }
            }
        }
    }
}

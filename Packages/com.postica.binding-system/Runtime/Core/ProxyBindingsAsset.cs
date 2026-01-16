using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    [DefaultExecutionOrder(-32000)]
    [HideMember]
    internal class ProxyBindingsAsset : ScriptableObject, IBindProxyProvider, IBindProxyContainer
    {
        internal static Action<IBindProxyProvider> MakeDirty;
        
        [SerializeField]
        private List<BindProxy> bindings = new();
        [SerializeField]
        private List<ObjectKey> _keys = new();

        [NonSerialized]
        private Dictionary<Object, List<BindProxy>> _proxies;
        [NonSerialized]
        private Dictionary<string, BindProxy> _proxyMap;
        
        IEnumerable<BindProxy> IBindProxyProvider.GetAllProxies() => bindings;

        private Dictionary<string, BindProxy> ProxyMap
        {
            get
            {
                if (_proxyMap != null)
                {
                    return _proxyMap;
                }
                
                var objects = BindProxies.Keys.ToHashSet();
                for (int i = 0; i < _keys.Count; i++)
                {
                    if(_keys[i].IsEmpty || objects.Contains(_keys[i].source)) continue;
                    _keys.RemoveAt(i--);
                }
                
                _proxyMap = new Dictionary<string, BindProxy>();
                foreach (var p in bindings)
                {
                    _proxyMap[GetProxyId(p)] = p;
                }

                return _proxyMap;
            }
        }
        
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
        
        public void UpdateProxy(string id)
        {
            if(ProxyMap.TryGetValue(id, out var proxy))
            {
                proxy.Update();
            }
        }

        public bool UpdateProxyAt(int index)
        {
            if(index < 0 || index >= bindings.Count)
            {
                return false;
            }
            
            bindings[index].Update();
            return false;
        }

        public string GetProxyId(BindProxy proxy)
        {
            var key = proxy.Source ? GetKey(proxy.Source) : "null";
            return proxy.Path + "@" + key;
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
        
        private string GetKey(Object source)
        {
            var key = _keys.Find(k => k.source == source);
            if(key.IsEmpty)
            {
                key = new ObjectKey {source = source, id = Guid.NewGuid().ToString()};
                _keys.Add(key);
            }
            return key.id;
        }

        public bool IsEmpty => bindings.Count == 0;

        public IReadOnlyDictionary<Object, List<BindProxy>> BindProxies
        {
            get
            {
                if (_proxies == null)
                {
                    _proxies = new Dictionary<Object, List<BindProxy>>();

                    bool markDirty = false;
                    
                    for (var index = 0; index < bindings.Count; index++)
                    {
                        var bindProxy = bindings[index];
                        
                        if (bindProxy?.IsValid != true)
                        {
                            bindings.RemoveAt(index--);
                            markDirty = true;
                            continue;
                        }

                        if (!_proxies.TryGetValue(bindProxy.Source, out var list))
                        {
                            list = new List<BindProxy>();
                            _proxies[bindProxy.Source] = list;
                        }
                        
                        list.Add(bindProxy);
                    }
                    
                    if (markDirty)
                    {
                        _proxyMap = null;
                        MakeDirty?.Invoke(this);
                    }
                }

                return _proxies;
            }
        }

        public bool HasBindings => bindings?.Count > 0;

        private void OnValidate()
        {
            var shouldUpdateDictionary = false;
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i]?.IsValid != true)
                {
                    bindings.RemoveAt(i--);
                    shouldUpdateDictionary = true;
                    continue;
                }
                
                bindings[i].Provider = this;
                bindings[i].OnValidate();
            }
            
            if (shouldUpdateDictionary)
            {
                _proxies = null;
                _proxyMap = null;
                MakeDirty?.Invoke(this);
            }
        }

        private void OnEnable()
        {
            foreach (var binding in bindings)
            {
                if(binding == null) continue;
                
                binding.Provider = this;
                binding.RegisterForUpdates();
            }
        }
        
        private void OnDisable()
        {
            foreach (var binding in bindings)
            {
                binding?.UnregisterForUpdates();
            }
        }
        
        private void OnDestroy()
        {
            foreach (var binding in bindings)
            {
                binding?.OnDestroy();
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
            return BindProxies.TryGetValue(source, out var proxies)
                ? proxies
                : new List<BindProxy>();
        }

        public bool RemoveProxy(Object source, string path)
        {
            if(bindings.RemoveAll(p => p.Source == source && p.Path == path) > 0)
            {
                _proxies = null;
                _proxyMap = null;
                MakeDirty?.Invoke(this);
                return true;
            }
            return false;
        }
        
        public bool RemoveProxy(BindProxy proxy)
        {
            if (bindings.Remove(proxy))
            {
                _proxies = null;
                _proxyMap = null;
                MakeDirty?.Invoke(this);
                return true;
            }
            return false;
        }

        public bool AddProxy(BindProxy proxy)
        {
            if (BindProxies.TryGetValue(proxy.Source, out var proxies)
                && proxies.Contains(proxy))
            {
                return false;
            }
            bindings.Add(proxy);
            _proxies = null;
            _proxyMap = null;
            MakeDirty?.Invoke(this);
            return true;
        }

        public bool RemoveProxies(Object source)
        {
            if (bindings.RemoveAll(p => p.Source == source) > 0)
            {
                _proxies = null;
                _proxyMap = null;
                MakeDirty?.Invoke(this);
                return true;
            }
            return false;
        }

        [Serializable]
        private struct ObjectKey
        {
            public Object source;
            public string id;
            
            public bool IsEmpty => source == null;
        }
    }
}

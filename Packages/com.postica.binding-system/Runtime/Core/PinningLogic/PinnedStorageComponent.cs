using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.PinningLogic
{
    [DefaultExecutionOrder(-32000)]
    [HideMember]
    public class PinnedStorageComponent : MonoBehaviour, IPinnedStorage
    {
        private const int MaxLastUsedContexts = 3;
        
        [SerializeField]
        private List<Object> _lastUsedContexts = new();
        
        [SerializeField]
        private List<PinnedPath> _paths = new();

        public string Id => this ? "Scene " + gameObject.scene.name : "Pinned Storage";
        public IEnumerable<PinnedPath> AllPaths => _paths;

        private void OnEnable()
        {
            _paths ??= new List<PinnedPath>();
            _paths.RemoveAll(path => !path.context);
        }
        
        public void AddPath(PinnedPath path)
        {
            if (!_paths.Contains(path))
            {
                _paths.Add(path);
            }
        }

        public void RemovePath(PinnedPath path)
        {
            _paths.Remove(path);
        }

        public void Clear()
        {
            _paths.Clear();
        }
        
        public void StorePinUsage(Object context)
        {
            _lastUsedContexts.Remove(context);
            _lastUsedContexts.Insert(0, context);
            if (_lastUsedContexts.Count > MaxLastUsedContexts)
            {
                _lastUsedContexts.RemoveRange(MaxLastUsedContexts, _lastUsedContexts.Count - MaxLastUsedContexts);
            }
        }

        public IEnumerable<Object> GetLastUsedPins()
        {
            return _lastUsedContexts;
        }

        public bool ContainsPath(PinnedPath path)
        {
            return _paths.Contains(path);
        }

        public IEnumerable<PinnedPath> GetPathsForObject(Object obj)
        {
            return _paths.FindAll(path => path.context == obj);
        }
    }
}

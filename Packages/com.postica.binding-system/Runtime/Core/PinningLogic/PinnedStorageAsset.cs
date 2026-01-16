using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.PinningLogic
{
    [DefaultExecutionOrder(-32000)]
    [HideMember]
    public class PinnedStorageAsset : ScriptableObject, IPinnedStorage
    {
        private const int MaxLastUsedContexts = 3;
        
        [SerializeField]
        private List<Object> _lastUsedContexts;
        [SerializeField]
        private List<PinnedPath> _paths;

        public string Id => "Assets";
        public IEnumerable<PinnedPath> AllPaths => _paths;

        private void OnEnable()
        {
            _paths ??= new List<PinnedPath>();
            _lastUsedContexts ??= new List<Object>();
            
            var removedItems = _paths.RemoveAll(path => !path.context);
            if (removedItems > 0)
            {
                Debug.LogWarning($"Removed {removedItems} invalid paths from PinnedStorageAsset.");
            }
            var removedContexts = _lastUsedContexts.RemoveAll(context => !context);
            if (removedContexts > 0)
            {
                Debug.LogWarning($"Removed {removedContexts} invalid contexts from PinnedStorageAsset.");
            }
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

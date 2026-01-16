using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Postica.Common;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Dependencies
{
    internal class BindFindInLoadedScenes : IBatchBindFinder
    {
        const int MaxProcessedPerFrame = 100;
        private static readonly Dictionary<Type, List<string>> _bindDataPaths = new();
        
        
        private Dictionary<(Object root, string path), SearchItem> _searchItems = new();
        private List<SearchItem> _currentBatch = new();
        
        public string Name => "Loaded Scenes";
        public Texture2D Icon => ObjectIcon.GetFor<Scene>();
        public bool IsBatchPaused { get; private set; }
        
        public async Task<IEnumerable<BindDependencyGroup>> FindDependencies(Object root, string path, Action<float> progress, CancellationToken cancellationToken)
        {
            if (_searchItems.TryGetValue((root, path), out var searchItem) && searchItem.state == SearchItemState.Ready)
            {
                progress?.Invoke(1);
                return searchItem.groups.Values;
            }
            
            searchItem ??= new SearchItem(root, path, progress);

            if (searchItem.state == SearchItemState.Ready)
            {
                progress?.Invoke(1);
                return searchItem.groups.Values;
            }
            
            _currentBatch?.Remove(searchItem);

            try
            {
                PauseBatch();
                
                var scenes = SceneManager.sceneCount;

                using var awaiter = new SmartAwaiter();
                var items = new[] { searchItem };
                for (int i = 0; i < scenes; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    await SearchInScene(SceneManager.GetSceneAt(i), items, 1f / scenes, awaiter, true, null, cancellationToken);
                }

                searchItem.state = SearchItemState.Ready;
            }
            finally
            {
                progress?.Invoke(1);
                ResumeBatch();
            }
            
            return searchItem.groups.Values;
        }

        private async Task SearchInScene(Scene scene,
            IEnumerable<SearchItem> items,
            float progressFactor,
            SmartAwaiter awaiter,
            bool isPriorityFind,
            Action<float> progressCallback,
            CancellationToken cancellationToken)
        {
            var (groupName, groupIcon) = (scene.name, ObjectIcon.GetFor<Scene>());
            var processedComponents = new HashSet<Component>();
            var roots = scene.GetRootGameObjects();
            var rootsProcessed = 0;
            
            foreach (var rootGO in roots)
            {
                while (IsBatchPaused && !cancellationToken.IsCancellationRequested && !isPriorityFind)
                {
                    await Task.Yield();
                }
                
                if(cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await awaiter.Await();
                
                var progress = rootsProcessed++ / (float)roots.Length * progressFactor;
                progressCallback?.Invoke(progress);
                
                foreach (var proxyBindings in rootGO.GetComponentsInChildren<IBindProxyProvider>(true))
                {
                    if(cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    if(proxyBindings is not Component component)
                    {
                        continue;
                    }
                    
                    if(!processedComponents.Add(component))
                    {
                        continue;
                    }

                    await awaiter.Await();

                    foreach (var proxy in proxyBindings.GetAllProxies())
                    {
                        if (!proxy.IsBound)
                        {
                            continue;
                        }

                        while (IsBatchPaused && !cancellationToken.IsCancellationRequested && !isPriorityFind)
                        {
                            await Task.Yield();
                        }
                        
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        
                        foreach (var item in items)
                        {
                            item.progress = progress;
                            item.state = SearchItemState.Processing;

                            if (proxy.BindData?.Source != item.root || !IsSimilarPath(proxy.BindData?.Path, item.path))
                            {
                                continue;
                            }
                            
                            if(cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }
                            
                            var group = item.GetGroup(groupName, groupIcon);
                            group.Dependencies.Add(new BindDependency(item.root, proxy.Source, item.path, proxy.Path));
                            
                            await awaiter.Await();
                        }
                    }
                }
                
                foreach (var component in rootGO.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if(!processedComponents.Add(component))
                    {
                        continue;
                    }
                    
                    while (IsBatchPaused && !cancellationToken.IsCancellationRequested && !isPriorityFind)
                    {
                        await Task.Yield();
                    }
                    
                    if(cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    await awaiter.Await();

                    using var serObj = new SerializedObject(component);
                    if(_bindDataPaths.TryGetValue(component.GetType(), out var paths))
                    {
                        foreach (var p in paths)
                        {
                            var prop = serObj.FindProperty(p);
                            foreach (var item in items)
                            {
                                if (!BindDataPointsTo(prop, item.root, item.path))
                                {
                                    continue;
                                }
                                
                                if(cancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }
                                
                                var group = item.GetGroup(groupName, groupIcon);
                                group.Dependencies.Add(new BindDependency(item.root, component, item.path,
                                        prop.propertyPath.RemoveAtEnd(prop.name).TrimEnd('.')));
                                
                                await awaiter.Await();
                            }
                        }
                    }
                    else
                    {
                        var prop = serObj.FindProperty("m_Script");
                        var typePaths = new List<string>();
                        _bindDataPaths.Add(component.GetType(), typePaths);
                        while (prop.NextVisible(prop.propertyType == SerializedPropertyType.Generic))
                        {
                            if(prop.propertyPath.EndsWith("bindData.Path", StringComparison.OrdinalIgnoreCase))
                            {
                                var bindProp = prop.GetParent();
                                typePaths.Add(bindProp.propertyPath);
                                foreach (var item in items)
                                {
                                    if (!BindDataPointsTo(bindProp, item.root, item.path))
                                    {
                                        continue;
                                    }
                                    
                                    if(cancellationToken.IsCancellationRequested)
                                    {
                                        return;
                                    }
                                    
                                    var group = item.GetGroup(groupName, groupIcon);
                                    group.Dependencies.Add(new BindDependency(item.root, component, item.path, bindProp.propertyPath.RemoveAtEnd(bindProp.name).TrimEnd('.')));
                                    
                                    await awaiter.Await();
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool BindDataPointsTo(SerializedProperty prop, Object root, string path)
        {
            var sourceProp = prop.FindPropertyRelative("Source");
            if(sourceProp.objectReferenceValue != root)
            {
                return false;
            }
            
            var pathProp = prop.FindPropertyRelative("Path");
            var pathValue = pathProp.stringValue;
            if (!IsSimilarPath(pathValue, path))
            {
                return false;
            }
            
            var isBoundPath = prop.propertyPath.ReplaceAtEnd(".bindData", "._isBound");
            var isBoundProp = prop.serializedObject.FindProperty(isBoundPath);
            
            return isBoundProp is { propertyType: SerializedPropertyType.Boolean, boolValue: true };
        }

        private static bool IsSimilarPath(string pathValue, string path)
        {
            pathValue = pathValue.Replace('.', '/');
            
            if (pathValue == path)
            {
                return true;
            }

            if (string.IsNullOrEmpty(pathValue))
            {
                return false;
            }

            if (pathValue.StartsWith("_"))
            {
                return pathValue.Substring(1) == path 
                       || (char.IsLower(pathValue[1]) && char.ToUpper(pathValue[1]) + pathValue.Substring(2) == path)
                       || (char.IsUpper(pathValue[1]) && char.ToLower(pathValue[1]) + pathValue.Substring(2) == path);
            }
            
            if(pathValue.StartsWith("m_"))
            {
                return pathValue.Substring(2) == path 
                       || (char.IsLower(pathValue[2]) && char.ToUpper(pathValue[2]) + pathValue.Substring(3) == path)
                       || (char.IsUpper(pathValue[2]) && char.ToLower(pathValue[2]) + pathValue.Substring(3) == path);
            }
            
            if("m_" + pathValue == path)
            {
                return true;
            }
            
            if("_" + pathValue == path)
            {
                return true;
            }
            
            if(char.IsUpper(pathValue[0]) && "_" + char.ToLower(pathValue[0]) + pathValue.Substring(1) == path)
            {
                return true;
            }
            
            if(char.IsLower(pathValue[0]) && "_" + char.ToUpper(pathValue[0]) + pathValue.Substring(1) == path)
            {
                return true;
            }
            
            if(char.IsUpper(pathValue[0]) && "m_" + char.ToLower(pathValue[0]) + pathValue.Substring(1) == path)
            {
                return true;
            }
            
            if(char.IsLower(pathValue[0]) && "m_" + char.ToUpper(pathValue[0]) + pathValue.Substring(1) == path)
            {
                return true;
            }
            
            return false;
        }

        public void AddToBatch(Object root, string path, Action<float> progress)
        {
            if (!_searchItems.TryGetValue((root, path), out var searchItem))
            {
                searchItem = new SearchItem(root, path, progress);
                _searchItems.Add((root, path), searchItem);
            }
            searchItem.progressCallback = progress;
        }

        public void PauseBatch() => IsBatchPaused = true;
        public void ResumeBatch() => IsBatchPaused = false;

        public void ClearBatch()
        {
            _searchItems.Clear();
            _currentBatch = null;
        }

        public async Task<IEnumerable<BindDependencyGroup>> BatchFind(Action<float> progressCallback, CancellationToken cancellationToken)
        {
            var items = _searchItems.Values.Where(i => i.state != SearchItemState.Ready).ToList();
            if (items.Count == 0)
            {
                return _searchItems.Values.SelectMany(i => i.groups.Values);
            }

            if(_currentBatch == null)
            {
                _currentBatch = items;
            }
            else
            {
                for (int i = 0; i < _currentBatch.Count; i++)
                {
                    if(_currentBatch[i].state == SearchItemState.Ready)
                    {
                        _currentBatch.RemoveAt(i--);
                    }
                }
            }
            
#if BS_DEBUG
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
#endif
            using var awaiter = new SmartAwaiter();
            var scenes = SceneManager.sceneCount;
            for (int i = 0; i < scenes; i++)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    return null;
                }
                var currentProgress = i / (float)scenes;

                void SetProgress(float progress)
                {
                    progressCallback?.Invoke(currentProgress + progress);
                }
                await SearchInScene(SceneManager.GetSceneAt(i), _currentBatch, 1f / scenes, awaiter, false, SetProgress, cancellationToken);
            }
            
            foreach (var item in items)
            {
                item.state = SearchItemState.Ready;
            }
            
            progressCallback?.Invoke(1);
            
#if BS_DEBUG
            stopWatch.Stop();
            Debug.Log(Name + $"Batch search took {stopWatch.ElapsedMilliseconds}ms");
#endif
            
            return _searchItems.Values.SelectMany(i => i.groups.Values);
        }
        
        private enum SearchItemState
        {
            Queued,
            Processing,
            Ready,
        }
        
        private class SearchItem
        {
            public readonly Object root;
            public readonly string path;
            public readonly string rawPath;
            public readonly Dictionary<string, BindDependencyGroup> groups = new();
            public Action<float> progressCallback;
            public SearchItemState state = SearchItemState.Queued;
            
            private float _progress;
            public float progress
            {
                get => _progress;
                set
                {
                    _progress = Mathf.Clamp01(value);
                    progressCallback?.Invoke(_progress);
                }
            }
            
            public BindDependencyGroup GetGroup(string name, Texture icon = null)
            {
                if (!groups.TryGetValue(name, out var group))
                {
                    group = new BindDependencyGroup(name, icon);
                    groups.Add(name, group);
                }

                return group;
            }
            
            public SearchItem(Object root, string path, Action<float> progress)
            {
                this.root = root;
                this.path = path.Replace('.', '/');
                this.rawPath = path;
                progressCallback = progress;
            }
        }
    }
}
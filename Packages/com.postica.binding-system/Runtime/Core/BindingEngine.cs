using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    /// <summary>
    /// The main binding engine that manages the update stages, bind controls and data refreshers.
    /// </summary>
    public static class BindingEngine
    {
        /// <summary>
        /// Updates all bindings related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UpdateAllBinds(this IBindController controller) => BindingEngineInternal.UpdateAllBinds(controller);
        
        /// <summary>
        /// Updates the specific binding related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="path"></param>
        /// <returns>The number of successfully updated bindings</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UpdateBind(this IBindController controller, string path) => BindingEngineInternal.UpdateBind(controller, path);
        
        /// <summary>
        /// Updates the specific bindings related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="paths">The paths to update the bindings at</param>
        /// <returns>The number of successfully updated bindings</returns>
        public static int UpdateBind(this IBindController controller, params string[] paths) => BindingEngineInternal.UpdateBind(controller, paths);
        
        /// <summary>
        /// Removes all bindings related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearAllBinds(this IBindController controller) => BindingEngineInternal.ClearAllBinds(controller);
        
        /// <summary>
        /// Pauses all bindings related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PauseAllBinds(this IBindController controller) => BindingEngineInternal.PauseAllBinds(controller);
        
        /// <summary>
        /// Resumes all bindings related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResumeAllBinds(this IBindController controller) => BindingEngineInternal.ResumeAllBinds(controller);
        
        /// <summary>
        /// Pauses the specific binding related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="path"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PauseBind(this IBindController controller, string path) => BindingEngineInternal.PauseBind(controller, path);
        
        /// <summary>
        /// Pauses the specific bindings related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="paths">The bindings paths to pause</param>
        public static void PauseBind(this IBindController controller, params string[] paths) => BindingEngineInternal.PauseBind(controller, paths);
        
        /// <summary>
        /// Resumes the specific binding related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="path"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResumeBind(this IBindController controller, string path) => BindingEngineInternal.ResumeBind(controller, path);
        
        /// <summary>
        /// Resumes the specific bindings related to this object, both if it is bound as target or as source.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="paths">The paths to resume</param>
        public static void ResumeBind(this IBindController controller, params string[] paths) => BindingEngineInternal.ResumeBind(controller, paths);
        
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UpdateAllBinds(object controller) => BindingEngineInternal.UpdateAllBinds(controller);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int UpdateBind(object controller, string path) => BindingEngineInternal.UpdateBind(controller, path);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearAllBinds(object controller) => BindingEngineInternal.ClearAllBinds(controller);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PauseAllBinds(object controller) => BindingEngineInternal.PauseAllBinds(controller);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResumeAllBinds(object controller) => BindingEngineInternal.ResumeAllBinds(controller);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PauseBind(object controller, string path) => BindingEngineInternal.PauseBind(controller, path);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResumeBind(object controller, string path) => BindingEngineInternal.ResumeBind(controller, path);
    }
    
    internal static class BindingEngineInternal
    {
        public interface IDataRefresherHandler
        {
            IReadOnlyList<IDataRefresher> AllDataRefreshers { get; }
            void Register(IDataRefresher refresher);
            void Unregister(IDataRefresher refresher);
        }
        
        public interface IDataUpdater
        {
            string Name { get; }
            IReadOnlyList<IStageData> AllDataUpdaters { get; }
        }

        public interface IStageData
        {
            int Id { get; }
            Object Context { get; }
            string StageName { get; }
            double CurrentExecutionTimeMs { get; }
            int TotalExecutionsCount { get; }
            bool IsPaused { get; set; }
            
            int UpdateFrameInterval { get; }
            float UpdateTimeInterval { get; }
            
            event Action<IStageData> OnUpdate;
        }

        internal static Action<Action> _registerToEditorUpdate;
        internal static Action<Action> _unregisterFromEditorUpdate;
        
        private static readonly Dictionary<(Type updateType, bool isPre), StageUpdater> _customStageUpdaters = new();
        
        private static readonly HashSet<BindProxy> _disabledProxies = new(64);
        private static int _disabledProxiesCount = 0;
        private static readonly object _disabledProxiesLock = new();
        private static Action _delayedDisabledProxiesUpdate;
        
        private static SynchronizationContext _unitySynchronizationContext;
        
        internal static Action<DataUpdater, BindProxy> ProxyUpdated;
        internal static Action<DataUpdater.StageData> DataUpdateRegistered;
        internal static Action<DataUpdater.StageData> DataUpdateUnregistered;

        public static readonly EditTimeUpdater UpdateAtEditTime = new();
        public static readonly PreStageUpdater<Update> PreUpdate = new();
        public static readonly PostStageUpdater<Update> PostUpdate = new();
        public static readonly PreStageUpdater<PreLateUpdate> PreLateUpdate = new();
        public static readonly PostStageUpdater<PreLateUpdate> PostLateUpdate = new();
        public static readonly PreStageUpdater<PostLateUpdate> PreRender = new();
        public static readonly PreStageUpdater<PreUpdate> PostRender = new();
        public static readonly PreStageUpdater<FixedUpdate> PreFixedUpdate = new();
        public static readonly PostStageUpdater<FixedUpdate> PostFixedUpdate = new();
        
        private static readonly Dictionary<object, BindControl> _bindControllers = new(64);
        
        private static readonly Dictionary<int, BindProxy> _allProxies = new(64);
        
#if UNITY_EDITOR
        internal static IEnumerable<BindProxy> AllProxies => _allProxies.Values;
#endif
        
        internal static bool IsProfilingEnabled { get; set; } = false;
        
        private static readonly Stopwatch ProfilerStopwatch = new();
        
        public static IDataRefresherHandler DataRefresherHandler { get; private set; } = new BindDataRefresher()
        {
            AutoRegister = false
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
            ((BindDataRefresher)DataRefresherHandler).RegisterToPlayerLoop();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        internal static void CaptureSynchronizationContext()
        {
            if (_unitySynchronizationContext != null)
            {
                return;
            }
            CaptureUnityContext.BeginCapture(OnContextCaptured);
        }

        internal static BindProxy GetProxyById(int id)
        {
            return _allProxies.GetValueOrDefault(id);
        }

        private static void DataUpdated(int id, DataUpdater stage)
        {
            if(_allProxies.TryGetValue(id, out var proxy))
            {
                ProxyUpdated?.Invoke(stage, proxy);
            }
        }

        private static void OnContextCaptured(SynchronizationContext context)
        {
            if (_unitySynchronizationContext == null)
            {
                _unitySynchronizationContext = context;
            }

            if (_delayedDisabledProxiesUpdate != null)
            {
                var delayedDisabledProxiesUpdate = _delayedDisabledProxiesUpdate;
                _delayedDisabledProxiesUpdate = null;
                _unitySynchronizationContext.Post(_ => delayedDisabledProxiesUpdate(), null);
            }
        }

        // TODO: Check if we can use OnApplicationQuit or OnDisable
        internal static void Deinitialize()
        {
            // Unregister all data refreshers
            ((BindDataRefresher)DataRefresherHandler).ForcedUnregisterFromPlayerLoop();
            // Unregister all custom stage updaters
            foreach (var updater in _customStageUpdaters.Values)
            {
                updater.Dispose();
            }
            _customStageUpdaters.Clear();
            // Unregister from player loop
            PlayerLoopHandler.Unregister<BindDataRefresher>();
        }
        
        public static void RegisterDataRefresher(IDataRefresher refresher) => DataRefresherHandler.Register(refresher);
        public static void UnregisterDataRefresher(IDataRefresher refresher) => DataRefresherHandler.Unregister(refresher);
        
        public static void RegisterDisabledProxy(BindProxy proxy)
        {
            lock (_disabledProxiesLock)
            {
                if(_disabledProxies.Add(proxy) && _disabledProxies.Count == 1)
                {
                    _disabledProxiesCount = _disabledProxies.Count;
                    
                    // Start the update loop for disabled proxies
                    if (_unitySynchronizationContext != null)
                    {
                        _unitySynchronizationContext.Post(_ => AddDisabledProxiesToTheLoop(), null);
                    }
                    else
                    {
                        _delayedDisabledProxiesUpdate = AddDisabledProxiesToTheLoop;
                    }
                }
                _disabledProxiesCount = _disabledProxies.Count;
            }
        }
        
        public static void UnregisterDisabledProxy(BindProxy proxy)
        {
            lock (_disabledProxiesLock)
            {
                _disabledProxies.Remove(proxy);
                _disabledProxiesCount = _disabledProxies.Count;
            }
        }

        private static void AddDisabledProxiesToTheLoop()
        {
            if (_disabledProxiesCount <= 0) return;
            
            lock (_disabledProxiesLock)
            {
                foreach (var disabledProxy in _disabledProxies)
                {
                    if ((disabledProxy.Provider is not Object provider || provider) && disabledProxy.OptionsValue.HasFlag(BindProxy.Options.NeedsToBeAlive))
                    {
                        disabledProxy.RegisterForUpdates();
                    }
                }

                _disabledProxies.Clear();
                _disabledProxiesCount = 0;
            }
        }

        public static void UnregisterFromAllUpdate(int id)
        {
            UpdateAtEditTime.Unregister(id);
            PreUpdate.Unregister(id);
            PreLateUpdate.Unregister(id);
            PostUpdate.Unregister(id);
            PostLateUpdate.Unregister(id);
            PreFixedUpdate.Unregister(id);
            PostFixedUpdate.Unregister(id);
        }
        
        // TODO: Implement on Changes updater

        class PlayerLoopHandler
        {

            public static bool Register<TSystem>(PlayerLoopSystem.UpdateFunction callback, params Type[] entryPoints)
            {
                var loop = PlayerLoop.GetCurrentPlayerLoop();
                var originalLoop = loop;
                // PrintPlayerLoop(loop);

                try
                {
                    bool shouldUpdateLoop = false;
                    foreach(var entryPoint in entryPoints)
                    {
                        shouldUpdateLoop |= RegisterSystem(entryPoint, typeof(TSystem), ref loop, 0, callback);
                    }

                    if (shouldUpdateLoop)
                    {
                        PlayerLoop.SetPlayerLoop(loop);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    PlayerLoop.SetPlayerLoop(originalLoop);
                }
                return false;
            }
            
            public static bool RegisterPost<TSystem>(PlayerLoopSystem.UpdateFunction callback, params Type[] entryPoints)
            {
                var loop = PlayerLoop.GetCurrentPlayerLoop();
                var originalLoop = loop;
                // PrintPlayerLoop(loop);

                try
                {
                    bool shouldUpdateLoop = false;
                    foreach(var entryPoint in entryPoints)
                    {
                        shouldUpdateLoop |= RegisterSystem(entryPoint, typeof(TSystem), ref loop, -1, callback);
                    }

                    if (shouldUpdateLoop)
                    {
                        PlayerLoop.SetPlayerLoop(loop);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    PlayerLoop.SetPlayerLoop(originalLoop);
                }
                return false;
            }

            public static bool Unregister<TSystem>() => Unregister(typeof(TSystem));

            public static bool Unregister(Type systemType)
            {
                var loop = PlayerLoop.GetCurrentPlayerLoop();
                var originalLoop = loop;

                try
                {
                    var shouldUpdateLoop = RemoveAllSystems(systemType, ref loop);

                    if (shouldUpdateLoop)
                    {
                        PlayerLoop.SetPlayerLoop(loop);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    PlayerLoop.SetPlayerLoop(originalLoop);
                }
                return false;
            }

            private static bool RegisterSystem(Type entryPointType, Type systemType, ref PlayerLoopSystem loop, int index, PlayerLoopSystem.UpdateFunction callback)
            {
                var refreshSystem = new PlayerLoopSystem()
                {
                    subSystemList = null,
                    type = systemType,
                    updateDelegate = callback,
                };

                return InsertSystem(entryPointType, ref loop, index, refreshSystem);
            }

            public static void PrintPlayerLoop()
            {
                PrintPlayerLoop(PlayerLoop.GetCurrentPlayerLoop());
            }
            
            public static void PrintPlayerLoop(PlayerLoopSystem loop)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("UNITY PLAYER LOOP");
                foreach (var subSystem in loop.subSystemList)
                {
                    PrintSubsystem(subSystem, sb, 0);
                }

                Debug.Log(sb.ToString());
            }

            private static bool InsertSystem(Type entryPointType, ref PlayerLoopSystem loop, int index, in PlayerLoopSystem systemToInsert)
            {
                if (loop.type == entryPointType)
                {
                    var list = new List<PlayerLoopSystem>();
                    if (loop.subSystemList != null)
                    {
                        list.AddRange(loop.subSystemList);
                    }

                    if (index < 0 || index >= list.Count)
                    {
                        list.Add(systemToInsert);
                    }
                    else
                    {
                        list.Insert(index, systemToInsert);
                    }

                    loop.subSystemList = list.ToArray();
                    return true;
                }

                var subSystems = loop.subSystemList;
                if (subSystems != null)
                {
                    for (int i = 0; i < subSystems.Length; i++)
                    {
                        if (InsertSystem(entryPointType, ref subSystems[i], index, systemToInsert))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private static bool RemoveSystem(Type systemType, ref PlayerLoopSystem loop)
            {
                var subSystems = loop.subSystemList;
                if (subSystems != null)
                {
                    for (int i = 0; i < subSystems.Length; i++)
                    {
                        if (subSystems[i].type == systemType)
                        {
                            var list = new List<PlayerLoopSystem>(subSystems);
                            list.RemoveAt(i);
                            loop.subSystemList = list.ToArray();
                            return true;
                        }
                        if (RemoveSystem(systemType, ref subSystems[i]))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            
            private static bool RemoveFromSystem(Type systemType, ref PlayerLoopSystem loop, PlayerLoopSystem.UpdateFunction callback)
            {
                var subSystems = loop.subSystemList;
                if (subSystems != null)
                {
                    for (int i = 0; i < subSystems.Length; i++)
                    {
                        if (subSystems[i].type == systemType)
                        {
                            var list = new List<PlayerLoopSystem>(subSystems);
                            list.RemoveAt(i);
                            loop.subSystemList = list.ToArray();
                            return true;
                        }
                        if (RemoveSystem(systemType, ref subSystems[i]))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private static bool RemoveAllSystems(Type systemType, ref PlayerLoopSystem loop)
            {
                bool success = false;
                var subSystems = loop.subSystemList;
                if (subSystems != null)
                {
                    var list = subSystems.ToList();
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (subSystems[i].type == systemType)
                        {
                            list.RemoveAt(i--);
                            success = true;
                        }
                        else
                        {
                            success |= RemoveSystem(systemType, ref subSystems[i]);
                        }
                    }
                    loop.subSystemList = list.ToArray();
                }

                return success;
            }

            // TODO: Hook this method into some update
            private static void PrintSubsystem(PlayerLoopSystem system, StringBuilder sb, int level)
            {
                sb.Append(' ', level * 4).AppendLine(system.type.ToString());
                if (system.subSystemList?.Length > 0)
                {
                    foreach (var subSystem in system.subSystemList)
                    {
                        PrintSubsystem(subSystem, sb, level + 1);
                    }
                }
            }
        }
        
        
        #region [  Custom Controls  ]
        
        private class BindControl : Dictionary<string, List<BindProxy>>
        {
            public object Controller { get; }
            public bool UpdateInProgress { get; set; } = false;
            private HashSet<(string path, BindProxy proxy)> _delayedRegisteredProxies = new(16);
            private HashSet<(string path, BindProxy proxy)> _delayedUnregisteredProxies = new(16);
            
            public BindControl(object controller)
            {
                Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            }

            public void Register(string path, BindProxy proxy)
            {
                if (UpdateInProgress)
                {
                    _delayedRegisteredProxies.Add((path, proxy));
                    _delayedUnregisteredProxies.Remove((path, proxy));
                    return;
                }
                if (TryGetValue(path, out var proxies))
                {
                    if (!proxies.Contains(proxy))
                    {
                        proxies.Add(proxy);
                    }
                }
                else
                {
                    proxies = new List<BindProxy>(16) { proxy };
                    this[path] = proxies;
                }
            }

            public void Unregister(string path, BindProxy bindProxy)
            {
                if (UpdateInProgress)
                {
                    _delayedRegisteredProxies.Remove((path, bindProxy));
                    _delayedUnregisteredProxies.Add((path, bindProxy));
                    return;
                }
                
                if (TryGetValue(path, out var proxies))
                {
                    proxies.Remove(bindProxy);
                    if (proxies.Count == 0)
                    {
                        Remove(path);
                    }
                }
            }

            public void Refresh()
            {
                if (UpdateInProgress)
                {
                    return;
                }

                if (_delayedRegisteredProxies.Count > 0)
                {
                    foreach (var (path, proxy) in _delayedRegisteredProxies)
                    {
                        Register(path, proxy);
                    }

                    _delayedRegisteredProxies.Clear();
                }

                if (_delayedUnregisteredProxies.Count > 0)
                {
                    foreach (var (path, proxy) in _delayedUnregisteredProxies)
                    {
                        Unregister(path, proxy);
                    }

                    _delayedUnregisteredProxies.Clear();
                }
            }
        }

        public static void TryRegisterBindController(BindProxy proxy)
        {
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                _allProxies[proxy.Id] = proxy;
            }
#endif

            if (proxy.Source is IBindController sourceController)
            {
                AddController(proxy.Source, proxy, proxy.RuntimePath);
            }
            if(proxy.BindData?.Source is IBindController bindDataController)
            {
                AddController(proxy.BindData.Value.Source, proxy, proxy.BindData.Value.Path);
            }

            void AddController(object controller, BindProxy bindProxy, string path)
            {
                if (!_bindControllers.TryGetValue(controller, out var control))
                {
                    control = new BindControl(controller);
                    _bindControllers[controller] = control;
                }
                control.Register(path, bindProxy);
            }
        }
        
        public static void UnregisterBindController(BindProxy proxy)
        {
            if (proxy.Source is IBindController sourceController)
            {
                RemoveController(sourceController, proxy, proxy.RuntimePath);
            }
            if(proxy.BindData?.Source is IBindController bindDataController)
            {
                RemoveController(bindDataController, proxy, proxy.BindData.Value.Path);
            }

            void RemoveController(object controller, BindProxy bindProxy, string path)
            {
                if (!_bindControllers.TryGetValue(controller, out var control))
                {
                    return;
                }
                
                control.Unregister(path, bindProxy);

                if (control.Count == 0)
                {
                    _bindControllers.Remove(controller);
                }
            }
        }

        public static int UpdateAllBinds(object controller)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return 0;
            }
            
            int updatedCount = 0;
            try
            {
                bindControl.UpdateInProgress = true;
                foreach (var proxies in bindControl.Values)
                {
                    foreach (var proxy in proxies)
                    {
                        if (proxy.TryFullUpdate())
                        {
                            updatedCount++;
                        }
                    }
                }
            }
            finally
            {
                bindControl.UpdateInProgress = false;
            }

            bindControl.Refresh();
            
            return updatedCount;
        }
        
        public static int UpdateBind(object controller, string path)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return 0;
            }
            
            if (!bindControl.TryGetValue(path, out var proxies))
            {
                return 0;
            }

            int updatedCount = 0;
            try
            {
                bindControl.UpdateInProgress = true;
                foreach (var proxy in proxies)
                {
                    if (proxy.TryFullUpdate())
                    {
                        updatedCount++;
                    }
                }
            }
            finally
            {
                bindControl.UpdateInProgress = false;
            }
            
            bindControl.Refresh();

            return updatedCount;
        }
        
        public static int UpdateBind(object controller, params string[] paths)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return 0;
            }

            int updatedCount = 0;
            try
            {
                bindControl.UpdateInProgress = true;
                foreach (var path in paths)
                {
                    if (!bindControl.TryGetValue(path, out var proxies))
                    {
                        continue;
                    }

                    foreach (var proxy in proxies)
                    {
                        if (proxy.TryFullUpdate())
                        {
                            updatedCount++;
                        }
                    }
                }
            }
            finally
            {
                bindControl.UpdateInProgress = false;
            }
            
            bindControl.Refresh();

            return updatedCount;
        }
        
        public static void ClearAllBinds(object controller)
        {
            _bindControllers.Remove(controller);
        }
        
        public static void PauseAllBinds(object controller)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return;
            }
            
            foreach (var proxies in bindControl.Values)
            {
                foreach (var proxy in proxies)
                {
                    proxy.UnregisterForUpdates();
                    proxy.IsPaused = true;
                }
            }
        }
        
        public static void ResumeAllBinds(object controller)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return;
            }
            
            foreach (var proxies in bindControl.Values)
            {
                foreach (var proxy in proxies)
                {
                    proxy.RegisterForUpdates();
                    proxy.IsPaused = false;
                }
            }
        }
        
        public static void PauseBind(object controller, string path)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return;
            }
            
            if (!bindControl.TryGetValue(path, out var proxies))
            {
                return;
            }

            foreach (var proxy in proxies)
            {
                proxy.UnregisterForUpdates();
                proxy.IsPaused = true;
            }
        }
        
        public static void PauseBind(object controller, params string[] paths)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return;
            }

            foreach (var path in paths)
            {
                if (!bindControl.TryGetValue(path, out var proxies))
                {
                    continue;
                }

                foreach (var proxy in proxies)
                {
                    proxy.UnregisterForUpdates();
                    proxy.IsPaused = false;
                }
            }
        }
        
        public static void ResumeBind(object controller, string path)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return;
            }
            
            if (!bindControl.TryGetValue(path, out var proxies))
            {
                return;
            }

            foreach (var proxy in proxies)
            {
                proxy.RegisterForUpdates();
            }
        }
        
        public static void ResumeBind(object controller, params string[] paths)
        {
            if(!_bindControllers.TryGetValue(controller, out var bindControl))
            {
                return;
            }

            foreach (var path in paths)
            {
                if (!bindControl.TryGetValue(path, out var proxies))
                {
                    continue;
                }

                foreach (var proxy in proxies)
                {
                    proxy.RegisterForUpdates();
                }
            }
        }
        
        #endregion

        private class BindDataRefresher : IDataRefresherHandler, IDisposable
        {
            private readonly List<IDataRefresher> _dataRefreshers = new(256);
            private bool _active;
            
            public bool AutoRegister { get; set; } = true;
            
            public IReadOnlyList<IDataRefresher> AllDataRefreshers => _dataRefreshers;

            public void Register(IDataRefresher refresher)
            {
                if (AutoRegister && !_active)
                {
                    RegisterToPlayerLoop();
                }

                _dataRefreshers.RemoveAll(r => r.RefreshId == refresher.RefreshId && r != refresher);
                _dataRefreshers.Add(refresher);
            }

            public void Unregister(IDataRefresher refresher)
            {
                if (_dataRefreshers.Remove(refresher))
                {
                    TryUnregisterFromPlayerLoop();
                    return;
                }

                var removedItems = _dataRefreshers.RemoveAll(r => r.RefreshId == refresher.RefreshId);
                if(removedItems > 0 && AutoRegister && _dataRefreshers.Count == 0)
                {
                    TryUnregisterFromPlayerLoop();
                }
            }

            public void RegisterToPlayerLoop()
            {
                if (_active)
                {
                    return;
                }
                _active = PlayerLoopHandler.Register<BindDataRefresher>(RefreshData, typeof(Update), typeof(PreLateUpdate));
            }

            public void TryUnregisterFromPlayerLoop()
            {
                if (AutoRegister && _dataRefreshers.Count == 0 && _active && PlayerLoopHandler.Unregister<BindDataRefresher>())
                {
                    _active = false;
                }
            }

            public void ForcedUnregisterFromPlayerLoop()
            {
                PlayerLoopHandler.Unregister<BindDataRefresher>();
            }

            internal void RefreshData()
            {
                if (_dataRefreshers.Count == 0)
                {
                    return;
                }

                if (!Application.isPlaying)
                {
                    return;
                }

                for (int i = 0; i < _dataRefreshers.Count; i++)
                {
                    var dataRefresher = _dataRefreshers[i];
                    if (dataRefresher == null)
                    {
                        _dataRefreshers.RemoveAt(i--);
                        continue;
                    }
                    var (owner, path) = dataRefresher.RefreshId;
                    if (string.IsNullOrEmpty(path) || IsObjectDead(owner) || !dataRefresher.CanRefresh())
                    {
                        _dataRefreshers.RemoveAt(i--);
                        continue;
                    }

                    try
                    {
                        dataRefresher.Refresh();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, owner);
                    }
                }
            }

            private static bool IsObjectDead(Object obj) => !ReferenceEquals(obj, null) && !obj;

            public void Dispose()
            {
                ForcedUnregisterFromPlayerLoop();
            }
        }
        
        internal abstract class DataUpdater 
#if UNITY_EDITOR
            : IDataUpdater
#endif
        {
            protected readonly List<StageData> _dataUpdaters = new(256);
            
            public abstract string Name { get; }
            
#if UNITY_EDITOR
            public IReadOnlyList<IStageData> AllDataUpdaters => _dataUpdaters;
#endif
            
            [StructLayout(LayoutKind.Sequential)]
            internal class StageData
#if UNITY_EDITOR
                : IStageData
#endif
            {
                public readonly int id;
                public readonly Func<bool> isAlive;
                public readonly Object context;
                public readonly Action onRemove;

                private readonly Action update;
                private readonly int updateFrameInterval;
                private readonly float updateTimeInterval;
                private float nextUpdateTime;
                
#if UNITY_EDITOR
                private readonly DataUpdater _owner;
                private Stopwatch _stopwatch;
                
                public int Id => id;
                public Object Context => context;
                public string StageName => _owner.Name;
                public int UpdateFrameInterval => updateFrameInterval;
                public float UpdateTimeInterval => updateTimeInterval;
                public double CurrentExecutionTimeMs { get; private set; }
                public int TotalExecutionsCount { get; private set; }
                public bool IsPaused { get; set; } = false;
                
                public event Action<IStageData> OnUpdate;
#endif
                
                public StageData(DataUpdater owner, 
                    int id, 
                    Func<bool> isAlive, 
                    Action update, 
                    Object context,
                    int updateFrameInterval,
                    float updateTimeInterval,
                    Action onRemove)
                {
                    this.id = id;
                    this.isAlive = isAlive;
                    this.update = update;
                    this.onRemove = onRemove;
                    this.context = context;
                    this.updateFrameInterval = updateFrameInterval;
                    this.updateTimeInterval = updateTimeInterval;
#if UNITY_EDITOR
                    this._owner = owner;
#endif
                    nextUpdateTime = 0;
                }
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private bool IsReadyToUpdate(float currentTime, int frameCount)
                {
                    if (updateFrameInterval > 0)
                    {
                        return frameCount % updateFrameInterval == 0;
                    }

                    if (!(updateTimeInterval > 0)) return true;
                    
                    if (currentTime < nextUpdateTime)
                    {
                        return false;
                    }
                    
                    // Update next time
                    nextUpdateTime = currentTime + updateTimeInterval;
                    return true;
                }
                
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Update(float currentTime, int frameCount)
                {
                    if (!IsReadyToUpdate(currentTime, frameCount)) return;
                    
#if UNITY_EDITOR
                    if (IsProfilingEnabled)
                    {
                        if (IsPaused)
                        {
                            return;
                        }
                        _stopwatch ??= new Stopwatch();
                        _stopwatch.Restart();
                        update();
                        _stopwatch.Stop();
                        TotalExecutionsCount++;
                        CurrentExecutionTimeMs = _stopwatch.Elapsed.TotalMilliseconds;
                        OnUpdate?.Invoke(this);
                        DataUpdated(id, _owner);
                    }
                    else
                    {
                        update();
                    }

#else
                    update();
#endif
                }
            }

            public virtual void Register(int id, 
                Func<bool> isAlive, 
                Action update, 
                Object context,
                int updateFrameInterval,
                float updateTimeInterval,
                Action onRemove)
            {
                RemoveDataUpdaters(id);

                var stageData = new StageData(this, id, isAlive, update, context, updateFrameInterval, updateTimeInterval, onRemove);
                _dataUpdaters.Add(stageData);
                
#if UNITY_EDITOR
                DataUpdateRegistered?.Invoke(stageData);
#endif
            }

            protected void RemoveDataUpdaters(int id)
            {
                for (int i = 0; i < _dataUpdaters.Count; i++)
                {
                    var dataUpdater = _dataUpdaters[i];
                    if (dataUpdater.id == id)
                    {
                        dataUpdater.onRemove?.Invoke();
                        _dataUpdaters.RemoveAt(i--);
                    }
                }
            }

            public virtual void Unregister(int id)
            {
#if UNITY_EDITOR
                if (DataUpdateUnregistered != null)
                {
                    var dataUpdaters = _dataUpdaters.FindAll(r => r.id == id);
                    foreach (var dataUpdater in dataUpdaters)
                    {
                        try
                        {
                            DataUpdateUnregistered?.Invoke(dataUpdater);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex, dataUpdater.context);
                        }
                    }
                }
#endif
                RemoveDataUpdaters(id);
            }
        }

        internal abstract class StageUpdater<T> : StageUpdater
        {
            protected StageUpdater(bool isPreStage) : base(typeof(T), isPreStage) { }
        }
        
        internal abstract class StageUpdater : DataUpdater, IDisposable
        {
            private bool _active;
            private readonly bool _isPreStage;
            private readonly Type _stageType;
            
            public override string Name => _stageType?.Name ?? "UnknownStage";

            public StageUpdater(Type stageType, bool isPreStage)
            {
                _isPreStage = isPreStage;
                _stageType = stageType;
            }
            
            public override void Register(int id, 
                Func<bool> isAlive, 
                Action update, 
                Object context,
                int updateFrameInterval,
                float updateTimeInterval,
                Action onRemove)
            {
                base.Register(id, isAlive, update, context, updateFrameInterval, updateTimeInterval, onRemove);
                RegisterToPlayerLoop();
            }

            public override void Unregister(int id)
            {
                base.Unregister(id);
                TryUnregisterFromPlayerLoop();
            }

            public void RegisterToPlayerLoop()
            {
                if (_active)
                {
                    return;
                }

                if (!Application.isPlaying)
                {
                    return;
                }
                
                _active = _isPreStage 
                        ? PlayerLoopHandler.Register<BindDataRefresher>(RefreshData, _stageType)
                        : PlayerLoopHandler.RegisterPost<BindDataRefresher>(RefreshData, _stageType);
            }

            private void TryUnregisterFromPlayerLoop()
            {
                if (_dataUpdaters.Count == 0 && _active && PlayerLoopHandler.Unregister(GetType()))
                {
                    _active = false;
                }
            }

            private void RefreshData()
            {
                if (_dataUpdaters.Count == 0)
                {
                    return;
                }

                if (!Application.isPlaying)
                {
                    return;
                }

                for (int i = 0; i < _dataUpdaters.Count; i++)
                {
                    var dataUpdater = _dataUpdaters[i];
                    if (!dataUpdater.isAlive())
                    {
                        var dataOnRemove = dataUpdater.onRemove;
                        _dataUpdaters.RemoveAt(i--);
                        dataOnRemove?.Invoke();
                        continue;
                    }

                    try
                    {
                        dataUpdater.Update(Time.time, Time.frameCount);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, dataUpdater.context);
                    }
                }
            }
            
            public void Dispose()
            {
                PlayerLoopHandler.Unregister(GetType());
            }
        }
        
        internal sealed class PreStageUpdater<T> : StageUpdater<T>
        {
            public PreStageUpdater() : base(true) { }
        }
        
        internal sealed class PostStageUpdater<T> : StageUpdater<T>
        {
            public PostStageUpdater() : base(false) { }
        }

        internal sealed class EditTimeUpdater : DataUpdater, IDisposable
        {
            private bool _active;

            public override string Name => "EditTime";

            public override void Register(int id,
                Func<bool> isAlive,
                Action update,
                Object context,
                int updateFrameInterval,
                float updateTimeInterval,
                Action onRemove)
            {
                base.Register(id, isAlive, update, context, 1, 0, onRemove);
                if (!_active && _registerToEditorUpdate != null)
                {
                    _registerToEditorUpdate(RefreshData);
                    _active = true;
                }
            }

            public override void Unregister(int id)
            {
                base.Unregister(id);
                if (_dataUpdaters.Count == 0 && _active && _unregisterFromEditorUpdate != null)
                {
                    _unregisterFromEditorUpdate(RefreshData);
                    _active = false;
                }
            }
            
            private void RefreshData()
            {
                if (_dataUpdaters.Count == 0)
                {
                    return;
                }

                if (Application.isPlaying)
                {
                    return;
                }

                for (int i = 0; i < _dataUpdaters.Count; i++)
                {
                    var dataUpdater = _dataUpdaters[i];
                    if (!dataUpdater.isAlive())
                    {
                        var dataOnRemove = dataUpdater.onRemove;
                        _dataUpdaters.RemoveAt(i--);
                        dataOnRemove?.Invoke();
                        continue;
                    }

                    try
                    {
                        dataUpdater.Update(Time.time, Time.frameCount);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex, dataUpdater.context);
                    }
                }
            }

            public void Dispose()
            {
                _unregisterFromEditorUpdate?.Invoke(RefreshData);
            }
        }
    }
}

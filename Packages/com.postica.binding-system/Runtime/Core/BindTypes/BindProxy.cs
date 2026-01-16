using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Postica.BindingSystem.Accessors;
using UnityEngine;
using Object = UnityEngine.Object;
using Postica.Common;

namespace Postica.BindingSystem
{
    [Serializable]
    [HideMember]
    internal class BindProxy : IBind, IBindData<BindData>, IBindProxy
    {
        internal delegate ref BindData GetBindDataDelegate();
        internal static Func<BindData> GetDefaultBindData;
        
        private static int GetDeterministicHashCode(params string[] strings)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int s = 0; s < strings.Length; s++)
                {
                    var str = strings[s];
                    for (int i = 0; i < str.Length; i++)
                    {
                        var c = str[i];
                        hash1 = ((hash1 << 5) + hash1) ^ c;
                        if (++i < str.Length)
                            hash2 = ((hash2 << 5) + hash2) ^ str[i];
                    }
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
        
        internal static event Action<BindProxy> OnInvalidBindProxy;
        
        [Flags]
        public enum Options
        {
            None = 0,
            MaterialProperty = 1,
            NeedsToBeAlive = 1 << 1,
        }
        
        [NonSerialized]
        private int _id;
        [SerializeField]
        private Object _proxySource;
        [SerializeField]
        private SerializedType _proxySourceType;
        [SerializeField]
        private string _proxyPath;
        [SerializeField]
        private string _runtimeProxyPath;
        [SerializeField]
        private SerializedType _proxyType;
        [SerializeField]
        private bool _isBound;
        [SerializeField]
        private Options _options;

        [SerializeField] 
        [BindTypeSource(nameof(ValueType))]
        [MultiUpdate]
        private BindData _bindData;

        [NonSerialized]
        private BindPair _proxyBind;
        [NonSerialized]
        private bool _validationInProgress;

        public int Id
        {
            get
            {
                if (_id == 0 && _proxySource != null)
                {
                    if (!(_proxySource != null))
                    {
                        Debug.LogError(BindSystem.DebugPrefix + "BindProxy Id is being set to 0 because the source is null. This should not happen.");
                        return _id = 0;
                    }
                    
                    // The id is the combined hash code of all the properties of the object
                    // _id = HashCode.Combine(_proxySource.GetScenePathOrName(), _proxyPath, _proxyType?.Name, _options);
                    var objectId = _proxySource.GetHashCode().ToString();
                    _id = GetDeterministicHashCode(objectId, _proxyPath, _proxyType?.Name, _options.ToString());
                }
                return _id;
            }
        }
        
        public Object Source { get => _proxySource; set => _proxySource = value; }

        Object IBindProxy.Source
        {
            get => _bindData.Source;
            set
            {
                if (_bindData.Source == value)
                {
                    return;
                }
                UnregisterForUpdates();
                _bindData.Source = value;
                _proxyBind = null;
                RegisterForUpdates();
            }
        }
        
        string IBindProxy.SourcePath
        {
            get => _bindData.Path;
            set
            {
                if(_bindData.Path == value)
                {
                    return;
                }
                UnregisterForUpdates();
                _bindData.Path = value;
                _proxyBind = null;
                RegisterForUpdates();
            }
        }

        Object IBindProxy.Target
        {
            get => Source;
            set
            {
                if(_proxySource == value)
                {
                    return;
                }

                if (_proxySource)
                {
                    Provider?.RemoveProxy(this);
                }

                _proxySource = value;
                if (_proxySource)
                {
                    Provider?.AddProxy(this);
                    RefreshProxy();
                }
            }
        }

        string IBindProxy.TargetPath
        {
            get => RuntimePath;
            set
            {
                if(Path == value)
                {
                    return;
                }
                UnregisterForUpdates();
                Provider.RemoveProxy(this);
                _proxyPath = value;
                _proxyBind = null;
                Provider.AddProxy(this);
                RefreshProxy();
            }
        }

        public IBindProxyProvider Provider { get; set; }

        public string Path
        {
            get => _proxyPath;
            set
            {
                if (_proxyPath == value)
                {
                    return;
                }
                _proxyPath = value;
                UpdateRuntimePath();
            }
        }
        
        public string RuntimePath => string.IsNullOrEmpty(_runtimeProxyPath) ? _proxyPath : _runtimeProxyPath;
        
        public Type SourceType { get => _proxySourceType; set => _proxySourceType = value; }
        public string SourceTypeFullName => _proxySourceType?.AssemblyQualifiedName;
        public Type ValueType { get => _proxyType; set => _proxyType = value; }
        public string ValueTypeFullName => _proxyType?.AssemblyQualifiedName;
        internal Options OptionsValue
        {
            get => _options;
            set => _options = value;
        }

        public BindData? BindData => _bindData;

        public Object Context => _proxySource ? _proxySource : _bindData.Context;
        internal Object ActualContext => _bindData.Context;
        internal string ContextPath => _bindData.ContextPath;
        internal bool IsPaused { get; set; }

        /// <summary>
        /// Whether this object should be bound to another object value or not
        /// </summary>
        public bool IsBound
        {
            get => _isBound;
            set
            {
                if (_isBound == value) return;
                
                _isBound = value;
                if (_isBound)
                {
                    BindProxyPair.WarmUp();
                    BindProxyPair.UpdateRead();
                }
                else
                {
                    BindProxyPair.RestoreUnboundValue();
                }
            }
        }

        internal BindPair BindProxyPair
        {
            get
            {
                if (_proxyBind != null) return _proxyBind;
                
                PreInitialize();
                _proxyBind = GenerateBindPair(ValueType);
                _proxyBind.Initialize(this, GetBindData);
                return _proxyBind;
            }
        }

        internal ref BindData GetBindData() => ref _bindData;

        private void PreInitialize()
        {
            if (Application.isPlaying && Source is Renderer)
            {
                if (string.IsNullOrEmpty(_runtimeProxyPath))
                {
                    _proxyPath = _proxyPath.Replace("sharedMaterial", "material");
                }
                else
                {
                    _runtimeProxyPath = _runtimeProxyPath.Replace("sharedMaterial", "material");
                }
            }

            if (Application.isEditor)
            {
                BindSystem.Options.OnOptionsChanged -= OnValidate;
                BindSystem.Options.OnOptionsChanged += OnValidate;
            }
        }

        public bool IsValid => _proxySource && !string.IsNullOrEmpty(_proxyPath);

        public void OnValidate()
        {
            if (!Application.isEditor)
            {
                Debug.LogError("OnValidate should only be called in editor mode");
                return;
            }
            
            if(GetDefaultBindData != null && _bindData.IsInitial())
            {
                _bindData.CopyFromTemplate(GetDefaultBindData());
            }

            _validationInProgress = true;
            try
            {
                RefreshProxy();

                if (Application.isPlaying && !IsBound)
                {
                    _proxyBind?.RestoreUnboundValue();
                }
            }
            finally
            {
                _validationInProgress = false;
            }
        }
        
        public void OnDestroy()
        {
            BindingEngineInternal.UnregisterBindController(this);
            BindingEngineInternal.UnregisterFromAllUpdate(Id);
            _proxyBind?.UnregisterFromValueChanged();

            if (Application.isEditor)
            {
                BindSystem.Options.OnOptionsChanged -= OnValidate;
            }
        }

        internal void RefreshProxy(bool updateRuntimePath = true, bool resetId = false)
        {
            if (resetId)
            {
                _id = 0;
            }
            
            if(_proxySource && _proxySourceType.Get() != _proxySource.GetType())
            {
                _proxySourceType = _proxySource.GetType();
            }
            
            if (updateRuntimePath)
            {
                UpdateRuntimePath();
            }

            if (_proxySource is GameObject && Path == "m_IsActive")
            {
                _options |= Options.NeedsToBeAlive;
                _isBound = true;
            }
            else if(_proxySource is Component && Path == "m_Enabled")
            {
                _isBound = true;
            }
            
            RegisterForUpdates();
        }

        internal void UpdateRuntimePath()
        {
            if(!Source)
            {
                _runtimeProxyPath = string.Empty;
                return;
            }

            if (string.IsNullOrEmpty(_proxyPath))
            {
                _runtimeProxyPath = string.Empty;
                return;
            }
                
            if(Source.GetType().TryMakeUnityRuntimePath(_proxyPath, out var runtimePath, false, deepSearch: true))
            {
                _runtimeProxyPath = runtimePath.Replace(".Array.data", "");
            }
            else if(_proxyPath.Contains(".Array.data"))
            {
                _runtimeProxyPath = _proxyPath.Replace(".Array.data", "");
            }
            else
            {
                _runtimeProxyPath = string.Empty;
            }
        }
        
        public void Update()
        {
            if (!IsBound)
            {
                return;
            }

            BindProxyPair.WarmUp();
            BindProxyPair.UpdateRead();
            BindProxyPair.UpdateWrite();
        }

        public bool TryFullUpdate()
        {
            if (!IsBound)
            {
                return false;
            }

            BindProxyPair.WarmUp();
            
            bool success = false;
            if (_bindData.Mode.CanRead())
            {
                BindProxyPair.UpdateRead();
                success = true;
            }

            if (_bindData.Mode.CanWrite())
            {
                BindProxyPair.UpdateWrite();
                success = true;
            }

            return success;
        }

        public void RegisterForUpdates()
        {
            UnregisterForUpdates();
            _proxyBind = null;

            if (!IsBound)
            {
                return;
            }
            
            if(Provider is Object p && !p)
            {
#if BS_DEBUG
                Debug.LogError(BindSystem.DebugPrefix + "Cannot register for updates. The provider is destroyed.");
#endif
                return;
            }

            try
            {
                if (Provider is not Component component || component.gameObject.scene.IsValid())
                {
                    BindingEngineInternal.TryRegisterBindController(this);
                }

                var shouldUpdateImmediately = false;
                if (Application.isEditor && _bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.UpdateInEditor))
                {
                    BindProxyPair.TryGetReadAction(out var readAction);
                    BindProxyPair.TryGetWriteAction(out var writeAction);
                    var update = readAction != null && writeAction != null
                        ? () =>
                        {
                            readAction();
                            writeAction();
                        }
                        : readAction ?? writeAction;
                    BindingEngineInternal.UpdateAtEditTime.Register(Id, BindProxyPair.GetIsAliveFunctor(), update,
                        _proxySource, 0, 0, OnUnregisteredFromUpdates);
                }

                if (_bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.UpdateOnChange))
                {
                    shouldUpdateImmediately = true;
                    BindProxyPair.RegisterToValueChanged();
                }

                // All other update points are related to runtime execution
                if (!Application.isPlaying || (Provider is Component c && !c.gameObject.scene.IsValid()))
                {
                    if (_validationInProgress && BindProxyPair == null)
                    {
                        Debug.LogError(BindSystem.DebugPrefix +
                                       "BindProxy is not initialized properly. This should not happen.");
                    }

                    return;
                }

                if (_bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.UpdateOnEnable))
                {
                    shouldUpdateImmediately = true;
                }

                if (_bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.UpdateOnUpdate))
                {
                    shouldUpdateImmediately = true;
                    RegisterReadForUpdate(BindingEngineInternal.PreUpdate);
                    RegisterWriteForUpdate(BindingEngineInternal.PostUpdate);
                }

                if (_bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.UpdateOnLateUpdate))
                {
                    shouldUpdateImmediately = true;
                    RegisterReadForUpdate(BindingEngineInternal.PreLateUpdate);
                    RegisterWriteForUpdate(BindingEngineInternal.PostLateUpdate);
                }

                if (_bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.UpdateOnPrePostRender))
                {
                    shouldUpdateImmediately = true;
                    RegisterReadForUpdate(BindingEngineInternal.PreRender);
                    RegisterWriteForUpdate(BindingEngineInternal.PostRender);
                }

                if (_bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.UpdateOnFixedUpdate))
                {
                    shouldUpdateImmediately = true;
                    RegisterReadForUpdate(BindingEngineInternal.PreFixedUpdate);
                    RegisterWriteForUpdate(BindingEngineInternal.PostFixedUpdate);
                }

                if (shouldUpdateImmediately && !IsPaused && !_validationInProgress)
                {
                    Update();
                }

                if (!_validationInProgress)
                {
                    IsPaused = false;
                }
            }
            catch (InvalidBindProxyException ex)
            {
                // Here perform either the refactoring or the removal of the invalid BindProxy
                if (OnInvalidBindProxy != null)
                {
                    OnInvalidBindProxy(this);
                }
                else
                {
                    Debug.LogError(BindSystem.DebugPrefix +
                                   "Failed to register for updates. Invalid Bind Proxy detected and will be removed.\nException: " +
                                   ex.Message);
                    Provider.RemoveProxy(this);
                }
            }
            catch (Exception e)
            {
#if BS_DEBUG
                Debug.LogError(LogMessage(e), Provider as Object);
#else
                Debug.LogError(LogMessage(e.Message), Provider as Object);
#endif

                string LogMessage(object exception)
                {
                    var isSceneObject = Source && Source.IsValidSceneObject();
                    var scene = Source is GameObject go ? go.scene :
                        Source is Component comp ? comp.gameObject.scene : default;
                    return isSceneObject 
                        ? BindSystem.DebugPrefix + $"Binding <b>{Source}.{Path}</b> at <b>{scene.name}</b>/{Source.GetScenePathOrName()} failed: {exception}"
                        : BindSystem.DebugPrefix + $"Binding <b>{Source}.{Path}</b> failed: {exception}";
                }
            }
        }

        public void UnregisterForUpdates()
        {
            BindingEngineInternal.UnregisterFromAllUpdate(Id);
            _proxyBind?.UnregisterFromValueChanged();
            if (Application.isEditor)
            {
                BindSystem.Options.OnOptionsChanged -= OnValidate;
            }
        }
        
        private void OnUnregisteredFromUpdates()
        {
            _proxyBind?.UnregisterFromValueChanged();
            if (Application.isEditor)
            {
                BindSystem.Options.OnOptionsChanged -= OnValidate;
            }
        }

        private void RegisterWriteForUpdate(BindingEngineInternal.DataUpdater updater)
        {
            if (BindProxyPair.TryGetWriteAction(out var action))
            {
                updater.Register(Id,
                    BindProxyPair.GetIsAliveFunctor(),
                    action,
                    _proxySource,
                    _bindData.UpdateFrameInterval,
                    _bindData.UpdateTimeInterval,
                    OnUnregisteredFromUpdates);
            }
        }
        
        private void RegisterReadForUpdate(BindingEngineInternal.DataUpdater updater)
        {
            if (BindProxyPair.TryGetReadAction(out var action))
            {
                updater.Register(Id,
                    BindProxyPair.GetIsAliveFunctor(),
                    action,
                    _proxySource,
                    _bindData.UpdateFrameInterval,
                    _bindData.UpdateTimeInterval,
                    OnUnregisteredFromUpdates);
            }
        }

        #region [  STATIC PART  ]
        
        private static Dictionary<(Type bindType, bool isPhased), Func<BindPair>> _bindPairGenerators = new();
        
        private static BindPair GenerateBindPair(Type type)
        {
            var isPhased = BindSystem.Options.UsePhasedUpdates;
            if (_bindPairGenerators.TryGetValue((type, isPhased), out var generator))
            {
                return generator();
            }

            var generatedType = isPhased 
                ? typeof(PhasedBindPair<>).MakeGenericType(type)
                : typeof(BindPair<>).MakeGenericType(type);
            
            generator = () => Activator.CreateInstance(generatedType) as BindPair;
            _bindPairGenerators[(type, isPhased)] = generator;
            return generator();    
        }
        
        #endregion
        
        private class InvalidBindProxyException : Exception
        {
            public InvalidBindProxyException(string message) : base(message)
            {
            }
        }
        
        public abstract class BindPair
        {
            public abstract void UpdateWrite();
            public abstract void UpdateRead();
            public abstract void RestoreUnboundValue();
            internal virtual void WarmUp() { }
            internal abstract void Initialize(BindProxy owner, GetBindDataDelegate getBindData);
            
            internal abstract bool TryGetWriteAction(out Action action);
            internal abstract bool TryGetReadAction(out Action action);
            internal abstract Func<bool> GetIsAliveFunctor();
            internal abstract void RegisterToValueChanged();
            internal abstract void UnregisterFromValueChanged();
            
#if UNITY_EDITOR
            internal abstract object SourceValue { get; }
            internal abstract object TargetValue { get; }
            internal abstract bool IsPhasedBind { get; }
#endif
        }

        private sealed class PhasedBindPair<T> : BindPair, IDataRefresher
        {
            private Object _source;
            private IAccessor<T> _sourceAccessor;
            private FastPhasedBind<T> _targetBind;
            private T _initialValue;
            private BindProxy _owner;
            private GetBindDataDelegate _getBindData;
            private bool _updateAlways;
            private bool _isOnChangeRegistered;
            private IShortCircuitConverter _shortCircuitReadConverter;

            public bool IsAlive() => _source && _targetBind.Source;
            
#if UNITY_EDITOR
            private IAccessor _targetAccessor;
            internal override object SourceValue => _sourceAccessor != null && _source ? _sourceAccessor.GetValue(_source) : default;
            internal override object TargetValue => _targetAccessor != null && _targetBind?.Source ? _targetAccessor.GetValue(_targetBind.Source) : default;
            internal override bool IsPhasedBind => true;
#endif
            
            public override void UpdateRead()
            {
                if (!_targetBind.CanRead)
                {
                    return;
                }
                DoFullRead();
            }
            
            private void DoFullRead()
            {
#if UNITY_EDITOR
                _updateAlways = !_getBindData().Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
#endif
                if (_updateAlways)
                {
                    var value = _targetBind.Value;
                    if (_shortCircuitReadConverter?.HasShortCircuited == true)
                    {
                        return;
                    }
                    _sourceAccessor.SetValue(_source, value);
                    return;
                }
                
                if (!_targetBind.Source.IsActiveInHierarchy()) return;
                
                if (!_targetBind.HasNewData()) return;

                if (_shortCircuitReadConverter?.HasShortCircuited == true)
                {
                    return;
                }
                
                _sourceAccessor.SetValue(_source, _targetBind.GetNewData());
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void DoContinuousDirectRead()
            {
                _sourceAccessor.SetValue(_source, _targetBind.Value);
            }
            
            private void DoOptimizedDirectRead()
            {
#if UNITY_EDITOR
                _updateAlways = !_getBindData().Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
                if (_updateAlways)
                {
                    _sourceAccessor.SetValue(_source, _targetBind.Value);
                    return;
                }
#endif
                if (!_targetBind.Source.IsActiveInHierarchy()) return;
                
                if (!_targetBind.HasNewData()) return;
                
                _sourceAccessor.SetValue(_source, _targetBind.GetNewData());
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void DoContinuousShortCircuitedRead()
            {
                var value = _targetBind.Value;
                if (_shortCircuitReadConverter.HasShortCircuited)
                {
                    return;
                }
                _sourceAccessor.SetValue(_source, value);
            }
            
            private void DoOptimizedShortCircuitedRead()
            {
#if UNITY_EDITOR
                _updateAlways = !_getBindData().Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
                if (_updateAlways)
                {
                    var value = _targetBind.Value;
                    if (_shortCircuitReadConverter.HasShortCircuited)
                    {
                        return;
                    }
                    _sourceAccessor.SetValue(_source, value);
                    return;
                }
#endif
                
                if (!_targetBind.Source.IsActiveInHierarchy()) return;
                
                if (!_targetBind.HasNewData()) return;

                if (_shortCircuitReadConverter.HasShortCircuited)
                {
                    return;
                }
                _sourceAccessor.SetValue(_source, _targetBind.GetNewData());
            }

            public override void UpdateWrite()
            {
                if (!_targetBind.CanWrite)
                {
                    return;
                }
                DoFullWrite();
                
#if !UNITY_EDITOR
                return;
#else
                
                if(!Application.isPlaying || !Application.isEditor || !_targetBind.IsBound)
                {
                    return;
                }
                
                ref var ownerBindData = ref _getBindData();
                
                if (ownerBindData.IsLiveDebug && ownerBindData.Mode.CanWrite() && _targetBind.BindData.HasValue)
                {
                    ownerBindData.DebugValue = _targetBind.BindData.Value.DebugValue;
                }
#endif
            }

            private void DoFullWrite()
            {
#if UNITY_EDITOR
                _updateAlways = !_getBindData().Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
#endif
                if (_updateAlways)
                {
                    _targetBind.Value = _sourceAccessor.GetValue(_source);
                    return;
                }
                
                if (!_targetBind.Source.IsActiveInHierarchy()) return;
                
                _targetBind.SetDataFast(_sourceAccessor.GetValue(_source));
            }

            public override void RestoreUnboundValue()
            {
                _sourceAccessor?.SetValue(_source, _initialValue);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal override void WarmUp() => InitializeTargetBindIfNeeded();

            internal override void Initialize(BindProxy owner, GetBindDataDelegate getBindData)
            {
                _owner = owner;
                _source = owner._proxySource;
                _updateAlways = !owner._bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
                if (_owner._bindData.ReadConverter is IShortCircuitConverter shortCircuitConverter)
                {
                    shortCircuitConverter.SetTarget(owner.Source, owner.SourceType, owner.RuntimePath);
                    _shortCircuitReadConverter = shortCircuitConverter;
                }
                _getBindData = getBindData;
                try
                {
                    _sourceAccessor = AccessorsFactory.GetAccessor<T>(_source, owner.RuntimePath);
                    if (Application.isPlaying)
                    {
                        _initialValue = _sourceAccessor.GetValue(_source);
                    }
                }
                catch (NullReferenceException)
                {
                    // Do nothing, will be handled below
                }
                catch (Exception e)
                {
#if BS_DEBUG
                    Debug.LogError(BindSystem.DebugPrefix + $"Failed to initialize BindPair. Exception: {e}", owner._proxySource);
#endif
                    throw new InvalidBindProxyException(e.Message);
                }

#if UNITY_EDITOR
                try
                {
                    _targetAccessor = AccessorsFactory.GetAccessor(owner._bindData.Source, owner._bindData.Path);
                }
                catch (Exception)
                {
                    // Nothing for now
                }
#endif
                _targetBind = new FastPhasedBind<T>(_owner._bindData);
            }

            internal override bool TryGetWriteAction(out Action action)
            {
                if (_targetBind.CanWrite)
                {
                    InitializeTargetBindIfNeeded();
                
                    action = UpdateWrite;
                    return true;
                }

                action = null;
                return false;
            }

            private void InitializeTargetBindIfNeeded()
            {
                _targetBind ??= new FastPhasedBind<T>(_owner._bindData);
                if (!_targetBind.Initialize() && Application.isPlaying)
                {
                    throw new Exception("Failed to initialize binding. Check data for correctness.");
                }
            }

            internal override bool TryGetReadAction(out Action action)
            {
                if (_targetBind.CanRead)
                {
                    InitializeTargetBindIfNeeded();
                    
                    action = Application.isEditor 
                        ? DoFullRead 
                        : (_updateAlways, _shortCircuitReadConverter) switch
                    {
                        (true, null) => DoContinuousDirectRead,
                        (false, null) => DoOptimizedDirectRead,
                        (true, _) => DoContinuousShortCircuitedRead,
                        (false, _) => DoOptimizedShortCircuitedRead,
                    };
                    return true;
                }

                action = null;
                return false;
            }

            internal override Func<bool> GetIsAliveFunctor() => IsAlive;
            
            internal override void RegisterToValueChanged()
            {
                if(_targetBind == null)
                {
                    return;
                }
                
                InitializeTargetBindIfNeeded();

                _isOnChangeRegistered = true;
                BindingEngineInternal.RegisterDataRefresher(this);
            }

            internal override void UnregisterFromValueChanged()
            {
                if(_targetBind == null)
                {
                    return;
                }
                
                if (_isOnChangeRegistered)
                {
                    BindingEngineInternal.UnregisterDataRefresher(this);
                    _isOnChangeRegistered = false;
                }
            }

            public (Object owner, string path) RefreshId => (_source, _owner.RuntimePath);

            public bool CanRefresh() => _isOnChangeRegistered && _targetBind.BindData?.IsValid == true;

            public void Refresh()
            {
                if (_targetBind.CanRead && _targetBind.HasNewData())
                {
                    _sourceAccessor.SetValue(_source, _targetBind.GetNewData());
                }

                if (_targetBind.CanWrite)
                {
                    _targetBind.SetDataFast(_sourceAccessor.GetValue(_source));
                }
            }
        }
        
        private sealed class BindPair<T> : BindPair, IDataRefresher
        {
            private Object _source;
            private IAccessor<T> _sourceAccessor;
            private Bind<T> _targetBind;
            private T _initialValue;
            private BindProxy _owner;
            private GetBindDataDelegate _getBindData;
            private T _lastValue;
            private bool _hasLastValue;
            private bool _updateAlways;
            private bool _isOnChangeRegistered;
            private IShortCircuitConverter _shortCircuitReadConverter;

            public bool IsAlive() => _source && _targetBind.Source;
            
#if UNITY_EDITOR
            private IAccessor _targetAccessor;
            internal override object SourceValue => _sourceAccessor != null && _source ? _sourceAccessor.GetValue(_source) : default;
            internal override object TargetValue => _targetAccessor != null && _targetBind?.Source ? _targetAccessor.GetValue(_targetBind.Source) : default;
            internal override bool IsPhasedBind => false;
#endif
            
            public override void UpdateRead()
            {
                if (!_targetBind.CanRead)
                {
                    return;
                }
                DoFullRead();
            }
            
            private void DoFullRead()
            {
#if UNITY_EDITOR
                _updateAlways = !_getBindData().Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
#endif
                if (_updateAlways)
                {
                    var value = _targetBind.Value;
                    if (_shortCircuitReadConverter?.HasShortCircuited == true)
                    {
                        return;
                    }
                    _sourceAccessor.SetValue(_source, value);
                    return;
                }
                
                if (!_targetBind.Source.IsActiveInHierarchy()) return;
                
                var currentValue = _targetBind.Value;
                
                if (_hasLastValue && Equals(_lastValue, currentValue)) return;

                _hasLastValue = true;
                _lastValue = currentValue;
                if (_shortCircuitReadConverter?.HasShortCircuited == true)
                {
                    return;
                }
                _sourceAccessor.SetValue(_source, currentValue);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void DoContinuousDirectRead()
            {
                _sourceAccessor.SetValue(_source, _targetBind.Value);
            }
            
            private void DoOptimizedDirectRead()
            {
#if UNITY_EDITOR
                _updateAlways = !_getBindData().Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
                if (_updateAlways)
                {
                    _sourceAccessor.SetValue(_source, _targetBind.Value);
                    return;
                }
#endif
                if (!_targetBind.Source.IsActiveInHierarchy()) return;
                
                var currentValue = _targetBind.Value;
                
                if (_hasLastValue && Equals(_lastValue, currentValue)) return;

                _hasLastValue = true;
                _lastValue = currentValue;
                _sourceAccessor.SetValue(_source, currentValue);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void DoContinuousShortCircuitedRead()
            {
                var currentValue = _targetBind.Value;
                if (_shortCircuitReadConverter.HasShortCircuited)
                {
                    return;
                }
                _sourceAccessor.SetValue(_source, currentValue);
            }
            
            private void DoOptimizedShortCircuitedRead()
            {
#if UNITY_EDITOR
                _updateAlways = !_getBindData().Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
                if (_updateAlways)
                {
                    if (_shortCircuitReadConverter.HasShortCircuited)
                    {
                        return;
                    }
                    _sourceAccessor.SetValue(_source, _targetBind.Value);
                    return;
                }
#endif
                
                if (!_targetBind.Source.IsActiveInHierarchy()) return;
                
                var currentValue = _targetBind.Value;
                
                if (_hasLastValue && Equals(_lastValue, currentValue)) return;

                _hasLastValue = true;
                _lastValue = currentValue;
                if (_shortCircuitReadConverter.HasShortCircuited)
                {
                    return;
                }
                _sourceAccessor.SetValue(_source, currentValue);
            }

            public override void UpdateWrite()
            {
                if (!_targetBind.CanWrite)
                {
                    return;
                }
                DoFullWrite();
                
#if !UNITY_EDITOR
                return;
#else
                
                if(!Application.isPlaying || !Application.isEditor || !_targetBind.IsBound)
                {
                    return;
                }
                
                ref var ownerBindData = ref _getBindData();
                
                if (ownerBindData.IsLiveDebug && ownerBindData.Mode.CanWrite() && _targetBind.BindData.HasValue)
                {
                    ownerBindData.DebugValue = _targetBind.BindData.Value.DebugValue;
                }
#endif
            }

            private void DoFullWrite()
            {
#if UNITY_EDITOR
                _updateAlways = !_getBindData().Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
#endif
                if (_updateAlways)
                {
                    _targetBind.Value = _sourceAccessor.GetValue(_source);
                    return;
                }
                
                if (!_targetBind.Source.IsActiveInHierarchy()) return;
                
                var currentValue = _sourceAccessor.GetValue(_source);
                if (_hasLastValue && Equals(_lastValue, currentValue)) return;
                
                _hasLastValue = true;
                _lastValue = currentValue;
                _targetBind.Value = currentValue;
            }

            public override void RestoreUnboundValue()
            {
                _sourceAccessor?.SetValue(_source, _initialValue);
            }

            internal override void Initialize(BindProxy owner, GetBindDataDelegate getBindData)
            {
                _owner = owner;
                _source = owner._proxySource;
                _updateAlways = !owner._bindData.Flags.HasFlag(BindingSystem.BindData.BitFlags.OptimizeUpdate);
                if (_owner._bindData.ReadConverter is IShortCircuitConverter shortCircuitConverter)
                {
                    shortCircuitConverter.SetTarget(owner.Source, owner.SourceType, owner.RuntimePath);
                    _shortCircuitReadConverter = shortCircuitConverter;
                }
                _getBindData = getBindData;
                try
                {
                    _sourceAccessor = AccessorsFactory.GetAccessor<T>(_source, owner.RuntimePath);
                    if (Application.isPlaying)
                    {
                        _initialValue = _sourceAccessor.GetValue(_source);
                    }
                }
                catch (NullReferenceException)
                {
                    // Do nothing, will be handled below
                }
                catch (Exception e)
                {
#if BS_DEBUG
                    Debug.LogError(BindSystem.DebugPrefix + $"Failed to initialize BindPair. Exception: {e}", owner._proxySource);
#endif
                    throw new InvalidBindProxyException(e.Message);
                }

#if UNITY_EDITOR
                try
                {
                    _targetAccessor = AccessorsFactory.GetAccessor(owner._bindData.Source, owner._bindData.Path);
                }
                catch (Exception)
                {
                    // Nothing for now
                }
#endif
                _targetBind = new Bind<T>(owner._bindData);
            }

            internal override bool TryGetWriteAction(out Action action)
            {
                if (_targetBind.CanWrite)
                {
                    action = UpdateWrite;
                    return true;
                }

                action = null;
                return false;
            }

            internal override bool TryGetReadAction(out Action action)
            {
                if (_targetBind.CanRead)
                {
                    action = Application.isEditor 
                        ? DoFullRead 
                        : (_updateAlways, _shortCircuitReadConverter) switch
                    {
                        (true, null) => DoContinuousDirectRead,
                        (false, null) => DoOptimizedDirectRead,
                        (true, _) => DoContinuousShortCircuitedRead,
                        (false, _) => DoOptimizedShortCircuitedRead,
                    };
                    return true;
                }

                action = null;
                return false;
            }

            internal override Func<bool> GetIsAliveFunctor() => IsAlive;
            
            internal override void RegisterToValueChanged()
            {
                if(_targetBind == null)
                {
                    return;
                }

                if (_targetBind.CanRead)
                {
                    _targetBind.ValueChanged -= OnValueChanged;
                    _targetBind.ValueChanged += OnValueChanged;
                }

                if (_targetBind.CanWrite)
                {
                    _isOnChangeRegistered = true;
                    _lastValue = _sourceAccessor.GetValue(_source);
                    BindingEngineInternal.RegisterDataRefresher(this);
                }
            }

            internal override void UnregisterFromValueChanged()
            {
                if(_targetBind == null)
                {
                    return;
                }
                _targetBind.ValueChanged -= OnValueChanged;
                if (_isOnChangeRegistered)
                {
                    BindingEngineInternal.UnregisterDataRefresher(this);
                    _isOnChangeRegistered = false;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void OnValueChanged(T oldvalue, T newvalue)
            {
                _sourceAccessor.SetValue(_source, newvalue);
            }

            public (Object owner, string path) RefreshId => (_source, _owner.RuntimePath);

            public bool CanRefresh() => _isOnChangeRegistered && _targetBind.BindData?.IsValid == true;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

            public void Refresh()
            {
                var currentValue = _sourceAccessor.GetValue(_source);
                
                if (Equals(_lastValue, currentValue)) return;
                
                _targetBind.Value = currentValue;
                _lastValue = currentValue;
            }
        }
    }
}

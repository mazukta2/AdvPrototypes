using System;
using System.ComponentModel;
using Postica.BindingSystem.Accessors;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    internal interface IBindDataDebug
    {
        Object Source { get; }
        string Path { get; }
        object DebugValue { get; }
        bool DebugValueReady { get; }
        object GetRawData();
        IAccessor GetRawAccessor();
    }

    internal interface IBindDataSimple
    {
        Object Source { get; }
        string Path { get; }
        string Id { get; }
        Object Context { get; }
        BindData.BitFlags Flags { get; }
        IConverter ReadConverter { get; }
        IConverter WriteConverter { get; }
    }

    internal interface IBindDataComplex : IBindDataSimple
    {
        int MainParameterIndex { get; }
        BindDataParameter[] Parameters { get; }
    }

    /// <summary>
    /// This struct holds all required information to identify and potentially create 
    /// <see cref="Accessors.IAccessor"/> complete with <see cref="IConverter"/>s and/or <see cref="IModifier"/>s. 
    /// <para/>
    /// The data can be serialized, essentially allowing this object to be easily persist and inspected. <br/>
    /// The inspector view of this object allows specifying all its data in a very user-friendly way.
    /// </summary>
    [Serializable]
    public struct BindData : IBindDataDebug, IBindDataComplex
    {
        /// <summary>
        /// Additional meta information on how this object should be previewed.
        /// </summary>
        [Flags]
        public enum BitFlags
        {
            /// <summary>
            /// No flags set
            /// </summary>
            None = 0,
            /// <summary>
            /// When active, the values of each stage (value fetch, conversion, modification, etc.) will be displayed on every frame 
            /// </summary>
            LiveDebug = 1 << 0,
            /// <summary>
            /// Reserved for future use
            /// </summary>
            [EditorBrowsable(EditorBrowsableState.Never)]
            __Reserved = 1 << 1,
            /// <summary>
            /// Shows the value preview of the path
            /// </summary>
            ShowPathValuePreview = 1 << 2,
            
            /// <summary>
            /// Auto updates this bind data. Implicitly enabled when subscribing to onChange events.
            /// </summary>
            AutoUpdate = 1 << 3,
            
            /// <summary>
            /// Enables unity events for this bind data
            /// </summary>
            EnableEvents = 1 << 4,
            /// <summary>
            /// When active, the bind data will be updated in the editor
            /// </summary>
            [Tooltip("When active, the bind value will be updated at edit time")]
            UpdateInEditor = 1 << 5,
            /// <summary>
            /// Update when the source value changes
            /// </summary>
            [Tooltip("Updates when the source value changes")]
            UpdateOnChange = 1 << 6,
            /// <summary>
            /// Update on update. Reads the value before update and writes updated value after update
            /// </summary>
            [Tooltip("Updates on <b>Update</b>. \n  - Reads the value <b>before</b> update\n  - Writes updated value <b>after</b> update")]
            UpdateOnUpdate = 1 << 7,
            /// <summary>
            /// Update on fixed update. Reads the value before fixed update and writes updated value after fixed update
            /// </summary>
            [Tooltip("Updates on <b>Fixed Update</b>. \n  - Reads the value <b>before</b> fixed update\n  - Writes updated value <b>after</b> fixed update")]
            UpdateOnFixedUpdate = 1 << 8,
            /// <summary>
            /// Update on late update. Reads the value before late update and writes updated value after late update
            /// </summary>
            [Tooltip("Updates on <b>Late Update</b>. \n  - Reads the value <b>before</b> late update\n  - Writes updated value <b>after</b> late update")]
            UpdateOnLateUpdate = 1 << 9,
            
            /// <summary>
            /// Update on pre/post render. Reads the value before render and writes updated value after render
            /// </summary>
            [Tooltip("Updates on <b>Render</b>. \n  - Reads the value <b>before</b> rendering the frame\n  - Writes updated value <b>immediately after</b> rendering the frame, but already in the next frame")]
            UpdateOnPrePostRender = 1 << 10,
            
            /// <summary>
            /// Whether the UI has been initialized or not
            /// </summary>
            UIInitialized = 1 << 11,
            /// <summary>
            /// If true, the source is not needed and this bind data may work without one
            /// </summary>
            SourceNotNeeded = 1 << 12,
            
            /// <summary>
            /// If true, the bind data will update also when the source is enabled.
            /// </summary>
            [Tooltip("Updates when the <b>context game object is activated</b>." +
                     "\n<b>Note:</b> for retro-compatibility reasons, all other update points except Editor will update as well when the context is activated. Use this flag to update only on enable.")]
            UpdateOnEnable = 1 << 15,
            /// <summary>
            /// If true, the bind data will update only when there are changes.
            /// </summary>
            OptimizeUpdate = 1 << 16,
            /// <summary>
            /// If true, the update points will make use of an interval when updating the value.
            /// </summary>
            UseTimeUpdateInterval = 1 << 17,
            
            /// <summary>
            /// When active in Minimal UI, the target field will be collapsed
            /// </summary>
            CompactTargetView = 1 << 21,
            /// <summary>
            /// When active in Minimal UI, the converter fields will be collapsed
            /// </summary>
            CompactConverterView = 1 << 22,
            /// <summary>
            /// When active in Minimal UI, the modifier fields will be collapsed
            /// </summary>
            CompactModifiersView = 1 << 23,
        }

        /// <summary>
        /// The source to bind
        /// </summary>
        public Object Source;
        /// <summary>
        /// The path to bind
        /// </summary>
        public string Path;
        [SerializeField]
        private BindMode _mode;
        [SerializeField]
        [NonReorderable]
        private BindDataParameter[] _parameters;
        [SerializeField]
        private int _mainParamIndex;
        [SerializeReference]
        private IConverter _readConverter;
        [SerializeReference]
        private IConverter _writeConverter;
        [SerializeReference]
        [NonReorderable]
        private IModifier[] _modifiers;
        [SerializeReference]
        [NonReorderable]
        private IModifier[] _preModifiers;
#pragma warning disable CS0414 // Remove unused private members
#pragma warning disable IDE0052 // Remove unread private members
        [SerializeField]
        [HideInInspector]
        private string _sourceType; //<-- used for editor only
        [SerializeField]
        [HideInInspector]
        private string _ppath;
        [FormerlySerializedAs("context")]
        [SerializeField]
        [HideInInspector]
        private Object _context;
        [SerializeField]
        [HideInInspector]
        private BitFlags _flags;
        [SerializeField]
        [HideInInspector]
        private float _updateInterval;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore CS0414 // Remove unused private members

        [NonSerialized]
        private object _debugValue;
        [NonSerialized]
        private bool _debugValueReady;

        /// <summary>
        /// Constructor. Builds the data with specified values.
        /// </summary>
        /// <param name="source">The source to bind to</param>
        /// <param name="path">The path to bind</param>
        /// <param name="parameters">The parameters if the path points either to an array, indexer property or method</param>
        /// <param name="mainParamIndex">The index of the parameter to be considered for write operation</param>
        /// <param name="modifiers">The modifiers, if there are any</param>
        public BindData(Object source, string path, IValueProvider[] parameters, int mainParamIndex, params IModifier[] modifiers)
        {
            Source = source;
            Path = path;
            _sourceType = source ? source.GetType().Name : null;
            _readConverter = null;
            _writeConverter = null;
            _mode = BindMode.ReadWrite;
            _modifiers = modifiers;
            _preModifiers = Array.Empty<IModifier>();
            _ppath = "";
            _mainParamIndex = mainParamIndex;
            _flags = BitFlags.None;
            _debugValue = null;
            _debugValueReady = false;
            _context = null;
            _updateInterval = 1f;

            if (parameters != null)
            {
                _parameters = new BindDataParameter[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    _parameters[i] = new BindDataParameter(parameters[i]);
                }
            }
            else
            {
                _parameters = Array.Empty<BindDataParameter>();
            }
        }

        /// <summary>
        /// Internal Constructor. Used for internal purposes only.
        /// </summary>
        /// <param name="flags"></param>
        internal BindData(BitFlags flags)
        {
            _flags = flags;
            Source = null;
            Path = "";
            _sourceType = null;
            _readConverter = null;
            _writeConverter = null;
            _mode = BindMode.Read;
            _modifiers = Array.Empty<IModifier>();
            _preModifiers = Array.Empty<IModifier>();
            _ppath = "";
            _mainParamIndex = -1;
            _debugValue = null;
            _debugValueReady = false;
            _context = null;
            _parameters = Array.Empty<BindDataParameter>();
            _updateInterval = 1f;
        }
        
        internal BitFlags SetFlags(BitFlags flags) => _flags = flags;
        
        internal float UpdateInterval
        {
            get => _updateInterval;
            set => _updateInterval = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Whether the bind is valid or not. <br/>
        /// It is only a shallow validation, it checks only if the source is set and the path is non empty. <br/>
        /// <b>It does not check</b> if the path is compatible with the source for performance reasons.
        /// </summary>
        public readonly bool IsValid => (_flags.HasFlag(BitFlags.SourceNotNeeded) || Source) && !string.IsNullOrEmpty(Path);
        /// <summary>
        /// The bind mode, that is, if the value at <see cref="Path"/> should be red, written or both.
        /// </summary>
        public BindMode Mode { get => _mode; internal set => _mode = value; }
        /// <summary>
        /// The index of the parameter used for write operations. <br/>
        /// If the path doesn't point to an array, an indexer property or a method, or there are no parameters, 
        /// this property will return -1
        /// </summary>
        public readonly int MainParameterIndex => _mainParamIndex;
        /// <summary>
        /// The parameters to get the values from.
        /// </summary>
        public readonly BindDataParameter[] Parameters => _parameters;
        /// <summary>
        /// The main parameter for write operations, if available.
        /// </summary>
        public readonly BindDataParameter MainParameter => 0 <= _mainParamIndex && _mainParamIndex < _parameters.Length
                                             ? _parameters[_mainParamIndex] : null;

        /// <summary>
        /// The <see cref="IModifier"/>s for this bind data.
        /// </summary>
        public readonly IModifier[] Modifiers => _modifiers;
        /// <summary>
        /// The <see cref="IModifier"/>s applied before conversion for this bind data.
        /// </summary>
        public readonly IModifier[] PreModifiers => _preModifiers;
        /// <summary>
        /// The <see cref="IConverter"/> used for read operations, may be null or implicit.
        /// </summary>
        public readonly IConverter ReadConverter => _readConverter;
        /// <summary>
        /// The <see cref="IConverter"/> used for write operations, may be null or implicit.
        /// </summary>
        public readonly IConverter WriteConverter => _writeConverter;
        /// <summary>
        /// The various additional options as flags. Please see <see cref="BitFlags"/> for more information.
        /// </summary>
        public readonly BitFlags Flags => _flags;
        /// <summary>
        /// Gets the raw data of this bind data. Moslty used for debug purposes.
        /// </summary>
        /// <returns></returns>
        internal readonly object GetRawData()
        {
            var accessor = GetRawAccessor();
            return accessor.GetValue(Source);
        }

        internal readonly IAccessor GetRawAccessor()
        {
            var accessor = AccessorsFactory.GetAccessor(Source, Path, _parameters.GetValues(), _mainParamIndex);
            return accessor;
        }
        
        IAccessor IBindDataDebug.GetRawAccessor() => GetRawAccessor();

        object IBindDataDebug.GetRawData() => GetRawData();

        /// <summary>
        /// Gets whether the live debug is enabled or not. <br/>
        /// When live debug is active, the values of each stage (value fetch, conversion, modification, etc.) 
        /// will be displayed on every frame, both for write and/or read operations as separate values.
        /// </summary>
        internal readonly bool IsLiveDebug => (_flags & BitFlags.LiveDebug) == BitFlags.LiveDebug;

        /// <summary>
        /// The current frame debug value. This property is loosely related to <see cref="GetRawData"/>. <br/>
        /// The difference is that this property value has at most one change per frame.
        /// </summary>
        internal object DebugValue
        {
            get => _debugValue;
            set
            {
                _debugValue = value;
                _debugValueReady = true;
            }
        }

        /// <summary>
        /// The unique Id of this bind data
        /// </summary>
        public string Id => _ppath;

        /// <summary>
        /// Which unity object has this bind data
        /// </summary>
        public Object Context => _context;

        /// <summary>
        /// Returns the update interval in frames, if it should be updated in frames.
        /// </summary>
        public int UpdateFrameInterval =>
            !_flags.HasFlag(BitFlags.UseTimeUpdateInterval) ? Mathf.CeilToInt(_updateInterval) : 0;
        
        /// <summary>
        /// Returns the update interval in seconds, if it should be updated in seconds.
        /// </summary>
        public float UpdateTimeInterval => _flags.HasFlag(BitFlags.UseTimeUpdateInterval) ? _updateInterval : 0f;
        
        /// <summary>
        /// What is the context path of this bind data
        /// </summary>
        internal string ContextPath => _ppath;

        public bool DebugValueReady => _debugValueReady;

        object IBindDataDebug.DebugValue => DebugValue;

        Object IBindDataDebug.Source => Source;

        string IBindDataDebug.Path => Path;
        
        Object IBindDataSimple.Source => Source;

        string IBindDataSimple.Path => Path;

        /// <summary>
        /// If true, shows a small preview of the path value in the inspector
        /// </summary>
        internal readonly bool IsPathPreviewEnabled => (_flags & BitFlags.ShowPathValuePreview) == BitFlags.ShowPathValuePreview;
        
        internal BindData<T> ToGeneric<T>()
        {
            return new BindData<T>(Source, _sourceType, Path, _mode, new UnityEvent<T>(), _parameters, _mainParamIndex, _readConverter, _writeConverter, _modifiers, _preModifiers, _flags, _ppath, _context);
        }

        /// <summary>
        /// Tries to enable or disable a specific flag. <br/>
        /// </summary>
        /// <param name="flag">The flag to change</param>
        /// <param name="enable">Whether to enable or disable it</param>
        /// <returns>Returns true if the flag was changed, false otherwise</returns>
        internal bool TryEnableFlag(BitFlags flag, bool enable)
        {
            if(_flags.HasFlag(flag) == enable)
            {
                return false;
            }
            
            if (enable)
            {
                _flags |= flag;
            }
            else
            {
                _flags &= ~flag;
            }
            return true;
        }

        internal bool IsInitial()
        {
            return Source == null 
                   && string.IsNullOrEmpty(Path) 
                   && _flags is BitFlags.None or BitFlags.SourceNotNeeded
                   && (_parameters == null || _parameters.Length == 0) 
                   && _readConverter == null && _writeConverter == null
                   && (_modifiers == null || _modifiers.Length == 0) 
                   && (_preModifiers == null || _preModifiers.Length == 0);
        }

        internal void CopyFromTemplate(BindData template)
        {
            _flags = template._flags;
            _updateInterval = template._updateInterval;
        }
    }

    /// <summary>
    /// This struct holds all required information to identify and potentially create 
    /// <see cref="Accessors.IAccessor"/> complete with <see cref="IConverter"/>s and/or <see cref="IModifier"/>s. 
    /// <para/>
    /// The data can be serialized, essentially allowing this object to be easily persist and inspected. <br/>
    /// The inspector view of this object allows specifying all its data in a very user-friendly way.
    /// </summary>
    [Serializable]
    public struct BindData<T> : IBindDataDebug, IBindDataComplex
    {
        /// <summary>
        /// The source to bind
        /// </summary>
        public Object Source;
        /// <summary>
        /// The path to bind
        /// </summary>
        public string Path;
        [SerializeField]
        private BindMode _mode;
        [SerializeField]
        private UnityEvent<T> _onValueChanged;
        [SerializeField]
        [NonReorderable]
        private BindDataParameter[] _parameters;
        [SerializeField]
        private int _mainParamIndex;
        [SerializeReference]
        private IConverter _readConverter;
        [SerializeReference]
        private IConverter _writeConverter;
        [SerializeReference]
        [NonReorderable]
        private IModifier[] _modifiers;
        [SerializeReference]
        [NonReorderable]
        private IModifier[] _preModifiers;

#pragma warning disable CS0414 // Remove unused private members
#pragma warning disable IDE0052 // Remove unread private members
        [SerializeField]
        [HideInInspector]
        private string _sourceType; //<-- used for editor only
        [SerializeField]
        [HideInInspector]
        private string _ppath;
        [SerializeField]
        [HideInInspector]
        private Object _context;
        [SerializeField]
        [HideInInspector]
        private BindData.BitFlags _flags;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore CS0414 // Remove unused private members

        [NonSerialized]
        private object _debugValue;
        [NonSerialized]
        private bool _debugValueReady;

        /// <summary>
        /// Constructor. Builds the data with specified values.
        /// </summary>
        /// <param name="source">The source to bind to</param>
        /// <param name="path">The path to bind</param>
        /// <param name="parameters">The parameters if the path points either to an array, indexer property or method</param>
        /// <param name="mainParamIndex">The index of the parameter to be considered for write operation</param>
        /// <param name="modifiers">The modifiers, if there are any</param>
        public BindData(Object source, string path, IValueProvider[] parameters, int mainParamIndex, params IModifier[] modifiers)
        {
            Source = source;
            Path = path;
            _sourceType = source ? source.GetType().Name : null;
            _readConverter = null;
            _writeConverter = null;
            _mode = BindMode.ReadWrite;
            _modifiers = modifiers;
            _preModifiers = Array.Empty<IModifier>();
            _ppath = "";
            _mainParamIndex = mainParamIndex;
            _flags = BindData.BitFlags.None;
            _debugValue = null;
            _debugValueReady = false;
            _context = null;

            if (parameters != null)
            {
                _parameters = new BindDataParameter[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    _parameters[i] = new BindDataParameter(parameters[i]);
                }
            }
            else
            {
                _parameters = new BindDataParameter[0];
            }
            _onValueChanged = new UnityEvent<T>();
        }
        
        // Internal constructor which has all fields as parameters
        internal BindData(Object source, string sourceType, string path, BindMode mode, UnityEvent<T> onValueChanged, BindDataParameter[] parameters, int mainParamIndex, IConverter readConverter, IConverter writeConverter, IModifier[] modifiers, IModifier[] preModifiers, BindData.BitFlags flags, string ppath, Object context)
        {
            Source = source;
            Path = path;
            _mode = mode;
            _onValueChanged = onValueChanged;
            _parameters = parameters;
            _mainParamIndex = mainParamIndex;
            _readConverter = readConverter;
            _writeConverter = writeConverter;
            _modifiers = modifiers;
            _preModifiers = preModifiers;
            _flags = flags;
            _ppath = ppath;
            _context = context;
            _debugValue = null;
            _debugValueReady = false;
            _sourceType = sourceType;
        }

        /// <summary>
        /// Whether the bind is valid or not. <br/>
        /// It is only a shallow validation, it checks only if the source is set and the path is non empty. <br/>
        /// <b>It does not check</b> if the path is compatible with the source for performance reasons.
        /// </summary>
        public readonly bool IsValid => (_flags.HasFlag(BindData.BitFlags.SourceNotNeeded) || Source) && !string.IsNullOrEmpty(Path);
        /// <summary>
        /// The bind mode, that is, if the value at <see cref="Path"/> should be red, written or both.
        /// </summary>
        public BindMode Mode { get => _mode; internal set => _mode = value; }
        /// <summary>
        /// The event for when value changes
        /// </summary>
        internal readonly UnityEvent<T> OnValueChanged => _onValueChanged;
        /// <summary>
        /// The index of the parameter used for write operations. <br/>
        /// If the path doesn't point to an array, an indexer property or a method, or there are no parameters, 
        /// this property will return -1
        /// </summary>
        public readonly int MainParameterIndex => _mainParamIndex;
        /// <summary>
        /// The parameters to get the values from.
        /// </summary>
        public readonly BindDataParameter[] Parameters => _parameters;
        /// <summary>
        /// The main parameter for write operations, if available.
        /// </summary>
        public readonly BindDataParameter MainParameter => 0 <= _mainParamIndex && _mainParamIndex < _parameters.Length 
                                             ? _parameters[_mainParamIndex] : null;

        /// <summary>
        /// The <see cref="IModifier"/>s for this bind data.
        /// </summary>
        public readonly IModifier[] Modifiers => _modifiers;
        /// <summary>
        /// The <see cref="IModifier"/>s applied before conversion for this bind data.
        /// </summary>
        public readonly IModifier[] PreModifiers => _preModifiers;
        /// <summary>
        /// The <see cref="IConverter"/> used for read operations, may be null or implicit.
        /// </summary>
        public readonly IConverter ReadConverter => _readConverter;
        /// <summary>
        /// The <see cref="IConverter"/> used for write operations, may be null or implicit.
        /// </summary>
        public readonly IConverter WriteConverter => _writeConverter;
        /// <summary>
        /// The various additional options as flags. Please see <see cref="BitFlags"/> for more information.
        /// </summary>
        public readonly BindData.BitFlags Flags => _flags;
        /// <summary>
        /// Gets the raw data of this bind data. Moslty used for debug purposes.
        /// </summary>
        /// <returns></returns>
        internal readonly object GetRawData()
        {
            var accessor = GetRawAccessor();
            return accessor.GetValue(Source);
        }

        internal readonly IAccessor GetRawAccessor()
        {
            var accessor = AccessorsFactory.GetAccessor(Source, Path, _parameters.GetValues(), _mainParamIndex);
            return accessor;
        }
        
        IAccessor IBindDataDebug.GetRawAccessor() => GetRawAccessor();
        object IBindDataDebug.GetRawData() => GetRawData();

        /// <summary>
        /// Gets whether the live debug is enabled or not. <br/>
        /// When live debug is active, the values of each stage (value fetch, conversion, modification, etc.) 
        /// will be displayed on every frame, both for write and/or read operations as separate values.
        /// </summary>
        internal readonly bool IsLiveDebug => (_flags & BindData.BitFlags.LiveDebug) == BindData.BitFlags.LiveDebug;

        /// <summary>
        /// The current frame debug value. This property is loosely related to <see cref="GetRawData"/>. <br/>
        /// The difference is that this property value has at most one change per frame.
        /// </summary>
        internal object DebugValue
        {
            get => _debugValue;
            set
            {
                _debugValue = value;
                _debugValueReady = true;
            }
        }

        /// <summary>
        /// The unique Id of this bind data
        /// </summary>
        public string Id => _ppath;
        
        /// <summary>
        /// Which unity object has this bind data. May be null
        /// </summary>
        public Object Context => _context;
        public bool DebugValueReady => _debugValueReady;

        object IBindDataDebug.DebugValue => DebugValue;

        Object IBindDataDebug.Source => Source;

        string IBindDataDebug.Path => Path;
        
        Object IBindDataSimple.Source => Source;

        string IBindDataSimple.Path => Path;

        /// <summary>
        /// If true, shows a small preview of the path value in the inspector
        /// </summary>
        internal readonly bool IsPathPreviewEnabled => (_flags & BindData.BitFlags.ShowPathValuePreview) == BindData.BitFlags.ShowPathValuePreview;

        /// <summary>
        /// True if the value changed event is enabled
        /// </summary>
        public readonly bool IsValueChangedEnabled => (_flags & BindData.BitFlags.EnableEvents) == BindData.BitFlags.EnableEvents;

        /// <summary>
        /// True if this bind data is auto updated
        /// </summary>
        public readonly bool IsAutoUpdated => (_flags & BindData.BitFlags.AutoUpdate) == BindData.BitFlags.AutoUpdate;

        /// <summary>
        /// True if this bind data has persistent events
        /// </summary>
        public readonly bool HasPersistentEvents => _onValueChanged?.GetPersistentEventCount() > 0;

        /// <summary>
        /// Transforms a specific BindData to a generic one
        /// </summary>
        /// <param name="tdata"></param>
        public static implicit operator BindData(BindData<T> tdata) => new BindData(tdata.Source, tdata.Path, tdata.Parameters, tdata.MainParameterIndex, tdata.Modifiers);

        /// <summary>
        /// Transforms a generic BindData to a specific one
        /// </summary>
        /// <param name="tdata"></param>
        public static implicit operator BindData<T>(BindData tdata) => tdata.ToGeneric<T>();
        
        /// <summary>
        /// Tries to enable or disable a specific flag. <br/>
        /// </summary>
        /// <param name="flag">The flag to change</param>
        /// <param name="enable">Whether to enable or disable it</param>
        /// <returns>Returns true if the flag was changed, false otherwise</returns>
        internal bool TryEnableFlag(BindData.BitFlags flag, bool enable)
        {
            if(_flags.HasFlag(flag) == enable)
            {
                return false;
            }
            
            if (enable)
            {
                _flags |= flag;
            }
            else
            {
                _flags &= ~flag;
            }
            return true;
        }
    }



    [Serializable]
    public class BindDataParameter : IValueProvider
    {
        [SerializeField]
        private string _typename;
        [SerializeReference]
        private object _value;
        [SerializeField]
        [BindTypeSource(nameof(_typename))]
        private ReadOnlyBindLite<Object> _unityObject;

        public object Value => !string.IsNullOrEmpty(_typename) 
            ? _unityObject 
            : _value is IValueProvider provider 
                ? provider.UnsafeValue 
                : _value;

        public object UnsafeValue => Value;

        internal BindDataParameter() { }

        public BindDataParameter(object value)
        {
            if (value is IValueProvider provider)
            {
                value = provider.UnsafeValue;
            }

            if (value is Object unityObj)
            {
                _unityObject = unityObj.Bind();
                _typename = unityObj.GetType().AssemblyQualifiedName;
            }
            else
            {
                _value = value;
            }
        }
    }



    /// <summary>
    /// This struct is used instead of <see cref="BindData"/> to avoid recursive serialization.
    /// <para/>
    /// This struct holds all required information to identify and potentially create 
    /// <see cref="Accessors.IAccessor"/> complete with <see cref="IConverter"/>s and/or <see cref="IModifier"/>s.
    /// <para/>
    /// The data can be serialized, essentially allowing this object to be easily persist and inspected. 
    /// The inspector view of this object allows specifying all its data in a very user-friendly way.
    /// </summary>
    /// <remarks>This struct <b>does not have parameters</b>, unlike the fully-fledged <see cref="BindData"/></remarks>
    [Serializable]
    public struct BindDataLite : IBindDataDebug, IBindDataSimple
    {
        /// <summary>
        /// The source to bind
        /// </summary>
        public Object Source;
        /// <summary>
        /// The path to bind
        /// </summary>
        public string Path;
        [SerializeField]
        private BindMode _mode;
        [SerializeReference]
        private IConverter _readConverter;
        [SerializeReference]
        private IConverter _writeConverter;
        [SerializeReference]
        [NonReorderable]
        private IModifier[] _modifiers;
        [SerializeReference]
        [NonReorderable]
        private IModifier[] _preModifiers;

#pragma warning disable CS0414 // Remove unused private members
#pragma warning disable IDE0052 // Remove unread private members
        [SerializeField]
        [HideInInspector]
        private string _sourceType; //<-- used for editor only
        [SerializeField]
        [HideInInspector]
        private string _ppath;
        [SerializeField]
        [HideInInspector]
        private Object context;
        [SerializeField]
        [HideInInspector]
        private BindData.BitFlags _flags;
#pragma warning restore IDE0052 // Remove unread private members
#pragma warning restore CS0414 // Remove unused private members

        [NonSerialized]
        private object _debugValue;
        [NonSerialized]
        private bool _debugValueReady;

        /// <summary>
        /// Constructor. Builds the data with specified values.
        /// </summary>
        /// <param name="source">The source to bind to</param>
        /// <param name="path">The path to bind</param>
        /// <param name="modifiers">The modifiers, if there are any</param>
        public BindDataLite(Object source, string path, params IModifier[] modifiers)
        {
            Source = source;
            Path = path;
            _sourceType = source ? source.GetType().Name : null;
            _readConverter = null;
            _writeConverter = null;
            _mode = BindMode.ReadWrite;
            _modifiers = modifiers;
            _preModifiers = Array.Empty<IModifier>();
            _ppath = "";
            _flags = BindData.BitFlags.None;
            _debugValue = null;
            _debugValueReady = false;
            context = null;
        }

        /// <summary>
        /// The unique Id of this bind data
        /// </summary>
        public string Id => _ppath;
        /// <summary>
        /// Which unity object has this bind data. May be null
        /// </summary>
        public Object Context => context;
        
        /// <summary>
        /// Whether the bind is valid or not. <br/>
        /// It is only a shallow validation, it checks only if the source is set and the path is non empty. <br/>
        /// <b>It does not check</b> if the path is compatible with the source for performance reasons.
        /// </summary>
        public readonly bool IsValid => Source && !string.IsNullOrEmpty(Path);

        /// <summary>
        /// The bind mode, that is, if the value at <see cref="Path"/> should be red, written or both.
        /// </summary>
        public BindMode Mode { get => _mode; internal set => _mode = value; }
        
        /// <summary>
        /// The <see cref="IModifier"/>s for this bind data.
        /// </summary>
        public readonly IModifier[] Modifiers => _modifiers;

        /// <summary>
        /// The <see cref="IModifier"/>s applied before conversion for this bind data.
        /// </summary>
        public readonly IModifier[] PreModifiers => _preModifiers;
        
        /// <summary>
        /// The <see cref="IConverter"/> used for read operations, may be null or implicit.
        /// </summary>
        public readonly IConverter ReadConverter => _readConverter;

        /// <summary>
        /// The <see cref="IConverter"/> used for write operations, may be null or implicit.
        /// </summary>
        public readonly IConverter WriteConverter => _writeConverter;

        /// <summary>
        /// The various additional options as flags. Please see <see cref="BindData.BitFlags"/> for more information.
        /// </summary>
        public readonly BindData.BitFlags Flags => _flags;

        /// <summary>
        /// Gets the raw data of this bind data. Moslty used for debug purposes.
        /// </summary>
        /// <returns></returns>
        internal readonly object GetRawData()
        {
            var accessor = GetRawAccessor();
            return accessor.GetValue(Source);
        }

        internal readonly IAccessor GetRawAccessor()
        {
            var accessor = AccessorsFactory.GetAccessor(Source, Path);
            return accessor;
        }
        
        IAccessor IBindDataDebug.GetRawAccessor() => GetRawAccessor();

        object IBindDataDebug.GetRawData() => GetRawData();

        /// <summary>
        /// Gets whether the live debug is enabled or not. <br/>
        /// When live debug is active, the values of each stage (value fetch, conversion, modification, etc.) 
        /// will be displayed on every frame, both for write and/or read operations as separate values.
        /// </summary>
        internal readonly bool IsLiveDebug => (_flags & BindData.BitFlags.LiveDebug) == BindData.BitFlags.LiveDebug;

        /// <summary>
        /// The current frame debug value. This property is loosely related to <see cref="GetRawData"/>. <br/>
        /// The difference is that this property value has at most one change per frame.
        /// </summary>
        internal object DebugValue
        {
            get => _debugValue;
            set
            {
                _debugValue = value;
                _debugValueReady = true;
            }
        }

        public bool DebugValueReady => _debugValueReady;

        object IBindDataDebug.DebugValue => DebugValue;

        Object IBindDataDebug.Source => Source;

        string IBindDataDebug.Path => Path;
        
        Object IBindDataSimple.Source => Source;

        string IBindDataSimple.Path => Path;
        /// <summary>
        /// True if this bind data is auto updated
        /// </summary>
        public readonly bool IsAutoUpdated => (_flags & BindData.BitFlags.AutoUpdate) == BindData.BitFlags.AutoUpdate;

    }

    internal static class BindDataExtensions
    {
        public static bool IsFlagOf(this BindData.BitFlags flags, int other)
        {
            return (other & (int)flags) == (int)flags;
        }

        public static int AddFlagIn(this BindData.BitFlags flags, int other)
        {
            return (int)flags | other;
        }

        public static int RemoveFlagIn(this BindData.BitFlags flags, int other)
        {
            return other & ~(int)flags;
        }

        public static int EnableFlagIn(this BindData.BitFlags flags, bool enable, int other)
        {
            return enable ? AddFlagIn(flags, other) : RemoveFlagIn(flags, other);
        }

        public static object[] GetValues(this BindDataParameter[] parameters)
        {
            var values = new object[parameters.Length];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = parameters[i].Value;
            }
            return values;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Postica.BindingSystem.Accessors;

using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Postica.BindingSystem.PhasedBindUtils;
using UnityEngine.Serialization;
using Postica.Common;

namespace Postica.BindingSystem
{

    namespace PhasedBindUtils
    {
        /// <summary>
        /// For internal use only. <br/>
        /// This class holds temporary objects to avoid memory allocations during bind operations.
        /// </summary>
        internal static class TempObjects
        {
            private static readonly List<IModifier>[] ModifiersLists = { new(), new(), new(), new(), new() };
            private static int _currentModifiersListIndex = -1;

            public static List<IModifier> ModifiersListClear()
            {
                _currentModifiersListIndex = (_currentModifiersListIndex + 1) % ModifiersLists.Length;
                var list = ModifiersLists[_currentModifiersListIndex];
                list.Clear();
                return list;
            }
        }

        internal class ModifierBlock
        {
            public IBind Owner;
            public IModifier Modifier;
            public Delegate SmartSetFunc;
            public bool IsReadModifier;
            
            public Action<bool> SetUpdateOnEnable;
            
            private Delegate _readDelegate;
            private Delegate _writeDelegate;
            
            public ModifierBlock AsReadModifier()
            {
                IsReadModifier = true;
                return this;
            }
            
            public ModifierBlock AsWriteModifier()
            {
                IsReadModifier = false;
                return this;
            }
            
            public ModifyDelegate<T> GetModifyDelegate<T>()
            {
                if(IsReadModifier && _readDelegate is ModifyDelegate<T> readDelegate)
                {
                    return readDelegate;
                }
                
                if(!IsReadModifier && _writeDelegate is ModifyDelegate<T> writeDelegate)
                {
                    return writeDelegate;
                }
                
                if(Modifier is IRequiresAutoUpdate { ShouldAutoUpdate: true, UpdateOnEnable: true })
                {
                    SetUpdateOnEnable?.Invoke(true);
                }
                
                (_readDelegate, _writeDelegate) = Modifier.GetBothFunc(Owner, SmartSetFunc as Action<ISmartModifier, T>);
                
                if(IsReadModifier)
                {
                    return _readDelegate as ModifyDelegate<T>;
                }
                
                if(!IsReadModifier)
                {
                    return _writeDelegate as ModifyDelegate<T>;
                }
                
                return null;
            }
        }
        
        internal abstract class Phase
        {
        }
        
        internal sealed class Phase<X> : Phase
        {
            private readonly ModifyDelegate<X> _modifier;
            
            public Phase(ModifierBlock modifier)
            {
                _modifier = modifier.GetModifyDelegate<X>();
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public X Execute(in X value)
            {
                return _modifier(value);
            }
        }
        
        internal abstract class Phase<X, Y> : Phase
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public abstract Y Execute(in X value);
        }
        
        internal sealed class FullPhase<X, Y> : Phase<X, Y>
        {
            private readonly ModifyDelegate<X> _preModifier;
            private readonly Func<X, Y> _conversion;
            private readonly ModifyDelegate<Y> _postModifier;
            
            public FullPhase(ModifierBlock preModify, Delegate conversion, ModifierBlock postModify)
            {
                _preModifier = preModify.GetModifyDelegate<X>();
                _conversion = conversion as Func<X, Y>;
                _postModifier = postModify.GetModifyDelegate<Y>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Y Execute(in X value)
            {
                return _postModifier(_conversion(_preModifier(value)));
            }
        }
        
        internal sealed class PrePhase<X, Y> : Phase<X, Y>
        {
            private readonly ModifyDelegate<X> _preModifier;
            private readonly Func<X, Y> _conversion;
            
            public PrePhase(ModifierBlock preModify, Delegate conversion)
            {
                _preModifier = preModify.GetModifyDelegate<X>();
                _conversion = conversion as Func<X, Y>;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Y Execute(in X value)
            {
                return _conversion(_preModifier(value));
            }
        }
        
        internal sealed class PostPhase<X, Y> : Phase<X, Y>
        {
            private readonly Func<X, Y> _conversion;
            private readonly ModifyDelegate<Y> _postModifier;
            
            public PostPhase(Delegate conversion, ModifierBlock postModify)
            {
                _conversion = conversion as Func<X, Y>;
                _postModifier = postModify.GetModifyDelegate<Y>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Y Execute(in X value)
            {
                return _postModifier(_conversion(value));
            }
        }
        
        internal sealed class ConversionPhase<X, Y> : Phase<X, Y>
        {
            private readonly Func<X, Y> _conversion;
            
            public ConversionPhase(Delegate conversion)
            {
                _conversion = conversion as Func<X, Y>;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override Y Execute(in X value)
            {
                return _conversion(value);
            }
        }

        internal static class Phases
        {
            public static Phase Create(Type S, Type T, ModifierBlock preModifiers, Delegate conversion,
                ModifierBlock postModifiers)
            {
                return (preModifiers != null, conversion != null, postModifiers != null) switch
                {
                    (true, true, true) => (Phase)Activator.CreateInstance(typeof(FullPhase<,>).MakeGenericType(S, T), preModifiers, conversion, postModifiers),
                    (true, true, false) => (Phase)Activator.CreateInstance(typeof(PrePhase<,>).MakeGenericType(S, T), preModifiers, conversion),
                    (false, true, true) => (Phase)Activator.CreateInstance(typeof(PostPhase<,>).MakeGenericType(S, T), conversion, postModifiers),
                    (false, true, false) => (Phase)Activator.CreateInstance(typeof(ConversionPhase<,>).MakeGenericType(S, T), conversion),
                    (true, false, false) => (Phase)Activator.CreateInstance(typeof(Phase<>).MakeGenericType(S), preModifiers),
                    (false, false, true) => (Phase)Activator.CreateInstance(typeof(Phase<>).MakeGenericType(T), postModifiers),
                    _ => null
                };
            }
        }
        
        internal struct PureValue<S, T> where S : IValueProvider<T>
        {
            private S _value;
            private T _innerValue;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetValue(in S source)
            {
                _value = source;
                _innerValue = source.Value;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(in S other)
            {
                return EqualityComparer<T>.Default.Equals(_innerValue, other.Value);
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public S GetValue() =>_value;
        }
    }

    /// <summary>
    /// This class allows binding a value to another value directly from the inspector. <br/><br/>
    /// <b>This variant of Bind is even further optimized and can operate in multiple phases to reduce overhead even more.</b>
    /// <para/>
    /// Visually this object operates in two modes, one bound to other object's value, the other not. 
    /// The first mode allows the user to select a bind source and a path, and the value
    /// returned by this object will be the value found at the source following the selected path.
    /// The second mode behaves as a normal value field where user inserts the value directly.
    /// <para/>
    /// This object is designed to operate with minimum overhead possible and reach an execution performance 
    /// close to direct access of the members while generating near-zero memory allocations (in most cases it is zero).
    /// <para/>
    /// This object implicitly converts to <typeparamref name="T"/>. 
    /// To convert a value of type <typeparamref name="T"/> to this object, use <see cref="BindExtensions.Bind{T}(T)"/>
    /// </summary>
    /// <typeparam name="T">The type to bind</typeparam>
    [Serializable]
    // [HideMember(HideMemberAttribute.Hide.InternalsOnly)]
    internal class FastPhasedBind<T> : IBind<T>, IBindData<BindData>, IValueProvider<T>, IBindAccessor, IFormattable
    {
        [SerializeField]
        [BindType]
        [BindValuesOnChange(nameof(ResetBind))]
        private BindData _bindData;

        private IAccessor _accessor;
        
        private BasePhasedReader _reader;
        private BasePhasedWriter _writer;
        
        [NonSerialized]
        private bool _writeInProgress;
        [NonSerialized]
        private bool _initialized;
        
        private static MethodInfo SetSmartModifierValueMethod => typeof(FastPhasedBind<T>).GetMethod(nameof(SetSmartModifierValue), BindingFlags.NonPublic | BindingFlags.Instance);

        private Delegate _setSmartModifierValueDelegate;
        private Delegate SetSmartModifierValueDelegate => _setSmartModifierValueDelegate ??=
            Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(typeof(ISmartModifier), typeof(T)), this, SetSmartModifierValueMethod);
        
        /// <summary>
        /// Gets the <see cref="BindData"/> if the field is bound to, null otherwise.
        /// </summary>
        [HideMember]
        public BindData? BindData => _bindData;

        /// <summary>
        /// Gets the <see cref="IAccessor"/> associated with this bind. <br/>
        /// The accessor typically handles all the heavy lifting for value read/write.
        /// </summary>
        public IAccessor<T> Accessor {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if(_accessor == null && _bindData.IsValid)
                {
                    Initialize();
                }
                return _accessor as IAccessor<T>;
            }
        }

        object IBindAccessor.RawAccessor => Accessor;

        /// <summary>
        /// Whether this object should be bound to another object value or not
        /// </summary>
        public bool IsBound
        {
            get => true;
            set { }
        }
        
        /// <summary>
        /// Whether this object can read its value or not
        /// </summary>
        public bool CanRead => _bindData.Mode.CanRead();
        /// <summary>
        /// Whether this object can write its value or not
        /// </summary>
        public bool CanWrite => _bindData.Mode.CanWrite();

        /// <summary>
        /// Gets or sets the source for the bind. <br/>
        /// Setting the value requires this bind object to have <i><see cref="IsBound"/></i> <b>to be true first</b>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is thrown if the new source value is not compatible with the existing bind path</exception>
        public Object Source 
        {
            get => _bindData.Source;
            set
            {
                if(_bindData.Source == value)
                {
                    return;
                }
                if (value is null || !_bindData.IsValid || !_initialized)
                {
                    _bindData.Source = value;
                    ResetBind();
                    return;
                }
                try
                {
                    BuildAccessor(value);
                    _bindData.Source = value;
                }
                catch(ArgumentException)
                {
                    throw new InvalidOperationException($"The Bind<{typeof(T).Name}> with path {_bindData.Path} cannot change its bound source to {value} because it is not compatible");
                }
            }
        }
        
        /// <summary>
        /// Gets or sets the value of this binder.
        /// </summary>
        public T Value {
            get => GetValue(_bindData.Source);
            set
            {
                if (_writeInProgress) return;
                
                _writeInProgress = true;
                SetValue(_bindData.Source, value);
                _writeInProgress = false;
            }
        }

        /// <inheritdoc/>
        public object UnsafeValue => Value;
        
        /// <summary>
        /// Constructor. Creates a bind field for <paramref name="target"/> at specified <paramref name="path"/>
        /// </summary>
        /// <param name="target">The object to bind to</param>
        /// <param name="path">The path to bind at</param>
        /// <param name="parameters">Parameters for the specified path. Can be either direct values or <see cref="IValueProvider"/>s</param>
        public FastPhasedBind(Object target, string path, params object[] parameters)
        {
            _bindData = new BindData<T>(target, path, parameters.ToValueProviders(), 0);
            _writeInProgress = false;
        }

        internal FastPhasedBind(in BindData data)
        {
            _bindData = data;
            _writeInProgress = false;
        }
        
        private ModifierBlock CreateModifierBlock(Type modifyType, IModifier[] modifiers, int from, int length = int.MaxValue)
        {
            if (modifiers == null)
            {
                return null;
            }
            
            var modifiersSpan = modifiers.AsSpan(from, Mathf.Min(length, modifiers.Length - from));
            var modifier = modifiersSpan.Length > 1
                ? (IModifier)Activator.CreateInstance(typeof(MultiModifier<>).MakeGenericType(modifyType), this, modifiersSpan.ToArray())
                : modifiersSpan.Length > 0 ? modifiersSpan[0] : null;

            if (modifier == null)
            {
                return null;
            }

            var block = new ModifierBlock()
            {
                Owner = this,
                Modifier = modifier,
                SmartSetFunc = SetSmartModifierValueDelegate
            };

            return block;
        }

        #region [ Phased Readers/Writers ]
        
        private abstract class BasePhasedReader
        {
            public abstract void Build(IAccessor input, Phase phase1, Phase phase2);
            public abstract T GetValue(object target);
            public abstract bool CanGetValue(object target);
            public abstract T GetRestOfValue(object target);
        }

        private sealed class DirectReader : BasePhasedReader
        {
            private IAccessor<T> _input;
            
            private T _pureValue;
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<T>;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override T GetValue(object target) => _input.GetValue(target);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool CanGetValue(object target)
            {
                var value = _input.GetValue(target);
                if (FastPhasedBind<T>.Equals(value, _pureValue))
                {
                    return false;
                }
                _pureValue = value;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override T GetRestOfValue(object target) => _pureValue;
        }

        private sealed class PhasedReaderS<S> : BasePhasedReader
        {
            private IAccessor<S> _input;
            private Phase<S> _phase1;
            private Phase<S, T> _phase2;

            private bool _isReady;
            private S _pureValue;
            private S _lastValue;

            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<S>;
                _phase1 = phase1 as Phase<S>;
                _phase2 = phase2 as Phase<S, T>;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool Equals(S a, S b) => EqualityComparer<S>.Default.Equals(a, b);
            
            public override T GetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && Equals(pureValue, _pureValue))
                {
                    return _phase2.Execute(_lastValue);
                }
                _isReady = true;
                _pureValue = pureValue;
                _lastValue = _phase1.Execute(pureValue);
                return _phase2.Execute(_lastValue);
            }

            public override bool CanGetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && Equals(pureValue, _pureValue))
                {
                    return true;
                }
                _isReady = true;
                _pureValue = pureValue;
                _lastValue = _phase1.Execute(pureValue);
                return true;
            }

            public override T GetRestOfValue(object target)
            {
                return _phase2.Execute(_lastValue);
            }
        }
        
        private sealed class PhasedReaderS_VP<S> : BasePhasedReader where S : IValueProvider<T>
        {
            private IAccessor<S> _input;
            private Phase<S> _phase1;
            private Phase<S, T> _phase2;

            private bool _isReady;
            private PureValue<S, T> _pureValue;
            private S _lastValue;

            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<S>;
                _phase1 = phase1 as Phase<S>;
                _phase2 = phase2 as Phase<S, T>;
            }

            public override T GetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && _pureValue.Equals(pureValue))
                {
                    return _phase2.Execute(_lastValue);
                }
                _isReady = true;
                _pureValue.SetValue(pureValue);
                _lastValue = _phase1.Execute(pureValue);
                return _phase2.Execute(_lastValue);
            }

            public override bool CanGetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && _pureValue.Equals(pureValue))
                {
                    return true;
                }
                _isReady = true;
                _pureValue.SetValue(pureValue);
                _lastValue = _phase1.Execute(pureValue);
                return true;
            }

            public override T GetRestOfValue(object target)
            {
                return _phase2.Execute(_lastValue);
            }
        }
        
        private class PhasedReaderT<S> : BasePhasedReader
        {
            private IAccessor<S> _input;
            private Phase<S, T> _phase1;
            private Phase<T> _phase2;
            
            private bool _isReady;
            private S _pureValue;
            private T _lastValue;
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<S>;
                _phase1 = phase1 as Phase<S, T>;
                _phase2 = phase2 as Phase<T>;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool Equals(S a, S b) => EqualityComparer<S>.Default.Equals(a, b);
            
            public override T GetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && Equals(pureValue, _pureValue))
                {
                    return _phase2.Execute(_lastValue);
                }
                _isReady = true;
                _pureValue = pureValue;
                _lastValue = _phase1.Execute(pureValue);
                return _phase2.Execute(_lastValue);
            }
            
            
            public override bool CanGetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && Equals(pureValue, _pureValue))
                {
                    return true;
                }
                _isReady = true;
                _pureValue = pureValue;
                _lastValue = _phase1.Execute(pureValue);
                return true;
            }

            public override T GetRestOfValue(object target)
            {
                return _phase2.Execute(_lastValue);
            }
        }
        
        private class PhasedReaderT_VP<S> : BasePhasedReader where S : IValueProvider<T>
        {
            private IAccessor<S> _input;
            private Phase<S, T> _phase1;
            private Phase<T> _phase2;
            
            private bool _isReady;
            private PureValue<S, T> _pureValue;
            private T _lastValue;
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<S>;
                _phase1 = phase1 as Phase<S, T>;
                _phase2 = phase2 as Phase<T>;
            }
            
            public override T GetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && _pureValue.Equals(pureValue))
                {
                    return _phase2.Execute(_lastValue);
                }
                _isReady = true;
                _pureValue.SetValue(pureValue);
                _lastValue = _phase1.Execute(pureValue);
                return _phase2.Execute(_lastValue);
            }
            
            
            public override bool CanGetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && _pureValue.Equals(pureValue))
                {
                    return true;
                }
                _isReady = true;
                _pureValue.SetValue(pureValue);
                _lastValue = _phase1.Execute(pureValue);
                return true;
            }

            public override T GetRestOfValue(object target)
            {
                return _phase2.Execute(_lastValue);
            }
        }
        
        private class PhasedReader : BasePhasedReader
        {
            private IAccessor<T> _input;
            private Phase<T> _phase1;
            private Phase<T> _phase2;
            
            private bool _isReady;
            private T _pureValue;
            private T _lastValue;
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<T>;
                _phase1 = phase1 as Phase<T>;
                _phase2 = phase2 as Phase<T>;
            }
            
            public override T GetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && FastPhasedBind<T>.Equals(pureValue, _pureValue))
                {
                    return _phase2.Execute(_lastValue);
                }
                _isReady = true;
                _pureValue = pureValue;
                _lastValue = _phase1.Execute(pureValue);
                return _phase2.Execute(_lastValue);
            }
            
            
            public override bool CanGetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && FastPhasedBind<T>.Equals(pureValue, _pureValue))
                {
                    return true;
                }
                _isReady = true;
                _pureValue = pureValue;
                _lastValue = _phase1.Execute(pureValue);
                return true;
            }

            public override T GetRestOfValue(object target)
            {
                return _phase2.Execute(_lastValue);
            }
        }
        
        private class OnePhasedReader : BasePhasedReader
        {
            private IAccessor<T> _input;
            private Phase<T> _phase1;
            
            private bool _isReady;
            private T _pureValue;
            private T _lastValue;
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<T>;
                _phase1 = phase1 as Phase<T>;
            }
            
            public override T GetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && FastPhasedBind<T>.Equals(pureValue, _pureValue))
                {
                    return _lastValue;
                }
                _isReady = true;
                _pureValue = pureValue;
                return _lastValue = _phase1.Execute(pureValue);
            }
            
            public override bool CanGetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && FastPhasedBind<T>.Equals(pureValue, _pureValue))
                {
                    return false;
                }
                _isReady = true;
                _pureValue = pureValue;
                return true;
            }

            public override T GetRestOfValue(object target)
            {
                return _phase1.Execute(_pureValue);
            }
        }
        
        private sealed class ContinuousReader : BasePhasedReader
        {
            private IAccessor<T> _input;
            private Phase<T> _phase1;
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<T>;
                _phase1 = phase1 as Phase<T>;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override T GetValue(object target) => _phase1.Execute(_input.GetValue(target));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool CanGetValue(object target) => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override T GetRestOfValue(object target) => _phase1.Execute(_input.GetValue(target));
        }
        
        private class OnePhasedReader<S> : BasePhasedReader
        {
            private IAccessor<S> _input;
            private Phase<S, T> _phase1;
            
            private bool _isReady;
            private S _pureValue;
            private T _lastValue;
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool Equals(S a, S b) => EqualityComparer<S>.Default.Equals(a, b);
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<S>;
                _phase1 = phase1 as Phase<S, T>;
            }
            
            public override T GetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && Equals(pureValue, _pureValue))
                {
                    return _lastValue;
                }
                _isReady = true;
                _pureValue = pureValue;
                return _lastValue = _phase1.Execute(pureValue);
            }
            
            public override bool CanGetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && Equals(pureValue, _pureValue))
                {
                    return false;
                }
                _isReady = true;
                _pureValue = pureValue;
                return true;
            }

            public override T GetRestOfValue(object target)
            {
                return _phase1.Execute(_pureValue);
            }
        }
        
        private class OnePhasedReader_VP<S> : BasePhasedReader where S : IValueProvider<T>
        {
            private IAccessor<S> _input;
            private Phase<S, T> _phase1;
            
            private bool _isReady;
            private PureValue<S, T> _pureValue;
            private T _lastValue;
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<S>;
                _phase1 = phase1 as Phase<S, T>;
            }
            
            public override T GetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && _pureValue.Equals(pureValue))
                {
                    return _lastValue;
                }
                _isReady = true;
                _pureValue.SetValue(pureValue);
                return _lastValue = _phase1.Execute(pureValue);
            }
            
            public override bool CanGetValue(object target)
            {
                var pureValue = _input.GetValue(target);
                if (_isReady && _pureValue.Equals(pureValue))
                {
                    return false;
                }
                _isReady = true;
                _pureValue.SetValue(pureValue);
                return true;
            }

            public override T GetRestOfValue(object target)
            {
                return _phase1.Execute(_pureValue.GetValue());
            }
        }
        
        private sealed class ContinuousReader<S> : BasePhasedReader
        {
            private IAccessor<S> _input;
            private Phase<S, T> _phase1;
            
            public override void Build(IAccessor input, Phase phase1, Phase phase2)
            {
                _input = input as IAccessor<S>;
                _phase1 = phase1 as Phase<S, T>;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override T GetValue(object target) => _phase1.Execute(_input.GetValue(target));

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool CanGetValue(object target) => true;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override T GetRestOfValue(object target) => _phase1.Execute(_input.GetValue(target));
        }

        private abstract class BasePhasedWriter
        {
            public abstract void Build(IAccessor output, Phase phase1, Phase phase2);
            public abstract void SetValue(object target, in T value, out bool hasChanged);
        }
        
        private sealed class DirectWriter : BasePhasedWriter
        {
            private IAccessor<T> _output;
            
            private T _pureValue;
            
            public override void Build(IAccessor output, Phase phase1, Phase phase2)
            {
                _output = output as IAccessor<T>;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void SetValue(object target, in T value, out bool hasChanged)
            {
                if (FastPhasedBind<T>.Equals(value, _pureValue))
                {
                    hasChanged = false;
                    return;
                }
                hasChanged = true;
                _pureValue = value;
                _output.SetValue(target, value);
            }
        }

        private sealed class PhasedWriterS<S> : BasePhasedWriter
        {
            private Phase<T, S> _phase1;
            private Phase<S> _phase2;
            private IAccessor<S> _output;
            
            private T _pureValue;
            private S _lastValue;

            public override void Build(IAccessor output, Phase phase1, Phase phase2)
            {
                _phase1 = phase2 as Phase<T, S>;
                _phase2 = phase1 as Phase<S>;
                _output = output as IAccessor<S>;
            }
            
            public override void SetValue(object target, in T value, out bool hasChanged)
            {
                if (FastPhasedBind<T>.Equals(value, _pureValue))
                {
                    hasChanged = false;
                    _output.SetValue(target, _phase2.Execute(_lastValue));
                    return;
                }
                hasChanged = true;
                _pureValue = value;
                _lastValue = _phase1.Execute(value);
                _output.SetValue(target, _phase2.Execute(_lastValue));
            }
        }
        
        private class PhasedWriterT<S> : BasePhasedWriter
        {
            private Phase<T> _phase1;
            private Phase<T, S> _phase2;
            private IAccessor<S> _output;
            
            private T _pureValue;
            private T _lastValue;
            
            public override void Build(IAccessor output, Phase phase1, Phase phase2)
            {
                _phase1 = phase2 as Phase<T>;
                _phase2 = phase1 as Phase<T, S>;
                _output = output as IAccessor<S>;
            }
            
            public override void SetValue(object target, in T value, out bool hasChanged)
            {
                if (FastPhasedBind<T>.Equals(value, _pureValue))
                {
                    hasChanged = false;
                    _output.SetValue(target, _phase2.Execute(_lastValue));
                    return;
                }
                hasChanged = true;
                _pureValue = value;
                _lastValue = _phase1.Execute(value);
                _output.SetValue(target, _phase2.Execute(_lastValue));
            }
        }
        
        private class PhasedWriter : BasePhasedWriter
        {
            private Phase<T> _phase1;
            private Phase<T> _phase2;
            private IAccessor<T> _output;
            
            private T _pureValue;
            private T _lastValue;
            
            public override void Build(IAccessor output, Phase phase1, Phase phase2)
            {
                _phase1 = phase2 as Phase<T>;
                _phase2 = phase1 as Phase<T>;
                _output = output as IAccessor<T>;
            }
            
            public override void SetValue(object target, in T value, out bool hasChanged)
            {
                if (FastPhasedBind<T>.Equals(value, _pureValue))
                {
                    hasChanged = false;
                    _output.SetValue(target, _phase2.Execute(_lastValue));
                    return;
                }
                hasChanged = true;
                _pureValue = value;
                _lastValue = _phase1.Execute(value);
                _output.SetValue(target, _phase2.Execute(_lastValue));
            }
        }
        
        private class OnePhasedWriter : BasePhasedWriter
        {
            private Phase<T> _phase1;
            private IAccessor<T> _output;
            
            private T _pureValue;
            
            public override void Build(IAccessor output, Phase phase1, Phase phase2)
            {
                _phase1 = phase1 as Phase<T>;
                _output = output as IAccessor<T>;
            }
            
            public override void SetValue(object target, in T value, out bool hasChanged)
            {
                if (FastPhasedBind<T>.Equals(value, _pureValue))
                {
                    hasChanged = false;
                    return;
                }
                hasChanged = true;
                _pureValue = value;
                _output.SetValue(target, _phase1.Execute(value));
            }
        }
        
        private sealed class ContinuousWriter : BasePhasedWriter
        {
            private Phase<T> _phase1;
            private IAccessor<T> _output;
            
            public override void Build(IAccessor output, Phase phase1, Phase phase2)
            {
                _phase1 = phase1 as Phase<T>;
                _output = output as IAccessor<T>;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void SetValue(object target, in T value, out bool hasChanged)
            {
                hasChanged = true;
                _output.SetValue(target, _phase1.Execute(value));
            }
        }
        
        private sealed class OnePhasedWriter<S> : BasePhasedWriter
        {
            private Phase<T, S> _phase1;
            private IAccessor<S> _output;
            
            private T _pureValue;
            
            public override void Build(IAccessor output, Phase phase1, Phase phase2)
            {
                _phase1 = phase1 as Phase<T, S>;
                _output = output as IAccessor<S>;
            }
            
            public override void SetValue(object target, in T value, out bool hasChanged)
            {
                if (FastPhasedBind<T>.Equals(value, _pureValue))
                {
                    hasChanged = false;
                    return;
                }
                hasChanged = true;
                _pureValue = value;
                _output.SetValue(target, _phase1.Execute(value));
            }
        }
        
        private sealed class ContinuousWriter<S> : BasePhasedWriter
        {
            private Phase<T, S> _phase1;
            private IAccessor<S> _output;
            
            public override void Build(IAccessor output, Phase phase1, Phase phase2)
            {
                _phase1 = phase1 as Phase<T, S>;
                _output = output as IAccessor<S>;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override void SetValue(object target, in T value, out bool hasChanged)
            {
                hasChanged = true;
                _output.SetValue(target, _phase1.Execute(value));
            }
        }
        
        private bool TryBuildPhasedReaderAndWriter(out BasePhasedReader reader, out BasePhasedWriter writer)
        {
            reader = null;
            writer = null;
            
            if (_accessor == null)
            {
                return false;
            }
            
            var preModifiers = _bindData.PreModifiers ?? Array.Empty<IModifier>();
            var postModifiers = _bindData.Modifiers ?? Array.Empty<IModifier>();
            var readConverter = _bindData.ReadConverter;
            var writeConverter = _bindData.WriteConverter;

            Phase phase1 = null;
            Phase phase2 = null;
            
            (readConverter, writeConverter) = AccessorsFactory.PrepareConverters(_accessor.ValueType, typeof(T), readConverter, writeConverter, _bindData.Source, _bindData.Path);

            var (readConversion, writeConversion) = ConvertersFactory.GetDelegates(readConverter, writeConverter, _accessor.ValueType, typeof(T));
            
            if(preModifiers.Length == 0
               && postModifiers.Length == 0 
               && readConversion == null 
               && writeConversion == null)
            {
                // Make direct reader/writer
                if (CanRead)
                {
                    reader = new DirectReader();
                    reader.Build(_accessor, null, null);
                }
                if (CanWrite)
                {
                    writer = new DirectWriter();
                    writer.Build(_accessor, null, null);
                }
                _reader = reader;
                _writer = writer;
                return true;
            }

            var isValueProvider = typeof(IValueProvider<T>).IsAssignableFrom(_accessor.ValueType);
            
            var dynamicIndex = -1;

            for (var i = 0; i < preModifiers.Length; i++)
            {
                var modifier = preModifiers[i];
                if (modifier is IDynamicComponent { IsDynamic: true })
                {
                    dynamicIndex = i;
                    break;
                }
            }

            if (dynamicIndex == 0)
            {
                var preModifierBlock = CreateModifierBlock(_accessor.ValueType, preModifiers, 0);
                var postModifierBlock = CreateModifierBlock(typeof(T), postModifiers, 0);
                
                if (CanRead)
                {
                    phase1 = Phases.Create(_accessor.ValueType, typeof(T), preModifierBlock?.AsReadModifier(),
                        readConversion,
                        postModifierBlock?.AsReadModifier());
                    var readerType = typeof(ContinuousReader<>).MakeGenericType(typeof(T), _accessor.ValueType);
                    reader = (BasePhasedReader)Activator.CreateInstance(readerType);
                    reader.Build(_accessor, phase1, null);
                }
                
                if (CanWrite)
                {
                    phase1 = Phases.Create(typeof(T), _accessor.ValueType, postModifierBlock?.AsWriteModifier(),
                        writeConversion,
                        preModifierBlock?.AsWriteModifier());
                    var writerType = typeof(ContinuousWriter<>).MakeGenericType(typeof(T), _accessor.ValueType);
                    writer = (BasePhasedWriter)Activator.CreateInstance(writerType);
                    writer.Build(_accessor, phase1, null);
                }
                
                _reader = reader;
                _writer = writer;
                return true;
            }

            if (dynamicIndex > 0)
            {
                var staticPreModifiers = CreateModifierBlock(_accessor.ValueType, preModifiers, 0, dynamicIndex);
                var dynamicPreModifiers = CreateModifierBlock(_accessor.ValueType, preModifiers, dynamicIndex);
                var postModifierBlock = CreateModifierBlock(typeof(T), postModifiers, 0);
                
                if (CanRead)
                {
                    phase1 = Phases.Create(_accessor.ValueType, typeof(T), staticPreModifiers?.AsReadModifier(),null, null);
                    phase2 = Phases.Create(_accessor.ValueType, typeof(T), dynamicPreModifiers?.AsReadModifier(),
                        readConversion,
                        postModifierBlock?.AsReadModifier());
                    var readerType = isValueProvider
                        ? typeof(PhasedReaderS_VP<>).MakeGenericType(typeof(T), _accessor.ValueType)
                        : typeof(PhasedReaderS<>).MakeGenericType(typeof(T), _accessor.ValueType);
                    reader = (BasePhasedReader)Activator.CreateInstance(readerType);
                    reader.Build(_accessor, phase1, phase2);
                }
                if (CanWrite)
                {
                    phase1 = Phases.Create(typeof(T), _accessor.ValueType, postModifierBlock, writeConversion, dynamicPreModifiers?.AsWriteModifier());
                    phase2 = Phases.Create(typeof(T), _accessor.ValueType, null,
                        null,
                        staticPreModifiers?.AsWriteModifier());
                    var writerType = typeof(PhasedWriterS<>).MakeGenericType(typeof(T), _accessor.ValueType);
                    writer = (BasePhasedWriter)Activator.CreateInstance(writerType);
                    writer.Build(_accessor, phase1, phase2);
                }
                _reader = reader;
                _writer = writer;
                return true;
            }

            if (CanRead && readConverter is IDynamicComponent { IsDynamic: true })
            {
                var preModifierBlock = CreateModifierBlock(_accessor.ValueType, preModifiers, 0);
                var postModifiersBlock = CreateModifierBlock(typeof(T), postModifiers, 0);
                phase1 = Phases.Create(_accessor.ValueType, typeof(T), preModifierBlock?.AsReadModifier(),
                            null,
                            null);
                phase2 = Phases.Create(_accessor.ValueType, typeof(T), null, readConversion,
                    postModifiersBlock?.AsReadModifier());
                var readerType = phase1 != null 
                    ? isValueProvider 
                        ? typeof(PhasedReaderS_VP<>).MakeGenericType(typeof(T), _accessor.ValueType) 
                        : typeof(PhasedReaderS<>).MakeGenericType(typeof(T), _accessor.ValueType)
                    : typeof(ContinuousReader<>).MakeGenericType(typeof(T), _accessor.ValueType);
                reader = (BasePhasedReader)Activator.CreateInstance(readerType);
                reader.Build(_accessor, phase1 ?? phase2, phase1 != null ? phase2 : null);
                _reader = reader;
            }
            
            if (CanWrite && writeConverter is IDynamicComponent { IsDynamic: true })
            {
                var preModifierBlock = CreateModifierBlock(_accessor.ValueType, preModifiers, 0);
                var postModifiersBlock = CreateModifierBlock(typeof(T), postModifiers, 0);
                phase1 = Phases.Create(typeof(T), _accessor.ValueType, postModifiersBlock?.AsWriteModifier(),
                            null,
                            null);
                phase2 = Phases.Create(typeof(T), _accessor.ValueType, null, writeConversion,
                    preModifierBlock?.AsWriteModifier());
                var writerType = phase1 != null
                    ? typeof(PhasedWriterS<>).MakeGenericType(typeof(T), _accessor.ValueType)
                    : typeof(ContinuousWriter<>).MakeGenericType(typeof(T), _accessor.ValueType);
                writer = (BasePhasedWriter)Activator.CreateInstance(writerType);
                writer.Build(_accessor, phase1 ?? phase2, phase1 != null ? phase2 : null);
                _writer = writer;
            }
            
            for (var i = 0; i < postModifiers.Length; i++)
            {
                var modifier = postModifiers[i];
                if (modifier is IDynamicComponent { IsDynamic: true })
                {
                    dynamicIndex = i;
                    break;
                }
            }

            if (dynamicIndex == 0)
            {
                var preModifierBlock = CreateModifierBlock(_accessor.ValueType, preModifiers, 0);
                var postModifiersBlock = CreateModifierBlock(typeof(T), postModifiers, 0);
                if (_reader == null && CanRead)
                {
                    if (readConversion != null)
                    {
                        phase1 = Phases.Create(_accessor.ValueType, typeof(T), preModifierBlock?.AsReadModifier(),
                            readConversion,
                            null);
                        phase2 = Phases.Create(_accessor.ValueType, typeof(T), null, null,
                            postModifiersBlock?.AsReadModifier());
                        var readerType = isValueProvider 
                            ? typeof(PhasedReaderT_VP<>).MakeGenericType(typeof(T), _accessor.ValueType)
                            : typeof(PhasedReaderT<>).MakeGenericType(typeof(T), _accessor.ValueType);
                        reader = (BasePhasedReader)Activator.CreateInstance(readerType);
                        reader.Build(_accessor, phase1, phase2);
                    }
                    else
                    {
                        phase1 = Phases.Create(_accessor.ValueType, typeof(T), null,
                            null,
                            postModifiersBlock?.AsReadModifier());
                        reader = new ContinuousReader();
                        reader.Build(_accessor, phase1, null);
                    }
                }
                if (_writer == null && CanWrite)
                {
                    if (writeConversion != null)
                    {
                        phase1 = Phases.Create(typeof(T), _accessor.ValueType, postModifiersBlock?.AsWriteModifier(),
                            null, null);
                        phase2 = Phases.Create(typeof(T), _accessor.ValueType, null, writeConversion,
                            preModifierBlock?.AsWriteModifier());
                        var writerType = typeof(PhasedWriterT<>).MakeGenericType(typeof(T), _accessor.ValueType);
                        writer = (BasePhasedWriter)Activator.CreateInstance(writerType);
                        writer.Build(_accessor, phase1, phase2);
                    }
                    else
                    {
                        phase1 = Phases.Create(typeof(T), _accessor.ValueType, postModifiersBlock?.AsWriteModifier(),
                            null,
                            null);
                        writer = new ContinuousWriter();
                        writer.Build(_accessor, phase1, null);
                    }
                }
                
                _reader ??= reader;
                _writer ??= writer;
                return true;
            }

            if (dynamicIndex > 0)
            {
                var preModifierBlock = CreateModifierBlock(_accessor.ValueType, preModifiers, 0);
                var staticPostModifiers = CreateModifierBlock(typeof(T), postModifiers, 0, dynamicIndex);
                var dynamicPostModifiers = CreateModifierBlock(typeof(T), postModifiers, dynamicIndex);
                if (_reader == null && CanRead)
                {
                    phase1 = Phases.Create(_accessor.ValueType, typeof(T), preModifierBlock?.AsReadModifier(),
                        readConversion,
                        staticPostModifiers?.AsReadModifier());
                    phase2 = Phases.Create(_accessor.ValueType, typeof(T), null, null, dynamicPostModifiers?.AsReadModifier());
                    var readerType = readConversion != null 
                        ? isValueProvider 
                            ? typeof(PhasedReaderS_VP<>).MakeGenericType(typeof(T), _accessor.ValueType) 
                            : typeof(PhasedReaderS<>).MakeGenericType(typeof(T), _accessor.ValueType)
                        : typeof(PhasedReader);
                    reader = (BasePhasedReader)Activator.CreateInstance(readerType);
                    reader.Build(_accessor, phase1, phase2);
                }

                if (_writer == null && CanWrite)
                {
                    phase1 = Phases.Create(typeof(T), _accessor.ValueType, dynamicPostModifiers?.AsWriteModifier(),
                        null,
                        null);
                    phase2 = Phases.Create(typeof(T), _accessor.ValueType, staticPostModifiers?.AsWriteModifier(), writeConversion,
                        preModifierBlock?.AsWriteModifier());
                    writer = new PhasedWriter();
                    writer.Build(_accessor, phase1, phase2);
                }
                
                _reader ??= reader;
                _writer ??= writer;
                return true;
            }
            
            // Here we have only static modifiers and/or conversions
            var preModifierBlockFinal = CreateModifierBlock(_accessor.ValueType, preModifiers, 0);
            var postModifierBlockFinal = CreateModifierBlock(typeof(T), postModifiers, 0);
            if (_reader == null && CanRead)
            {
                phase1 = Phases.Create(_accessor.ValueType, typeof(T), preModifierBlockFinal?.AsReadModifier(),
                    readConversion,
                    postModifierBlockFinal?.AsReadModifier());
                var readerType = readConversion != null 
                    ? isValueProvider 
                        ? typeof(OnePhasedReader_VP<>).MakeGenericType(typeof(T), _accessor.ValueType) 
                        : typeof(OnePhasedReader<>).MakeGenericType(typeof(T), _accessor.ValueType)
                    : typeof(OnePhasedReader);
                reader = (BasePhasedReader)Activator.CreateInstance(readerType);
                reader.Build(_accessor, phase1, null);
            }
            
            if (_writer == null && CanWrite)
            {
                phase1 = Phases.Create(typeof(T), _accessor.ValueType, postModifierBlockFinal?.AsWriteModifier(),
                    writeConversion,
                    preModifierBlockFinal?.AsWriteModifier());
                var writerType = writeConversion != null 
                    ? typeof(OnePhasedWriter<>).MakeGenericType(typeof(T), _accessor.ValueType)
                    : typeof(OnePhasedWriter);
                writer = (BasePhasedWriter)Activator.CreateInstance(writerType);
                writer.Build(_accessor, phase1, null);
            }
            
            _reader ??= reader;
            _writer ??= writer;

            return true;
        }
        
        #endregion

        internal bool Initialize()
        {
            if (_initialized)
            {
                return true;
            }
            
            if (!_bindData.IsValid)
            {
                return false;
            }
            
            _initialized = true;
            // Find the first dynamic component between pre-modifiers, conversions and modifiers and split there the phases
            if(!BuildAccessor(_bindData.Source))
            {
                return false;
            }

            return TryBuildPhasedReaderAndWriter(out _reader, out _writer);
        }
        
        private void SetSmartModifierValue(ISmartModifier modifier, T value)
        {
            if (!_accessor.CanWrite)
            {
                return;
            }
            ((IAccessor<T>)_accessor).SetValue(_bindData.Source, value);
        }

        private bool BuildAccessor(object source)
        {
            if(source == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_bindData.Path))
            {
                _accessor = AccessorsFactory.GetAccessor(source,
                                                            _bindData.Path,
                                                            typeof(T),
                                                            _bindData.Parameters.GetValues(),
                                                            _bindData.MainParameterIndex);
                return _accessor != null;
            }

            // We do not allow self references in write mode
            if(_bindData.Mode != BindMode.Read)
            {
                throw new InvalidOperationException($"{GetType().Name}<{typeof(T).Name}> cannot operate on self reference, the Bind Mode should be read-only");
            }

            if(typeof(T) == typeof(bool) && source is Object)
            {
                _accessor = new UnityObjectToBoolAccessor(this);
            }

            return _accessor != null;
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasNewData() => _reader.CanGetValue(_bindData.Source);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetNewData() => _reader.GetRestOfValue(_bindData.Source);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetDataFast(in T value)
        {
            if (_writeInProgress) return;
                
            _writeInProgress = true;
            
            if (Application.isEditor && _bindData.IsLiveDebug)
            {
                _bindData.DebugValue = value;
            }
            
            _writer.SetValue(_bindData.Source, value, out _);
            _writeInProgress = false;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(object target)
        {
            if(_bindData.Mode == BindMode.Write)
            {
                return !_accessor.CanRead ? throw new InvalidOperationException($"The Bind<{typeof(T).Name}> at path {_bindData.Path} is not read enabled") : default;
            }
            
            return _reader.GetValue(target);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValueUnsafe(object target)
        {
            if (!_accessor.CanRead)
            {
                return default;
            }
            
            return _reader.GetValue(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(object target, in T value)
        {
            if (_bindData.Mode == BindMode.Read)
            {
                if (_accessor.CanWrite)
                {
                    if (Application.isEditor && _bindData.IsLiveDebug)
                    {
                        _bindData.DebugValue = value;
                    }
                    return;
                }
                
                throw new InvalidOperationException(
                    $"The Bind<{typeof(T).Name}> at path {_bindData.Path} is not write enabled");
            }

            if (Application.isEditor && _bindData.IsLiveDebug)
            {
                _bindData.DebugValue = value;
            }
            
            _writer.SetValue(target, value, out _);
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T PipeValue(T value)
        {
            Value = value;
            return value;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public T StoreValueTo(ref T field)
        {
            var value = Value;
            field = value;
            return value;
        }

        private void ResetBind()
        {
            _accessor = null;
            _initialized = false;
        }

        public override string ToString()
        {
            if (Application.isPlaying)
            {
                return $"{_bindData.FullPath()}: {GetValueUnsafe(_bindData.Source)}";
            }
            return _bindData.FullPath();
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            var value = Value;
            if(value is IFormattable formattable)
            {
                return formattable.ToString(format, formatProvider);
            }
            return value?.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

        public static implicit operator T(FastPhasedBind<T> binder) => binder.Value;
        public static explicit operator FastPhasedBind<T>(ReadOnlyBind<T> binder)
        {
            var bindData = binder.BindData;
            if (bindData == null)
            {
                return new FastPhasedBind<T>(null, string.Empty);
            }
            else
            {
                return new FastPhasedBind<T>(bindData.Value.Source, bindData.Value.Path);
            }
        }
    }
}

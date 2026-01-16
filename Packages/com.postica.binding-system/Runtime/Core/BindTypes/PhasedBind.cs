using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Postica.BindingSystem.Accessors;

using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using Postica.BindingSystem.PhasedBindUtils;
using UnityEngine.Serialization;
using Postica.Common;

namespace Postica.BindingSystem
{
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
    public class PhasedBind<T> : IBind<T>, IBindData<BindData<T>>, IValueProvider<T>, IBindAccessor, IDataRefresher, IDisposable, ISerializationCallbackReceiver, IFormattable
    {
        [SerializeField]
        [HideMember]
        [BindType]
        [BindValuesOnChange(nameof(ResetBind))]
        private BindData<T> _bindData;
        [SerializeField]
        [HideMember]
        [FormerlySerializedAs("_isBinded")]
        private bool _isBound;
        [SerializeField]
        [HideMember]
        private T _value;

        private IAccessor _accessor;
        
        private BasePhasedReader _reader;
        private BasePhasedWriter _writer;
        
        [NonSerialized]
        private bool _writeInProgress;
        [NonSerialized]
        private bool _initialized;

        [NonSerialized]
        private bool _wasEnabled;
        [NonSerialized]
        private bool _updateOnEnable;

        private T _prevValue;

        private ValueChanged<T> _valueChanged;
        
        private static MethodInfo SetSmartModifierValueMethod => typeof(PhasedBind<T>).GetMethod(nameof(SetSmartModifierValue), BindingFlags.NonPublic | BindingFlags.Instance);

        private Delegate _setSmartModifierValueDelegate;
        private Delegate SetSmartModifierValueDelegate => _setSmartModifierValueDelegate ??=
            SetSmartModifierValueMethod.CreateDelegate(
                typeof(Action<,>).MakeGenericType(typeof(ISmartModifier), typeof(T)));
        
        private void SetUpdateOnEnable(bool updateOnEnable)
        {
            _updateOnEnable = updateOnEnable;
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
                SetUpdateOnEnable = SetUpdateOnEnable,
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
                if (_isReady && PhasedBind<T>.Equals(pureValue, _pureValue))
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
                if (_isReady && PhasedBind<T>.Equals(pureValue, _pureValue))
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
                if (_isReady && PhasedBind<T>.Equals(pureValue, _pureValue))
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
                if (_isReady && PhasedBind<T>.Equals(pureValue, _pureValue))
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
                if (PhasedBind<T>.Equals(value, _pureValue))
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
                if (PhasedBind<T>.Equals(value, _pureValue))
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
                if (PhasedBind<T>.Equals(value, _pureValue))
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
                if (PhasedBind<T>.Equals(value, _pureValue))
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
                if (PhasedBind<T>.Equals(value, _pureValue))
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
            
            var preModifiers = _bindData.PreModifiers;
            var postModifiers = _bindData.Modifiers;
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
                return false;
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
        
        /// <summary>
        /// If true, the bind will update the value on enable if the context is a <see cref="Behaviour"/> and it is enabled.
        /// </summary>
        [HideMember]
        public bool UpdateOnEnable
        {
            get => _updateOnEnable;
            set => _updateOnEnable = value;
        }

        /// <summary>
        /// This event will be raised each time the bound value has changed. <br/>
        /// Beware that this event will be raised at the closest Unity Player Loop stage.
        /// </summary>
        public event ValueChanged<T> ValueChanged
        {
            add
            {
                if(_valueChanged == null)
                {
                    BindingEngineInternal.RegisterDataRefresher(this);
                }
                _valueChanged += value;
            }
            remove
            {
                _valueChanged -= value;
                if(_valueChanged == null)
                {
                    BindingEngineInternal.UnregisterDataRefresher(this);
                }
            }
        }

        private void Initialize()
        {
            _initialized = true;
            // Find the first dynamic component between pre-modifiers, converters and modifiers and split there the phases
            BuildAccessor(_bindData.Source);
            if (!TryBuildPhasedReaderAndWriter(out _reader, out _writer))
            {
                return;
            }
        }
        
        private void SetSmartModifierValue(ISmartModifier modifier, T value)
        {
            if (!_isBound)
            {
                return;
            }

            if (_accessor is not { CanWrite: true })
            {
                return;
            }
            ((IAccessor<T>)_accessor).SetValue(_bindData.Source, value);
            if (_valueChanged != null && !Equals(_prevValue, value))
            {
                _valueChanged(_prevValue, value);
            }
            _prevValue = value;
        }

        private void BuildAccessor(object source)
        {
            if(source == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_bindData.Path))
            {
                _accessor = AccessorsFactory.GetAccessor(source,
                                                        _bindData.Path,
                                                        typeof(T),
                                                        _bindData.Parameters.GetValues(),
                                                        _bindData.MainParameterIndex);
                return;
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
        }

        /// <summary>
        /// Gets the <see cref="BindData"/> if the field is bound to, null otherwise.
        /// </summary>
        [HideMember]
        public BindData<T>? BindData => _isBound ? _bindData : (BindData<T>?)null;

        /// <summary>
        /// Gets the <see cref="IAccessor"/> associated with this bind. <br/>
        /// The accessor typically handles all the heavy lifting for value read/write.
        /// </summary>
        [HideMember]
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
            get => _isBound;
            set => _isBound = value;
        }
        
        /// <summary>
        /// Whether this object can read its value or not
        /// </summary>
        public bool CanRead => !_isBound || _bindData.Mode.CanRead();
        /// <summary>
        /// Whether this object can write its value or not
        /// </summary>
        public bool CanWrite => !_isBound || _bindData.Mode.CanWrite();

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
                if (!_isBound)
                {
                    return;
                }
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
        
        public bool HasNewData() => !_isBound || _reader?.CanGetValue(_bindData.Source) == true;
        
        public T GetNewData()
        {
            if (!_isBound)
            {
                return _value;
            }
            if (_reader == null)
            {
                return GetValue(_bindData.Source);
            }
            return _reader.GetRestOfValue(_bindData.Source);
        }

        /// <summary>
        /// Gets or sets the value of this binder.
        /// </summary>
        public T Value {
            get {
                if (_isBound)
                {
                    return GetValue(_bindData.Source);
                }
                return _value;
            }
            set {
                if (_isBound)
                {
                    // This is to avoid recursive looping while setting the value
                    if (!_writeInProgress)
                    {
                        _writeInProgress = true;
                        SetValue(_bindData.Source, value);
                        _writeInProgress = false;
                    }
                }
                else
                {
                    _value = value;
                    if (_valueChanged != null && !Equals(_prevValue, value))
                    {
                        var prevValue = _prevValue;
                        _valueChanged(prevValue, value);
                    }

                    _prevValue = value;
                }
            }
        }

        [Obsolete("Use UnboundValue instead")]
        public T FallbackValue
        {
            get => UnboundValue;
            set => UnboundValue = value;
        }
        
        /// <summary>
        /// Gets or sets the unbound value of this bind
        /// </summary>
        public T UnboundValue {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _value = value;
        }

        /// <inheritdoc/>
        [HideMember]
        public object UnsafeValue => Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValue(object target)
        {
            if (!_initialized)
            {
                Initialize();
            }
            
            if(_bindData.Mode == BindMode.Write)
            {
                return !_accessor.CanRead ? throw new InvalidOperationException($"The Bind<{typeof(T).Name}> at path {_bindData.Path} is not read enabled") : _prevValue;
            }

            if (_reader == null)
            {
                return _prevValue = ((IAccessor<T>)_accessor).GetValue(target);
            }
            
            if(_reader.CanGetValue(target))
            {
                return _prevValue = _reader.GetRestOfValue(target);
            }
            
            return _prevValue;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetValueUnsafe(object target)
        {
            if (!_isBound)
            {
                return _value;
            }
            
            if (!_initialized)
            {
                Initialize();
            }
            
            if (!_accessor.CanRead)
            {
                return _prevValue;
            }
            
            if (_reader == null)
            {
                return _prevValue = ((IAccessor<T>)_accessor).GetValue(target);
            }
            
            if(_reader.CanGetValue(target))
            {
                return _prevValue = _reader.GetRestOfValue(target);
            }
            
            return _prevValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetValue(object target, in T value)
        {
            if (!_initialized)
            {
                Initialize();
            }

            if (_bindData.Mode == BindMode.Read)
            {
                if (_accessor.CanWrite)
                {
                    if (Application.isEditor && _bindData.IsLiveDebug)
                    {
                        _bindData.DebugValue = value;
                    }

                    _prevValue = value;
                    return;
                }
                
                throw new InvalidOperationException(
                    $"The Bind<{typeof(T).Name}> at path {_bindData.Path} is not write enabled");
            }

            if (Application.isEditor && _bindData.IsLiveDebug)
            {
                _bindData.DebugValue = value;
            }
            
            if(_writer != null)
            {
                _writer.SetValue(target, value, out var hasChanged);
                if (hasChanged && _valueChanged != null)
                {
                    _valueChanged(_prevValue, value);
                }

                return;
            }

            ((IAccessor<T>)_accessor).SetValue(target, value);

            if (_valueChanged != null && !Equals(_prevValue, value))
            {
                _valueChanged(_prevValue, value);
            }
            _prevValue = value;
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
            _prevValue = default;
        }

        /// <inheritdoc/>
        (Object owner, string path) IDataRefresher.RefreshId => (_bindData.Context, _bindData.Id);
        
        /// <inheritdoc/>
        public bool CanRefresh() => _valueChanged != null || (_isBound && _bindData.IsAutoUpdated && _bindData.IsValid);

        /// <inheritdoc/>
        public void Refresh()
        {
            if(_valueChanged == null && !_bindData.IsAutoUpdated) { return; }

            if (_bindData.Context is Behaviour { isActiveAndEnabled: false })
            {
                _wasEnabled = false;
                return;
            }

            if (!_wasEnabled && _updateOnEnable)
            {
                T value = _bindData.Mode.CanRead() ? GetValueUnsafe(_bindData.Source) : _prevValue;
                _valueChanged?.Invoke(_prevValue, value);
                if (_bindData.Mode.CanWrite())
                {
                    Value = value;
                }
            }
            _wasEnabled = true;
            
            if (_isBound && _bindData.Mode == BindMode.Write)
            {
                if (!_initialized)
                {
                    Initialize();
                }
                BindingEngineInternal.UnregisterDataRefresher(this);
                return;
            }
            
            var newValue = _isBound ? GetValueUnsafe(_bindData.Source) : _value;
            if (Equals(newValue, _prevValue)) return;
            var lastValue = _prevValue;
            _prevValue = newValue;
            _valueChanged?.Invoke(lastValue, newValue);
        }

        public override string ToString()
        {
            if (Application.isPlaying)
            {
                return (_isBound ? $"{_bindData.FullPath()}: " : "") + GetValueUnsafe(_bindData.Source);
            }
            return _isBound ? _bindData.FullPath() : _value?.ToString();
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

        /// <inheritdoc/>
        public void Dispose()
        {
            BindingEngineInternal.UnregisterDataRefresher(this);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            // Do nothing
        }
        
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (!_isBound)
            {
                return;
            }
        
            AutoRegister();
        }
        
        private void AutoRegister()
        {
            if (_bindData is { IsValueChangedEnabled: true, HasPersistentEvents: true })
            {
                ValueChanged -= InvokePersistentEvents;
                ValueChanged += InvokePersistentEvents;
                return;
            }

            if (_bindData.IsAutoUpdated)
            {
                BindingEngineInternal.RegisterDataRefresher(this);
            }
        }

        private void InvokePersistentEvents(T oldValue, T newValue)
        {
            if (!_isBound)
            {
                return;
            }

            if (_bindData.IsValueChangedEnabled)
            {
                _bindData.OnValueChanged.Invoke(newValue);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

        /// <summary>
        /// Constructor. Creates a bind field for <paramref name="target"/> at specified <paramref name="path"/>
        /// </summary>
        /// <param name="target">The object to bind to</param>
        /// <param name="path">The path to bind at</param>
        /// <param name="parameters">Parameters for the specified path. Can be either direct values or <see cref="IValueProvider"/>s</param>
        public PhasedBind(Object target, string path, params object[] parameters)
        {
            _bindData = new BindData<T>(target, path, parameters.ToValueProviders(), 0);
            _value = default;
            _isBound = _bindData.IsValid;
            _writeInProgress = false;
        }

        internal PhasedBind(in BindData data)
        {
            _bindData = data;
            _isBound = _bindData.IsValid;
            _writeInProgress = false;
        }

        /// <summary>
        /// Constructor. Creates an unbound field with specified direct <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value to set</param>
        public PhasedBind(in T value) 
        {
            _value = value;
        }

        public static implicit operator T(PhasedBind<T> binder) => binder.Value;
        public static explicit operator PhasedBind<T>(T value) => new PhasedBind<T>(value);
        public static explicit operator PhasedBind<T>(ReadOnlyBind<T> binder)
        {
            var bindData = binder.BindData;
            if (bindData == null)
            {
                return new PhasedBind<T>(binder.UnboundValue);
            }
            else
            {
                return new PhasedBind<T>(bindData.Value.Source, bindData.Value.Path) { _value = binder.UnboundValue };
            }
        }

        public static implicit operator PhasedBind<T>(BindExtensions.NewBind<T> newBind) => new PhasedBind<T>(newBind.value);
    }
}

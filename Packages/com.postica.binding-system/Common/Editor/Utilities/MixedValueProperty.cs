using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.Common
{
    internal class MixedValueProperty : IDisposable
    {
        protected SerializedObject _serializedObject;
        protected string _path;

        protected bool? _isMixedValue;
        protected bool _updatePostponed;

        public bool? isMixedValue => _isMixedValue;

        public MixedValueProperty(SerializedObject serializedObject, string propPath)
        {
            _serializedObject = new SerializedObject(serializedObject.targetObjects, serializedObject.context);
            _path = propPath;

            if (!serializedObject.isEditingMultipleObjects)
            {
                _isMixedValue = default;
                return;
            }

            Update();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateIfNeeded()
        {
            if (_updatePostponed)
            {
                Update();
            }
        }
        public void PostponeUpdate() => _updatePostponed = true;

        public virtual void Update()
        {
            _updatePostponed = false;
            _isMixedValue = null;

            if (!_serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            var commonValue = default(object);
            var firstValueSet = false;

            foreach (var target in _serializedObject.targetObjects)
            {
                using (var so = new SerializedObject(target))
                {
                    var soProp = so.FindProperty(_path);
                    var soPropValue = soProp.GetValue();

                    if (!firstValueSet)
                    {
                        commonValue = soPropValue;
                        firstValueSet = true;
                        continue;
                    }

                    if ((soPropValue == null && commonValue != null)
                        || (soPropValue != null && commonValue == null))
                    {
                        commonValue = default;
                        _isMixedValue = true;
                        return;
                    }

                    if ((commonValue == null && soPropValue == null) || commonValue?.Equals(soPropValue) == true)
                    {
                        _isMixedValue = false;
                        continue;
                    }

                    // We have a different value, no need to continue
                    commonValue = default;
                    _isMixedValue = true;
                    return;
                }
            }
        }

        public virtual void Dispose()
        {
            _serializedObject?.Dispose();
        }
    }

    internal class MixedValueProperty<T> : IDisposable
    {
        public delegate void ForEachDelegate(Object target, SerializedProperty property, T value);

        protected SerializedObject _serializedObject;
        protected Func<SerializedProperty, T> _getter;
        protected Action<SerializedProperty, T> _setter;
        protected string _path;

        protected bool? _isMixedValue;
        protected T _commonValue;
        protected T _anyValue;
        protected Dictionary<Object, T> _values;

        protected bool _updatePostponed;

        public bool? isMixedValue => _isMixedValue;
        public T commonValue => _commonValue;
        public T anyValue => _anyValue;
        public Dictionary<Object, T> values => _values ??= new Dictionary<Object, T>();

        public T this[Object target]
        {
            get
            {
                using(var so = new SerializedObject(target))
                {
                    var prop = so.FindProperty(_path);
                    return prop != null ? _getter(prop) : default;
                }
            }
            set
            {
                using (var so = new SerializedObject(target))
                {
                    var prop = so.FindProperty(_path);
                    if(prop != null)
                    {
                        SetValue(prop, value);
                        so.ApplyModifiedProperties();
                    }
                }
            }
        }

        public MixedValueProperty(SerializedObject serializedObject, string propPath, Action<SerializedProperty, T> propValueSetter, Func<SerializedProperty, T> propValueGetter)
        {
            _serializedObject = new SerializedObject(serializedObject.targetObjects, serializedObject.context);
            _path = propPath;
            _setter = propValueSetter;
            _getter = propValueGetter;

            if (!serializedObject.isEditingMultipleObjects)
            {
                _isMixedValue = default;
                _commonValue = default;
                _values = default;
                return;
            }

            Update();
        }

        public void PostponeUpdate() => _updatePostponed = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateIfNeeded()
        {
            if (_updatePostponed)
            {
                Update();
            }
        }

        protected virtual void SetValue(SerializedProperty prop, T value) => _setter(prop, value);

        public virtual void ForEach(ForEachDelegate callback)
        {
            foreach (var t in _serializedObject.targetObjects)
            {
                using (var so = new SerializedObject(t))
                {
                    so.Update();
                    var prop = so.FindProperty(_path);
                    var value = _getter(prop);
                    callback(t, prop, value);
                }
            }
        }

        public virtual void Update()
        {
            _updatePostponed = false;

            _isMixedValue = null;
            _commonValue = default;
            _anyValue = default;
            _values = new Dictionary<Object, T>();

            if(!_serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            var firstValueSet = false;

            foreach (var target in _serializedObject.targetObjects)
            {
                using (var so = new SerializedObject(target))
                {
                    var soProp = so.FindProperty(_path);
                    var soPropValue = _getter(soProp);

                    _values[target] = soPropValue;

                    if(_anyValue is null && soPropValue is not null)
                    {
                        _anyValue = soPropValue;
                    }

                    if (!firstValueSet)
                    {
                        _commonValue = soPropValue;
                        firstValueSet = true;
                        continue;
                    }

                    if ((soPropValue == null && _commonValue != null)
                        || (soPropValue != null && _commonValue == null))
                    {
                        _commonValue = default;
                        _isMixedValue = true;
                        return;
                    }

                    if ((_commonValue == null && soPropValue == null) || _commonValue?.Equals(soPropValue) == true)
                    {
                        _isMixedValue = false;
                        continue;
                    }

                    // We have a different value, no need to continue
                    _commonValue = default;
                    _isMixedValue = true;
                    return;
                }
            }
        }

        public virtual void Dispose()
        {
            _serializedObject?.Dispose();
        }
    }
}
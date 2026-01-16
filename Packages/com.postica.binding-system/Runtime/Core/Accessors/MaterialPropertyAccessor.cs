using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Postica.BindingSystem.Accessors;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace Postica.BindingSystem
{
    [Preserve]
#if BIND_AVOID_IL2CPP_CHECKS
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    internal sealed class MaterialPropertyAccessor<T> :
        IAccessor,
        IAccessor<T>, IAccessor<Material, T>,
        IAccessorLink, IBoundAccessor<T>,
        ICompiledAccessor<T>,
        IBoundCompiledAccessor<T>, ICompiledAccessor<Material, T>
    {
        private static List<(Type valueType, Func<Material, int, T> getter, Action<Material, int, T> setter)>
            _accessors = new();
        
        private readonly bool _valueIsValueType = typeof(T).IsValueType;
        private readonly string _propertyName;
        private readonly string _propertyType;
        private readonly int _propertyHash;
        private readonly Func<Material, int, T> _getter;
        private readonly Action<Material, int, T> _setter;

        private Material _cachedValue;
        private bool _cacheReady;
        private RefBoundGetterDelegate<Material> _boundGetter;
        private RefBoundGetterDelegate<Material> _boundGetterForSet;
        
        private IAccessorLink _parent;
        private IAccessorLink _child;

        public MaterialPropertyAccessor(MaterialPropertyAccessor<T> other)
        {
            _propertyName = other._propertyName;
            _propertyHash = other._propertyHash;
            _propertyType = other._propertyType;
            _getter = other._getter;
            _setter = other._setter;
        }

        public MaterialPropertyAccessor(string propertyName, string propertyTypename)
        {
            _propertyName = propertyName;
            _propertyType = propertyTypename;
            _propertyHash = Shader.PropertyToID(propertyName);
            _getter = GetGetter(propertyTypename);
            _setter = GetSetter(propertyTypename);
        }

        private static Func<Material, int, T> GetGetter(string propertyType)
        {
            var accessors = _accessors.Find(p => p.valueType == typeof(T));
            if (accessors != default && accessors.getter != null)
            {
                return accessors.getter;
            }

            var method = typeof(T) switch
            {
                // For int
                { } t when t == typeof(int) => typeof(Material).GetMethod(nameof(Material.GetInt), new[] {typeof(int)}),
                { } t when t == typeof(float) => typeof(Material).GetMethod(nameof(Material.GetFloat), new[] {typeof(int)}),
                { } t when t == typeof(Color) => typeof(Material).GetMethod(nameof(Material.GetColor), new[] {typeof(int)}),
                { } t when t == typeof(Vector2) && propertyType == "tiling" => typeof(Material).GetMethod(nameof(Material.GetTextureScale), new[] {typeof(int)}),
                { } t when t == typeof(Vector2) && propertyType == "offset" => typeof(Material).GetMethod(nameof(Material.GetTextureOffset), new[] {typeof(int)}),
                { } t when t == typeof(Vector2) => typeof(Material).GetMethod(nameof(Material.GetVector), new[] {typeof(int)}),
                { } t when t == typeof(Vector3) => typeof(Material).GetMethod(nameof(Material.GetVector), new[] {typeof(int)}),
                { } t when t == typeof(Vector4) => typeof(Material).GetMethod(nameof(Material.GetVector), new[] {typeof(int)}),
                { } t when t == typeof(Matrix4x4) => typeof(Material).GetMethod(nameof(Material.GetMatrix), new[] {typeof(int)}),
                // For texture
                { } t when t == typeof(Texture) => typeof(Material).GetMethod(nameof(Material.GetTexture), new[] {typeof(int)}),
                { } t when t == typeof(Texture2D) => typeof(Material).GetMethod(nameof(Material.GetTexture), new[] {typeof(int)}),
                { } t when t == typeof(Texture3D) => typeof(Material).GetMethod(nameof(Material.GetTexture), new[] {typeof(int)}),
                { } t when t == typeof(Cubemap) => typeof(Material).GetMethod(nameof(Material.GetTexture), new[] {typeof(int)}),
                { } t when t == typeof(RenderTexture) => typeof(Material).GetMethod(nameof(Material.GetTexture), new[] {typeof(int)}),
                // For float array
                { } t when t == typeof(float[]) => typeof(Material).GetMethod(nameof(Material.GetFloatArray), new[] {typeof(int)}),
                { } t when t == typeof(Vector4[]) => typeof(Material).GetMethod(nameof(Material.GetVectorArray), new[] {typeof(int)}),
                { } t when t == typeof(Color[]) => typeof(Material).GetMethod(nameof(Material.GetColorArray), new[] {typeof(int)}),
                { } t when t == typeof(Matrix4x4[]) => typeof(Material).GetMethod(nameof(Material.GetMatrixArray), new[] {typeof(int)}),
                _ => null
            };
            
            if (method == null)
            {
                throw new ArgumentException($"Material does not have a method to get property of type {typeof(T).Name}");
            }
            
            var getter = (Func<Material, int, T>)method.CreateDelegate(typeof(Func<Material, int, T>));
            _accessors.Remove(accessors);
            accessors.getter = getter;
            _accessors.Add(accessors);
            return accessors.getter;
        }

        private static Action<Material, int, T> GetSetter(string propertyType)
        {
            var accessors = _accessors.Find(p => p.valueType == typeof(T));
            if (accessors != default && accessors.setter != null)
            {
                return accessors.setter;
            }
            
            var pars = new[] {typeof(int), typeof(T)};

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            Type[] In<Ttype>()
            {
                pars[1] = typeof(Ttype);
                return pars;
            }

            var method = typeof(T) switch
            {
                // For int
                { } t when t == typeof(int) => typeof(Material).GetMethod(nameof(Material.SetInt), In<int>()),
                { } t when t == typeof(float) => typeof(Material).GetMethod(nameof(Material.SetFloat), In<float>()),
                { } t when t == typeof(Color) => typeof(Material).GetMethod(nameof(Material.SetColor), In<Color>()),
                { } t when t == typeof(Vector2) && propertyType == "tiling" => typeof(Material).GetMethod(nameof(Material.SetTextureScale), In<Vector2>()),
                { } t when t == typeof(Vector2) && propertyType == "offset" => typeof(Material).GetMethod(nameof(Material.SetTextureOffset), In<Vector2>()),
                { } t when t == typeof(Vector2) => typeof(Material).GetMethod(nameof(Material.SetVector), In<Vector4>()),
                { } t when t == typeof(Vector3) => typeof(Material).GetMethod(nameof(Material.SetVector), In<Vector4>()),
                { } t when t == typeof(Vector4) => typeof(Material).GetMethod(nameof(Material.SetVector), In<Vector4>()),
                { } t when t == typeof(Matrix4x4) => typeof(Material).GetMethod(nameof(Material.SetMatrix), In<Matrix4x4>()),
                // For texture
                { } t when t == typeof(Texture) => typeof(Material).GetMethod(nameof(Material.SetTexture), In<Texture>()),
                { } t when t == typeof(Texture2D) => typeof(Material).GetMethod(nameof(Material.SetTexture), In<Texture>()),
                { } t when t == typeof(Texture3D) => typeof(Material).GetMethod(nameof(Material.SetTexture), In<Texture>()),
                { } t when t == typeof(Cubemap) => typeof(Material).GetMethod(nameof(Material.SetTexture), In<Texture>()),
                { } t when t == typeof(RenderTexture) => typeof(Material).GetMethod(nameof(Material.SetTexture), In<Texture>()),
                // For float array
                { } t when t == typeof(float[]) => typeof(Material).GetMethod(nameof(Material.SetFloatArray), In<float[]>()),
                { } t when t == typeof(Vector4[]) => typeof(Material).GetMethod(nameof(Material.SetVectorArray), In<Vector4[]>()),
                { } t when t == typeof(Color[]) => typeof(Material).GetMethod(nameof(Material.SetColorArray), In<Color[]>()),
                { } t when t == typeof(Matrix4x4[]) => typeof(Material).GetMethod(nameof(Material.SetMatrixArray), In<Matrix4x4[]>()),
                _ => null,
            };

            if (method == null)
            {
                throw new ArgumentException($"Material does not have a method to set property of type {typeof(T).Name}");
            }
            
            var setter = (Action<Material, int, T>)method.CreateDelegate(typeof(Action<Material, int, T>));
            _accessors.Remove(accessors);
            accessors.setter = setter;
            _accessors.Add(accessors);
            return accessors.setter;
        }

        public IAccessorLink Previous
        {
            get => _parent;
            set
            {
                if (_parent == value) return;
                
                if (_parent != null && _parent.Next == this)
                {
                    _parent.Next = null;
                }
                
                _parent = value;
                
                if (_parent == null) return;
                
                if (_parent is IBoundCompiledAccessor<Material> boundCompiledAccessor)
                {
                    _boundGetter = boundCompiledAccessor.CompileBoundGetter();
                    _boundGetterForSet = boundCompiledAccessor.CompileBoundGetterForSet();
                }
                else if (_parent is IBoundAccessor<Material> boundAccessor)
                {
                    _boundGetter = boundAccessor.GetValue;
                }
                _parent.Next = this;
            }
        }

        public IAccessorLink Next
        {
            get => _child;
            set
            {
                if (_child == value) return;
                
                _child = value;
                if (_child != null)
                {
                    _child.Previous = this;
                }
            }
        }

        public Type ObjectType => typeof(Material);

        public Type ValueType => typeof(T);

        public bool CanRead => true;

        public bool CanWrite => true;

        public object GetValue(object target) => GetValue((Material)target);

        public T GetValue() => GetValue(_boundGetter());

        public T GetValue(Material target)
        {
            if (!target.HasProperty(_propertyHash))
            {
                throw new ArgumentException($"Material {target} does not have property {_propertyName}");
            }

            return _getter(target, _propertyHash);
        }

        public IAccessor Duplicate() => new MaterialPropertyAccessor<T>(this);

        public void SetValue(object target, object value)
        {
            if (value is T tValue)
            {
                SetValue(target, tValue);
            }
            else if (_valueIsValueType)
            {
                if (value is string)
                {
                    try
                    {
                        SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                    }
                    catch (FormatException)
                    {
                        throw new InvalidCastException($"{value.GetType().Name} cannot be cast to {typeof(T).Name}");
                    }
                }
                else
                {
                    SetValue(target, (T)Convert.ChangeType(value, typeof(T)));
                }
            }
        }

        public void SetValue(object target, in T value)
        {
            if (target == null)
            {
                return;
            }
            if(target is not Material material)
            {
                throw new ArgumentException($"Expected target to be of type Material, but got {target.GetType().Name}");
            }
            if(!material.HasProperty(_propertyHash))
            {
                throw new ArgumentException($"Material {material} does not have property {_propertyName}");
            }
            _setter(material, _propertyHash, value);
        }

        public T GetValueToSet()
        {
            _cachedValue = _boundGetterForSet();
            _cacheReady = true;
            return _getter(_cachedValue, _propertyHash);
        }

        public void SetValue(in T value)
        {
            var material = _cacheReady ? _cachedValue : _boundGetterForSet();
            if(!material.HasProperty(_propertyHash))
            {
                throw new ArgumentException($"Material {material} does not have property {_propertyName}");
            }
            _setter(material, _propertyHash, value);
            _cacheReady = false;
        }

        public void SetValue(ref Material target, in T value)
        {
            if(!target.HasProperty(_propertyHash))
            {
                throw new ArgumentException($"Material {target} does not have property {_propertyName}");
            }
            _setter(target, _propertyHash, value);
        }

        T IAccessor<T>.GetValue(object target) => GetValue((Material)target);
        private T GetValueSpecial(object target) => GetValue((Material)target);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        public IConcurrentAccessor<Material, T> MakeConcurrent() => new WrapConcurrentAccessor<Material, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor<T> IAccessor<T>.MakeConcurrent() => new WrapConcurrentAccessor<Material, T>(this);

        // TODO: This could potentially lead to deadlocks, consider making getters and setters thread-safe
        IConcurrentAccessor IAccessor.MakeConcurrent() => new WrapConcurrentAccessor<Material, T>(this);

        
        RefSetterDelegate<Material, T> ICompiledAccessor<Material, T>.CompileSetter() => SetValue;
        RefGetterDelegate<Material, T> ICompiledAccessor<Material, T>.CompileGetter() => GetValue;
        public RefGetterDelegate<T> CompileGetter() => GetValueSpecial;
        public RefSetterDelegate<T> CompileSetter() => SetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetter() => GetValue;
        public RefBoundGetterDelegate<T> CompileBoundGetterForSet() => GetValueToSet;
        public RefBoundSetterDelegate<T> CompileBoundSetter() => SetValue;
    }
}

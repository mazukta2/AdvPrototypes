using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This interface is used to mark a class as needing validation.
    /// </summary>
    public interface IRequiresValidation
    {
        /// <summary>
        /// Performs a validation on the object.
        /// </summary>
        /// <param name="hasChanged">Returns true if the object has changed</param>
        void Validate(out bool hasChanged);
    }
    
    /// <summary>
    /// A value wrapper which works in two modes, either in direct value mode or in bound mode
    /// </summary>
    public interface IBind
    {
        /// <summary>
        /// Gets or sets whether this wrapper has its value bound or not
        /// </summary>
        bool IsBound { get; set; }
    }

    /// <summary>
    /// Indicates the type of binding data this object has.
    /// </summary>
    internal interface IBindData<T> where T : struct
    {
        /// <summary>
        /// The <typeparamref name="T"/> BindData associated to this object.
        /// </summary>
        T? BindData { get; }
    }

    /// <summary>
    /// A value wrapper which works in two modes, either in direct value mode or in bound mode
    /// </summary>
    public interface IBind<T> : IBind
    {
    }
    
    /// <summary>
    /// This interface is used to mark a class as a controller for one or multiple binds.<br/>
    /// Implementing this interface gives access to multiple bind control extension methods.
    /// </summary>
    public interface IBindController { }

    internal interface IBindAccessor
    {
        object RawAccessor { get; }
    }
    
    /// <summary>
    /// An object which can change dynamically at runtime. When this object is dynamic, the binding system will re-evaluate it on every data update.
    /// </summary>
    public interface IDynamicComponent
    {
        bool IsDynamic { get; }
    }

    /// <summary>
    /// An object which returns a value. Used mostly for generalization purposes.
    /// </summary>
    public interface IValueProvider
    {
        object UnsafeValue { get; }
    }

    /// <summary>
    /// A specialized variant of the <see cref="IValueProvider"/>. Returns a value of type <typeparamref name="T"/>. <br/>
    /// Is typically used for parameters, as those may not be a direct value, but a proxy one as well.
    /// </summary>
    /// <typeparam name="T">The type of the value it provides.</typeparam>
    public interface IValueProvider<out T> : IValueProvider
    {
        T Value { get; }
    }

    /// <summary>
    /// This object is used by a binding update engine to update in background data refreshers. 
    /// </summary>
    public interface IDataRefresher
    {
        /// <summary>
        /// An unique identifier for this object. Required by refresh engines to identify the object.
        /// </summary>
        (Object owner, string path) RefreshId { get; }
        /// <summary>
        /// Returns a Boolean indicating if this object can update its data.
        /// </summary>
        /// <returns></returns>
        bool CanRefresh();
        /// <summary>
        /// Updates the data internally.
        /// </summary>
        void Refresh();
    }

    /// <summary>
    /// An object which modifies an input data and outputs the result. <br/>
    /// This object may be part of a binding pipeline, where the data is processed through modifiers before being delivered to the user.
    /// </summary>
    /// <remarks>The input and output data must be of the same type. </remarks>
    public interface IModifier
    {
        /// <summary>
        /// The unique identifier of the modifier.
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// A very short description of how the data is modified.
        /// </summary>
        /// <example>
        /// A modifier which inverses the number would have its description as follows: [-X]
        /// </example>
        string ShortDataDescription { get; }

        /// <summary>
        /// Applies the changes to input <paramref name="value"/> and returns it as the same type of <paramref name="value"/>.
        /// </summary>
        /// <param name="mode">The mode this modification is currently in</param>
        /// <param name="value">The value to modify.</param>
        /// <returns>The result of the modification.</returns>
        object Modify(BindMode mode, object value);
        
        /// <summary>
        /// In which mode this object operates.
        /// </summary>
        BindMode ModifyMode { get; }
        
        /// <summary>
        /// This method is called when the object is validated in the editor.
        /// </summary>
        void OnValidate() { }
    }

    /// <summary>
    /// A specialized variant of <see cref="IModifier"/> for read only operations.
    /// </summary>
    /// <typeparam name="T">The type of data to modify.</typeparam>
    public interface IReadModifier<T> : IModifier
    {
        /// <summary>
        /// Modifies the <paramref name="value"/> obtained from a read operation.
        /// </summary>
        /// <param name="value">The data to modify.</param>
        /// <returns>The result of the modification.</returns>
        T ModifyRead(in T value);
    }

    /// <summary>
    /// A specialized variant of <see cref="IModifier"/> for write only operations.
    /// </summary>
    /// <typeparam name="T">The type of data to modify.</typeparam>
    public interface IWriteModifier<T> : IModifier
    {
        /// <summary>
        /// Modifies the <paramref name="value"/> obtained from a write operation.
        /// </summary>
        /// <param name="value">The data to modify.</param>
        /// <returns>The result of the modification.</returns>
        T ModifyWrite(in T value);
    }

    /// <summary>
    /// A modifier which is aware of its owner.
    /// </summary>
    public interface ISmartModifier : IModifier
    {
        IBind BindOwner { get; set; }

        void SetSetValueCallback<T>(Action<ISmartModifier, T> callback)
        {
            if (this is ISmartValueModifier<T> smartValueModifier)
            {
                smartValueModifier.SetValue = callback;
            }
        }

    }
    
    /// <summary>
    /// A modifier which is aware of its owner and can set a value up the chain.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISmartValueModifier<T> : ISmartModifier
    {
        Action<ISmartModifier, T> SetValue { get; set; }
    }
    
    /// <summary>
    /// An object which requires a bind to auto-update or not.
    /// </summary>
    public interface IRequiresAutoUpdate
    {
        /// <summary>
        /// Whether the bound value owner should update automatically.
        /// </summary>
        bool ShouldAutoUpdate { get; }
        /// <summary>
        /// If true, the bound value owner will update on every context activation (e.g. A MonoBehaviour becoming enabled).
        /// </summary>
        bool UpdateOnEnable { get; }
    }
    
    /// <summary>
    /// A specialized variant of <see cref="IModifier"/>.
    /// </summary>
    /// <remarks>This object is both a <see cref="IReadModifier{T}"/> and <see cref="IWriteModifier{T}"/>.</remarks>
    /// <typeparam name="T">The type of data to modify.</typeparam>
    [Obsolete("Use IReadWriteModifier<T> instead")]
    public interface IModifier<T> : IReadModifier<T>, IWriteModifier<T>
    {
        
    }

    /// <summary>
    /// A specialized variant of <see cref="IModifier"/>.
    /// </summary>
    /// <remarks>This object is both a <see cref="IReadModifier{T}"/> and <see cref="IWriteModifier{T}"/>.</remarks>
    /// <typeparam name="T">The type of data to modify.</typeparam>
    public interface IReadWriteModifier<T> : IReadModifier<T>, IWriteModifier<T>
    {
        
    }

    /// <summary>
    /// A modifier which can be applied to derived types.
    /// </summary>
    public interface IObjectModifier : IModifier
    {
        /// <summary>
        /// The type of object this modifier can be applied to.
        /// </summary>
        Type TargetType { get; set; }
    }
    
    /// <summary>
    /// Specialized variant of <see cref="IObjectModifier"/> which can be applied to derived types.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IObjectModifier<T> : IObjectModifier where T : class { }
    
    internal interface IBindProxyProvider
    {
        bool TryGetProxy(Object source, string path, out BindProxy proxy, out int index);
        bool TryGetProxiesInTree(Object source, string rootPath, out List<(BindProxy proxy, int index)> proxies);
        List<BindProxy> GetProxies(Object source);
        bool RemoveProxy(Object source, string path);
        bool RemoveProxy(BindProxy proxy);
        bool AddProxy(BindProxy proxy);
        bool RemoveProxies(Object source);
        bool IsEmpty { get; }
        IEnumerable<BindProxy> GetAllProxies();

        void UpdateProxy(string id);
        bool UpdateProxyAt(int index);
        string GetProxyId(BindProxy proxy);
        bool TryGetProxy(string id, out BindProxy proxy, out int index);
    }
    
    /// <summary>
    /// Contains a list of <see cref="IBindProxy"/> objects which can be used to bind data between two objects.<br/>
    /// </summary>
    public interface IBindProxyContainer
    {
        IReadOnlyList<IBindProxy> Bindings { get; }
        void RemoveProxy(IBindProxy proxy);
        void RemoveProxyAt(int index);
    }

    /// <summary>
    /// A proxy object which binds a source and target together, allowing data to flow between them.<br/>
    /// </summary>
    public interface IBindProxy
    {
        /// <summary>
        /// The source object from which the data is read.<br/>
        /// </summary>
        Object Source { get; set; }
        /// <summary>
        /// The path in the source object from which the data is read.<br/>
        /// </summary>
        string SourcePath { get; set; }
        /// <summary>
        /// The target object to which the data is written.<br/>
        /// </summary>
        Object Target { get; set; }
        /// <summary>
        /// The path in the target object to which the data is written.<br/>
        /// </summary>
        string TargetPath { get; set; }
        
        [EditorBrowsable(EditorBrowsableState.Never)]
        void SetBindDataSource(Object source) => Source = source;
        [EditorBrowsable(EditorBrowsableState.Never)]
        void SetBindDataPath(string path) => SourcePath = path;
    }

    /// <summary>
    /// This class is used to generate delegates which can modify a value of type <typeparamref name="T"/>.
    /// </summary>
    public static class ModifierExtensions
    {
        /// <summary>
        /// Gets the generic modifier as a specialized <see cref="IReadModifier{T}"/>. <br/>
        /// It may return null if the modifier is not a <see cref="IReadModifier{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of data to modify.</typeparam>
        /// <param name="modifier">The modifier to transform.</param>
        /// <returns>The <see cref="IReadModifier{T}"/> or null.</returns>
        [Obsolete("Use GetReadFunc instead")]
        public static IReadModifier<T> AsReadModifier<T>(this IModifier modifier)
        {
            return modifier is IReadWriteModifier<T> Tmodifier && Tmodifier.ModifyMode.CanRead() ? Tmodifier : modifier as IReadModifier<T>;
        }

        /// <summary>
        /// Gets the generic modifier as a specialized <see cref="IWriteModifier{T}"/>. <br/>
        /// It may return null if the modifier is not a <see cref="IWriteModifier{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of data to modify.</typeparam>
        /// <param name="modifier">The modifier to transform.</param>
        /// <returns>The <see cref="IWriteModifier{T}"/> or null.</returns>
        [Obsolete("Use GetWriteFunc instead")]
        public static IWriteModifier<T> AsWriteModifier<T>(this IModifier modifier)
        {
            return modifier is IReadWriteModifier<T> Tmodifier && Tmodifier.ModifyMode.CanWrite() ? Tmodifier : modifier as IWriteModifier<T>;
        }
        
        public static ModifyDelegate<T> GetReadFunc<T>(this IModifier modifier, IBind bindField, Action<ISmartModifier, T> setSmartValue)
        {
            if (modifier == null)
            {
                return null;
            }
            
            if (!modifier.ModifyMode.CanRead())
            {
                return null;
            }
            
            if(modifier is ISmartModifier smartModifier)
            {
                smartModifier.BindOwner = bindField;
                if(setSmartValue != null)
                {
                    smartModifier.SetSetValueCallback(setSmartValue);
                }
            }
            
            if(modifier is IReadModifier<T> readModifier)
            {
                return readModifier.ModifyRead;
            }

            if (typeof(T).IsValueType)
            {
                return null;
            }
            
            var lastReadType = typeof(object);
            ModifyDelegate<T> readFunc = null;
            
            // Try and find interfaces that match the type
            foreach (var i in modifier.GetType().GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadModifier<>))
                {
                    var type = i.GetGenericArguments()[0];
                    if (type.IsAssignableFrom(typeof(T)) && lastReadType.IsAssignableFrom(type))
                    {
                        lastReadType = type;
                        readFunc = GenerateReadFunc<T>(modifier, type);
                    }
                }
            }

            return readFunc ?? FallbackFunc<T>(modifier, BindMode.Read);
        }
        
        public static ModifyDelegate<T> GetWriteFunc<T>(this IModifier modifier, IBind bindField, Action<ISmartModifier, T> setSmartValue)
        {
            if (modifier == null)
            {
                return null;
            }
            
            if (!modifier.ModifyMode.CanWrite())
            {
                return null;
            }
            
            if(modifier is ISmartModifier smartModifier)
            {
                smartModifier.BindOwner = bindField;
                if(setSmartValue != null)
                {
                    smartModifier.SetSetValueCallback(setSmartValue);
                }
            }
            
            if(modifier is IWriteModifier<T> writeModifier)
            {
                return writeModifier.ModifyWrite;
            }

            if (typeof(T).IsValueType)
            {
                return null;
            }

            var lastWriteType = typeof(object);
            ModifyDelegate<T> writeFunc = null;
            
            // Try and find interfaces that match the type
            foreach (var i in modifier.GetType().GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWriteModifier<>))
                {
                    var type = i.GetGenericArguments()[0];
                    if (type.IsAssignableFrom(typeof(T)) && lastWriteType.IsAssignableFrom(type))
                    {
                        lastWriteType = type;
                        writeFunc = GenerateWriteFunc<T>(modifier, type);
                    }
                }
            }

            return writeFunc ?? FallbackFunc<T>(modifier, BindMode.Write);
        }

        public static (ModifyDelegate<T> readFunc, ModifyDelegate<T> writeFunc) GetBothFunc<T>(this IModifier modifier,
            IBind bindField, Action<ISmartModifier, T> setSmartValue)
            => GetBothFuncInternal<T>(modifier, bindField, setSmartValue);

        
        internal static (ModifyDelegate<T> readFunc, ModifyDelegate<T> writeFunc) GetBothFuncInternal<T>(this IModifier modifier, IBind bindField, Delegate setSmartValue)
        {
            if (modifier == null)
            {
                return (null, null);
            }
            
            if(modifier is ISmartModifier smartModifier)
            {
                smartModifier.BindOwner = bindField;
                if(setSmartValue is Action<ISmartModifier, T> setSmartValuePrecise)
                {
                    smartModifier.SetSetValueCallback(setSmartValuePrecise);
                }
            }
            
            ModifyDelegate<T> readFunc = null;
            ModifyDelegate<T> writeFunc = null;
            
            if(modifier.ModifyMode.CanRead() && modifier is IReadModifier<T> readModifier)
            {
                readFunc = readModifier.ModifyRead;
            }
            
            if(modifier.ModifyMode.CanWrite() && modifier is IWriteModifier<T> writeModifier)
            {
                writeFunc = writeModifier.ModifyWrite;
            }

            if (readFunc != null && writeFunc != null)
            {
                return (readFunc, writeFunc);
            }

            if (typeof(T).IsValueType)
            {
                return (readFunc, writeFunc);
            }

            Type lastReadType = typeof(object);
            Type lastWriteType = typeof(object);
            
            // Try and find interfaces that match the type
            foreach (var i in modifier.GetType().GetInterfaces())
            {
                if (!i.IsGenericType)
                {
                    continue;
                }

                if (i.GetGenericTypeDefinition() == typeof(IReadWriteModifier<T>))
                {
                    var type = i.GetGenericArguments()[0];
                    if (!type.IsAssignableFrom(typeof(T))) continue;

                    if (modifier.ModifyMode == BindMode.ReadWrite 
                        && lastReadType.IsAssignableFrom(type) 
                        && lastReadType.IsAssignableFrom(type))
                    {
                        var (read, write) = GenerateBothFunc<T>(modifier, type);
                        readFunc ??= read;
                        writeFunc ??= write;
                    }
                    else if (modifier.ModifyMode.CanRead() && lastReadType.IsAssignableFrom(type))
                    {
                        lastReadType = type;
                        readFunc = GenerateReadFunc<T>(modifier, type);
                    }
                    else if (modifier.ModifyMode.CanWrite() && lastWriteType.IsAssignableFrom(type))
                    {
                        lastWriteType = type;
                        writeFunc = GenerateWriteFunc<T>(modifier, type);
                    }
                }

                if (modifier.ModifyMode.CanWrite() && i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IWriteModifier<>))
                {
                    var type = i.GetGenericArguments()[0];
                    if (type.IsAssignableFrom(typeof(T)) && lastWriteType.IsAssignableFrom(type))
                    {
                        lastWriteType = type;
                        writeFunc = GenerateWriteFunc<T>(modifier, type);
                    }
                }
                else if (modifier.ModifyMode.CanRead() && i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadModifier<>))
                {
                    var type = i.GetGenericArguments()[0];
                    if (type.IsAssignableFrom(typeof(T)) && lastReadType.IsAssignableFrom(type))
                    {
                        lastReadType = type;
                        readFunc = GenerateReadFunc<T>(modifier, type);
                    }
                }
            }

            readFunc ??= FallbackFunc<T>(modifier, BindMode.Read);
            writeFunc ??= FallbackFunc<T>(modifier, BindMode.Write);
            
            return (readFunc, writeFunc);
        }

        private static ModifyDelegate<T> FallbackFunc<T>(IModifier modifier, BindMode mode)
        {
            return mode == BindMode.Write 
                ? (in T v) => modifier.Modify(BindMode.Write, v) is T tval ? tval : v
                : (in T v) => modifier.Modify(BindMode.Read, v) is T tval ? tval : v;
        }

        private static ModifyDelegate<T> GenerateReadFunc<T>(IModifier modifier, Type argType)
        {
            var generatorType = typeof(ModifyGenerator<,>).MakeGenericType(argType, typeof(T));
            var generator = Activator.CreateInstance(generatorType) as ModifyGenerator;
            return generator.GetReadFunc(modifier) as ModifyDelegate<T>;
        }
        
        private static ModifyDelegate<T> GenerateWriteFunc<T>(IModifier modifier, Type argType)
        {
            var generatorType = typeof(ModifyGenerator<,>).MakeGenericType(argType, typeof(T));
            var generator = Activator.CreateInstance(generatorType) as ModifyGenerator;
            return generator.GetWriteFunc(modifier) as ModifyDelegate<T>;
        }
        
        private static (ModifyDelegate<T> read, ModifyDelegate<T> write) GenerateBothFunc<T>(IModifier modifier, Type argType)
        {
            var generatorType = typeof(ModifyGenerator<,>).MakeGenericType(argType, typeof(T));
            var generator = Activator.CreateInstance(generatorType) as ModifyGenerator;
            return (generator.GetReadFunc(modifier) as ModifyDelegate<T>, generator.GetWriteFunc(modifier) as ModifyDelegate<T>);
        }

        private abstract class ModifyGenerator
        {
            public abstract Delegate GetWriteFunc(IModifier modifier);
            public abstract Delegate GetReadFunc(IModifier modifier);
        }

        private sealed class ModifyGenerator<S, T> : ModifyGenerator where T : S
        {
            public override Delegate GetWriteFunc(IModifier modifier)
            {
                var specialModifier = (IWriteModifier<S>)modifier;
                ModifyDelegate<T> result = (in T v) => (T)specialModifier.ModifyWrite(v);
                return result;
            }

            public override Delegate GetReadFunc(IModifier modifier)
            {
                var specialModifier = (IReadModifier<S>)modifier;
                ModifyDelegate<T> result = (in T v) => (T)specialModifier.ModifyRead(v);
                return result;
            }
        }
    }
}

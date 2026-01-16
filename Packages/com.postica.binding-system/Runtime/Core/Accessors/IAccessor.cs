using System;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Accessors
{
    public delegate T RefBoundGetterDelegate<out T>();
    public delegate void RefBoundSetterDelegate<T>(in T value);
    
    public delegate T RefGetterDelegate<out T>(object target);
    public delegate void RefSetterDelegate<T>(object target, in T value);
    
    public delegate T RefGetterDelegate<in S, out T>(S target);
    public delegate void RefSetterDelegate<S, T>(ref S target, in T value);
    
    /// <summary>
    /// A wrapper to read and/or write values to an object
    /// </summary>
    public interface IAccessor
    {
        /// <summary>
        /// The type of the object to read/write values from/to
        /// </summary>
        Type ObjectType { get; }
        /// <summary>
        /// The type of the value to read/write
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Whether this object can read the value or not
        /// </summary>
        bool CanRead { get; }
        /// <summary>
        /// Whether this object can write the value or not
        /// </summary>
        bool CanWrite { get; }

        /// <summary>
        /// Reads the value out of the target. <br/>
        /// The <paramref name="target"/> <b>must be compatible</b> with <see cref="ObjectType"/>
        /// </summary>
        /// <param name="target">The object compatible with <see cref="ObjectType"/></param>
        /// <returns>The value (usually of <see cref="ValueType"/>) red from the target</returns>
        object GetValue(object target);
        /// <summary>
        /// Writes the <paramref name="value"/> to the target. <br/>
        /// The <paramref name="target"/> <b>must be compatible</b> with <see cref="ObjectType"/> and 
        /// the <paramref name="value"/> <b>must be compatible</b> with <see cref="ValueType"/>
        /// </summary>
        /// <param name="target">The object compatible with <see cref="ObjectType"/></param>
        /// <param name="value">The value to be written, compatible with <see cref="ValueType"/></param>
        void SetValue(object target, object value);
        /// <summary>
        /// Duplicates the accessor
        /// </summary>
        /// <returns>Completly identical duplicate of the accessor</returns>
        IAccessor Duplicate();
        /// <summary>
        /// Make this accessor concurrent to be used in multithreaded environment.
        /// </summary>
        /// <remarks>The concurrent accessor is slower but it avoids race conditions</remarks>
        /// <returns>The concurrent version of this accessor</returns>
        IConcurrentAccessor MakeConcurrent();
    }

    /// <summary>
    /// This accessor is a wrapper for other accessors. <br/>
    /// Used only for internal purposes.
    /// </summary>
    internal interface IWrapperAccessor
    {
        /// <summary>
        /// The inner accessors of this accessor
        /// </summary>
        /// <returns></returns>
        IEnumerable<object> GetInnerAccessors();
    }

    /// <summary>
    /// A concurrent <see cref="IAccessor"/> which is slower than traditional one, but can be used a multithreaded environment.
    /// </summary>
    public interface IConcurrentAccessor
    {
        /// <summary>
        /// Reads the value out of the target. <br/>
        /// The <paramref name="target"/> must be compatible with <see cref="IAccessor.ObjectType"/>
        /// </summary>
        /// <param name="target">The object compatible with <see cref="IAccessor.ObjectType"/></param>
        /// <returns>The value (usually of <see cref="IAccessor.ValueType"/>) red from the target</returns>
        object GetValue(object target);
        /// <summary>
        /// Writes the <paramref name="value"/> to the target. <br/>
        /// The <paramref name="target"/> must be compatible with <see cref="IAccessor.ObjectType"/> and 
        /// the <paramref name="value"/> must be compatible with <see cref="IAccessor.ValueType"/>
        /// </summary>
        /// <param name="target">The object compatible with <see cref="IAccessor.ObjectType"/></param>
        /// <param name="value">The value to be written, compatible with <see cref="IAccessor.ValueType"/></param>
        void SetValue(object target, object value);
    }

    /// <summary>
    /// A partially specialized <see cref="IAccessor"/>.
    /// </summary>
    /// <remarks>Please note that <b>this accessor does not inherit</b> from <see cref="IAccessor"/>. 
    /// This is because of optimization reasons.</remarks>
    /// <typeparam name="T">Type of the value this accessor reads/writes</typeparam>
    public interface IAccessor<T>
    {
        /// <summary>
        /// Reads the value out of the target. <br/>
        /// The <paramref name="target"/> must be compatible with <see cref="IAccessor.ObjectType"/>
        /// </summary>
        /// <param name="target">The object compatible with <see cref="IAccessor.ObjectType"/></param>
        /// <returns>The value of type <typeparamref name="T"/> red from the target</returns>
        T GetValue(object target);
        /// <summary>
        /// Writes the <paramref name="value"/> to the target. <br/>
        /// The <paramref name="target"/> must be compatible with <see cref="ObjectType"/> and 
        /// the <paramref name="value"/> must be compatible with <see cref="ValueTuple"/>
        /// </summary>
        /// <param name="target">The object compatible with <see cref="ObjectType"/></param>
        /// <param name="value">The value to be written, compatible with <see cref="IAccessor.ValueType"/></param>
        void SetValue(object target, in T value); // TODO: <-- consider removing "in" keyword if performance is bad
        /// <summary>
        /// Make this accessor concurrent to be used in multithreaded environment.
        /// </summary>
        /// <remarks>The concurrent accessor is slower but it avoids race conditions</remarks>
        /// <returns>The concurrent version of this accessor</returns>
        IConcurrentAccessor<T> MakeConcurrent();
    }

    /// <summary>
    /// An <see cref="IAccessor"/> which stores paramter values.
    /// </summary>
    /// <remarks>Please note that <b>this accessor does not inherit</b> from <see cref="IAccessor"/>. 
    /// This is because of optimization reasons.</remarks>
    public interface IParametricAccessor
    {
        /// <summary>
        /// Which parameter is considererd the main one in <see cref="Parameters"/>. <br/>
        /// The main parameter will serve as the value write.
        /// </summary>
        int MainParamIndex { get; set; }
        /// <summary>
        /// The parameters values
        /// </summary>
        object[] Parameters { get; set; }
    }

    /// <summary>
    /// A concurrent <see cref="IAccessor{T}"/> which is slower than traditional one, but can be used a multithreaded environment.
    /// </summary>
    public interface IConcurrentAccessor<T>
    {
        /// <summary>
        /// Reads the value out of the target. <br/>
        /// The <paramref name="target"/> must be compatible with <see cref="IAccessor.ObjectType"/>
        /// </summary>
        /// <param name="target">The object compatible with <see cref="IAccessor.ObjectType"/></param>
        /// <returns>The value of type <typeparamref name="T"/> red from the target</returns>
        T GetValue(object target);
        /// <summary>
        /// Writes the <paramref name="value"/> to the target. <br/>
        /// The <paramref name="target"/> must be compatible with <see cref="ObjectType"/> and 
        /// the <paramref name="value"/> must be compatible with <see cref="ValueTuple"/>
        /// </summary>
        /// <param name="target">The object compatible with <see cref="ObjectType"/></param>
        /// <param name="value">The value to be written, compatible with <see cref="IAccessor.ValueType"/></param>
        void SetValue(object target, in T value); // TODO: <-- considere removing "in" keyword if performance is low
    }

    /// <summary>
    /// A fully specialized <see cref="IAccessor"/>
    /// </summary>
    /// <remarks>Please note that <b>this accessor does not inherit</b> from <see cref="IAccessor"/>. 
    /// This is because of optimization reasons.</remarks>
    /// <typeparam name="S">The type of the object to read/write values from/to</typeparam>
    /// <typeparam name="T">The type of data to read/write</typeparam>
    public interface IAccessor<S, T>
    {
        /// <summary>
        /// Reads the value from the target.
        /// </summary>
        /// <param name="target">The target to read the data from</param>
        /// <returns>The value of type <typeparamref name="T"/></returns>
        T GetValue(S target);

        /// <summary>
        /// Writes the value to the target.
        /// </summary>
        /// <param name="target">The target to write the data to</param>
        /// <param name="value">The value to write</param>
        void SetValue(ref S target, in T value); // TODO: <-- consider removing "in" keyword if performance is low
        /// <summary>
        /// Make this accessor concurrent to be used in multithreaded environment.
        /// </summary>
        /// <remarks>The concurrent accessor is slower but it avoids race conditions</remarks>
        /// <returns>The concurrent version of this accessor</returns>
        IConcurrentAccessor<S, T> MakeConcurrent();
    }

    /// <summary>
    /// A concurrent <see cref="IAccessor{S, T}"/> which is slower than traditional one, but can be used a multithreaded environment.
    /// </summary>
    public interface IConcurrentAccessor<S, T>
    {
        /// <summary>
        /// Reads the value from the target.
        /// </summary>
        /// <param name="target">The target to read the data from</param>
        /// <returns>The value of type <typeparamref name="T"/></returns>
        T GetValue(S target);
        /// <summary>
        /// Writes the value to the target.
        /// </summary>
        /// <param name="target">The target to write the data to</param>
        /// <param name="value">The value to write</param>
        void SetValue(S target, in T value); // TODO: <-- considere removing "in" keyword if performance is low
    }

    /// <summary>
    /// An accessor which is bound to a certain target. <br/>
    /// This accessor is crucial for chained accessors.
    /// </summary>
    /// <typeparam name="T">The type of date to read/write</typeparam>
    public interface IBoundAccessor<T>
    {
        /// <summary>
        /// Get the value from the bound target
        /// </summary>
        /// <returns></returns>
        T GetValue();
        /// <summary>
        /// A version of GetValue which optimizes the accessors chain
        /// </summary>
        /// <returns></returns>
        T GetValueToSet();
        /// <summary>
        /// Writes the value to the bound object
        /// </summary>
        /// <param name="value"></param>
        void SetValue(in T value);
    }
    
    /// <summary>
    /// An accessor which can be compiled to a delegate for faster execution.
    /// </summary>
    /// <typeparam name="S"></typeparam>
    /// <typeparam name="T"></typeparam>
    public interface ICompiledAccessor<S, T>
    {
        RefGetterDelegate<S, T> CompileGetter();
        RefSetterDelegate<S, T> CompileSetter();
    }
    
    public interface IBoundCompiledAccessor<T>
    {
        RefBoundGetterDelegate<T> CompileBoundGetter();
        RefBoundGetterDelegate<T> CompileBoundGetterForSet();
        RefBoundSetterDelegate<T> CompileBoundSetter();
    }
    
    public interface ICompiledAccessor<T>
    {
        RefGetterDelegate<T> CompileGetter();
        RefSetterDelegate<T> CompileSetter();
    }

    /// <summary>
    /// An <see cref="IAccessor"/> node in a chain of accessors.
    /// </summary>
    public interface IAccessorLink
    {
        /// <summary>
        /// The previous node to this node
        /// </summary>
        IAccessorLink Previous { get; set; }
        /// <summary>
        /// The next node of this node
        /// </summary>
        IAccessorLink Next { get; set; }
    }

    /// <summary>
    /// A provider is crucial when extending the binding system functionality. 
    /// It allows to have a custom collection of <see cref="IAccessor"/>s.
    /// </summary>
    public interface IAccessorProvider
    {
        /// <summary>
        /// The unique id of this provider. A non-unique Id will fail the registration.
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Tries to get the path out of an Id, which typically is the full path from bind menu
        /// </summary>
        /// <param name="id">The id of the path, usually coincides with the full bind path in the bind menu</param>
        /// <param name="separator">The string used to separate various parts of the path</param>
        /// <param name="path">The canonical path used by <see cref="AccessorsFactory"/></param>
        /// <returns>True if the conversion succeeded, false otherwise</returns>
        bool TryConvertIdToPath(string id, string separator, out string path);
        
        /// <summary>
        /// Returns a list of available <see cref="AccessorPath"/>s for <paramref name="source"/>
        /// </summary>
        /// <remarks>The list will be further filtered by the binding system to find only compatible paths</remarks>
        /// <param name="source">The source to get the list from</param>
        /// <returns>A list of <see cref="AccessorPath"/></returns>
        IEnumerable<AccessorPath> GetAvailablePaths(object source);
        
        /// <summary>
        /// Builds and returns an <see cref="IAccessor"/> to handle the specified 
        /// <paramref name="pathId"/> and the <param name="sourceType"> the path points to</param>. <br/>
        /// The path is usually the one selected by the user in the bind menu.
        /// </summary>
        /// <param name="sourceType">The type where the path points to</param>
        /// <param name="pathId">The path id to get the accessor for</param>
        /// <returns>The <see cref="IAccessor"/> which handles the path, or null if path is not compatible</returns>
        IAccessor GetAccessor(Type sourceType, string pathId);
    }

    /// <summary>
    /// A more specific <see cref="IAccessorProvider"/> which focuses mainly on <see cref="Component"/> types
    /// </summary>
    public interface IComponentAccessorProvider : IAccessorProvider
    {
        /// <summary>
        /// Gets the component of type <typeparamref name="T"/> from the specified <see cref="GameObject"/>
        /// </summary>
        /// <param name="gameObject">The gameobject to get the component from</param>
        /// <returns>The <typeparamref name="T"/> component or null</returns>
        Component GetComponent(GameObject gameObject);
    }
}

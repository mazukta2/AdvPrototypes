using System;
using System.Collections.Generic;

namespace Postica.BindingSystem.Accessors
{
    /// <summary>
    /// This class may serve as a starting point to implement custom <see cref="IAccessorProvider"/>s
    /// </summary>
    /// <typeparam name="T">The type of the object this provider should get the data from</typeparam>
    public abstract class BaseAccessorProvider<T> : IAccessorProvider where T : UnityEngine.Object
    {
        /// <summary>
        /// The id of the provider, should be unique and is also used when showing the bind menu
        /// </summary>
        public abstract string Id { get; }
        
        /// <summary>
        /// Returns a list of available <see cref="AccessorPath"/>s for <paramref name="source"/> of type <typeparamref name="T"/>
        /// </summary>
        /// <remarks>The list will be further filtered by the binding system to find only compatible paths</remarks>
        /// <param name="source">The source to get the list from</param>
        /// <returns>A list of <see cref="AccessorPath"/></returns>
        public abstract IEnumerable<AccessorPath> GetAvailablePaths(T source);

        /// <summary>
        /// Builds and returns an <see cref="IAccessor"/> to handle the specified <paramref name="path"/>. <br/>
        /// The path is usually the one selected by the user in the bind menu.
        /// </summary>
        /// <param name="path">The path to get the accessor for</param>
        /// <returns>The <see cref="IAccessor"/> which handles the path, or null if path is not compatible</returns>
        protected abstract IAccessor GetAccessor(string path);

        /// <inheritdoc/>
        public IAccessor GetAccessor(Type sourceType, string path)
        {
            return typeof(T).IsAssignableFrom(sourceType) ? GetAccessor(path) : null;
        }

        /// <inheritdoc/>
        public IEnumerable<AccessorPath> GetAvailablePaths(object source)
        {
            return TryGetPreciseObject(source, out var component) ? GetAvailablePaths(component) : Array.Empty<AccessorPath>();
        }

        /// <summary>
        /// Tries to get the correct source object from the specified source. Useful when the source is part of a bigger object.
        /// </summary>
        /// <param name="source">The source to get the object from</param>
        /// <param name="tSource">The object of type <typeparamref name="T"/> to get</param>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        protected abstract bool TryGetPreciseObject(object source, out T tSource);

        /// <inheritdoc/>
        public abstract bool TryConvertIdToPath(string id, string separator, out string path);
    }
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Postica.BindingSystem.Accessors
{
    /// <summary>
    /// A more contextual representation of the binding path.
    /// </summary>
    public sealed class AccessorPath
    {
        /// <summary>
        /// This prefix is used to mark the path which points to an Array element
        /// </summary>
        public const string ArrayPrefix = @"[i]";
        /// <summary>
        /// This prefix is used to mark the path which is handled by a provider
        /// </summary>
        public const string ProviderPrefix = @"[P]";
        /// <summary>
        /// The separator between logical parts of a binding path
        /// </summary>
        public const string Separator = ".";

        private const string _providerPathFormat = ProviderPrefix + @"[{0}]";
        private const string _providerPattern = @"\[P\]\[([\w\d_ ]*)\]";
        private static readonly Regex _providerPathRegex = new Regex(_providerPattern);

        /// <summary>
        /// The parent of this path, the hierarchy allows to have relative paths
        /// </summary>
        public AccessorPath Parent { get; set; }
        /// <summary>
        /// Whether this path can handle <see cref="BindMode.Read"/>, <see cref="BindMode.Write"/> or both
        /// </summary>
        public BindMode BindMode { get; set; }
        /// <summary>
        /// The id of the path. The id is used by bind menu to uniquely identify the path
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// The value this path originated from, or contains along the path. <br/><i>Used only for preview purposes</i>
        /// </summary>
        public object Object { get; }
        /// <summary>
        /// The type of the value this path points to.
        /// </summary>
        public Type Type { get; }

        internal string MenuPath { get; }
        internal bool IsSealed { get; }
        internal List<AccessorPath> Children { get; } = new List<AccessorPath>();
        internal string Name => MenuPath.Substring(MenuPath.LastIndexOf('/') + 1);
        internal string Description { get; }

        /// <summary>
        /// Constructor. Builds the path with specified parameters
        /// </summary>
        /// <param name="provider">The provider of this path, if any</param>
        /// <param name="id">The id of the path</param>
        /// <param name="mode">How this path accesses the data</param>
        /// <param name="type">The type of the data this path points to</param>
        /// <param name="menuPath">[Optional] The path to be shown in the menu. Default is <paramref name="id"/></param>
        /// <param name="isSealed">[Optional] When true, further path chains from <paramref name="type"/> are not allowed</param>
        /// <param name="description">[Optional] The description to be shown in the menu. Default is empty</param>
        public AccessorPath(IAccessorProvider provider, string id, BindMode mode, Type type, string menuPath = null, bool isSealed = true, string description = null)
            : this(provider, null, id, mode, type, null, menuPath, isSealed, description)
        {
        }

        /// <summary>
        /// Constructor. Builds the path with specified parameters
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="parent"></param>
        /// <param name="id"></param>
        /// <param name="mode"></param>
        /// <param name="type"></param>
        /// <param name="object"></param>
        /// <param name="menuPath"></param>
        /// <param name="isSealed"></param>
        /// <param name="description"></param>
        /// <exception cref="ArgumentException"></exception>
        public AccessorPath(IAccessorProvider provider, AccessorPath parent, string id, BindMode mode, Type type, object @object, string menuPath = null, bool isSealed = true, string description = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException($"'{nameof(id)}' cannot be null or whitespace.", nameof(id));
            }

            Parent = parent;
            Id = provider != null ? string.Format(_providerPathFormat, provider.Id) + id : id;
            BindMode = mode;
            Type = type;
            Object = @object;
            MenuPath = menuPath ?? id;
            IsSealed = isSealed;
            Description = description;

            if(parent != null)
            {
                parent.Children.Add(this);
            }
        }

        /// <summary>
        /// Tries to infer the provider id out of the specified path id
        /// </summary>
        /// <param name="pathId">The id of the path</param>
        /// <param name="providerId">Output var. The provider id part of the path</param>
        /// <param name="cleanId">Output var. The rest of the path without provider id</param>
        /// <returns>True if the operation succeeded, false otherwise</returns>
        public static bool TryGetProviderId(string pathId, out string providerId, out string cleanId)
        {
            var match = _providerPathRegex.Match(pathId);
            if (match.Success)
            {
                providerId = match.Groups[1].Value;
                cleanId = pathId.Substring(match.Length);
                return true;
            }

            providerId = null;
            cleanId = null;
            return false;
        }

        internal static string CleanPath(string pathId)
        {
            return pathId.Replace(ArrayPrefix, "", StringComparison.Ordinal).Replace(ProviderPrefix, "", StringComparison.Ordinal);
        }
    }
}

using Postica.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This class will store and provide different types of <see cref="IModifier"/>s.
    /// </summary>
    public class ModifiersFactory
    {
        /// <summary>
        /// A template from which to create a modifier. <br/>
        /// Useful when the modifier instance is not needed immediately and it can be instead lazily created.
        /// </summary>
        public interface IModifierTemplate
        {
            /// <summary>
            /// The unique id of the modifier. See <see cref="IModifier.Id"/>.
            /// </summary>
            string ModifierId { get; }
            /// <summary>
            /// Whether to allow similar modifiers from base types to be added.
            /// </summary>
            bool AllowSimilarModifiers { get; }
            /// <summary>
            /// Returns a Boolean indicating if the modifier will be able to modify the given <paramref name="type"/>.
            /// </summary>
            /// <param name="type">The type to check whether the modifier is capable of modify or not.</param>
            /// <returns>True if the type can be modified by the modifier, False otherwise.</returns>
            bool CanModifyType(Type type);
            /// <summary>
            /// Returns the type of modifier on which this template is based.
            /// </summary>
            Type OriginalType { get; }
            /// <summary>
            /// Tries go get the <see cref="BindMode"/> the modifier will be capable to modify the <paramref name="type"/> with.
            /// </summary>
            /// <param name="type">The type get the modification bind mode for.</param>
            /// <param name="mode">[Output] the resulting mode.</param>
            /// <returns>True if it was possible to infer the mode, False otherwise.</returns>
            bool TryGetBindModeFor(Type type, out BindMode mode);
            /// <summary>
            /// Creates the <see cref="IModifier"/> which will operate in the given <paramref name="mode"/>.
            /// </summary>
            /// <param name="mode">The <see cref="BindMode"/> the modifier will operate in.</param>
            /// <returns>The modifier.</returns>
            IModifier Create(BindMode mode = BindMode.ReadWrite);
        }
 
        internal class ModifierTemplate : IModifierTemplate
        {
            private readonly string _modifierId;
            private readonly HashSet<Type> _handlingTypes;
            private readonly Type _modifierType;
            private readonly bool _allowDerivedTypes;
            private readonly bool _allowSimilarTypes;
            private readonly Dictionary<Type, BindMode> _modes;
            private readonly Action<object, object> _modeSetter;

            internal IEnumerable<Type> HandlingTypes => _handlingTypes;

            public string ModifierId => _modifierId;

            public Type OriginalType => _modifierType;
            
            public bool AllowSimilarModifiers => _allowSimilarTypes;

            internal ModifierTemplate(Type modifierType)
            {
                _modifierId = GetId(modifierType) ?? GenerateId(modifierType);
                _modifierType = modifierType;
                _handlingTypes = new HashSet<Type>();
                _modes = new Dictionary<Type, BindMode>();
                
                var attr = modifierType.GetCustomAttribute<ModifierOptionsAttribute>();
                if (attr != null)
                {
                    _allowDerivedTypes = attr.AllowForDerivedTypes;
                    _allowSimilarTypes = attr.AllowSimilarTypes;
                }
                else
                {
                    _allowDerivedTypes = true;
                    _allowSimilarTypes = false;
                }

                bool? addGeneric = null;
                BindMode? defaultMode = null;
                
                var interfaces = modifierType.GetInterfaces();
                foreach (var iface in interfaces)
                {
                    if (!typeof(IModifier).IsAssignableFrom(iface)) continue;
                    
                    if (iface.GetGenericArguments()?.Length == 1)
                    {
                        var genericArg = iface.GetGenericArguments()[0];
                        _handlingTypes.Add(genericArg);

                        if(iface.GetGenericTypeDefinition() == typeof(IReadWriteModifier<>))
                        {
                            if (_modeSetter == null)
                            {
                                var bindModeProperty = modifierType.GetProperty(nameof(IReadWriteModifier<bool>.ModifyMode));
                                if (bindModeProperty != null)
                                {
                                    _modeSetter = bindModeProperty.GetSetMethod() != null 
                                                ? bindModeProperty.SetValue
                                                : null;
                                }
                            }
                            _modes[genericArg] = BindMode.ReadWrite;
                            addGeneric = false;
                        }
                        else if(iface.GetGenericTypeDefinition() == typeof(IReadModifier<>))
                        {
                            if (!_modes.TryGetValue(genericArg, out var mode))
                            {
                                _modes[genericArg] = BindMode.Read;
                            }
                            else if (!mode.CanRead())
                            {
                                _modes[genericArg] = BindMode.ReadWrite;
                            }

                            addGeneric = false;
                        }
                        else if (iface.GetGenericTypeDefinition() == typeof(IWriteModifier<>))
                        {
                            if (!_modes.TryGetValue(genericArg, out var mode))
                            {
                                _modes[genericArg] = BindMode.Write;
                            }
                            else if (!mode.CanWrite())
                            {
                                _modes[genericArg] = BindMode.ReadWrite;
                            }

                            addGeneric = false;
                        }
                        else if (iface.GetGenericTypeDefinition() == typeof(IObjectModifier<>))
                        {
                            if (_modeSetter == null)
                            {
                                var bindModeProperty = modifierType.GetProperty(nameof(IReadWriteModifier<bool>.ModifyMode));
                                if (bindModeProperty != null)
                                {
                                    _modeSetter = bindModeProperty.GetSetMethod() != null 
                                        ? bindModeProperty.SetValue
                                        : null;
                                }
                            }
                            
                            defaultMode ??= TryGetModifyModeFor(modifierType);
                            _modes[genericArg] = defaultMode.Value;
                            addGeneric = false;
                        }

                        addGeneric ??= true;
                    }

                }

                if (addGeneric == true)
                {
                    defaultMode ??= TryGetModifyModeFor(modifierType);
                    _handlingTypes.Add(typeof(object));
                    _modes[typeof(object)] = defaultMode.Value;
                }
            }
            
            private BindMode TryGetModifyModeFor(Type type)
            {
                var bindModeAttr = type.GetCustomAttribute<ModifierOptionsAttribute>();
                if (bindModeAttr?.ModifierMode != null)
                {
                    return bindModeAttr.ModifierMode.Value;
                }
                 
                try
                {
                    var instance = Activator.CreateInstance(type) as IModifier;
                    return instance.ModifyMode;
                }
                catch
                {
                    // ignored
                }

                return BindMode.Read;
            }

            private string GetId(Type modifierType)
            {
                try
                {
                    var instance = Activator.CreateInstance(modifierType) as IModifier;
                    return instance.Id;
                }
                catch { }

                return null;
            }

            public bool CanModifyType(Type type)
            {
                if(!_handlingTypes.Contains(type) && !type.IsValueType)
                {
                    foreach(var t in _handlingTypes)
                    {
                        if (t == type)
                        {
                            return true;
                        }
                        if (_allowDerivedTypes && t.IsAssignableFrom(type))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public IModifier Create(BindMode mode)
            {
                var modifier = Activator.CreateInstance(_modifierType) as IModifier;
                if(modifier == null)
                {
                    Debug.LogError($"[BindingSystem] Unable to create modifier {_modifierType}. " +
                        $"Please check if the modifier is implementing IModifier interface and has a parameterless constructor");
                }
                try
                {
                    _modeSetter?.Invoke(modifier, mode);
                }
                catch
                {
                    Debug.LogWarning($"Modifier of type '{_modifierType}' do not have a setter for BindMode property. " +
                        $"Please consider adding the setter and make the bind mode persistent through serialization " +
                        $"if you intend to use it both in write mode and/or read mode");
                }
                return modifier;
            }

            internal static string GenerateId(Type modifierType) => StringUtility.NicifyName(modifierType.Name.Replace("Modifier", ""));

            public bool TryGetBindModeFor(Type type, out BindMode mode)
            {
                foreach(var pair in _modes)
                {
                    if (pair.Key.IsAssignableFrom(type))
                    {
                        mode = pair.Value;
                        return true;
                    }
                }

                mode = BindMode.ReadWrite;
                return false;
            }
        }

        private static Dictionary<string, IModifier> _modifiersById = new Dictionary<string, IModifier>();
        private static Dictionary<Type, List<IModifier>> _modifiersByType = new Dictionary<Type, List<IModifier>>();

        private static Dictionary<string, IModifierTemplate> _templatesById = new Dictionary<string, IModifierTemplate>();
        private static Dictionary<Type, List<IModifierTemplate>> _templatesByType = new Dictionary<Type, List<IModifierTemplate>>();
        
        /// <summary>
        /// Registers the modifier for further usage by other users.
        /// </summary>
        /// <remarks>If there is already a previously registered modifier with the same id, an <see cref="ArgumentException"/> will be thrown.</remarks>
        /// <param name="modifier">The modifier</param>
        /// <exception cref="ArgumentNullException"> if the modifier is null.</exception>
        /// <exception cref="ArgumentException"> if there is already a modifier with the same <see cref="IModifier.Id"/>.</exception>
        public static void RegisterModifier(IModifier modifier)
        {
            if (modifier is null)
            {
                throw new ArgumentNullException(nameof(modifier));
            }
            if (_modifiersById.TryGetValue(modifier.Id, out var existing))
            {
                if(existing == modifier)
                {
                    // Repeated registration
                    return;
                }
                throw new ArgumentException($"BindingSystem: Modifier with id {modifier.Id} already exists", nameof(modifier));
            }
            _modifiersById.Add(modifier.Id, modifier);
            foreach(var iface in modifier.GetType().GetInterfaces())
            {
                if(typeof(IModifier).IsAssignableFrom(iface) && iface.GetGenericArguments()?.Length == 1)
                {
                    var genericArg = iface.GetGenericArguments()[0];
                    AddToType(genericArg, modifier);
                    if (!genericArg.IsValueType)
                    {
                        var baseType = genericArg.BaseType;
                        while(baseType != typeof(object) && baseType != null)
                        {
                            AddToType(baseType, modifier);
                            baseType = baseType.BaseType;
                        }
                    }
                }
            }

            RegisterTemplate(modifier.GetType());
        }

        /// <summary>
        /// Registers a type of modifier. The modifier will be instantiated only when needed.
        /// </summary>
        /// <typeparam name="T">The type of the modifier to register.</typeparam>
        public static void Register<T>() where T : IModifier => RegisterTemplate(typeof(T));

        /// <summary>
        /// Registers a type of modifier. The modifier will be instantiated only when needed.
        /// </summary>
        /// <param name="modifierType">The type of the modifier to register.</param>
        /// <exception cref="ArgumentNullException"> if the specified <paramref name="modifierType"/> is null.</exception>
        /// <exception cref="ArgumentException"> if the specified <paramref name="modifierType"/> is not a <see cref="IModifier"/> type.</exception>
        public static void RegisterTemplate(Type modifierType)
        {
            if (modifierType is null)
            {
                throw new ArgumentNullException(nameof(modifierType));
            }
            if (!typeof(IModifier).IsAssignableFrom(modifierType))
            {
                Debug.LogError(BindSystem.DebugPrefix + $"Type {modifierType} does not implement {nameof(IModifier)} and won't be registered.");
                return;
            }

            var id = ModifierTemplate.GenerateId(modifierType);
            if (_templatesById.ContainsKey(id))
            {
                Debug.LogWarning(BindSystem.DebugPrefix + $"Modifier Template with id {id} already registered.");
                return;
            }

            var template = new ModifierTemplate(modifierType);
            _templatesById.Add(id, template);

            foreach(var type in template.HandlingTypes)
            {
                AddToType(type, template);
                if (!type.IsValueType)
                {
                    var baseType = type.BaseType;
                    while (baseType != typeof(object) && baseType != null)
                    {
                        AddToType(baseType, template);
                        baseType = baseType.BaseType;
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of all modifiers which can modify <typeparamref name="T"/>s.
        /// </summary>
        /// <typeparam name="T">The type to get the compatible modifiers for.</typeparam>
        /// <returns>A read-only list of modifiers.</returns>
        public static IReadOnlyList<IModifier> GetModifiersFor<T>() => GetFor(typeof(T), false);
        /// <summary>
        /// Returns a list of all modifiers which can modify data of specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type to get the compatible modifiers for.</param>
        /// <returns>A read-only list of modifiers.</returns>
        public static IReadOnlyList<IModifier> GetModifiersFor(Type type) => GetFor(type, false);

        private static void AddToType(Type type, IModifier modifier)
        {
            var list = GetFor(type, true);
            if (!list.Contains(modifier))
            {
                list.Add(modifier);
            }
        }

        private static List<IModifier> GetFor(Type type, bool createIfNull)
        {
            if(!_modifiersByType.TryGetValue(type, out var list) && createIfNull)
            {
                list = new List<IModifier>();
                _modifiersByType[type] = list;
            }
            return list;
        }

        /// <summary>
        /// Returns a list of all modifier templates which can modify <typeparamref name="T"/>s. 
        /// The templates can be further used to create <see cref="IModifier"/>s to include in various pipelines.
        /// </summary>
        /// <typeparam name="T">The type to get the compatible modifiers' templates for.</typeparam>
        /// <returns>A read-only list of modifier templates.</returns>
        public static IReadOnlyList<IModifierTemplate> GetTemplatesFor<T>() => GetTemplatesFor(typeof(T), false);
        /// <summary>
        /// Returns a list of all modifier templates which can modify data of specified <paramref name="type"/>. 
        /// The templates can be further used to create <see cref="IModifier"/>s to include in various pipelines.
        /// </summary>
        /// <param name="type">The type to get the compatible modifiers' templates for.</param>
        /// <returns>A read-only list of modifier templates.</returns>
        public static IReadOnlyList<IModifierTemplate> GetTemplatesFor(Type type) => GetTemplatesFor(type, false);

        private static void AddToType(Type type, IModifierTemplate template)
        {
            var list = GetTemplatesFor(type, true, true);
            if (!list.Contains(template))
            {
                list.Add(template);
            }
        }

        private static List<IModifierTemplate> GetTemplatesFor(Type type, bool createIfNull, bool addToBaseTypes = true)
        {
            if(type == null)
            {
                return null;
            }
            if (!_templatesByType.TryGetValue(type, out var list) && createIfNull)
            {
                list = new List<IModifierTemplate>();
                _templatesByType[type] = list;
            }

            if (list != null || !addToBaseTypes || type.IsValueType) return list;
            
            list = new List<IModifierTemplate>();
            
            var baseType = type.BaseType;
            while (baseType != null)
            {
                var baseList = GetTemplatesFor(baseType, createIfNull, false);
                if (baseList != null)
                {
                    foreach (var template in baseList)
                    {
                        if (list.Contains(template)) continue;

                        if (template.CanModifyType(type))
                        {
                            list.Add(template);
                        }
                    }
                }
                baseType = baseType.BaseType;
            }
            
            _templatesByType[type] = list;
            
            return list;
        }

        /// <summary>
        /// Returns a Boolean indicating if the factory contains modifiers which can handle the <paramref name="type"/> Type.
        /// </summary>
        /// <param name="type">The type to check against.</param>
        /// <returns>True if there are compatible modifiers, False otherwise.</returns>
        public static bool HasModifiersFor(Type type)
        {
            return _modifiersByType.ContainsKey(type) || _templatesByType.ContainsKey(type);
        }
    }
}

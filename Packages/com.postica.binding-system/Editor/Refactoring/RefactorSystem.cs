using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable InconsistentNaming

namespace Postica.BindingSystem.Refactoring
{
    internal static class RefactorSystem
    {
        private const string _refactorsFilepath = "Library/bs-refactors.json";
        
        private static bool _isInitialized;
        
        private static readonly Regex _arrayRegex = new(@"\.Array\.data(\[\d+\]|\[i\])");
        private static readonly Dictionary<string, Refactor> _refactors = new();
        private static readonly Dictionary<(string rootType, string path), string> _cachedKeys = new();
        
        private static string ToKey(string rootTypename, string path)
        {
            if (_cachedKeys.TryGetValue((rootTypename, path), out var key)) return key;
            
            var (typename, member, _) = GetMissingMember(rootTypename, path);
            key = typename + "#" + member;
            _cachedKeys[(rootTypename, path)] = key;

            return key;
        }

        private static (string typename, string member, bool isLastMember) GetMissingMember(string rootTypename, string path)
        {
            if (!path.Contains('.'))
            {
                return (rootTypename, path, true);
            }
            
            var split = _arrayRegex.Replace(path, "").Split('.');
            var rootType = Type.GetType(rootTypename, throwOnError: false);
            if (rootType == null)
            {
                return (rootTypename, split[0], split.Length == 1);
            }
            
            var type = rootType;
            var lastType = type;
            var lastMember = split[0];
            for (var i = 0; i < split.Length; i++)
            {
                var memberName = split[i];
                var typeRecursionChecks = 4;

                while(type != null && typeRecursionChecks-- > 0)
                {
                    if (type.IsArray)
                    {
                        type = type.GetElementType();
                    }
                    else if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        var baseType = type;
                        while(baseType != null && !baseType.IsGenericType)
                        {
                            baseType = baseType.BaseType;
                        }
                        if (baseType == null)
                        {
                            break;
                        }
                        type = baseType.GetGenericArguments()[0];
                    }
                    else
                    {
                        break;
                    }
                }
                
                if(type == null)
                {
                    return (lastType.AssemblyQualifiedName, lastMember, i == split.Length - 1);
                }
                
                lastMember = memberName;

                var members = type.GetMember(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var member = members.FirstOrDefault();
                if (member == null)
                {
                    return (type.AssemblyQualifiedName, memberName, i == split.Length - 1);
                }

                lastType = type;
                type = member switch
                {
                    FieldInfo f => f.FieldType,
                    PropertyInfo p => p.PropertyType,
                    // TODO: Add support for other member types
                    _ => null
                };
                
                if (type == null)
                {
                    return (lastType.AssemblyQualifiedName, member.Name, i == split.Length - 1);
                }
            }
            
            // Something went wrong, we're in a state where we can't find the missing member
            throw new Exception(BindSystem.DebugPrefix + "Failed to find missing member for path: " + path);
        }
        
        private static (string type, string member) FromKey(string key)
        {
            var split = key.Split('#');
            return (split[0], split[1]);
        }
        
        public static IEnumerable<Refactor> AllRefactors => _refactors.Values;

        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            
            var refactorsFile = RefactorsFile.Load();
            var needsSave = false;
            foreach (var refactor in refactorsFile.GetRefactors())
            {
                if (refactor.IsAlive())
                {
                    needsSave = true;
                    continue;
                }
                _refactors[refactor.Key] = refactor;
            }
            
            if (needsSave)
            {
                RefactorsFile.FromRefactors(_refactors.Values).Save();
            }

            // Hook to all known events
            BindProxy.OnInvalidBindProxy += OnInvalidBindProxy;
        }

        private static void OnInvalidBindProxy(BindProxy proxy)
        {
            if (!proxy.Source)
            {
                // No source, nothing to refactor
                return;
            }
            
            var refactoringIsEnabled = BindingSettings.Current.EnableRefactoring;
            if (!refactoringIsEnabled)
            {
                Debug.LogWarning(BindSystem.DebugPrefix + $"Refactoring is disabled, removing invalid proxy {proxy.SourceTypeFullName}.{proxy.Path}");
                proxy.Provider?.RemoveProxy(proxy);
                return;
            }
            
            var isUnityClass = proxy.Source.GetType().FullName?.StartsWith("Unity", StringComparison.Ordinal) == true;
            if(isUnityClass && !BindingSettings.Current.EnableUnityClassesRefactoring)
            {
                Debug.LogWarning(BindSystem.DebugPrefix + $"Refactoring Unity classes is disabled, ignoring invalid proxy {proxy.SourceTypeFullName}.{proxy.Path}. " +
                                 $"Please consider rerouting the field to a different path for this proxy.");
                return;
            }
            
            var key = ToKey(proxy.SourceTypeFullName, proxy.Path);
            
            // If the proxy is invalid, we need to check if there is a refactor for it
            if(!_refactors.TryGetValue(key, out var refactor))
            {
                refactor = new Refactor(proxy.SourceTypeFullName, proxy.Path, "Field", proxy.ValueTypeFullName);
                _refactors[refactor.Key] = refactor;
            }
            
            // Check if the refactor has to values and if they are valid
            if (refactor.IsReady && refactor.ToMemberInstance is FieldInfo f && f.FieldType.AssemblyQualifiedName == proxy.ValueTypeFullName)
            {
                if (proxy.Context)
                {
                    EditorUtility.SetDirty(proxy.Context);
                }
                
                proxy.Path = refactor.RefactorPath(proxy.Path);
                // proxy.SourceType = refactor.ToTypeInstance; // TODO: Use ToRootTypeInstance
                proxy.RefreshProxy();
                return;
            }
            
            if (refactor.IsReady && refactor.IsToRemove)
            {
                proxy.Provider?.RemoveProxy(proxy);
                return;
            }

            refactor.AddAction(r =>
            {
                if(proxy.Context)
                {
                    EditorUtility.SetDirty(proxy.Context);
                }
                
                if(r.IsToRemove)
                {
                    proxy.Provider?.RemoveProxy(proxy);
                    return;
                }
                proxy.Path = r.RefactorPath(proxy.Path);
                // proxy.SourceType = r.ToTypeInstance; // TODO: Use ToRootTypeInstance
                proxy.RefreshProxy();
            });
            
            EditorApplication.delayCall -= ShowRefactorWindow;
            EditorApplication.delayCall += ShowRefactorWindow;
        }

        public static void ApplyAllRefactors()
        {
            var validRefactors = _refactors.Values.Where(r => r.IsReady).ToList();
            var oldRefactors = _refactors.Values.Select(r => (r.toType + "#" + r.toMember, r)).ToList();

            var refactorsToSave = new Dictionary<string, Refactor>(_refactors.Where(p => p.Value.isPersistent));
            
            // Then apply all valid refactors
            foreach (var refactor in validRefactors.ToArray())
            {
                refactor.Apply();
                foreach (var (key, oldRefactor) in oldRefactors)
                {
                    if(key != refactor.Key) continue;
                    
                    oldRefactor.toType = refactor.toType;
                    oldRefactor.toMember = refactor.toMember;
                    oldRefactor.Refresh();
                }
                refactor.isPersistent = true;
                refactorsToSave[refactor.Key] = refactor;
            }
            
            // Save all valid refactors
            var refactorsFile = RefactorsFile.FromRefactors(refactorsToSave.Values);
            refactorsFile.Save();
        }
        
        private static void ShowRefactorWindow()
        {
            RefactorWindow.ShowWindow();
        }
        
        public class Refactor
        {
            private const BindingFlags MemberBindingFlags = BindingFlags.Public 
                                                            | BindingFlags.NonPublic 
                                                            | BindingFlags.Instance 
                                                            | BindingFlags.Static;
            public bool isPersistent;
            
            public string fromType;
            public string fromMember;
            public string fromMemberKind;
            public string fromMemberType;
            
            public string toType;
            public string toMember;

            private bool? _isReady;
            private Type _toType;
            private MemberInfo _toMember;
            
            private readonly List<Action<Refactor>> _actionsToPerform = new();
            
            public int ActionCount => _actionsToPerform.Count;
            public int SuccessCount { get; private set; }
            
            public Type ToTypeInstance => _toType;
            
            public MemberInfo ToMemberInstance => _toMember;
            
            public string Key => ToKey(fromType, fromMember);

            public bool IsToRemove => toType == "[remove]";
            public bool IsToIgnore => toType == "[ignore]";
            public bool IsReady
            {
                get
                {
                    if(_isReady.HasValue) return _isReady.Value;
                    
                    if (IsToRemove)
                    {
                        _isReady = true;
                        return true;
                    }
                    
                    if (string.IsNullOrEmpty(toType) || string.IsNullOrEmpty(toMember))
                    {
                        _isReady = false;
                        return false;
                    }
                    
                    _toType = Type.GetType(toType, throwOnError: false);
                    if (_toType == null)
                    {
                        _isReady = false;
                        return false;
                    }
                    
                    _toMember = _toType.GetMember(toMember, MemberBindingFlags).FirstOrDefault();
                    _isReady = _toMember != null;
                    return _isReady.Value;
                }
            }
            
            public Refactor(bool isPersistent)
            {
                this.isPersistent = isPersistent;
            }

            public Refactor(string rootTypename, string path, string pathKind, string pathType)
            {
                var (typename, member, isLastMember) = GetMissingMember(rootTypename, path);
                fromType = typename;
                fromMember = member;
                fromMemberKind = pathKind;
                fromMemberType = isLastMember ? pathType : null;

                isPersistent = AttemptFormerlySerializedAsFix();
            }

            private bool AttemptFormerlySerializedAsFix()
            {
                try
                {
                    if (!BindingSettings.Current.PreferRenamingAutoFix)
                    {
                        return false;
                    }
                    
                    var fromTypeInstance = Type.GetType(fromType, throwOnError: false);
                    if (fromTypeInstance == null)
                    {
                        return false;
                    }
                    
                    var fieldsWithNameChanges = TypeCache.GetFieldsWithAttribute<FormerlySerializedAsAttribute>();
                    foreach (var field in fieldsWithNameChanges)
                    {
                        if (field.DeclaringType?.IsAssignableFrom(fromTypeInstance) != true)
                        {
                            continue;
                        }

                        var oldName = fromMember;
                        var isMatch = true;
                        while (isMatch)
                        {
                            isMatch = false;
                            if (field.GetCustomAttributes<FormerlySerializedAsAttribute>()
                                .Any(formerNameAttribute => formerNameAttribute.oldName == oldName))
                            {
                                toType = field.DeclaringType.AssemblyQualifiedName;
                                toMember = field.Name;
                                _toMember = field;
                                isMatch = true;
                                oldName = field.Name;
                            }
                        }
                        
                        if(toMember != null)
                        {
                            // Success, we found a match
                            return true;
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                return false;
            }

            public string RefactorPath(string path) => path.Replace(fromMember, toMember);
            
            public void Apply()
            {
                SuccessCount = 0;
                foreach (var action in _actionsToPerform)
                {
                    try
                    {
                        action(this);
                        SuccessCount++;
                    } 
                    catch (Exception e) 
                    {
                        Debug.LogError(BindSystem.DebugPrefix + $"Failed to apply refactor {fromType}.{fromMember} -> {toType}.{toMember}: {e}");
                    }
                }

                _isReady = true;
            }

            public void AddAction(Action<Refactor> action)
            {
                _actionsToPerform.Remove(action);
                _actionsToPerform.Add(action);
            }

            public void Refresh()
            {
                _isReady = null;
            }

            public bool IsAlive()
            {
                var type = Type.GetType(fromType, throwOnError: false);
                if (type == null)
                {
                    return false;
                }
                
                var member = type.GetMember(fromMember, MemberBindingFlags).FirstOrDefault();

                return member switch
                {
                    FieldInfo f => fromMemberKind == "Field" && 
                                   (string.IsNullOrEmpty(fromMemberType) ||
                                   f.FieldType.AssemblyQualifiedName == fromMemberType),
                    PropertyInfo p => fromMemberKind == "Property" &&
                                      (string.IsNullOrEmpty(fromMemberType) ||
                                      p.PropertyType.AssemblyQualifiedName == fromMemberType),
                    // TODO: Add support for other member types
                    _ => false
                };
            }
        }

        #region [  SERIALIZATION  ]
        
        [Serializable]
        private class RefactorsFile
        {
            public Item[] items;
            
            public IEnumerable<Refactor> GetRefactors()
            {
                return items.Select(i => i.ToRefactor());
            }
            
            public static RefactorsFile FromRefactors(IEnumerable<Refactor> refactors)
            {
                return new RefactorsFile()
                {
                    items = refactors.Select(Item.FromRefactor).ToArray()
                };
            }
            
            public void Save()
            {
                var json = JsonUtility.ToJson(this, true);
                System.IO.File.WriteAllText(_refactorsFilepath, json);
            }
            
            public static RefactorsFile Load()
            {
                if (!System.IO.File.Exists(_refactorsFilepath))
                {
                    return new RefactorsFile(){ items = Array.Empty<Item>()};
                }
                
                var json = System.IO.File.ReadAllText(_refactorsFilepath);
                return JsonUtility.FromJson<RefactorsFile>(json);
            }
            
            [Serializable]
            public class Item
            {
                public string from;
                public string to;
                
                public Refactor ToRefactor()
                {
                    var splitFrom = from.Split('#');
                    var splitTo = to.Split('#');
                    
                    return new Refactor(true)
                    {
                        fromType = splitFrom[0], 
                        fromMemberType = splitFrom[1],
                        fromMemberKind = splitFrom[2],
                        fromMember = splitFrom[3], 
                        toType = splitTo[0], 
                        toMember = splitTo[1]
                    };
                }
                
                public static Item FromRefactor(Refactor refactor)
                {
                    return new Item()
                    {
                        from = refactor.fromType + "#" + refactor.fromMemberType + "#" + refactor.fromMemberKind + "#" + refactor.fromMember,
                        to = refactor.toType + "#" + refactor.toMember
                    };
                }
            }
        }
            
        #endregion
    }
}
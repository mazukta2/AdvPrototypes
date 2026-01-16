using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Profiling;
using Postica.Common;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Serialization
{
    [InitializeOnLoad]
    internal class BindDatabase : AssetPostprocessor
    {
        private const string ShouldRebuildKey = "[BindDatabase_ShouldRebuildKey]";
        private const string CanDeleteDeltaKey = "[BindDatabase_AutoDeleteDeltaDBKey]";
        private const string FilePath = "Library/BindDB.json";
        private const string PrefabsFilePath = "Library/PrefabsDB.json";
        private const string DeltaFilePath = "Library/BindDeltaDB.json";

        private static HashSet<Type> _stdTypes = new HashSet<Type>()
        {
            typeof(byte),
            typeof(char),
            typeof(ushort),
            typeof(short),
            typeof(uint),
            typeof(int),
            typeof(ulong),
            typeof(long),
            typeof(bool),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(string),
            typeof(Color),
            typeof(LayerMask),
            typeof(Enum),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Rect),
            typeof(AnimationCurve),
            typeof(Bounds),
            typeof(Gradient),
            typeof(Quaternion),
            typeof(Vector2Int),
            typeof(Vector3Int),
            typeof(RectInt),
            typeof(BoundsInt),
            typeof(Hash128)
        };

        [NonSerialized]
        private static FullDatabase _db;
        [NonSerialized]
        private static DeltaDatabase _deltaDb;
        [NonSerialized]
        private static PrefabsDatabase _prefabs;

        internal static FullDatabase Database
        {
            get
            {
                if (_db != null)
                {
                    return _db;
                }

                RebuildTree();
                if (_db != null)
                {
                    return _db;
                }

                try
                {
                    _db = File.Exists(FilePath)
                             ? JsonUtility.FromJson<FullDatabase>(FilePath)
                             : new FullDatabase();
                }
                catch
                {
                    _db = new FullDatabase();
                }
                return _db;
            }
        }

        internal static DeltaDatabase DeltaDb
        {
            get
            {
                if (_deltaDb != null)
                {
                    return _deltaDb;
                }

                RebuildTree();
                if (_deltaDb != null)
                {
                    return _deltaDb;
                }

                try
                {
                    _deltaDb = File.Exists(DeltaFilePath)
                             ? JsonUtility.FromJson<DeltaDatabase>(File.ReadAllText(DeltaFilePath))
                             : new DeltaDatabase();
                }
                catch { _deltaDb = new DeltaDatabase(); }

                return _deltaDb;
            }
        }

        internal static PrefabsDatabase PrefabsDb
        {
            get
            {
                if (_prefabs != null)
                {
                    return _prefabs;
                }

                try
                {
                    if (File.Exists(PrefabsFilePath))
                    {
                        _prefabs = JsonUtility.FromJson<PrefabsDatabase>(File.ReadAllText(PrefabsFilePath));
                    }
                    else
                    {
                        RebuildPrefabs();
                    }
                }
                catch/* (Exception ex) */
                { 
                    RebuildPrefabs(); 
                }

                return _prefabs;
            }
        }
        
        internal static bool CanDeleteDeltaDB
        {
            get => PlayerPrefs.HasKey(CanDeleteDeltaKey);
            set
            {
                if (value)
                {
                    PlayerPrefs.SetInt(CanDeleteDeltaKey, 1);
                }
                else
                {
                    PlayerPrefs.DeleteKey(CanDeleteDeltaKey);
                }
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            var shouldAutoFix = BindingSettings.Current.AutoFixSerializationUpgrade;
            if (!shouldAutoFix)
            {
                return;
            }
            
            if(didDomainReload && importedAssets.Length == 0 && deletedAssets.Length == 0)
            {
                return;
            }

            if (importedAssets.Any(f => f.EndsWith(".cs", StringComparison.Ordinal)))
            {
                PlayerPrefs.SetInt(ShouldRebuildKey, 1);
            }

            Profiler.BeginSample($"BindingSystem.{nameof(BindDatabase)}.ImportPrefabs");

            var overwritePrefabsDb = false;
            foreach(var file in importedAssets)
            {
                if(!file.EndsWith(".prefab", StringComparison.Ordinal))
                {
                    continue;
                }

                var guid = AssetDatabase.AssetPathToGUID(file);
                if (RefreshPrefab(guid))
                {
                    overwritePrefabsDb = true;
                }
            }

            foreach (var file in deletedAssets)
            {
                if (!file.EndsWith(".prefab", StringComparison.Ordinal))
                {
                    continue;
                }

                overwritePrefabsDb = true;

                var guid = AssetDatabase.AssetPathToGUID(file);
                if(!PrefabsDb.TryGetDependantFiles(guid, out var files) || files.Count == 0)
                {
                    continue;
                }

                foreach(var depFile in files)
                {
                    depFile.dependencies.Remove(guid);
                }
            }

            if (overwritePrefabsDb)
            {
                File.WriteAllText(PrefabsFilePath, JsonUtility.ToJson(PrefabsDb));
                PrefabsDb.Refresh();
            }

            Profiler.EndSample();
        }

        private static bool RefreshPrefab(string guid, HashSet<string> processed = null)
        {
            processed ??= new HashSet<string>();

            void ReplacePrefab(PrefabFile old, PrefabFile newFile)
            {
                var index = PrefabsDb.prefabs.IndexOf(old);
                if(index < 0)
                {
                    if(newFile != null)
                    {
                       PrefabsDb.prefabs.Add(newFile);
                    }
                    return;
                }
                PrefabsDb.prefabs.RemoveAt(index);
                PrefabsDb.prefabs.Insert(index, newFile);
            }

            if (!PrefabsDb.TryGetPrefabFile(guid, out var prefabFile))
            {
                BuildPrefabFile(guid);
                return true;
            }

            var isValid = TryCreatePrefabFile(guid, out var newFile);

            if (prefabFile.dependencies.SequenceEqual(newFile.dependencies) &&
                prefabFile.components.SequenceEqual(newFile.components))
            {
                // Most probably a data change
                return false;
            }

            if (!PrefabsDb.TryGetDependantFiles(guid, out var files) || files.Count == 0)
            {
                if (isValid)
                {
                    // Replace it
                    ReplacePrefab(prefabFile, newFile);
                    return true;
                }

                // Since there are no dependencies, it is enough to remove the prefab
                // and no refresh is required
                PrefabsDb.prefabs.Remove(prefabFile);
                return false;
            }

            // The hardest part -> update current one and update existing dependencies

            ReplacePrefab(prefabFile, newFile);

            // Replace the dependencies
            foreach(var depFile in files)
            {
                if (!processed.Add(depFile.guid))
                {
                    continue;
                }

                RefreshPrefab(depFile.guid, processed);
            }

            return true;
        }

        [DidReloadScripts]
        static void RebuildTree()
        {
            var shouldAutoFix = BindingSettings.Current.AutoFixSerializationUpgrade;
            if (!shouldAutoFix)
            {
                return;
            }
            
            if (!PlayerPrefs.HasKey(ShouldRebuildKey) && File.Exists(FilePath))
            {
                if (CanDeleteDeltaDB && File.Exists(DeltaFilePath))
                {
                    File.Delete(DeltaFilePath);
                }
                return;
            }

            PlayerPrefs.DeleteKey(ShouldRebuildKey);

            Profiler.BeginSample("BindDatabase_StoreFields");

            BuildDatabase();

            if (File.Exists(FilePath))
            {
                try
                {
                    ComputeDelta(false, false);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            File.WriteAllText(FilePath, JsonUtility.ToJson(_db));

            Profiler.EndSample();
        }

        [InitializeOnLoadMethod]
        private static void RebuildPrefabs()
        {
            var shouldAutoFix = BindingSettings.Current.AutoFixSerializationUpgrade;
            if (!shouldAutoFix)
            {
                return;
            }
            
            if (File.Exists(PrefabsFilePath))
            {
                return;
            }

            Profiler.BeginSample("BindDatabase_StorePrefabs");
            
            _prefabs = new PrefabsDatabase();

            var allPrefabs = AssetDatabase.FindAssets("t:prefab").Select(g => AssetDatabase.GUIDToAssetPath(g));

            var dependencies = new HashSet<string>();

            foreach(var guid in AssetDatabase.FindAssets("t:prefab"))
            {
                BuildPrefabFile(guid, dependencies);
            }

            var json = JsonUtility.ToJson(_prefabs);
            File.WriteAllText(PrefabsFilePath, json);

            Profiler.EndSample();
        }

        private static PrefabFile BuildPrefabFile(string guid, HashSet<string> dependencies = null)
        {
            if (TryCreatePrefabFile(guid, out var prefabFile, dependencies))
            {
                _prefabs.prefabs.Add(prefabFile);
            }

            return prefabFile;
        }

        private static bool TryCreatePrefabFile(string guid, out PrefabFile prefabFile, HashSet<string> dependencies = null)
        {
            if (dependencies == null)
            {
                dependencies = new HashSet<string>();
            }
            else
            {
                dependencies.Clear();
            }

            var file = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(file);
            prefabFile = new PrefabFile()
            {
                path = file,
                guid = guid
            };

            foreach (var monoBehaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                // Debug a message if monoBehaviour is null
                if (!monoBehaviour)
                {
                    Debug.LogWarning($"A null MonoBehaviour has been detected in prefab {file}");
                    continue;
                }
                var source = PrefabUtility.GetCorrespondingObjectFromSource(monoBehaviour);
                if (!source && PrefabUtility.GetCorrespondingObjectFromOriginalSource(monoBehaviour) == monoBehaviour)
                {
                    // Most probably it is the origin source
                    source = monoBehaviour;
                }
                if (source
                    && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out var cGuid, out long cLocalId)
                    && TryGetTypeGuid(monoBehaviour, out var typeGuid))
                {
                    prefabFile.components.Add(new ComponentType()
                    {
                        typeGuid = typeGuid,
                        guid = cGuid,
                        localId = cLocalId.ToString(),
                    });

                    dependencies.Add(cGuid);
                }
            }

            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (!t)
                {
                    Debug.LogWarning($"A null Transform has been detected in prefab {file}");
                    continue;
                }
                var source = PrefabUtility.GetCorrespondingObjectFromSource(t);
                if (source && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out var cGuid, out long cLocalId))
                {
                    dependencies.Add(cGuid);
                }
            }

            if (dependencies.Count > 0 || prefabFile.components.Count > 0)
            {
                prefabFile.dependencies = dependencies.ToList();
                return true;
            }

            return false;
        }

        private static bool TryGetTypeGuid(MonoBehaviour mono, out string guid)
        {
            var monoScript = MonoScript.FromMonoBehaviour(mono);
            if(monoScript != null)
            {
                guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(monoScript));
                return true;
            }
            guid = null;
            return false;
        }

        private static void ComputeDelta(bool registerAdded, bool registerDeleted)
        {
            Profiler.BeginSample($"BindingSystem.{nameof(BindDatabase)}.CreateDeltaFields");

            var prevDb = JsonUtility.FromJson<FullDatabase>(File.ReadAllText(FilePath));

            _deltaDb = new DeltaDatabase();

            foreach (var type in _db.types)
            {
                if (!prevDb.TryGetType(type.guid, out var prevType))
                {
                    continue;
                }

                var fieldsList = new List<BindField>(prevType.fields);
                var deltaFields = new List<DeltaBindField>();

                foreach (var field in type.fields)
                {
                    if (prevType.TryGetField(field.id, out var prevField))
                    {
                        fieldsList.Remove(prevField);
                        if (field.isBind == prevField.isBind)
                        {
                            continue;
                        }
                        var deltaField = new DeltaBindField()
                        {
                            id = field.id,
                            path = field.path,
                            oldPath = prevField.path,
                            isCompound = field.isCompound,
                            primitive = field.primitive.IfNullOrEmpty(prevField.primitive),
                            isArray = field.isArray,
                            change = field.isBind ? DeltaBindField.Change.ToBind : DeltaBindField.Change.FromBind
                        };
                        deltaFields.Add(deltaField);
                    }
                    else if (registerAdded)
                    {
                        var deltaField = new DeltaBindField()
                        {
                            id = field.id,
                            path = field.path,
                            change = DeltaBindField.Change.Added
                        };
                        deltaFields.Add(deltaField);
                    }
                }
                if (registerDeleted)
                {
                    foreach (var field in fieldsList)
                    {
                        var deltaField = new DeltaBindField()
                        {
                            id = field.id,
                            path = field.path,
                            oldPath = field.path,
                            change = DeltaBindField.Change.Removed
                        };
                        deltaFields.Add(deltaField);
                    }
                }
                if (deltaFields.Count > 0)
                {
                    deltaFields.Sort((a, b) => a.id.Length - b.id.Length);
                    _deltaDb.types.Add(new DeltaBindType()
                    {
                        type = type.type,
                        guid = type.guid,
                        fields = deltaFields,
                        localId = type.localId,
                    });
                }
            }

            File.WriteAllText(DeltaFilePath, JsonUtility.ToJson(_deltaDb));

            Profiler.EndSample();
        }

        private static void BuildDatabase()
        {
            var types = GetAllScriptedTypes();
            var dependencies = new Dictionary<Type, List<string>>();

            _db = BuildDatabase(types, dependencies);
        }

        internal static FullDatabase BuildDatabase(List<(Type type, string guid, string localId)> types, Dictionary<Type, List<string>> dependencies)
        {
            var db = new FullDatabase();

            foreach (var (type, guid, localId) in types)
            {
                if (type.IsAbstract)
                {
                    continue;
                }
                if (type.GetCustomAttribute<ObsoleteAttribute>() != null)
                {
                    continue;
                }

                var bindType = BuildType(type, guid, localId, dependencies);
                db.types.Add(bindType);
            }

            db.dependencies = dependencies.Select(d => new BindDependency()
            {
                name = d.Key.UserFriendlyName(),
                type = d.Key.AssemblyQualifiedName,
                dependencies = d.Value
            }).ToList();

            return db;
        }

        internal static BindType BuildType(Type type, string guid, string localId, Dictionary<Type, List<string>> dependencies)
        {
            var bindType = new BindType()
            {
                type = type.AssemblyQualifiedName,
                guid = guid,
                localId = localId,
            };

            BuildInnerType(guid, bindType, new BindField()
            {
                depth = 0,
                id = "",
                path = "",
            }, 0, "", type, dependencies, new HashSet<Type>());

            return bindType;
        }
         
        private static void BuildInnerType(string guid,
                                           BindType bindType,
                                           BindField parent,
                                           int depth,
                                           string propertyPath,
                                           Type type,
                                           Dictionary<Type, List<string>> dependencies,
                                           HashSet<Type> processedTypes)
        {

            if (processedTypes.Contains(type))
            {
                return;
            }
            processedTypes.Add(type);

            // TODO: for ref fields, add ".Ref[#]" to mark them as ref


            var isBindType = IsBindType(type);
            var fields = isBindType
                       ? new FieldInfo[] { type.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance) }
                       : type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var f in fields)
            {
                if (f == null)
                {
                    continue;
                }
                if (!IsValidField(f))
                {
                    continue;
                }

                var field = f;

                var isBind = IsBindType(field.FieldType);
                if (isBind)
                {
                    field = field.FieldType.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field == null)
                    {
                        continue;
                    }
                }

                if(parent != null)
                {
                    parent.isCompound = !parent.isArray;
                }

                var childType = GetChildType(field);

                var bindField = new BindField()
                {
                    isArray = field.FieldType.IsArray || IsList(field.FieldType),
                    isBind = isBind || isBindType,
                    //isReference = field.GetCustomAttribute<SerializeReference>() != null,
                    depth = depth,
                };
                bindType.fields.Add(bindField);

                var pathSuffix = isBind ? f.Name + '.' + field.Name : field.Name;
                var idSuffix = isBindType ? "" : '.' + f.Name;
                bindField.path = parent.isArray
                               ? $"{propertyPath}.Array[#]." + pathSuffix
                               : (propertyPath + '.' + pathSuffix).TrimStart('.');
                bindField.id = parent.isArray
                             ? $"{parent.id}.Array[#]" + idSuffix
                             : (parent.id + '.' + f.Name).TrimStart('.');

                //if (bindField.isReference)
                //{
                //    bindField.path += ".Ref[#]";
                //}

                TryAddDependency(guid, childType, dependencies);

                var isPureArray = bindField.isArray && !IsBindType(childType);
                var childIsSimple = IsSimpleType(childType);

                if (isPureArray)
                {
                    bindType.fields.Add(new BindField()
                    {
                        depth = depth + 1,
                        id = bindField.id + ".Array[#]",
                        path = bindField.path + ".Array[#]",
                        isCompound = !childIsSimple && !isBind,
                        primitive = GetPrimitive(childType)
                    });
                }
                if (!childIsSimple)
                {
                    BuildInnerType(guid,
                                   bindType,
                                   bindField,
                                   isPureArray ? depth + 2 : depth + 1,
                                   bindField.path,
                                   childType,
                                   dependencies,
                                   processedTypes);
                }
                //else if(bindField.isArray && isBind)
                //{
                //    bindType.fields.Add(new BindField()
                //    {
                //        depth = depth + 1,
                //        id = bindField.id + ".Array[#]",
                //        path = bindField.path + ".Array[#]",
                //    });
                //}
            }
        }

        private static string GetPrimitive(Type childType)
        {
            return childType.IsPrimitive 
                && childType != typeof(float) 
                && childType != typeof(double) 
                    ? childType.Name
                    : null;
        }

        private static bool IsBindType(Type type)
        {
            return typeof(IBind).IsAssignableFrom(type);
        }

        private static void TryAddDependency(string guid, Type fieldType, Dictionary<Type, List<string>> dependencies)
        {
            if (fieldType.IsArray)
            {
                return;
            }
            if (fieldType.IsInterface)
            {
                return;
            }
            if (IsList(fieldType))
            {
                return;
            }
            if (fieldType.Module.Name.Contains("mscorlib", StringComparison.Ordinal))
            {
                return;
            }
            if (fieldType.Namespace?.StartsWith("Unity", StringComparison.Ordinal) == true)
            {
                return;
            }
            if (IsBindType(fieldType))
            {
                return;
            }
            if (fieldType == typeof(BindData))
            {
                return;
            }

            if (!dependencies.TryGetValue(fieldType, out var list))
            {
                list = new List<string>();
                dependencies[fieldType] = list;
            }

            if (!list.Contains(guid))
            {
                list.Add(guid);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static Type GetChildType(FieldInfo field)
        {
            var type = field.FieldType;
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            if (IsList(field.FieldType))
            {
                return type.GetGenericArguments()[0];
            }
            return type;
        }

        private static bool IsSimpleType(Type fieldType)
        {
            return fieldType.IsEnum
                || typeof(Object).IsAssignableFrom(fieldType)
                || _stdTypes.Contains(fieldType);
        }

        private static bool IsValidField(FieldInfo field)
        {
            return !field.IsInitOnly
                && field.GetCustomAttribute<ObsoleteAttribute>() == null
                && field.GetCustomAttribute<NonSerializedAttribute>() == null
                && !field.FieldType.IsInterface
                && (field.IsPublic
                    || field.GetCustomAttribute<SerializeField>() != null
                    || field.GetCustomAttribute<SerializeReference>() != null)
                && IsValidFieldType(field.FieldType);
        }

        private static bool IsValidFieldType(Type type)
        {
            return type.Namespace?.Contains("Unity", StringComparison.Ordinal) == true
                || typeof(Object).IsAssignableFrom(type)
                || (IsList(type) && IsValidFieldType(type.GetGenericArguments()[0]))
                || (type.IsArray && IsValidFieldType(type.GetElementType()))
                || type.GetCustomAttribute<SerializableAttribute>() != null;
        }

        private static List<(Type type, string guid, string localId)> GetAllScriptedTypes()
        {
            var allTypes = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                           .Concat(TypeCache.GetTypesDerivedFrom<ScriptableObject>().Where(IsValidScriptableObjectType))
                           .Select(t => t.Name + ".cs");
            var types = new List<(Type type, string guid, string localId)>();

            foreach (var scriptGUID in AssetDatabase.FindAssets("t:script"))
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(scriptGUID);

                if (Path.GetFullPath(scriptPath).Contains("PackageCache", StringComparison.Ordinal))
                {
                    continue;
                }

                var filename = Path.GetFileName(scriptPath);

                if (!allTypes.Contains(filename))
                {
                    continue;
                }

                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);

                if(monoScript == null)
                {
                    continue;
                }

                var type = monoScript.GetClass();

                if (type == null)
                {
                    continue;
                }

                if (type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsSubclassOf(typeof(ScriptableObject)))
                {
                    types.Add((monoScript.GetClass(), scriptGUID, "11500000"));
                }
            }

            return types;
        }

        private static bool IsValidScriptableObjectType(Type t)
        {
            if (t == null)
            {
                return false;
            }

            var baseType = t.BaseType;
            while (baseType != typeof(ScriptableObject))
            {
                if (baseType == null)
                {
                    return false;
                }
                if (baseType.Namespace?.StartsWith("UnityEditor", StringComparison.Ordinal) == true)
                {
                    return false;
                }
                baseType = baseType.BaseType;
            }
            return true;
        }

        [Serializable]
        internal class FullDatabase
        {
            public List<BindType> types = new List<BindType>();
            public List<BindDependency> dependencies = new List<BindDependency>();

            private Dictionary<string, List<string>> _deps;

            public bool TryGetType(string guid, out BindType type)
            {
                foreach (var t in types)
                {
                    if (t.guid.Equals(guid, StringComparison.Ordinal))
                    {
                        type = t;
                        return true;
                    }
                }

                type = null;
                return false;
            }

            public bool TryGetDependencies(Type type, out List<string> dependencies)
                => TryGetDependencies(type.AssemblyQualifiedName, out dependencies);

            public bool TryGetDependencies(string typename, out List<string> dependencies)
            {
                if (_deps == null)
                {
                    _deps = new Dictionary<string, List<string>>();
                    foreach (var dep in this.dependencies)
                    {
                        _deps[dep.name] = dep.dependencies;
                        _deps[dep.type] = dep.dependencies;
                    }
                }

                return _deps.TryGetValue(typename, out dependencies);
            }
        }

        [Serializable]
        internal class DeltaDatabase
        {
            public List<DeltaBindType> types = new List<DeltaBindType>();
        }

        [Serializable]
        internal class DeltaBindType
        {
            public string type;
            public string guid;
            public string localId;
            public List<DeltaBindField> fields;

            public DeltaBindField this[string id]
            {
                get
                {
                    foreach (var field in fields)
                    {
                        if (field.id.Equals(id, StringComparison.Ordinal))
                        {
                            return field;
                        }
                    }
                    return null;
                }
            }

            public bool TryGetField(string id, out DeltaBindField field)
            {
                foreach (var f in fields)
                {
                    if (f.id.Equals(id, StringComparison.Ordinal))
                    {
                        field = f;
                        return true;
                    }
                }
                field = null;
                return false;
            }
        }

        [Serializable]
        internal class DeltaBindField
        {
            public enum Change
            {
                ToBind,
                FromBind,
                Removed,
                Added
            }

            public string id;
            public string path;
            public string oldPath;
            public bool isCompound;
            public bool isReference;
            public bool isArray;
            public string primitive;
            public Change change;
        }

        [Serializable]
        internal class BindType
        {
            public string type;
            public string guid;
            public string localId;
            public List<BindField> fields = new List<BindField>();

            public BindField this[string id]
            {
                get
                {
                    foreach (var field in fields)
                    {
                        if (field.id.Equals(id, StringComparison.Ordinal))
                        {
                            return field;
                        }
                    }
                    return null;
                }
            }

            public bool TryGetField(string id, out BindField field)
            {
                foreach (var f in fields)
                {
                    if (f.id.Equals(id, StringComparison.Ordinal))
                    {
                        field = f;
                        return true;
                    }
                }
                field = null;
                return false;
            }
        }

        [Serializable]
        internal class BindDependency
        {
            public string name;
            public string type;
            public List<string> dependencies = new List<string>();
        }

        [Serializable]
        internal class BindField
        {
            [Flags]
            public enum Flags
            {
                None = 0,
                IsBind = 1 << 0,
                IsArray = 1 << 1,
                IsCompound = 1 << 2,
                IsReference = 1 << 3,
            }

            public string id;
            public string path;
            public int depth;
            public string primitive;
            public Flags flags;

            public string name
            {
                get
                {
                    var index = id.LastIndexOf('.');
                    return index < 0 ? id : id.Substring(index + 1);
                }
            }
            public bool isBind { get => flags.HasFlag(Flags.IsBind); set => flags = value ? flags | Flags.IsBind : flags & ~Flags.IsBind; }
            public bool isArray { get => flags.HasFlag(Flags.IsArray); set => flags = value ? flags | Flags.IsArray : flags & ~Flags.IsArray; }
            public bool isCompound { get => flags.HasFlag(Flags.IsCompound); set => flags = value ? flags | Flags.IsCompound : flags & ~Flags.IsCompound; }
            public bool isReference { get => flags.HasFlag(Flags.IsReference); set => flags = value ? flags | Flags.IsReference : flags & ~Flags.IsReference; }

        }

        [Serializable]
        internal class PrefabsDatabase 
        {
            [NonSerialized]
            private Dictionary<string, PrefabFile> _prefabs;
            [NonSerialized]
            private Dictionary<string, List<PrefabFile>> _deps;
            [NonSerialized]
            private Dictionary<string, List<ComponentType>> _componentsByType;
            [NonSerialized]
            private Dictionary<string, List<PrefabFile>> _prefabsByType;

            public List<PrefabFile> prefabs = new List<PrefabFile>();

            public bool HasType(string prefabGuid, string typeGuid)
                => TryGetPrefabFile(prefabGuid, out var file) && file.components.Any(c => c.typeGuid.FastEquals(typeGuid));

            public void Refresh()
            {
                _prefabs = null;
                _deps = null;
            }

            public bool TryGetPrefabFile(string fileGuid, out PrefabFile prefabFile)
            {
                if(_prefabs == null)
                {
                    Rebuild();
                }

                return _prefabs.TryGetValue(fileGuid, out prefabFile);
            }

            public bool TryGetPrefabsByType(string typeGuid, out List<PrefabFile> prefabFiles)
            {
                if (_prefabsByType == null)
                {
                    RebuildPrefabsByType();
                }

                return _prefabsByType.TryGetValue(typeGuid, out prefabFiles);
            }

            public bool TryGetComponentsByType(string typeGuid, out List<ComponentType> components)
            {
                if (_componentsByType == null)
                {
                    RebuildComponentsByType();
                }

                return _componentsByType.TryGetValue(typeGuid, out components);
            }

            public bool TryGetComponentsByType(string prefabGuid, string typeGuid, out List<ComponentType> components)
            {
                if(!TryGetPrefabFile(prefabGuid, out var file))
                {
                    components = null;
                    return false;
                }

                components = file.components.Where(c => c.typeGuid.FastEquals(typeGuid)).ToList();
                return components.Count > 0;
            }

            private void RebuildPrefabsByType()
            {
                _prefabsByType = new Dictionary<string, List<PrefabFile>>();
                foreach (var prefab in prefabs)
                {
                    foreach (var dep in prefab.components)
                    {
                        if (!_prefabsByType.TryGetValue(dep.typeGuid, out var list))
                        {
                            list = new List<PrefabFile>();
                            _prefabsByType[dep.typeGuid] = list;
                        }
                        if (!list.Contains(prefab))
                        {
                            list.Add(prefab);
                        }
                    }
                }
            }

            private void RebuildComponentsByType()
            {
                _componentsByType = new Dictionary<string, List<ComponentType>>();
                foreach (var prefab in prefabs)
                {
                    foreach (var dep in prefab.components)
                    {
                        if(!_componentsByType.TryGetValue(dep.typeGuid, out var list))
                        {
                            list = new List<ComponentType>();
                            _componentsByType[dep.typeGuid] = list;
                        }
                        list.Add(dep);
                    }
                }
            }

            public bool TryGetDependantFiles(string guid, out List<PrefabFile> prefabFiles)
            {
                if(_deps == null)
                {
                    RebuildDeps();
                }

                return _deps.TryGetValue(guid, out prefabFiles);
            }

            private void RebuildDeps()
            {
                _deps = new Dictionary<string, List<PrefabFile>>();
                foreach(var prefab in prefabs)
                {
                    if (!_deps.TryGetValue(prefab.guid, out var list))
                    {
                        list = new List<PrefabFile>();
                        _deps[prefab.guid] = list;
                    }
                    foreach (var dep in prefab.dependencies)
                    {
                        if(TryGetPrefabFile(dep, out var depFile))
                        {
                            list.Add(depFile);
                        }
                    }
                }
            }

            private void Rebuild()
            {
                _prefabs = new Dictionary<string, PrefabFile>();
                foreach(var prefabFile in prefabs)
                {
                    _prefabs[prefabFile.guid] = prefabFile;
                }
            }
        }

        [Serializable]
        internal class PrefabFile 
        {
            public string path;
            public string guid;

            public List<string> dependencies = new List<string>();
            public List<ComponentType> components = new List<ComponentType>();
        }

        [Serializable]
        internal class ComponentType
        {
            public string typeGuid;
            public string localId;
            public string guid;

            public override bool Equals(object obj)
            {
                return obj is ComponentType c
                    && c.typeGuid == typeGuid
                    && c.localId == localId
                    && c.guid == guid;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        internal abstract class Data
        {
            public string guid;
        }
    }

}
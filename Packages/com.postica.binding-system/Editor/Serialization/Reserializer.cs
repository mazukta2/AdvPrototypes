using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Postica.Common;

using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Serialization
{
    [InitializeOnLoad]
    public class Reserializer : AssetPostprocessor
    {
        private const string SessionKey_Ready = "[SerializationTracker_SessionKey_Ready]";
        private const string DialogKey = "[SerializationTracker_OptOutKey]";
        private const string DialogUpgradeKey = "[SerializationTracker_Upgrade_Key]";
        private const string FilePath = "Library/TempUpgrades.json";
        private const string PrefabsFilePath = "Library/PrefabsComponents.json";

        private static readonly Regex TypeRegex = new Regex(@"\s*(class|struct)\s*([a-zA-Z_][\w_]*)");
        private static readonly (SerializedProperty property, string valuePath)[] TempUnitArray
                            = new (SerializedProperty property, string valuePath)[1];

        internal static class Yaml
        {
            public const int indentSpaces = 2;

            public static readonly string[] lineSplitter = new string[] { Environment.NewLine };
            public const string separator = "--- !u!";
            public static readonly Regex localIdRegex = new Regex(@"--- !u!114 &(\d+)", RegexOptions.Compiled);
            public const string prefabInstancePrefix = "--- !u!1001 &";
            public const string prefabPrefix = "--- !u!1 &";

            public const string scriptGuidPrefix = "  m_Script: {fileID: 11500000, guid: ";
            public const string prefabSourcePrefix = "m_SourcePrefab: {fileID: 100100000, guid: ";
            public const int prefabSourcePrefixLength = 42; //"m_SourcePrefab: {fileID: 100100000, guid: ";
            public const int scriptGuidPrefixLength = 37; // "  m_Script: {fileID: 11500000, guid: ".Length;
            public const int scriptGuidLength = 32;

            public static readonly Regex targetRegex = new Regex(@"target: *{fileID: *(\d+), guid: ([a-fA-F0-9]+), *type: *\d+}", RegexOptions.Compiled);
            public const string editorClassId = "  m_EditorClassIdentifier:";
            public const int editorClassIdLength = 26; //"  m_EditorClassIdentifier:".Length

            public const string references = "  references:";
            public const string referencesVersion = "    version: ";
            public const string referencesRefIds = "    RefIds:";

            public const string prefabPropertyTemplate = "- target: {{fileID: {0}, guid: {1}, type: 3}}\n      propertyPath: {2}";

            public static readonly string defaultBindValue = string.Intern(@"
{0}_bindData:
{0}  Source: {{fileID: 0}}
{0}  Path: 
{0}  _mode: 0
{0}  _parameters: []
{0}  _mainParamIndex: 0
{0}  _readConverter:
{0}    rid: -2
{0}  _writeConverter:
{0}    rid: -2
{0}  _modifiers: []
{0}  _sourceType: 
{0}  _ppath: 
{0}  _flags: 0
{0}_isBound: 0
{0}_value:".FastReplace("\r", ""));

            public static readonly Regex oneIndent = new Regex(@"  [^- ]", RegexOptions.Compiled);

            public static string GetDefaultBindValue(int indentLevel)
            {
                return string.Format(defaultBindValue, new string(' ', indentLevel * 2));
            }

            public static string AddIndent(int indent, string s)
            {
                var sb = new StringBuilder();
                foreach (var line in s.Split(lineSplitter, StringSplitOptions.None))
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        sb.AppendLine(line);
                        continue;
                    }
                    sb.Append(' ', indent * 2).AppendLine(line);
                }
                if (sb[sb.Length - 1] == '\n')
                {
                    sb.Length -= 2;
                }
                return sb.ToString();
            }
        }

        internal class RegEx
        {
            public static readonly Regex bindDataRegex = new Regex(@"\n? *_bindData: *[^❘]+?_value:", RegexOptions.Compiled | RegexOptions.Multiline);

            private static readonly Dictionary<string, Regex> _pathRegexes = new Dictionary<string, Regex>();

            public static string ReplaceArrayField(string input, string from, string to)
            {
                if (!_pathRegexes.TryGetValue(from, out Regex regex))
                {
                    var regexPath = from.FastReplace(@".Array[#]", @"\.Array\.data\[(\d+)\]");
                    regex = new Regex(regexPath, RegexOptions.Compiled);
                    _pathRegexes[from] = regex;
                }

                var toPath = to.FastReplace(".Array[#]", @".Array.data[$1]");

                return regex.Replace(input, toPath);
            }
        }

        private static bool _upgradePerformed = false;
        private static TrackingDatabase _trackDB = new TrackingDatabase();
        private static RegEx _regEx = new RegEx();

        private static bool _isDomainLocked;
        private static bool _alreadyPreparedDB;
        private static DateTime _domainLockTimeout;

        internal static bool IsDomainLocked
        {
            get => _isDomainLocked;
            set
            {
                _isDomainLocked = value;
                if (value)
                {
                    EditorApplication.LockReloadAssemblies();
                    _domainLockTimeout = DateTime.Now.AddSeconds(1); // Limit to 1 second
                    EditorApplication.update -= CheckDomainLockedTimeout;
                    EditorApplication.update += CheckDomainLockedTimeout;
                }
                else
                {
                    EditorApplication.UnlockReloadAssemblies();
                    EditorApplication.update -= CheckDomainLockedTimeout;
                }
            }
        }

        static Reserializer()
        {
            EditorApplication.delayCall -= PerformUpgrades;
            EditorApplication.delayCall += PerformUpgrades;

            CompilationPipeline.compilationStarted -= CompilationPipeline_compilationStarted;
            CompilationPipeline.compilationStarted += CompilationPipeline_compilationStarted;
        }

        private static void CompilationPipeline_compilationStarted(object obj)
        {
            CompilationPipeline.compilationStarted -= CompilationPipeline_compilationStarted;

            var settings = BindingSettings.Current;
            if (!_alreadyPreparedDB && settings?.AutoFixSerializationUpgrade != false)
            {
                //Debug.Log($"Locking Domain Reload");
                IsDomainLocked = true;
            }
        }

        private static void CheckDomainLockedTimeout()
        {
            //Debug.Log($"Update: {_isDomainLocked}   {DateTime.Now} -> {_domainLockTimeout}");
            if (IsDomainLocked && DateTime.Now < _domainLockTimeout)
            {
                return;
            }

            IsDomainLocked = false;
        }

        [RunBeforeClass(typeof(BindDatabase))]
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            var shouldAutoFix = BindingSettings.Current.AutoFixSerializationUpgrade;
            if (!shouldAutoFix)
            {
                return;
            }
            
            IsDomainLocked = false;
            //if (IsDomainLocked)
            //{
            //    EditorApplication.UnlockReloadAssemblies();
            //    Debug.Log($"Unlocking Domain Reload: {didDomainReload}" +
            //        $"\n- IMPORTED: \n{string.Join("\n   ", importedAssets)}" +
            //        $"\n- DELETED: \n{string.Join("\n   ", deletedAssets)}" +
            //        $"\n- MOVED: \n{string.Join("\n   ", movedAssets)}"
            //        );
            //    IsDomainLocked = false;
            //}

            if (didDomainReload)
            {
                return;
            }

            var settings = BindingSettings.Current;
            if (!settings.AutoFixSerializationUpgrade)
            {
                return;
            }

            var typesToTrack = new List<(Type type, string guid)>();

            var bindDB = BindDatabase.Database;
            var typesDictionary = new Dictionary<string, Type>();

            void MarkForTracking(Type type, string guid)
            {
                if (!typesToTrack.Any(t => t.type == type))
                {
                    typesToTrack.Add((type, guid));
                }
            }

            foreach (var file in deletedAssets)
            {
                var guid = AssetDatabase.AssetPathToGUID(file);
            }

            foreach (string file in importedAssets)
            {
                if (!file.EndsWith(".cs", StringComparison.Ordinal))
                {
                    continue;
                }

                if (TryGetValidType(file, out var scriptType))
                {
                    MarkForTracking(scriptType, AssetDatabase.AssetPathToGUID(file));
                    continue;
                }

                if (!TryGetAllDependandTypes(file, bindDB, out var guids))
                {
                    continue;
                }

                foreach (var guid in guids)
                {
                    if (!typesDictionary.TryGetValue(guid, out scriptType))
                    {
                        var depFile = AssetDatabase.AssetPathToGUID(guid);
                        var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(depFile);
                        if (monoScript == null)
                        {
                            continue;
                        }
                        scriptType = monoScript.GetType();
                        typesDictionary[guid] = scriptType;
                    }

                    MarkForTracking(scriptType, guid);
                }
            }

            BindDatabase.CanDeleteDeltaDB = typesToTrack.Count == 0;

            if (typesToTrack.Count > 0)
            {
                _alreadyPreparedDB = true;

                _trackDB = new TrackingDatabase();

                foreach (var (type, guid) in typesToTrack)
                {
                    Track(type, guid);
                }

                if (EditorSettings.serializationMode != SerializationMode.ForceText
                    && _trackDB.types.Any(t => t.instances.Count > 0))
                {
                    EditorUtility.DisplayDialog("Upgrade Notice",
                        "Binding System automatic upgrade tool requires Serialization Mode set to Force Text to work correctly, otherwise not-loaded scenes will not be upgraded.",
                        "I understand", DialogOptOutDecisionType.ForThisSession, DialogKey);
                }

                var json = EditorJsonUtility.ToJson(_trackDB, prettyPrint: true);
                File.WriteAllText(FilePath, json);
            }
        }

        private static bool TryGetAllDependandTypes(string file, BindDatabase.FullDatabase depsDB, out HashSet<string> guids)
        {
            guids = new HashSet<string>();
            using (StreamReader reader = new StreamReader(file))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.Contains(" class ", StringComparison.Ordinal)
                        && !line.Contains(" struct ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var match = TypeRegex.Match(line);
                    if (!match.Success)
                    {
                        continue;
                    }

                    if (!depsDB.TryGetDependencies(match.Groups[2].Value, out var deps))
                    {
                        continue;
                    }

                    foreach (var dep in deps)
                    {
                        guids.Add(dep);
                    }
                }

                return guids.Count > 0;
            }
        }

        private static bool IsPotentialBindCandidate(string file)
        {
            using (StreamReader reader = new StreamReader(file))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("Bind", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static bool TryGetValidType(string fileName, out Type type)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            type = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.Ordinal));
            if (type != null)
            {
                return true;
            }

            type = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.Ordinal));

            if (type != null)
            {
                return true;
            }

            return false;
        }

        private static void Track(Type type, string guid)
        {
            if (_trackDB.types.Any(t => t.name.Equals(type.AssemblyQualifiedName, StringComparison.Ordinal)))
            {
                return;
            }

            var trackType = new TrackingType()
            {
                name = type.AssemblyQualifiedName,
            };

            _trackDB.types.Add(trackType);

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                var assets = GetAssets(type);

                foreach (var asset in assets)
                {
                    if (!TryGetTrackingInfo(asset, out TrackingInstance trackingInstance))
                    {
                        continue;
                    }

                    trackType.instances.Add(trackingInstance);
                }
            }

            if (!typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                return;
            }

            // Always load the scene
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    foreach (var target in rootGo.GetComponentsInChildren(type, true))
                    {
                        if (!TryGetTrackingInfo(scene, target, out TrackingInstance trackingInstance))
                        {
                            continue;
                        }
                        trackType.instances.Add(trackingInstance);
                    }
                }
            }
        }

        #region [  SERIALIZED PROPERTIES TRACKING  ]

        private static List<Object> GetAssets(Type type)
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                return AssetDatabase.FindAssets("t:Prefab")
                                .Select(f => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(f)))
                                .SelectMany(p => p.GetComponentsInChildren(type, true)).Cast<Object>().ToList();
            }

            return AssetDatabase.FindAssets("t:" + type.FullName)
                                .SelectMany(f => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(f)))
                                .Where(o => type.IsAssignableFrom(o.GetType())).ToList();
        }

        private static bool TryGetTrackingInfo(Scene scene, Component target, out TrackingInstance trackingInstance)
        {
            if (!scene.IsValid())
            {
                trackingInstance = null;
                return false;
            }

            trackingInstance = new TrackingInstance()
            {
                guid = AssetDatabase.AssetPathToGUID(scene.path),
                hierarchyPath = GetHierarchyPath(target),
            };

            return TryGetTrackingInstance(target, trackingInstance);
        }

        private static bool TryGetTrackingInfo(Object asset, out TrackingInstance trackingInstance)
        {
            if (!asset)
            {
                trackingInstance = null;
                return false;
            }

            trackingInstance = new TrackingInstance()
            {
                guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)),
            };

            if (asset is MonoBehaviour mb)
            {
                trackingInstance.hierarchyPath = GetHierarchyPath(mb);
            }

            return TryGetTrackingInstance(asset, trackingInstance);
        }

        private static bool TryGetTrackingInstance(Object asset, TrackingInstance trackingInstance)
        {
            var success = false;
            using var so = new SerializedObject(asset);
            var property = so.FindProperty("m_EditorClassIdentifier");

            bool EnterChildren(SerializedProperty property, bool forceNotEnter = false)
            {
                if (forceNotEnter)
                {
                    return false;
                }
                if (property.propertyType == SerializedPropertyType.String)
                {
                    return false;
                }
                if (property.isArray && property.arraySize > 0)
                {
                    return true;
                }
                return property.propertyType == SerializedPropertyType.Generic;
            }

            var doNotEnterChildren = false;
            while (property.Next(EnterChildren(property, doNotEnterChildren)))
            {
                doNotEnterChildren = false;

                SerializedProperty valueProperty = null;
                var shouldBreak = false;
                var isBind = false;

                if (!property.isArray && property.propertyType == SerializedPropertyType.Generic)
                {
                    if (typeof(IBind).IsAssignableFrom(property.GetPropertyType()))
                    {
                        valueProperty = property.FindPropertyRelative("_value");
                        isBind = true;
                        if (valueProperty != null)
                        {
                            var nextProperty = property.Copy();
                            shouldBreak = !nextProperty.Next(false);
                            doNotEnterChildren = valueProperty.propertyType != SerializedPropertyType.Generic;
                            property = valueProperty;
                        }
                        else
                        {
                            doNotEnterChildren = true;
                        }
                    }
                    if (valueProperty == null)
                    {
                        continue;
                    }
                }
                else
                {
                    valueProperty = property;
                }

                var trackingProperty = new TrackingProperty()
                {
                    path = property.propertyPath,
                    parentPath = property.GetParent()?.propertyPath,
                    isBind = isBind
                };

                SetData(trackingProperty, GetPropertyValue(valueProperty));
                trackingInstance.properties.Add(trackingProperty);
                success = true;

                if (shouldBreak)
                {
                    break;
                }
            }

            return success;
        }

        #endregion

        #region [  UPGRADE PART  ]

        private static void PerformUpgrades()
        {
            EditorApplication.delayCall -= PerformUpgrades;

            if (_upgradePerformed)
            {
                return;
            }

            _upgradePerformed = true;

            if (!BindingSettings.Current.AutoFixSerializationUpgrade)
            {
                EditorUtility.SetDialogOptOutDecision(DialogOptOutDecisionType.ForThisSession, DialogUpgradeKey, false);
                return;
            }

            //Debug.Log("Starting upgrading...");

            var deltaDb = BindDatabase.DeltaDb;

            if (deltaDb.types.Count == 0)
            {
                //Debug.Log("No changes...");

                return;
            }

            var workWithText = EditorSettings.serializationMode == SerializationMode.ForceText;

            if (!workWithText && !File.Exists(FilePath))
            {
                return;
            }

            if (!EditorUtility.DisplayDialog("Auto Upgrade",
                            "Binding System has detected changes in data types. " +
                            "\nThe system will now attempt to reserialize correctly the values. " +
                            "\nPlease make sure the project is backed up before continuing." +
                            "\n\nAborting now will reset all serialized data touched by code changes!" +
                            "\n\nThis feature can be completely disabled in Project Settings -> Binding Settings",
                            "Continue", "Abort this time", DialogOptOutDecisionType.ForThisSession, DialogUpgradeKey))
            {
                return;
            }

            Profiler.BeginSample($"BindingSystem.{nameof(Reserializer)}.Reserialize");

            //Debug.Log("... Upgrading");

            _trackDB = File.Exists(FilePath)
                     ? JsonUtility.FromJson<TrackingDatabase>(File.ReadAllText(FilePath))
                     : new TrackingDatabase();

            if (workWithText)
            {
                UpgradeTextFiles(deltaDb);
            }
            else
            {
                UpgradeBinaryFiles(deltaDb);
            }

            UpgradeOpenScenes(deltaDb);

            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
            BindDatabase.CanDeleteDeltaDB = true;

            Profiler.EndSample();
        }

        private static List<string> GetAssetFiles(Type type)
        {
            return AssetDatabase.FindAssets("t:" + type.FullName).ToList();
        }

        private static List<string> GetPrefabsFiles(string typeGuid, BindDatabase.PrefabsDatabase prefabs)
        {
            if (!prefabs.TryGetPrefabsByType(typeGuid, out var list))
            {
                return new List<string>();
            }

            return list.Select(p => p.guid).ToList();
        }

        private static void UpgradeTextFiles(BindDatabase.DeltaDatabase deltaDb)
        {
            var filesToImport = new List<string>();
            var prefabs = BindDatabase.PrefabsDb;
#if UNITY_2022_2_OR_NEWER
            var loadedSceneCount = SceneManager.loadedSceneCount;
#else
            var loadedSceneCount = SceneManager.sceneCount;
#endif
            var loadedScenes = new List<string>(loadedSceneCount);
            for (int i = 0; i < loadedSceneCount; i++)
            {
                loadedScenes.Add(AssetDatabase.AssetPathToGUID(SceneManager.GetSceneAt(i).path));
            }

            var allTextScenes = AssetDatabase.FindAssets("t:scene")
                                .Where(g => !loadedScenes.Contains(g))
                                .ToList();

            foreach (var deltaType in deltaDb.types)
            {
                if (deltaType.fields.Count == 0)
                {
                    continue;
                }

                var typePath = AssetDatabase.GUIDToAssetPath(deltaType.guid);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(typePath);
                var type = monoScript.GetClass();

                var files = type.IsSubclassOf(typeof(MonoBehaviour))
                          ? GetPrefabsFiles(deltaType.guid, prefabs)
                          : GetAssetFiles(type);

                foreach (var file in files)
                {
                    UpgradeTextFile(file, deltaType, prefabs, loadedScenes, filesToImport);
                }

                foreach (var sceneFile in allTextScenes)
                {
                    // Do not compute local ids. It's useless for scenes
                    UpgradeTextFile(sceneFile, deltaType, prefabs, loadedScenes, filesToImport);
                }
            }

            foreach (var file in filesToImport)
            {
                AssetDatabase.ImportAsset(file);
            }
        }

        private static void UpgradeTextFile(string guid,
                                            BindDatabase.DeltaBindType deltaType,
                                            BindDatabase.PrefabsDatabase prefabs,
                                            List<string> loadedScenes,
                                            List<string> filesToImport)
        {
            var file = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(file))
            {
                Debug.LogWarning($"{nameof(Reserializer)}: Unable to reserialize text file with guid {guid}");
                return;
            }

            var fileContent = File.ReadAllText(file);

            var saveFile = false;

            if (file.EndsWith(".prefab"))
            {
                if (!prefabs.TryGetComponentsByType(guid, deltaType.guid, out var components))
                {
                    // Nothing to do here
                    return;
                }
                saveFile |= UpgradePrefabInstances(deltaType, components, ref fileContent);
            }

            if (file.EndsWith(".unity")
                && !loadedScenes.Contains(guid)
                && prefabs.TryGetComponentsByType(deltaType.guid, out var list)
                && list.Count > 0)
            {
                // We need an unloaded scene to operate
                // Need to iterate per prefab to get this data correctly
                saveFile |= UpgradePrefabInstances(deltaType, list, ref fileContent);
            }

            var searchString = Yaml.scriptGuidPrefix + deltaType.guid;

            var index = fileContent.IndexOf(searchString, StringComparison.Ordinal);
            if (index < 0)
            {
                if (saveFile)
                {
                    File.WriteAllText(file, fileContent);
                    filesToImport.Add(file);
                }
                return;
            }

            var sb = new StringBuilder(fileContent);
            var errrosSb = new StringBuilder();

            while (index >= 0)
            {
                var startIndex = sb.IndexOf(Yaml.editorClassId, index, 300) + Yaml.editorClassIdLength;

                UpgradeTextBlock(deltaType, startIndex, sb, errrosSb);

                index = sb.IndexOf(searchString, startIndex);
            }

            if (errrosSb.Length > 0)
            {
                Debug.LogWarning($"Reserializing issues in file {file}:");
            }

            File.WriteAllText(file, sb.ToString());
            filesToImport.Add(file);
        }

        private static bool UpgradePrefabInstances(BindDatabase.DeltaBindType deltaType, List<BindDatabase.ComponentType> list, ref string fileContent)
        {
            var success = false;
            foreach (var component in list)
            {
                foreach (var field in deltaType.fields)
                {
                    try
                    {
                        var fromString = string.Format(Yaml.prefabPropertyTemplate, component.localId, component.guid, field.oldPath);
                        var toString = string.Format(Yaml.prefabPropertyTemplate, component.localId, component.guid, field.path);

                        if (field.path.Contains(".Array[#]", StringComparison.Ordinal))
                        {
                            if (field.isCompound)
                            {
                                fromString += @"\."; // Add the mandatory dot for child properties
                            }
                            fileContent = RegEx.ReplaceArrayField(fileContent, fromString, toString);
                        }
                        else
                        {
                            if (field.isCompound)
                            {
                                fromString += '.'; // Add the mandatory dot for child properties
                            }
                            fileContent = fileContent.Replace(fromString, toString, StringComparison.Ordinal);
                        }

                        success = true;
                    }
                    catch(Exception ex)
                    {
                        Debug.LogError($"{nameof(Reserializer)}.{nameof(UpgradePrefabInstances)}(): failed to reserialize field {field.path}. Exception: {ex}");
                    }
                }
            }

            return success;
        }

        internal static void UpgradeTextBlock(BindDatabase.DeltaBindType deltaType,
                                             int startIndex,
                                             StringBuilder content,
                                             StringBuilder errorsSB)
        {
            foreach (var field in deltaType.fields)
            {
                try
                {
                    if (!ProcessField(field, content, startIndex))
                    {
                        errorsSB.AppendLine($" - Unable to process field {field.id} with previous path: {field.oldPath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{nameof(Reserializer)}.{nameof(UpgradeTextBlock)}(): Failed to process field {field.path.Replace(".Array[#}", ".Array.data[#]")}");
                    Debug.LogException(ex);
                }
            }
        }

        internal static bool ProcessField(BindDatabase.DeltaBindField field, StringBuilder content, int startIndex)
        {
            var addBind = field.change == BindDatabase.DeltaBindField.Change.ToBind;
            var path = field.oldPath;
            if (field.change == BindDatabase.DeltaBindField.Change.FromBind
                && path.EndsWith("._value", StringComparison.Ordinal))
            {
                path = path.Substring(0, path.Length - "._value".Length);
            }
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var cursor = startIndex;
            return ProcessField(field, parts, 0, addBind, 1, ref cursor, content);
        }

        private static bool ProcessField(BindDatabase.DeltaBindField field,
                                         string[] path,
                                         int index,
                                         bool addBind,
                                         int indent,
                                         ref int cursor,
                                         StringBuilder content)
        {
            if (path.Length == index)
            {
                if (addBind)
                {
                    cursor = InsertBind(field, indent, cursor, content);
                }
                else
                {
                    cursor = RemoveBind(field, indent, cursor, content);
                }
                return true;
            }

            var piece = path[index];
            if (!piece.Equals("Array[#]", StringComparison.Ordinal))
            {
                var searchValue = new string(' ', indent * Yaml.indentSpaces) + piece + ':';

                while (cursor >= 0)
                {
                    var searchIndex = content.IndexOf(searchValue, cursor);

                    if (searchIndex < 0)
                    {
                        return false;
                    }

                    var nextIndent = IndexOfNextIndent(content, indent, cursor, searchIndex);

                    if (nextIndent.indent < indent && nextIndent.index < searchIndex)
                    {
                        return false;
                    }

                    searchIndex += searchValue.Length;
                    var nextIndentLevel = GetIndent(content, searchIndex) + 1;
                    if (ProcessField(field, path, index + 1, addBind, nextIndentLevel, ref searchIndex, content))
                    {
                        return true;
                    }

                    cursor = searchIndex;
                }

                if (cursor < 0)
                {
                    return false;
                }
            }

            if (cursor < content.Length - 3 && content[cursor + 1] == '[' && content[cursor + 2] == ']')
            {
                // Empty list
                return true;
            }

            var isPrimitive = index == path.Length - 1 && !string.IsNullOrEmpty(field.primitive);
            var startCursor = cursor;

            if (isPrimitive && addBind
                    // The arrays may be serialized as conventional ones, this checks it up
                    && cursor < content.Length - 3
                    && !(char.IsWhiteSpace(content[cursor + 1]) && char.IsWhiteSpace(content[cursor + 2])))
            {
                ConvertFromPrimitiveList(field, indent, cursor, content);
            }

            indent--;
            var success = false;

            while (cursor >= 0 && TryGetNextDash(content, indent, out var dashIndex, cursor))
            {
                cursor = dashIndex;
                success |= ProcessField(field, path, index + 1, addBind, indent + 1, ref cursor, content);
            }

            if (isPrimitive && success && !addBind)
            {
                ConvertToPrimitiveList(field, indent, startCursor, content);
                cursor = startCursor;
            }

            return success;
        }

        private static void ConvertFromPrimitiveList(BindDatabase.DeltaBindField field, int indent, int cursor, StringBuilder content)
        {
            // Get the string
            var sb = new StringBuilder();
            var startIndex = cursor;
            while (cursor < content.Length && content[cursor] != '\n' && content[cursor] != '\r')
            {
                sb.Append(content[cursor]);
                cursor++;
            }

            var bytes = GetBytes(field.primitive);

            if (sb.Length < bytes * 2)
            {
                Debug.LogError($"{nameof(Reserializer)}: Unable to parse list {field.id.Replace(".Array[#]", "")}");
                return;
            }

            // Clear the content
            content.Remove(startIndex, sb.Length);
            cursor = startIndex;

            var byteString = sb.ToString().Trim();

            sb.Clear();
            sb.Append('\n').Append(' ', (indent - 1) * Yaml.indentSpaces).Append('-').Append(' ');

            for (int i = 0; i < byteString.Length; i += bytes * 2)
            {
                var strValue = byteString.Substring(i, bytes * 2);
                var finalValue = ConvertFromByteArray(field.primitive, strValue);
                sb.Append(finalValue);
                content.Insert(cursor, sb.ToString());
                cursor += indent * Yaml.indentSpaces + finalValue.Length + 1;
                sb.Length -= finalValue.Length;
            }
        }

        private static int GetBytes(string type)
        {
            return type switch
            {
                nameof(Boolean) => sizeof(Boolean),
                nameof(Byte) => sizeof(Byte),
                nameof(SByte) => sizeof(SByte),
                nameof(Int16) => sizeof(Int16),
                nameof(UInt16) => sizeof(UInt16),
                nameof(Int32) => sizeof(Int32),
                nameof(UInt32) => sizeof(UInt32),
                nameof(Int64) => sizeof(Int64),
                nameof(UInt64) => sizeof(UInt64),
                nameof(IntPtr) => sizeof(Int64),
                nameof(UIntPtr) => sizeof(UInt64),
                nameof(Char) => sizeof(Char),
                nameof(Double) => sizeof(Double),
                nameof(Single) => sizeof(Single),
                _ => 0,
            };
        }

        private static string ConvertFromByteArray(string type, string value)
        {
            return type switch
            {
                nameof(Boolean) => BitConverter.ToBoolean(value.ToByteArray()) ? "1" : "0",
                nameof(Byte) => value.ToByteArray()[0].ToString(),
                nameof(SByte) => ((sbyte)value.ToByteArray()[0]).ToString(),
                nameof(Int16) => BitConverter.ToInt16(value.ToByteArray()).ToString(),
                nameof(UInt16) => BitConverter.ToUInt16(value.ToByteArray()).ToString(),
                nameof(Int32) => BitConverter.ToInt32(value.ToByteArray()).ToString(),
                nameof(UInt32) => BitConverter.ToUInt32(value.ToByteArray()).ToString(),
                nameof(Int64) => BitConverter.ToInt64(value.ToByteArray()).ToString(),
                nameof(UInt64) => BitConverter.ToUInt64(value.ToByteArray()).ToString(),
                nameof(IntPtr) => BitConverter.ToInt64(value.ToByteArray()).ToString(),
                nameof(UIntPtr) => BitConverter.ToUInt64(value.ToByteArray()).ToString(),
                nameof(Char) => value.Length > 4
                              ? BitConverter.ToInt32(value.ToByteArray()).ToString()
                              : BitConverter.ToInt16(value.ToByteArray()).ToString(),
                nameof(Double) => BitConverter.ToDouble(value.ToByteArray()).ToString(),
                nameof(Single) => BitConverter.ToSingle(value.ToByteArray()).ToString(),
                _ => value
            };
        }

        private static string ConvertToByteArray(string type, string value)
        {
            return type switch
            {
                nameof(Boolean) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToByte(value) > 0)).Replace("-", string.Empty),
                nameof(Byte) => Convert.ToByte(value).ToString("x"),
                nameof(SByte) => Convert.ToSByte(value).ToString("x"),
                nameof(Int16) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToInt16(value))).Replace("-", string.Empty),
                nameof(UInt16) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToUInt16(value))).Replace("-", string.Empty),
                nameof(Int32) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToInt32(value))).Replace("-", string.Empty),
                nameof(UInt32) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToUInt32(value))).Replace("-", string.Empty),
                nameof(Int64) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToInt64(value))).Replace("-", string.Empty),
                nameof(UInt64) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToUInt64(value))).Replace("-", string.Empty),
                nameof(IntPtr) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToInt64(value))).Replace("-", string.Empty),
                nameof(UIntPtr) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToUInt64(value))).Replace("-", string.Empty),
                nameof(Char) => BitConverter.ToString(BitConverter.GetBytes(value.Length == 1 ? Convert.ToChar(value) : (char)Convert.ToInt32(value))).Replace("-", string.Empty),
                nameof(Double) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToDouble(value))).Replace("-", string.Empty),
                nameof(Single) => BitConverter.ToString(BitConverter.GetBytes(Convert.ToSingle(value))).Replace("-", string.Empty),
                _ => value
            };
        }

        private static void ConvertToPrimitiveList(BindDatabase.DeltaBindField field, int indent, int cursor, StringBuilder content)
        {
            // Use BitConverter.ToString(BitConverter.GetBytes(int.Parse("12"))).Replace("-", string.Empty)
            var sb = new StringBuilder();
            var valueSb = new StringBuilder();

            var startIndex = cursor;
            var endIndex = cursor;

            var spaces = 0;
            var minSpaces = indent * Yaml.indentSpaces;
            while (cursor < content.Length)
            {
                while (content[cursor] == '\n' || content[cursor] == '\r')
                {
                    cursor++;
                }

                endIndex = cursor - 1;

                spaces = 0;
                while (content[cursor] == ' ')
                {
                    cursor++;
                    spaces++;
                }

                if (spaces < minSpaces)
                {
                    break;
                }

                if (content[cursor] != '-' || content[cursor + 1] != ' ')
                {
                    break;
                }

                cursor += 2;

                sb.Clear();
                while (cursor < content.Length && content[cursor] != '\n' && content[cursor] != '\r')
                {
                    sb.Append(content[cursor]);
                    cursor++;
                }

                var byteString = ConvertToByteArray(field.primitive, sb.ToString());
                valueSb.Append(byteString);
            }

            if (valueSb.Length == 0)
            {
                // Something went wrong here
                return;
            }

            valueSb.Insert(0, ' ');

            // Clear the content
            content.Remove(startIndex, endIndex - startIndex);
            cursor = startIndex;

            content.Insert(cursor, valueSb.ToString());
        }

        private static int RemoveBind(BindDatabase.DeltaBindField field, int indent, int cursor, StringBuilder content)
        {
            var isArrayElement = content[cursor - 1] == '-';
            RegexReplace(content, RegEx.bindDataRegex, "", 1, cursor);
            if (field.isCompound || field.isArray)
            {
                if (isArrayElement)
                {
                    // First append a space
                    content.Insert(cursor++, ' ');
                    // Need to eliminate first row
                    ClearEmptySpace(cursor, content);
                }
                AddIndent(content, indent, -1, cursor, isArrayElement, field.isArray);
            }
            return cursor;
        }

        private static int InsertBind(BindDatabase.DeltaBindField field, int indent, int cursor, StringBuilder content)
        {
            var isArray = content[cursor - 1] == '-';
            var bindInsert = Yaml.GetDefaultBindValue(indent);
            if (isArray)
            {
                // Here is a list
                bindInsert = ' ' + bindInsert.TrimStart('\n', '\r', ' ');
            }
            content.Insert(cursor, bindInsert);
            cursor += bindInsert.Length;
            if (field.isCompound || field.isArray)
            {
                if (isArray)
                {
                    content.Insert(cursor++, '\n').Remove(cursor, 1); // Add newline and remove a space
                    content.Insert(cursor, new string(' ', indent * Yaml.indentSpaces));
                    cursor += indent * Yaml.indentSpaces;
                }
                var indentDelta = AddIndent(content, indent - 1, 1, cursor, !isArray, field.isArray);
                cursor = Mathf.Max(0, cursor + indentDelta);
            }

            return cursor;
        }

        private static void ClearEmptySpace(int cursor, StringBuilder content)
        {
            var i = cursor;
            while (i < content.Length && (content[i] == ' ' || content[i] == '\n' || content[i] == '\r'))
            {
                i++;
            }
            content.Remove(cursor, i - cursor);
        }

        internal static int AddIndent(StringBuilder content,
                                      int minIndent,
                                      int deltaIndent,
                                      int cursor,
                                      bool skipFirstLine,
                                      bool isArray)
        {
            if (minIndent < deltaIndent)
            {
                throw new ArgumentException($"{nameof(AddIndent)}(): {nameof(deltaIndent)} should be less or equal than {nameof(minIndent)}", nameof(deltaIndent));
            }

            var spacesCount = 0;
            var minSpaces = minIndent * Yaml.indentSpaces;
            var deltaChars = 0;
            var spacesToAdd = deltaIndent > 0 ? new string(' ', deltaIndent * Yaml.indentSpaces) : null;

            if (!skipFirstLine && !isArray)
            {
                while (cursor > 0 && content[cursor] != '\n' && content[cursor] != '\r')
                {
                    cursor--;
                }
                cursor = cursor < 0 ? 0 : cursor - 1;
            }

            while (cursor < content.Length)
            {
                while (cursor < content.Length && content[cursor] != '\n' && content[cursor] != '\r')
                {
                    cursor++;
                }

                cursor++;

                var lineStartIndex = cursor;

                spacesCount = 0;

                while (cursor < content.Length && content[cursor] == ' ')
                {
                    cursor++;
                    spacesCount++;
                }

                if (cursor >= content.Length)
                {
                    break;
                }

                if (spacesCount < minSpaces)
                {
                    break;
                }

                if (spacesCount == minSpaces && !isArray)
                {
                    break;
                }

                if (isArray && content[cursor] != '-')
                {
                    break;
                }

                if (content[cursor] == '\n' || content[cursor] == '\r')
                {
                    continue;
                }

                deltaChars += deltaIndent * Yaml.indentSpaces;
                if (deltaIndent > 0)
                {
                    content.Insert(cursor - 1, spacesToAdd);
                }
                else
                {
                    content.Remove(lineStartIndex, -deltaIndent * Yaml.indentSpaces);
                }
            }

            return deltaChars;
        }

        internal static int RegexReplace(StringBuilder content, Regex regex, string replace, int count, int cursor)
        {
            var indent = GetIndent(content, cursor);
            var block = GetSubstringTillNextIndent(indent, content, cursor);
            var replaced = regex.Replace(block, replace, count);
            content.Replace(block, replaced, cursor, block.Length + 1);
            return replaced.Length - block.Length;
        }

        internal static int GetIndent(StringBuilder content, int cursor, bool includeLists = false)
        {
            var spacesCount = 0;
            if (cursor > 0 && content[cursor] == '\n')
            {
                cursor--;
            }
            while (cursor > 0)
            {
                if (content[cursor] == '\n' || content[cursor] == '\r')
                {
                    return spacesCount / Yaml.indentSpaces;
                }

                if (content[cursor] == ' ')
                {
                    spacesCount++;
                }
                else if (includeLists && spacesCount == 1 && content[cursor] == '-')
                {
                    spacesCount++;
                }
                else
                {
                    spacesCount = 0;
                }
                cursor--;
            }

            return 0;
        }

        internal static string GetSubstringTillNextIndent(int indent, StringBuilder content, int cursor)
        {
            var sb = new StringBuilder();
            var indentSpaces = (indent * Yaml.indentSpaces) + 1;
            for (int i = cursor; i < content.Length; i++)
            {
                sb.Append(content[i]);

                if (content[i] != '\n'
                    && content[i] != '\r')
                {
                    continue;
                }

                i++;

                for (int j = 0; j < indentSpaces; j++)
                {
                    sb.Append(content[i]);
                    if (content[i + j] != ' ')
                    {
                        sb.Length -= j + 1;
                        return sb.ToString();
                    }
                }

                i += indent * Yaml.indentSpaces;
            }
            return sb.ToString();
        }

        private static bool TryGetNextDash(StringBuilder content, int indent, out int index, int startIndex, int endIndex = -1)
        {
            index = -1;
            var indentSpaces = indent * Yaml.indentSpaces;
            endIndex = endIndex < 0 ? content.Length : Mathf.Min(content.Length, endIndex);
            for (int i = startIndex; i < endIndex; i++)
            {
                if (content[i] != '\n'
                    && content[i] != '\r')
                {
                    continue;
                }

                i++;

                for (int j = 1; j < indentSpaces; j++)
                {
                    if (content[i + j] != ' ')
                    {
                        return false;
                    }
                }
                i += indentSpaces;
                if (content[i] == '-' && content[i + 1] == ' ')
                {
                    index = i + 1;
                    return true;
                }
                if (content[i] != ' ')
                {
                    return false;
                }
            }
            return false;
        }

        private static (int index, int indent) IndexOfNextIndent(StringBuilder content, int indent, int startIndex, int endIndex = -1)
        {
            var indentSpaces = indent * Yaml.indentSpaces;
            endIndex = endIndex < 0 ? content.Length : Mathf.Min(content.Length, endIndex);
            for (int i = startIndex; i < endIndex; i++)
            {
                if (content[i] != '\n'
                    && content[i] != '\r')
                {
                    continue;
                }

                i++;

                var dashConsumed = false;
                for (int j = 1; j <= indentSpaces; j++)
                {
                    if (content[i + j] == '-' && !dashConsumed)
                    {
                        dashConsumed = true;
                    }
                    else
                    if (content[i + j] != ' ')
                    {
                        return (i, j / Yaml.indentSpaces);
                    }
                }
                i += indentSpaces;
            }
            return (-1, 0);
        }

        private static void UpgradeOpenScenes(BindDatabase.DeltaDatabase deltaDb)
        {
            var scenes = new Scene[SceneManager.sceneCount];
            for (int i = 0; i < scenes.Length; i++)
            {
                scenes[i] = SceneManager.GetSceneAt(i);
            }
            foreach (var deltaType in deltaDb.types)
            {
                if (deltaType.fields.Count == 0)
                {
                    continue;
                }

                var typePath = AssetDatabase.GUIDToAssetPath(deltaType.guid);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(typePath);
                var type = monoScript.GetClass();

                if (!typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    continue;
                }

                foreach (var scene in scenes)
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        foreach (var component in root.GetComponentsInChildren(type, true))
                        {
                            UpgradeObject(component, deltaType.fields);
                        }
                    }
                }
            }
        }

        private static void UpgradeObject(Object obj, List<BindDatabase.DeltaBindField> fields)
        {
            if (!_trackDB.TryGetInstance(obj, out var instance))
            {
                return;
            }

            instance.Initialize();

            using var so = new SerializedObject(obj);
            foreach (var field in fields)
            {
                foreach (var (property, oldPath) in RetrieveProperties(so, field))
                {
                    if (property == null)
                    {
                        Debug.LogError($"{obj}: Unable to find property for {field.change} with id: {field.id} and path: {field.path}");
                        continue;
                    }

                    if (!instance.TryGetProperty(oldPath, out var trackProp))
                    {
                        Debug.LogError($"{obj}: Unable to find tracked property {property.propertyPath} from previous path {oldPath}");
                        continue;
                    }

                    SetPropertyValue(property, trackProp.value, trackProp.guid);

                    foreach(var child in trackProp.children)
                    {
                        var childPath = ReplaceBeginning(child.path, trackProp.path, property.propertyPath);
                        var childProperty = so.FindProperty(childPath);
                        if (childProperty == null) 
                        {
                            Debug.LogError($"Unable to find child property at path: {childPath}");
                            continue;
                        }
                        SetPropertyValue(childProperty, child.value, child.guid);
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        public static string ReplaceBeginning(string a, string oldValue, string newValue)
        {
            var index = a.IndexOf(oldValue, StringComparison.Ordinal);
            if (index < 0)
            {
                return a;
            }
            return newValue + a.Remove(0, oldValue.Length);
        }

        private static (SerializedProperty property, string valuePath)[] RetrieveProperties(SerializedObject so, BindDatabase.DeltaBindField field)
        {
            var (path, oldPath) = (field.path, field.oldPath);

            if (IsPotentialArrayElement(path))
            {
                var list = new List<(SerializedProperty property, string valuePath)>();
                GetPropertiesList(0, path, oldPath, so, list);

                return list.ToArray();
            }

            var property = so.FindProperty(path);

            var array = TempUnitArray;
            array[0] = (property, oldPath);
            return array;
        }

        private static void GetPropertiesList(int depth, string path, string vpath, SerializedObject so, List<(SerializedProperty property, string valuePath)> list)
        {
            var arrayIndex = path.IndexOf(".Array[#]");
            if (arrayIndex < 0)
            {
                var prop = so.FindProperty(path);
                list.Add((prop, vpath));
                return;
            }

            var lastIndex = path.LastIndexOf(".Array[#]");
            var loadInList = arrayIndex == lastIndex;
            var pathSuffix = path.Substring(arrayIndex + ".Array[#]".Length);

            var arrayPath = path.Substring(0, arrayIndex);
            var arrayProperty = so.FindProperty(arrayPath);
            var arraySize = arrayProperty.arraySize;

            string finalVpath = string.Empty;

            for (int i = 0; i < arraySize; i++)
            {
                var property_i = arrayProperty.GetArrayElementAtIndex(i);
                var skip = depth;
                for (int c = 0; c < vpath.Length; c++)
                {
                    if (vpath[c] != '#' || skip-- > 0)
                    {
                        continue;
                    }
                    finalVpath = vpath.Substring(0, c - 1) + ".data[" + i.ToString() + vpath.Substring(c + 1, vpath.Length - c - 1);
                    break;
                }
                if (loadInList)
                {
                    var prop = so.FindProperty(property_i.propertyPath + pathSuffix);
                    list.Add((prop, finalVpath));
                }
                else
                {
                    GetPropertiesList(depth + 1, property_i.propertyPath + pathSuffix, finalVpath, so, list);
                }
            }
        }

        private static void UpgradeBinaryFiles(BindDatabase.DeltaDatabase deltaDb)
        {
            _trackDB.InitializeInstances();
            foreach (var deltaType in deltaDb.types)
            {
                if (deltaType.fields.Count == 0)
                {
                    continue;
                }

                var typePath = AssetDatabase.GUIDToAssetPath(deltaType.guid);
                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(typePath);
                var type = monoScript.GetClass();

                foreach (var obj in _trackDB.GetTargets())
                {
                    if (type.IsInstanceOfType(obj))
                    {
                        UpgradeObject(obj, deltaType.fields);
                    }
                }
            }
        }

        private static bool IsPotentialArrayElement(string path) => path.Contains(".Array[#]", StringComparison.Ordinal);

#endregion

        #region [  UTILITIES  ]

        private static string GetHierarchyPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        private static string GetHierarchyPath(Component component)
        {
            return $"{GetHierarchyPath(component.gameObject)}[{component.GetType().Name}]";
        }

        private static bool TryGetAtPath(Transform root, string path, out Object obj)
        {
            if (string.IsNullOrEmpty(path))
            {
                obj = null;
                return false;
            }

            if (!path.EndsWith(']'))
            {
                obj = FindGameObject(root, path);
                return obj;
            }

            var index = path.LastIndexOf('[');
            if (index < 0)
            {
                obj = FindGameObject(root, path);
                return obj;
            }

            var goPath = path[0..index];
            var componentType = path[(index + 1)..^1];

            var go = FindGameObject(root, goPath);
            var component = go ? go.GetComponent(componentType) : null;

            obj = component ? component : go;

            return true;
        }

        private static GameObject FindGameObject(Transform root, string path)
        {
            if (!root)
            {
                return GameObject.Find(path);
            }
            var go = root.Find(path);
            return go ? go.gameObject : GameObject.Find(path);
        }

        private static bool SetData(TrackingProperty property, object value)
        {
            if (value == null)
            {
                return true;
            }
            if (value is Object obj)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                var guid = path != null ? AssetDatabase.AssetPathToGUID(path) : null;
                property.guid = guid;

                if (obj is GameObject go)
                {
                    property.value = GetHierarchyPath(go);
                    return true;
                }
                else if (obj is Component c)
                {
                    property.value = GetHierarchyPath(c);
                }

                return true;
            }

            property.value = JsonData.ToJson(value);
            return true;
        }

        private static void SetPropertyValue(SerializedProperty property, string value, string guid)
        {
            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer: property.intValue = JsonData.FromJson<int>(value); break;
                    case SerializedPropertyType.Boolean: property.boolValue = JsonData.FromJson<bool>(value); break;
                    case SerializedPropertyType.Float: property.floatValue = JsonData.FromJson<float>(value); break;
                    case SerializedPropertyType.String: property.stringValue = value; break;
                    case SerializedPropertyType.Color: property.colorValue = JsonData.FromJson<Color>(value); break;
                    case SerializedPropertyType.LayerMask: property.intValue = JsonData.FromJson<LayerMask>(value); break;
                    case SerializedPropertyType.Enum: property.enumValueIndex = JsonData.FromJson<int>(value); break;
                    case SerializedPropertyType.Vector2: property.vector2Value = JsonData.FromJson<Vector2>(value); break;
                    case SerializedPropertyType.Vector3: property.vector3Value = JsonData.FromJson<Vector3>(value); break;
                    case SerializedPropertyType.Vector4: property.vector4Value = JsonData.FromJson<Vector4>(value); break;
                    case SerializedPropertyType.Rect: property.rectValue = JsonData.FromJson<Rect>(value); break;
                    case SerializedPropertyType.ArraySize: property.arraySize = JsonData.FromJson<int>(value); break;
                    case SerializedPropertyType.Character: property.stringValue = value; break;
                    case SerializedPropertyType.AnimationCurve: property.animationCurveValue = JsonData.FromJson<AnimationCurve>(value); break;
                    case SerializedPropertyType.Bounds: property.boundsValue = JsonData.FromJson<Bounds>(value); break;
                    case SerializedPropertyType.Gradient: property.SetValue(JsonData.FromJson<Gradient>(value)); break;
                    case SerializedPropertyType.Quaternion: property.quaternionValue = JsonData.FromJson<Quaternion>(value); break;
                    case SerializedPropertyType.Vector2Int: property.vector2IntValue = JsonData.FromJson<Vector2Int>(value); break;
                    case SerializedPropertyType.Vector3Int: property.vector3IntValue = JsonData.FromJson<Vector3Int>(value); break;
                    case SerializedPropertyType.RectInt: property.rectIntValue = JsonData.FromJson<RectInt>(value); break;
                    case SerializedPropertyType.BoundsInt: property.boundsIntValue = JsonData.FromJson<BoundsInt>(value); break;
                    case SerializedPropertyType.Hash128: property.hash128Value = JsonData.FromJson<Hash128>(value); break;
                    case SerializedPropertyType.ManagedReference:
                        try
                        {
                            property.managedReferenceValue = JsonData.FromJson(value, Type.GetType(property.managedReferenceFieldTypename));
                        }
                        catch { }
                        break;

                    case SerializedPropertyType.ExposedReference: property.exposedReferenceValue = GetObjectValue(value, guid); break;
                    case SerializedPropertyType.ObjectReference: property.objectReferenceValue = GetObjectValue(value, guid); break;
                }
            }
            catch(Exception ex)
            {
                Debug.LogException(ex, property.serializedObject.targetObject);
            }
        }

        private static Object GetObjectValue(string value, string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return TryGetAtPath(null, value, out var obj) ? obj : null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<Object>(path);

            if (!asset)
            {
                return null;
            }

            if (asset is GameObject go)
            {
                return TryGetAtPath(go.transform, value, out var obj) ? obj : null;
            }

            return asset;
        }

        private static object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return property.intValue;
                case SerializedPropertyType.Boolean: return property.boolValue;
                case SerializedPropertyType.Float: return property.floatValue;
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Color: return property.colorValue;
                case SerializedPropertyType.ObjectReference: return property.objectReferenceValue;
                case SerializedPropertyType.LayerMask: return property.intValue;
                case SerializedPropertyType.Enum: return property.enumValueIndex;
                case SerializedPropertyType.Vector2: return property.vector2Value;
                case SerializedPropertyType.Vector3: return property.vector3Value;
                case SerializedPropertyType.Vector4: return property.vector4Value;
                case SerializedPropertyType.Rect: return property.rectValue;
                case SerializedPropertyType.ArraySize: return property.intValue;
                case SerializedPropertyType.Character: return (char)property.intValue;
                case SerializedPropertyType.AnimationCurve: return property.animationCurveValue;
                case SerializedPropertyType.Bounds: return property.boundsValue;
                case SerializedPropertyType.Gradient: return property.GetValue();
                case SerializedPropertyType.Quaternion: return property.quaternionValue;
                case SerializedPropertyType.ExposedReference: return property.exposedReferenceValue;
                case SerializedPropertyType.FixedBufferSize: return property.fixedBufferSize;
                case SerializedPropertyType.Vector2Int: return property.vector2IntValue;
                case SerializedPropertyType.Vector3Int: return property.vector3IntValue;
                case SerializedPropertyType.RectInt: return property.rectIntValue;
                case SerializedPropertyType.BoundsInt: return property.boundsIntValue;
                case SerializedPropertyType.ManagedReference: return property.managedReferenceValue;
                case SerializedPropertyType.Hash128: return property.hash128Value;
                default: return null;
            }
        }

        #endregion

        [Serializable]
        private class TrackingDatabase
        {
            public List<TrackingType> types = new List<TrackingType>();

            private Dictionary<Object, TrackingInstance> _instances;

            public bool TryGetInstance(Object obj, out TrackingInstance instance)
            {
                if (_instances == null)
                {
                    InitializeInstances();
                }

                return _instances.TryGetValue(obj, out instance);
            }

            public void InitializeInstances()
            {
                _instances = new Dictionary<Object, TrackingInstance>();
                foreach (var group in types.SelectMany(t => t.instances).GroupBy(i => i.guid))
                {
                    var path = AssetDatabase.GUIDToAssetPath(group.Key);
                    var root = default(Transform);

                    if (path.EndsWith(".unity", StringComparison.Ordinal))
                    {
                        var scene = EditorSceneManager.GetSceneByPath(path);
                        if (!scene.IsValid() || !scene.isLoaded)
                        {
                            continue;
                        }
                        root = null;
                    }
                    else if (path.EndsWith(".prefab", StringComparison.Ordinal))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (!prefab)
                        {
                            continue;
                        }
                        root = prefab.transform;
                    }
                    else
                    {
                        var key = AssetDatabase.LoadAssetAtPath<Object>(path);
                        if (key)
                        {
                            _instances[key] = group.FirstOrDefault();
                        }
                        continue;
                    }

                    foreach (var i in group)
                    {
                        if (TryGetAtPath(root, i.hierarchyPath, out var key) && key)
                        {
                            _instances[key] = i;
                        }
                    }
                }
            }

            internal IEnumerable<Object> GetTargets() => _instances.Keys;
        }

        [Serializable]
        private class TrackingType
        {
            public string name;
            public List<TrackingInstance> instances = new List<TrackingInstance>();
        }

        [Serializable]
        private class TrackingInstance
        {
            public string guid;
            public string hierarchyPath;
            public List<TrackingProperty> properties = new List<TrackingProperty>();

            private Dictionary<string, TrackingProperty> _dict;

            public void Initialize()
            {
                if(_dict != null)
                {
                    return;
                }

                _dict = new Dictionary<string, TrackingProperty>();
                foreach (var prop in properties)
                {
                    _dict[prop.path] = prop;
                }

                foreach(var prop in properties)
                {
                    if (string.IsNullOrEmpty(prop.parentPath))
                    {
                        continue;
                    }
                    if(!_dict.TryGetValue(prop.parentPath, out var parent))
                    {
                        continue;
                    }
                    prop.parent = parent;
                }
            }

            public bool TryGetProperty(string path, out TrackingProperty property)
            {
                return _dict.TryGetValue(path, out property);
            }
        }

        [Serializable]
        private class TrackingProperty
        {
            public string path;
            public string value;
            public string guid;
            public bool isBind;
            public string parentPath;

            [NonSerialized]
            private TrackingProperty _parent;
            [NonSerialized]
            public readonly List<TrackingProperty> children = new List<TrackingProperty>();
            public TrackingProperty parent
            {
                get => _parent;
                set
                {
                    if(_parent == value)
                    {
                        return;
                    }
                    _parent?.children.Remove(this);
                    _parent = value;
                    if(_parent?.children.Contains(this) == false)
                    {
                        _parent.children.Add(this);
                    }
                }
            }
        }
    }

}
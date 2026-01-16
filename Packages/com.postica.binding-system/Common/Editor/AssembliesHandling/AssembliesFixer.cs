using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Postica.BindingSystem
{
    [InitializeOnLoad]
    class AssembliesFixer : AssetPostprocessor
    {
        private static bool _initialized;
        
        private static bool IsDevelopmentProject => File.Exists(Path.Combine("Library/.postica_dev"));
        
        static AssembliesFixer()
        {
            Startup();
        }

        [InitializeOnLoadMethod]
        [DidReloadScripts]
        static void Startup()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            
            if (IsDevelopmentProject)
            {
                return;
            }

            EditorApplication.delayCall += UpdateAllAssemblies;
        }

        private static void UpdateAllAssemblies()
        {
            EditorApplication.LockReloadAssemblies();
            try
            {
                List<string> pathsToImport = new List<string>();
                if (UpdateFastReflectDefines(PointerLibStandardIsPresent(), pathsToImport, false))
                {
                    Log("Postica Binding System: Updated FastReflect assembly definition based on PointerLibStandard presence.");
                }

                if (UpdateMethodHookDefines(!MethodHookWindowsIsPresent(), pathsToImport, false))
                {
                    Log("Postica Binding System: Updated MethodHook assembly definition based on MethodHookWin presence.");
                }

                // if(pathsToImport.Count == 0)
                // {
                //     return;
                // }
                //
                // if (pathsToImport.Count == 1)
                // {
                //     AssetDatabase.ImportAsset(pathsToImport[0]);
                // }
                //
                // AssetDatabase.Refresh();
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (IsDevelopmentProject)
            {
                return;
            }
            
            // Lock the Asset Database during this operation to prevent multiple calls
            EditorApplication.LockReloadAssemblies();
            try
            {
                List<string> pathsToImport = new();
                if (deletedAssets.Any(d => d.EndsWith("Reflection/Plugins/PointerLibStandard.dll")))
                {
                    Log("PointerLibStandard.dll deleted, updating FastReflect defines.");
                    if(UpdateFastReflectDefines(false, pathsToImport, false))
                    {
                        Log("FastReflect define removed.");
                    }
                }
                else if (importedAssets.Any(i => i.EndsWith("Reflection/Plugins/PointerLibStandard.dll")))
                {
                    Log("PointerLibStandard.dll imported, updating FastReflect defines.");
                    if(UpdateFastReflectDefines(true, pathsToImport, false))
                    {
                        Log("FastReflect define added.");
                    }
                }

                if (deletedAssets.Any(d => d.EndsWith("Editor/UnsafeLib/Plugins/MethodHookWin.dll")))
                {
                    Log("MethodHookWin.dll deleted, updating MethodHook defines.");
                    if(UpdateMethodHookDefines(false, pathsToImport, false))
                    {
                        Log("MethodHook define removed.");
                    }
                }
                else if (importedAssets.Any(i => i.EndsWith("Editor/UnsafeLib/Plugins/MethodHookWin.dll")))
                {
                    Log("MethodHookWin.dll imported, updating MethodHook defines.");
                    if(UpdateMethodHookDefines(true, pathsToImport, false))
                    {
                        Log("MethodHook define imported.");
                    }
                }

                // if(pathsToImport.Count == 0)
                // {
                //     return;
                // }
                //
                // if (pathsToImport.Count == 1)
                // {
                //     AssetDatabase.ImportAsset(pathsToImport[0]);
                // }
                //
                // AssetDatabase.Refresh();
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }
        
        [Conditional("BS_DEBUG")]
        private static void Log(string message)
        {
#if BS_DEBUG
            Debug.Log("Postica.AssembliesFixer: " + message);
#endif
        }
        
        private static bool PointerLibStandardIsPresent() => AssemblyQuery.IsAssemblyPresent("PointerLibStandard");
        private static bool MethodHookWindowsIsPresent() => AssemblyQuery.IsDllPresent("Editor/UnsafeLib/Plugins/MethodHookWin");

        private static bool UpdateFastReflectDefines(bool addDefine, List<string> pathsToImport, bool autoImport)
        {
            var asmDefName = "Postica.Common.Reflection";
            var versionDefine = new AsmDefJson.VersionDefine
            {
                name = "Unity",
                expression = "2022",
                define = "POSTICA_FASTREFLECT_SUPPORTED"
            };
            return addDefine 
                ? TrySetVersionDefines(asmDefName, versionDefine, pathsToImport, autoImport) 
                : TryRemoveVersionDefines(asmDefName, versionDefine, pathsToImport, autoImport);
        }
        
        private static bool UpdateMethodHookDefines(bool isDllPresent, List<string> pathsToImport, bool autoImport)
        {
            var asmDefName = "Postica.Common.Editor.Unsafe";
            var versionDefine = new AsmDefJson.VersionDefine
            {
                name = "Unity",
                expression = "2022",
                define = "POSTICA_METHODHOOK_CODE"
            };
            
            if (isDllPresent)
            {
                var dllImporter = PluginImporter.GetAllImporters().FirstOrDefault(pi => pi.assetPath.EndsWith("Editor/UnsafeLib/Plugins/MethodHookWin.dll"));
                if (dllImporter != null && !dllImporter.isNativePlugin && !dllImporter.GetCompatibleWithEditor())
                {
                    TryRemoveVersionDefines(asmDefName, versionDefine, pathsToImport, autoImport);
                    dllImporter.SetCompatibleWithEditor(true);
                    dllImporter.SaveAndReimport();
                    return true;
                }

                return false;
            }
            
            return TrySetVersionDefines(asmDefName, versionDefine, pathsToImport, autoImport);
        }
        
        private static bool TrySetVersionDefines(string asmDefName,
            AsmDefJson.VersionDefine versionDefine,
            List<string> pathsToImport = null,
            bool autoImport = true)
        {
            var resourceName = versionDefine.name;
            var defineSymbol = versionDefine.define;
            var success = false;
            
            // Search for the assembly definition asset
            string[] guids = AssetDatabase.FindAssets(asmDefName + " t:AssemblyDefinitionAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asmDef = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(path);
                if (asmDef == null) continue;
                
                var json = JsonUtility.FromJson<AsmDefJson>(asmDef.text);
                if (json == null) continue;
                
                if(json.versionDefines.Any(v => v.name == resourceName && v.define == defineSymbol))
                {
                    continue;
                }
                        
                if (json.versionDefines == null)
                {
                    json.versionDefines = new[] { versionDefine };
                }
                else
                {
                    var versionDefinesList = json.versionDefines.ToList();
                    versionDefinesList.Add(versionDefine);
                    json.versionDefines = versionDefinesList.ToArray();
                }
                
                string updatedJson = JsonUtility.ToJson(json, true);
                File.WriteAllText(path, updatedJson);
                success = true;
                if (autoImport)
                {
                    AssetDatabase.ImportAsset(path);
                }
                else
                {
                    pathsToImport?.Add(path);
                }
            }
            
            return success;
        }
        
        private static bool TryRemoveVersionDefines(string asmDefName,
            AsmDefJson.VersionDefine versionDefine,
            List<string> pathsToImport = null,
            bool autoImport = true)
        {
            var success = false;
            var resourceName = versionDefine.name;
            var defineSymbol = versionDefine.define;
            // Search for the assembly definition asset
            string[] guids = AssetDatabase.FindAssets(asmDefName + " t:AssemblyDefinitionAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asmDef = AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(path);
                if (asmDef == null) continue;
                
                var json = JsonUtility.FromJson<AsmDefJson>(asmDef.text);
                if (json == null || json.versionDefines == null) continue;
                
                var versionDefinesList = json.versionDefines.ToList();
                int initialCount = versionDefinesList.Count;
                versionDefinesList.RemoveAll(v => v.name == resourceName && v.define == defineSymbol);
                
                if (versionDefinesList.Count < initialCount)
                {
                    json.versionDefines = versionDefinesList.ToArray();
                    string updatedJson = JsonUtility.ToJson(json, true);
                    File.WriteAllText(path, updatedJson);
                    success = true;
                    if (autoImport)
                    {
                        AssetDatabase.ImportAsset(path);
                    }
                    else
                    {
                        pathsToImport?.Add(path);
                    }
                }
            }
            return success;
        }
        
        [Serializable]
        internal class AsmDefJson
        {
            public string name;
            public string[] references;
            public string[] optionalUnityReferences;
            public string[] defineConstraints;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public VersionDefine[] versionDefines;
            public bool noEngineReferences;
            
            [Serializable]
            public class VersionDefine
            {
                public string name;
                public string expression;
                public string define;
            }
        }
    }
}

    
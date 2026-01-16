using System;
using Postica.BindingSystem.Accessors;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Postica.BindingSystem
{

    class BindingSystemIO
    {
        private static string _bindingSystemPath;
        private static string _bindingSystemLocalPath;
        private static string _bindingsResourcesPath;

        public static string RootFullPath
        {
            get
            {
                if (_bindingSystemPath == null)
                {
                    BuildPaths();
                }
                return _bindingSystemPath;
            }
        }

        public static string RootLocalPath
        {
            get
            {
                if (_bindingSystemLocalPath == null)
                {
                    BuildPaths();
                }
                return _bindingSystemLocalPath;
            }
        }
        
        public static string BindingsResourcesPath
        {
            get
            {
                if (_bindingsResourcesPath == null)
                {
                    _bindingsResourcesPath = FixAssetPath(BuildLocalPath("Bindings", "Resources"));
                }
                return _bindingsResourcesPath;
            }
        }
        
        [OnBindSystemUpgrade]
        private static void Upgrade(string fromVersion)
        {
            if(string.Compare(fromVersion, "2.2.5", StringComparison.Ordinal) < 0)
            {
                try
                {
                    // Move the Bindings folder from Assets to Packages
                    var from = FixAssetPath(Path.Combine("Assets", "Bindings"));
                    if (!Directory.Exists(from))
                    {
                        Debug.LogWarning(BindSystem.DebugPrefix +
                                         $"The Bindings folder does not exist at {from}. Nothing to move.");
                        return;
                    }

                    var to = FixAssetPath(BuildLocalPath("Bindings"));
                    if (Directory.Exists(to))
                    {
                        AssetDatabase.DeleteAsset(to);
                    }

                    Directory.Move(from, to);
                    File.Move(from + ".meta", to + ".meta");
                    AssetDatabase.Refresh();
                    Optimizer.RunAutoRegistrations(BindingSettings.Current);
                    Debug.Log(BindSystem.DebugPrefix + $"Successfully moved the Bindings folder from {from} to {to}.");
                }
                catch (Exception e)
                {
                    Debug.LogError(BindSystem.DebugPrefix + $"Failed to move the Bindings folder. Exception: {e}");
                }
            } 
        }

        private static void BuildPaths()
        {
            var rootDirectory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Packages");
            foreach (var directory in Directory.GetDirectories(rootDirectory, "*com.postica.binding-system*", SearchOption.AllDirectories))
            {
                var dinfo = new DirectoryInfo(directory);
                if (dinfo.GetDirectories("*PinningLogic*", SearchOption.AllDirectories).Any())
                {
                    _bindingSystemPath = dinfo.FullName;
                    _bindingSystemLocalPath = (FixAssetPath(dinfo.FullName).Replace(FixAssetPath(rootDirectory), "Packages") + '/').TrimStart('/');
                    break;
                }
            }
        }
        
        public static string FixAssetPath(string path) => path.Replace("\\", "/");

        public static string GetAssetPath(params string[] pieces)
        {
            var path = Path.Combine(RootLocalPath, Path.Combine(pieces));
            return FixAssetPath(path);
        }
        
        public static string BuildLocalPath(params string[] pieces)
        {
            var path = Path.Combine(RootLocalPath, Path.Combine(pieces));
            var directory = Path.GetDirectoryName(path);
            var shouldRefresh = false;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                shouldRefresh = true;
            }
            
            if (string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                // Most likely a directory
                Directory.CreateDirectory(path);
                shouldRefresh = true;
            }

            if (shouldRefresh)
            {
                // Let's refresh the AssetDatabase to make sure the new directory is recognized
                AssetDatabase.Refresh();
            }
            return FixAssetPath(path);
        }
        
        public static string BuildPath(params string[] pieces)
        {
            var path = Path.Combine(Path.Combine(pieces));
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                if (string.IsNullOrEmpty(Path.GetExtension(path)))
                {
                    // Most likely a directory
                    Directory.CreateDirectory(path);
                }
            }
            return FixAssetPath(path);
        }

        public static string GetResourcePath(string resourcePath)
        {
            return FixAssetPath(Path.Combine(BindingsResourcesPath, resourcePath));
        }
        
        public static string GetBindingsPath(string resourcePath)
        {
            return FixAssetPath(BuildLocalPath("Bindings", resourcePath));
        }
    }
}
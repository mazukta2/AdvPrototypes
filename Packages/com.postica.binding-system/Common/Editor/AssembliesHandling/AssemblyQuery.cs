using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Postica.BindingSystem
{
    class AssemblyQuery
    {
        public static bool IsAssemblyPresent(string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == assemblyName);
        }
        
        public static bool IsDllPresent(string dllPartialName)
        {
            // Get all dlls in the project
            if (!dllPartialName.ToLower().EndsWith(".dll"))
            {
                dllPartialName += ".dll";
            }

            var dlls = Directory.GetFiles(Application.dataPath, "*.dll", SearchOption.AllDirectories);
            return dlls.Any(dll => dll.Replace('\\', Path.PathSeparator).Replace('/', Path.PathSeparator).EndsWith(dllPartialName));
        }
    }
}
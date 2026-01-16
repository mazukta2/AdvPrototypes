using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Postica.BindingSystem
{

    public class BindingSystemBuild : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private string _FilePath => "Assets/AOT_Accessors.cs";
        private string _LinksFilePath => "Assets/zBuildBS_CanBeRemoved/link.xml";

        public void OnPreprocessBuild(BuildReport report)
        {
            var buildGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(buildGroup)) == ScriptingImplementation.IL2CPP)
            {
                Optimizer.GenerateLinkFile(_LinksFilePath, (s, v) => EditorUtility.DisplayProgressBar("Binding System", s, v));
            }
        }
        
        public void OnPostprocessBuild(BuildReport report)
        {
            if (File.Exists(_FilePath))
            {
                File.Delete(_FilePath);
            }
            if (File.Exists(_LinksFilePath))
            {
                var directory = Path.GetDirectoryName(_LinksFilePath);
                if (directory != null && Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                    if(File.Exists(directory + ".meta"))
                    {
                        File.Delete(directory + ".meta");
                    }
                }
                else
                {
                    File.Delete(_LinksFilePath);
                }
            }
        }
    }
}
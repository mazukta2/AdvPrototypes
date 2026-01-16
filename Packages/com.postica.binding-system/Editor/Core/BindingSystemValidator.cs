using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Postica.BindingSystem
{
    internal class BindingSystemValidator
    {

        private static DateTime _nextValidation;
        private static readonly HashSet<Scene> _validatedScenes = new();
        
        public static void AutoValidate(bool enable)
        {
            SceneManager.sceneLoaded -= ValidateScene;
            EditorSceneManager.sceneOpened -= ValidateScene;
            if (enable)
            {
                SceneManager.sceneLoaded += ValidateScene;
                EditorSceneManager.sceneOpened += ValidateScene;
                for (int i = 0; i < SceneManager.loadedSceneCount; i++)
                {
                    ValidateScene(SceneManager.GetSceneAt(i));
                }
            }
        }

        private static void ValidateScene(Scene scene, OpenSceneMode mode)
        {
            ValidateScene(scene);
        }

        private static void ValidateScene(Scene scene, LoadSceneMode _)
        {
            ValidateScene(scene);
        }

        public static void ValidateScene(Scene scene)
        {
            if (_nextValidation <= DateTime.Now)
            {
                _validatedScenes.Clear();
            }
            _nextValidation = DateTime.Now.AddSeconds(5);
            
            if (!scene.isLoaded)
            {
                return;
            }

            if (!scene.IsValid())
            {
                return;
            }
            
            if (!_validatedScenes.Add(scene))
            {
                return;
            }


            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                foreach (var component in rootGameObject.GetComponentsInChildren<MonoBehaviour>())
                {
                    ValidateObject(component, true);
                }
            }
        }

        public static bool ValidateObject(Object obj, bool logResult = false)
        {
            if (!SerializationUtility.HasManagedReferencesWithMissingTypes(obj))
            {
                return true;
            }

            var missingRefs = SerializationUtility.GetManagedReferencesWithMissingTypes(obj);
            foreach (var missingRef in missingRefs)
            {
                if (missingRef.className.Contains("modifier", StringComparison.OrdinalIgnoreCase))
                {
                    LogResult("Modifier", missingRef);
                    return false;
                }

                if (missingRef.className.Contains("convert", StringComparison.OrdinalIgnoreCase))
                {
                    LogResult("Converter", missingRef);
                    return false;
                }
            }

            return true;

            void LogResult(string type, ManagedReferenceMissingType missingRef)
            {
                if (logResult)
                {
                    Debug.LogError(BindSystem.DebugPrefix + $"Found potential missing {type} Type {missingRef.className}. Click the message to focus on target!", obj);
                }
            }
        }
    }
}
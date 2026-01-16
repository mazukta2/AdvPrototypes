using System;
using Postica.BindingSystem.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Postica.BindingSystem
{
    [InitializeOnLoad]
    class BindingSystemEditorInit
    {
        private static bool _initialized;
        private static Action _updates;

        static BindingSystemEditorInit() => Init();

        [UnityEditor.Callbacks.DidReloadScripts]
        [InitializeOnLoadMethod]
        internal static void Init()
        {
            if (_initialized) { return; }

            _initialized = true;

            RegisterReflectionFilters();

            RegisterReflectionOverrides();
            
            RegisterDrawerOverrides();

            RegisterBridgeFunctions();
        }

        private static void RegisterBridgeFunctions()
        {
            BindingEngineInternal._registerToEditorUpdate = a =>
            {
                if (_updates == null)
                {
                    EditorApplication.update += UpdateWrapper;
                }
                _updates += a;
            };
            BindingEngineInternal._unregisterFromEditorUpdate = a =>
            {
                _updates -= a;
                if (_updates == null)
                {
                    EditorApplication.update -= UpdateWrapper;
                }
            };
        }

        private static void UpdateWrapper()
        {
            _updates?.Invoke();
        }

        private static void RegisterReflectionFilters()
        {
            // Hide Unity hidden types
            ReflectionFactory.HideTypeTreeOfRoot<string>(Hide.InternalsOnly);
            ReflectionFactory.HideTypeTreeOfRoot<Matrix4x4>(Hide.InternalsOnly);
            //ReflectionFactory.HideTypeTreeOfRoot<Scene>(Hide.InternalsOnly);

            // Hide special members
            ReflectionFactory.HideMemberOf<Vector4>(v => v.normalized, Hide.ShowOnlyOnce);
            ReflectionFactory.HideMemberOf<Vector3>(v => v.normalized, Hide.ShowOnlyOnce);
            ReflectionFactory.HideMemberOf<Vector2>(v => v.normalized, Hide.ShowOnlyOnce);
            ReflectionFactory.HideMemberOf<Color>(v => v.linear, Hide.ShowOnlyOnce);
            ReflectionFactory.HideMemberOf<Color>(v => v.gamma, Hide.ShowOnlyOnce);
            ReflectionFactory.HideMemberOf<Transform>(v => v.root, Hide.ShowOnlyOnce);
            ReflectionFactory.HideMemberOf<Transform>(v => v.parent, Hide.ShowOnlyOnce);
            ReflectionFactory.HideMemberOf<Scene>(v => v.isDirty, Hide.Completely);
            ReflectionFactory.HideMemberOf<Scene>(v => v.isSubScene, Hide.Completely);
            ReflectionFactory.HideMemberOf<Scene>(v => v.buildIndex, Hide.Completely);
            ReflectionFactory.HideMemberOf<Scene>(v => v.handle, Hide.Completely);
            ReflectionFactory.HideMemberOf<Array>(v => v.LongLength, Hide.Completely);
            ReflectionFactory.HideMemberOf<Array>(v => v.Rank, Hide.Completely);

            // Hide redundant members
            ReflectionFactory.HideMemberOf<GameObject>(v => v.transform, Hide.Completely);
            ReflectionFactory.HideMemberOf<GameObject>(v => v.gameObject, Hide.Completely);
            ReflectionFactory.HideMemberOf<Component>(v => v.gameObject, Hide.Completely, true);
            ReflectionFactory.HideMemberOf<Component>(v => v.transform, Hide.Completely, true);
            ReflectionFactory.HideMemberOf<Component>(v => v.hideFlags, Hide.Completely, true);
            ReflectionFactory.HideMemberOf<Component>(v => v.name, Hide.Completely, true);
            ReflectionFactory.HideMemberOf<Component>(v => v.tag, Hide.Completely, true);
            ReflectionFactory.HideMemberOf<MonoBehaviour>(v => v.useGUILayout, Hide.Completely, true);
            ReflectionFactory.HideMemberOf<MonoBehaviour>(v => v.runInEditMode, Hide.Completely, true);
        }
        
        private static void RegisterReflectionOverrides()
        {
            RegisterGameObjectOverrides();
            RegisterParticleSystemModules();
        }
        
        private static void RegisterDrawerOverrides()
        {
            RegisterParticleSystemProxyDrawerOverrides();
            RegisterRectTransformProxyDrawerOverrides();
        }

        private static void RegisterParticleSystemProxyDrawerOverrides()
        {
            ProxyBindingsDrawOverrides.RegisterFilter(TryGetOverride);
            
            bool TryGetOverride(SerializedProperty property, out ProxyBindingsDrawOverrides.Overrides overrides)
            {
                overrides = null;
                if (property.serializedObject.targetObject is not ParticleSystem) { return false; }

                overrides = new ProxyBindingsDrawOverrides.Overrides()
                {
                    panelShiftX = 12f,
                    shiftX = 9,
                };
                return true;
            }
        }
        
        private static void RegisterRectTransformProxyDrawerOverrides()
        {
            ProxyBindingsDrawOverrides.RegisterFilter(TryGetOverride);
            
            bool TryGetOverride(SerializedProperty property, out ProxyBindingsDrawOverrides.Overrides overrides)
            {
                overrides = null;
                if (property.serializedObject.targetObject is not RectTransform) { return false; }

                if (property?.propertyPath.StartsWith("m_SizeDelta") != true
                    && property?.propertyPath.StartsWith("m_AnchoredPosition") != true
                    && property?.propertyPath.StartsWith("m_Anchor") != true)
                {
                    return false;
                }

                overrides = new ProxyBindingsDrawOverrides.Overrides()
                {
                    keepFieldHeight = true,
                    coverWithBackground = true,
                };
                return true;
            }
        }

        private static void RegisterParticleSystemModules()
        {
            var canWriteOverride = new ReflectionFactory.Override { canWrite = true };

            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.main, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.collision, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.emission, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.lights, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.noise, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.shape, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.trails, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.trigger, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.customData, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.externalForces, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.inheritVelocity, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.subEmitters, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.colorBySpeed, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.colorOverLifetime, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.forceOverLifetime, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.rotationBySpeed, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.rotationOverLifetime, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.sizeBySpeed, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.sizeOverLifetime, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.textureSheetAnimation, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.lifetimeByEmitterSpeed, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.limitVelocityOverLifetime, canWriteOverride);
            ReflectionFactory.OverrideMemberOf<ParticleSystem>(v => v.velocityOverLifetime, canWriteOverride);
        }
        
        private static void RegisterGameObjectOverrides()
        {
            var canWriteOverride = new ReflectionFactory.Override { canWrite = true };

            ReflectionFactory.OverrideMemberOf<GameObject>(v => v.activeSelf, canWriteOverride);
        }
    }
}

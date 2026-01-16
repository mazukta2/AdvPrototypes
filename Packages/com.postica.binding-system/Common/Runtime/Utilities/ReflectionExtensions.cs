using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// An utility class (with extension methods) to perform various reflection operations.
    /// </summary>
    internal static class ReflectionExtensions
    {
        private static readonly List<(Type type, string from, string to)> _unityPropertyMap = new()
        {
            (typeof(Camera), "m_BackGroundColor", nameof(Camera.backgroundColor)),
            (typeof(Camera), "field of view", nameof(Camera.fieldOfView)),
            (typeof(Light), "m_Shadows.m_Type", nameof(Light.shadows)),
            #if UNITY_EDITOR
            (typeof(Light), "m_Lightmapping", nameof(Light.lightmapBakeType)),
            #endif
            (typeof(Light), "m_Shadows.m_Strength", nameof(Light.shadowStrength)),
            (typeof(Light), "m_Shadows.m_Bias", nameof(Light.shadowBias)),
            (typeof(Light), "m_Shadows.m_Resolution", nameof(Light.shadowResolution)),
            (typeof(Light), "m_Shadows.m_NormalBias", nameof(Light.shadowNormalBias)),
            (typeof(Light), "m_Shadows.m_NearPlane", nameof(Light.shadowNearPlane)),
            // Rigidbody
            (typeof(Rigidbody), "m_ImplicitCom", nameof(Rigidbody.automaticCenterOfMass)),
            (typeof(Rigidbody), "m_ImplicitTensor", nameof(Rigidbody.automaticInertiaTensor)),
            (typeof(Rigidbody), "m_Interpolate", nameof(Rigidbody.interpolation)),
            (typeof(Rigidbody), "m_CollisionDetection", nameof(Rigidbody.collisionDetectionMode)),
            // Trail Renderer
            (typeof(TrailRenderer), "m_Parameters.", ""),
            // ParticleSystem
            (typeof(ParticleSystem), "InitialModule", nameof(ParticleSystem.main)),
            (typeof(ParticleSystem), nameof(ParticleSystem.MainModule), nameof(ParticleSystem.main)),
            (typeof(ParticleSystem), nameof(ParticleSystem.EmissionModule), nameof(ParticleSystem.emission)),
            (typeof(ParticleSystem), nameof(ParticleSystem.ShapeModule), nameof(ParticleSystem.shape)),
            (typeof(ParticleSystem), "VelocityModule", nameof(ParticleSystem.velocityOverLifetime)),
            (typeof(ParticleSystem), nameof(ParticleSystem.CollisionModule), nameof(ParticleSystem.collision)),
            (typeof(ParticleSystem), nameof(ParticleSystem.TriggerModule), nameof(ParticleSystem.trigger)),
            (typeof(ParticleSystem), nameof(ParticleSystem.LimitVelocityOverLifetimeModule), nameof(ParticleSystem.limitVelocityOverLifetime)),
            (typeof(ParticleSystem), "ClampvelocityOverLifetime", nameof(ParticleSystem.limitVelocityOverLifetime)),
            (typeof(ParticleSystem), "ClampVelocityModule", nameof(ParticleSystem.limitVelocityOverLifetime)),
            (typeof(ParticleSystem), nameof(ParticleSystem.InheritVelocityModule), nameof(ParticleSystem.inheritVelocity)),
            (typeof(ParticleSystem), "InheritvelocityOverLifetime", nameof(ParticleSystem.inheritVelocity)),
            (typeof(ParticleSystem), nameof(ParticleSystem.ForceOverLifetimeModule), nameof(ParticleSystem.forceOverLifetime)),
            (typeof(ParticleSystem), "ForceModule", nameof(ParticleSystem.forceOverLifetime)),
            (typeof(ParticleSystem), nameof(ParticleSystem.LifetimeByEmitterSpeedModule), nameof(ParticleSystem.lifetimeByEmitterSpeed)),
            (typeof(ParticleSystem), nameof(ParticleSystem.ColorOverLifetimeModule), nameof(ParticleSystem.colorOverLifetime)),
            (typeof(ParticleSystem), "ColorModule", nameof(ParticleSystem.colorOverLifetime)),
            (typeof(ParticleSystem), nameof(ParticleSystem.ColorBySpeedModule), nameof(ParticleSystem.colorBySpeed)),
            (typeof(ParticleSystem), nameof(ParticleSystem.SizeOverLifetimeModule), nameof(ParticleSystem.sizeOverLifetime)),
            (typeof(ParticleSystem), "SizeModule", nameof(ParticleSystem.sizeOverLifetime)),
            (typeof(ParticleSystem), nameof(ParticleSystem.SizeBySpeedModule), nameof(ParticleSystem.sizeBySpeed)),
            (typeof(ParticleSystem), nameof(ParticleSystem.RotationOverLifetimeModule), nameof(ParticleSystem.rotationOverLifetime)),
            (typeof(ParticleSystem), "RotationModule", nameof(ParticleSystem.rotationOverLifetime)),
            (typeof(ParticleSystem), nameof(ParticleSystem.RotationBySpeedModule), nameof(ParticleSystem.rotationBySpeed)),
            (typeof(ParticleSystem), nameof(ParticleSystem.ExternalForcesModule), nameof(ParticleSystem.externalForces)),
            (typeof(ParticleSystem), nameof(ParticleSystem.NoiseModule), nameof(ParticleSystem.noise)),
            (typeof(ParticleSystem), nameof(ParticleSystem.SubEmittersModule), nameof(ParticleSystem.subEmitters)),
            (typeof(ParticleSystem), "SubModule", nameof(ParticleSystem.subEmitters)),
            (typeof(ParticleSystem), nameof(ParticleSystem.LightsModule), nameof(ParticleSystem.lights)),
            (typeof(ParticleSystem), nameof(ParticleSystem.TrailModule), nameof(ParticleSystem.trails)),
            (typeof(ParticleSystem), nameof(ParticleSystem.CustomDataModule), nameof(ParticleSystem.customData)),
            (typeof(ParticleSystem), nameof(ParticleSystem.TextureSheetAnimationModule), nameof(ParticleSystem.textureSheetAnimation)),
            (typeof(ParticleSystem.MinMaxCurve), "scalar", nameof(ParticleSystem.MinMaxCurve.constant)),
            (typeof(ParticleSystem.MainModule), "maxNumParticles", "maxParticles"),
            (typeof(ParticleSystem.MainModule), "rotation3D", "startRotation3D"),
            (typeof(ParticleSystem.MainModule), "size3D", "startSize3D"),
            (typeof(ParticleSystem), "startDelay", "main.startDelay"),
            (typeof(ParticleSystem), "lengthInSec", "main.duration"),
            (typeof(ParticleSystem), "looping", "main.loop"),
            (typeof(ParticleSystem), "prewarm", "main.prewarm"),
            (typeof(ParticleSystem), "moveWithTransform", "main.simulationSpace"),
            (typeof(ParticleSystem), "moveWithCustomTransform", "main.customSimulationSpace"),
            (typeof(ParticleSystem), "simulationSpeed", "main.simulationSpeed"),
            (typeof(ParticleSystem), "useUnscaledTime", "main.useUnscaledTime"),
            (typeof(ParticleSystem), "emitterVelocityMode", "main.emitterVelocityMode"),
            (typeof(ParticleSystem), "autoRandomSeed", "useAutoRandomSeed"),
            (typeof(ParticleSystem), "stopAction", "main.stopAction"),
            (typeof(ParticleSystem), "cullingMode", "main.cullingMode"),
            (typeof(ParticleSystem), "ringBufferMode", "main.ringBufferMode"),
            (typeof(ParticleSystem), "ringBufferLoopRange", "main.ringBufferLoopRange"),
            (typeof(ParticleSystem.ShapeModule), "type", nameof(ParticleSystem.ShapeModule.shapeType)),
            (typeof(ParticleSystem.LimitVelocityOverLifetimeModule), "separateAxis", nameof(ParticleSystem.LimitVelocityOverLifetimeModule.separateAxes)),
            (typeof(ParticleSystem.LimitVelocityOverLifetimeModule), "magnitude", nameof(ParticleSystem.LimitVelocityOverLifetimeModule.limit)),
            (typeof(ParticleSystem.LimitVelocityOverLifetimeModule), "drag", nameof(ParticleSystem.LimitVelocityOverLifetimeModule.drag)),
            (typeof(ParticleSystem.InheritVelocityModule), "m_Curve", nameof(ParticleSystem.InheritVelocityModule.curve)),
            (typeof(ParticleSystem.ForceOverLifetimeModule), "inWorldSpace", nameof(ParticleSystem.ForceOverLifetimeModule.space)),
            (typeof(ParticleSystem.ColorOverLifetimeModule), "gradient", nameof(ParticleSystem.ColorOverLifetimeModule.color)),
            (typeof(ParticleSystem.ColorBySpeedModule), "gradient", nameof(ParticleSystem.ColorBySpeedModule.color)),
            (typeof(ParticleSystem.SizeOverLifetimeModule), "curve", nameof(ParticleSystem.SizeOverLifetimeModule.size)),
            (typeof(ParticleSystem.SizeBySpeedModule), "curve", nameof(ParticleSystem.SizeBySpeedModule.size)),
            (typeof(ParticleSystem.RotationOverLifetimeModule), "curve", nameof(ParticleSystem.RotationOverLifetimeModule.x)),
            (typeof(ParticleSystem.RotationBySpeedModule), "curve", nameof(ParticleSystem.RotationBySpeedModule.x)),
            (typeof(ParticleSystem.CollisionModule), "m_EnergyLossOnCollision", nameof(ParticleSystem.CollisionModule.lifetimeLoss)),
            (typeof(ParticleSystem.CollisionModule), "collisionMessages", nameof(ParticleSystem.CollisionModule.sendCollisionMessages)),
            
            // TextMeshPro
            (typeof(TMPro.TMP_Text), "m_text", "text"),
            (typeof(TMPro.TMP_Text), "m_fontColor", "color"),
            (typeof(TMPro.TMP_Text), "m_fontColorGradient", "colorGradient"),
            
            // UNITY UI
            (typeof(UnityEngine.UI.Image), "m_FillAmount", nameof(UnityEngine.UI.Image.fillAmount)),
            
            // COMPONENTS
            (typeof(Renderer), "m_Enabled", nameof(Renderer.enabled)),
            (typeof(Behaviour), "m_Enabled", nameof(Behaviour.enabled)),
            (typeof(Collider), "m_Enabled", nameof(Collider.enabled)),
            (typeof(GameObject), "m_IsActive", nameof(GameObject.activeSelf)),
        };

        public static bool RerouteFieldPath(Type type, string from, string to, bool overwrite = true)
        {
            for (int i = 0; i < _unityPropertyMap.Count; i++)
            {
                var (t, f, _) = _unityPropertyMap[i];
                if (t == type && f == from)
                {
                    if (overwrite)
                    {
                        _unityPropertyMap[i] = (t, f, to);
                        return true;
                    }
                    return false;
                }
            }
            
            _unityPropertyMap.Add((type, from, to));
            return true;
        }
        
        public static bool UnRerouteFieldPath(Type type, string from)
        {
            for (int i = 0; i < _unityPropertyMap.Count; i++)
            {
                var (t, f, _) = _unityPropertyMap[i];
                if (t == type && f == from)
                {
                    _unityPropertyMap.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
        
        private static (string name, string correctedPath) PropertyMap(Type type, string path)
        {
            foreach (var (t, from, to) in _unityPropertyMap)
            {
                if (type != null && t?.IsAssignableFrom(type) == false)
                {
                    continue;
                }
                if (path.Contains(from))
                {
                    return (to, path.Replace(from, to));
                }
            }
            
            return default;
        }
        
        private static (string name, string correctedPath) SmartPropertyMap(Type type, string path)
        {
            foreach (var (t, from, to) in _unityPropertyMap)
            {
                if (type != null && t?.IsAssignableFrom(type) == false)
                {
                    continue;
                }
                if (path.Contains(from))
                {
                    return (to, path.Replace(from, to));
                }
            }

            var index = path.IndexOf('.');
            if (index == -1)
            {
                return (path, path);
            }
            
            return (path.Substring(0, index), path);
        }
        
        private static bool TryPropertyMap(Type type, string path, out string name, out string reduced)
        {
            foreach (var (t, from, to) in _unityPropertyMap)
            {
                if (type != null && t?.IsAssignableFrom(type) == false)
                {
                    continue;
                }
                if (path.Contains(from))
                {
                    name = to;
                    reduced = path.Replace(from, to);
                    return true;
                }
            }
            
            name = null;
            reduced = null;
            return false;
        }
        
        /// <summary>
        /// Get the type of the member info.
        /// </summary>
        /// <param name="member">The member to get the type from</param>
        /// <returns>The type of the memeber</returns>
        internal static Type GetMemberType(this MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo info: return info.FieldType;
                case PropertyInfo info: return info.PropertyType;
                case MethodInfo info: return info.ReturnType;
                case Type info: return info;
                case EventInfo info: return info.EventHandlerType;
                default: return null;
            }
        }
        
        /// <summary>
        /// Get the value of the member from specified <see cref="target"/>.
        /// </summary>
        /// <param name="member"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        internal static object GetValue(this MemberInfo member, object target)
        {
            switch (member)
            {
                case FieldInfo info: return info.GetValue(target);
                case PropertyInfo info: return info.GetValue(target);
                case MethodInfo info: return info.Invoke(target, null);
                default: return null;
            }
        }

        /// <summary>
        /// Tries to convert a unity serialized path into a runtime reflection path.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        /// <param name="runtimePath"></param>
        /// <param name="deepSearch">Whether to search for valid properties or fields, or just compute the path</param>
        /// <returns></returns>
        internal static bool TryMakeUnityRuntimePath(this Type type, string path, out string runtimePath, bool onlyUnityObjects = true, bool deepSearch = false)
        {
            if (type == null)
            {
                runtimePath = null;
                return false;
            }

            if (string.IsNullOrEmpty(path))
            {
                runtimePath = null;
                return false;
            }
            
            if (onlyUnityObjects && !type.IsUnityType())
            {
                runtimePath = null;
                return false;
            }
            
            var (name, reduced) = PropertyMap(type, path);
            if(name == null)
            {
                runtimePath = null;
                return false;
            }

            runtimePath = reduced;
            
            if (!deepSearch || !reduced.Contains('.'))
            {
                return true;
            }

            var sb = new StringBuilder();
            
            var currentType = type;
            
            while (!string.IsNullOrEmpty(reduced) && reduced.Contains('.'))
            {
                var index = reduced.IndexOf('.');
                reduced = reduced.Substring(index + 1);
                var property = currentType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    currentType = property.PropertyType;
                    sb.Append(property.Name).Append('.');
                    (name, reduced) = SmartPropertyMap(currentType, reduced);
                    continue;
                }
                var field = currentType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    currentType = field.FieldType;
                    sb.Append(field.Name).Append('.');
                    (name, reduced) = SmartPropertyMap(currentType, reduced);
                    continue;
                }
                
                // runtimePath = null;
                return true;
            }

            sb.Append(name);
            runtimePath = sb.ToString();
            return true;
        }

        /// <summary>
        /// Tries to get the property from the unity object from a unity serialized path.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        /// <param name="property"></param>
        /// <param name="reducedPath"></param>
        /// <param name="useFullPath"></param>
        /// <returns></returns>
        internal static bool TryGetUnityObjectProperty(this Type type, string path, out PropertyInfo property,
            out string reducedPath, bool useFullPath = false)
        {
            // Check if the type is null
            if (type == null)
            {
                property = null;
                reducedPath = null;
                return false;
            }

            if (string.IsNullOrEmpty(path))
            {
                property = null;
                reducedPath = null;
                return false;
            }
            
            if (!type.IsUnityType())
            {
                property = null;
                reducedPath = null;
                return false;
            }
            
            var (name, reduced) = PropertyMap(type, path);
            if(name == null)
            {
                property = null;
                reducedPath = null;
                return false;
            }

            var longerName = useFullPath ? reduced : name;
            if (longerName.Contains('.'))
            {
                reducedPath = reduced;
                property = null;
                
                // Multiple properties
                var parts = longerName.Split('.');
                var currentType = type;
                foreach (var part in parts)
                {
                    property = currentType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (property == null)
                    {
                        reducedPath = null;
                        break;
                    }

                    currentType = property.PropertyType;
                }

                if (property != null)
                {
                    return true;
                }
            }
            
            property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            reducedPath = reduced;
            return property != null;
        }
    }
}

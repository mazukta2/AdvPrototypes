using UnityEngine;
using Object = UnityEngine.Object;

namespace Postica.Common
{
    /// <summary>
    /// This class contains extension methods for the <see cref="UnityEngine.Object"/> class.
    /// </summary>
    internal static class UnityObjectExtensions
    {
        /// <summary>
        /// Returns the scene path of the Unity object, or its name if it is not part of a scene.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetScenePathOrName(this Object obj)
        {
            // Handle non-scene objects (prefabs/assets)
            if (obj is GameObject gameObject)
            {
                return GetScenePathOrName(gameObject);
            }
            
            if(obj is Component component)
            {
                // For components, return the GameObject's scene path or name
                return GetScenePathOrName(component.gameObject) + $" ({component.GetType().FullName})";
            }

            // For other Unity objects, return the name
            return obj ? obj.name : string.Empty;
        }
        
        /// <summary>
        /// Returns the scene path of the GameObject, or its name if it is not part of a scene.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        public static string GetScenePathOrName(this GameObject gameObject)
        {
            if (gameObject == null) 
                return string.Empty;
        
            // Handle non-scene objects (prefabs/assets)
            if (!gameObject.scene.IsValid())
                return gameObject.name;
        
            // Calculate hierarchy depth to pre-allocate array
            Transform current = gameObject.transform;
            int depth = 0;
            while (current != null)
            {
                depth++;
                current = current.parent;
            }
        
            // Fill names in reverse order (leaf to root)
            string[] names = new string[depth];
            current = gameObject.transform;
            int index = depth - 1;
            while (current != null)
            {
                names[index--] = current.name;
                current = current.parent;
            }
        
            // Join names with separators
            return string.Join("/", names);
        }

        /// <summary>
        /// Returns true if the object is a valid scene object, meaning it is either a GameObject or Component that belongs to a valid scene.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="allowAssets">What to return in case the object is neither a <see cref="GameObject"/> nor a <see cref="Component"/></param>
        /// <returns></returns>
        internal static bool IsValidSceneObject(this Object obj, bool allowAssets = false)
        {
            if (obj == null) return false;

            // Check if the object is a GameObject or Component and if it belongs to a valid scene
            if (obj is GameObject gameObject)
            {
                return gameObject.scene.IsValid();
            }

            if (obj is Component component)
            {
                return component.gameObject.scene.IsValid();
            }

            // For other Unity objects, we assume they are not scene objects
            return allowAssets;
        }

        /// <summary>
        /// Checks if the object is active in the hierarchy.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static bool IsActiveInHierarchy(this Object obj)
        {
            if (obj is GameObject gameObject)
            {
                return gameObject.activeInHierarchy;
            }
            
            if(obj is Behaviour behaviour)
            {
                return behaviour.isActiveAndEnabled;
            }

            if (obj is Component component)
            {
                return component.gameObject.activeInHierarchy;
            }

            // For other Unity objects, we assume they are always active
            return true;
        }
    }
}

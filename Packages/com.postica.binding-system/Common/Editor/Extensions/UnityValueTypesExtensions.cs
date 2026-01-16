using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    public static class UnityValueTypesExtensions
    {
        public static Rect Resized(this Rect rect, float width, float height)
        {
            rect.width = width;
            rect.height = height;
            return rect;
        }

        public static Rect OffsetX(this Rect rect, float x)
        {
            rect.x += x;
            return rect;
        }
        
        public static Rect OffsetY(this Rect rect, float y)
        {
            rect.y += y;
            return rect;
        }
        
        public static Rect Offset(this Rect rect, float x, float y)
        {
            rect.x += x;
            rect.y += y;
            return rect;
        }
        
        public static Rect HalfHeight(this Rect rect)
        {
            rect.height /= 2;
            return rect;
        }

        public static Rect Inflate(this Rect rect, float pixels)
        {
            return new Rect(rect.x - pixels, rect.y - pixels, rect.width + pixels * 2, rect.height + pixels * 2);
        }

        public static Rect Shrink(this Rect rect, float pixels)
        {
            return new Rect(rect.x + pixels, rect.y + pixels, rect.width - pixels * 2, rect.height - pixels * 2);
        }

        public static Rect WithHeight(this Rect rect, float height)
        {
            return new Rect(rect.x, rect.y, rect.width, height);
        }

        public static Rect WithWidth(this Rect rect, float width)
        {
            return new Rect(rect.x, rect.y, width, rect.height);
        }

        public static Rect FromRight(this Rect rect, float pixels)
        {
            return new Rect(rect.x, rect.y, rect.width - pixels, rect.height);
        }

        public static Rect FromLeft(this Rect rect, float pixels)
        {
            return new Rect(rect.x + pixels, rect.y, rect.width - pixels, rect.height);
        }

        public static Rect FromTop(this Rect rect, float pixels)
        {
            return new Rect(rect.x, rect.y + pixels, rect.width, rect.height - pixels);
        }

        public static Rect FromBottom(this Rect rect, float pixels)
        {
            return new Rect(rect.x, rect.y, rect.width, rect.height - pixels);
        }
        
        public static Rect AbsSize(this Rect rect)
        {
            rect.width = Mathf.Abs(rect.width);
            rect.height = Mathf.Abs(rect.height);
            return rect;
        }

        internal static bool IsAssignableTo(this Object source, Object target)
        {
            return target.IsFromScene() || !source.IsFromScene();
        }

        internal static bool IsFromScene(this Object obj)
        {
            return obj.TryGetGameObject(out var go) && !string.IsNullOrEmpty(go.scene.path);
        }

        internal static bool TryGetGameObject(this Object obj, out GameObject gameObject)
        {
            if (!obj)
            {
                gameObject = null;
                return false;
            }
            
            if (obj is GameObject go)
            {
                gameObject = go;
                return true;
            }

            if (obj is Component c)
            {
                gameObject = c.gameObject;
                return true;
            }

            gameObject = null;
            return false;
        }
        
        internal static bool IsSceneObject(this Object obj)
        {
            return obj.TryGetGameObject(out var go) && IsSceneObject(go);
        }
        
        internal static bool IsSceneObject(this GameObject obj)
        {
            return !PrefabUtility.IsPartOfPrefabAsset(obj);
        }
        
        internal static string GetHierarchyPath(this GameObject go)
        {
            return go ? GetHierarchyPath(go.transform) : null;
        }
        
        internal static string GetHierarchyPath(this Transform t)
        {
            return t.parent ? GetHierarchyPath(t.parent) + "/" + t.name : t.name;
        }
    }
}
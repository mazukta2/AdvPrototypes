using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Postica.Common
{
    static class ObjectIcon
    {
        public delegate bool TryGetIconDelegate(Type value, out Texture2D icon);

        
        private static readonly Dictionary<Type, Texture2D> Icons = new();
        private static readonly List<(Delegate predicate, Texture2D icon)> SpecializedIcons = new();
        private static Texture2D _enumIcon;
        private static Texture2D _testIcon;
        private static Texture2D _defaultIcon;
        private static bool _initialized;
        private static readonly List<TryGetIconDelegate> Resolvers = new();
        private static readonly List<Action> OnInitialize = new();

        // For more info, check: https://unitylist.com/p/5c3/Unity-editor-icons
        // Or this link as well: https://github.com/halak/unity-editor-icons
        public static class EditorIcons
        {
            public class Icon
            {
                private readonly string _liteName;
                private readonly string _darkName;

                private bool? _isSmallDarkSkin;
                private bool? _isBigDarkSkin;
                private Texture2D _small;
                private Texture2D _big;

                public Texture2D Small
                {
                    get
                    {
                        if (_isSmallDarkSkin != EditorGUIUtility.isProSkin)
                        {
                            _isSmallDarkSkin = EditorGUIUtility.isProSkin;
                            _small = Fetch(_liteName, _darkName, string.Empty);
                        }
                        return _small;
                    }
                }

                public Texture2D Big
                {
                    get
                    {
                        if (_isBigDarkSkin != EditorGUIUtility.isProSkin)
                        {
                            _isBigDarkSkin = EditorGUIUtility.isProSkin;
                            var texture = Fetch(_liteName, _darkName, "@2x");
                            _big = texture ? texture : Small;
                        }
                        return _big;
                    }
                }

                internal Icon(string liteSkinResource, string darkSkinResource = null)
                {
                    _liteName = liteSkinResource;
                    _darkName = darkSkinResource;
                }

                private static Texture2D Fetch(string liteResource, string darkResource, string suffix)
                {
                    if (!EditorGUIUtility.isProSkin)
                    {
                        return EditorGUIUtility.FindTexture(liteResource + suffix);
                    }
                    if (darkResource != null)
                    {
                        return EditorGUIUtility.FindTexture(darkResource + suffix);
                    }
                    var attemptIcon = EditorGUIUtility.FindTexture("d_" + liteResource + suffix);
                    if (attemptIcon != null) return attemptIcon;
                    attemptIcon = EditorGUIUtility.FindTexture(liteResource + suffix);
                    if (attemptIcon != null) return attemptIcon;
                    attemptIcon = EditorResources.Load<Texture2D>("d_" + liteResource + suffix, false);
                    if (attemptIcon != null) return attemptIcon;
                    return EditorResources.Load<Texture2D>(liteResource + suffix, false);
                }

                public static implicit operator Texture2D(Icon set) => set.Big;
            }

            public static readonly Icon CSharpScript        = new Icon("cs Script Icon");
            public static readonly Icon ScriptableObjectIcon= new Icon("ScriptableObject Icon");
            public static readonly Icon PrefabIcon          = new Icon("Prefab Icon");
            public static readonly Icon FolderIcon          = new Icon("Folder Icon");
            public static readonly Icon Font                = new Icon("Font Icon");
            public static readonly Icon Menu3Dots           = new Icon("_Menu");
            public static readonly Icon Gear                = new Icon("_Popup");
            public static readonly Icon Search              = new Icon("Search Icon");
            public static readonly Icon Save                = new Icon("SaveAs");
            public static readonly Icon Valid               = new Icon("Valid");
            public static readonly Icon Dropdown            = new Icon("icon dropdown");
            public static readonly Icon Lamp                = new Icon("Lighting");
            public static readonly Icon Plus                = new Icon("Toolbar Plus");
            public static readonly Icon PlusMore            = new Icon("Toolbar Plus More");
            public static readonly Icon Preset              = new Icon("Preset.Context");
            public static readonly Icon AudioLoop           = new Icon("preAudioLoopOff");
            public static readonly Icon VerticalLayout      = new Icon("VerticalLayoutGroup Icon");
            public static readonly Icon Refresh             = new Icon("Refresh");

            public static readonly Icon HierarchyWindow     = new Icon("UnityEditor.SceneHierarchyWindow");

            public static readonly Icon ConsoleWindow       = new Icon("UnityEditor.ConsoleWindow");
            public static readonly Icon ConsoleError_Gray   = new Icon("console.erroricon.inactive.sml");
            public static readonly Icon ConsoleError        = new Icon("console.erroricon.sml");
            public static readonly Icon ConsoleInfo_Gray    = new Icon("console.infoicon.inactive.sml");
            public static readonly Icon ConsoleInfo         = new Icon("console.infoicon.sml");
            public static readonly Icon ConsoleWarn_Gray    = new Icon("console.warnicon.inactive.sml");
            public static readonly Icon ConsoleWarn         = new Icon("console.warnicon.sml");

            public static readonly Icon ViewToolOrbit       = new Icon("ViewToolOrbit");
            public static readonly Icon ViewToolZoom        = new Icon("ViewToolZoom");
        }

        private static void EnsureReady()
        {
            if (_initialized)
            {
                return;
            }
            _initialized = true;
            foreach (var action in OnInitialize)
            {
                action?.Invoke();
            }
        }
        
        public static void RegisterOnInitialize(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (_initialized)
            {
                action();
                return;
            }

            if(OnInitialize.Contains(action))
            {
                return;
            }

            OnInitialize.Add(action);
        }
        
        public static void RegisterResolver(TryGetIconDelegate resolver)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            if(Resolvers.Contains(resolver))
            {
                return;
            }

            Resolvers.Add(resolver);
        }

        public static bool UnregisterResolver(TryGetIconDelegate resolver)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            return Resolvers.Remove(resolver);
        }

        public static void RegisterEnumIcon(Texture2D icon)
        {
            if (!icon)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            _enumIcon = icon;
        }
        
        public static void RegisterDefaultIcon(Texture2D icon)
        {
            if (!icon)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            _defaultIcon = icon;
        }

        public static void RegisterIconFor<T>(Texture2D icon)
        {
            if (!icon)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            Icons[typeof(T)] = icon;
        }

        public static void RegisterIconWhen<T>(Predicate<T> condition, Texture2D icon)
        {
            if (condition is null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            if (icon is null)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            SpecializedIcons.Add((condition, icon));
        }

        public static Texture2D GetFor<T>() => GetFor(typeof(T));

        public static Texture2D GetFor(Type type)
        {
            EnsureReady();
            
            if (Icons.TryGetValue(type, out var icon) && icon)
            {
                return icon;
            }
            
            // Check for custom icon
            var typeIcon = type.GetCustomAttribute<TypeIconAttribute>();
            if (typeIcon != null)
            {
                var resourceTexture = Resources.Load<Texture2D>(typeIcon.ResourcePath);
                if (resourceTexture)
                {
                    Icons[type] = resourceTexture;
                    return resourceTexture;
                }
            }

            if (type.IsEnum)
            {
                return _enumIcon;
            }

            var texture = AssetPreview.GetMiniTypeThumbnail(type);
            if(!texture && typeof(Component).IsAssignableFrom(type))
            {
                Texture2D csharpIcon = EditorIcons.CSharpScript;
                texture = csharpIcon ? csharpIcon : AssetPreview.GetMiniTypeThumbnail(typeof(MonoScript));
            }
            else if (texture != null && texture.name == "d_DefaultAsset Icon" && typeof(ScriptableObject).IsAssignableFrom(type))
            {
                Texture2D scriptableObjectIcon = EditorIcons.ScriptableObjectIcon;
                texture = scriptableObjectIcon ? scriptableObjectIcon : EditorGUIUtility.ObjectContent(null, typeof(ScriptableObject))?.image as Texture2D;
            } 

            if(!texture)
            {
                foreach (var resolver in Resolvers)
                {
                    if (resolver(type, out texture))
                    {
                        break;
                    }
                }
            }

            if (texture)
            {
                // Cache it
                Icons[type] = texture;
            }
            else if(_defaultIcon)
            {
                texture = _defaultIcon;
                Icons[type] = texture;
            }
            return texture;
        }

        public static Texture2D GetFor<T>(T value)
        {
            EnsureReady();
            
            if(!ReferenceEquals(value, null))
            {
                foreach(var (predicate, icon) in SpecializedIcons)
                {
                    if(predicate is Predicate<T> condition && condition(value))
                    {
                        return icon;
                    }
                }
                if(value is UnityEngine.Object tObject)
                {
                    var icon = AssetPreview.GetMiniThumbnail(tObject);
                    if (icon)
                    {
                        return icon;
                    }
                    var objectContent = EditorGUIUtility.ObjectContent(tObject, typeof(T));
                    if (objectContent?.image)
                    {
                        return objectContent.image as Texture2D;
                    }
                }
            }

            var iconT = GetFor<T>();
            if(!iconT && value is MonoBehaviour)
            {
                return GetFor<MonoBehaviour>();
            }
            return iconT;
        }
    }
}
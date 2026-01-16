using UnityEditor;
using UnityEngine;
using Postica.Common;

namespace Postica.BindingSystem
{
    partial class BindDataDrawer
    {
        protected internal static class Errors
        {
            public static class Classes
            {
                public const string BindMode = "bind-mode";
                public const string Path = "bind-path";
                public const string MissingComponent = "missing-component";
                public const string MissingTarget = "missing-target";
            }
        }

        protected internal class Contents
        {
            public readonly GUIContent mixedValue = new GUIContent(@"—", "Mixed Values");
            public readonly GUIContent bindIcon = new GUIContent(string.Empty, "Bind this field");
            public readonly GUIContent bindMenu = new GUIContent("···", "Show Bind Menu");
            public readonly GUIContent path = new GUIContent("Path");
            public readonly GUIContent multiplePaths = new GUIContent("Multiple Paths");
            public readonly GUIContent target = new GUIContent("Bind Source");
            public readonly GUIContent multipleTargetTypes = new GUIContent("Multiple Types");
            public readonly GUIContent multipleTargetObjects = new GUIContent("Same Type - Multiple Objects");
            public readonly GUIContent multipleTargetIncompatibleTypes = new GUIContent("Incompatible Types", ObjectIcon.EditorIcons.ConsoleError.Small, "Selected types are incompatible between them. The path cannot be set when multiple incompatible types are selected.");
            public readonly GUIContent multipleTargetComponentTypes = new GUIContent(ObjectIcon.EditorIcons.ConsoleWarn.Small, "Multiple Components of the same GameObject are used");
            public readonly GUIContent targetAssetsNotAllowed = new GUIContent("Assets NOT Allowed", ObjectIcon.EditorIcons.ConsoleError.Small);
            public readonly GUIContent targetSceneObjectsNotAllowed = new GUIContent("Scene Objects NOT Allowed", ObjectIcon.EditorIcons.ConsoleError.Small);
            public readonly GUIContent formattedPath = new GUIContent();
            public readonly GUIContent readConverter = new GUIContent("Read Convert");
            public readonly GUIContent writeConverter = new GUIContent("Write Convert");
            public readonly GUIContent multipleConverters = new GUIContent("Converters editing is not allowed with multi selection", ObjectIcon.EditorIcons.ConsoleWarn_Gray.Small);
            public readonly GUIContent multipleModifiers = new GUIContent("Modifiers editing is not allowed with multi selection", ObjectIcon.EditorIcons.ConsoleWarn_Gray.Small);
            public readonly GUIContent modifier = new GUIContent("Modifier");
            public readonly GUIContent modifierUp = new GUIContent("▲");
            public readonly GUIContent modifierDown = new GUIContent("▼");
            public readonly GUIContent modifierRemove = new GUIContent("‒", "Remove this modifier");
            public readonly GUIContent error = new GUIContent(Icons.ErrorCircle);
            public readonly GUIContent debug = new GUIContent(Icons.DebugIcon, "Live debug is active");
            public readonly GUIContent multipleBindModes = new GUIContent(Icons.ReadWriteIcon, "Multiple Bind Modes");
            public readonly GUIContent pathPreview = new GUIContent(ObjectIcon.EditorIcons.ViewToolOrbit.Small, "Preview the value at the path");


            public readonly GUIContent[] bindModes = new GUIContent[]
            {
                new GUIContent(EditorGUIUtility.isProSkin ? Icons.ReadIcon : Icons.ReadIcon.MultiplyBy(Color.grey), "Is ReadOnly"),
                new GUIContent(EditorGUIUtility.isProSkin ? Icons.WriteIcon : Icons.WriteIcon.MultiplyBy(Color.grey), "Is WriteOnly"),
                new GUIContent(EditorGUIUtility.isProSkin ? Icons.ReadWriteIcon : Icons.ReadWriteIcon.MultiplyBy(Color.grey), "Can Read and Write"),
            };

            public readonly GUIContent[] bindModesForModifiers = new GUIContent[]
            {
                new GUIContent(EditorGUIUtility.isProSkin ? Icons.ReadIcon : Icons.ReadIcon.MultiplyBy(Color.grey), "Applies when reading values"),
                new GUIContent(EditorGUIUtility.isProSkin ? Icons.WriteIcon : Icons.WriteIcon.MultiplyBy(Color.grey), "Applies when writing values"),
                new GUIContent(EditorGUIUtility.isProSkin ? Icons.ReadWriteIcon : Icons.ReadWriteIcon.MultiplyBy(Color.grey), "Applies both when reading and writing values"),
            };
        }

        private static readonly Color _unsafeColor = Color.yellow.Green(0.75f);

        private readonly GUIContent _tempContent = new GUIContent();
    }
}
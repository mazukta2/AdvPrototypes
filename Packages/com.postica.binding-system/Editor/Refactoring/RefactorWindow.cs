using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Postica.Common;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;


namespace Postica.BindingSystem.Refactoring
{
    internal class RefactorWindow : EditorWindow
    {
        private static RefactorWindow _instance;
        
        public static void ShowWindow()
        {
            GetWindow<RefactorWindow>(true);
        }

        private void OnEnable()
        {
            minSize = maxSize = new Vector2(640, 480);

            Rect main = EditorGUIUtility.GetMainWindowPosition();
            Rect pos = position;
            float centerWidth = (main.width - pos.width) * 0.5f;
            float centerHeight = (main.height - pos.height) * 0.5f;
            pos.x = main.x + centerWidth;
            pos.y = main.y + centerHeight;
            position = pos;
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;

            var ussPath = BindingSystemIO.BuildLocalPath("Editor", "Refactoring", "RefactorWindow.uss");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            root.styleSheets.Add(styleSheet);

            if (!EditorGUIUtility.isProSkin)
            {
                ussPath = BindingSystemIO.BuildLocalPath("Editor", "Refactoring", "RefactorWindowLite.uss"); 
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
                root.styleSheets.Add(styleSheet);
            }

            root.StretchToParentSize();
            
            BuildInfoPart(root);
            BuildRefactorPart(root);
            BuildControlsPart(root);
        }

        private void BuildInfoPart(VisualElement container)
        {
            var title = new Label("Code changes detected!").WithClass("header__title");
            var header = new VisualElement().WithClass("header");
            header.Add(title);
            container.Add(header);

            var infoText =
                "Changes in serialized fields and/or properties have been detected that may break existing bindings. Please review the following modifications and decide how to proceed.";
            var info = new Label(infoText).WithClass("info__text");
            var infoContainer = new VisualElement().WithClass("info");
            header.Add(infoContainer.WithChildren(new Image().WithClass("info__icon"), info));
        }

        private void BuildRefactorPart(VisualElement container)
        {
            var refactors = RefactorSystem.AllRefactors;
            var refactorsContainer = new ScrollView().WithClass("refactors");
            container.Add(refactorsContainer);
            
            foreach (var refactor in refactors)
            {
                if (refactor.isPersistent)
                {
                    continue;
                }

                var refactorContainer = CreateRefactorView(refactor);
                refactorsContainer.Add(refactorContainer);
            }
        }

        private VisualElement CreateRefactorView(RefactorSystem.Refactor refactor)
        {
            var refactorView = new VisualElement().WithClass("refactor");
            refactorView.EnableInClassList("refactor--ready", refactor.IsReady);
            refactorView.EnableInClassList("refactor--to-remove", refactor.IsToRemove);
            
            refactorView.Add(new Label(refactor.ActionCount.ToString()){tooltip = "Number of bindings to refactor in currently loaded scenes and assets"}.WithClass("refactor__actions"));
            
            var fromType = Type.GetType(refactor.fromType, throwOnError: false);
            var fromTypeView = new VisualElement().WithClass("refactor__from-type");
            var fromTypeIcon = new Image().WithClass("refactor__from-type__icon");
            var fromTypeLabel = new Label().WithClass("refactor__from-type__label");
            if (fromType != null)
            {
                fromTypeIcon.image = ObjectIcon.GetFor(fromType);
                fromTypeLabel.text = fromType.FullName;
            }
            else
            {
                // Get only the fullname from the AssemblyQualifiedName of refactor.fromType
                fromTypeLabel.text = refactor.fromType.Split(',')[0];
                fromTypeIcon.image = Resources.Load<Texture2D>("_bsicons/question_mark");
            }
            fromTypeView.WithChildren(fromTypeIcon, fromTypeLabel);
            refactorView.Add(fromTypeView);
            
            refactorView.Add(new Label(refactor.fromMember).WithClass("refactor__from-member"));
            refactorView.Add(new Label(" \u2192 ").WithClass("refactor__arrow"));
            
            var solverView = new VisualElement().WithClass("refactor__solver");
            
            RebuildSolver(refactor, refactorView, solverView);

            refactorView.Add(solverView);
            
            return refactorView;
        }

        private void RebuildSolver(RefactorSystem.Refactor refactor, VisualElement refactorView, VisualElement solverView)
        {
            solverView.Clear();
            
            refactorView.EnableInClassList("refactor--editing", false);
            refactorView.EnableInClassList("refactor--ready", refactor.IsReady);
            refactorView.EnableInClassList("refactor--to-remove", refactor.IsToRemove);
            
            // Add to remove label
            if (refactor.IsToRemove)
            {
                var toRemoveView = new VisualElement().WithClass("refactor__solver__to-remove");
                var toRemoveIcon = new Image().WithClass("refactor__solver__to-remove__icon");
                var toRemoveLabel = new Label("Will be deleted").WithClass("refactor__solver__to-remove__label");
                toRemoveView.WithChildren(toRemoveIcon, toRemoveLabel);
                solverView.Add(toRemoveView);
            }
            
            // Add ready view
            else if (refactor.IsReady)
            {
                var replaceView = new VisualElement().WithClass("refactor__solver__ready");
                var toType = Type.GetType(refactor.toType, throwOnError: false);
                var toTypeView = new VisualElement().WithClass("refactor__to-type");
                var toTypeIcon = new Image().WithClass("refactor__to-type__icon");
                var toTypeLabel = new Label().WithClass("refactor__to-type__label");
                if (toType != null)
                {
                    toTypeIcon.image = ObjectIcon.GetFor(toType);
                    toTypeLabel.text = toType.FullName;
                }
                else
                {
                    // Get only the fullname to the AssemblyQualifiedName of refactor.toType
                    toTypeLabel.text = refactor.toType.Split(',')[0];
                    toTypeIcon.image = Resources.Load<Texture2D>("_bsicons/question_mark");
                }
                toTypeView.WithChildren(toTypeIcon, toTypeLabel);
            
                replaceView.WithChildren(toTypeView, new Label(refactor.toMember).WithClass("refactor__to-member"));
                solverView.Add(replaceView);
            }
            
            // Add replace and delete buttons
            var deleteButton = new Button(){ focusable = false, tooltip = "Delete all bindings of this member" }
                .WithClass("refactor__solver__button", "refactor__solver__button--remove");
            deleteButton.clicked += () =>
            {
                refactor.toType = "[remove]";
                refactor.Refresh();
                RebuildSolver(refactor, refactorView, solverView);
            };
            var deleteIcon = new Image().Unpickable().WithClass("refactor__solver__button__icon");
            var deleteLabel = new Label("Delete").Unpickable().WithClass("refactor__solver__button__label");
            
            deleteButton.Add(deleteIcon);
            deleteButton.Add(deleteLabel);
            
            var replaceButton = new Button(){ focusable = false, tooltip = "Replace all bindings to this member" }
                .WithClass("refactor__solver__button", "refactor__solver__button--replace");
            replaceButton.clicked += () => TryReplaceFor(refactor, refactorView, solverView);
            var replaceIcon = new Image().Unpickable().WithClass("refactor__solver__button__icon");
            var replaceLabel = new Label("Replace").Unpickable().WithClass("refactor__solver__button__label");
            
            replaceButton.Add(replaceIcon);
            replaceButton.Add(replaceLabel);

            solverView.WithChildren(replaceButton, deleteButton);
        }

        private void TryReplaceFor(RefactorSystem.Refactor refactor, VisualElement refactorView, VisualElement solverView)
        {
            refactorView.EnableInClassList("refactor--editing", true);
            var namespaceIcon = Resources.Load<Texture2D>("_bsicons/namespace");
            var fromMember = refactor.fromMember;
            var fromType = Type.GetType(refactor.fromType, throwOnError: false);
            var includeUnity = fromType?.FullName?.StartsWith("Unity") == true;
            var valueType = !string.IsNullOrEmpty(refactor.fromMemberType) 
                        ? Type.GetType(refactor.fromMemberType, throwOnError: false)
                        : null;
            var valueIsConvertible = valueType != null && typeof(IConvertible).IsAssignableFrom(valueType);
            var dropdown = new SmartDropdown(true, "Replace with");
            foreach (var ns in TypeCache.GetTypesDerivedFrom(typeof(object))
                         .Where(t => IsValid(t, includeUnity)).GroupBy(t => t.Namespace ?? "No Namespace")
                         .OrderBy(n => n.Key))
            {
                if (ns.Key.StartsWith("<"))
                {
                    continue;
                }
                
                if (ns.Key.Contains("UnityEditor", StringComparison.OrdinalIgnoreCase) == true)
                {
                    continue;
                }
                
                foreach (var type in ns)
                {
                    var groupPath = type.FullName?.Replace('.', SmartDropdown.Separator)
                        .Replace('+', SmartDropdown.Separator);
                    groupPath ??= "No Namespace/" + type.Name;
                    var group = dropdown.GetGroupAt(groupPath);
                    group.Icon = ObjectIcon.GetFor(type);
                    group.Name = type.Name;
                    group.AllowDuplicates = true;
                    group.IsSelected = type.AssemblyQualifiedName == refactor.fromType;
                    group.Description = type.IsClass ? "Class" : "Struct";

                    var parent = group.Parent as SmartDropdown.IPathGroup;
                    while (parent != null)
                    {
                        parent.Icon = namespaceIcon;
                        parent = parent.Parent as SmartDropdown.IPathGroup;
                    }

                    var groupIsValid = false;

                    foreach (var member in type.GetMembers())
                    {
                        if (IsNotValid(member))
                        {
                            continue;
                        }

                        var (memberType, memberKind) = member switch
                        {
                            FieldInfo field => (field.FieldType, "Field"),
                            PropertyInfo property => (property.PropertyType, "Property"),
                            MethodInfo method => (method.ReturnType, "Method"),
                            _ => (null, null)
                        };

                        if (memberType == null)
                        {
                            continue;
                        }

                        if (valueType != null 
                            && memberType != valueType
                            && (valueIsConvertible != typeof(IConvertible).IsAssignableFrom(memberType)
                                || valueType?.IsAssignableFrom(memberType) != true))
                        {
                            continue;
                        }

                        groupIsValid = true;
                        var secondLabel = (valueType == null ? memberType.UserFriendlyName() + " - " : "") + memberKind;
                        var item = new DropdownItem(member.Name, secondLabel, () =>
                            {
                                refactor.toType = type.AssemblyQualifiedName;
                                refactor.toMember = member.Name;
                                refactor.Refresh();
                                RebuildSolver(refactor, refactorView, solverView);
                            }, ObjectIcon.GetFor(memberType),
                            canAlterGroup: false);
                        group.Add(member.Name, item);
                    }

                    if (!groupIsValid)
                    {
                        dropdown.Remove(group.Path);
                    }
                    else if (group.IsSelected)
                    {
                        group.SortChildren((a, b) => a.Name.AdvancedSimilarityDistance(fromMember).CompareTo(b.Name.AdvancedSimilarityDistance(fromMember)));
                        var item = group.Children.FirstOrDefault(i => i.Name.AdvancedSimilarityDistance(fromMember) < 4);
                        item?.OnPreBuildView(block =>
                        {
                            if (block is DropdownItem dpItem)
                            {
                                dpItem.SecondLabel = "Best Match".RT().Bold().Color(BindColors.Primary) + " - " + dpItem.SecondLabel;
                                dpItem.OnPreRender(v => v.AddToClassList("sd-best-match"));
                            }
                        });
                    }
                }
            }
            
            dropdown.RemoveEmptyGroups();
            dropdown.Root.Icon = Resources.Load<Texture2D>("_bsicons/refactor");
            dropdown.Show(solverView.worldBound.FromLeft(-40), onClose:r => refactorView.EnableInClassList("refactor--editing", false));
        }

        private static bool IsValid(Type type, bool includeUnity = false)
        {
            return !type.IsPrimitive 
                   && !type.IsSpecialName 
                   && !type.IsEnum 
                   && !type.IsInterface 
                   && !type.IsGenericType 
                   && !type.Name.StartsWith('<')
                   && (includeUnity || type.FullName?.StartsWith("Unity") != true)
                   && (type.IsSerializable || typeof(Object).IsAssignableFrom(type));
        }

        private bool IsNotValid(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.IsSpecialName || field.IsLiteral,
                PropertyInfo property => property.IsSpecialName || property.GetIndexParameters().Length > 0,
                // MethodInfo method => method.IsSpecialName,
                _ => true
            };
        }

        private void BuildControlsPart(VisualElement container)
        {
            var footer = new VisualElement().WithClass("footer");
            
            var buttonCancel = new Button(() =>
            {
                if(EditorUtility.DisplayDialog("Warning", "Are you sure you want to cancel? All changes will be lost.", "Yes", "No"))
                {
                    // Maybe should reset all refactors?
                    Close();
                    RepaintInspector();
                }
            }){text = "Cancel"}.WithClass("footer__button", "footer__button--cancel");
            
            var buttonApply = new Button(() =>
            {
                if (MayProceedWithWarningDialog())
                {
                    RefactorSystem.ApplyAllRefactors();
                    Close();
                    RepaintInspector();
                }
            }){text = "Apply All"}.WithClass("footer__button", "footer__button--apply");
            
            footer.Add(new VisualElement().WithClass("footer__logo")
                .WithChildren(new Image().WithClass("footer__logo__icon"), 
                    new Label(BindSystem.ProductName).WithClass("footer__logo__label")));
            footer.Add(buttonCancel);
            footer.Add(buttonApply);
            container.Add(footer);
        }

        private bool MayProceedWithWarningDialog()
        {
            if (RefactorSystem.AllRefactors.Any(r => !r.IsReady && !r.isPersistent))
            {
                var response = EditorUtility.DisplayDialog("Warning", "Some modifications were not considered. Please decide what to do with them before applying.", "Apply Anyway", "Cancel");
                return response;
            }

            return true;
        }

        private static void RepaintInspector()
        {
            var selection = Selection.objects;
            Selection.objects = Array.Empty<Object>();
            EditorApplication.delayCall += () => Selection.objects = selection;
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }
    }
}
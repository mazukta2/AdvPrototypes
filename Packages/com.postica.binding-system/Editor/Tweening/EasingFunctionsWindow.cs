using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using System.Collections.Generic;
using Postica.BindingSystem.Tweening;
using Postica.Common;
using UnityEditor.UIElements;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Tweening
{

    public class EasingFunctionsWindow : EditorWindow
    {
        private const int GRID_SIZE = 3;
        private const float CELL_SIZE = 100f;
        private const float PADDING = 10f;

        private List<EasingFunction> easingFunctions;
        private Action<EasingFunction> onFunctionSelected;
        private EasingFunction selectedFunction;
        private SerializedObject serializedObject;
        private (FunctionType functionType, EaseType easeType)? selected;
        private Func<float, float> customEase;
        
        [FormerlySerializedAs("animationDuration")]
        [SerializeField]
        [Range(0.1f, 5f)]
        private float duration = 2f;

#if BS_DEBUG
        [MenuItem("Window/Easing Functions")]
        public static void ShowWindow()
        {
            var rect = new Rect(0, 0, GRID_SIZE * (CELL_SIZE + PADDING) + PADDING,
                GRID_SIZE * (CELL_SIZE + PADDING) + PADDING);
            Show(rect, null);
        }
#endif
        
        public static void Show(Rect position, Action<EasingFunction> callback, (FunctionType, EaseType)? selected = null, Func<float, float> customEase = null)
        {
            var window = CreateInstance<EasingFunctionsWindow>();
            window.titleContent = new GUIContent("Select Easing Function");
            window.onFunctionSelected = callback;
            window.position = position;
            window.selected = selected;
            window.customEase = customEase;

            var gridSize = Mathf.Floor(position.width / CELL_SIZE);
            var minSize = new Vector2(gridSize * (CELL_SIZE + PADDING) + PADDING,
                gridSize * (CELL_SIZE + PADDING) + PADDING);
            var size = new Vector2(minSize.x, Mathf.Max(minSize.y, position.height));
            window.ShowAsDropDown(position, size);

            // window.minSize = window.maxSize = size;
            // window.Show();
        }

        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            serializedObject.Update();
            InitializeEasingFunctions();
            CreateGUI();
        }

        private void InitializeEasingFunctions()
        {
            void AddFuntions(List<EasingFunction> list, FunctionType functionType)
            {
                list.Add(new(EaseType.EaseIn, functionType));
                list.Add(new(EaseType.EaseOut, functionType));
                list.Add(new(EaseType.EaseInOut, functionType));
            }
            
            easingFunctions = new List<EasingFunction>
            {
                new("Linear", EaseType.EaseIn, FunctionType.Linear)
            };
            AddFuntions(easingFunctions, FunctionType.Sine);
            AddFuntions(easingFunctions, FunctionType.Quad);
            AddFuntions(easingFunctions, FunctionType.Cubic);
            AddFuntions(easingFunctions, FunctionType.Quart);
            AddFuntions(easingFunctions, FunctionType.Quint);
            AddFuntions(easingFunctions, FunctionType.Expo);
            AddFuntions(easingFunctions, FunctionType.Circ);
            AddFuntions(easingFunctions, FunctionType.Back);
            AddFuntions(easingFunctions, FunctionType.Elastic);
            AddFuntions(easingFunctions, FunctionType.Bounce);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.AddTweensStyle();
            root.AddToClassList("tween-root");
            if (!EditorGUIUtility.isProSkin)
            {
                root.styleSheets.Add(Resources.Load<StyleSheet>("_tweening/_style_lite"));
            }
            
            root.Clear();
            
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.WithClass("tween-scroll-view");
            root.Add(scrollView);
            
            var topCell = new EasingFunctionCell(easingFunctions[0], () => duration);
            topCell.clicked += () => OnFunctionSelected(easingFunctions[0]);
            if (selected?.functionType == easingFunctions[0].FunctionType)
            {
                topCell.AddToClassList("tween-cell--selected");
            }
            scrollView.Add(topCell);
            
            EasingFunctionCell selectedCell = null;
            
            if(customEase != null)
            {
                var customFunction = new EasingFunction("Custom", customEase);
                var customCell = new EasingFunctionCell(customFunction, () => duration);
                customCell.clicked += () => OnFunctionSelected(customFunction);
                scrollView.Add(customCell);

                if (selected != null && selected.Value.functionType == FunctionType.Custom)
                {
                    customCell.AddToClassList("tween-cell--selected");
                }
            }
            
            scrollView.Add(new VisualElement().WithClass("tween-cell__separator"));
            
            var selectedIndex = 0;
            
            foreach (var func in easingFunctions)
            {
                if(func == easingFunctions[0])
                {
                    continue;
                }
                var cell = new EasingFunctionCell(func, () => duration);
                cell.clicked += () => OnFunctionSelected(func);
                scrollView.Add(cell);
                if (selected == null || selectedCell != null)
                {
                    continue;
                }

                selectedIndex++;
                if (selected.Value.functionType == func.FunctionType && selected.Value.easeType == func.EaseType)
                {
                    selectedCell = cell;
                }
            }

            if (selectedCell != null)
            {
                selectedCell.AddToClassList("tween-cell--selected");
                scrollView.OnAttachToPanel(evt => scrollView.ScrollTo(selectedCell), 200);
                if (selectedIndex > 6)
                {
                    scrollView.Insert(customEase == null ? 1 : 2,
                        selectedCell.Clone(() => OnFunctionSelected(selectedCell.Function)));
                }
            }
            
            var header = new VisualElement().WithClass("tween-header")
                .WithChildren(
                    new Label("Select Easing Function").WithClass("tween-header__title"),
                    new PropertyField(){ focusable = false }.EnsureBind(serializedObject.FindProperty(nameof(duration))).WithClass("tween-header__duration"));
            root.Add(header);
        }

        private void OnFunctionSelected(EasingFunction function)
        {
            selectedFunction = function;
            onFunctionSelected?.Invoke(function);
            Close();
        }
        
        private void OnDisable()
        {
            serializedObject?.Dispose();
            serializedObject = null;
        }
    }
}
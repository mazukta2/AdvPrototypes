using Postica.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Postica.BindingSystem
{
    static partial class VisualElementExtensions
    {
        private static StyleSheet _darkStyle;
        private static StyleSheet _lightStyle;

        internal static StyleSheet DarkStyle
        {
            get
            {
                if (_darkStyle == null)
                {
                    _darkStyle = Resources.Load<StyleSheet>("_bsstyles/_style");
                }
                return _darkStyle;
            }
        }

        internal static StyleSheet LightStyle
        {
            get
            {
                if (_lightStyle == null)
                {
                    _lightStyle = Resources.Load<StyleSheet>("_bsstyles/_style_lite");
                }
                return _lightStyle;
            }
        }

        internal static StyleSheet CurrentStyle => EditorGUIUtility.isProSkin ? DarkStyle : LightStyle;

        // This method adds the current stylesheet to the binding system VisualElement for drawers.
        public static T AddBSStyle<T>(this T element) where T : VisualElement
        {
            element.styleSheets.Add(DarkStyle);
            if (!EditorGUIUtility.isProSkin)
            {
                element.styleSheets.Add(CurrentStyle);
            }
            return element;
        }
        
        // This method removes the current stylesheet from the binding system VisualElement for drawers.
        public static T RemoveBSStyle<T>(this T element) where T : VisualElement
        {
            element.styleSheets.Remove(DarkStyle);
            element.styleSheets.Remove(CurrentStyle);
            return element;
        }
    }
}
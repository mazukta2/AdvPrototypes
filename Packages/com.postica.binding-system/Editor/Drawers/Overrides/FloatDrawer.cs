using Postica.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Postica.BindingSystem
{
    //[CustomPropertyDrawer(typeof(float))]
    public class FloatDrawer : StackedPropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
            GUI.Label(new Rect(position.xMax - 40, position.y, 38, position.height), "float", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
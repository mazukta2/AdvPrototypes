using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using Postica.Common;

namespace Postica.Common
{
    class DropdownButton : BaseField<string>
    {
        public readonly Button buttonElement;
        private readonly Label _value;

        public event Action clicked
        {
            add => buttonElement.clicked += value;
            remove => buttonElement.clicked -= value;
        }

        public override string value
        {
            get => _value.text;
            set => _value.text = value;
        }

        public static (Button button, Label label) CreateOnlyButton()
        {
            var b = CreateInput(out _, out var l);
            return (b, l);
        }

        private static Button CreateInput(out Button button, out Label value)
        {
            button = new Button().StyleAsFieldInput()
#if UNITY_2022_3_OR_NEWER
                            .WithClass(BasePopupField<int, int>.inputUssClassName, PopupField<int>.inputUssClassName, DropdownField.inputUssClassName)
#endif
                            .WithoutClass(TextElement.ussClassName, Button.ussClassName);

            var chevron = new VisualElement() { pickingMode = PickingMode.Ignore }
#if UNITY_2022_3_OR_NEWER
                .WithClass(DropdownField.arrowUssClassName)
#endif
                ;

            value = new Label().WithClass("dropdown-value");

            return button.WithChildren(value, chevron);
        }

        // default constructor
        public DropdownButton() : base(null, CreateInput(out var button, out var label))
        {
            this.StyleAsField().WithClass(DropdownField.ussClassName 
#if UNITY_2022_3_OR_NEWER
                , BasePopupField<int, int>.ussClassName
#endif
                );

            buttonElement = button;
            _value = label;

            Add(buttonElement);
        }

        // constructor with callback
        public DropdownButton(Action clickEvent) : this()
        {
            buttonElement.clicked += clickEvent;
        }

        // constructor with label and callback
        public DropdownButton(string label, Action clickEvent) : this(clickEvent)
        {
            this.label = label;
        }

        // constructor with label
        public DropdownButton(string label) : this()
        {
            this.label = label;
        }

        public DropdownButton Aligned() => this.WithClass(BaseField<int>.alignedFieldUssClassName);

        public DropdownButton NotAsField() => this.StyleAsField(false).WithoutClass(DropdownField.ussClassName 
#if UNITY_2022_3_OR_NEWER
            , BasePopupField<int, int>.ussClassName
#endif
            );
    }
}
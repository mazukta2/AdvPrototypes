using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;
using System.Collections.Generic;

namespace Postica.Common
{
    public class MultiClickable : Clickable
    {
        public List<Clickable> clickables { get; }

        public MultiClickable(params Clickable[] clickables)
            : base((Action)null)
        {
            this.clickables = new List<Clickable>(clickables);
            activators.Add(new ManipulatorActivationFilter()
            {
                clickCount = 100, // To avoid using this click
            });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            foreach (var clickable in clickables)
            {
                target.AddManipulator(clickable);
            }
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            foreach (var clickable in clickables)
            {
                target.RemoveManipulator(clickable);
            }
        }
    }

}
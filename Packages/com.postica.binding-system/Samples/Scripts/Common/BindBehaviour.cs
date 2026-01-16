using System;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public abstract class BindBehaviour<T> : MonoBehaviour where T : Component
    {
        /// <summary>
        /// The component to control. It is a readonly bind, meaning that its value cannot be written.
        /// Since it is a bind, it may have a direct value, or a bound value retrieved from somewhere else.
        /// </summary>
        [Header("Setup")]
        public ReadOnlyBind<T> component;
        public ReadOnlyBind<bool> isEnabled;
        public ReadOnlyBind<bool> isGameObjectActive;
        
        private void Reset()
        {
            // Every value can be bound, the Bind() method transforms the value into a bind value
            component = GetComponentInChildren<T>().Bind();
            isEnabled = component.Value is Behaviour behaviour ? behaviour.enabled.Bind() : true.Bind();
            isGameObjectActive = component.Value.gameObject.activeSelf.Bind();
        }

        protected void Update()
        {
            if (!component.Value)
            {
                return;
            }

            UpdateInternal();
            
            if (component.Value is Behaviour behaviour)
            {
                behaviour.enabled = isEnabled.Value;
            }
            component.Value.gameObject.SetActive(isGameObjectActive.Value);
        }

        protected virtual void UpdateInternal()
        {
            
        }
    }
}

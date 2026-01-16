using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class AreaTrigger : MonoBehaviour
    {
        [Header("Settings")]
        public ReadOnlyBind<string> activationTag = "Player".Bind();
        public ReadOnlyBind<string> acceptedTag = "Player".Bind();
        public List<ReadOnlyBind<Rigidbody>> whiteList = new();

        [Header("Output")]
        // WriteOnlyBind will allow the write operation of the bind
        [WriteOnlyBind]
        public Bind<bool> isActive;
        [WriteOnlyBind]
        public Bind<float> timeActive;
        [WriteOnlyBind]
        public Bind<float> totalMass;
        [WriteOnlyBind]
        public Bind<int> totalColliders;
        
        private HashSet<Collider> colliders = new();
        private HashSet<Rigidbody> rigidbodies = new();
        private float startTime;

        private void Start()
        {
            isActive.Value = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(!IsValidCollider(other))
            {
                return;
            }
            if(colliders.Add(other) && !isActive.Value)
            {
                foreach (var col in colliders)
                {
                    if(col && col.attachedRigidbody 
                           && (string.IsNullOrEmpty(acceptedTag) || col.attachedRigidbody.CompareTag(activationTag)))
                    {
                        isActive.Value = true;
                        startTime = Time.time;
                        break;
                    }
                }
            }
            UpdateTotalMass();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsValidCollider(other))
            {
                return;
            }
            if (colliders.Remove(other) && colliders.Count == 0)
            {
                isActive.Value = false;
            }
            UpdateTotalMass();
        }
        
        private void Update()
        {
            UpdateTotalMass();
            timeActive.Value = isActive ? Time.time - startTime : 0;
            totalColliders.Value = colliders.Count;
        }

        private bool IsValidCollider(Collider collider)
        {
            if (!collider.attachedRigidbody)
            {
                return false;
            }
            
            if (!string.IsNullOrEmpty(acceptedTag)
                && !CompareTag(collider.attachedRigidbody,acceptedTag)
                && !string.IsNullOrEmpty(acceptedTag)
                && !CompareTag(collider.attachedRigidbody,activationTag))
            {
                return false;
            }
            
            // Check if the collider is in the white list
            foreach (var whiteListCollider in whiteList)
            {
                if (whiteListCollider.Value == collider.attachedRigidbody)
                {
                    return true;
                }
            }
            
            return string.IsNullOrEmpty(acceptedTag) || whiteList.Count == 0;
        }

        private void UpdateTotalMass()
        {
            rigidbodies.Clear();
            var mass = 0f;
            var hasActivationColliders = false;
            foreach (var collider in colliders)
            {
                if(!collider || !collider.attachedRigidbody)
                {
                    continue;
                }
                if (rigidbodies.Add(collider.attachedRigidbody))
                {
                    mass += collider.attachedRigidbody.mass;
                }
                if(!hasActivationColliders && CompareTag(collider.attachedRigidbody,activationTag))
                {
                    hasActivationColliders = true;
                }
            }

            if (!hasActivationColliders)
            {
                isActive.Value = false;
            }

            totalMass.Value = mass;
        }
        
        private static bool CompareTag(Component component, string tag)
        {
            return (string.IsNullOrEmpty(tag) && string.IsNullOrEmpty(tag)) || component.CompareTag(tag);
        }
    } 
}

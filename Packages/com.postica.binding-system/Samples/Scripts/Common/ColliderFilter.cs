using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    /// <summary>
    /// This component allows you to filter collisions with other colliders.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [SelectionBase]
    public class ColliderFilter : MonoBehaviour
    {
        public List<Collider> excludeColliders = new List<Collider>();
        public List<Collider> includeColliders = new List<Collider>();
        
        private void Start()
        {
            var thisCollider = GetComponent<Collider>();
            foreach (var other in excludeColliders)
            {
                Physics.IgnoreCollision(thisCollider, other, true);
            }
            foreach (var other in includeColliders)
            {
                Physics.IgnoreCollision(thisCollider, other, false);
            }
        }
    }
}
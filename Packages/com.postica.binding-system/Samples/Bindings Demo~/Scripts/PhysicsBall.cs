using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class PhysicsBall : MonoBehaviour
    {   
        [Header("Setup")]
        [Bind]
        [Range(0, 50)]
        public ReadOnlyBind<float> initialSpin;
        public ReadOnlyBind<float> lifeTime = 10f.Bind();
        
        [Header("Material Config")]
        #if UNITY_6000_0_OR_NEWER
        public ReadOnlyBind<PhysicsMaterial> physicMaterial;
        #else
        public ReadOnlyBind<PhysicMaterial> physicMaterial;
        #endif
        public ReadOnlyBind<float> dynamicFriction = 0.6f.Bind();
        public ReadOnlyBind<float> staticFriction = 0.8f.Bind();
        public ReadOnlyBind<float> bounciness = 0.1f.Bind();
        #if UNITY_6000_0_OR_NEWER
        public ReadOnlyBind<PhysicsMaterialCombine> frictionCombine = PhysicsMaterialCombine.Average.Bind();
        public ReadOnlyBind<PhysicsMaterialCombine> bounceCombine = PhysicsMaterialCombine.Average.Bind();
        #else
        public ReadOnlyBind<PhysicMaterialCombine> frictionCombine = PhysicMaterialCombine.Average.Bind();
        public ReadOnlyBind<PhysicMaterialCombine> bounceCombine = PhysicMaterialCombine.Average.Bind();
        #endif

        private Rigidbody _rb;
        
        private void Start()
        {
            if (lifeTime > 0)
            {
                Destroy(gameObject, lifeTime);
            }

            ApplyPhysicMaterialValues(physicMaterial);
            
            _rb = GetComponent<Rigidbody>();
            _rb.maxAngularVelocity = Mathf.Max(15f, initialSpin);
            _rb.AddTorque(transform.right * initialSpin.Value, ForceMode.VelocityChange);
        }

#if UNITY_6000_0_OR_NEWER
        private void ApplyPhysicMaterialValues(PhysicsMaterial material)
#else
        private void ApplyPhysicMaterialValues(PhysicMaterial material)
#endif
        {
            if (!material)
            {
                return;
            }
            
            // Apply material properties
            material.dynamicFriction = dynamicFriction.Value;
            material.staticFriction = staticFriction.Value;
            material.bounciness = bounciness.Value;
            material.frictionCombine = frictionCombine.Value;
            material.bounceCombine = bounceCombine.Value;
        }
    }
}

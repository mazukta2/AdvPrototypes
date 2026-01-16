using UnityEngine;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Samples
{
    public class PhysicsShurikenBall : MonoBehaviour
    {
        [Header("Setup")] 
        public ReadOnlyBind<Vector3> initialRotation;
        public ReadOnlyBind<float> lifeTime = 10f.Bind();
        
        [Header("Configuration")]
        [Bind]
        [Range(0, 20)]
        public ReadOnlyBind<float> spin = 6f.Bind();
        public ReadOnlyBind<float> mass = 1f.Bind();
        public ReadOnlyBind<float> drag = 0.1f.Bind();
        public ReadOnlyBind<float> angularDrag = 0.1f.Bind();
        public ReadOnlyBind<float> scale = 1f.Bind();
        
        private Rigidbody _rb;
        
        private void Start()
        {
            if (lifeTime > 0)
            {
                Destroy(gameObject, lifeTime);
            }

            _rb = GetComponent<Rigidbody>();
            _rb.maxAngularVelocity = 20;
            _rb.rotation = Quaternion.Euler(initialRotation);
            _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            _rb.mass = mass.Value;
#if UNITY_6000_0_OR_NEWER
            _rb.linearDamping = drag.Value;
            _rb.angularDamping = angularDrag.Value;
#else
            _rb.drag = drag.Value;
            _rb.angularDrag = angularDrag.Value;
#endif
            transform.localScale = Vector3.one * scale.Value;
            if (_rb.angularVelocity.magnitude < spin.Value * 1.5f)
            {
                _rb.AddTorque(transform.up * spin.Value, ForceMode.VelocityChange);
            }
        }
    }
}

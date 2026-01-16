using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class PhysicsProp : MonoBehaviour
    {   
        [Header("Setup")]
        [Bind]
        [Min(0)]
        public ReadOnlyBind<float> destroyDelay = 1f.Bind();
        public ReadOnlyBind<float> mass = 1f.Bind();
        
        [Header("Configuration")] 
        public Bind<bool> isAlive;
        public Bind<float> health = 100f.Bind();
        public Bind<bool> isDying;

        public float TakeDamage
        {
            get => 0;
            set => health.Value -= value;
        }
        
        private Rigidbody _rb;
        
        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            _rb.mass = mass.Value;
            isAlive.Value = health.Value > 0;
            if (isAlive.Value) return;
            
            Destroy(gameObject, destroyDelay.Value);
            isDying.Value = true;
        }
    }
}

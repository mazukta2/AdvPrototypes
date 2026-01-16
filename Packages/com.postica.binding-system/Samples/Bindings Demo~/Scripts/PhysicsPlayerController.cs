using UnityEngine;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Samples
{
    public class PhysicsPlayerController : MonoBehaviour
    {
        [Header("Configuration")]
        public ReadOnlyBind<bool> torqueMotion = true.Bind();
        public ReadOnlyBind<float> playerCenterHeight = 0.5f.Bind();
        public ReadOnlyBind<float> maxForwardSpeed = 5f.Bind();
        public ReadOnlyBind<float> maxSidewaysSpeed = 5f.Bind();
        public ReadOnlyBind<float> maxJumpVelocity = 5f.Bind();
        public ReadOnlyBind<float> moveInAirFactor = 0.01f.Bind();
        
        [Header("Material Config")]
#if UNITY_6000_0_OR_NEWER
        public ReadOnlyBind<PhysicsMaterial> physicMaterial;
#else
        public ReadOnlyBind<PhysicMaterial> physicMaterial;
#endif
        public ReadOnlyBind<float> dynamicFriction = 0.6f.Bind();
        public ReadOnlyBind<float> staticFriction = 0.8f.Bind();
        public ReadOnlyBind<float> bounciness = 0.1f.Bind();
        
        [Header("Input")]
        public ReadOnlyBind<Vector3> forwardDirection = Vector3.forward.Bind();
        public ReadOnlyBind<Vector3> rightDirection = Vector3.right.Bind();
        [Space]
        [Bind]
        [Range(-1, 1)]
        public ReadOnlyBind<float> forwardInput;
        [Bind]
        [Range(-1, 1)]
        public ReadOnlyBind<float> sidewaysInput;
        [Bind]
        [Range(0, 1)]
        public ReadOnlyBind<float> jumpInput;
        
        [Header("Output")]
        public Bind<float> currentSpeed;
        public Bind<bool> isGrounded;
        public Bind<float> distanceFromGround;
        
        private Rigidbody _rb;
        private Vector3 _lastJumpPoint;

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (Time.fixedDeltaTime == 0)
            {
                return;
            }

            ApplyPhysicMaterialValues();
            ApplyGroundVelocity();
            LimitGroundVelocity();
            ApplyJump();
        }

        private void ApplyPhysicMaterialValues()
        {
            // Apply material properties
            var material = physicMaterial.Value;
            if (!material) return;
            
            material.dynamicFriction = Mathf.Clamp01(dynamicFriction);
            material.staticFriction = Mathf.Clamp01(staticFriction);
            material.bounciness = Mathf.Clamp01(bounciness);
        }

        private void ApplyJump()
        {
            // Compute distance from ground
            var ray = new Ray(transform.position, Vector3.down);
            var distance = 100f;
            if (Physics.Raycast(ray, out var hit, distance, -1, QueryTriggerInteraction.Ignore))
            {
                distance = hit.distance - playerCenterHeight;
            }

            var isTouchingGround = distance < 0.02f;
            isGrounded.Value = isTouchingGround;
            distanceFromGround.Value = distance;

            if (isTouchingGround && jumpInput > 0)
            {
                _lastJumpPoint = hit.point;
                var jumpValue = Mathf.Clamp01(jumpInput) * maxJumpVelocity;
                var jumpVelocity = Mathf.Sqrt(2 * jumpValue * Physics.gravity.magnitude);
#if UNITY_6000_0_OR_NEWER
                if (_rb.linearVelocity.y < jumpVelocity)
                {
                    _rb.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
                }
#else
                if (_rb.velocity.y < jumpVelocity)
                {
                    _rb.AddForce(Vector3.up * jumpVelocity, ForceMode.VelocityChange);
                }
#endif
            }
        }

        private void LimitGroundVelocity()
        {
            // Limit the speed on the xz plane
#if UNITY_6000_0_OR_NEWER
            var velocity = _rb.linearVelocity;
#else
            var velocity = _rb.velocity;
#endif
            velocity.x = Mathf.Clamp(velocity.x, -maxForwardSpeed, maxForwardSpeed);
            velocity.z = Mathf.Clamp(velocity.z, -maxSidewaysSpeed, maxSidewaysSpeed);
            
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = velocity;
            currentSpeed.Value = _rb.linearVelocity.magnitude;
#else
            _rb.velocity = velocity;
            currentSpeed.Value = _rb.velocity.magnitude;
#endif
        }

        private void ApplyGroundVelocity()
        {
            if (torqueMotion)
            {
                var maxAngularForwardSpeed = maxForwardSpeed * 3.14f;
                var maxAngularSidewaysSpeed = maxSidewaysSpeed * 3.14f;
                _rb.maxAngularVelocity = Mathf.Min(maxAngularForwardSpeed, maxAngularSidewaysSpeed);
                var forward = rightDirection.Value * forwardInput * maxAngularForwardSpeed;
                var sideways = forwardDirection.Value * (-sidewaysInput * maxAngularSidewaysSpeed);
                _rb.AddTorque(forward, ForceMode.VelocityChange);
                _rb.AddTorque(sideways, ForceMode.VelocityChange);
            }
            else if(isGrounded)
            {
                var forward = forwardDirection.Value * forwardInput * maxForwardSpeed;
                var sideways = rightDirection.Value * sidewaysInput * maxSidewaysSpeed;
                _rb.AddForce(forward + sideways, ForceMode.VelocityChange);
            }

            if (!isGrounded && moveInAirFactor > 0)
            {
                var factor = moveInAirFactor.Value;
                var forward = forwardDirection.Value * forwardInput * maxForwardSpeed * factor;
                var sideways = rightDirection.Value * sidewaysInput * maxSidewaysSpeed * factor;
                _rb.AddForce(forward + sideways, ForceMode.VelocityChange);
            }
        }
    }

}
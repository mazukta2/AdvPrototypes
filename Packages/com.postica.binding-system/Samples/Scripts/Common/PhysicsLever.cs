using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class PhysicsLever : PhysicsBehaviour<HingeJoint>
    {
        [Space]
        [Tooltip("The main transform to use as lever")]
        public ReadOnlyBind<Transform> lever;
        [Tooltip("The initial rotation of the lever")]
        public ReadOnlyBind<Quaternion> startRotation = Quaternion.identity.Bind();
        [Tooltip("The axis around which the lever rotates")]
        public ReadOnlyBind<Vector3> rotationAxis = Vector3.up.Bind();
        [Tooltip("The position of the hinge in local space")]
        public ReadOnlyBind<Vector3> hingePosition = Vector3.zero.Bind();
        [Tooltip("The amount of rotation the lever can make")]
        public ReadOnlyBind<float> rotationAmount = 150f.Bind();
        [Tooltip("The force applied to the lever when rotating to the desired opening angle")]
        public ReadOnlyBind<float> rotationForce = 100f.Bind();
        
        [Header("Configuration")]
        public ReadOnlyBind<float> linearDrag = 0.5f.Bind();
        public ReadOnlyBind<float> angularDrag = 1f.Bind();
        
        [Header("Output")]
        [Bind]
        [Range(0, 1)]
        [Tooltip("The current value of the lever, from 0 to 1")]
        public Bind<float> value;

        private bool _shouldMoveJoint;

        protected override Transform GetCustomTarget() => lever;

        protected override void SetupJoint(HingeJoint joint)
        {
            // Setup the joint correctly
            // Configure based on fields values under setup
            joint.axis = rotationAxis.Value * Mathf.Sign(rotationAmount);
            joint.useLimits = true;
            joint.limits = new JointLimits
            {
                min = 0,
                max = Mathf.Abs(rotationAmount)
            };
            joint.useMotor = true;

            joint.anchor = hingePosition.Value;
            joint.connectedAnchor = JointInverseTransformPoint(transform.TransformPoint(hingePosition.Value));
        }

        protected override void Start()
        {
            base.Start();
            value.ValueChanged += (_, _) => _shouldMoveJoint = true;
        }

        private void FixedUpdate()
        {
            if (Time.fixedDeltaTime <= 0)
            {
                return;
            }
            
            var body = RigidBody;
#if UNITY_6000_0_OR_NEWER
            body.linearDamping = linearDrag;
            body.angularDamping = angularDrag;
#else
            body.drag = linearDrag;
            body.angularDrag = angularDrag;
#endif

            var joint = Joint;

            if (_shouldMoveJoint)
            {
                var targetAngle = Mathf.Clamp01(value) * (joint.limits.max - joint.limits.min);
                var targetVelocity = (targetAngle - joint.angle) / Time.fixedDeltaTime;
            
                joint.motor = new JointMotor()
                {
                    force = Mathf.Abs(rotationForce),
                    targetVelocity = targetVelocity
                };
            }
            else
            {
                value.Value =
                    Mathf.Clamp01((joint.angle - joint.limits.min) / (joint.limits.max - joint.limits.min));
            }
            
            _shouldMoveJoint = false;
        }
    }
}
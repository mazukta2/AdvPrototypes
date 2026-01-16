using System;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{

    /// <summary>
    /// A door that is controlled by physics, using a hinge joint. It can be opened and closed by setting the 'opening' field.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [SelectionBase]
    public class PhysicsDoor : PhysicsBehaviour<HingeJoint>
    {
        [Space]
        [Tooltip("The door physics object")]
        public Transform door;
        [Tooltip("The axis around which the door rotates")]
        public Vector3 rotationAxis = Vector3.up;
        [Tooltip("The position of the hinge in local space")]
        public Vector3 hingePosition = Vector3.zero;
        [Tooltip("The amount of rotation the door can make")]
        public float rotationAmount = 150f;
        [Tooltip("The force applied to the door when rotating to the desired opening angle")]
        public float rotationForce = 100f;
        [Tooltip("The tolerance in degrees to consider when the door reaches the desired opening")]
        public float rotationTolerance = 2f;
        [Tooltip("The dampening force when bouncing back")]
        public float rotationDampening = 10f;
        
        [Header("Input")]
        [Range(0, 1)]
        [Tooltip("The desired opening of the door, from 0 to 1")]
        public float desiredOpening;
        [Tooltip("When true, the door will always try to reach the desired opening, even when blocked")]
        public bool continuousOpening;
        [Tooltip("Whether the door is locked. A locked door cannot move in space")]
        public bool isLocked;

        [Header("Runtime")]
        [SerializeField]
        private float opening;

        [NonSerialized]
        private float _lastDesiredOpening;
        [NonSerialized]
        private float _lastOpening;
        
        protected override Transform GetCustomTarget() => door;
        
        public float CurrentOpening => opening;

        protected override void SetupJoint(HingeJoint joint)
        {
            // Setup the joint correctly
            // Configure based on fields values under setup
            joint.axis = rotationAxis * Mathf.Sign(rotationAmount);
            joint.useLimits = true;
            joint.limits = new JointLimits
            {
                min = 0,
                max = rotationAmount
            };

            joint.useMotor = false;
            joint.anchor = hingePosition;
            joint.connectedAnchor = JointInverseTransformPoint(Target.TransformPoint(hingePosition));
        }

        private void FixedUpdate()
        {
            if (Time.fixedDeltaTime <= 0)
            {
                return;
            }

            var body = RigidBody;
            body.constraints = isLocked ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;

            if(!Mathf.Approximately(opening, _lastOpening))
            {
                desiredOpening = opening;
            }
            
            var desiredChanged = !Mathf.Approximately(desiredOpening, _lastDesiredOpening);
            
            _lastDesiredOpening = desiredOpening;
            
            var joint = Joint;
            
            var maxAngle = joint.limits.max - joint.limits.min;
            
            var angle = float.IsNaN(joint.angle) ? 0 : joint.angle;
            var targetAngle = Mathf.Clamp01(desiredOpening) * (joint.limits.max - joint.limits.min);
            var openingReached = Mathf.Abs(angle - joint.limits.min - targetAngle) < rotationTolerance;

            opening = Mathf.Approximately(maxAngle, 0) ? opening : Mathf.Clamp01((angle - joint.limits.min) / maxAngle);
            _lastOpening = opening;
            
            if (desiredOpening == 0 && _lastDesiredOpening == 0)
            {
                joint.useSpring = false;
            }
            else if (openingReached && !continuousOpening)
            {
                joint.useSpring = false;
                joint.spring = default;
                opening = desiredOpening;
                _lastOpening = opening;
            }
            else if(desiredChanged && (!openingReached || continuousOpening))
            {
                joint.useSpring = true;
                joint.spring = new JointSpring
                {
                    spring = rotationForce,
                    damper = rotationDampening,
                    targetPosition = float.IsNaN(targetAngle) ? 0 : targetAngle
                };
            }
        }
    }
}
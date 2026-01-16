using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class PhysicsSlider : PhysicsBehaviour<ConfigurableJoint>
    {
        [Space]
        [Tooltip("The main transform to use as slider")]
        public ReadOnlyBind<Transform> slider;
        [Tooltip("The start point of this slider")]
        public ReadOnlyBind<Transform> startPoint;
        [Tooltip("The end point of this slider")]
        public ReadOnlyBind<Transform> endPoint;
        
        [Header("Output")]
        [Bind]
        [Range(0, 1)]
        [Tooltip("The desired value of the slider, from 0 to 1")]
        public Bind<float> value;
        [Tooltip("The force needed to reach the desired value")]
        public Bind<float> reachValueForce;

        private float _targetValue;
        private float _lastValue;

        protected override Transform GetCustomTarget() => slider;

        protected override void OnValidate()
        {
            if (!startPoint.Value || !endPoint.Value)
            {
                return;
            }
            
            base.OnValidate();
        }
        
        protected override void SetupJoint(ConfigurableJoint joint)
        {
            var localStartPoint = Target.InverseTransformPoint(startPoint.Value.position);
            var localEndPoint = Target.InverseTransformPoint(endPoint.Value.position);
            var fromStartToEnd = localEndPoint - localStartPoint;

            var axis = fromStartToEnd.normalized;

            joint.axis = axis;
            joint.anchor = localStartPoint;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = (endPoint.Value.position + startPoint.Value.position) / 2;
            joint.xMotion = ConfigurableJointMotion.Limited;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.linearLimit = new SoftJointLimit()
            {
                limit = fromStartToEnd.magnitude / 2
            };

            Target.localPosition = localStartPoint;
        }

        protected override void Start()
        {
            base.Start();
            Target.localPosition = Joint.axis * value.Value * Joint.linearLimit.limit * 2;
            RigidBody.MovePosition(Target.position);
        }

        private void FixedUpdate()
        {
            if (Time.fixedDeltaTime <= 0)
            {
                return;
            }

            var body = RigidBody;
            var joint = Joint;
            
            var localStartPoint = JointInverseTransformPoint(startPoint.Value.position);
            var localEndPoint = JointInverseTransformPoint(endPoint.Value.position);
            var localThisPoint = JointInverseTransformPoint(Target.position);
            var targetJustSet = false;
            
            if(!Mathf.Approximately(value.Value, _lastValue))
            {
                _targetValue = value.Value;
                targetJustSet = true;
            }
            
            var totalDistance = Vector3.Distance(localStartPoint, localEndPoint);
            var currentDistance = Vector3.Distance(localStartPoint, localThisPoint);
                
            var currentValue = currentDistance / totalDistance;
            if (value.CanWrite)
            {
                value.Value = Mathf.Clamp01(currentValue);
            }

            if (!targetJustSet && Mathf.Abs(value.Value - _targetValue) < 0.001f)
            {
                _targetValue = -1;
            }

            if (_targetValue >= 0)
            {
                var delta = _targetValue - currentValue;
                var deltaPosition = delta * totalDistance * Joint.axis;
                var deltaVelocity = deltaPosition / Time.fixedDeltaTime;
                var worldDeltaVelocity = JointTransformVector(deltaVelocity);
                if (!body.isKinematic)
                {
#if UNITY_6000_0_OR_NEWER
                    body.linearVelocity = worldDeltaVelocity;
#else
                    body.velocity = worldDeltaVelocity;
#endif
                    body.angularVelocity = Vector3.zero;
                }
            }
            else if (reachValueForce > 0 && !body.isKinematic)
            {
#if UNITY_6000_0_OR_NEWER
                body.linearVelocity = Vector3.zero;
#else
                body.velocity = Vector3.zero;
#endif
                body.angularVelocity = Vector3.zero;
                body.AddRelativeForce(-joint.axis * reachValueForce * currentValue);
            }
            
            _lastValue = value;
        }
    }
}
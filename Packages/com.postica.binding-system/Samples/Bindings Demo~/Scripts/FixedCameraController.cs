using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class FixedCameraController : MonoBehaviour
    {
        [Header("Setup")]
        public ReadOnlyBind<Transform> targetToFollow;
        public ReadOnlyBind<bool> followRotation = true.Bind();
        [Bind]
        [Range(0f, 10f)]
        public ReadOnlyBind<float> followStrength = 2f.Bind();

        [Header("Output")]
        [BindMode(BindMode.Write)]
        public Bind<Vector3> forwardAxisProjected;
        [BindMode(BindMode.Write)]
        public Bind<Vector3> rightAxisProjected;
        [BindMode(BindMode.Write)]
        public Bind<Vector3> upAxisProjected;

        private Vector3 _relativePosition;
        private Transform _preciseFollower;

        private void Start()
        {
            _relativePosition = transform.position - targetToFollow.Value.position;
            _preciseFollower = new GameObject("PreciseFollower").transform;
            _preciseFollower.SetParent(transform, false);
        }

        void Update()
        {
            var finalPosition = targetToFollow.Value.position + _relativePosition;
            var finalFollowStrength = followStrength.Value * Time.deltaTime;
            if (followRotation)
            {
                _preciseFollower.LookAt(targetToFollow);
                transform.rotation = Quaternion.Slerp(transform.rotation, _preciseFollower.rotation, finalFollowStrength);
            }

            transform.position = Vector3.Lerp(transform.position, finalPosition, finalFollowStrength);
            
            forwardAxisProjected.Value = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            rightAxisProjected.Value = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            upAxisProjected.Value = Vector3.up;
        }
    } 
}

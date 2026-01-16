using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Postica.BindingSystem.Samples
{

    /// <summary>
    /// An abstract behaviour with some utility functions.
    /// </summary>
    public abstract class PhysicsBehaviour<TJoint> : MonoBehaviour where TJoint : Joint
    {
        [Header("Setup")] 
        [Tooltip("Whether to setup the joint at edit time")]
        public bool editTimeSetup = true;

        [NonSerialized]
        protected Rigidbody _rb;

        [NonSerialized]
        protected TJoint _joint;

        public Transform Target => GetValue(GetCustomTarget(), transform);


        public Rigidbody RigidBody => GetValue(ref _rb);
        public TJoint Joint => GetValue(ref _joint);
        
        
        protected abstract void SetupJoint(TJoint joint);
        
        protected virtual Transform GetCustomTarget() => null;
        protected static T GetValue<T>(T value, T fallback) where T : Object
            => value ? value : fallback;
        protected T GetValue<T>(ref T value) where T : Component
        {
            if (!value)
            {
                value = Target.GetComponent<T>();
            }

            return value;
        }

        protected virtual void OnValidate()
        {
            if (editTimeSetup && !Application.isPlaying)
            {
                #if UNITY_EDITOR
                if (!_joint)
                {
                    UnityEditor.EditorApplication.delayCall += SetupJoint;
                }
                else
                #endif
                {
                    SetupJoint();
                }
            }
        }

        private void SetupJoint()
        {
            if (!this)
            {
                return;
            }
            
            if (!Joint || _joint.gameObject != gameObject)
            {
                _joint = gameObject.AddComponent<TJoint>();
            }

            var target = Target;
            var currentPosition = target.localPosition;
            var currentRotation = target.localRotation;
            SetupJoint(_joint);
            target.localPosition = currentPosition;
            target.localRotation = currentRotation;
        }
        
        protected Vector3 JointTransformPoint(Vector3 position)
        {
            var target = Target;
            return target.parent ? target.parent.TransformPoint(position) : position;
        }

        protected Vector3 JointInverseTransformPoint(Vector3 position)
        {
            var target = Target;
            return target.parent ? target.parent.InverseTransformPoint(position) : position;
        }
        
        protected Vector3 JointTransformVector(Vector3 vector)
        {
            var target = Target;
            return target.parent ? target.parent.TransformVector(vector) : vector;
        }
        
        protected Vector3 JointInverseTransformVector(Vector3 vector)
        {
            var target = Target;
            return target.parent ? target.parent.InverseTransformVector(vector) : vector;
        }
        
        protected Vector3 JointTransformDirection(Vector3 direction)
        {
            var target = Target;
            return target.parent ? target.parent.TransformDirection(direction) : direction;
        }
        
        protected Vector3 JointInverseTransformDirection(Vector3 direction)
        {
            var target = Target;
            return target.parent ? target.parent.InverseTransformDirection(direction) : direction;
        }

        protected virtual void Start()
        {
            if (!_joint || !editTimeSetup)
            {
                SetupJoint();
            }
        }
    }
}
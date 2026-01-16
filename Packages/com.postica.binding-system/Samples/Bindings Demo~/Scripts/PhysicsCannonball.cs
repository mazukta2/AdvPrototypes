using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class PhysicsCannonball : MonoBehaviour
    {
        [Header("Setup")]
        public ReadOnlyBind<Transform> gunTransform;
        public ReadOnlyBind<Transform> spawnPoint;
        
        [Header("Configuration")]
        public ReadOnlyBind<float> fireRate = 1f.Bind();
        public ReadOnlyBind<float> force = 1000f.Bind();
        public ReadOnlyBind<float> recoil = 100f.Bind();
        
        [Header("Input")] 
        public ReadOnlyBind<Rigidbody> ballToUse;
        public ReadOnlyBind<bool> fire;
        public ReadOnlyBind<Vector3> targetPoint;

        [Header("Audio")] 
        public AudioSource fireAudioSource;
        public AudioClip fireSound;

        private float _nextFireTime;
        private Rigidbody _rb;

        private void Start()
        {
            _rb = GetComponentInParent<Rigidbody>();
        }

        void Update()
        {
            gunTransform.Value.LookAt(targetPoint);
            TryFire();
        }

        private void TryFire()
        {
            if (!fire) return;

            if (Time.time < _nextFireTime) return;
            
            _nextFireTime = Time.time + 1 / fireRate.Value;
            Fire();
        }

        private void Fire()
        {
            var ball = Instantiate(ballToUse.Value, spawnPoint.Value.position, spawnPoint.Value.rotation);
            ball.gameObject.SetActive(true);
            var direction = spawnPoint.Value.forward;
            ball.AddForce(direction * force.Value);
            
            if (_rb)
            {
                _rb.AddForce(-direction * recoil.Value);
            }

            if (!fireAudioSource)
            {
                if (fireSound)
                {
                    AudioSource.PlayClipAtPoint(fireSound, spawnPoint.Value.position);
                }

                return;
            }

            if (!fireSound)
            {
                fireAudioSource.Play();
                return;
            }
            
            fireAudioSource.PlayOneShot(fireSound);
        }
    }
}

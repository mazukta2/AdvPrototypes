using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class GameManager : MonoBehaviour
    {
        [Header("Settings")]
        [Min(1)]
        public float physicsFrequencyHz = 50f;
        public int physicsIterations = 10;
        
        [Header("Inventory")]
        public Bind<GameObject> gun;
        public Bind<Rigidbody> ball;
        public Bind<float> fireRate;
        public Bind<float> force;
        public Bind<float> recoil;

        [Header("Player Related")]
        public Bind<GameObject> currentPlayer;
        public Bind<GameObject> currentGun;
        public Bind<Rigidbody> currentBall;
        
        public Bind<bool> torqueMotion;
        public Bind<float> maxForwardSpeed;
        public Bind<float> maxSidewaysSpeed;
        public Bind<float> maxJumpVelocity;
        public Bind<float> moveInAirFactor;
        
        [Header("Global Stats")]
        public Bind<Trigger> activeTrigger;
        public Bind<float> timeSinceStart;
        public Bind<int> ballsFired;
        
        private float _startTime;
        private GameObject _previousPlayer;
        private GameObject _previousGun;
        private Rigidbody _previousBall;

        private void Start()
        {
            _startTime = Time.time;
            Physics.defaultSolverIterations = physicsIterations;
            Physics.defaultSolverVelocityIterations = physicsIterations;
            Time.fixedDeltaTime = 1f / physicsFrequencyHz;
        }
        private void Update()
        {
            timeSinceStart.Value = Time.time - _startTime;
            UpdateChange(ref _previousPlayer, currentPlayer.Value);
            UpdateChange(ref _previousGun, currentGun.Value);
            UpdateChange(ref _previousBall, currentBall.Value);
        }

        private void UpdateChange<T>(ref T previous, T current) where T : Component
        {
            if (previous == current) return;
            
            if (previous)
            {
                previous.gameObject.SetActive(false);
            }
            previous = current;
        }
        
        private void UpdateChange(ref GameObject previous, GameObject current)
        {
            if (previous == current) return;
            
            if (previous)
            {
                previous.SetActive(false);
            }
            current.SetActive(true);
            previous = current;
        }
    } 
}

using System;
using System.Collections;
using System.Collections.Generic;
using Postica.BindingSystem.Modifiers;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class Trigger : MonoBehaviour
    {
        [Header("Setup")]
        public Bind<AreaTrigger> areaTrigger;
        public Bind<GameObject> triggerZone;
        public Bind<bool> isVisible;
        public Bind<Color> mainColor;
        [Bind]
        [ColorUsage(true, true)]
        public Bind<Color> emissionColor;
        
        [Header("Firing Related")]
        public Bind<GameObject> gun;
        public Bind<Rigidbody> ball;
        public Bind<float> fireRate;
        public Bind<float> force;
        public Bind<float> recoil;

        [Header("Player Related")]
        public Bind<bool> torqueMotion;
        public Bind<float> maxForwardSpeed;
        public Bind<float> maxSidewaysSpeed;
        public Bind<float> maxJumpVelocity;
        public Bind<float> moveInAirFactor;
        
        [Header("Material Config")]
#if UNITY_6000_0_OR_NEWER
        public Bind<PhysicsMaterial> physicMaterial;
#else
        public Bind<PhysicMaterial> physicMaterial;
#endif
        public Bind<float> dynamicFriction;
        public Bind<float> staticFriction;
        public Bind<float> bounciness;
        
        private Renderer[] _renderers;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            _renderers = triggerZone.Value.GetComponentsInChildren<Renderer>(true);
        }

        private void OnEnable()
        {
            // isVisible.Value = true;
            triggerZone.Value.SetActive(isVisible.Value);
        }
        
        private void OnDisable()
        {
            // isVisible.Value = false;
            triggerZone.Value.SetActive(isVisible.Value);
        }

        private void Update()
        {
            triggerZone.Value.SetActive(isVisible.Value);
            foreach (var r in _renderers)
            {
                r.material.color = mainColor.Value;
                if (r.material.HasProperty(EmissionColor))
                {
                    r.material.SetColor(EmissionColor, emissionColor.Value);
                }
            }
        }
    }
}

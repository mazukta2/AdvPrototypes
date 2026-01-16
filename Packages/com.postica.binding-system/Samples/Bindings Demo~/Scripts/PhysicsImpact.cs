using UnityEngine;
using UnityEngine.Serialization;

namespace Postica.BindingSystem.Samples
{
    /// <summary>
    /// This class plays an impact sound when the object collides with another object.
    /// </summary>
    public class PhysicsImpact : MonoBehaviour
    {
        [Header("Configuration")] 
        [Tooltip("The audio source to play when the object impacts. \nIf <b>null</b>, a default audio source will be used.")]
        public AudioSource audioSource;
        public AudioClip[] impactSounds;
        [Space]
        public float pitchVariance = 0.1f;
        public float volumeVariance = 0.1f;
        public float minImpactVelocity = 0.1f;
        
        private void OnCollisionEnter(Collision other)
        {
            var relativeVelocity = minImpactVelocity > 0 
                ? other.relativeVelocity.magnitude / minImpactVelocity
                : other.relativeVelocity.magnitude;
            if (relativeVelocity <= 0)
            {
                return;
            }
            
            if (!audioSource)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance * relativeVelocity);
            audioSource.volume = relativeVelocity * Random.Range(1f - volumeVariance, 1f + volumeVariance);
            audioSource.PlayOneShot(impactSounds[Random.Range(0, impactSounds.Length)]);
        }
    }
}

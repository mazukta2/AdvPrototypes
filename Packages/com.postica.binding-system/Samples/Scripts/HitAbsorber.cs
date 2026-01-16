using Postica.BindingSystem;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class HitAbsorber : MonoBehaviour
    {
        public ReadOnlyBind<float> factor = 1f.Bind();

        [Space]
		[Bind(BindMode.Write)]
		public Bind<float> onHit;

        private void OnCollisionStay(Collision collision)
        {
            onHit.Value = collision.impulse.magnitude * factor;
        }
    }
}

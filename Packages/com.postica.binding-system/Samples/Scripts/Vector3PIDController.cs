using Postica.BindingSystem;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class Vector3PIDController : MonoBehaviour
    {
		public ReadOnlyBind<Vector3> input = Vector3.zero.Bind();
		public ReadOnlyBind<Vector3> desiredOutput = Vector3.zero.Bind();
		[Bind(BindMode.Write)]
		public Bind<Vector3> output = Vector3.zero.Bind();
        public ReadOnlyBind<Vector3> clampValue = (ReadOnlyBind<Vector3>)Vector3.one;

		[Header("Constants")]
		[Tooltip("Proportional constant (counters current error)")]
		public ReadOnlyBind<Vector3> Kp = (ReadOnlyBind<Vector3>)(0.2f * Vector3.one);
		[Tooltip("Integral constant (counters cumulated error)")]
		public ReadOnlyBind<Vector3> Ki = (ReadOnlyBind<Vector3>)(0.05f * Vector3.one);
		[Tooltip("Derivative constant (fights oscillation)")]
		public ReadOnlyBind<Vector3> Kd = (ReadOnlyBind<Vector3>)(1f * Vector3.one);

        private Vector3 lastError;
        private Vector3 integral;

        private void OnValidate()
        {
            
        }

        private void Update()
        {
            output.Value += Compute(input, desiredOutput, clampValue, Time.deltaTime);
        }

        private Vector3 Compute(Vector3 input, Vector3 targetOutput, Vector3 clamp, float dt)
        {
            var error = targetOutput - input;
            Vector3 derivative = (error - lastError) / dt;
            integral += error * dt;
            lastError = error;
            var kp = Kp.Value;
            var ki = Ki.Value;
            var kd = Kd.Value;
            var result = new Vector3(Mathf.Clamp(kp.x * error.x + ki.x * integral.x + kd.x * derivative.x, -clamp.x, clamp.x),
                Mathf.Clamp(kp.y * error.y + ki.y * integral.y + kd.y * derivative.y, -clamp.y, clamp.y),
                Mathf.Clamp(kp.z * error.z + ki.z * integral.z + kd.z * derivative.z, -clamp.z, clamp.z));
            return result;
        }

    }
}

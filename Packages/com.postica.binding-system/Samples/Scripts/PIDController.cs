using Postica.BindingSystem;
using System;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class PIDController : MonoBehaviour
    {
		public ReadOnlyBind<float> input;
		public ReadOnlyBind<float> desiredOutput;
		[Bind(BindMode.Write)]
		public Bind<float> output;
        public ReadOnlyBind<float> clampValue = (ReadOnlyBind<float>)1;

		[Header("Constants")]
		[Tooltip("Proportional constant (counters current error)")]
		public ReadOnlyBind<float> Kp = 0.2f.Bind();
		[Tooltip("Integral constant (counters cumulated error)")]
		public ReadOnlyBind<float> Ki = 0.05f.Bind();
		[Tooltip("Derivative constant (fights oscillation)")]
		public ReadOnlyBind<float> Kd = 1f.Bind();

        private float lastError;
        private float integral;


        private float Compute(float input, float targetOutput, float clamp, float dt)
        {
            var error = targetOutput - input;
            float derivative = (error - lastError) / dt;
            integral += error * dt;
            lastError = error;
            var result = Mathf.Clamp(Kp * error + Ki * integral + Kd * derivative, -clamp, clamp);
            return result;
        }

        private void Update()
        {
            output.Value += Compute(input, desiredOutput, clampValue, Time.deltaTime);
        }
    }
}

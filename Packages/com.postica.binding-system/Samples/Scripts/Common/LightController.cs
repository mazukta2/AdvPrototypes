using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    /// <summary>
    /// This component controls basic properties of a light component.
    /// It serves as an example how to use Binding System with events only.
    /// </summary>
    public class LightController : MonoBehaviour
    {
        /// <summary>
        /// The light component to control. It is a readonly bind, meaning that its value cannot be written.
        /// Since it is a bind, it may have a direct value, or a bound value retrieved from somewhere else.
        /// </summary>
        public ReadOnlyBind<Light> targetLight;

        // Here below are the examples of some bound fields of different types, all of them readonly

        [Space]
        // The tooltips are forwarded to the correct drawer
        [Tooltip("Changes the intensity of the controlled light")]
        public ReadOnlyBind<float> intensity;
        public ReadOnlyBind<float> radius;
        // It is possible to initialize default values using Bind() method
        public ReadOnlyBind<Color> color = Color.red.Bind();

        private void Reset()
        {
            // Every value can be bound, the Bind() method transforms the value into a bind value
            targetLight = GetComponentInChildren<Light>().Bind();
        }

        private void OnEnable()
        {
            // The ValueChanged event is useful to intercept when there are changes to the specified bind fields,
            // if there are any. Please note that this event is evaluated before the update and immediately after
            // script update methods

            intensity.ValueChanged -= Intensity_ValueChanged;
            intensity.ValueChanged += Intensity_ValueChanged;

            radius.ValueChanged -= Radius_ValueChanged;
            radius.ValueChanged += Radius_ValueChanged;

            color.ValueChanged -= Color_ValueChanged;
            color.ValueChanged += Color_ValueChanged;
        }

        private void OnDisable()
        {
            intensity.ValueChanged -= Intensity_ValueChanged;
            radius.ValueChanged -= Radius_ValueChanged;
            color.ValueChanged -= Color_ValueChanged;
        }

        private void Color_ValueChanged(Color oldValue, Color newValue)
        {
            if(targetLight.Value == null)
            {
                // Typically the NullReferenceException may be a bit cryptic in the console,
                // it is better to write a shorter message with context
                Debug.LogError("The target light is not set", this);
                return;
            }

            targetLight.Value.color = newValue;
        }

        private void Radius_ValueChanged(float oldValue, float newValue)
        {
            if (targetLight.Value == null)
            {
                Debug.LogError("The target light is not set", this);
                return;
            }

            targetLight.Value.spotAngle = newValue;
        }

        private void Intensity_ValueChanged(float oldValue, float newValue)
        {
            if (targetLight.Value == null)
            {
                Debug.LogError("The target light is not set", this);
                return;
            }

            targetLight.Value.intensity = newValue;
        }
    }
}

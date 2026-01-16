using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class TextFormatter : MonoBehaviour
    {
        [SerializeField]
        private ReadOnlyBind<string> _input;
        [SerializeField]
        private string _format = "{0}";

        [Space]
        [SerializeField]
        [Bind(BindMode.Write)]
        private Bind<string>[] _outputs;

        public string Input { get => _input; }

        private void Start()
        {
            _input.ValueChanged += Input_ValueChanged;
            var renderer = GetComponent<Renderer>();
            if (renderer)
            {
                Debug.Log(renderer.material.color);
            }
        }

        private void Input_ValueChanged(string oldValue, string newValue)
        {
            var value = string.IsNullOrEmpty(_format) ? newValue : string.Format(_format, newValue);
            foreach(var output in _outputs)
            {
                output.Value = value;
            }
        }
    }
}

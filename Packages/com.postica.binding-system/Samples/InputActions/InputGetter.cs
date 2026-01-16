using System;
using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class InputGetter : MonoBehaviour
    {
#if BS_INPUT_SYSTEM
        public UnityEngine.InputSystem.InputActionAsset input;
#endif
        public Retriever<bool> Boolean { get; } = new();
        public Retriever<float> Float { get; } = new();
        public Retriever<Vector2> Vector2 { get; } = new();
        public Retriever<Vector3> Vector3 { get; } = new();
        public Retriever<Vector4> Vector4 { get; } = new();

        private void OnValidate()
        {
            Awake();
        }

        private void Awake()
        {
            Boolean.inputGetter = this;
            Float.inputGetter = this;
            Vector2.inputGetter = this;
            Vector3.inputGetter = this;
            Vector4.inputGetter = this;
        }

        public class Retriever<T> where T : struct
        {
            internal InputGetter inputGetter;
            
            public T this[string actionName]
            {
                get
                {
#if BS_INPUT_SYSTEM
                    var action = inputGetter.input?.FindAction(actionName);
                    if (action != null)
                    {
                        return action.ReadValue<T>();
                    }
                    throw new Exception($"Action '{actionName}' not found in InputActionAsset.");
#else
                    return actionName.ToLower() switch
                    {
                        "mouseposition" => Input.mousePosition is T value ? value : throw new Exception($"Unable to convert {actionName} to {typeof(T).Name}"),
                        "mousefire" => Input.GetMouseButton(0) is T value ? value : throw new Exception($"Unable to convert {actionName} to {typeof(T).Name}"),
                        "forward" => Input.GetKey(KeyCode.W) is T value ? value : throw new Exception($"Unable to convert {actionName} to {typeof(T).Name}"),
                        "backward" => Input.GetKey(KeyCode.S) is T value ? value : throw new Exception($"Unable to convert {actionName} to {typeof(T).Name}"),
                        "left" => Input.GetKey(KeyCode.A) is T value ? value : throw new Exception($"Unable to convert {actionName} to {typeof(T).Name}"),
                        "right" => Input.GetKey(KeyCode.D) is T value ? value : throw new Exception($"Unable to convert {actionName} to {typeof(T).Name}"),
                        "jump" => Input.GetKey(KeyCode.Space) is T value ? value : throw new Exception($"Unable to convert {actionName} to {typeof(T).Name}"),
                        _ => throw new Exception($"Action '{actionName}' not found. Please enable the Input System package to use custom actions."),
                    };
#endif
                }
            }
        }
    }
}

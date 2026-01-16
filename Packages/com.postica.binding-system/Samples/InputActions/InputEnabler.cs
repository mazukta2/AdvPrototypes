using UnityEngine;

namespace Postica.BindingSystem.Samples
{
    public class InputEnabler : MonoBehaviour
    {
        #if BS_INPUT_SYSTEM
        public UnityEngine.InputSystem.InputActionAsset input;

        [Space] 
        public bool debug;
        
        // Start is called before the first frame update
        void Start()
        {
            input.Enable();
            if (debug)
            {
                foreach (var map in input.actionMaps)
                {
                    map.actionTriggered += ctx =>
                    {
                        Debug.Log($"Action {ctx.action.name} triggered at {ctx.time} with value {ctx.ReadValueAsObject()}");
                    };
                }
            }
        }
        #endif
    }
}

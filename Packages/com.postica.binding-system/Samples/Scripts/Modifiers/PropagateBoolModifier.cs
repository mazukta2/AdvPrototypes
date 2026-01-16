using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This modifier will propagate the input bool value to multiple other properties. <br/>
    /// </summary>
    public class PropagateBoolModifier : IReadWriteModifier<bool>
    {
        [SerializeField]
        private BindMode _mode;

        [WriteOnlyBind]
        public Bind<bool>[] targets = new Bind<bool>[0];

        public bool ModifyRead(in bool value)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].Value = value;
            }
            return value;
        }

        public bool ModifyWrite(in bool value) => ModifyRead(value);

        public object Modify(BindMode mode, object value) => ModifyRead((bool)value);

        public string ShortDataDescription => targets.Length == 1 ? "to another target" : $"to other {targets.Length} targets";

        public string Id => "Propagate Boolean";

        public BindMode ModifyMode { get => _mode; set => _mode = value; }
    } 
}

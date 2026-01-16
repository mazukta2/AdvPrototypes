namespace Postica.BindingSystem
{
    /// <summary>
    /// This modifier will propagate the input value to multiple other properties. <br/>
    /// It extends <see cref="NumericModifier"/> for convenience when working with numeric values. <br/>
    /// </summary>
    public class PropagateModifier : NumericModifier, IReadWriteModifier<bool>
    {
        [WriteOnlyBind]
        public Bind<double>[] targets = new Bind<double>[0];

        protected override double Modify(double value)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].Value = value;
            }
            return value;
        }

        public bool ModifyRead(in bool value)
        {
            if (!value)
            {
                return value;
            }
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].Value = 0;
            }
            return value;
        }

        public bool ModifyWrite(in bool value) => ModifyRead(value);

        public override string ShortDataDescription => targets.Length == 1 ? "to another target" : $"to other {targets.Length} targets";
    } 
}

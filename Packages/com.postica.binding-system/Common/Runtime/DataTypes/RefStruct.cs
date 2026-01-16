namespace Postica.Common
{
    /// <summary>
    /// Wrapper class which keeps an internal struct value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class Ref<T> where T : struct
    {
        private T _value;
        private bool _valueSet;

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                _valueSet = true;
            }
        }
        
        public bool IsSet => _valueSet;
        
        public void Reset()
        {
            _value = default;
            _valueSet = false;
        }
        
        public Ref()
        {
            Value = default;
        }
        
        public Ref(T value)
        {
            Value = value;
        }
        
        public static implicit operator T(Ref<T> reference)
        {
            return reference.Value;
        }
        
        public static implicit operator Ref<T>(T value)
        {
            return new Ref<T>(value);
        }
    }
}

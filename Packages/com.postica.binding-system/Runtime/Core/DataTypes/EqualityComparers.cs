using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Postica.BindingSystem
{
    [Serializable]
    public sealed class ValueProviderEqualityComparer<S, T> : EqualityComparer<S>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(S x, S y)
        {
            return EqualityComparer<T>.Default.Equals(((IValueProvider<T>)x).Value, ((IValueProvider<T>)y).Value);
        }

        public override int GetHashCode(S obj) => ((IValueProvider<T>)obj).Value.GetHashCode();
    }
}

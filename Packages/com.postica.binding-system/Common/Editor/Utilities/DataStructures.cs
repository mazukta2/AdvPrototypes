using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Postica.Common
{
    public struct TemporaryData : IDisposable
    {
        private readonly Action _reset;

        public TemporaryData(Action reset) => _reset = reset;

        public void Dispose() => _reset?.Invoke();
    }
}
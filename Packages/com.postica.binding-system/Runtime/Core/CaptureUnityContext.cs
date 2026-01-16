using System;
using System.Threading;
using UnityEngine;

namespace Postica.BindingSystem
{
    /// <summary>
    /// This component captures the current Unity SynchronizationContext
    /// </summary>
    internal class CaptureUnityContext : MonoBehaviour
    {
        public static Action<SynchronizationContext> SynchronizationContextCaptured;

        private void Awake()
        {
            SynchronizationContextCaptured?.Invoke(SynchronizationContext.Current);
            SynchronizationContextCaptured = null; // Clear the event to prevent memory leaks
            Destroy(gameObject);
        }
        
        public static void BeginCapture(Action<SynchronizationContext> onCaptured)
        {
            if (onCaptured == null) return;

            SynchronizationContextCaptured += onCaptured;
            var go = new GameObject("CaptureUnityContext")
            {
                hideFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable
            };
            go.AddComponent<CaptureUnityContext>();
        }
    }
}

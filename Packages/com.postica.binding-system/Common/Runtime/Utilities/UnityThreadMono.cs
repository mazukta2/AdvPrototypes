using UnityEngine;

namespace Postica.Common
{
    /// <summary>
    /// This class is used to spin the UnityThread during Runtime.
    /// </summary>
    [AddComponentMenu("")]
    internal class UnityThreadMono : MonoBehaviour
    {
        private void Update() => UnityThread.SpinOnce();
    }
}
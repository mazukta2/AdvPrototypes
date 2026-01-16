
using UnityEngine;

namespace Postica.BindingSystem.Accessors
{
    /// <summary>
    /// This class contains accessors for the GameObject class.
    /// </summary>
    internal static class GameObjectAccessors
    {
        [RegistersCustomAccessors]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void RegisterAccessors()
        {
            AccessorsFactory.RegisterAccessors(
                (typeof(GameObject), nameof(GameObject.activeSelf),
                    new ObjectTypeAccessor<GameObject, bool>(g => g.activeSelf, (g, v) => g.SetActive(v))),
                (typeof(GameObject), "m_IsActive",
                    new ObjectTypeAccessor<GameObject, bool>(g => g.activeSelf, (g, v) => g.SetActive(v)))
            );
        }
    }
}

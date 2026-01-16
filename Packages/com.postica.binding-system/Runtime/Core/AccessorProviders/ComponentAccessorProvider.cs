using UnityEngine;

namespace Postica.BindingSystem.Accessors
{
    /// <summary>
    /// This class serves as a starting point to implement custom <see cref="IComponentAccessorProvider"/> classes
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Component"/> to handle</typeparam>
    public abstract class ComponentAccessorProvider<T> : BaseAccessorProvider<T>, IComponentAccessorProvider where T : Component
    {
        /// <inheritdoc/>
        public virtual Component GetComponent(GameObject gameObject)
        {
            return gameObject.GetComponent<T>();
        }

        protected sealed override bool TryGetPreciseObject(object source, out T component)
        {
            component = source as T;
            if (!component)
            {
                if (source is GameObject go)
                {
                    component = go.GetComponent<T>();
                }
                else if (source is Component c)
                {
                    component = c.GetComponent<T>();
                }
            }

            return component != null;
        }
    }
}

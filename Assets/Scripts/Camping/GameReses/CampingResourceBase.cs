using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Camping
{
    public abstract class CampingResourceBase : MonoBehaviour
    {
        public abstract string GetName();
        public abstract void TakeResource();
        public abstract float GetProgressModificator();
        public virtual bool CanTakeResource() => true;
    }
}
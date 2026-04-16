using System.Linq;
using Deckbuilding.Interactables;
using UnityEngine;

namespace Deckbuilding.InteractionRules
{
    /*[CreateAssetMenu(fileName = "InteractionRule", menuName = "ScriptableObjects/InteractionRule", order = 1)]
    public class InteractionRule : ScriptableObject
    {
        [Header("Conditions")]
        public PartyMemberClass[] RequiredClasses;
        public BuildingTypes[] RequiredBuilding;
        
        [Header("Actions")]
        [SerializeReference] public IRuleAction Action;

        public bool Match(PartyMember member, Interactable interactable)
        {
            if (RequiredClasses != null && RequiredClasses.All(m => member.Class != m))
                return false;
            if (RequiredBuilding != null && RequiredBuilding.All(r => r !=  interactable.GetBuidingType()))
            {
                return false;
            }

            return true;
        }
    }*/
}
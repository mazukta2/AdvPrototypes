using Deckbuilding.PartyGameResources;

namespace Deckbuilding.Buildings.Requirements
{
    public class HasPartyResource : IRequirement
    {
        public PartyResourceData Resource;
        public int Value;
        public string Description;

        public bool Check()
        {
            var resource = PartyResources.Instance.Get(Resource).Get();
            return resource >= Value;
        }

        public string GetDesc()
        {
            return Description;
        }
    }
}
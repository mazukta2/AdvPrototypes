namespace Deckbuilding
{
    public class PartyMember
    {
        public PartyMemberClass Class{ get; set; }
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public bool IsDead => CurrentHealth <= 0;
        public int Charge { get; set; }
    }
}
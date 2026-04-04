using System;
using Common;
using Deckbuilding.Interactables;

namespace Deckbuilding
{
    public class PartySeasons  : SingletonMonoBehavior<PartySeasons>
    {
        
        private bool _requestEndSeason;

        public void Update()
        {
            if ((_requestEndSeason || PartyHealth.IsDead()) && 
                !ChangePartyScreen.Instance.IsOpened() &&
                !ChangeQuestScreen.Instance.IsOpened())
            {
                _requestEndSeason = false;
                
                PartyResources.Instance.Change(PartyResources.ResourceType.Gold, PartyResources.Instance.Get(PartyResources.ResourceType.Fuel));
                PartyResources.Instance.Set(PartyResources.ResourceType.Fuel, 0);
                
                PartyMembers.Instance.Clear();
                PartyMovement.NewSeason();
                Enemy.ResetEnemies();
                Bullet.DestroyAll();
                Lighthouse.NewSeason();
                Zone.NewSeason();
                Gates.NewSeason();
                Sawmill.NewSeason();
                PartyQuests.NewSeason();
                
                ChangeQuestScreen.Instance.Open();
            }
        }
        
        public void EndSeason()
        {
            _requestEndSeason = true;
        }

    }
}
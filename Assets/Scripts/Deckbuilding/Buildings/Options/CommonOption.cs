using System.Linq;
using Deckbuilding.Heroes.Skills;
using Deckbuilding.Windows;
using UnityEngine;

namespace Deckbuilding.Buildings.Options
{
    public class CommonOption: IBuildingOption
    {
        public string OptionName;
        [Multiline]public string OptionDesc;
        public bool CloseWindow;

        public SkillData SkillCheck;
        public int SkillValue = 10;
        
        [SerializeReference] public IBuildingAction[] Success;
        [SerializeReference] public IBuildingAction[] Failure;
        
        public string GetName(BuildingWindowContext context)
        {
            return OptionName;
        }

        public string GetDescription(BuildingWindowContext context, PartyMember partyMember)
        {
            var skillCheck = "";
            if (SkillCheck != null)
            {
                var skill = Rolls.GetSkill(partyMember, SkillCheck);
                skillCheck += SkillCheck.Name + ": " + skill + " против " + SkillValue + "\r\n";
                skillCheck += "Вероятность успеха: " + 
                              (Rolls.GetChances(skill, SkillValue)*100) + "%\r\n\r\n";
            }

            return skillCheck + string.Format(OptionDesc, Success.SelectMany(a => a.GetParameters()).ToArray());
        }

        public string GetDescription(BuildingWindowContext context)
        {
            var skillCheck = "";
            if (SkillCheck != null)
            {
                skillCheck += SkillCheck.Name + ": " + SkillValue + "\r\n\r\n";
            }
            
            return skillCheck + string.Format(OptionDesc, Success.SelectMany(a => a.GetParameters()).ToArray());
        }

        public void Click(BuildingWindowContext context, PartyMember partyMember)
        {
            if (SkillCheck == null)
            {
                foreach (var action in Success)
                {
                    action.Execute(context.Building);
                }
            }
            else
            {
                if (Rolls.Roll(partyMember, SkillCheck, SkillValue))
                {
                    foreach (var action in Success)
                    {
                        action.Execute(context.Building);
                    }
                }
                else
                {
                    foreach (var action in Failure)
                    {
                        action.Execute(context.Building);
                    }
                }
            }
            
            if (CloseWindow)
                context.Window.Close();
        }

        public bool HasSelector()
        {
            return SkillCheck !=null;
        }
    }
}
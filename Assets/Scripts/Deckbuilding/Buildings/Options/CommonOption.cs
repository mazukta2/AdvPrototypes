using System.Linq;
using Deckbuilding.Buildings.Requirements;
using Deckbuilding.Heroes.Skills;
using Deckbuilding.Windows;
using Sirenix.Utilities;
using Unity.VisualScripting;
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
        
        [SerializeReference] public IRequirement[] Requirements;

        public int Cost;
        
        [SerializeReference] public IBuildingAction[] Success;
        [SerializeReference] public IBuildingAction[] Failure;
        
        public string GetName(BuildingWindowContext context)
        {
            return OptionName + (Cost>0 ? " - " +Cost.ToString(): "");
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
            
            var desc = skillCheck + string.Format(OptionDesc, Success.SelectMany(a => a.GetParameters()).ToArray());

            if (Requirements != null && Requirements.Length > 0)
            {
                desc += "\r\n\r\n";
                desc += string.Join("\r\n", Requirements.Select(r => GetRequiromentText(r, context)));
            }
            return desc;
        }

        public void Click(BuildingWindowContext context, PartyMember partyMember)
        {
            if (partyMember != null)
                partyMember.Charge -= Cost;
            
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

        public bool CanSelect(BuildingWindowContext context, PartyMember partyMember)
        {
            if (partyMember == null)
                return true;
            
            if (partyMember.Charge < Cost)
                return false;

            if (Requirements != null)
            {
                foreach (var requirement in Requirements)
                {
                    if (!requirement.Check())
                        return false;
                }
            }
            
            return true;
        }

        public bool CanSelect(BuildingWindowContext context)
        {
            if (Requirements != null)
            {
                foreach (var requirement in Requirements)
                {
                    if (!requirement.Check())
                        return false;
                }
            }
            
            return true;
        }

        private string GetRequiromentText(IRequirement requirement, BuildingWindowContext context)
        {
            var color = Color.red;
            if (requirement.Check())
            {
                color = Color.green;
            }
            
            return string.Format("<color=#{0}>* {1}</color>", ColorToHex(color), requirement.GetDesc());
        }
        
        private string ColorToHex(Color color)
        {
            Color32 color32 = color;
            return $"{color32.r:X2}{color32.g:X2}{color32.b:X2}";
        }
    }
}
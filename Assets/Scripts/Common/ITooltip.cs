using System.Collections;
using Deckbuilding.Buildings;

namespace Common
{
    public interface ITooltip
    {
        string GetName();
        string GetDescription();
        Building GetBuilding();
    }
}
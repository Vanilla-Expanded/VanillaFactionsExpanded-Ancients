using RimWorld;
using Verse;

namespace VFEAncients
{
    public class Building_VaultDoor : Building_Door
    {
        public override bool PawnCanOpen(Pawn p) => base.PawnCanOpen(p) && GetComp<CompPowerTrader>().PowerOn;
    }
}
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEAncients
{
    public class JobDriver_SitFacingBuilding_Learning : JobDriver_SitFacingBuilding
    {
        public override void ModifyPlayToil(Toil toil)
        {
            base.ModifyPlayToil(toil);
            toil.AddPreTickAction(() => pawn.skills.Learn(pawn.skills.skills.Select(sk => sk.def).RandomElement(), 0.1f));
        }
    }
}
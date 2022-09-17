using RimWorld;
using Verse;

namespace VFEAncients
{
    public class IncidentWorker_SupplyDrop : IncidentWorker
    {
        public override bool CanFireNowSub(IncidentParms parms) => base.CanFireNowSub(parms) && parms.target is Map;

        public override bool TryExecuteWorker(IncidentParms parms)
        {
            var crate = (Building_Crate) ThingMaker.MakeThing(VFEA_DefOf.VFEA_AncientSupplyCrate);
            foreach (var thing in VFEA_DefOf.VFEA_Contents_SuuplyDrop.root.Generate()) crate.TryAcceptThing(thing, false);
            if (!(parms.target is Map map)) return false;
            var skyfaller = SkyfallerMaker.SpawnSkyfaller(VFEA_DefOf.VFEA_SupplyCrateIncoming, crate, DropCellFinder.RandomDropSpot(map), map);
            Find.LetterStack.ReceiveLetter(def.letterLabel, def.letterText, def.letterDef, skyfaller, Faction.OfAncients);
            return true;
        }
    }
}
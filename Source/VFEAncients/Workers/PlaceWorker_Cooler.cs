using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients
{
    public class PlaceWorker_Cooler : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var currentMap = Find.CurrentMap;
            var pos = center + (IntVec3.North * 2).RotatedBy(rot);
            GenDraw.DrawFieldEdges(new List<IntVec3>
            {
                pos
            }, GenTemperature.ColorSpotCold);
            var room = pos.GetRoom(currentMap);
            if (room != null && !room.UsesOutdoorTemperature) GenDraw.DrawFieldEdges(room.Cells.ToList(), GenTemperature.ColorRoomCold);
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            var c = center + (IntVec3.North * 2).RotatedBy(rot);
            if (c.Impassable(map)) return "MustPlaceCoolerWithFreeSpaces".Translate();
            var frame = c.GetFirstThing<Frame>(map);
            if (frame?.def.entityDefToBuild != null && frame.def.entityDefToBuild.passability == Traversability.Impassable) return "MustPlaceCoolerWithFreeSpaces".Translate();
            var blueprint = c.GetFirstThing<Blueprint>(map);
            if (blueprint?.def.entityDefToBuild != null && blueprint.def.entityDefToBuild.passability == Traversability.Impassable)
                return "MustPlaceCoolerWithFreeSpaces".Translate();
            return true;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VFEAncients
{
	public class ScenPart_ReconnectAllPowerBuildings : ScenPart
	{
        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            map.powerNetManager.UpdatePowerNetsAndConnections_First();
            UpdateDesiredPowerOutputForAllGenerators(map);
			EnsureTransmittersConnected(map);
			EnsurePowerUsersConnected(map);
			map.powerNetManager.UpdatePowerNetsAndConnections_First();
		}

		private List<Thing> tmpThings = new List<Thing>();
		private void UpdateDesiredPowerOutputForAllGenerators(Map map)
		{
			tmpThings.Clear();
			tmpThings.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));
			for (int i = 0; i < tmpThings.Count; i++)
			{
				if (IsPowerGenerator(tmpThings[i]))
				{
					tmpThings[i].TryGetComp<CompPowerPlant>()?.UpdateDesiredPowerOutput();
				}
			}
		}

		private void EnsureTransmittersConnected(Map map)
        {
			tmpThings.Clear();
			tmpThings.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));
			for (int i = 0; i < tmpThings.Count; i++)
            {
				var transmitter = tmpThings[i].TryGetComp<CompPowerTransmitter>();
				if (transmitter != null && transmitter.PowerNet is null)
                {
					if (TryFindClosestReachableNet(transmitter.parent.Position, (PowerNet x) => HasAnyPowerGenerator(x), map, out var foundNet, out var closestTransmitter))
                    {
						transmitter.transNet = foundNet;
						transmitter.TryManualReconnect();
					}
				}
			}
		}
		private bool HasAnyPowerGenerator(PowerNet net)
		{
			List<CompPowerTrader> powerComps = net.powerComps;
			for (int i = 0; i < powerComps.Count; i++)
			{
				if (IsPowerGenerator(powerComps[i].parent))
				{
					return true;
				}
			}
			return false;
		}
		private void EnsurePowerUsersConnected(Map map)
		{
			tmpThings.Clear();
			tmpThings.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial));
			for (int i = 0; i < tmpThings.Count; i++)
			{
				if (!IsPowerUser(tmpThings[i]))
				{
					continue;
				}
				CompPowerTrader powerComp = tmpThings[i].TryGetComp<CompPowerTrader>();
				PowerNet powerNet = powerComp.PowerNet;
				if (powerNet != null && powerNet.hasPowerSource)
				{
					TryTurnOnImmediately(powerComp, map);
					continue;
				}
				map.powerNetManager.UpdatePowerNetsAndConnections_First();
				TryTurnOnImmediately(powerComp, map);
			}
		}

		private bool IsPowerUser(Thing thing)
		{
			CompPowerTrader compPowerTrader = thing.TryGetComp<CompPowerTrader>();
			if (compPowerTrader != null)
			{
				if (!(compPowerTrader.PowerOutput < 0f))
				{
					if (!compPowerTrader.PowerOn)
					{
						return compPowerTrader.Props.basePowerConsumption > 0f;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		private bool IsPowerGenerator(Thing thing)
		{
			if (thing.TryGetComp<CompPowerPlant>() != null)
			{
				return true;
			}
			CompPowerTrader compPowerTrader = thing.TryGetComp<CompPowerTrader>();
			if (compPowerTrader != null)
			{
				if (!(compPowerTrader.PowerOutput > 0f))
				{
					if (!compPowerTrader.PowerOn)
					{
						return compPowerTrader.Props.basePowerConsumption < 0f;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		private Dictionary<PowerNet, bool> tmpPowerNetPredicateResults = new Dictionary<PowerNet, bool>();
		private bool TryFindClosestReachableNet(IntVec3 root, Predicate<PowerNet> predicate, Map map, out PowerNet foundNet, out IntVec3 closestTransmitter)
		{
			tmpPowerNetPredicateResults.Clear();
			PowerNet foundNetLocal = null;
			IntVec3 closestTransmitterLocal = IntVec3.Invalid;
			map.floodFiller.FloodFill(root, (IntVec3 x) => EverPossibleToTransmitPowerAt(x, map), delegate (IntVec3 x)
			{
				PowerNet powerNet = x.GetTransmitter(map)?.GetComp<CompPower>().PowerNet;
				if (powerNet == null)
				{
					return false;
				}
				if (!tmpPowerNetPredicateResults.TryGetValue(powerNet, out var value))
				{
					value = predicate(powerNet);
					tmpPowerNetPredicateResults.Add(powerNet, value);
				}
				if (value)
				{
					foundNetLocal = powerNet;
					closestTransmitterLocal = x;
					return true;
				}
				return false;
			}, int.MaxValue, rememberParents: true);
			tmpPowerNetPredicateResults.Clear();
			if (foundNetLocal != null)
			{
				foundNet = foundNetLocal;
				closestTransmitter = closestTransmitterLocal;
				return true;
			}
			foundNet = null;
			closestTransmitter = IntVec3.Invalid;
			return false;
		}
		private bool EverPossibleToTransmitPowerAt(IntVec3 c, Map map)
		{
			if (c.GetTransmitter(map) == null)
			{
				return GenConstruct.CanBuildOnTerrain(ThingDefOf.PowerConduit, c, map, Rot4.North);
			}
			return true;
		}
		private void TryTurnOnImmediately(CompPowerTrader powerComp, Map map)
		{
			if (!powerComp.PowerOn)
			{
				map.powerNetManager.UpdatePowerNetsAndConnections_First();
				if (powerComp.PowerNet != null)
				{
					var flickComp = powerComp.parent.TryGetComp<CompFlickable>();
					var compSchedule = powerComp.parent.TryGetComp<CompSchedule>();
					if (compSchedule != null)
                    {
						compSchedule.intAllowed = true;
					}

					if (flickComp != null && !flickComp.SwitchIsOn)
                    {
						flickComp.SwitchIsOn = true;
                    }



					powerComp.PowerOn = true;
					if (compSchedule != null)
                    {
						compSchedule.RecalculateAllowed();
					}
				}
				else
                {
					Log.Message("Can't enable " + powerComp);
                }
			}
		}
	}
}
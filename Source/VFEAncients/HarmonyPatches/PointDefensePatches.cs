using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients.HarmonyPatches;

public class PointDefensePatches
{
    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(typeof(Building_TurretGun), "TryStartShootSomething"),
            new HarmonyMethod(typeof(PointDefensePatches), nameof(TryShootProjectile)));
        harm.Patch(AccessTools.Method(typeof(Projectile), nameof(Projectile.Launch),
            new[]
            {
                typeof(Thing), typeof(Vector3), typeof(LocalTargetInfo), typeof(LocalTargetInfo), typeof(ProjectileHitFlags), typeof(bool), typeof(Thing),
                typeof(ThingDef)
            }), new HarmonyMethod(typeof(PointDefensePatches), nameof(PreLaunch)));
        harm.Patch(AccessTools.Method(typeof(Projectile), "ImpactSomething"), new HarmonyMethod(typeof(PointDefensePatches), nameof(PreImpactSomething)));
        harm.Patch(AccessTools.PropertyGetter(typeof(Building_TurretGun), nameof(Building_TurretGun.AttackVerb)),
            new HarmonyMethod(typeof(PointDefensePatches), nameof(OverrideAttackVerb)));
    }

    public static bool OverrideAttackVerb(Building_TurretGun __instance, ref Verb __result)
    {
        if (__instance.CurrentTarget.Thing is Projectile_Explosive or DropPodIncoming)
        {
            __result = __instance.GunCompEq.AllVerbs.First(v => v.verbProps.label == "point-defense");
            return false;
        }

        return true;
    }

    private static bool TryFindPDTarget(Building_TurretPD searcher, out LocalTargetInfo target)
    {
        var range = searcher.AttackVerb.verbProps.range;
        target = LocalTargetInfo.Invalid;
        if (searcher.Opts.AtProjectiles)
            target = searcher.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile)
                .Where(t => t is Projectile_Explosive pe && pe.Launcher.HostileTo(searcher) && t.Position.InHorDistOf(searcher.Position, range))
                .OrderByDescending(t => t.Position.DistanceTo(searcher.Position))
                .FirstOrDefault();

        if (target.IsValid) return true;
        if (searcher.Opts.AtPods)
            target = searcher.Map.listerThings.ThingsInGroup(ThingRequestGroup.ActiveDropPod)
                .Where(t => t is DropPodIncoming pod && (pod.Contents.innerContainer.OfType<Pawn>().FirstOrDefault()?.HostileTo(searcher) ?? false) &&
                            Mathf.Abs((t.DrawPos - searcher.DrawPos).magnitude) <= range)
                .OrderByDescending(t => Mathf.Abs((t.DrawPos - searcher.DrawPos).magnitude))
                .FirstOrDefault();
        return target.IsValid;
    }

    public static bool TryShootProjectile(Building_TurretGun __instance, ref LocalTargetInfo ___currentTargetInt, ref int ___burstWarmupTicksLeft)
    {
        if (__instance is not Building_TurretPD pd) return true;

        if (!TryFindPDTarget(pd, out var target)) return pd.Opts.AtPawns;

        ___currentTargetInt = target;
        ___burstWarmupTicksLeft = __instance.def.building.turretBurstWarmupTime.RandomInRange.SecondsToTicks();

        return false;
    }

    public static void PreLaunch(ref LocalTargetInfo usedTarget, LocalTargetInfo intendedTarget)
    {
        usedTarget = intendedTarget.Thing switch
        {
            Projectile_Explosive proj when Rand.Chance(0.75f) && proj.Spawned => proj,
            DropPodIncoming { Spawned: true } pod => pod,
            _ => usedTarget
        };
    }

    public static bool PreImpactSomething(Projectile __instance)
    {
        var flag = false;
        switch (__instance.usedTarget.Thing)
        {
            case Projectile_Explosive { Spawned: true } proj:
                proj.Destroy();
                break;
            case DropPodIncoming { Spawned: true } pod:
                pod.Destroy();
                break;
            default:
                flag = true;
                break;
        }

        if (!flag)
        {
            GenClamor.DoClamor(__instance, 12f, ClamorDefOf.Impact);
            __instance.Destroy();
        }

        return flag;
    }
}
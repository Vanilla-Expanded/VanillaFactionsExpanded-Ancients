using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using AbilityDef = VFECore.Abilities.AbilityDef;

namespace VFEAncients;

public class Ability_ElectricBlast : Ability
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);
        for (var i = 0; i < targets.Length; i++)
        {
            var blast = (ElectricBlast)GenSpawn.Spawn(VFEA_DefOf.VFEA_ElectricBlast, targets[i].Cell, targets[i].Map);
            blast.Target = pawn;
            blast.Props = def.GetModExtension<AbilityExtension_ElectricityBlast>();
            blast.Caster = pawn;
            blast.Ability = def;
            blast.FireAt(targets[i].Thing);
        }
    }
}

public class AbilityExtension_ElectricityBlast : DefModExtension
{
    public bool addFire;
    public bool allowRepeat;
    public int bounceCount;
    public int bounceDelay;
    public BouncePriority bouncePriority;
    public DamageDef damageDef;
    public DamageDef explosionDamageDef;
    public float impactRadius;
    public bool targetFriendly;
}

[StaticConstructorOnStartup]
public class ElectricBlast : Thing
{
    public AbilityDef Ability;
    public Thing Caster;
    public Thing Holder;

    public AbilityExtension_ElectricityBlast Props;

    public Thing Target;

    private int numBounces;
    private List<Thing> prevTargets = new();
    private int ticksTillBounce;

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        var vec1 = Holder.DrawPos;
        var vec2 = Target.DrawPos;
        if (vec2.magnitude > vec1.magnitude) (vec1, vec2) = (vec2, vec1);

        Graphics.DrawMesh(MeshPool.plane10,
            Matrix4x4.TRS(vec2 + (vec1 - vec2) / 2, Quaternion.AngleAxis(vec1.AngleToFlat(vec2) + 90f, Vector3.up),
                new(1f, 1f, (vec1 - vec2).magnitude)),
            Graphic.MatSingle, 0);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Target, "target");
        Scribe_References.Look(ref Holder, "holder");
        Scribe_Values.Look(ref ticksTillBounce, "ticksTillBounce");
        Scribe_Values.Look(ref numBounces, "numBounces");
        Scribe_Collections.Look(ref prevTargets, "prevTargets", LookMode.Reference);
        Scribe_Deep.Look(ref Props, "props");
    }

    public void FireAt(Thing target)
    {
        if (target == null || numBounces >= Props.bounceCount)
        {
            Destroy();
            return;
        }

        Holder = Target;
        prevTargets.Add(Target);
        Target = target;
        numBounces++;
        ticksTillBounce = Props.bounceDelay;
        Target.TakeDamage(new(Props.damageDef, Ability.power, -1f, Holder.DrawPos.AngleToFlat(Target.DrawPos), Caster));
        if (Props.addFire && Target.TryGetComp<CompAttachBase>(out _))
        {
            var fire = (Fire)GenSpawn.Spawn(ThingDefOf.Fire, Target.Position, Target.Map);
            fire.AttachTo(Target);
        }

        if (Props.impactRadius > 0f)
            GenExplosion.DoExplosion(Target.Position, Map, Props.impactRadius, Props.explosionDamageDef, Caster, Mathf.RoundToInt(Ability.power));
    }

    public override void Tick()
    {
        if (ticksTillBounce > 0) ticksTillBounce--;

        if (ticksTillBounce <= 0) FireAt(NextTarget());

        Position = Holder.Position;
    }

    private Thing NextTarget()
    {
        var things = GenRadial.RadialDistinctThingsAround(Holder.Position, Map, Ability.range, false)
           .Where(t => Ability.targetingParametersList[0].CanTarget(new(t)) &&
                       (Props.targetFriendly || t.HostileTo(Caster)))
           .Except(new[] { this, Target });
        if (!Props.allowRepeat) things = things.Except(prevTargets);
        switch (Props.bouncePriority)
        {
            case BouncePriority.Near:
                things = things.OrderBy(t => t.Position.DistanceTo(Holder.Position));
                break;
            case BouncePriority.Far:
                things = things.OrderByDescending(t => t.Position.DistanceTo(Holder.Position));
                break;
            case BouncePriority.Random:
                things = things.InRandomOrder();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return things.FirstOrDefault();
    }
}

public enum BouncePriority
{
    Near, Far, Random
}

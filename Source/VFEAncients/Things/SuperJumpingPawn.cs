using UnityEngine;
using Verse;

namespace VFEAncients;

public class SuperJumpingPawn : PawnFlyerWorker
{
    public SuperJumpingPawn(PawnFlyerProperties properties) : base(properties) { }

    public override float GetHeight(float t) => t - Mathf.Pow(t, 2);
}

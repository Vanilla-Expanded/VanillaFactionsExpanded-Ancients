using UnityEngine;
using VFECore.Abilities;

namespace VFEAncients
{
    public class SuperJumpingPawn : AbilityPawnFlyer
    {
        public override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var x = ticksFlying / (float) ticksFlightTime;
            FlyingPawn.DrawAt(position + Vector3.forward * (x - Mathf.Pow(x, 2)) * 15f, flip);
        }
    }
}
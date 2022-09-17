using RimWorld;
using Verse;

namespace VFEAncients
{
    public class CompDisappearOnHacked : ThingComp
    {
        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == CompHackable.HackedSignal) parent.Destroy();
        }
    }
}
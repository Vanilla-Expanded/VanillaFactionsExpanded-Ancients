using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace VFEAncients;

public class QuestNode_GetFactionOfDef : QuestNode
{
    public SlateRef<FactionDef> factionDef;
    public SlateRef<string> storeAs;

    public override void RunInt()
    {
        DoIt(QuestGen.slate);
    }

    public override bool TestRunInt(Slate slate) => DoIt(slate);

    private bool DoIt(Slate slate)
    {
        var def = factionDef.GetValue(slate);
        var faction = Find.FactionManager.FirstFactionOfDef(def);
        if (faction == null) return false;
        slate.Set(storeAs.GetValue(slate), faction);
        return true;
    }
}

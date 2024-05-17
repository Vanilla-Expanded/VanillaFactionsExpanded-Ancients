using HarmonyLib;
using UnityEngine;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class VFEAncientsMod : Mod
    {
        public static Harmony Harm;

        public static bool YayosCombat;

        public static VFEAncientsSettings settings;

        public VFEAncientsMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<VFEAncientsSettings>();
            if (ModLister.HasActiveModWithName("Yayo's Combat 3 [Adopted]")) YayosCombat = true;
            Harm = new Harmony("VanillaExpanded.VFEA");
            // Harmony.DEBUG = true;
            PowerPatches.Do(Harm);
            AbilityPatches.Do(Harm);
            BuildingPatches.Do(Harm);
            PreceptPatches.Do(Harm);
            PointDefensePatches.Do(Harm);
            MetaMorphPatches.Do(Harm);
            StorytellerPatches.Do(Harm);
            MendingPatches.Do(Harm);
            QuestPatches.Do(Harm);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => Content.Name;
    }

    public class VFEAncientsSettings : ModSettings
    {
        public bool enableGloryKillMusic = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref enableGloryKillMusic, "enableGloryKillMusic", true);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            var rect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            var listingStandard = new Listing_Standard();
            listingStandard.Begin(rect);
            listingStandard.CheckboxLabeled("VFEAncients.EnableGloryKillMusic".Translate(), ref enableGloryKillMusic);
            listingStandard.End();
        }
    }
}
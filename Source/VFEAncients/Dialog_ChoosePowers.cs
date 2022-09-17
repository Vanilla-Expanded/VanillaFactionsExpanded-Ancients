using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace VFEAncients
{
    [StaticConstructorOnStartup]
    public class Dialog_ChoosePowers : Window
    {
        public static Texture2D SuperpowerBackgroundTex = ContentFinder<Texture2D>.Get("Powers/Backgrounds/Background_Power");
        public static Texture2D WeaknessBackgroundTex = ContentFinder<Texture2D>.Get("Powers/Backgrounds/Background_Weakness");
        private readonly List<Tuple<PowerDef, PowerDef>> choices;
        private readonly Action<Tuple<PowerDef, PowerDef>> onChosen;
        private readonly Pawn pawn;

        public Dialog_ChoosePowers(List<Tuple<PowerDef, PowerDef>> choices, Pawn pawn, Action<Tuple<PowerDef, PowerDef>> onChosen)
        {
            this.choices = choices;
            this.pawn = pawn;
            this.onChosen = onChosen;
            this.forcePause = true;
        }

        public override Vector2 InitialSize => new(500f, 300f);

        public override void DoWindowContents(Rect inRect)
        {
            inRect = inRect.ContractedBy(15f, 7f);
            Widgets.Label(inRect.TopPartPixels(60f), "VFEAncients.PowerChoice".Translate(pawn.NameShortColored));
            inRect.y += 60f;
            foreach (var (superpower, weakness, rect) in choices.Zip(Split(inRect, choices.Count, new Vector2(80f, 200f)), (tuple, rect) => (tuple.Item1, tuple.Item2, rect)))
            {
                var superpowerRect = new Rect(rect.x, rect.y, 80f, 80f);
                var weaknessRect = new Rect(rect.x, rect.y + 85f, 80f, 80f);
                var buttonRect = new Rect(rect.x + 5f, rect.y + 170f, 70f, 30f);
                GUI.DrawTexture(superpowerRect, SuperpowerBackgroundTex);
                GUI.DrawTexture(weaknessRect, WeaknessBackgroundTex);
                GUI.DrawTexture(superpowerRect, superpower.Icon);
                TooltipHandler.TipRegion(superpowerRect, new TipSignal($"{superpower.LabelCap}\n\n{superpower.description}\n{superpower.Worker.EffectString()}"));
                GUI.DrawTexture(weaknessRect, weakness.Icon);
                TooltipHandler.TipRegion(weaknessRect, new TipSignal($"{weakness.LabelCap}\n\n{weakness.description}\n{weakness.Worker.EffectString()}"));
                if (Widgets.ButtonText(buttonRect, "VFEAncients.Select".Translate()))
                {
                    onChosen(new Tuple<PowerDef, PowerDef>(superpower, weakness));
                    Close();
                    break;
                }
            }
        }

        private static IEnumerable<Rect> Split(Rect rect, int parts, Vector2 size, bool vertical = false)
        {
            var distance = (vertical ? rect.height : rect.width) / parts;
            var curLoc = new Vector2(rect.x, rect.y);
            var offset = vertical ? new Vector2(0, distance / 2 - size.y / 2) : new Vector2(distance / 2 - size.x / 2, 0);
            for (var i = 0f; i < (vertical ? rect.height : rect.width); i += distance)
            {
                yield return new Rect(curLoc + offset, size);
                if (vertical) curLoc.y += distance;
                else curLoc.x += distance;
            }
        }
    }
}
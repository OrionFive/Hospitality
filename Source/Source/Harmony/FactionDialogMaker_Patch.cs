using System.Linq;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality.Harmony
{
    /// <summary>
    /// Detouring for escalating silver costs...
    /// </summary>
    public class FactionDialogMaker_Patch
    {
        [HarmonyPatch(typeof(FactionDialogMaker), "OfferGiftOption")]
        public class OfferGiftOption
        {
            public static Pawn negotiator
            {
                get { return Traverse.Create(typeof(FactionDialogMaker)).Field("negotiator").GetValue<Pawn>(); }
            }

            public static Faction faction
            {
                get { return Traverse.Create(typeof(FactionDialogMaker)).Field("faction").GetValue<Faction>(); }
            }

            [HarmonyPrefix]
            public static bool Replacement(ref DiaOption __result, Map map)
            {
                int cost = GetCost(Hospitality_MapComponent.Instance(map).GetBribeCount(faction));
                //Log.Message(faction.name + ": " + Hospitality_MapComponent.Instance.GetBribeCount(faction) + " = " + cost);
                int silver = TradeUtility.AllLaunchableThings(map).Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount);
                if (silver < cost)
                {
                    var diaOption = new DiaOption("OfferGift".Translate());
                    diaOption.Disable("NeedSilverLaunchable".Translate(cost));
                    __result = diaOption;
                    return false;
                }
                float goodwillDelta = 12f*negotiator.GetStatValue(StatDefOf.GiftImpact);
                var diaOption2 = new DiaOption("OfferGift".Translate() + " (" + "SilverForGoodwill".Translate(cost, goodwillDelta.ToString("#####0")) + ")");
                diaOption2.action = delegate {
                    TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, cost, map, null);
                    faction.AffectGoodwillWith(Faction.OfPlayer, goodwillDelta);
                    Hospitality_MapComponent.Instance(map).Bribe(faction);
                };
                string text = "SilverGiftSent".Translate(faction.leader.LabelIndefinite(), Mathf.RoundToInt(goodwillDelta)).CapitalizeFirst();
                diaOption2.link = new DiaNode(text)
                {
                    options =
                    {
                        new DiaOption("OK".Translate())
                        {
                            linkLateBind = () => FactionDialogMaker.FactionDialogFor(negotiator, faction)
                        }
                    }
                };
                __result = diaOption2;
                return false;
            }

            private static int GetCost(int bribeCount)
            {
                int amount = 150;
                int increase = 50;
                const int increase2 = 50;

                for (int i = 0; i < bribeCount; i++)
                {
                    amount += increase;
                    increase += increase2;
                }
                return amount;
            }
        }
    }
}
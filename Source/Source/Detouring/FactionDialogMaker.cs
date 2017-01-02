using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using Source = RimWorld.FactionDialogMaker;

namespace Hospitality.Detouring
{
    /// <summary>
    /// Detouring for escalating silver costs...
    /// </summary>
    public class FactionDialogMaker
    {
        public static Pawn negotiator
        {
            get { return (Pawn) typeof (Source).GetField("negotiator", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null); }
        }

        public static Faction faction
        {
            get { return (Faction)typeof(Source).GetField("faction", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null); }
        }

        [Detour(typeof(Source))]
        private static DiaOption OfferGiftOption()
        {
            int cost = GetCost(Hospitality_MapComponent.Instance.GetBribeCount(faction));
            //Log.Message(faction.name + ": " + Hospitality_MapComponent.Instance.GetBribeCount(faction) + " = " + cost);
            int silver = TradeUtility.AllLaunchableThings.Where(t => t.def == ThingDefOf.Silver).Sum(t => t.stackCount);
            if (silver < cost)
            {
                var diaOption = new DiaOption("OfferGift".Translate() + " (" + "NeedSilverLaunchable".Translate(new object[]
                                                                                                                {
                                                                                                                    cost
                                                                                                                }) + ")");
                diaOption.Disable("NotEnoughSilver".Translate());
                return diaOption;
            }
            float goodwillDelta = 12f*negotiator.GetStatValue(StatDefOf.GiftImpact);
            var diaOption2 = new DiaOption("OfferGift".Translate() + " (" + "SilverForGoodwill".Translate(new object[]
                                                                                                          {
                                                                                                              cost,
                                                                                                              goodwillDelta.ToString("#####0")
                                                                                                          }) + ")");
            diaOption2.action = delegate
                                {
                                    TradeUtility.LaunchThingsOfType(ThingDefOf.Silver, cost, null);
                                    faction.AffectGoodwillWith(Faction.OfPlayer, goodwillDelta);
                                    Hospitality_MapComponent.Instance.Bribe(faction);
                                };
            string text = "SilverGiftSent".Translate(new object[]
                                                     {
                                                         faction.Name,
                                                         Mathf.RoundToInt(goodwillDelta)
                                                     });
            diaOption2.link = new DiaNode(text)
                              {
                                  options =
                                  {
                                      new DiaOption("OK".Translate())
                                      {
                                          linkLateBind = () => Source.FactionDialogFor(negotiator, faction)
                                      }
                                  }
                              };
            return diaOption2;
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
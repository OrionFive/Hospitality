using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JoyGiver_BuyStuff : JoyGiver
    {
        private readonly JobDef jobDefBuy = DefDatabase<JobDef>.GetNamed("BuyItem");
        private readonly JobDef jobDefBrowse = DefDatabase<JobDef>.GetNamed("BrowseItems");
        public JoyGiverDefShopping Def => (JoyGiverDefShopping) def;

        public override void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            outCandidates.AddRange(pawn.Map.listerThings.ThingsInGroup(Def.requestGroup));
        }

        public override float GetChance(Pawn pawn)
        {
            if (!pawn.IsArrivedGuest(out _)) return 0;
            if (!pawn.MayBuy()) return 0;
            if (pawn.GetShoppingArea() == null) return 0;
            var money = pawn.GetMoney();
            //Log.Message(pawn.NameStringShort + " has " + money + " silver left.");
            return Mathf.InverseLerp(0, 25, money)*base.GetChance(pawn);
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            var shoppingArea = pawn?.GetShoppingArea();
            if (shoppingArea == null) return null;

            var map = pawn.MapHeld;
            var things = shoppingArea.ActiveCells.SelectMany(cell => map.thingGrid.ThingsListAtFast(cell))
                .Where(t => t != null && ItemUtility.IsBuyableAtAll(pawn, t) && Qualifies(t, pawn)).ToList();
            var storage = shoppingArea.ActiveCells.Select(cell=>map.edificeGrid[cell]).OfType<Building_Storage>();
            things.AddRange(storage.SelectMany(s => s.slotGroup.HeldThings.Where(t => ItemUtility.IsBuyableAtAll(pawn, t) && Qualifies(t, pawn))));
            if (things.Count == 0) return null;
            var requiresFoodFactor = GuestUtility.GetRequiresFoodFactor(pawn);

            // Try some things
            var selection = things.TakeRandom(5).Where(t => pawn.CanReach(t.Position, PathEndMode.Touch, Danger.None, false, false, TraverseMode.PassDoors)).ToArray();
            Thing thing = null;
            if (selection.Length > 1)
                thing = selection.MaxBy(t => Likey(pawn, t, requiresFoodFactor));
            else if (selection.Length == 1) thing = selection[0];

            if (thing == null) return null;

            if (Likey(pawn, thing, requiresFoodFactor) <= 0.5f)
            {
                //Log.Message(thing.Label + ": not interesting for " + pawn.NameStringShort);
                int duration = Rand.Range(JobDriver_BuyItem.MinShoppingDuration, JobDriver_BuyItem.MaxShoppingDuration);

                var canBrowse = CellFinder.TryRandomClosewalkCellNear(thing.Position, map, 2, out var standTarget) && ItemUtility.IsBuyableNow(pawn, thing);
                if (canBrowse)
                {
                    return new Job(jobDefBrowse, standTarget, thing) {expiryInterval = duration * 2};
                }

                return null;
            }

            //Log.Message($"{pawn.NameShortColored} is going to buy {thing.LabelShort} at {thing.Position}.");
            return new Job(jobDefBuy, thing);
        }

        private static float Likey(Pawn pawn, Thing thing, float requiresFoodFactor)
        {
            if (thing == null) return 0;

            // Health of object
            var hpFactor = thing.def.useHitPoints?(float)thing.HitPoints/thing.MaxHitPoints:1;
            
            // Apparel
            var appFactor = thing is Apparel apparel ? 1 + ApparelScoreGain(pawn, apparel) : 0.8f; // Not apparel, less likey
            //Log.Message(thing.Label + " - apparel score: " + appFactor);

            // Food
            if(thing.IsFood() && pawn.RaceProps.CanEverEat(thing))
            {
                appFactor = FoodUtility.FoodOptimality(pawn, thing, FoodUtility.GetFinalIngestibleDef(thing), 0, true) / 300f; // 300 = optimality max
                //Log.Message($"{pawn.LabelShort} looked at {thing.LabelShort} at {thing.Position}.");
                //Log.Message($"{pawn.LabelShort} added {requiresFoodFactor} to the score for his hunger and {appFactor} for food optimality.");
                appFactor += requiresFoodFactor;
                if (thing.def.IsWithinCategory(ThingCategoryDefOf.PlantFoodRaw)) appFactor -= 0.25f;
                if (thing.def.IsWithinCategory(ThingCategoryDefOf.MeatRaw)) appFactor -= 0.5f;
            }
            // Other consumables
            else if (thing.IsIngestible() && thing.def.ingestible.joy > 0)
            {
                appFactor = 1 + thing.def.ingestible.joy*0.5f;

                // Hungry? Care less about other stuff
                if(requiresFoodFactor > 0) appFactor -= requiresFoodFactor;
            }
            else
            {
                // Hungry? Care less about other stuff
                if(requiresFoodFactor > 0) appFactor -= requiresFoodFactor;
            }

            if (CompBiocodable.IsBiocoded(thing) && !CompBiocodable.IsBiocodedFor(thing, pawn)) return 0;

            // Weapon
            if (thing.def.IsRangedWeapon)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Brawler)) return 0;
                if (pawn.apparel.WornApparel.OfType<ShieldBelt>().Any()) return 0;
            }
            if (thing.def.IsWeapon)
            {
                // Weapon is also good!
                appFactor = 1;
                if (pawn.RaceProps.Humanlike && pawn.WorkTagIsDisabled(WorkTags.Violent)) return 0;
                if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return 0;
                if (!ItemUtility.AlienFrameworkAllowsIt(pawn.def, thing.def, "CanEquip")) return 0;
            }
            // Shield belt
            if (thing is ShieldBelt)
            {
                if (pawn.equipment.Primary?.def.IsRangedWeapon == true) return 0;
                if (!ItemUtility.AlienFrameworkAllowsIt(pawn.def, thing.def, "CanEquip")) return 0;
            }

            // Quality of object
            var qFactor = 0.7f;
            if (thing.TryGetQuality(out var cat))
            {
                qFactor = (float) cat;
                qFactor -= (float) QualityCategory.Normal;
                qFactor /= (float) QualityCategory.Masterwork - (float) QualityCategory.Normal;
                qFactor += 1;
                //Log.Message(thing.Label+" - quality: "+cat+" = "+ qFactor);
            }
            // Tech level of object
            var tFactor = 0.8f;
            if (thing.def.techLevel != TechLevel.Undefined)
            {
                tFactor = (float) thing.def.techLevel;
                tFactor -= (float) pawn.Faction.def.techLevel;
                tFactor /= (float) TechLevel.Spacer;
                tFactor += 1;
                //Log.Message(thing.Label + " - techlevel: " + thing.def.techLevel + " = " + tFactor);
            }
            var rFactor = Rand.RangeSeeded(0.5f, 1.7f, pawn.thingIDNumber*60509 + thing.thingIDNumber*33151);
            //if(hpFactor*hpFactor*qFactor*qFactor*tFactor*appFactor > 0.5) 
            //    Log.Message($"{thing.LabelShort.Colorize(Color.yellow)} - score: {hpFactor * hpFactor * qFactor * qFactor * tFactor * appFactor}, random: {rFactor}");
            return Mathf.Max(0, hpFactor*hpFactor*qFactor*qFactor*tFactor*appFactor*rFactor); // <= 0.5 = don't buy
        }

        // Copied so we can make some adjustments
        public static float ApparelScoreGain(Pawn pawn, Apparel ap)
        {
            if (ap is ShieldBelt && pawn.equipment.Primary?.def.IsWeaponUsingProjectiles == true)
                return -1000;
            // Added
            if (!ItemUtility.AlienFrameworkAllowsIt(pawn.def, ap.def, "CanWear")) 
                return -1000;
            if (!ApparelUtility.HasPartsToWear(pawn, ap.def))
                return -1000;
            if (pawn.story.traits.HasTrait(TraitDefOf.Nudist)) return -1000;
            //if (PawnApparelGenerator.IsHeadgear(ap.def)) return 0;
            float num = RimWorld.JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, ap);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            bool flag = false;
            // Added:
            var newReq = ItemUtility.IsRequiredByRoyalty(pawn, ap.def);

            for (int i = 0; i < wornApparel.Count; ++i)
            {
                if (!ApparelUtility.CanWearTogether(wornApparel[i].def, ap.def, pawn.RaceProps.body))
                {
                    if (pawn.apparel.IsLocked(wornApparel[i])) return -1000;
                    // Added: 
                    var wornReq = ItemUtility.IsRequiredByRoyalty(pawn, wornApparel[i].def);
                    if (wornReq && !newReq) return -1000;
                    //if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[index]))
                    //    return -1000f;
                    num -= JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, wornApparel[i]);
                    flag = true;
                }
            }
            if (!flag)
                num *= 10f;
            return num;
        }

        protected virtual bool Qualifies(Thing thing, Pawn pawn)
        {
            return Def.requestGroup.Includes(thing.def);
        }

        public static bool CanEat(Thing thing, Pawn pawn)
        {
            return thing.def.IsNutritionGivingIngestible && thing.def.IsWithinCategory(ThingCategoryDefOf.Foods) && ItemUtility.AlienFrameworkAllowsIt(pawn.def, thing.def, "CanEat");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Hospitality
{
    public class JoyGiver_BuyStuff : JoyGiver
    {
        private JobDef jobDefBuy = DefDatabase<JobDef>.GetNamed("BuyItem");
        private JobDef jobDefBrowse = DefDatabase<JobDef>.GetNamed("BrowseItems");
        protected virtual ThingRequestGroup RequestGroup => ThingRequestGroup.HaulableEver;

        public override float GetChance(Pawn pawn)
        {
            if (!pawn.IsGuest()) return 0;
            if (!pawn.MayBuy()) return 0;
            var money = GetMoney(pawn);
            //Log.Message(pawn.NameStringShort + " has " + money + " silver left.");

            return Mathf.InverseLerp(0, 25, money)*base.GetChance(pawn);
        }

        public static int GetMoney(Pawn pawn)
        {
            var money = pawn.inventory.innerContainer.FirstOrDefault(i => i.def == ThingDefOf.Silver);
            if (money == null) return 0;
            return money.stackCount;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            var map = pawn.MapHeld;
            var things = map.listerThings.ThingsInGroup(RequestGroup).Where(t => IsBuyableAtAll(pawn, t) && Qualifies(t, pawn)).ToList();
            var storage = map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>().Where(pawn.IsInShoppingZone);
            things.AddRange(storage.SelectMany(s => s.slotGroup.HeldThings.Where(t => IsBuyableAtAll(pawn, t) && Qualifies(t, pawn))));
            if (things.Count == 0) return null;
            Thing thing = things.RandomElement(); //things.MaxBy(t => Likey(pawn, t));

            if (thing == null)
            {
                return null;
            }
            if (Likey(pawn, thing) <= 0.5f)
            {
                //Log.Message(thing.Label + ": not interesting for " + pawn.NameStringShort);
                int duration = Rand.Range(JobDriver_BuyItem.MinShoppingDuration, JobDriver_BuyItem.MaxShoppingDuration);

                var canBrowse = CellFinder.TryRandomClosewalkCellNear(thing.Position, map, 2, out var standTarget) && IsBuyableNow(pawn, thing);
                if (canBrowse)
                {
                    return new Job(jobDefBrowse, standTarget, thing) {expiryInterval = duration*2};
                }
                return null;
            }

            return new Job(jobDefBuy, thing);
        }

        private static float Likey(Pawn pawn, Thing thing)
        {
            if (thing == null) return 0;

            // Health of object
            var hpFactor = thing.def.useHitPoints?((float)thing.HitPoints/thing.MaxHitPoints):1;
            
            // Apparel
            var appFactor = thing is Apparel apparel ? 1+ApparelScoreGain(pawn, apparel) : 0.8f; // Not apparel, less likey
            //Log.Message(thing.Label + " - apparel score: " + appFactor);

            // Food
            if(IsFood(thing) && pawn.RaceProps.CanEverEat(thing))
            {
                var needFood = pawn.needs.TryGetNeed<Need_Food>();
                var hungerFactor = 1 - needFood?.CurLevelPercentage ?? 0;
                hungerFactor -= 1 - needFood?.PercentageThreshHungry ?? 0; // about -0.7
                appFactor = FoodUtility.FoodOptimality(pawn, thing, FoodUtility.GetFinalIngestibleDef(thing), 0, true) / 300f; // 300 = optimality max
                //Log.Message($"{pawn.LabelShort} looked at {thing.LabelShort} at {thing.Position} and scored it {appFactor}.");
                appFactor += hungerFactor;
                //Log.Message($"{pawn.LabelShort} added {hungerFactor} to the score for his hunger.");
                if (thing.def.IsWithinCategory(ThingCategoryDefOf.PlantFoodRaw)) appFactor -= 0.25f;
                if (thing.def.IsWithinCategory(ThingCategoryDefOf.MeatRaw)) appFactor -= 0.5f;
            }

            // Weapon
            if (thing.def.IsRangedWeapon)
            {
                if (pawn.story.traits.HasTrait(TraitDefOf.Brawler)) return 0;
                if (pawn.apparel.WornApparel.OfType<ShieldBelt>().Any()) return 0;
            }
            if (thing.def.IsWeapon)
            {
                appFactor = 1; // Weapon is also good!
                if (pawn.RaceProps.Humanlike && pawn.story.WorkTagIsDisabled(WorkTags.Violent)) return 0;
                if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) return 0;
            }
            // Shieldbelt
            if (thing is ShieldBelt && pawn.equipment.Primary?.def.IsRangedWeapon == true) return 0;

            // Quality of object
            var qFactor = 0.8f;
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
            var rFactor = Rand.RangeSeeded(0.5f, 2f, pawn.thingIDNumber*60509 + thing.thingIDNumber*33151);
            //Log.Message(thing.Label + " - score: " + hpFactor*hpFactor*qFactor*tFactor*appFactor);
            return Mathf.Max(0, hpFactor*hpFactor*qFactor*tFactor*appFactor*rFactor); // 0 = don't buy
        }

        private static bool IsFood(Thing thing)
        {
            return thing.def.ingestible != null
                   && thing.def.ingestible.preferability != FoodPreferability.NeverForNutrition 
                   && thing.def.ingestible.preferability != FoodPreferability.DesperateOnlyForHumanlikes
                   && thing.def.ingestible.preferability != FoodPreferability.DesperateOnly;
        }

        // Copied so outfits can be commented
        public static float ApparelScoreGain(Pawn pawn, Apparel ap)
        {
            if (ap is ShieldBelt && pawn.equipment.Primary?.def.IsWeaponUsingProjectiles == true)
                return -1000f;
            if (!AlienFrameworkAllowsIt(pawn.def, ap.def)) 
                return -1000;
            float num = JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, ap);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            bool flag = false;
            for (int index = 0; index < wornApparel.Count; ++index)
            {
                if (!ApparelUtility.CanWearTogether(wornApparel[index].def, ap.def, pawn.RaceProps.body))
                {
                    //if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[index]))
                    //    return -1000f;
                    num -= JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, wornApparel[index]);
                    flag = true;
                }
            }
            if (!flag)
                num *= 10f;
            return num;
        }

        public static bool AlienFrameworkAllowsIt(ThingDef pawnDef, ThingDef apparelDef)
        {
            var restriction = Traverse.Create(pawnDef).Field("raceRestrictions");
            if (!restriction.FieldExists()) return true; // Not using AlienFramework
            var canWear = restriction.Method("CanWear").GetValue<bool>(apparelDef, pawnDef);
            return canWear;
        }

        protected virtual bool Qualifies(Thing thing, Pawn pawn)
        {
            return true;
        }

        public static bool IsBuyableAtAll(Pawn pawn, Thing thing)
        {
            if (!IsBuyableNow(pawn, thing)) return false;
            if (!pawn.IsInShoppingZone(thing))
            {
                //if (thing.GetRoom() == null) Log.Message(thing.Label + ": not in room");
                //else Log.Message(thing.Label + ": in room " + thing.GetRoom().Role.LabelCap);
                return false;
            }
            if (thing.def.isUnfinishedThing)
            {
                return false;
            }
            if (thing.def == ThingDefOf.Silver)
            {
                return false;
            }
            if (thing.def.tradeability == Tradeability.None)
            {
                return false;
            }
            //if (!thing.IsSociallyProper(pawn))
            //{
            //    Log.Message(thing.Label + ": is not proper for " + pawn.NameStringShort);
            //    return false;
            //}
            var marketValue = thing.MarketValue * JobDriver_BuyItem.PriceFactor;
            if (marketValue < 1)
            {
                return false;
            }
            if (marketValue > GetMoney(pawn))
            {
                return false;
            }
            if (BoughtByPlayer(pawn, thing))
            {
                return false;
            }
            //if (thing.IsInValidStorage()) Log.Message(thing.Label + " in storage ");
            return true;
        }

        private static bool BoughtByPlayer(Pawn pawn, Thing thing)
        {
            var lord = pawn.GetLord();
            return !(lord?.CurLordToil is LordToil_VisitPoint toil) || toil.BoughtOrSoldByPlayer(thing);
        }

        public static bool IsBuyableNow(Pawn pawn, Thing thing)
        {
            if (!thing.SpawnedOrAnyParentSpawned)
            {
                return false;
            }
            if (thing.ParentHolder is Pawn)
            {
                //Log.Message(thing.Label+": is inside pawn "+pawn.NameStringShort);
                return false;
            }
            if (thing.IsForbidden(Faction.OfPlayer))
            {
                //Log.Message(thing.Label+": is forbidden for "+pawn.NameStringShort);
                return false;
            }
            if (!pawn.HasReserved(thing) && !pawn.CanReserveAndReach(thing, PathEndMode.OnCell, Danger.None))
            {
                //Log.Message(thing.Label+": can't be reserved or reached by "+pawn.NameStringShort);
                return false;
            }
            if (pawn.GetInventorySpaceFor(thing) < 1)
            {
                return false;
            }

            return true;
        }
    }
}

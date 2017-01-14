using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JoyGiver_BuyStuff : JoyGiver
    {
        private JobDef jobDefBuy = DefDatabase<JobDef>.GetNamed("BuyItem");
        private JobDef jobDefBrowse = DefDatabase<JobDef>.GetNamed("BrowseItems");
        protected virtual ThingRequestGroup RequestGroup { get { return ThingRequestGroup.HaulableEver; } }

        public override float GetChance(Pawn pawn)
        {
            if (!pawn.IsGuest()) return 0;
            var money = GetMoney(pawn);
            //Log.Message(pawn.NameStringShort + " has " + money + " silver left.");

            return Mathf.InverseLerp(0, 25, money)*base.GetChance(pawn);
        }

        public static int GetMoney(Pawn pawn)
        {
            var money = pawn.inventory.GetInnerContainer().FirstOrDefault(i => i.def == ThingDefOf.Silver);
            if (money == null) return 0;
            return money.stackCount;
        }

        public override Job TryGiveJob(Pawn pawn)
        {
            var map = pawn.MapHeld;
            var things = map.listerThings.ThingsInGroup(RequestGroup).Where(t => IsBuyableAtAll(pawn, t) && Qualifies(t)).ToList();
            var storage = map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>().Where(pawn.IsInGuestZone);
            things.AddRange(storage.SelectMany(s => s.slotGroup.HeldThings.Where(t => IsBuyableAtAll(pawn, t) && Qualifies(t))));
            if (things.Count == 0) return null;
            Thing thing = things.RandomElement(); //things.MaxBy(t => Likey(pawn, t));

            if (thing == null)
            {
                return null;
            }
            if (Likey(pawn, thing) <= 0.4f)
            {
                //Log.Message(thing.Label + ": not interesting for " + pawn.NameStringShort);
                IntVec3 standTarget;
                int duration = Rand.Range(JobDriver_BuyItem.MinShoppingDuration, JobDriver_BuyItem.MaxShoppingDuration);

                var canBrowse = CellFinder.TryRandomClosewalkCellNear(thing.Position, map, 2, out standTarget) && IsBuyableNow(pawn, thing);
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
            // Health of object
            var hpFactor = thing.def.useHitPoints?((float)thing.HitPoints/thing.MaxHitPoints):1;
            
            // Quality of object
            var qFactor = 1f;
            QualityCategory cat;
            if (thing.TryGetQuality(out cat))
            {
                qFactor = (float) cat;
                qFactor -= (float) QualityCategory.Normal;
                qFactor /= (float) QualityCategory.Masterwork - (float) QualityCategory.Normal;
                qFactor += 1;
                //Log.Message(thing.Label+" - quality: "+cat+" = "+ qFactor);
            }
            // Tech level of object
            var tFactor = 1f;
            if (thing.def.techLevel != TechLevel.Undefined)
            {
                tFactor = (float) thing.def.techLevel;
                tFactor -= (float) pawn.Faction.def.techLevel;
                tFactor /= (float) TechLevel.Spacer;
                tFactor += 1;
                //Log.Message(thing.Label + " - techlevel: " + thing.def.techLevel + " = " + tFactor);
            }
            var rFactor = Rand.Range(0.7f, 2f);
            //Log.Message(thing.Label+" - score: "+hpFactor*qFactor*tFactor);
            return Mathf.Max(0, hpFactor*hpFactor*qFactor*tFactor*rFactor); // 0 = don't buy
        }

        protected virtual bool Qualifies(Thing thing)
        {
            return true;
        }

        public static bool IsBuyableAtAll(Pawn pawn, Thing thing)
        {
            if (!IsBuyableNow(pawn, thing)) return false;
            if (!pawn.IsInGuestZone(thing))
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
            if (thing.def.tradeability == Tradeability.Never)
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
            //if (thing.IsInValidStorage()) Log.Message(thing.Label + " in storage ");
            return true;
        }

        public static bool IsBuyableNow(Pawn pawn, Thing thing)
        {
            if (thing.IsForbidden(Faction.OfPlayer))
            {
                //Log.Message(thing.Label+": is forbidden for "+pawn.NameStringShort);
                return false;
            }
            if (!pawn.CanReserveAndReach(thing, PathEndMode.OnCell, Danger.None))
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

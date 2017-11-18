using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Hospitality
{
    public class JobDriver_BuyItem : JobDriver
    {
        //Constants
        public const int MinShoppingDuration = 75;
        public const int MaxShoppingDuration = 300;
        public const float PriceFactor = 0.85f;

        //Properties
        protected Thing Item { get { return job.targetA.Thing; } }

        public override bool TryMakePreToilReservations()
        {
            return pawn.Reserve(job.targetA.Thing, job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

            this.FailOn(() => !JoyGiver_BuyStuff.IsBuyableNow(pawn, Item));
            //AddEndCondition(() =>
            //{
            //    if (Deliveree.health.ShouldGetTreatment)
            //        return JobCondition.Ongoing;
            //    return JobCondition.Succeeded;
            //});

            if (TargetThingA != null)
            {
                Toil reserveTargetA = Toils_Reserve.Reserve(TargetIndex.A);

                yield return reserveTargetA;
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.A);

                int duration = Rand.Range(MinShoppingDuration, MaxShoppingDuration);
                yield return Toils_General.Wait(duration);

                Toil takeThing = new Toil();
                takeThing.initAction = () => TakeThing(takeThing);
                yield return takeThing.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            }

            //yield return Toils_Jump.Jump(gotoToil); // shop some more
        }

        private void TakeThing(Toil toil)
        {
            Job curJob = toil.actor.jobs.curJob; 
            //Toils_Haul.ErrorCheckForCarry(toil.actor, Item);
            if (curJob.count == 0)
            {
                throw new Exception(string.Concat("BuyItem job had count = ", curJob.count, ". Job: ", curJob));
            }

            if (Item.MarketValue <= 0) return;
            int maxSpace = toil.actor.GetInventorySpaceFor(Item);
            var inventory = toil.actor.inventory.innerContainer;

            Thing silver = inventory.FirstOrDefault(i => i.def == ThingDefOf.Silver);
            if (silver == null) return;

            var itemCost = Item.MarketValue*PriceFactor;
            var maxAffordable = Mathf.FloorToInt(silver.stackCount/itemCost);
            if (maxAffordable < 1) return;

            // Changed formula a bit, so guests are less likely to leave small stacks if they can afford it
            var maxWanted = Rand.RangeInclusive(1, maxAffordable);
            int count = Mathf.Min(Item.stackCount, maxSpace, maxWanted);

            var price = Mathf.FloorToInt(count*itemCost);

            if(silver.stackCount < price) return;

            var map = toil.actor.MapHeld;
            var inventoryItemsBefore = inventory.ToArray();
            var tookItems = inventory.TryAdd(Item.SplitOff(count), count);

            var comp = toil.actor.GetComp<CompGuest>();
            if (tookItems > 0 && comp != null)
            {
                inventory.TryDrop(silver, toil.actor.Position, map, ThingPlaceMode.Near, price, out silver);

                // Check what's new in the inventory (TryAdd creates a copy of the original object!)
                var newItems = toil.actor.inventory.innerContainer.Except(inventoryItemsBefore).ToArray();
                foreach (var item in newItems)
                {
                    //Log.Message(pawn.NameStringShort + " bought " + item.Label);
                    comp.boughtItems.Add(item.thingIDNumber);

                    // Handle trade stuff
                    Trade(toil, item, map);
                }
            }
        }

        private void Trade(Toil toil, Thing item, Map map)
        {
            var twc = item as ThingWithComps;
            if (twc != null && map.mapPawns.FreeColonistsSpawnedCount > 0) twc.PreTraded(TradeAction.PlayerSells, map.mapPawns.FreeColonistsSpawned.RandomElement(), toil.actor);

            // Register with lord toil
            var lord = pawn.GetLord();
            if (lord == null) return;
            var lordToil = lord.CurLordToil as LordToil_VisitPoint;
            if (lordToil == null) return;

            lordToil.OnPlayerSoldItem(item);

        }
    }
}
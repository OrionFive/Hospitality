using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality
{
    public class JobDriver_BuyItem : JobDriver
    {
        //Constants
        public const int MinShoppingDuration = 75;
        public const int MaxShoppingDuration = 300;
        public const float PriceFactor = 0.85f;

        //Properties
        protected Thing Item { get { return CurJob.targetA.Thing; } }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => {
                if (!JoyGiver_BuyStuff.IsBuyableNow(pawn, Item)) return true;
                return false;
            });
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
                yield return takeThing;
            }

            //yield return Toils_Jump.Jump(gotoToil); // shop some more
        }

        private void TakeThing(Toil toil)
        {
            if (Toils_Haul.ErrorCheckForCarry(Item, toil.actor)) return;
            if (Item.MarketValue <= 0) return;
            int maxSpace = toil.actor.GetInventorySpaceFor(Item);
            int count = Mathf.Min(CurJob.maxNumToCarry, Item.stackCount, maxSpace);
            Thing silver = toil.actor.inventory.GetContainer().FirstOrDefault(i => i.def == ThingDefOf.Silver);
            var price = Mathf.FloorToInt(Item.MarketValue*count*PriceFactor);
            if (silver == null || silver.stackCount < price) return;

            var inventoryItemsBefore = toil.actor.inventory.GetContainer().ToArray();
            var tookItem = toil.actor.inventory.GetContainer().TryAdd(Item, count);
            toil.actor.inventory.GetContainer().TryDrop(silver, toil.actor.Position, ThingPlaceMode.Near, price, out silver);

            var comp = toil.actor.GetComp<CompGuest>();
            if (tookItem && comp != null)
            {
                // Check what's new in the inventory (TryAdd creates a copy of the original object!)
                var newItems = toil.actor.inventory.GetContainer().Except(inventoryItemsBefore).ToArray();
                foreach (var item in newItems)
                {
                    //Log.Message(pawn.NameStringShort + " bought " + item.Label);
                    comp.boughtItems.Add(item.thingIDNumber);

                    // Handle trade stuff
                    var twc = item as ThingWithComps;
                    if (twc != null && Find.MapPawns.FreeColonistsSpawnedCount > 0) twc.PreTraded(TradeAction.PlayerSells, Find.MapPawns.FreeColonistsSpawned.RandomElement(), toil.actor);
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using FoodUtility = Hospitality.Utilities.FoodUtility;

namespace Hospitality
{
    public class CompVendingMachine : ThingComp, IThingHolder
    {
        private bool isActive = false;

        private int basePrice = 10;

        private ThingOwner<Thing> silverContainer;

        public Building_NutrientPasteDispenser Dispenser => (Building_NutrientPasteDispenser) parent;

        public ThingOwner<Thing> MainContainer
        {
            get
            {
                if (silverContainer == null)
                {
                    silverContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
                    if (parent is Building_NutrientPasteDispenser { DispensableDef: { } } dispenser)
                    {
                        basePrice = Mathf.CeilToInt(dispenser.DispensableDef.BaseMarketValue);
                    }
                }
                return silverContainer;
            }
        }

        public int CurrentPrice
        {
            get => basePrice;
            internal set => basePrice = Mathf.Clamp(value, 0, int.MaxValue);
        }

        public bool IsFree => CurrentPrice == 0;
        public bool ShouldEmpty => MainContainer.Count > 0;
        public string TotalSold => ((float)MainContainer.TotalStackCount).ToStringMoney();

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isActive, "isActive");
            Scribe_Values.Look(ref basePrice, "basePrice");
            Scribe_Deep.Look(ref silverContainer, "silverContainer");
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        internal void SetAutoPricing()
        {
            var mapcomp = parent.Map.GetMapComponent();
            if (!mapcomp.PresentGuests.Any()) return;
            var val = mapcomp.PresentGuests.Where(p => p.inventory.Count(ThingDefOf.Silver) > 0).Select(g => g.inventory.Count(ThingDefOf.Silver)).Min();
            CurrentPrice = Mathf.CeilToInt(val/2f);
        }

        public bool IsActive()
        {
            return isActive;
        }

        public void ReceivePayment(ThingOwner<Thing> inventoryContainer, Thing silver)
        {
            inventoryContainer.TryTransferToContainer(silver, MainContainer, CurrentPrice);
        }

        public bool CanAffordFast(Pawn buyerGuest, out Thing silver)
        {
            silver = buyerGuest.inventory.innerContainer.FirstOrDefault(i => i.def == ThingDefOf.Silver);
            if (silver == null) return false;
            return silver.stackCount >= basePrice;
        }

        public bool CanBeUsedBy(Pawn eaterGuest, Thing foodSource = null, ThingDef foodDef = null)
        {
            if (!CanAffordFast(eaterGuest, out _)) return false;
            if (!FoodUtility.WillConsume(eaterGuest, foodDef)) return false;
            return isActive;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Toggle
            {
                defaultLabel = "Hospitality_VendingMachine".Translate(),
                defaultDesc = "Hospitality_VendingMachineToggleDesc".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Commands/AsGuest"),
                isActive = IsActive,
                toggleAction = () => isActive = !isActive,
                disabled = false,
            };

            if (isActive)
            {
                yield return new Gizmo_VendingMachine()
                {
                    vendingMachine = this,
                };
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            return;
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return MainContainer;
        }
    }

    public class CompProperties_VendingMachine : CompProperties
    {
        public CompProperties_VendingMachine()
        {
            compClass = typeof(CompVendingMachine);
        }
    }
}

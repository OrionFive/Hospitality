using System.Reflection;
using HugsLib.Source.Detour;
using RimWorld;
using Verse;
using Verse.AI;

namespace Hospitality.Detouring
{
    public static class ITab_Pawn_Gear
    {
        // This is so the player can't force visitors to drop items. The button remains, though, until fixed by Ludeon.
        [DetourMethod(typeof(RimWorld.ITab_Pawn_Gear), "InterfaceDrop")]
        public static void InterfaceDrop(this RimWorld.ITab_Pawn_Gear _this, Thing t)
        {
            var SelPawnForGear = (Pawn)typeof(RimWorld.ITab_Pawn_Gear).GetMethod("get_SelPawnForGear", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_this, null);

            if (SelPawnForGear.HostFaction == Faction.OfPlayer && !SelPawnForGear.IsPrisoner) return;

            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null && SelPawnForGear.apparel != null
                && SelPawnForGear.apparel.WornApparel.Contains(apparel))
            {
                SelPawnForGear.jobs.TryTakeOrderedJob(new Job(JobDefOf.RemoveApparel, apparel));
            }
            else if (thingWithComps != null && SelPawnForGear.equipment != null
                     && SelPawnForGear.equipment.AllEquipment.Contains(thingWithComps))
            {
                SelPawnForGear.jobs.TryTakeOrderedJob(new Job(JobDefOf.DropEquipment, thingWithComps));
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawnForGear.inventory.innerContainer.TryDrop(t, SelPawnForGear.Position,
                    SelPawnForGear.Map, ThingPlaceMode.Near, out thing, null);
            }
        }
    }
}
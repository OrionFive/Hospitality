using System;
using System.Collections.Generic;
using System.Linq;
using Hospitality.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using FoodUtility = RimWorld.FoodUtility;
using GuestUtility = Hospitality.Utilities.GuestUtility;

namespace Hospitality;

public class JoyGiver_BuyStuff : JoyGiver
{
    private readonly JobDef jobDefBrowse = DefDatabase<JobDef>.GetNamed("BrowseItems");
    private readonly JobDef jobDefBuy = DefDatabase<JobDef>.GetNamed("BuyItem");
    public JoyGiverDefShopping Def => (JoyGiverDefShopping)def;
    protected virtual int OptimalMoneyForShopping => 50;

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
        return Mathf.InverseLerp(1, OptimalMoneyForShopping, money) * base.GetChance(pawn);
    }

    public override Job TryGiveJob(Pawn pawn)
    {
        var shoppingArea = pawn?.GetShoppingArea();
        if (shoppingArea == null) return null;
        
        // Gather all things lying on the ground, and in storage
        var map = pawn.MapHeld;
        var groundThings = shoppingArea.ActiveCells.SelectMany(c => map.thingGrid.ThingsListAtFast(c));
        var storedThings = shoppingArea.ActiveCells.Select(cell => map.edificeGrid[cell]).OfType<Building_Storage>().SelectMany(s => s.slotGroup.HeldThings);
        
        var allThings = storedThings.Concat(groundThings).ToList();
        
        var pawnWealth = pawn.GetMoney();
        bool IsValidThing(Thing t) => ItemUtility.IsBuyableAtAll(pawn, pawnWealth, t) && Qualifies(t, pawn);

        List<Thing> selectedThings = null;
        var requiresFoodFactor = GuestUtility.GetRequiresFoodFactor(pawn);
        if (requiresFoodFactor <= 0.8)
        {
            // We can select non-food things, so anything random is good
            selectedThings = SelectRandomThings(allThings, 5, IsValidThing);
        }
        else
        {
            bool ValidFoodThing(Thing t) => t.IsFood() && pawn.RaceProps.CanEverEat(t) && FoodUtility.MoodFromIngesting(pawn, t, t.def) >= 0;
            selectedThings = SelectRandomThings(allThings, 5, t => ValidFoodThing(t) && IsValidThing(t));
        }
        
        var selection = selectedThings.Where(t => pawn.CanReach(t.Position, PathEndMode.Touch, Danger.None, false, false, TraverseMode.PassDoors)).ToArray();
        
        Thing thing = null;
        if (selection.Length > 1)
            thing = selection.MaxBy(t => Likey(pawn, t, requiresFoodFactor));
        else if (selection.Length == 1) thing = selection[0];

        if (thing == null) return null;

        if (Likey(pawn, thing, requiresFoodFactor) <= 0.5f)
        {
            //Log.Message(thing.Label + ": not interesting for " + pawn.NameStringShort);
            var duration = Rand.Range(JobDriver_BuyItem.MinShoppingDuration, JobDriver_BuyItem.MaxShoppingDuration);
            var urgent = pawn.needs?.food?.CurCategory >= HungerCategory.UrgentlyHungry;
            if (urgent) duration = 50;

            var canBrowse = CellFinder.TryRandomClosewalkCellNear(thing.Position, map, 2, out var standTarget) && ItemUtility.IsBuyableNow(pawn, thing);
            if (canBrowse)
            {
                return new Job(jobDefBrowse, standTarget, thing) { expiryInterval = duration * 2 };
            }

            return null;
        }

        //Log.Message($"{pawn.NameShortColored} is going to buy {thing.LabelShort} at {thing.Position}.");
        return new Job(jobDefBuy, thing);
    }

    private static List<Thing> SelectRandomThings(List<Thing> things, int count, Func<Thing, bool> predicate)
    {
        var selectedThings = new List<Thing>();
        
        // For performance ... doesn't necessarily mean we are actually doing this randomly :)
        var randomBaseIdx = Rand.Range(0, things.Count);
        for (var i = 0; i < count; i++)
        {
            var thing = things[(randomBaseIdx + i) % things.Count];
            
            if (predicate(thing))
            {
                selectedThings.Add(thing);
                if (selectedThings.Count == count)
                    break;
            }
        }

        return selectedThings;
    }

    private static float Likey(Pawn pawn, Thing thing, float requiresFoodFactor)
    {
        if (thing == null) return 0;

        // Health of object
        var hpFactor = thing.def.useHitPoints ? (float)thing.HitPoints / thing.MaxHitPoints : 1;

        // Apparel
        var appFactor = thing is Apparel apparel ? 1 + ApparelScoreGain(pawn, apparel) : 0.8f; // Not apparel, less likey
        //Log.Message(thing.Label + " - apparel score: " + appFactor);

        // Food
        if (thing.IsFood() && pawn.RaceProps.CanEverEat(thing))
        {
            appFactor = FoodUtility.FoodOptimality(pawn, thing, FoodUtility.GetFinalIngestibleDef(thing), 0, true) / 300f; // 300 = optimality max
            //Log.Message($"{pawn.LabelShort} looked at {thing.LabelShort} at {thing.Position}.");
            //Log.Message($"{pawn.LabelShort} added {requiresFoodFactor} to the score for his hunger and {appFactor} for food optimality.");
            appFactor += requiresFoodFactor;
            // FoodOptimality() factors in mood effect, but still returns positive results even for abhorrent food.
            // Adjust explicitly to make pawns avoid food that would result in mood debuffs.
            appFactor += FoodUtility.MoodFromIngesting(pawn, thing, thing.def) / 10f;
        }
        // Other consumables
        else if (thing.IsIngestible() && thing.def.ingestible.joy > 0)
        {
            appFactor = 1 + thing.def.ingestible.joy * 0.5f;

            // Hungry? Care less about other stuff
            if (requiresFoodFactor > 0) appFactor -= requiresFoodFactor / 3;
        }
        else
        {
            // Hungry? Care less about other stuff
            if (requiresFoodFactor > 0) appFactor -= requiresFoodFactor / 3;
        }

        if (CompBiocodable.IsBiocoded(thing) && !CompBiocodable.IsBiocodedFor(thing, pawn)) return 0;

        // Weapon
        if (thing.def.IsRangedWeapon)
        {
            if (pawn.story.traits.HasTrait(TraitDefOf.Brawler)) return 0;
            if (pawn.apparel.WornApparel.Exists(apparelObject => apparelObject.def.IsShieldThatBlocksRanged)) return 0;
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
        if (thing.def.IsShieldThatBlocksRanged)
        {
            if (pawn.equipment.Primary?.def.IsRangedWeapon == true) return 0;
            if (!ItemUtility.AlienFrameworkAllowsIt(pawn.def, thing.def, "CanEquip")) return 0;
        }

        // Quality of object
        var qFactor = 0.7f;
        if (thing.TryGetQuality(out var cat))
        {
            qFactor = (float)cat;
            qFactor -= (float)QualityCategory.Normal;
            qFactor /= (float)QualityCategory.Masterwork - (float)QualityCategory.Normal;
            qFactor += 1;
            //Log.Message(thing.Label+" - quality: "+cat+" = "+ qFactor);
        }

        // Tech level of object
        var tFactor = 0.8f;
        if (thing.def.techLevel != TechLevel.Undefined)
        {
            tFactor = (float)thing.def.techLevel;
            tFactor -= (float)pawn.Faction.def.techLevel;
            tFactor /= (float)TechLevel.Spacer;
            tFactor += 1;
            //Log.Message(thing.Label + " - techlevel: " + thing.def.techLevel + " = " + tFactor);
        }

        var rFactor = Rand.RangeSeeded(0.5f, 1.7f, pawn.thingIDNumber * 60509 + thing.thingIDNumber * 33151);
        //if(hpFactor*hpFactor*qFactor*qFactor*tFactor*appFactor > 0.5) 
        //    Log.Message($"{thing.LabelShort.Colorize(Color.yellow)} - score: {hpFactor * hpFactor * qFactor * qFactor * tFactor * appFactor}, random: {rFactor}");
        return Mathf.Max(0, hpFactor * hpFactor * qFactor * qFactor * tFactor * appFactor * rFactor); // <= 0.5 = don't buy
    }

    // Copied so we can make some adjustments
    public static float ApparelScoreGain(Pawn pawn, Apparel ap)
    {
        if (ap.def.IsShieldThatBlocksRanged && pawn.equipment.Primary?.def.IsWeaponUsingProjectiles == true)
            return -1000;
        // Added
        if (!ItemUtility.AlienFrameworkAllowsIt(pawn.def, ap.def, "CanWear"))
            return -1000;
        if (!ApparelUtility.HasPartsToWear(pawn, ap.def))
            return -1000;
        if (pawn.story.traits.HasTrait(TraitDefOf.Nudist)) return -1000;
        //if (PawnApparelGenerator.IsHeadgear(ap.def)) return 0;
        var num = JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, ap);
        var wornApparel = pawn.apparel.WornApparel;
        var flag = false;
        // Added:
        var newReq = ItemUtility.IsRequiredByRoyalty(pawn, ap.def);

        for (var i = 0; i < wornApparel.Count; ++i)
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
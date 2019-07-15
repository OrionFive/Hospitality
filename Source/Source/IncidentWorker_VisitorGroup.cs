using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Harmony;

namespace Hospitality
{
    // Note that this implementation is VERY different from vanilla
    public class IncidentWorker_VisitorGroup : IncidentWorker_NeutralGroup
    {
        private static ThingDef[] _items;

        // Taken from core
        private static readonly SimpleCurve pointsCurve = new SimpleCurve
        {
            new CurvePoint(45f, 0f), new CurvePoint(50f, 1f), new CurvePoint(100f, 1f), new CurvePoint(200f, 0.25f), new CurvePoint(300f, 0.1f), new CurvePoint(500f, 0f)
        };

        public static float MaxPleaseAmount(float current)
        {
            // if current standing is 100, 10 can be gained
            // if current standing is -100, 50 can be gained
            // then clamped.
            return Mathf.Clamp(current + 30 - Offset(current), -100, 100);
        }

        public static float MaxAngerAmount(float current)
        {
            return Mathf.Clamp(current - 30, -100, 100);
        }

        private static float Offset(float current)
        {
            return Mathf.Lerp(-20, 20, Mathf.InverseLerp(-100, 100, current));
        }

        protected override bool FactionCanBeGroupSource(Faction f, Map map, bool desperate = false)
        {
            return !f.IsPlayer && !f.defeated && !f.def.hidden && !f.HostileTo(Faction.OfPlayer);
        }

        private static bool CheckCanCome(Map map, Faction faction, out string reasons)
        {
            var fallout = map.GameConditionManager.ConditionIsActive(GameConditionDefOf.ToxicFallout);
            var hostileFactions = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead && !p.IsPrisoner && p.Faction != null && !p.Downed && !IsFogged(p)).Select(p => p.Faction).Where(p =>
                                                                   p.HostileTo(Faction.OfPlayer) || p.HostileTo(faction)).ToArray();
            var winter = map.GameConditionManager.ConditionIsActive(GameConditionDefOf.VolcanicWinter);
            var temp = faction.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.OutdoorTemp) && faction.def.allowedArrivalTemperatureRange.Includes(map.mapTemperature.SeasonalTemp);
            var beds = map.listerBuildings.AllBuildingsColonistOfClass<Building_GuestBed>().Any();

            reasons = null;

            if (temp && !fallout && !winter && !hostileFactions.Any() && beds) return true; // All clear, don't ask

            var reasonList = new List<string>();
            if (!beds) reasonList.Add("- " + "VisitorsArrivedReasonNoBeds".Translate());
            if (fallout) reasonList.Add("- " + GameConditionDefOf.ToxicFallout.LabelCap);
            if (winter) reasonList.Add("- " + GameConditionDefOf.VolcanicWinter.LabelCap);
            if (!temp) reasonList.Add("- " + "Temperature".Translate());

            foreach (var f in hostileFactions)
            {
                reasonList.Add("- " + f.def.pawnsPlural.CapitalizeFirst());
            }
            
            reasons = reasonList.Distinct().Aggregate((a, b) => a+"\n"+b);
            return false; // Do ask
    
        }

        private static bool IsFogged(Pawn pawn)
        {
            return pawn.MapHeld.fogGrid.IsFogged(pawn.PositionHeld);
        }

        private static void ShowAskMayComeDialog(Faction faction, Map map, string reasons, Direction8Way spawnDirection, Action allow, Action refuse)
        {
            string text = "VisitorsArrivedDesc".Translate(faction, reasons);

            DiaNode diaNode = new DiaNode(text);
            DiaOption diaOption = new DiaOption("VisitorsArrivedAccept".Translate());
            diaOption.action = allow;
            diaOption.resolveTree = true;
            diaNode.options.Add(diaOption);

            DiaOption diaOption2 = new DiaOption("VisitorsArrivedRefuse".Translate());
            diaOption2.action = refuse;
            diaOption2.resolveTree = true;
            diaNode.options.Add(diaOption2);

            if (!map.listerBuildings.AllBuildingsColonistOfClass<Building_GuestBed>().Any())
            {
                DiaOption diaOption3 = new DiaOption("VisitorsArrivedRefuseUntilBeds".Translate());
                diaOption3.action = () => {
                    GuestUtility.RefuseGuestsUntilWeHaveBeds(map);
                    refuse();
                };
                diaOption3.resolveTree = true;
                diaNode.options.Add(diaOption3);
            }

            var location = ((MapParent) map.ParentHolder).Label;
            string title = "VisitorsArrivedTitle".Translate(location, spawnDirection.LabelShort());
            Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true, true, title));
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryResolveParms(parms)) return false;

            Map map = parms.target as Map;

            // Is map not available anymore?
            if (map == null) return true;

            if (parms.points < 40)
            {
                Log.ErrorOnce("Trying to spawn visitors, but points are too low.", 9827456);
                return false;
            }

            if (parms.faction == null)
            {
                Log.ErrorOnce("Trying to spawn visitors, but couldn't find valid faction.", 43638973);
                return false;
            }
            if (!parms.spawnCenter.IsValid)
            {
                Log.ErrorOnce("Trying to spawn visitors, but could not find a valid spawn point.", 94839643);
                return false;
            }

            if (parms.faction == Faction.OfPlayer)
            {
                Log.ErrorOnce("Trying to spawn visitors, but they are of Faction.OfPlayer.", 3464363);
                return true;
            }
            if (parms.faction.RelationWith(Faction.OfPlayer).kind == FactionRelationKind.Hostile)
            {
                Log.ErrorOnce("Trying to spawn visitors, but they are hostile to the player (now).", 4736345);
                return true;
            }

            if (Settings.disableGuests || map.mapPawns.ColonistCount == 0)
            {
                GenericUtility.PlanNewVisit(map, Rand.Range(5f, 25f), parms.faction);
            }
            else
            {
                // Did the player refuse guests until beds are made and there are no beds yet?
                if (!GuestUtility.BedCheck(map))
                {
                    GenericUtility.PlanNewVisit(map, Rand.Range(2f, 5f), parms.faction);
                    return true;
                }

                // We check here instead of CanFireNow, so we can reschedule the visit.
                // Any reasons not to come?
                if (CheckCanCome(map, parms.faction, out var reasons))
                {
                    // No, spawn
                    return SpawnGroup(parms, map);
                }

                // Yes, ask the player for permission
                var spawnDirection = GetSpawnDirection(map, parms.spawnCenter);
                ShowAskMayComeDialog(parms.faction, map, reasons, spawnDirection,
                    // Permission, spawn
                    () => SpawnGroup(parms, map),
                    // No permission, come again later
                    () => { GenericUtility.PlanNewVisit(map, Rand.Range(2f, 5f), parms.faction); });
            }
            return true;
        }

        private static Direction8Way GetSpawnDirection(Map map, IntVec3 position)
        {
            var offset = map.Center - position;
            var angle = (offset.AngleFlat+180)%360;

            const float step = 360/16f;
            if(angle < 1*step) return Direction8Way.North;
            if(angle < 3*step) return Direction8Way.NorthEast;
            if(angle < 5*step) return Direction8Way.East;
            if(angle < 7*step) return Direction8Way.SouthEast;
            if(angle < 9*step) return Direction8Way.South;
            if(angle < 11*step) return Direction8Way.SouthWest;
            if(angle < 13*step) return Direction8Way.West;
            if(angle < 15*step) return Direction8Way.NorthWest;
            return Direction8Way.North;
        }

        private bool SpawnGroup(IncidentParms parms, Map map)
        {
            List<Pawn> visitors;
            try
            {
                //Log.Message(string.Format("Spawning visitors from {0}, at {1}.", parms.faction, parms.spawnCenter));
                visitors = SpawnPawns(parms);

                CheckVisitorsValid(visitors);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("Something failed when spawning visitors: " + e.Message + "\n" + e.StackTrace, 464365853);
                GenericUtility.PlanNewVisit(map, Rand.Range(1f, 3f), parms.faction);
                return true; // be gone, event
            }
            if (visitors == null || visitors.Count == 0) return false;

            foreach (var visitor in visitors)
            {
                GuestUtility.AddNeedJoy(visitor);
                GuestUtility.AddNeedComfort(visitor);
                visitor.FixTimetable();
                visitor.FixDrugPolicy();
                //Log.Message(visitor.NameStringShort + ": "
                //            + visitor.drugs.CurrentPolicy[ThingDefOf.Luciferium].allowedForJoy);
            }

            var spot = GetSpot(map, visitors.First().GetGuestArea(), visitors.First().Position);

            if (!spot.IsValid)
            {
                Log.ErrorOnce("Visitors failed to find a valid travel target.", 827358325);
                foreach (var visitor in visitors)
                {
                    visitor.DestroyOrPassToWorld();
                }
                GenericUtility.PlanNewVisit(map, Rand.Range(1f, 3f), parms.faction);
                return false;
            }
            
            GiveItems(visitors);

            CreateLord(parms.faction, spot, visitors, map);
            return true;
        }

        private static List<Pawn> GetKnownPawns(IncidentParms parms)
        {
            return Find.WorldPawns.AllPawnsAlive.Where(pawn => ValidGuest(pawn, parms.faction)).ToList();
        }

        private static bool ValidGuest(Pawn pawn, Faction faction)
        {
            var validGuest = !pawn.Discarded && !pawn.Dead && !pawn.Spawned && !pawn.NonHumanlikeOrWildMan() && !pawn.Downed && pawn.Faction == faction;
            // Leader only comes when relations are good
            if (faction.leader == pawn && faction.PlayerGoodwill < 80) return false;

            return validGuest;
        }

        protected override void ResolveParmsPoints(IncidentParms parms)
        {
            if (parms.points < 0f)
            {
                parms.points = Rand.ByCurve(pointsCurve);
            }
        }

        protected new List<Pawn> SpawnPawns(IncidentParms parms)
        {
            var map = (Map)parms.target;
            var options = GetKnownPawns(parms);

            if (options.Count < 10)
            {
                // Create some new people
                var newPawns = PawnGroupMakerUtility.GeneratePawns(IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDef, parms, true), false);
                Log.Message($"Created {newPawns.Count()} new pawns.");
                options.AddRange(newPawns);
            }

            options.Shuffle();

            var amount = GetGroupSize();

            var selection = options.Take(amount).ToList();
            var spawned = new List<Pawn>();
            foreach (var pawn in selection)
            {
                GenerateNewGearFor(pawn);
                if (pawn.IsWorldPawn()) Find.WorldPawns.RemovePawn(pawn);
                if (GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5), map) is Pawn spawnedPawn)
                {
                    spawnedPawn.needs.SetInitialLevels();
                    if (spawnedPawn.needs != null && spawnedPawn.needs.rest != null) {
                        spawnedPawn.needs.rest.CurLevel = Rand.Range(0.1f, 0.7f);
                    }
                    spawned.Add(spawnedPawn);
                }
            }
            return spawned;
        }

        protected virtual int GetGroupSize()
        {
            //Log.Message($"Optimal amount of guests = {OptimalAmount}, max = {OptimalAmount * 16f/6}");
            var random = Rand.GaussianAsymmetric(OptimalAmount, 1.5f, 16f / 6);
            var amount = Mathf.Clamp(Mathf.CeilToInt(random), Settings.minGuestGroupSize, Settings.maxGuestGroupSize);
            return amount;
        }

        /// <summary>
        /// From year 1-6, increase for 0 to 6 as the optimal amount
        /// </summary>
        private static float OptimalAmount => 1 + Mathf.Clamp(GenDate.YearsPassedFloat, 0f, 5f);

        private static void GenerateNewGearFor(Pawn pawn)
        {
            var request = new PawnGenerationRequest(pawn.kindDef, pawn.Faction);
            Traverse.Create(typeof(PawnGenerator)).Method("GenerateGearFor", pawn, request).GetValue();
        }

        private static void CheckVisitorsValid(List<Pawn> visitors)
        {
            foreach (var visitor in visitors)
            {
                if (visitor.TryGetComp<CompGuest>() != null) continue;

                try
                {
                    var humanlike = (visitor.def.race.Humanlike ? "humanlike" : "not humanlike");
                    var modName = visitor.def.modContentPack == null ? "vanilla (?)" : visitor.def.modContentPack.Name;

                    Log.Error($"Spawned visitor without GuestComp: {visitor.def.defName} - {humanlike} - {modName}");
                }
                catch
                {
                    Log.Error($"Failed to get more information about {visitor.Label}.");
                }

                visitors.Remove(visitor);
                visitor.Destroy();
            }
        }

        private static IntVec3 GetSpot(Map map, Area guestArea, IntVec3 startPos)
        {
            if(map == null) throw new NullReferenceException("map is null!");
            if(map.reachability == null) throw new NullReferenceException("map.reachability is null!");

            List<IntVec3> cells = new List<IntVec3>();
            GetSpotAddGuestArea(map, guestArea, cells);

            GetSpotAddDropSpots(map, cells);

            // Prefer roofed
            foreach (var cell in cells)
            {
                if (cell.IsValid && cell.Roofed(map) && map.reachability.CanReach(startPos, cell, PathEndMode.OnCell, TraverseMode.PassDoors)) return cell;
            }
            // Otherwise not roofed
            foreach (var cell in cells)
            {
                if (cell.IsValid && map.reachability.CanReach(startPos, cell, PathEndMode.OnCell, TraverseMode.PassDoors)) return cell;
            }
            return IntVec3.Invalid;
        }

        private static void GetSpotAddDropSpots(Map map, List<IntVec3> cells)
        {
            var tradeDropSpot = DropCellFinder.TradeDropSpot(map);
            cells.Add(tradeDropSpot);

            if (tradeDropSpot.IsValid && DropCellFinder.TryFindDropSpotNear(tradeDropSpot, map, out var near, false, false)) cells.Add(near);
            cells.Add(DropCellFinder.RandomDropSpot(map));
        }

        private static void GetSpotAddGuestArea(Map map, Area guestArea, List<IntVec3> cells)
        {
            if(map.areaManager == null) throw new NullReferenceException("map.areaManager is null!");

            if (guestArea?.ActiveCells.Any() != true) guestArea = Hospitality_MapComponent.Instance(map).defaultAreaRestriction;
            if (guestArea?.ActiveCells.Any() != true) guestArea = map.areaManager.Home;

            cells.AddRange(guestArea.ActiveCells);
        }

        private static void GiveItems(IEnumerable<Pawn> visitors)
        {
            foreach (var visitor in visitors)
            {
                PawnInventoryGenerator.GiveRandomFood(visitor);
                if(Rand.Value < 0.5f) visitor.TryGiveBackpack();


                float totalValue = 0;

                // Money
                //Log.Message("Goodwill: "+visitor.Faction.ColonyGoodwill);
                var amountS = Mathf.RoundToInt(Rand.Gaussian(visitor.Faction.PlayerGoodwill, visitor.Faction.PlayerGoodwill)*2)+Rand.Range(0, 50);
                if (amountS > Rand.Range(10, 50))
                {
                    var money = CreateRandomItem(visitor, ThingDefOf.Silver);
                    money.stackCount = amountS;

                    var spaceFor = visitor.GetInventorySpaceFor(money);
                    if (spaceFor > 0)
                    {
                        money.stackCount = Mathf.Min(spaceFor, amountS);
                        var success = visitor.inventory.innerContainer.TryAdd(money);
                        if (success) totalValue += money.MarketValue*money.stackCount;
                        else if(!money.Destroyed) money.Destroy();
                    }
                }



                // Items
                float maxValue = (visitor.Faction.PlayerGoodwill + 10)*Rand.Range(3, 5);
                float value = maxValue - totalValue;
                int curCount = 0;
                while (value > 100 && curCount < 200)
                {
                    //Log.Message("Total is now " + totalValue + ", space left is " + space);
                    curCount++;

                    bool apparel = Rand.Value < 0.5f;
                    ThingDef thingDef;
                    do thingDef = GetRandomItem(visitor.Faction.def.techLevel); 
                    while (thingDef != null && apparel && thingDef.IsApparel);
                    if (thingDef == null) break;

                    //var amount = Mathf.Min(Mathf.RoundToInt(Mathf.Abs(Rand.Gaussian(1, thingDef.stackLimit/2f))),
                    //    thingDef.stackLimit);
                    //if (amount <= 0) continue;

                    var item = CreateRandomItem(visitor, thingDef);

                    //Log.Message(item.Label + " has this value: " + item.MarketValue);
                    if (item.Destroyed) continue;

                    if (item.MarketValue >= value)
                    {
                        item.Destroy();
                        continue;
                    }
                    
                    if (item.MarketValue < 1)
                    {
                        item.Destroy();
                        continue;
                    }
                    var uniquesAmount = item.TryGetComp<CompQuality>() != null ? 1 : item.def.stackLimit;
                    var maxItems = Mathf.Min(Mathf.FloorToInt(value/item.MarketValue), item.def.stackLimit, uniquesAmount);
                    var minItems = Mathf.Max(1, Mathf.CeilToInt(Rand.Range(50,100)/item.MarketValue));

                    if (maxItems < 1 || minItems > maxItems)
                    {
                        item.Destroy();
                        continue;
                    }

                    //Log.Message("Can fit " + maxItems+" of "+item.Label);
                    item.stackCount = Rand.RangeInclusive(minItems, maxItems);
                    //Log.Message("Added " + item.stackCount + " with a value of " + (item.MarketValue * item.stackCount));

                    var spaceFor = visitor.GetInventorySpaceFor(item);
                    if (spaceFor > 0)
                    {
                        item.stackCount = Mathf.Min(spaceFor, item.stackCount);
                        var success = visitor.inventory.innerContainer.TryAdd(item);
                        if (success) totalValue += item.MarketValue*item.stackCount;
                        else if(!item.Destroyed) item.Destroy();
                    }
                    value = maxValue - totalValue;
                }
            }
        }

        private static Thing CreateRandomItem(Pawn visitor, ThingDef thingDef)
        {
            ThingDef stuff = GenStuff.RandomStuffFor(thingDef);
            var item = ThingMaker.MakeThing(thingDef, stuff);
            item.stackCount = 1;

            CompQuality compQuality = item.TryGetComp<CompQuality>();
            if (compQuality != null)
            {
                compQuality.SetQuality(QualityUtility.GenerateQualityTraderItem(), ArtGenerationContext.Outsider);
            }
            if (item.def.Minifiable)
            {
                item = item.MakeMinified();
            }
            if (item.def.useHitPoints)
            {
                // Make sure health is at least 60%. Otherwise too expensive items can become gifts.
                const float minHealthPct = 0.6f;
                var healthRange = visitor.kindDef.gearHealthRange;
                healthRange.min = minHealthPct;
                healthRange.max = Mathf.Max(minHealthPct, healthRange.max);

                var healthPct = healthRange.RandomInRange;
                if (healthPct < 1)
                {
                    int num = Mathf.RoundToInt(healthPct * item.MaxHitPoints);
                    num = Mathf.Max(1, num);
                    item.HitPoints = num;
                }
            }
            return item;
        }

        private static ThingDef GetRandomItem(TechLevel techLevel)
        {
            if (_items == null)
            {
                Predicate<ThingDef> qualifies =
                    d =>
                        d.category == ThingCategory.Item && d.EverStorable(true) && d.alwaysHaulable
                        && d.thingClass != typeof(MinifiedThing) && d.tradeability != Tradeability.None
                        && d.GetCompProperties<CompProperties_Hatcher>() == null;
                _items = DefDatabase<ThingDef>.AllDefs.Where(d => qualifies(d)).ToArray();
                //highestValue = _items.Max(i => i.BaseMarketValue);
            }
            ThingDef def;
            return _items.Where(thingDef => thingDef.techLevel <= Increase(techLevel)).TryRandomElementByWeight(thingDef => TechLevelDiff(thingDef.techLevel, techLevel), out def)
                ? def
                : null;
            //return _items.RandomElementByWeight(i => highestValue + 50 - i.BaseMarketValue);
        }

        private static TechLevel Increase(TechLevel techLevel)
        {
            return techLevel+1;
        }

        private static float TechLevelDiff(TechLevel def, TechLevel target)
        {
            return (float) TechLevel.Ultra - Mathf.Abs((float) target - (float) def);
        }

        private static void CreateLord(Faction faction, IntVec3 chillSpot, List<Pawn> pawns, Map map)
        {
            var mapComp = Hospitality_MapComponent.Instance(map);

            int stayDuration = (int)(Rand.Range(1f, 2.4f) * GenDate.TicksPerDay);
            var lordJob = new LordJob_VisitColony(faction, chillSpot, stayDuration);
            var lord = LordMaker.MakeNewLord(faction, lordJob, map, pawns);


            // Set default interaction
            pawns.ForEach(delegate(Pawn p) {
                var compGuest = p.GetComp<CompGuest>();
                if (compGuest != null)
                {
                    compGuest.ResetForGuest(lord);
                    compGuest.chat = mapComp.defaultInteractionMode == PrisonerInteractionModeDefOf.ReduceResistance;
                    compGuest.GuestArea = mapComp.defaultAreaRestriction;
                    compGuest.ShoppingArea = mapComp.defaultAreaShopping;
                }
            });

            bool gotTrader = false;
            int traderIndex = 0;
            if (Rand.Value < 0.75f)
            {
                gotTrader = TryConvertOnePawnToSmallTrader(pawns, faction, map, out traderIndex);
            }
            Pawn pawn = pawns.Find((Pawn x) => faction.leader == x);
            string label;
            string description;
            if (pawns.Count == 1)
            {
                string value = (!gotTrader) ? string.Empty : ("\n\n" + "SingleVisitorArrivesTraderInfo".Translate(pawns[0].Named("PAWN")).AdjustedFor(pawns[0], "PAWN"));
                string value2 = (pawn == null) ? string.Empty : ("\n\n" + "SingleVisitorArrivesLeaderInfo".Translate(pawns[0].Named("PAWN")).AdjustedFor(pawns[0], "PAWN"));
                label = "LetterLabelSingleVisitorArrives".Translate();
                description = "SingleVisitorArrives".Translate(pawns[0].story.Title, faction.Name, pawns[0].Name.ToStringFull, value, value2, pawns[0].Named("PAWN")).AdjustedFor(pawns[0], "PAWN");
            }
            else
            {
                string value3 = (!gotTrader) ? string.Empty : ("\n\n" + "GroupVisitorsArriveTraderInfo".Translate());
                string value4 = (pawn == null) ? string.Empty : ("\n\n" + "GroupVisitorsArriveLeaderInfo".Translate(pawn.LabelShort, pawn));
                label = "LetterLabelGroupVisitorsArrive".Translate();
                description = "GroupVisitorsArrive".Translate(faction.Name, value3, value4);
            }

            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(pawns, ref label, ref description, "LetterRelatedPawnsNeutralGroup".Translate(Faction.OfPlayer.def.pawnsPlural), true, true);
            // NEW
            //Find.LetterStack.ReceiveLetter(label, description, LetterDefOf.NeutralEvent, pawns[0], faction, null);
            var lookTarget = gotTrader ? pawns[traderIndex] : pawns[0];
            Find.LetterStack.ReceiveLetter(label, description, LetterDefOf.PositiveEvent, lookTarget, faction);
        }

        private static bool TryConvertOnePawnToSmallTrader(List<Pawn> pawns, Faction faction, Map map, out int traderIndex)
        {
            if (faction.def.visitorTraderKinds.NullOrEmpty())
            {
                traderIndex = 0;
                return false;
            }
            Pawn pawn = pawns.RandomElement();
            Lord lord = pawn.GetLord();
            pawn.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);
            TraderKindDef traderKindDef = faction.def.visitorTraderKinds.RandomElementByWeight(traderDef => traderDef.commonality);
            pawn.trader.traderKind = traderKindDef;
            pawn.inventory.DestroyAll();

            pawn.TryGiveBackpack();

            var parms = default(ThingSetMakerParams);
            parms.traderDef = traderKindDef;
            parms.tile = map.Tile;
            parms.traderFaction = faction;

            foreach (Thing current in ThingSetMakerDefOf.TraderStock.root.Generate(parms))
            {
                Pawn slave = current as Pawn;
                if (slave != null)
                {
                    if (slave.Faction != pawn.Faction)
                    {
                        slave.SetFaction(pawn.Faction);
                    }
                    IntVec3 loc = CellFinder.RandomClosewalkCellNear(pawn.Position, lord.Map, 5);
                    GenSpawn.Spawn(slave, loc, lord.Map);
                    lord.AddPawn(slave);
                }
                else
                {
                    var spaceFor = pawn.GetInventorySpaceFor(current);

                    if (current.Destroyed) continue;
                    if (spaceFor <= 0)
                    {
                        current.Destroy();
                        continue;
                    }
                    current.stackCount = spaceFor;

                    // Core stuff
                    if (!pawn.inventory.innerContainer.TryAdd(current))
                    {
                        current.Destroy();
                    }
                }
            }
            traderIndex = pawns.IndexOf(pawn);
            return true;
        }
    }
}

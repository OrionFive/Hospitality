using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    // Note that this implementation is VERY different from vanilla
    public class IncidentWorker_VisitorGroup : IncidentWorker_NeutralGroup
    {
        private static ThingDef[] _items;

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
            if (parms.faction.RelationWith(Faction.OfPlayer).hostile)
            {
                Log.ErrorOnce("Trying to spawn visitors, but they are hostile to the player (now).", 4736345);
                return true;
            }

            if (Settings.disableGuests || map.mapPawns.ColonistCount == 0)
            {
                GuestUtility.PlanNewVisit(map, Rand.Range(5f, 25f), parms.faction);
            }
            else
            {
                string reasons;
                // We check here instead of CanFireNow, so we can reschedule the visit.
                // Any reasons not to come?
                if (CheckCanCome(map, parms.faction, out reasons))
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
                    () => { GuestUtility.PlanNewVisit(map, Rand.Range(2f, 5f), parms.faction); });
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
                GuestUtility.PlanNewVisit(map, Rand.Range(1f, 3f), parms.faction);
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
                visitor.GetComp<CompGuest>().sentAway = false;
            }

            var spot = GetSpot(map, visitors.First().GetGuestArea());

            if (!spot.IsValid)
            {
                Log.ErrorOnce("Visitors failed to find a valid travel target.", 827358325);
                foreach (var visitor in visitors)
                {
                    visitor.DestroyOrPassToWorld();
                }
                return false;
            }
            
            GiveItems(visitors);

            CreateLord(parms.faction, spot, visitors, map);
            return true;
        }

        protected new List<Pawn> SpawnPawns(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            var list = PawnGroupMakerUtility.GeneratePawns(PawnGroupKindDef, IncidentParmsUtility.GetDefaultPawnGroupMakerParms(parms), false)
                .Take(Settings.maxGuestGroupSize) // Added
                .ToList();
            foreach (Pawn newThing in list)
                GenSpawn.Spawn(newThing, CellFinder.RandomClosewalkCellNear(parms.spawnCenter, map, 5), map);
            return list;
        }

        private static void CheckVisitorsValid(List<Pawn> visitors)
        {
            if (visitors.Any(v => v.TryGetComp<CompGuest>() == null))
            {
                foreach (var visitor in visitors)
                {
                    visitor.Destroy();
                }
                throw new Exception("Spawned visitors without GuestComp.");
            }
        }

        private static IntVec3 GetSpot(Map map, Area guestArea)
        {
            var area = guestArea;

            if (area == null) return DropCellFinder.TradeDropSpot(map);

            var cells = area.ActiveCells.Where(c => c.Walkable(map) && c.Roofed(map));

            if (!cells.Any()) return DropCellFinder.TradeDropSpot(map);
            
            return cells.RandomElement();
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
                compQuality.SetQuality(QualityUtility.RandomGeneratedGearQuality(visitor.kindDef), ArtGenerationContext.Outsider);
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
                        d.category == ThingCategory.Item && d.EverStoreable && d.alwaysHaulable
                        && d.thingClass != typeof(MinifiedThing) && d.tradeability != Tradeability.Never
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
            return (float) TechLevel.Transcendent - Mathf.Abs((float) target - (float) def);
        }

        private static void CreateLord(Faction faction, IntVec3 chillSpot, List<Pawn> pawns, Map map)
        {
            var mapComp = Hospitality_MapComponent.Instance(map);

            int stayDuration = (int)(Rand.Range(1f, 2.4f) * GenDate.TicksPerDay);
            var lordJob = new LordJob_VisitColony(faction, chillSpot, stayDuration);
            LordMaker.MakeNewLord(faction, lordJob, map, pawns);


            // Set default interaction
            pawns.ForEach(delegate(Pawn p) {
                var compGuest = p.GetComp<CompGuest>();
                if (compGuest != null)
                {
                    compGuest.chat = mapComp.defaultInteractionMode == PrisonerInteractionModeDefOf.Chat;
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
            string label;
            string description;
            Pawn pawn = pawns.Find(x => faction.leader == x);
            if (pawns.Count == 1)
            {
                string traderDesc = (!gotTrader) ? string.Empty : "SingleVisitorArrivesTraderInfo".Translate();
                string leaderDesc = (pawn == null) ? string.Empty : "SingleVisitorArrivesLeaderInfo".Translate();
                label = "LetterLabelSingleVisitorArrives".Translate();
                description = "SingleVisitorArrives".Translate(new object[]
				{
					pawns[0].GetTitle().ToLower(),
					faction.Name,
					pawns[0].Name,
					traderDesc,
                    leaderDesc
				});
                description = description.AdjustedFor(pawns[0]);
            }
            else
            {
                string traderDesc = (!gotTrader) ? string.Empty : "GroupVisitorsArriveTraderInfo".Translate();
 				string leaderDesc = (pawn == null) ? string.Empty : "GroupVisitorsArriveLeaderInfo".Translate(new object[] {pawn.LabelShort});
                label = "LetterLabelGroupVisitorsArrive".Translate();
                description = "GroupVisitorsArrive".Translate(new object[]
				{
					faction.Name,
					traderDesc,
                    leaderDesc
				});
            }
            var lookTarget = gotTrader ? pawns[traderIndex] : pawns[0];
            Find.LetterStack.ReceiveLetter(label, description, LetterDefOf.PositiveEvent, lookTarget);
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

            var parms = default(ItemCollectionGeneratorParams);
            parms.traderDef = traderKindDef;
            parms.tile = map.Tile;
            parms.traderFaction = faction;

            foreach (Thing current in ItemCollectionGeneratorDefOf.TraderStock.Worker.Generate(parms))
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

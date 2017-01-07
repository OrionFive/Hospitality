using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Hospitality
{
    // Note that this implementation is VERY different from vanilla
    public class IncidentWorker_VisitorGroup : IncidentWorker_NeutralGroup
    {
        private static readonly RoomRoleDef _roomRoleDefGuestRoom = DefDatabase<RoomRoleDef>.GetNamed("GuestRoom");
        private static ThingDef[] _items;
        private static float highestValue;

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

        private static bool CheckCanCome(Map map, Faction faction)
        {
            bool fallout = map.mapConditionManager.GetActiveCondition<MapCondition_ToxicFallout>() != null;
            var hostiles = map.mapPawns.AllPawnsSpawned.Where(p => !p.Dead && !p.IsPrisoner && p.Faction != null && !p.Downed
                                                                   && (p.Faction.HostileTo(Faction.OfPlayer) || p.Faction.HostileTo(faction)) && p.Faction != Faction.OfInsects);

            //if (Find.MapConditionManager.GetActiveCondition<MapCondition_VolcanicWinter>() != null) canFire = false;
            bool noRooms = GetRooms(null, map).Length == 0;

            if (!noRooms && !fallout && !hostiles.Any()) return true;
            // TODO: Show messages explaining why they can't come
            return false;
        }

        public override bool TryExecute(IncidentParms parms)
        {
            if (!TryResolveParms(parms)) return false;
            
            Map map = (Map)parms.target;

            // We check here instead of CanFireNow, so we can reschedule the visit.
            if (!CheckCanCome(map, parms.faction))
            {
                GuestUtility.PlanNewVisit(map, Rand.Range(1f, 3f), parms.faction);
                return false;
            }

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
            List<Pawn> visitors;
            try
            {
                //Log.Message(string.Format("Spawning visitors from {0}, at {1}.", parms.faction, parms.spawnCenter));
                visitors = SpawnPawns(parms);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("Something failed when spawning visitors: " + e.Message+"\n"+e.StackTrace, 464365853);
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

            var rooms = GetRooms(visitors[0], map);
            if (rooms.Length > 0)
            {
                var spot = rooms[0].room.Cells.Where(c=>c.Roofed(map)).RandomElement();

                GiveItems(visitors);

                CreateLord(parms.faction, spot, visitors, map);

                return true;
            }
            return false;
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
                        var success = visitor.inventory.GetInnerContainer().TryAdd(money);
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
                        var success = visitor.inventory.GetInnerContainer().TryAdd(item);
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
                float randomInRange = visitor.kindDef.gearHealthRange.RandomInRange;
                if (randomInRange < 1f)
                {
                    int num = Mathf.RoundToInt(randomInRange*item.MaxHitPoints);
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
                        && d.thingClass != typeof (MinifiedThing) && d.tradeability != Tradeability.Never;
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
            var lordJob = new LordJob_VisitColony(faction, chillSpot);
            LordMaker.MakeNewLord(faction, lordJob, map, pawns);

            // Set default interaction
            pawns.ForEach(delegate(Pawn p) {
                var comp = p.GetComp<CompGuest>();
                if (comp != null)
                {
                    comp.chat = Hospitality_MapComponent.Instance(map).defaultInteractionMode == PrisonerInteractionMode.Chat;
                }
            });

            bool gotTrader = false;
            if (Rand.Value < 0.8f)
            {
                gotTrader = TryConvertOnePawnToSmallTrader(pawns, faction);
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
            Find.LetterStack.ReceiveLetter(label, description, LetterType.Good, pawns[0]);
        }

        private static bool TryConvertOnePawnToSmallTrader(List<Pawn> pawns, Faction faction)
        {
            if (faction.def.visitorTraderKinds.NullOrEmpty())
            {
                return false;
            }
            Pawn pawn = pawns.RandomElement();
            Lord lord = pawn.GetLord();
            pawn.mindState.wantsToTradeWithColony = true;
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, true);
            TraderKindDef traderKindDef = faction.def.visitorTraderKinds.RandomElement();
            pawn.trader.traderKind = traderKindDef;
            pawn.inventory.DestroyAll();

            pawn.TryGiveBackpack();

            foreach (Thing current in TraderStockGenerator.GenerateTraderThings(traderKindDef, lord.Map))
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
                    if (!pawn.inventory.GetInnerContainer().TryAdd(current))
                    {
                        current.Destroy();
                    }
                }
            }
            return true;
        }

        private struct RoomAndScore
        {
            public Room room;
            public float score;
        }

        private static RoomAndScore[] GetRooms(Pawn searcher, Map map)
        {
            var rooms = new HashSet<Room>();
            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                var room = RoomQuery.RoomAtFast(building.Position, map);
                // Only check one cell per room - otherwise this may be way too expensive (e.g. far away room, when colony is closed off)
                if (room != null && room.CellCount > 8 && !room.PsychologicallyOutdoors && map.reachability.CanReachColony(room.Cells.First(cell => cell.Walkable(map))))
                {
                    rooms.Add(room);
                }
            }
            return
                rooms.Select(room => new RoomAndScore {room = room, score = RoomScore(room, searcher)})
                    .OrderByDescending(r => r.score)
                    .ToArray();
        }

        private static float RoomScore(Room room, Pawn searcher)
        {
            int score = 0;
            score += (int) (room.GetStat(RoomStatDefOf.Impressiveness)*1.5); // -150 to 215
            score += Math.Min((int) room.GetStat(RoomStatDefOf.Space), 40);

            if (room.Role == _roomRoleDefGuestRoom) score += 500;
            else if (room.Role == RoomRoleDefOf.Barracks) score -= 100;
            else if (room.Role == RoomRoleDefOf.Bedroom) score -= 500;
            else if (room.Role == RoomRoleDefOf.DiningRoom) score += 50;
            else if (room.Role == RoomRoleDefOf.Hospital) score -= 50;
            else if (room.Role == RoomRoleDefOf.Laboratory) score -= 100;
            else if (room.Role == RoomRoleDefOf.PrisonBarracks) score -= 150;
            else if (room.Role == RoomRoleDefOf.PrisonCell) score -= 150;
            else if (room.Role == RoomRoleDefOf.RecRoom) score += 50;

            var flags = room.AllContainedThings.OfType<VisitorFlag>().Count();
            score -= flags*500;

            if (room.UsesOutdoorTemperature) score -= 150;

            if (searcher != null && !searcher.ComfortableTemperatureRange().Includes(room.Temperature)) score -= 75;

            if (room.IsHuge) return score;

            // count beds
            foreach (var bed in room.ContainedBeds)
            {
                if (!bed.def.building.bed_humanlike) score -= 25;
                else if (bed.owners.Count > 0) score -= 50;
            }
            score += room.AllContainedThings.OfType<Building_GuestBed>().Sum(buildingGuestBed => 50);

            return score;
        }
    }
}

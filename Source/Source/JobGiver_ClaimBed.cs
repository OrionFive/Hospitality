using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using static UnityEngine.Mathf;
using Log = Verse.Log;

namespace Hospitality
{
    public class JobGiver_ClaimBed : ThinkNode_JobGiver
    {
        private static RoomRoleDef GuestRoomRoleDef => DefDatabase<RoomRoleDef>.GetNamed("GuestRoom");
        private static JobDef JobDef => DefDatabase<JobDef>.GetNamed("ClaimGuestBed");

        protected override Job TryGiveJob(Pawn guest)
        {
            var guestComp = guest.GetComp<CompGuest>();
            if (guestComp == null) return null;
            if (guestComp.HasBed) return null;

            if (GenTicks.TicksGame < guestComp.lastBedCheckTick + 2500) return null;
            
            guestComp.lastBedCheckTick = GenTicks.TicksGame;

            
            var silver = guest.inventory.innerContainer.FirstOrDefault(i => i.def == ThingDefOf.Silver);
            var money = silver?.stackCount ?? 0;

            var beds = FindAvailableBeds(guest, money);
            Log.Message($"Found {beds.Length} guest beds that {guest.LabelShort} can afford (<= {money} silver).");
            if (!beds.Any()) return null;

            var bed = SelectBest(beds, guest, money);
            return new Job(JobDef, bed) {takeExtraIngestibles = bed.rentalFee}; // Store rentalFee to avoid cheating
        }

        private static Building_GuestBed SelectBest(Building_GuestBed[] beds, Pawn guest, int money)
        {
            return beds.MaxBy(bed => BedValue(bed, guest, money));
        }

        private static float BedValue(Building_GuestBed bed, Pawn guest, int money)
        {
            // Stats
            var room = bed.GetRoom();
            //QualityCategory category;
            if(!bed.TryGetQuality(out QualityCategory category)) category = QualityCategory.Normal;
            var quality = ((int) category - 2) * 25; // -50 - 100
            var impressiveness = RoundToInt(room.GetStat(RoomStatDefOf.Impressiveness)); // 0 - 100 (and more)
            var fee = RoundToInt(money > bed.rentalFee ? 150*bed.rentalFee/money : 0); // 0 - 150
            var roomType = GetRoomTypeScore(room); // -50 - 50
            var otherPawnOpinion = bed.owners.Any() ? bed.owners.Where(owner => owner != guest).Sum(owner => guest.relations.OpinionOf(owner) - 15) : 0;
            var temperature = GetTemperatureScore(guest, room); // -200 - 0

            // Traits
            if (guest.story.traits.HasTrait(TraitDefOf.Greedy))
            {
                fee *= 2;
                impressiveness -= 50;
            }

            if (guest.story.traits.HasTrait(TraitDefOf.Kind))
                fee /= 2;

            if (guest.story.traits.HasTrait(TraitDefOf.Ascetic))
            {
                impressiveness *= -1;
                roomType = 75; // We don't care, so always max
            }

            if (guest.story.traits.HasTrait(TraitDef.Named("Jealous")))
            {
                fee /= 4;
                impressiveness -= 50;
                impressiveness *= 4;
            }

            var score = impressiveness + quality + roomType + temperature + otherPawnOpinion * 4;
            var value = score - fee;
            Log.Message($"For {guest.LabelShort} {bed.ThingID} has a score of {score} and value of {value}:\n"
                        + $"impressiveness = {impressiveness}, quality = {quality}, fee = {fee}, roomType = {roomType}, opinion = {otherPawnOpinion}, temperature = {temperature}");
            return value;
        }

        private static float GetTemperatureScore(Pawn guest, Room room)
        {
            var optimalTemperature = GenTemperature.ComfortableTemperatureRange(guest.def);
            var pctTemperature = Abs(optimalTemperature.InverseLerpThroughRange(room.Temperature) - 0.5f) * 2; // 0-1
            return RoundToInt(Lerp(0, -200, pctTemperature-0.75f)*4); // -200 - 0
        }

        private static int GetRoomTypeScore(Room room)
        {
            if (room.OutdoorsForWork) return -50;

            int roomType;
            if (room.Role == RoomRoleDefOf.Barracks) roomType = 0;
            else if (room.Role == GuestRoomRoleDef) roomType = 20;
            else roomType = -30;
            if (OnlyOneBed(room)) roomType += 30;
            return roomType;
        }

        private static bool OnlyOneBed(Room room)
        {
            return room.ContainedBeds.Count() <= 1;
        }

        private static Building_GuestBed[] FindAvailableBeds(Pawn guest, int money)
        {
            var beds = guest.MapHeld.GetGuestBeds(guest.GetGuestArea());
            return beds.Where(bed => bed.AnyUnownedSleepingSlot && bed.rentalFee <= money).ToArray();
        }
    }
}

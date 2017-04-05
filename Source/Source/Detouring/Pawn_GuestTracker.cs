using System.Reflection;
using HugsLib.Source.Detour;
using RimWorld;
using Verse;
using Verse.AI.Group;
using Source = RimWorld.Pawn_GuestTracker;

namespace Hospitality.Detouring
{
    internal static class Pawn_GuestTracker
    {
        [DetourMethod(typeof(Source), "Notify_PawnUndowned")]
        internal static void Notify_PawnUndowned(this Source _this)
        {
            // Just do nothing. We do the check somewhere else. Here is bad, because if the player rejects, the pawn will hang around way too long.
        } 
        
        // Detoured so guests don't become prisoners
        [DetourMethod(typeof(Source), "SetGuestStatus")]
        public static void SetGuestStatus(this Source _this, Faction newHost, bool prisoner = false)
        {
            // Added
            var pawn = (Pawn)typeof(Source).GetField("pawn", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_this);
            var fieldHostFactionInt = typeof(Source).GetField("hostFactionInt", BindingFlags.Instance | BindingFlags.NonPublic);

            // Added
            if (pawn != null && pawn.IsGuest()) prisoner = false;
            
            if (newHost != null)
            {
                _this.released = false;
            }
            if (newHost == _this.HostFaction && prisoner == _this.IsPrisoner)
            {
                return;
            }

            
            if (!prisoner && pawn.Faction.HostileTo(newHost))
            {
                Log.Error(string.Concat(new object[]
		{
			"Tried to make ",
			pawn,
			" a guest of ",
			newHost,
			" but their faction ",
			pawn.Faction,
			" is hostile to ",
			newHost
		}));
                return;
            }
            if (newHost == pawn.Faction && !prisoner)
            {
                Log.Error(string.Concat(new object[]
		{
			"Tried to make ",
			pawn,
			" a guest of their own faction ",
			pawn.Faction
		}));
                return;
            }
            bool flag = prisoner && (!_this.IsPrisoner || _this.HostFaction != newHost);
            _this.isPrisonerInt = prisoner;
            //_this.hostFactionInt = newHost;
            fieldHostFactionInt.SetValue(_this, newHost); // Changed

            pawn.ClearMind(false);
            pawn.ClearReservations();
            if (flag)
            {
                pawn.DropAndForbidEverything(false);
                Lord lord = pawn.GetLord();
                if (lord != null)
                {
                    lord.Notify_PawnLost(pawn, PawnLostCondition.MadePrisoner);
                }
                if (pawn.Drafted)
                {
                    pawn.drafter.Drafted = false;
                }
            }
            PawnComponentsUtility.AddAndRemoveDynamicComponents(pawn, false);
            pawn.health.surgeryBills.Clear();
            if (pawn.ownership != null)
            {
                pawn.ownership.Notify_ChangedGuestStatus();
            }
            ReachabilityUtility.ClearCache();
            if (pawn.Spawned)
            {
                pawn.Map.mapPawns.UpdateRegistryForPawn(pawn);
                pawn.Map.attackTargetsCache.UpdateTarget(pawn);
            }
            AddictionUtility.CheckDrugAddictionTeachOpportunity(pawn);
            if (prisoner && pawn.playerSettings != null)
            {
                pawn.playerSettings.Notify_MadePrisoner();
            }
        }

    }
}
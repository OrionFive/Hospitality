using HarmonyLib;
using Multiplayer.API;
using Verse;

namespace Hospitality
{
    [StaticConstructorOnStartup]
    internal static class Multiplayer
    {
        internal static ISyncField[] guestFields;
        internal static ISyncField[] mapFields;
        static Multiplayer()
        {
            if (!MP.enabled) return;

            // CompGuest
            guestFields = new [] {
                MP.RegisterSyncField(typeof(CompGuest), "guestArea_int").SetBufferChanges(),
                MP.RegisterSyncField(typeof(CompGuest), "shoppingArea_int").SetBufferChanges(),
                MP.RegisterSyncField(typeof(CompGuest), nameof(CompGuest.makeFriends)),
                MP.RegisterSyncField(typeof(CompGuest), nameof(CompGuest.entertain)),
                MP.RegisterSyncField(typeof(CompGuest), nameof(CompGuest.sentAway)),
            };

            // Hospitality_MapComponent
            mapFields = new [] {
                MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.defaultMakeFriends)),
                MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.defaultEntertain)),
                MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.guestsAreWelcome)),
                MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.defaultAreaRestriction)).SetBufferChanges(),
                MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.defaultAreaShopping)).SetBufferChanges(),
                MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_MapComponent.refuseGuestsUntilWeHaveBeds)),
                MP.RegisterSyncField(typeof(Hospitality_MapComponent), nameof(Hospitality_GameComponent.defaultFoodRestriction)),
            };

            // Guest ITab
            {
                MP.RegisterSyncMethod(AccessTools.Method(typeof(ITab_Pawn_Guest), nameof(ITab_Pawn_Guest.SendHome)));
                MP.RegisterSyncMethod(AccessTools.Method(typeof(GuestUtility), nameof(GuestUtility.Recruit)));
                MP.RegisterSyncMethod(AccessTools.Method(typeof(ITab_Pawn_Guest), nameof(ITab_Pawn_Guest.SetAllDefaults)));
            }

            // Beds
            {
                MP.RegisterSyncMethod(AccessTools.Method(typeof(Building_GuestBed), nameof(Building_GuestBed.Swap)));
                MP.RegisterSyncMethod(AccessTools.Method(typeof(Building_GuestBed), nameof(Building_GuestBed.AdjustFee)));
            }
        }

        internal static bool IsRunning => MP.IsInMultiplayer;

        internal static void WatchBegin() => MP.WatchBegin();

        internal static void WatchEnd() => MP.WatchEnd();

        internal static void Watch(this ISyncField[] fields, object obj)
        {
            if (!MP.IsInMultiplayer) return;

            foreach(var field in fields)
            {
                field.Watch(obj);
            }
        }
    }
}


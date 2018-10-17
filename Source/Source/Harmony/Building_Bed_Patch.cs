using System.Collections.Generic;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace Hospitality.Harmony
{
    public class Building_Bed_Patch
    {
        [HarmonyPatch(typeof(Building_Bed))]
        [HarmonyPatch("ForPrisoners", MethodType.Setter)]
        public class ForPrisoners
        {
            [HarmonyPostfix]
            public static void Postfix(Building_Bed __instance)
            {
                if (!__instance.ForPrisoners) return;

                if (__instance is Building_GuestBed)
                {
                    Building_GuestBed.Swap(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Building_Bed), "GetGizmos")]
        public class GetGizmos
        {
            [HarmonyPostfix]
            public static void Postfix(Building_Bed __instance, ref IEnumerable<Gizmo> __result)
            {
                __result = Process(__instance, __result);
            }

            private static IEnumerable<Gizmo> Process(Building_Bed __instance, IEnumerable<Gizmo> __result)
            {
                if (!__instance.ForPrisoners && !__instance.Medical && __instance.def.building.bed_humanlike)
                {
                    yield return
                        new Command_Toggle
                        {
                            defaultLabel = "CommandBedSetAsGuestLabel".Translate(),
                            defaultDesc = "CommandBedSetAsGuestDesc".Translate(),
                            icon = ContentFinder<Texture2D>.Get("UI/Commands/AsGuest"),
                            isActive = () => false,
                            toggleAction = () => Building_GuestBed.Swap(__instance),
                            hotKey = KeyBindingDefOf.Misc4
                        };
                }
                foreach (var gizmo in __result)
                {
                    yield return gizmo;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse;

namespace TheThirdAge
{
    [StaticConstructorOnStartup]
    public static class RemoveModernStuffHarmony
    {
        static RemoveModernStuffHarmony()
        {
            HarmonyInstance harmony = HarmonyInstance.Create(id: "rimworld.removemodernstuff");

            harmony.Patch(original: AccessTools.Method(type: typeof(PawnUtility), name: "IsTravelingInTransportPodWorldObject"),
                prefix: new HarmonyMethod(type: typeof(RemoveModernStuffHarmony), name: nameof(IsTravelingInTransportPodWorldObject)), postfix: null);
        }

        //No one travels in transport pods in the medieval times
        public static bool IsTravelingInTransportPodWorldObject(Pawn pawn, ref bool __result)
        {
            if (RemoveModernStuff.MAX_TECHLEVEL <= TechLevel.Industrial)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
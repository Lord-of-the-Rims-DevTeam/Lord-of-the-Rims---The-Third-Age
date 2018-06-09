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


            foreach (Type type in typeof(ItemCollectionGenerator_Standard).AllSubclassesNonAbstract())
                harmony.Patch(original: AccessTools.Method(type: type, name: "Generate", parameters: new []{typeof(ItemCollectionGeneratorParams)}), prefix: new HarmonyMethod(type: typeof(RemoveModernStuffHarmony), name: nameof(ItemCollectionGeneratorGeneratePrefix)), postfix: null);
        }

        public static void ItemCollectionGeneratorGeneratePrefix(ref ItemCollectionGeneratorParams parms)
        {
            if (!parms.techLevel.HasValue || parms.techLevel > RemoveModernStuff.MAX_TECHLEVEL)
                parms.techLevel = RemoveModernStuff.MAX_TECHLEVEL;
        }

        //No one travels in transport pods in the medieval times
        // ReSharper disable once RedundantAssignment
        public static bool IsTravelingInTransportPodWorldObject(Pawn pawn, ref bool __result)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (RemoveModernStuff.MAX_TECHLEVEL <= TechLevel.Industrial)
            {
                __result = false;
                return false;
            }
            // ReSharper disable once HeuristicUnreachableCode
            #pragma warning disable 162
            return true;
            #pragma warning restore 162
        }
    }
}
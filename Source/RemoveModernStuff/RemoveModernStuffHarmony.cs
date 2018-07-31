using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using RimWorld;
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


            foreach (Type type in typeof(ThingSetMaker).AllSubclassesNonAbstract())
                harmony.Patch(original: AccessTools.Method(type: type, name: "Generate", parameters: new []{typeof(ThingSetMakerParams)}), prefix: new HarmonyMethod(type: typeof(RemoveModernStuffHarmony), name: nameof(ItemCollectionGeneratorGeneratePrefix)), postfix: null);

            IEnumerable<MethodInfo> mis = AgeInjuryUtilityNamesHandler();
            if (mis.Any())
            {
                Log.Message("..." + mis.Count() + " AgeInjuryUtility types found. Attempting address to harmony...");
                foreach (MethodInfo mi in mis)
                {
                    harmony.Patch(original: mi,
                                  prefix: null, 
                                  postfix: new HarmonyMethod(type: typeof(RemoveModernStuffHarmony), 
                                                             name: nameof(RandomPermanentInjuryDamageTypePostfix)));
                }
            } else
            {
                Log.Message("No AgeInjuryUtility found.");
            }

        }

        public static IEnumerable<MethodInfo> AgeInjuryUtilityNamesHandler()
        {
            //Log.Message("Looking for AgeInjuryUtility...");
            return (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                    from type in assembly.GetTypes()
                                    from method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                    where method.Name == "RandomPermanentInjuryDamageType"
                                    select method);
        }

        public static void RandomPermanentInjuryDamageTypePostfix(ref DamageDef __result)
        {
            if (__result == DamageDefOf.Bullet) {
                __result = DamageDefOf.Scratch;
                //Log.Message("Hello from RandomOldInjuryDamageTypePostfix.\nI heard you don't like Gunshot, so I fixed it.");
            }
        }

        public static void ItemCollectionGeneratorGeneratePrefix(ref ThingSetMakerParams parms)
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
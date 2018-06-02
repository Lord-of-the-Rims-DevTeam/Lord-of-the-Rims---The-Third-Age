using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace TheThirdAge
{
    [StaticConstructorOnStartup]
    public static class RemoveModernStuff
    {
        public const TechLevel maxTechLevel = TechLevel.Medieval;
        static int removedDefs;

        static RemoveModernStuff()
        {
            removedDefs = 0;
            IEnumerable<ResearchProjectDef> projects = DefDatabase<ResearchProjectDef>.AllDefs.Where(rpd => rpd.techLevel > maxTechLevel);
            IEnumerable<ThingDef> things = DefDatabase<ThingDef>.AllDefs.Where(td => td.techLevel > maxTechLevel || (td.researchPrerequisites?.Any(rpd => projects.Contains(rpd)) ?? false) || td.defName == "Gun_Revolver");
            
            RemoveStuff(typeof(DefDatabase<RecipeDef>), DefDatabase<RecipeDef>.AllDefs.Where(rd => rd.products.Any(tcc => things.Contains(tcc.thingDef)) || rd.AllRecipeUsers.All(td => things.Contains(td)) || projects.Contains(rd.researchPrerequisite)).Cast<Def>());
            RemoveStuff(typeof(DefDatabase<ResearchProjectDef>), projects.Cast<Def>());

            FieldInfo getThingInfo = typeof(ScenPart_ThingCount).GetField("thingDef", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (ScenarioDef def in DefDatabase<ScenarioDef>.AllDefs)
                foreach (ScenPart sp in def.scenario.AllParts)
                    if (typeof(ScenPart_ThingCount).IsAssignableFrom(sp.GetType()) && things.Contains((ThingDef) getThingInfo.GetValue(sp)))
                        def.scenario.RemovePart(sp);

            foreach (ThingCategoryDef thingCategoryDef in DefDatabase<ThingCategoryDef>.AllDefs)
                thingCategoryDef.childThingDefs.RemoveAll(things.Contains);

            ItemCollectionGeneratorUtility.allGeneratableItems.RemoveAll(things.Contains);
            foreach (Type type in typeof(ItemCollectionGenerator_Standard).AllSubclassesNonAbstract())
                type.GetMethod("Reset").Invoke(null, null);

            RemoveStuff(typeof(DefDatabase<ThingDef>), things.Cast<Def>());

            MethodInfo resolveDesignatorsAgain = typeof(DesignationCategoryDef).GetMethod("ResolveDesignators", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (DesignationCategoryDef dcd in DefDatabase<DesignationCategoryDef>.AllDefs)
                resolveDesignatorsAgain.Invoke(dcd, null);

            RemoveStuff(typeof(DefDatabase<PawnKindDef>), DefDatabase<PawnKindDef>.AllDefs.Where(pkd => pkd?.defaultFactionType?.defName != "PlayerColony" && (pkd.race.techLevel > maxTechLevel || pkd.defaultFactionType?.techLevel > maxTechLevel)).Cast<Def>());
            RemoveStuff(typeof(DefDatabase<FactionDef>), DefDatabase<FactionDef>.AllDefs.Where(fd => fd.defName != "PlayerColony" && fd.techLevel > maxTechLevel).Cast<Def>());

            foreach (MapGeneratorDef mgd in DefDatabase<MapGeneratorDef>.AllDefs)
                mgd.GenSteps.RemoveAll(gs => gs.genStep is GenStep_SleepingMechanoids || gs.genStep is GenStep_Turrets || gs.genStep is GenStep_Power);

            foreach (RuleDef rd in DefDatabase<RuleDef>.AllDefs)
            {
                rd.resolvers.RemoveAll(sr => sr is SymbolResolver_AncientCryptosleepCasket || sr is SymbolResolver_ChargeBatteries || sr is SymbolResolver_EdgeMannedMortar || sr is SymbolResolver_FirefoamPopper || sr is SymbolResolver_MannedMortar || sr is SymbolResolver_OutdoorLighting);
                if (rd.resolvers.Count == 0)
                    rd.resolvers.Add(new SymbolResolver_AddWortToFermentingBarrels());
            }

            Log.Message("Removed " + removedDefs + " modern defs");
            
        }

        static void RemoveStuff(Type databaseType, IEnumerable<Def> defs)
        {
            MethodInfo mi = databaseType.GetMethod("Remove", BindingFlags.Static | BindingFlags.NonPublic);
            while(defs.Any())
            {
                removedDefs++;
                mi.Invoke(null, new object[] { defs.First() });
            }
        }
    }
}
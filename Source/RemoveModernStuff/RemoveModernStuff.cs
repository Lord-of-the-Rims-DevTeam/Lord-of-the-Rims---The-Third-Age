using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace TheThirdAge
{
    using System.Xml;
    using Harmony;
    using JetBrains.Annotations;

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
            
            RemoveStuffFromDatabase(typeof(DefDatabase<RecipeDef>), DefDatabase<RecipeDef>.AllDefs.Where(rd => rd.products.Any(tcc => things.Contains(tcc.thingDef)) || rd.AllRecipeUsers.All(td => things.Contains(td)) || projects.Contains(rd.researchPrerequisite)).Cast<Def>());
            RemoveStuffFromDatabase(typeof(DefDatabase<ResearchProjectDef>), projects.Cast<Def>());

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

            foreach (TraderKindDef tkd in DefDatabase<TraderKindDef>.AllDefs)
            {
                for (int i = tkd.stockGenerators.Count - 1; i >= 0; i--)
                {
                    StockGenerator stockGenerator = tkd.stockGenerators[i];

                    if (stockGenerator is StockGenerator_SingleDef sd && things.Contains(Traverse.Create(sd).Field("thingDef").GetValue<ThingDef>()))
                        tkd.stockGenerators.Remove(stockGenerator);
                    if (stockGenerator is StockGenerator_MultiDef md)
                    {
                        Traverse thingListTraverse = Traverse.Create(md).Field("thingDefs");
                        List<ThingDef> thingList = thingListTraverse.GetValue<List<ThingDef>>();
                        thingList.RemoveAll(things.Contains);

                        if (thingList.NullOrEmpty())
                            tkd.stockGenerators.Remove(stockGenerator);
                        else
                            thingListTraverse.SetValue(thingList);
                    }

                }
            }

            RemoveStuffFromDatabase(typeof(DefDatabase<IncidentDef>), DefDatabase<IncidentDef>.AllDefs.Where(id => new[]
                {
                    typeof(IncidentWorker_ShipChunkDrop),
                    AccessTools.TypeByName("IncidentWorker_ShipPartCrash"),
                    typeof(IncidentWorker_JourneyOffer),
                    typeof(IncidentWorker_ResourcePodCrash),
                    typeof(IncidentWorker_RefugeePodCrash),
                    typeof(IncidentWorker_PsychicDrone),
                    typeof(IncidentWorker_RansomDemand),
                    typeof(IncidentWorker_ShortCircuit),
                    typeof(IncidentWorker_OrbitalTraderArrival),
                    typeof(IncidentWorker_PsychicSoothe)

                }.SelectMany(it => it.AllSubclassesNonAbstract().Concat(it)).ToArray().Contains(id.workerClass) || 
                    new[] { "Disease_FibrousMechanites", "Disease_SensoryMechanites", "RaidEnemyEscapeShip" }.Contains(id.defName)).Cast<Def>());



            RemoveStuffFromDatabase(typeof(DefDatabase<RoadDef>), DefDatabase<RoadDef>.AllDefs.Where(rd => new[] { "AncientAsphaltRoad", "AncientAsphaltHighway" }.Contains(rd.defName)).Cast<Def>());

            RemoveStuffFromDatabase(typeof(DefDatabase<RaidStrategyDef>), DefDatabase<RaidStrategyDef>.AllDefs.Where(rs => typeof(ScenPart_ThingCount).IsAssignableFrom(rs.workerClass)).Cast<Def>());

            RemoveStuffFromDatabase(typeof(DefDatabase<ThingDef>), things.Cast<Def>());

            RemoveStuffFromDatabase(typeof(DefDatabase<TraitDef>), new []{ nameof(TraitDefOf.Prosthophobe), "Prosthophile"}.Select(TraitDef.Named).Cast<Def>());

            MethodInfo resolveDesignatorsAgain = typeof(DesignationCategoryDef).GetMethod("ResolveDesignators", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (DesignationCategoryDef dcd in DefDatabase<DesignationCategoryDef>.AllDefs)
                resolveDesignatorsAgain.Invoke(dcd, null);

            RemoveStuffFromDatabase(typeof(DefDatabase<PawnKindDef>), DefDatabase<PawnKindDef>.AllDefs.Where(pkd => pkd?.defaultFactionType?.defName != "PlayerColony" && (pkd.race.techLevel > maxTechLevel || pkd.defaultFactionType?.techLevel > maxTechLevel)).Cast<Def>());
            RemoveStuffFromDatabase(typeof(DefDatabase<FactionDef>), DefDatabase<FactionDef>.AllDefs.Where(fd => !fd.isPlayer && fd.techLevel > maxTechLevel).Cast<Def>());

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

        static void RemoveStuffFromDatabase(Type databaseType, IEnumerable<Def> defs)
        {
            if (defs.Any())
            {
                Traverse rm = Traverse.Create(databaseType).Method("Remove", defs.First());
                while (defs.Any())
                {
                    removedDefs++;
                    rm.GetValue(defs.First());
                }
            }
        }
    }

    [UsedImplicitly]
    public class PatchOperationRemoveModernStuff : PatchOperation
    {
        private static readonly PatchOperationRemove removeOperation = new PatchOperationRemove();
        private static readonly Traverse setXpathTraverse = Traverse.Create(root: removeOperation).Field(name: "xpath");
        private static readonly string xpath = $"//techLevel[.='{string.Join(separator: "' or .='", value: Enum.GetValues(enumType: typeof(TechLevel)).Cast<TechLevel>().Where(predicate: tl => tl > RemoveModernStuff.maxTechLevel).Select(selector: tl => tl.ToString()).ToArray())}']/..";

        protected override bool ApplyWorker(XmlDocument xml)
        {
            /*
            setXpathTraverse.SetValue(value: xpath);
            removeOperation.Apply(xml: xml);
            */
            return true;
        }
    }
}
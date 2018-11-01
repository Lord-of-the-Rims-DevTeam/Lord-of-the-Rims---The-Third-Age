using System.Text;
using UnityEngine;

namespace TheThirdAge
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using Harmony;
    using JetBrains.Annotations;
    using RimWorld;
    using RimWorld.BaseGen;
    using Verse;

    [StaticConstructorOnStartup]
    public static class RemoveModernStuff
    {
        public const TechLevel MAX_TECHLEVEL = TechLevel.Medieval;
        private static int removedDefs;
        private static StringBuilder DebugString = new StringBuilder();

        public static IEnumerable<HediffDef> hediffs;
        public static IEnumerable<ThingDef> things;

        static RemoveModernStuff()
        {
            DebugString.AppendLine("Lord of the Rings - The Third Age - Start Removal Log");
            DebugString.AppendLine("Tech Limiter Active: Max Level = " + MAX_TECHLEVEL.ToString());
            giveApproppriateTechLevels();

            removedDefs = 0;

            IEnumerable<ResearchProjectDef> projects =
                DefDatabase<ResearchProjectDef>.AllDefs.Where(rpd => rpd.techLevel > MAX_TECHLEVEL);

            things = DefDatabase<ThingDef>.AllDefs.Where(td =>
                td.techLevel > MAX_TECHLEVEL ||
                (td.researchPrerequisites?.Any(rpd => projects.Contains(rpd)) ?? false) || new[]
                {
                    "Gun_Revolver", "VanometricPowerCell", "PsychicEmanator", "InfiniteChemreactor", "Joywire",
                    "Painstopper"
                }.Contains(td.defName)).ToArray();

            DebugString.AppendLine("RecipeDef Removal List");
            var recipeDefsToRemove = DefDatabase<RecipeDef>.AllDefs.Where(rd =>
                rd.products.Any(tcc => things.Contains(tcc.thingDef)) ||
                rd.AllRecipeUsers.All(td => things.Contains(td)) ||
                projects.Contains(rd.researchPrerequisite)).Cast<Def>().ToList();
            recipeDefsToRemove?.RemoveAll(x =>
                x.defName == "ExtractMetalFromSlag" ||
                x.defName == "SmeltWeapon" ||
                x.defName == "DestroyWeapon" || 
                x.defName == "OfferingOfPlants_Meagre" ||
                x.defName == "OfferingOfPlants_Decent" ||
                x.defName == "OfferingOfPlants_Sizable" ||
                x.defName == "OfferingOfPlants_Worthy" ||
                x.defName == "OfferingOfPlants_Impressive" ||
                x.defName == "OfferingOfMeat_Meagre" ||
                x.defName == "OfferingOfMeat_Decent" ||
                x.defName == "OfferingOfMeat_Sizable" ||
                x.defName == "OfferingOfMeat_Worthy" ||
                x.defName == "OfferingOfMeat_Impressive" ||
                x.defName == "OfferingOfMeals_Meagre" ||
                x.defName == "OfferingOfMeals_Decent" ||
                x.defName == "OfferingOfMeals_Sizable" ||
                x.defName == "OfferingOfMeals_Worthy" ||
                x.defName == "OfferingOfMeals_Impressive" ||
                x.defName == "ROMV_ExtractBloodVial" ||
                x.defName == "ROMV_ExtractBloodPack"
                );
            RemoveStuffFromDatabase(typeof(DefDatabase<RecipeDef>), recipeDefsToRemove);

            DebugString.AppendLine("ResearchProjectDef Removal List");
            RemoveStuffFromDatabase(typeof(DefDatabase<ResearchProjectDef>), projects.Cast<Def>());


            DebugString.AppendLine("Scenario Part Removal List");
            FieldInfo getThingInfo =
                typeof(ScenPart_ThingCount).GetField("thingDef", BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (ScenarioDef def in DefDatabase<ScenarioDef>.AllDefs)
            foreach (ScenPart sp in def.scenario.AllParts)
                if (sp is ScenPart_ThingCount && things.Contains((ThingDef) getThingInfo?.GetValue(sp)))
                {
                    def.scenario.RemovePart(sp);
                    DebugString.AppendLine("- " + sp.Label + " " + ((ThingDef) getThingInfo?.GetValue(sp)).label +
                                           " from " + def.label);
                }

            foreach (ThingCategoryDef thingCategoryDef in DefDatabase<ThingCategoryDef>.AllDefs)
                thingCategoryDef.childThingDefs.RemoveAll(things.Contains);

            DebugString.AppendLine("Stock Generator Part Cleanup");
            foreach (TraderKindDef tkd in DefDatabase<TraderKindDef>.AllDefs)
            {
                for (int i = tkd.stockGenerators.Count - 1; i >= 0; i--)
                {
                    StockGenerator stockGenerator = tkd.stockGenerators[i];

                    switch (stockGenerator)
                    {
                        case StockGenerator_SingleDef sd when things.Contains(Traverse.Create(sd).Field("thingDef")
                            .GetValue<ThingDef>()):
                            ThingDef def = Traverse.Create(sd).Field("thingDef")
                                .GetValue<ThingDef>();
                            tkd.stockGenerators.Remove(stockGenerator);
                            DebugString.AppendLine("- " + def.label + " from " + tkd.label +
                                                   "'s StockGenerator_SingleDef");
                            break;
                        case StockGenerator_MultiDef md:
                            Traverse thingListTraverse = Traverse.Create(md).Field("thingDefs");
                            List<ThingDef> thingList = thingListTraverse.GetValue<List<ThingDef>>();
                            var removeList = thingList.FindAll(things.Contains);
                            removeList?.ForEach(x =>
                                DebugString.AppendLine("- " + x.label + " from " + tkd.label +
                                                       "'s StockGenerator_MultiDef"));
                            thingList.RemoveAll(things.Contains);

                            if (thingList.NullOrEmpty())
                                tkd.stockGenerators.Remove(stockGenerator);
                            else
                                thingListTraverse.SetValue(thingList);
                            break;
                    }
                }
            }


            DebugString.AppendLine("IncidentDef Removal List");



            IEnumerable<IncidentDef> incidents = DefDatabase<IncidentDef>.AllDefs
               .Where(id => new[]
                                {
                                    typeof
                                    (IncidentWorker_ShipChunkDrop
                                    ),
                                    AccessTools
                                       .TypeByName(
                                            "IncidentWorker_ShipPartCrash"),
                                    typeof
                                    (IncidentWorker_QuestJourneyOffer
                                    ),
                                    typeof
                                    (IncidentWorker_ResourcePodCrash
                                    ),
                                    //typeof(IncidentWorker_RefugeePodCrash),
                                    typeof(IncidentWorker_TransportPodCrash),
                                    typeof
                                    (IncidentWorker_PsychicDrone
                                    ),
                                    typeof
                                    (IncidentWorker_RansomDemand
                                    ),
                                    typeof
                                    (IncidentWorker_ShortCircuit
                                    ),
                                    typeof
                                    (IncidentWorker_OrbitalTraderArrival
                                    ),
                                    typeof
                                    (IncidentWorker_PsychicSoothe
                                    )
                                }.SelectMany(
                                    it =>
                                        it
                                           .AllSubclassesNonAbstract()
                                           .Concat(
                                                it))
                               .ToArray()
                               .Contains(
                                    id
                                       .workerClass) ||
                            new[]
                            {
                                "Disease_FibrousMechanites",
                                "Disease_SensoryMechanites",
                                "RaidEnemyEscapeShip",
                                "StrangerInBlackJoin"
                            }.Contains(
                                id
                                   .defName)).ToList();


            foreach (IncidentDef incident in incidents)
            {
                incident.targetTags?.Clear();
                incident.baseChance = 0f;
                incident.allowedBiomes?.Clear();
                incident.earliestDay = int.MaxValue;
            }

            RemoveStuffFromDatabase(typeof(DefDatabase<IncidentDef>), incidents.Cast<Def>());

            


            DebugString.AppendLine("Replaced Ancient Asphalt Road / Ancient Asphalt Highway with Stone Road");
            RoadDef[] targetRoads = {RoadDefOf.AncientAsphaltRoad, RoadDefOf.AncientAsphaltHighway};
            RoadDef originalRoad = DefDatabase<RoadDef>.GetNamed("StoneRoad");

            List<string> fieldNames = AccessTools.GetFieldNames(typeof(RoadDef));
            fieldNames.Remove("defName");
            foreach (FieldInfo fi in fieldNames.Select(name => AccessTools.Field(typeof(RoadDef), name)))
            {
                object fieldValue = fi.GetValue(originalRoad);
                foreach (RoadDef targetRoad in targetRoads) fi.SetValue(targetRoad, fieldValue);
            }

            DebugString.AppendLine("Special Hediff Removal List");
            RemoveStuffFromDatabase(typeof(DefDatabase<HediffDef>), (hediffs = new[] {HediffDefOf.Gunshot}).Cast<Def>());

            DebugString.AppendLine("RaidStrategyDef Removal List");
            RemoveStuffFromDatabase(typeof(DefDatabase<RaidStrategyDef>),
                DefDatabase<RaidStrategyDef>.AllDefs
                    .Where(rs => typeof(ScenPart_ThingCount).IsAssignableFrom(rs.workerClass)).Cast<Def>());

            //            ItemCollectionGeneratorUtility.allGeneratableItems.RemoveAll(match: things.Contains);
            //
            //            foreach (Type type in typeof(ItemCollectionGenerator_Standard).AllSubclassesNonAbstract())
            //                type.GetMethod(name: "Reset")?.Invoke(obj: null, parameters: null);

            DebugString.AppendLine("ThingDef Removal List");
            RemoveStuffFromDatabase(typeof(DefDatabase<ThingDef>), (ThingDef[]) things);

            DebugString.AppendLine("ThingSetMaker Reset");
            ThingSetMakerUtility.Reset();

            DebugString.AppendLine("TraitDef Removal List");
            RemoveStuffFromDatabase(typeof(DefDatabase<TraitDef>),
                //                                                                   { nameof(TraitDefOf.Prosthophobe), "Prosthophile" } ?
                DefDatabase<TraitDef>.AllDefs
                    .Where(td => new[] {nameof(TraitDefOf.BodyPurist), "Transhumanist"}.Contains(td.defName))
                    .Cast<Def>());

            DebugString.AppendLine("Designators Resolved Again");
            MethodInfo resolveDesignatorsAgain = typeof(DesignationCategoryDef).GetMethod("ResolveDesignators",
                BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (DesignationCategoryDef dcd in DefDatabase<DesignationCategoryDef>.AllDefs)
                resolveDesignatorsAgain?.Invoke(dcd, null);

            DebugString.AppendLine("PawnKindDef Removal List");
            RemoveStuffFromDatabase(typeof(DefDatabase<PawnKindDef>),
                DefDatabase<PawnKindDef>.AllDefs
                    .Where(pkd =>
                        (!pkd.defaultFactionType?.isPlayer ?? false) &&
                        (pkd.race.techLevel > MAX_TECHLEVEL || pkd.defaultFactionType?.techLevel > MAX_TECHLEVEL))
                    .Cast<Def>());

            DebugString.AppendLine("FactionDef Removal List");
            RemoveStuffFromDatabase(typeof(DefDatabase<FactionDef>),
                DefDatabase<FactionDef>.AllDefs.Where(fd => !fd.isPlayer && fd.techLevel > MAX_TECHLEVEL).Cast<Def>());

            DebugString.AppendLine("MapGeneratorDef Removal List");
            DebugString.AppendLine("- GenStep_SleepingMechanoids");
            DebugString.AppendLine("- GenStep_Turrets");
            DebugString.AppendLine("- GenStep_Power");
            foreach (MapGeneratorDef mgd in DefDatabase<MapGeneratorDef>.AllDefs)
                mgd.genSteps.RemoveAll(gs =>
                    gs.genStep is GenStep_SleepingMechanoids || gs.genStep is GenStep_Turrets ||
                    gs.genStep is GenStep_Power);

            DebugString.AppendLine("RuleDef Removal List");
            DebugString.AppendLine("- SymbolResolver_AncientCryptosleepCasket");
            DebugString.AppendLine("- SymbolResolver_ChargeBatteries");
            DebugString.AppendLine("- SymbolResolver_EdgeMannedMortor");
            DebugString.AppendLine("- SymbolResolver_FirefoamPopper");
            DebugString.AppendLine("- SymbolResolver_MannedMortar");
            DebugString.AppendLine("- SymbolResolver_");
            foreach (RuleDef rd in DefDatabase<RuleDef>.AllDefs)
            {
                rd.resolvers.RemoveAll(sr =>
                    sr is SymbolResolver_AncientCryptosleepCasket || sr is SymbolResolver_ChargeBatteries ||
                    sr is SymbolResolver_EdgeMannedMortar || sr is SymbolResolver_FirefoamPopper ||
                    sr is SymbolResolver_MannedMortar || sr is SymbolResolver_OutdoorLighting);
                if (rd.resolvers.Count == 0)
                    rd.resolvers.Add(new SymbolResolver_AddWortToFermentingBarrels());
            }

            Log.Message("Removed " + removedDefs + " modern defs");

            PawnWeaponGenerator.Reset();
            PawnApparelGenerator.Reset();

            Debug.Log(DebugString.ToString());
            DebugString = new StringBuilder();
        }

        private static void giveApproppriateTechLevels()
        {
            DebugString.AppendLine("ElectricSmelter's tech level changed to Industrial");
            ThingDef.Named("ElectricSmelter").techLevel = TechLevel.Industrial;

            DebugString.AppendLine("ElectricCrematorium's tech level changed to Industrial");
            ThingDef.Named("ElectricCrematorium").techLevel = TechLevel.Industrial;

            DebugString.AppendLine("FueledSmithy's tech level changed to Industrial");
            ThingDef.Named("FueledSmithy").techLevel = TechLevel.Industrial;
        }

        private static void RemoveStuffFromDatabase(Type databaseType, [NotNull] IEnumerable<Def> defs)
        {
            IEnumerable<Def> enumerable = defs as Def[] ?? defs.ToArray();
            if (!enumerable.Any()) return;
            Traverse rm = Traverse.Create(databaseType).Method("Remove", enumerable.First());
            foreach (Def def in enumerable)
            {
                removedDefs++;
                DebugString.AppendLine("- " + def.label);
                rm.GetValue(def);
            }
        }
    }

    [UsedImplicitly]
    public class PatchOperationRemoveModernStuff : PatchOperation
    {
        /*
        private static readonly PatchOperationRemove removeOperation = new PatchOperationRemove();
        private static readonly Traverse setXpathTraverse = Traverse.Create(root: removeOperation).Field(name: "xpath");
        private static readonly string xpath = $"//techLevel[.='{string.Join(separator: "' or .='", value: Enum.GetValues(enumType: typeof(TechLevel)).Cast<TechLevel>().Where(predicate: tl => tl > RemoveModernStuff.MAX_TECHLEVEL).Select(selector: tl => tl.ToString()).ToArray())}']/..";
        */
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
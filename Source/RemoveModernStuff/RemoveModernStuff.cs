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
        public const   TechLevel MAX_TECHLEVEL = TechLevel.Medieval;
        private static int       removedDefs;

        static RemoveModernStuff()
        {
            removedDefs = 0;

            IEnumerable<ResearchProjectDef> projects = DefDatabase<ResearchProjectDef>.AllDefs.Where(predicate: rpd => rpd.techLevel > MAX_TECHLEVEL);
            ThingDef[] things = DefDatabase<ThingDef>.AllDefs.Where(predicate: td =>
                td.techLevel > MAX_TECHLEVEL || (td.researchPrerequisites?.Any(predicate: rpd => projects.Contains(value: rpd)) ?? false) || new[] {"Gun_Revolver", "VanometricPowerCell", "PsychicEmanator", "InfiniteChemreactor", "Joywire", "Painstopper" }.Contains(value: td.defName)).ToArray();

            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<RecipeDef>),
                defs: DefDatabase<RecipeDef>.AllDefs.Where(predicate: rd =>
                    rd.products.Any(predicate: tcc => things.Contains(value: tcc.thingDef)) || rd.AllRecipeUsers.All(predicate: td => things.Contains(value: td)) ||
                    projects.Contains(value: rd.researchPrerequisite)).Cast<Def>());

            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<ResearchProjectDef>), defs: projects.Cast<Def>());
            
            FieldInfo getThingInfo = typeof(ScenPart_ThingCount).GetField(name: "thingDef", bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (ScenarioDef def in DefDatabase<ScenarioDef>.AllDefs)
                foreach (ScenPart sp in def.scenario.AllParts)
                    if (sp is ScenPart_ThingCount && things.Contains(value: (ThingDef) getThingInfo?.GetValue(obj: sp)))
                        def.scenario.RemovePart(part: sp);
            
            foreach (ThingCategoryDef thingCategoryDef in DefDatabase<ThingCategoryDef>.AllDefs)
                thingCategoryDef.childThingDefs.RemoveAll(match: things.Contains);
            
            foreach (TraderKindDef tkd in DefDatabase<TraderKindDef>.AllDefs)
            {
                for (int i = tkd.stockGenerators.Count - 1; i >= 0; i--)
                {
                    StockGenerator stockGenerator = tkd.stockGenerators[index: i];

                    switch (stockGenerator)
                    {
                        case StockGenerator_SingleDef sd when things.Contains(value: Traverse.Create(root: sd).Field(name: "thingDef").GetValue<ThingDef>()):
                            tkd.stockGenerators.Remove(item: stockGenerator);
                            break;
                        case StockGenerator_MultiDef md:
                            Traverse       thingListTraverse = Traverse.Create(root: md).Field(name: "thingDefs");
                            List<ThingDef> thingList         = thingListTraverse.GetValue<List<ThingDef>>();
                            thingList.RemoveAll(match: things.Contains);

                            if (thingList.NullOrEmpty())
                                tkd.stockGenerators.Remove(item: stockGenerator);
                            else
                                thingListTraverse.SetValue(value: thingList);
                            break;
                    }
                }
            }
            
            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<IncidentDef>), defs: DefDatabase<IncidentDef>.AllDefs.Where(predicate: id => new[]
                {
                    typeof(IncidentWorker_ShipChunkDrop),
                    AccessTools.TypeByName(
                        name: "IncidentWorker_ShipPartCrash"),
                    typeof(IncidentWorker_QuestJourneyOffer),
                    typeof(IncidentWorker_ResourcePodCrash),
                    typeof(IncidentWorker_RefugeePodCrash),
                    typeof(IncidentWorker_PsychicDrone),
                    typeof(IncidentWorker_RansomDemand),
                    typeof(IncidentWorker_ShortCircuit),
                    typeof(IncidentWorker_OrbitalTraderArrival),
                    typeof(IncidentWorker_PsychicSoothe)
                }.SelectMany(selector: it =>
                    it.AllSubclassesNonAbstract().Concat(rhs: it))
                .ToArray().Contains(value: id.workerClass) ||
                new[]
                {
                    "Disease_FibrousMechanites",
                    "Disease_SensoryMechanites",
                    "RaidEnemyEscapeShip"
                }.Contains(value: id.defName)).Cast<Def>());


            RoadDef[] targetRoads = {RoadDefOf.AncientAsphaltRoad, RoadDefOf.AncientAsphaltHighway};
            RoadDef originalRoad = DefDatabase<RoadDef>.GetNamed(defName: "StoneRoad");

            List<string> fieldNames = AccessTools.GetFieldNames(type: typeof(RoadDef));
            fieldNames.Remove(item: "defName");
            foreach (FieldInfo fi in fieldNames.Select(selector: name => AccessTools.Field(type: typeof(RoadDef), name: name)))
            {
                object fieldValue = fi.GetValue(obj: originalRoad);
                foreach (RoadDef targetRoad in targetRoads) fi.SetValue(obj: targetRoad, value: fieldValue);
            }

            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<HediffDef>), defs: new [] { HediffDefOf.Gunshot });

            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<RaidStrategyDef>),
                defs: DefDatabase<RaidStrategyDef>.AllDefs.Where(predicate: rs => typeof(ScenPart_ThingCount).IsAssignableFrom(c: rs.workerClass)).Cast<Def>());

            //            ItemCollectionGeneratorUtility.allGeneratableItems.RemoveAll(match: things.Contains);
            //
            //            foreach (Type type in typeof(ItemCollectionGenerator_Standard).AllSubclassesNonAbstract())
            //                type.GetMethod(name: "Reset")?.Invoke(obj: null, parameters: null);

            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<ThingDef>), defs: things);

            ThingSetMakerUtility.Reset();

            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<TraitDef>),
            //                                                                   { nameof(TraitDefOf.Prosthophobe), "Prosthophile" } ?
                defs: DefDatabase<TraitDef>.AllDefs.Where(predicate: td => new[] { nameof(TraitDefOf.BodyPurist), "Transhumanist" }.Contains(value: td.defName)).Cast<Def>());

            MethodInfo resolveDesignatorsAgain = typeof(DesignationCategoryDef).GetMethod(name: "ResolveDesignators", bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (DesignationCategoryDef dcd in DefDatabase<DesignationCategoryDef>.AllDefs)
                resolveDesignatorsAgain?.Invoke(obj: dcd, parameters: null);

            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<PawnKindDef>),
                defs: DefDatabase<PawnKindDef>.AllDefs
                   .Where(predicate: pkd => (!pkd.defaultFactionType?.isPlayer ?? false) && (pkd.race.techLevel > MAX_TECHLEVEL || pkd.defaultFactionType?.techLevel > MAX_TECHLEVEL)).Cast<Def>());
            RemoveStuffFromDatabase(databaseType: typeof(DefDatabase<FactionDef>),
                defs: DefDatabase<FactionDef>.AllDefs.Where(predicate: fd => !fd.isPlayer && fd.techLevel > MAX_TECHLEVEL).Cast<Def>());
            
            foreach (MapGeneratorDef mgd in DefDatabase<MapGeneratorDef>.AllDefs)
                mgd.genSteps.RemoveAll(match: gs => gs.genStep is GenStep_SleepingMechanoids || gs.genStep is GenStep_Turrets || gs.genStep is GenStep_Power);

            foreach (RuleDef rd in DefDatabase<RuleDef>.AllDefs)
            {
                rd.resolvers.RemoveAll(match: sr =>
                    sr is SymbolResolver_AncientCryptosleepCasket || sr is SymbolResolver_ChargeBatteries || sr is SymbolResolver_EdgeMannedMortar || sr is SymbolResolver_FirefoamPopper ||
                    sr is SymbolResolver_MannedMortar             || sr is SymbolResolver_OutdoorLighting);
                if (rd.resolvers.Count == 0)
                    rd.resolvers.Add(item: new SymbolResolver_AddWortToFermentingBarrels());
            }

            Log.Message(text: "Removed " + removedDefs + " modern defs");

        }

        private static void RemoveStuffFromDatabase(Type databaseType, [NotNull] IEnumerable<Def> defs)
        {
            IEnumerable<Def> enumerable = defs as Def[] ?? defs.ToArray();
            if (!enumerable.Any()) return;
            Traverse rm = Traverse.Create(type: databaseType).Method("Remove", enumerable.First());
            foreach (Def def in enumerable)
            {
                removedDefs++;
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
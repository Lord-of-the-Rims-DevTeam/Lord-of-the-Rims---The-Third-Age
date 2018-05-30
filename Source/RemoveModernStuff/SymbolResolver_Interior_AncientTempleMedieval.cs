using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace TheThirdAge
{
    public class SymbolResolver_Interior_AncientTempleMedieval : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            List<Thing> list = ItemCollectionGeneratorDefOf.AncientTempleContents.Worker.Generate(default(ItemCollectionGeneratorParams));
            for (int i = 0; i < list.Count; i++)
            {
                ResolveParams resolveParams = rp;
                resolveParams.singleThingToSpawn = list[i];
                BaseGen.symbolStack.Push("thing", resolveParams);
            }
            if (ModLister.AllInstalledMods.FirstOrDefault(x => x.enabled && x.Name == "Lord of the Rims - Men and Beasts") != null)
                SpawnGroups(rp);
            int? ancientTempleEntranceHeight = rp.ancientTempleEntranceHeight;
            int num = (ancientTempleEntranceHeight == null) ? 0 : ancientTempleEntranceHeight.Value;
            ResolveParams resolveParams4 = rp;
            resolveParams4.rect.minZ = resolveParams4.rect.minZ + num;
            BaseGen.symbolStack.Push("ancientShrinesGroup", resolveParams4);
        }

        private static void SpawnGroups(ResolveParams rp)
        {
            if (!Find.Storyteller.difficulty.peacefulTemples)
            {
//                if (Rand.Chance(0.5f))
//                {
//                    ResolveParams resolveParams2 = rp;
//                    int? mechanoidsCount = rp.mechanoidsCount;
//                    resolveParams2.mechanoidsCount = new int?((mechanoidsCount == null)
//                        ? SymbolResolver_Interior_AncientTempleMedieval.MechanoidCountRange.RandomInRange
//                        : mechanoidsCount.Value);
//                    BaseGen.symbolStack.Push("randomMechanoidGroup", resolveParams2);
//                }
//                else if (Rand.Chance(0.45f))
//                {
//                    ResolveParams resolveParams3 = rp;
//                    int? hivesCount = rp.hivesCount;
//                    resolveParams3.hivesCount = new int?((hivesCount == null)
//                        ? SymbolResolver_Interior_AncientTempleMedieval.HivesCountRange.RandomInRange
//                        : hivesCount.Value);
//                    BaseGen.symbolStack.Push("hives", resolveParams3);
//                }
            }
        }

        private const float MechanoidsChance = 0.5f;

        private static readonly IntRange MechanoidCountRange = new IntRange(1, 5);

        private const float HivesChance = 0.45f;

        private static readonly IntRange HivesCountRange = new IntRange(1, 2);
    }
}

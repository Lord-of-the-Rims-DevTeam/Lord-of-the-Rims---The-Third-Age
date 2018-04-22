using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoveModernStuff
{
    [StaticConstructorOnStartup]
    public static class MoveToSmithy
    {
        private static int movedDefs;
        private static int steelDefs;
        static MoveToSmithy()
        {
            MoveRecipesToSmithy();

            ChangeSteelToIron();
        }

        private static void MoveRecipesToSmithy()
        {
            movedDefs = 0;
            foreach (ThingDef td in DefDatabase<ThingDef>.AllDefs.Where(t =>
                (t?.recipeMaker?.recipeUsers?.Contains(ThingDef.Named("FueledSmithy")) ?? false) ||
                (t?.recipeMaker?.recipeUsers?.Contains(ThingDef.Named("TableMachining")) ?? false)))
            {
                td.recipeMaker.recipeUsers.RemoveAll(x => x.defName == "TableMachining" ||
                                                          x.defName == "FueledSmithy");
                td.recipeMaker.recipeUsers.Add(ThingDef.Named("LotR_TableSmithy"));
                movedDefs++;
            }
            Log.Message("Moved " + movedDefs + " from Machining Table to Smithy.");
        }

        private static void ChangeSteelToIron()
        {
            steelDefs = 0;
            foreach (ThingDef tdd in DefDatabase<ThingDef>.AllDefs.Where(tt =>
                tt?.costList?.Any(y => y?.thingDef == ThingDefOf.Steel) ?? false))
            {
                var tempCost = tdd.costList.FirstOrDefault(z => z.thingDef == ThingDefOf.Steel);
                var newTempCost = new ThingCountClass(ThingDef.Named("LotR_Iron"), tempCost.count);
                tdd.costList.Remove(tempCost);
                tdd.costList.Add(newTempCost);
                steelDefs++;
            }
            if (ThingDefOf.Steel?.deepCommonality > 2)
                ThingDefOf.Steel.deepCommonality = 2;
            Log.Message("Replaced " + steelDefs + " defs with Iron.");
        }
    }
}
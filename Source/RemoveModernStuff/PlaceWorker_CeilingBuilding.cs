using Verse;

namespace Rimworld
{

    public class PlaceWorker_CeilingBuilding : PlaceWorker
    {

        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef,
                                                            IntVec3 loc,
                                                               Rot4 rot,
                                                                Map map,
                                                              Thing thingToIgnore = null)
        {

            if (!map.roofGrid.Roofed(loc))
                return (AcceptanceReport)"Must be placed under roof.";

            Building building = loc.GetEdifice(map);

            if (building != null && building.def != null && building.def.graphicData != null)
            {
                if (building.def.IsDoor)
                    return "Can't be placed over doors.";
                if ((building.def.graphicData.linkFlags & (LinkFlags.Wall | LinkFlags.Rock)) != 0)
                    return "Can't be placed over walls.";
            }

            return AcceptanceReport.WasAccepted;
        }
    }
}
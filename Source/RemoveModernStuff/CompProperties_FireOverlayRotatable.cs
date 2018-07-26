using UnityEngine;
using Verse;

namespace RimWorld
{

    public enum DependencyType
    {
        None,
        Fuel
    };

    public class CompProperties_FireOverlayRotatable : CompProperties
    {

        public Vector2 fireSize = new Vector2(1f, 1f);
        public Vector3 offset_south;
        public Vector3 offset_west;
        public Vector3 offset_north;
        public Vector3 offset_east;
        public bool aboveThing = true;
        public string path = null;
        public int ticks = 60;
        public DependencyType dependency = DependencyType.None;

        public CompProperties_FireOverlayRotatable()
        {
            this.compClass = typeof(CompFireOverlayRotatable);
        }
    }
}
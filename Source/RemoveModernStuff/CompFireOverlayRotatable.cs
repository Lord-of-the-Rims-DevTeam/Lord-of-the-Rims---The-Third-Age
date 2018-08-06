using System;
using UnityEngine;
using Verse;

namespace RimWorld
{

    [StaticConstructorOnStartup]
    public class CompFireOverlayRotatable : ThingComp
    {
        protected CompRefuelable refuelableComp;
        // This need to be readonly, otherwise the game will complain about the thread it's being loaded.
        private static readonly GraphicRotatable FireGraphic = new GraphicRotatable(new GraphicRequest(null,
                                                        "Things/Special/Fire",
                                                        ShaderDatabase.TransparentPostLight,
                                                        Vector2.one, Color.white, Color.white,
                                                        null, 0, null));

        public CompProperties_FireOverlayRotatable Props =>
            (CompProperties_FireOverlayRotatable)this.props;

        ThingWithComps thing = null;
        ThingDef def = null;

        int scramble = 0;
        int mem1 = 0;
        int mem2 = 0;

        public override void Initialize(CompProperties props)
        {
            this.props = props;
            thing = this.parent;

            if (thing != null)
                def = thing.def;

            
            //LoadGraphics();
        }

        void Awake()
        {

        }


/*
        public void LoadGraphics()
        {
            try
            {
                GraphicRequest gr = new GraphicRequest(null, 
                    "Things/Special/Fire", 
                    ShaderDatabase.TransparentPostLight, 
                    Vector2.one, 
                    Color.white, 
                    Color.white, null, 0, null);
                FireGraphic = new GraphicRotatable(gr);
            }
            catch (Exception e)
            {
                Log.Message($"TTA: CompFire caught exception {e}.");
            }
        }
*/
        public override void PostDraw()
        {
            try
            {
                if (this.parent == null)
                {
                    return;
                }
                if (def == null)
                {
                    return;
                }
                if (!ShouldRender(thing, def))
                    return;

                Vector3 offset;
                int y = 0;

                switch (thing.Rotation.AsInt)
                {
                    case 0: // south
                        offset = Props.offset_south;
                        break;
                    case 1: // west
                        offset = Props.offset_west;
                        break;
                    case 2: // north
                        offset = Props.offset_north;
                        break;
                    case 3: // east
                        offset = Props.offset_east;
                        break;
                    default:
                        throw new Exception($"TTA: CompFire found thing {thing} with invalid rotation.");
                }

                Vector3 drawPosRotated = thing.DrawPos + offset;
                Vector3 drawSizeRotated = new Vector3(Props.fireSize.x, 1f, Props.fireSize.y);
                Quaternion quaternion = Quaternion.identity;

                y = ((int)def.altitudeLayer) + (Props.aboveThing == true ? 1 : -1);
                drawPosRotated.y = Altitudes.AltitudeFor((AltitudeLayer)y);

                FireFlicker(drawPosRotated, drawSizeRotated, quaternion);
            }
            catch (Exception e)
            {
                Log.Message($"TTA: CompFire caught exception {e}.");
            }
        }

        private void FireFlicker(Vector3 drawPosRotated, Vector3 drawSizeRotated, Quaternion quaternion)
        {
            if (scramble == 0)
                scramble = UnityEngine.Random.Range(0, 24735);
            int timeTicks = Find.TickManager.TicksGame;
            int timeTicksScrambled = timeTicks + scramble;
            int interval = timeTicksScrambled / Props.ticks;
            int thisIndex = interval % FireGraphic.SubGraphics.Length;
            int _scramble = Mathf.Abs(thing.thingIDNumber ^ 7419567) / 15;

            if (thisIndex != mem1)
            {
                mem2 = UnityEngine.Random.Range(0, FireGraphic.SubGraphics.Length);
                mem1 = thisIndex;
            }

            Vector3 radial = GenRadial
                .RadialPattern[_scramble % GenRadial.RadialPattern.Length]
                .ToVector3() / GenRadial.MaxRadialPatternRadius;
            radial *= 0.05f;

            Vector3 newPosRotated = drawPosRotated + radial * drawSizeRotated.x;
            Graphic graphic = FireGraphic.SubGraphics[mem2];
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(newPosRotated, quaternion, drawSizeRotated);
            Graphics.DrawMesh(MeshPool.plane10, matrix, graphic.MatSingle, 0);
        }

        public bool ShouldRender(ThingWithComps thing, ThingDef def)
        {
            if (Props.dependency == DependencyType.None) return true;

            if (Props.dependency == DependencyType.Fuel)
            {
                if (refuelableComp == null)
                    return false;
                else
                    return refuelableComp.HasFuel;
            }
            else return false;
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            this.refuelableComp = this.parent.GetComp<CompRefuelable>();
        }

        static CompFireOverlayRotatable()
        {
        }
    }
}
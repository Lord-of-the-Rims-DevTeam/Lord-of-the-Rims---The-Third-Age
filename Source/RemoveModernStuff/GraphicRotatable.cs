using System;
using Verse;

namespace TheThirdAge {

    public enum AutoRotate {
        None,
        Single,
        Multi
    }

    public class GraphicRotatable : Graphic_Collection {
        AutoRotate autoRotate;

        public GraphicRotatable(GraphicRequest req, AutoRotate autoRotate = AutoRotate.None) {
            base.Init(req);

            if (autoRotate != AutoRotate.None) {
                this.autoRotate = autoRotate;
                //ProcessAutoRotate();
            }
        }

        public Graphic First {
            get { return subGraphics[0]; }
        }

        public Graphic[] SubGraphics {
            get { return subGraphics; }
        }

        public Graphic GraphicAt(Rot4 rot) {
            if (autoRotate == AutoRotate.None)
                throw new Exception("Invalid attempt to get an autorotation on _Graphic object");

            switch (rot.AsInt) {
                case 0:
                    return subGraphics[0];
                case 1:
                    return (subGraphics.Length > 1) ? subGraphics[1] : null;
                case 2:
                    return (subGraphics.Length > 2) ? subGraphics[2] : null;
                case 3:
                    return (subGraphics.Length > 3) ? subGraphics[3] : null;
                default:
                    return null;
            }
        }
    }
}

using System;
using System.Linq;
using OpenTK;

namespace CubeSharp
{
    public abstract class MeshFactory {
        public abstract void AddMeshGraphUpon(
                ref MeshGraph mg, bool selected = true);

        public MeshGraph GenerateMeshGraph() {
            MeshGraph mg = new MeshGraph();
            AddMeshGraphUpon(ref mg, false);
            return mg;
        }

        protected void SelectAdjacency(MeshVertex v) {
            v.Selected = true;
            foreach(MeshEdge e in v.Edges) {
                e.Selected = true;
                if(e.F1 != null) e.F1.Selected = true;
                if(e.F2 != null) e.F2.Selected = true;
            }
        }
    }

    public class BoxMeshFactory : MeshFactory {
        public float Length = 2;
        public float Width = 2;
        public float Height = 2;

        public BoxMeshFactory() { }
        public BoxMeshFactory(float l, float w, float h) {
            Length = l; Width = w; Height = h;
        }

        public override void AddMeshGraphUpon(
                ref MeshGraph mg, bool selected = true) {
            float l = Length / 2 , w = Width / 2, h = Height / 2;

            MeshVertex[] vs = new MeshVertex[8];
            vs[0] = mg.AddVertex( l,  h,  w);
            vs[1] = mg.AddVertex( l,  h, -w);
            vs[2] = mg.AddVertex( l, -h,  w);
            vs[3] = mg.AddVertex( l, -h, -w);
            vs[4] = mg.AddVertex(-l,  h,  w);
            vs[5] = mg.AddVertex(-l,  h, -w);
            vs[6] = mg.AddVertex(-l, -h,  w);
            vs[7] = mg.AddVertex(-l, -h, -w);

            mg.AddFacet(vs[2], vs[3], vs[1], vs[0]);
            mg.AddFacet(vs[4], vs[5], vs[7], vs[6]);
            mg.AddFacet(vs[1], vs[5], vs[4], vs[0]);
            mg.AddFacet(vs[2], vs[6], vs[7], vs[3]);
            mg.AddFacet(vs[0], vs[4], vs[6], vs[2]);
            mg.AddFacet(vs[3], vs[7], vs[5], vs[1]);

            if(selected) {
                foreach(MeshVertex v in vs)
                    SelectAdjacency(v);
            }
        }

    }

    public class ArrowFactory : MeshFactory {
        public float Length = 3;
        public float HeadSize = 0.5f;

        public override void AddMeshGraphUpon(
                ref MeshGraph mg, bool selected = true) {
            MeshVertex vorg = mg.AddVertex(0, 0, 0);
            MeshVertex vhead = mg.AddVertex(0, 0, Length);
            MeshVertex vhead1 = mg.AddVertex( HeadSize / 8,  HeadSize / 8, Length - HeadSize);
            MeshVertex vhead2 = mg.AddVertex(-HeadSize / 8,  HeadSize / 8, Length - HeadSize);
            MeshVertex vhead3 = mg.AddVertex(-HeadSize / 8, -HeadSize / 8, Length - HeadSize);
            MeshVertex vhead4 = mg.AddVertex( HeadSize / 8, -HeadSize / 8, Length - HeadSize);

            mg.AddEdge(vorg, vhead);
            mg.AddFacet(vhead1, vhead2, vhead);
            mg.AddFacet(vhead2, vhead3, vhead);
            mg.AddFacet(vhead3, vhead4, vhead);
            mg.AddFacet(vhead4, vhead1, vhead);
        }
    }
}


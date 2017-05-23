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

            MeshVertex v0 = mg.AddVertex( l,  h,  w);
            MeshVertex v1 = mg.AddVertex( l,  h, -w);
            MeshVertex v2 = mg.AddVertex( l, -h,  w);
            MeshVertex v3 = mg.AddVertex( l, -h, -w);
            MeshVertex v4 = mg.AddVertex(-l,  h,  w);
            MeshVertex v5 = mg.AddVertex(-l,  h, -w);
            MeshVertex v6 = mg.AddVertex(-l, -h,  w);
            MeshVertex v7 = mg.AddVertex(-l, -h, -w);

            mg.AddFacet(v2, v3, v1, v0);
            mg.AddFacet(v4, v5, v7, v6);
            mg.AddFacet(v1, v5, v4, v0);
            mg.AddFacet(v2, v6, v7, v3);
            mg.AddFacet(v0, v4, v6, v2);
            mg.AddFacet(v3, v7, v5, v1);

            if(selected) {
                v0.Selected = true; v1.Selected = true;
                v2.Selected = true; v3.Selected = true;
                v4.Selected = true; v5.Selected = true;
                v6.Selected = true; v7.Selected = true;
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


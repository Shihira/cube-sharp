using System;
using System.Linq;
using OpenTK;

namespace CubeSharp
{
    public class BoxMeshFactory {
        public const string Name = "Box";

        public float Length = 2;
        public float Width = 2;
        public float Height = 2;

        public Mesh GenerateMesh() {
            Mesh m = new Mesh();

            int[,] gray_code = new int[4,2] {{0,0}, {1,0}, {1,1}, {0,1}};
            int[] tri_gc = new int[6] {0, 1, 2, 2, 3, 0};

            for(int i = 0; i <= 1; i++)
            for(int j = 0; j <= 1; j++)
            for(int k = 0; k <= 1; k++)
                m.PositionsStorage.Add(new Vector3(
                    (i - 0.5f) * Length,
                    (j - 0.5f) * Width,
                    (k - 0.5f) * Height));

            for(int i = 0; i < 4; i++)
                m.UvsStorage.Add(new Vector2(
                    (float)gray_code[i,0],
                    (float)gray_code[i,1]));

            for(int i = 0; i < 6; i++) {
                int dir = i % 2;
                int facet = i / 2;

                // add normal
                m.NormalsStorage.Add(new Vector3(0, 0, 0));
                var nm = m.NormalsStorage[m.NormalsStorage.Count - 1];
                nm[facet] = dir * 2 - 1;
                m.NormalsStorage[m.NormalsStorage.Count - 1] = nm;

                // add facet
                for(int g_ = 0; g_ < 6; g_++) {
                    int g = tri_gc[dir == 1 ? g_ : 5 - g_];
                    int[] ijk = new int[3];

                    ijk[facet] = dir;
                    ijk[(facet + 1) % 3] = gray_code[g,0];
                    ijk[(facet + 2) % 3] = gray_code[g,1];

                    m.Positions.Indices.Add(ijk[0] << 2 | ijk[1] << 1 | ijk[2]);
                    m.Normals.Indices.Add(i);
                    m.Uvs.Indices.Add(g);
                }
            }

            return m;
        }

        public MeshGraph GenerateMeshGraph() {
            MeshGraph mg = new MeshGraph();
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

            return mg;
        }
    }

    /*
    public class PlaneFactory {
        public float Height = 2;
        public float Wdith = 2;

        public Mesh GenerateMesh() {
        }
    }
    */
}


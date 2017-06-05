using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
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

    public class UVSphereFactory : MeshFactory {
        public float Radius = 2;
        public int USubdivision = 32;
        public int VSubdivision = 16;

        public override void AddMeshGraphUpon(
                ref MeshGraph mg, bool selected = true) {
            MeshVertex[,] vs = new MeshVertex[VSubdivision - 1,USubdivision];
            for(int v = 1; v < VSubdivision; v++)
            for(int u = 0; u < USubdivision; u++) {
                double a = Math.PI * 2 * u / USubdivision;
                double b = - Math.PI * v / VSubdivision + Math.PI / 2;
                vs[v-1,u] = mg.AddVertex(
                    (float)(Radius * Math.Cos(a) * Math.Cos(b)),
                    (float)(Radius * Math.Sin(b)),
                   -(float)(Radius * Math.Sin(a) * Math.Cos(b)));
            }

            for(int v = 0; v < VSubdivision - 2; v++)
            for(int u = 0; u < USubdivision; u++) {
                mg.AddFacet(vs[v,u], vs[v+1,u],
                        vs[v+1, (u+1) % USubdivision], vs[v,(u+1) % USubdivision]);
            }

            MeshVertex top = mg.AddVertex(0, Radius, 0),
                       btm = mg.AddVertex(0, -Radius, 0);
            for(int u = 0; u < USubdivision; u++) {
                mg.AddFacet(top, vs[0, u], vs[0,(u+1) % USubdivision]);
                mg.AddFacet(vs[VSubdivision - 2,(u+1) % USubdivision],
                        vs[VSubdivision - 2, u], btm);
            }

            foreach(MeshVertex v in vs)
                SelectAdjacency(v);
            SelectAdjacency(top);
            SelectAdjacency(btm);
        }
    }

    public class PlaneFactory : MeshFactory {
        public float Size = 2;
        public int USubdivision = 10;
        public int VSubdivision = 10;

        public override void AddMeshGraphUpon(
                ref MeshGraph mg, bool selected = true) {
            MeshVertex[,] vs = new MeshVertex[USubdivision + 1, VSubdivision + 1];

            for(int i = 0; i <= VSubdivision; i++)
            for(int j = 0; j <= USubdivision; j++) {
                vs[i,j] = mg.AddVertex(Size * j / USubdivision - Size / 2,
                        0, -Size * i / VSubdivision - Size / 2);
            }

            for(int i = 0; i < VSubdivision; i++)
            for(int j = 0; j < USubdivision; j++)
                mg.AddFacet(vs[i,j], vs[i,j+1], vs[i+1,j+1], vs[i+1,j]);
        }
    }

    public class CylinderFactory : MeshFactory {
        public float Height = 2;
        public float Radius = 1;
        public int Subdivision = 32;

        public override void AddMeshGraphUpon(
                ref MeshGraph mg, bool selected = true) {
            MeshVertex[] topf = new MeshVertex[Subdivision];
            MeshVertex[] btmf = new MeshVertex[Subdivision];

            for(int i = 0; i < Subdivision; i++) {
                double a = Math.PI * 2 * i / Subdivision;
                topf[i] = mg.AddVertex(Radius * (float)Math.Cos(a),
                        Height / 2, -Radius * (float)Math.Sin(a));
                btmf[i] = mg.AddVertex(Radius * (float)Math.Cos(a),
                        -Height / 2, -Radius * (float)Math.Sin(a));
            }

            mg.AddFacet(topf);
            mg.AddFacet(btmf.Reverse().ToArray());

            for(int i = 0; i < Subdivision; i++) {
                mg.AddFacet(topf[i], btmf[i],
                    btmf[(i+1)%Subdivision], topf[(i+1)%Subdivision]);
            }
        }
    }

    public class WavefrontFactory : MeshFactory {
        public String FileName;

        List<MeshVertex> vertices;
        Vector3 accum_point;

        Vector3 CenterPoint {
            get { return accum_point /= vertices.Count; }
        }

        MeshVertex ParseVertex(String s, MeshGraph mg) {
            Vector3 pos = new Vector3(0, 0, 0);
            String[] nums = s.Trim().Split(' ')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0).ToArray();

            if(nums.Length < 3)
                throw new Exception("Bad vertex format");
            pos[0] = float.Parse(nums[0]);
            pos[1] = float.Parse(nums[1]);
            pos[2] = float.Parse(nums[2]);

            if(nums.Length == 4)
                pos /= float.Parse(nums[3]);

            accum_point += pos;

            MeshVertex mv = mg.AddVertex(pos);
            vertices.Add(mv);
            return mv;
        }

        MeshFacet ParseFacet(String s, MeshGraph mg) {
            String[] items = s.Trim().Split(' ')
                .Select(x => x.Trim())
                .Where(x => x.Length > 0).ToArray();

            List<int> nums = new List<int>();

            foreach(String i in items) {
                String[] snums = i.Split('/');
                nums.Add(int.Parse(snums[0]));
            }

            var vs = nums.Select(n => vertices[n - 1]).ToArray();
            try {
                return mg.AddFacet(vs);
            } catch(Exception) {
                try {
                    return mg.AddFacet(vs.Reverse().ToArray());
                } catch(Exception e2) {
                    Console.WriteLine(e2.Message);
                    Console.WriteLine(e2.StackTrace);
                }
            }

            return null;
        }

        public override void AddMeshGraphUpon(
                ref MeshGraph mg, bool selected = true) {
            StreamReader rd = File.OpenText(FileName);

            vertices = new List<MeshVertex>();
            accum_point = new Vector3(0, 0, 0);

            while(!rd.EndOfStream) {
                String s = rd.ReadLine();

                if(s.StartsWith("v ")) {
                    ParseVertex(s.Substring(2), mg);
                }

                if(s.StartsWith("f ")) {
                    ParseFacet(s.Substring(2), mg);
                }
            }

            foreach(MeshVertex v in vertices) {
                SelectAdjacency(v);
            }
        }

        public void ExportMesh(MeshGraph mg) {
            StreamWriter rd = File.CreateText(FileName);
            rd.WriteLine("g CubeSharp_Model");

            foreach(MeshVertex v in mg.Vertices) {
                rd.WriteLine("v " + v.Position.X + " " + 
                    v.Position.Y + " " + v.Position.Z);
            }

            foreach(MeshFacet f in mg.Facets) {
                var n = f.Normal;

                rd.Write("vn ");
                rd.Write(n[0] + " " + n[1] + " " + n[2]);
                rd.WriteLine();

                rd.Write("f ");
                foreach(MeshVertex v in f.Vertices) {
                    rd.Write(v.Index + 1);
                    rd.Write("//");
                    rd.Write(f.Index + 1); // vn actually
                    rd.Write(" ");
                }
                rd.WriteLine();
            }

            rd.Close();
        }
    }
}


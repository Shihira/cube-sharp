using System;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CubeSharp
{
    public class MeshComponent {
        private MeshGraph parent;
        public MeshGraph Parent {
            get { return parent; }
        }

        private int index;
        public int Index {
            get { return index; }
        }

        internal void SetGraphInfo(MeshGraph g, int i) {
            index = i;
            parent = g;
        }
    }

    public class MeshVertex : MeshComponent {
        internal Dictionary<MeshVertex, MeshEdge> adjacency;

        private Vector3 position;
        public Vector3 Position {
            get { return position; }
            set { position = value; }
        }

        public MeshVertex() {
            adjacency = new Dictionary<MeshVertex, MeshEdge>();
            position = new Vector3(0, 0, 0);
        }
        public MeshVertex(Vector3 v) {
            adjacency = new Dictionary<MeshVertex, MeshEdge>();
            position = v;
        }
        public MeshVertex(float x, float y, float z) {
            adjacency = new Dictionary<MeshVertex, MeshEdge>();
            position = new Vector3(x, y, z);
        }

        public ICollection<MeshVertex> AdjacencyVertices {
            get { return adjacency.Keys; }
        }

        public ICollection<MeshEdge> Edges {
            get { return adjacency.Values; }
        }

        public IEnumerable<MeshFacet> AdjacencyFacets {
            get {
                throw new Exception("Not Implemented");
            }
        }

        public MeshEdge EdgeConnecting(MeshVertex v) {
            MeshEdge m;
            bool success = adjacency.TryGetValue(v, out m);

            if(!success) return null;
            else return m;
        }

        public void ClearAdjacency() {
            List<MeshEdge> tmp = new List<MeshEdge>(Edges);
            foreach(MeshEdge e in tmp)
                Parent.RemoveEdge(e);
        }
    }

    public class MeshEdge : MeshComponent {
        internal MeshVertex p1;
        internal MeshVertex p2;

        // An edge has at most two adjacenct facet,
        // one is positive directed and the other one is negative
        internal MeshFacet f1; // p1 -> p2
        internal MeshFacet f2; // p2 -> p1

        public Tuple<MeshVertex, MeshVertex> VerticesInFacet(MeshFacet f) {
            if(f1 == f)
                return new Tuple<MeshVertex, MeshVertex>(p1, p2);
            if(f2 == f)
                return new Tuple<MeshVertex, MeshVertex>(p2, p1);

            return null;
        }

        public MeshVertex OppositeVertex(MeshVertex v) {
            if(p1 == v) return p2;
            else if(p2 == v) return p1;

            throw new Exception("Edge does not contain this vertex.");
        }

        public MeshEdge(MeshVertex v1, MeshVertex v2) {
            p1 = v1;
            p2 = v2;
        }

        public MeshVertex V1 { get { return p1; } }
        public MeshVertex V2 { get { return p2; } }
        public MeshFacet F1 { get { return f1; } }
        public MeshFacet F2 { get { return f2; } }

        public void ClearAdjacency() {
            if(f1 != null)
                Parent.RemoveFacet(F1);
            if(f2 != null)
                Parent.RemoveFacet(F2);
        }

        public IEnumerable<MeshVertex> Endpoints {
            get { yield return V1; yield return V2; }
        }

        public IEnumerable<MeshFacet> AdjacencyFacets {
            get { yield return F1; yield return F2; }
        }
    }

    public class MeshFacet : MeshComponent {
        internal List<MeshVertex> vertices; // important: ordered

        internal MeshFacet() {
            vertices = new List<MeshVertex>();
        }

        public int TrianglesCount {
            get { return vertices.Count - 2; }
        }

        public MeshFacet(IEnumerable<MeshVertex> input) : this() {
            vertices = new List<MeshVertex>();
            foreach(MeshVertex v in input)
                vertices.Add(v);
        }

        public MeshFacet(params MeshVertex[] input) :
            this((IEnumerable<MeshVertex>)input) { }

        public IEnumerable<MeshVertex> Vertices {
            get { return vertices; }
        }

        public IEnumerable<MeshEdge> Edges {
            get {
                for(int i = 0; i < vertices.Count; i++) {
                    MeshVertex v = vertices[i];
                    MeshVertex next_v = vertices[(i + 1) % vertices.Count];
                    MeshEdge edge = v.EdgeConnecting(next_v);
                    if(edge == null)
                        throw new Exception("Bad vertex");
                    else yield return edge;
                }
            }
        }
    }

    ////////////////////////////////////////////////////////////////////////////

    public class MeshGraph {
        internal List<MeshVertex> vertices;
        internal List<MeshEdge> edges;
        internal List<MeshFacet> facets;
        private int triangles = 0;

        public List<MeshVertex> Vertices { get { return vertices; }}
        public List<MeshEdge> Edges { get { return edges; }}
        public List<MeshFacet> Facets { get { return facets; }}

        public MeshGraph() {
            vertices = new List<MeshVertex>();
            edges = new List<MeshEdge>();
            facets = new List<MeshFacet>();

            attrib_format = new int[] {4, 4};
            attrib_size = attrib_format.Sum();
        }

        public MeshVertex AddVertex(float x, float y, float z) {
            return AddVertex(new MeshVertex(x, y, z));
        }

        public MeshVertex AddVertex(MeshVertex v) {
            int i = vertices.Count;
            vertices.Add(v);
            v.SetGraphInfo(this, i);
            return v;
        }

        public void RemoveVertex(MeshVertex v) {
            int i = v.Index;

            if(i != vertices.Count - 1) {
                vertices[i] = vertices.Last();
                vertices[i].SetGraphInfo(this, i);
            }

            vertices.RemoveAt(vertices.Count - 1);
            v.ClearAdjacency();
        }

        public MeshEdge AddEdge(MeshVertex p1, MeshVertex p2) {
            MeshEdge e = p1.EdgeConnecting(p2);
            if(e != null) return e;

            MeshEdge real_e = new MeshEdge(p1, p2);
            p1.adjacency.Add(p2, real_e);
            p2.adjacency.Add(p1, real_e);

            int i = edges.Count;
            edges.Add(real_e);
            real_e.SetGraphInfo(this, i);
            return real_e;
        }

        public void RemoveEdge(MeshEdge e) {
            int i = e.Index;

            if(i != edges.Count - 1) {
                edges[i] = edges.Last();
                edges[i].SetGraphInfo(this, i);
            }

            edges.RemoveAt(edges.Count - 1);
            e.ClearAdjacency();
            e.p1.adjacency.Remove(e.p2);
            e.p2.adjacency.Remove(e.p1);
        }

        public MeshFacet AddFacet(params MeshVertex[] vs) {
            if(vs.Length < 3) throw new Exception("Vertex count less than 3");

            MeshFacet f = new MeshFacet(vs);

            for(int vi = 0; vi < vs.Length; vi++) {
                MeshVertex p1 = vs[vi];
                MeshVertex p2 = vs[(vi + 1) % vs.Length];
                MeshEdge e = AddEdge(p1, p2);

                if(e.V1 == p1 && e.V2 == p2) {
                    if(e.f1 != null)
                        throw new Exception("Positive facet has been occupied");
                    e.f1 = f;
                } else if(e.V1 == p2 && e.V2 == p1) {
                    if(e.f2 != null)
                        throw new Exception("Negative facet has been occupied");
                    e.f2 = f;
                } else
                    throw new Exception("Unexpected edge");
            }

            int i = facets.Count;
            facets.Add(f);
            f.SetGraphInfo(this, i);

            triangles += f.TrianglesCount;

            return f;
        }

        public void RemoveFacet(MeshFacet f) {
            int i = f.Index;

            triangles -= f.TrianglesCount;

            if(i != facets.Count - 1) {
                facets[i] = facets.Last();
                facets[i].SetGraphInfo(this, i);
            }

            facets.RemoveAt(facets.Count - 1);
            foreach(MeshEdge e in f.Edges) {
                if(e.f1 == f)
                    e.f1 = null;
                else if(e.f2 == f)
                    e.f2 = null;
                else
                    throw new Exception("Unexpected edge");
            }
        }

        public MeshVertex Eqv(MeshVertex v) {
            if(v == null) return null;
            return vertices[v.Index];
        }

        public MeshEdge Eqv(MeshEdge e) {
            if(e == null) return null;
            return edges[e.Index];
        }

        public MeshFacet Eqv(MeshFacet f) {
            if(f == null) return null;
            return facets[f.Index];
        }

        public MeshGraph Clone() {
            MeshGraph mg = new MeshGraph();
            mg.triangles = this.triangles;

            foreach(MeshVertex v in vertices) {
                MeshVertex new_v = new MeshVertex(v.Position);
                new_v.SetGraphInfo(mg, mg.vertices.Count);
                mg.vertices.Add(new_v);
            }

            foreach(MeshEdge e in edges) {
                MeshEdge new_e = new MeshEdge(mg.Eqv(e.V1), mg.Eqv(e.V2));
                new_e.SetGraphInfo(mg, mg.edges.Count);
                mg.edges.Add(new_e);
            }

            foreach(MeshFacet f in facets) {
                MeshFacet new_f = new MeshFacet();
                foreach(MeshVertex v in f.vertices)
                    new_f.vertices.Add(mg.Eqv(v));
                new_f.SetGraphInfo(mg, mg.facets.Count);
                mg.facets.Add(new_f);
            }

            // Maintain adjacency cache
            foreach(MeshEdge e in mg.edges) {
                MeshEdge old_e = this.Eqv(e);
                e.f1 = mg.Eqv(old_e.f1);
                e.f2 = mg.Eqv(old_e.f2);
            }

            foreach(MeshVertex v in mg.vertices) {
                MeshVertex old_v = this.Eqv(v);
                foreach(var entry in old_v.adjacency) {
                    v.adjacency.Add(mg.Eqv(entry.Key), mg.Eqv(entry.Value));
                }
            }

            return mg;
        }

        ////////////////////////////////////////////////////////////////////////
        // OpenGL Relevant

        private int gl_buffer = 0;
        private int gl_buffer_size = 0;
        private int gl_attrib = 0;

        public int TrianglesCount { get { return triangles; } }

        public int GLBuffer {
            get {
                if(gl_buffer == 0)
                    gl_buffer = GL.GenBuffer();

                return gl_buffer;
            }
        }

        public int GLAttrib {
            get {
                if(gl_attrib == 0)
                    gl_attrib = GL.GenVertexArray();

                return gl_attrib;
            }
        }

        private void ReserveSpace(int size) {
            Debug.Assert(size > 0, "Attempt to allocate invalid size of space");

            if(gl_buffer_size < size) {
                int log = (int) Math.Floor(Math.Log(size, 2)) + 1;
                int new_size = (int) Math.Pow(2, log);

                unsafe {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(new_size),
                            new IntPtr(null), BufferUsageHint.StaticDraw);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////

        int[] attrib_format;
        int attrib_size;

        public void UpdateAttrib() {
            GL.BindVertexArray(GLAttrib);
            GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);

            int off = 0;

            for(int i = 0; i < attrib_format.Length; i++) {
                GL.EnableVertexAttribArray(i);
                GL.VertexAttribPointer(i, attrib_format[i],
                        VertexAttribPointerType.Float, true,
                        attrib_size * sizeof(float), off * sizeof(float));
                off += attrib_format[i];
            }
        }

        public void UpdateBuffer() {
            ReserveSpace(this.TrianglesCount * 3 * attrib_size * sizeof(float));

            unsafe {
                GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);
                float* buf = (float*) GL.MapBuffer(
                    BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

                foreach(MeshFacet f in facets) {
                    for(int i = 0; i < f.TrianglesCount; i++) {
                        // Iteration Count: this.TrianglesCount

                        MeshVertex v1 = f.vertices[0];
                        MeshVertex v2 = f.vertices[i + 1];
                        MeshVertex v3 = f.vertices[i + 2];
                        MeshEdge e12 = v1.EdgeConnecting(v2);
                        MeshEdge e23 = v2.EdgeConnecting(v3);
                        MeshEdge e31 = v3.EdgeConnecting(v1);

                        /*
                        Console.WriteLine(v1.Position);
                        Console.WriteLine(v2.Position);
                        Console.WriteLine(v3.Position);
                        */

                        int[] edgeIdx = new int[] {
                            e12 == null ? 0 : (e12.Index+1),
                            e23 == null ? 0 : (e23.Index+1),
                            e31 == null ? 0 : (e31.Index+1), };

                        buf[0] = v1.Position[0];
                        buf[1] = v1.Position[1];
                        buf[2] = v1.Position[2];
                        buf[3] = v1.Index;
                        buf[4] = edgeIdx[0];
                        buf[5] = 0;
                        buf[6] = edgeIdx[2];
                        buf[7] = f.Index;
                        buf += attrib_size;

                        buf[0] = v2.Position[0];
                        buf[1] = v2.Position[1];
                        buf[2] = v2.Position[2];
                        buf[3] = v2.Index;
                        buf[4] = edgeIdx[0];
                        buf[5] = edgeIdx[1];
                        buf[6] = 0;
                        buf[7] = f.Index;
                        buf += attrib_size;

                        buf[0] = v3.Position[0];
                        buf[1] = v3.Position[1];
                        buf[2] = v3.Position[2];
                        buf[3] = v3.Index;
                        buf[4] = 0;
                        buf[5] = edgeIdx[1];
                        buf[6] = edgeIdx[2];
                        buf[7] = f.Index;
                        buf += attrib_size;
                    }
                }

                GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            }
        }
    }
}


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

        public bool Selected {
            get { return Parent.SelectedVertices.Contains(this); }
            set {
                if(value) {
                    Parent.SelectedVertices.Add(this);
                } else {
                    Parent.SelectedVertices.Remove(this);
                }
            }
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
                foreach(var e in Edges) {
                    if(e.F1 != null)
                        yield return e.F1;
                    if(e.F2 != null)
                        yield return e.F2;
                }
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

        public bool Selected {
            get { return Parent.SelectedEdges.Contains(this); }
            set {
                if(value) {
                    Parent.SelectedEdges.Add(this);
                } else {
                    Parent.SelectedEdges.Remove(this);
                }
            }
        }

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

        public bool Selected {
            get { return Parent.SelectedFacets.Contains(this); }
            set {
                if(value) {
                    Parent.SelectedFacets.Add(this);
                } else {
                    Parent.SelectedFacets.Remove(this);
                }
            }
        }

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

        public List<MeshVertex> Vertices {
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

        public HashSet<MeshVertex> SelectedVertices;
        public HashSet<MeshEdge> SelectedEdges;
        public HashSet<MeshFacet> SelectedFacets;

        private int triangles = 0;
        public int TrianglesCount {
            get { return triangles; }
        }

        public List<MeshVertex> Vertices { get { return vertices; }}
        public List<MeshEdge> Edges { get { return edges; }}
        public List<MeshFacet> Facets { get { return facets; }}

        public MeshGraph() {
            vertices = new List<MeshVertex>();
            edges = new List<MeshEdge>();
            facets = new List<MeshFacet>();

            SelectedVertices = new HashSet<MeshVertex>();
            SelectedEdges = new HashSet<MeshEdge>();
            SelectedFacets = new HashSet<MeshFacet>();
        }

        public MeshVertex AddVertex(float x, float y, float z) {
            return AddVertex(new MeshVertex(x, y, z));
        }

        public MeshVertex AddVertex(Vector3 pos) {
            return AddVertex(new MeshVertex(pos));
        }

        public MeshVertex AddVertex(MeshVertex v) {
            int i = vertices.Count;
            vertices.Add(v);
            v.SetGraphInfo(this, i);
            return v;
        }

        public void RemoveVertex(MeshVertex v) {
            int i = v.Index;
            if(vertices[i] != v) return;

            if(i != vertices.Count - 1) {
                vertices[i] = vertices.Last();
                vertices[i].SetGraphInfo(this, i);
            }

            vertices.RemoveAt(vertices.Count - 1);
            v.Selected = false;
            v.ClearAdjacency();
        }

        public MeshEdge AddEdge(MeshVertex p1, MeshVertex p2,
                bool check_facet = false) {
            MeshEdge e = p1.EdgeConnecting(p2);
            if(e != null) return e;

            e = new MeshEdge(p1, p2);
            p1.adjacency.Add(p2, e);
            p2.adjacency.Add(p1, e);

            int i = edges.Count;
            edges.Add(e);
            e.SetGraphInfo(this, i);

            if(check_facet) {
                // check the intersection
                MeshFacet common_facet = null;
                HashSet<MeshFacet> p1f = new HashSet<MeshFacet>(
                        p1.AdjacencyFacets);
                foreach(MeshFacet f in p2.AdjacencyFacets) {
                    if(p1f.Contains(f)) {
                        common_facet = f;
                        break;
                    }
                }

                if(common_facet != null) {
                    List<MeshVertex> f1 = new List<MeshVertex>();
                    List<MeshVertex> f2 = new List<MeshVertex>();

                    List<MeshVertex> current = f1;
                    foreach(MeshVertex v in common_facet.Vertices) {
                        // if current vertex matches p1 or p2, add this vertex
                        // to both vertex list and switch current to the other one
                        current.Add(v);

                        if(v == p1) {
                            current = current == f1 ? f2 : f1;
                            current.Add(p1);
                        } else if(v == p2) {
                            current = current == f1 ? f2 : f1;
                            current.Add(p2);
                        }
                    }

                    this.RemoveFacet(common_facet);
                    this.AddFacet(f1.ToArray());
                    this.AddFacet(f2.ToArray());
                }
            }

            return e;
        }

        public void RemoveEdge(MeshEdge e) {
            int i = e.Index;
            if(edges[i] != e) return;

            if(i != edges.Count - 1) {
                edges[i] = edges.Last();
                edges[i].SetGraphInfo(this, i);
            }

            edges.RemoveAt(edges.Count - 1);
            e.Selected = false;
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
            if(facets[i] != f) return;

            triangles -= f.TrianglesCount;

            if(i != facets.Count - 1) {
                facets[i] = facets.Last();
                facets[i].SetGraphInfo(this, i);
            }

            facets.RemoveAt(facets.Count - 1);
            f.Selected = false;
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

        public void DeselectAll() {
            SelectedVertices.Clear();
            SelectedEdges.Clear();
            SelectedFacets.Clear();
        }

        public void UpdateAll() {
            VertexData.UpdateData();
            EdgeData.UpdateData();
            FacetData.UpdateData();
        }

        public Vector3 SelectedVerticesMidpoint {
            get {
                Vector3 avg = new Vector3(0, 0, 0);
                foreach(MeshVertex v in SelectedVertices)
                    avg += v.Position;
                avg /= SelectedVertices.Count;
                return avg;
            }
        }

        ////////////////////////////////////////////////////////////////////////

        public MeshVertex SplitEdgeAt(MeshEdge e, Vector3 pos) {
            if(Edges[e.Index] != e)
                return null;

            MeshVertex new_v = this.AddVertex(pos);

            // save the vertex sequences of adjacency facets
            List<MeshVertex> f1v = new List<MeshVertex>();
            List<MeshVertex> f2v = new List<MeshVertex>();

            if(e.F1 != null) {
                for(int i = 0; i < e.F1.Vertices.Count; i++) {
                    var vs = e.F1.Vertices;
                    f1v.Add(vs[i]);
                    MeshEdge cur_e = vs[i].EdgeConnecting(vs[(i+1)%vs.Count]);
                    if(cur_e == e) f1v.Add(new_v);
                }
            }

            if(e.F2 != null) {
                for(int i = 0; i < e.F2.Vertices.Count; i++) {
                    var vs = e.F2.Vertices;
                    f2v.Add(vs[i]);
                    MeshEdge cur_e = vs[i].EdgeConnecting(vs[(i+1)%vs.Count]);
                    if(cur_e == e) f2v.Add(new_v);
                }
            }

            // reconstruct facet
            this.RemoveEdge(e);
            if(f1v.Count > 0) this.AddFacet(f1v.ToArray());
            if(f2v.Count > 0) this.AddFacet(f2v.ToArray());

            return new_v;
        }

        public MeshFacet AddTriangle(Vector3 posdir, params MeshVertex[] vs) {
            // There should always be one edge that has connected with
            // another facet to create a correct facet, otherwise the
            // algorithm will create the facet corresponding to posdir
            bool has_connected_edge = false;

            // After sorting, vs[0] -> vs[1] -> vs[2] is a positive order
            for(int i = 0; i < 2; i++)
            for(int j = i + 1; j < 2; j++) {
                MeshEdge e = vs[i].EdgeConnecting(vs[j]);

                if(e == null)
                    continue;

                if(e.F1 != null) {
                    has_connected_edge = true;

                    MeshVertex[] sorted_vs = new MeshVertex[3];
                    sorted_vs[0] = e.V2;
                    sorted_vs[1] = e.V1;
                    sorted_vs[2] = vs[3-i-j];
                    vs = sorted_vs;

                    break;
                }

                if(e.F2 != null) {
                    has_connected_edge = true;

                    MeshVertex[] sorted_vs = new MeshVertex[3];
                    sorted_vs[0] = e.V1;
                    sorted_vs[1] = e.V2;
                    sorted_vs[2] = vs[3-i-j];
                    vs = sorted_vs;

                    break;
                }
            }

            if(!has_connected_edge) {
                Vector3 normal = Vector3.Cross(
                        vs[1].Position - vs[0].Position,
                        vs[2].Position - vs[1].Position);

                if(Vector3.Dot(posdir, normal) < 0) {
                    MeshVertex[] sorted_vs = new MeshVertex[3];
                    sorted_vs[0] = vs[2];
                    sorted_vs[1] = vs[1];
                    sorted_vs[2] = vs[0];
                    vs = sorted_vs;
                }
            }

            return this.AddFacet(vs);
        }

        ////////////////////////////////////////////////////////////////////////
        
        MeshFacetData mfd;
        MeshEdgeData med;
        MeshVertexData mvd;

        public MeshFacetData FacetData {
            get {
                if(mfd == null)
                    mfd = new MeshFacetData(this);
                return mfd;
            }
        }

        public MeshEdgeData EdgeData {
            get {
                if(med == null)
                    med = new MeshEdgeData(this);
                return med;
            }
        }

        public MeshVertexData VertexData {
            get {
                if(mvd == null)
                    mvd = new MeshVertexData(this);
                return mvd;
            }
        }
    }

    public abstract class MeshDataBase {
        private int gl_buffer = 0;
        private int gl_buffer_size = 0;
        private int gl_attrib = 0;

        private int[] layouts;
        private int layout_len;

        protected int[] Layouts {
            get { return layouts; }
        }

        protected int LayoutLength {
            get { return layout_len; }
        }

        private int count = 0;
        public int Count {
            get { return count; }
        }

        public MeshDataBase(params int[] ly) {
            layouts = ly;
            layout_len = layouts.Sum();
        }

        public int GLBuffer {
            get {
                if(gl_buffer == 0)
                    gl_buffer = GL.GenBuffer();

                return gl_buffer;
            }
        }

        public int GLAttrib {
            get {
                if(gl_attrib == 0) {
                    gl_attrib = GL.GenVertexArray();

                    GL.BindVertexArray(gl_attrib);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);

                    int off = 0;
                    for(int i = 0; i < layouts.Length; i++) {
                        GL.EnableVertexAttribArray(i);
                        GL.VertexAttribPointer(i, layouts[i],
                                VertexAttribPointerType.Float, true,
                                LayoutLength * sizeof(float),
                                off * sizeof(float));
                        off += layouts[i];
                    }
                }

                return gl_attrib;
            }
        }

        private IntPtr data = IntPtr.Zero;
        private int offset = 0;

        protected void StartPushing(int count) {
            this.count = count;
            int size = count * LayoutLength * sizeof(float);

            unsafe {
                if(gl_buffer_size < size) {
                    int log = (int) Math.Floor(Math.Log(size, 2)) + 1;
                    int new_size = (int) Math.Pow(2, log);

                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(new_size),
                            new IntPtr(null), BufferUsageHint.StaticDraw);

                    gl_buffer_size = new_size;
                }

                GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);
                data = new IntPtr((void*) GL.MapBuffer(
                    BufferTarget.ArrayBuffer, BufferAccess.WriteOnly));

                Seek(0);
            }
        }

        protected void StopPushing() {
            unsafe {
                GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);
                GL.UnmapBuffer(BufferTarget.ArrayBuffer);
                data = IntPtr.Zero;
            }
        }

        int current_layout = 0;

        protected void Seek(int pos) {
            if(pos * LayoutLength * sizeof(float) >= gl_buffer_size)
                throw new Exception("Seek Out Bound");

            offset = pos * LayoutLength;
            current_layout = 0;
        }

        protected void PushData(params float[] floatdata) {
            unsafe {
                if(data == IntPtr.Zero)
                    throw new Exception("Did not start pushing.");
                if(floatdata.Length != layouts[current_layout])
                    throw new Exception("No such layout.");

                float* ptr = ((float*) data.ToPointer()) + offset;
                for(int i = 0; i < floatdata.Length; i++) {
                    ptr[i] = floatdata[i];
                }

                offset += floatdata.Length;
                current_layout = (current_layout + 1) % layouts.Length;
            }
        }

        public abstract void UpdateData();
    }

    public class MeshFacetData : MeshDataBase {

        MeshGraph Mesh;

        public MeshFacetData() : base(4, 1) { }
        public MeshFacetData(MeshGraph mg) : this() {
            Mesh = mg;
        }

        public override void UpdateData() {
            StartPushing(Mesh.TrianglesCount * 3);

            foreach(MeshFacet f in Mesh.Facets) {
                for(int i = 0; i < f.TrianglesCount; i++) {
                    // Iteration Count: this.TrianglesCount

                    MeshVertex v1 = f.vertices[0];
                    MeshVertex v2 = f.vertices[i + 1];
                    MeshVertex v3 = f.vertices[i + 2];

                    PushData(v1.Position[0], v1.Position[1], v1.Position[2], f.Index);
                    PushData(f.Selected ? 1 : 0);

                    PushData(v2.Position[0], v2.Position[1], v2.Position[2], f.Index);
                    PushData(f.Selected ? 1 : 0);

                    PushData(v3.Position[0], v3.Position[1], v3.Position[2], f.Index);
                    PushData(f.Selected ? 1 : 0);
                }
            }

            StopPushing();
        }
    }

    public class MeshEdgeData : MeshDataBase {
        MeshGraph Mesh;

        public MeshEdgeData() : base(4, 4) { }
        public MeshEdgeData(MeshGraph mg) : this() {
            Mesh = mg;
        }

        public override void UpdateData() {
            StartPushing(Mesh.Edges.Count * 2);

            foreach(MeshEdge e in Mesh.Edges) {
                PushData(e.V1.Position[0], e.V1.Position[1], e.V1.Position[2], e.Index);
                PushData(e.Selected ? 1 : 0, 0, 0, 0);

                PushData(e.V2.Position[0], e.V2.Position[1], e.V2.Position[2], e.Index);
                PushData(e.Selected ? 1 : 0, 0, 0, 0);
            }

            StopPushing();
        }
    }

    public class MeshVertexData : MeshDataBase {
        MeshGraph Mesh;

        public MeshVertexData() : base(4, 4) { }
        public MeshVertexData(MeshGraph mg) : this() {
            Mesh = mg;
        }

        public override void UpdateData() {
            StartPushing(Mesh.Vertices.Count);

            foreach(MeshVertex v in Mesh.Vertices) {
                PushData(v.Position[0], v.Position[1], v.Position[2], v.Index);
                PushData(v.Selected ? 1 : 0, 0, 0, 0);
            }

            StopPushing();
        }
    }
}


using System;
using System.Linq;
using NUnit.Framework;

namespace CubeSharp {

    [TestFixture]
    public class MeshGraphTests {
        [Test]
        public void CreateTriangle() {
            MeshVertex v1 = new MeshVertex(1, 0, 1);
            MeshVertex v2 = new MeshVertex(2, 1, 0);
            MeshVertex v3 = new MeshVertex(1, 3, 1);

            MeshGraph mg = new MeshGraph();
            mg.AddVertex(v1);
            mg.AddVertex(v2);
            mg.AddVertex(v3);

            MeshEdge e1 = mg.AddEdge(v1, v2);
            MeshEdge e2 = mg.AddEdge(v3, v2);
            MeshFacet f = mg.AddFacet(v1, v2, v3);

            CollectionAssert.Contains(mg.Vertices, v1);
            CollectionAssert.Contains(mg.Vertices, v2);
            CollectionAssert.Contains(mg.Vertices, v3);
            Assert.AreEqual(v1.EdgeConnecting(v2), v2.EdgeConnecting(v1), "", e1);
            Assert.AreEqual(v3.EdgeConnecting(v2), v2.EdgeConnecting(v3), "", e2);

            CollectionAssert.Contains(mg.Edges, v1.EdgeConnecting(v2));
            CollectionAssert.Contains(mg.Edges, v2.EdgeConnecting(v3));
            CollectionAssert.Contains(mg.Edges, v3.EdgeConnecting(v1));
            CollectionAssert.Contains(e1.AdjacencyFacets, f);
            CollectionAssert.Contains(e1.Endpoints, v1);

            CollectionAssert.Contains(f.Vertices, v1);
            CollectionAssert.Contains(f.Vertices, v2);
            CollectionAssert.Contains(f.Vertices, v3);
            CollectionAssert.Contains(f.Edges, v1.EdgeConnecting(v2));
            CollectionAssert.Contains(f.Edges, v2.EdgeConnecting(v3));
            CollectionAssert.Contains(f.Edges, v3.EdgeConnecting(v1));

            {   // Remove the facet
                MeshGraph rm = mg.Clone();
                MeshFacet rmf = rm.Eqv(f);
                rm.RemoveFacet(rmf);

                CollectionAssert.DoesNotContain(rm.Facets, rmf);
                CollectionAssert.DoesNotContain(
                        rm.Eqv(e1).AdjacencyFacets, rmf);
                CollectionAssert.DoesNotContain(
                        rm.Eqv(e2).AdjacencyFacets, rmf);
            }

            {   // Remove an edge
                MeshGraph rm = mg.Clone();
                MeshEdge rme = rm.Eqv(e1);
                MeshFacet rmf = rm.Eqv(f);
                rm.RemoveEdge(rme);

                CollectionAssert.DoesNotContain(rm.Edges, rme);
                CollectionAssert.DoesNotContain(rm.Facets, rmf);
                CollectionAssert.DoesNotContain(
                        rme.AdjacencyFacets, rmf);
                CollectionAssert.DoesNotContain(
                        rm.Eqv(e2).AdjacencyFacets, rmf);
            }

            {   // Remove an vertex
                MeshGraph rm = mg.Clone();
                MeshVertex rmv = rm.Eqv(v2);
                MeshEdge rme1 = rm.Eqv(v2.EdgeConnecting(rm.Eqv(v1)));
                MeshEdge rme2 = rm.Eqv(v2.EdgeConnecting(rm.Eqv(v3)));
                MeshFacet rmf = rm.Eqv(f);
                rm.RemoveVertex(rmv);

                CollectionAssert.DoesNotContain(rm.Vertices, rmv);
                CollectionAssert.DoesNotContain(rm.Edges, rme1);
                CollectionAssert.DoesNotContain(rm.Edges, rme2);
                CollectionAssert.DoesNotContain(rm.Facets, rmf);

                Assert.AreEqual(rmv.Edges.Count, 0);
                foreach(var v in rm.Vertices)
                    Assert.AreEqual(v.Edges.Count, 1);
            }
        }
    }
}

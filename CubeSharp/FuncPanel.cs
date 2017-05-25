using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics;

namespace CubeSharp
{
    public class FuncPanel : Panel {
        public MainWindow ParentWindow;

        TableLayoutPanel container;
        Dictionary<string, TableLayoutPanel> groups;

        TableLayoutPanel CreateGroup(string title) {
            GroupBox gb = new GroupBox();
            gb.Text = title;
            gb.Name = "gb_" + title;
            gb.AutoSize = true;
            gb.Dock = DockStyle.Top;
            gb.Padding = new Padding(10, 10, 10, 0);

            TableLayoutPanel gb_cont = new TableLayoutPanel();
            gb_cont.AutoSize = true;
            gb_cont.Dock = DockStyle.Top;

            gb.Controls.Add(gb_cont);
            groups[title] = gb_cont;
            container.Controls.Add(gb, 0, container.RowCount);

            return gb_cont;
        }

        Button button_pressed;
        delegate void Func();

        public FuncPanel() {
            this.AutoScroll = true;

            groups = new Dictionary<string, TableLayoutPanel>();
            container = new TableLayoutPanel();
            container.Dock = DockStyle.Top;
            container.AutoSize = true;

            foreach(MethodInfo mi in this.GetType().GetMethods()) {
                string[] parts = mi.Name.Split('_');
                TableLayoutPanel gb;

                if(parts.Length < 3)
                    continue;
                if(parts[0] != "f")
                    continue;
                if(!groups.TryGetValue(parts[1], out gb))
                    gb = CreateGroup(parts[1]);

                string title = "";
                for(int i = 2; i < parts.Length; i++)
                    title += parts[i] + (i == parts.Length - 1 ? "" : " ");

                Button btn = new Button();
                btn.Text = title;
                btn.Dock = DockStyle.Top;
                btn.FlatStyle = FlatStyle.System;
                btn.Click += (o, e) => {
                    button_pressed = btn;
                    try {
                        Func func = (Func) mi.CreateDelegate(typeof(Func), this);
                        func();
                    } catch(Exception exc) {
                        MessageBox.Show(exc.Message);
                        Console.WriteLine(exc.StackTrace);
                    }
                };

                gb.Controls.Add(btn, 0, gb.RowCount);
            }

            Controls.Add(container);
        }

        MeshGraph model {
            get { return ParentWindow.Model; }
        }

        protected void show_menu() {
            if(button_pressed != null && button_pressed.ContextMenuStrip != null)
                button_pressed.ContextMenuStrip.Show(
                    button_pressed.PointToScreen(new Point(0, 0)) +
                    new Size(0, button_pressed.Size.Height));
        }

        public void f_Edit_Translate() {
            ParentWindow.CurrentTransformer = TransformerType.TranslationTransformer;
        }

        public void f_Edit_Scale() {
            ParentWindow.CurrentTransformer = TransformerType.ScalingTransformer;
        }

        public void f_Edit_Rotate() {
            ParentWindow.CurrentTransformer = TransformerType.RotationTransformer;
        }

        public void f_Edit_Delete() {
            if(button_pressed.ContextMenuStrip == null) {
                ContextMenuStrip cms = new ContextMenuStrip();
                var delete_vertices = cms.Items.Add("Delete Vertices");
                var delete_edges = cms.Items.Add("Delete Edges");
                var delete_facets = cms.Items.Add("Delete Facets");

                delete_vertices.Click += (o, e) => {
                    var s = new HashSet<MeshVertex>(model.SelectedVertices);
                    foreach(MeshVertex v in s)
                        model.RemoveVertex(v);
                    model.UpdateAll();
                };

                delete_edges.Click += (o, e) => {
                    var s = new HashSet<MeshEdge>(model.SelectedEdges);
                    foreach(MeshEdge edge in s)
                        model.RemoveEdge(edge);
                    model.UpdateAll();
                };

                delete_facets.Click += (o, e) => {
                    var s = new HashSet<MeshFacet>(model.SelectedFacets);
                    foreach(MeshFacet f in s)
                        model.RemoveFacet(f);
                    model.UpdateAll();
                };

                button_pressed.ContextMenuStrip = cms;
            }

            show_menu();
        }

        public void f_Edit_Connect() {
            if(model.SelectedVertices.Count == 2) {
                model.AddEdge(model.SelectedVertices.First(),
                        model.SelectedVertices.Last(), true);
            } else if(model.SelectedVertices.Count == 3) {
                // There should always be one edge that has connected with
                // another facet to create a correct facet, otherwise the
                // algorithm will treat the direction of camera as positive.
                MeshVertex[] vs = new MeshVertex[3];
                int vi = 0;
                foreach(MeshVertex v in model.SelectedVertices)
                    vs[vi++] = v;

                Vector3 cam_dir = ParentWindow.MainCamera.Position -
                    vs[0].Position;

                ParentWindow.Model.AddTriangle(cam_dir, vs);
            } else
                throw new Exception("Can only connect 2 vertices with an edge or 3 vertices with a triangle");

            model.UpdateAll();
        }

        public void f_Edit_Join() {
        }

        public void f_Edit_Extrude() {
        }

        public void f_Edit_Split() {
            ParentWindow.IsSplitting = true;
        }

        public void f_Selection_Select_All() {
            foreach(MeshVertex v in ParentWindow.Model.Vertices)
                v.Selected = true;
            foreach(MeshEdge e in ParentWindow.Model.Edges)
                e.Selected = true;
            foreach(MeshFacet f in ParentWindow.Model.Facets)
                f.Selected = true;

            ParentWindow.Model.UpdateAll();
        }

        public void f_Selection_Select_Neighbours() {
            Stack<MeshVertex> vstack = new Stack<MeshVertex>(ParentWindow.Model.SelectedVertices);
            ParentWindow.Model.DeselectAll();

            while(vstack.Count > 0) {
                MeshVertex v = vstack.Pop();
                if(v.Selected) continue;
                v.Selected = true;

                foreach(MeshEdge e in v.Edges) {
                    e.Selected = true;
                    if(e.F1 != null) e.F1.Selected = true;
                    if(e.F2 != null) e.F2.Selected = true;
                }

                foreach(MeshVertex adj in v.AdjacencyVertices) {
                    vstack.Push(adj);
                }
            }

            ParentWindow.Model.UpdateAll();
        }

        public void f_Selection_Deselect_All() {
            ParentWindow.Model.DeselectAll();
            ParentWindow.Model.UpdateAll();
        }

        public void f_Selection_Box_Select() {
            if(button_pressed.ContextMenuStrip == null) {
                ContextMenuStrip cms = new ContextMenuStrip();
                var select_vertices = cms.Items.Add("Select Vertices");
                var select_edges = cms.Items.Add("Select Edges");
                var select_facets = cms.Items.Add("Select Facets");

                select_vertices.Click += (o, e) => {
                    ParentWindow.BoxSelectingType = ObjectType.ModelVertex;
                };

                select_edges.Click += (o, e) => {
                    ParentWindow.BoxSelectingType = ObjectType.ModelEdge;
                };

                select_facets.Click += (o, e) => {
                    ParentWindow.BoxSelectingType = ObjectType.ModelFacet;
                };

                button_pressed.ContextMenuStrip = cms;
            }

            show_menu();
        }

        public void f_View_Wireframe() {
        }

        public void f_View_Shaded() {
        }

        public void f_View_Reset_Camera() {
        }

        public void f_Create_Vertex() {
        }

        public void f_Create_Plane() {
        }

        public void f_Create_Cube() {
            ParentWindow.Model.DeselectAll();
            new BoxMeshFactory().AddMeshGraphUpon(ref ParentWindow.Model);
            ParentWindow.Model.UpdateAll();
        }

        public void f_Create_Sphere() {
        }

        public void f_Create_Cylinder() {
        }
    }
}

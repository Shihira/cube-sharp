using System;
using System.Reflection;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;

namespace CubeSharp
{
    public class FuncPanel : Panel {
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
                btn.Click += (o, e) => mi.Invoke(this, new object[0]);

                gb.Controls.Add(btn, 0, gb.RowCount);
            }

            Controls.Add(container);
        }

        public void f_Transformation_Translate() {
        }

        public void f_Transformation_Scale() {
        }

        public void f_Transformation_Rotate() {
        }

        public void f_Selection_Select_All() {
        }

        public void f_Selection_Deselect_All() {
        }

        public void f_Selection_Box_Select_Vertices() {
        }

        public void f_Selection_Box_Select_Edges() {
        }

        public void f_Selection_Box_Select_Facets() {
        }

        public void f_Rendering_Wireframe() {
        }

        public void f_Rendering_Shaded() {
        }
    }
}

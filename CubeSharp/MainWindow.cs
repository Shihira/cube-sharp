using System;
using System.Windows.Forms;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CubeSharp
{
    public enum ControllerType {
        Translation,
        Scaling,
        Rotation,
    }

    public class MainWindow : Form
    {
        // Attrib
        public MeshGraph Model;
        // Uniform
        Camera cam;
        // Output
        RenderTarget ctl;

        // Shaders
        ScreenRenderer srdr;
        GridRenderer grdr;
        ControllerRenderer crdr;
        TranslationControllerRenderer tcrdr;

        GLControl glc;
        FuncPanel func_panel;
        Timer timer;

        public MainWindow()
        {
            Fake_InitializeComponent();
        }

        private void Fake_InitializeComponent()
        {
            glc = new GLControl(GraphicsMode.Default, 3, 3, GraphicsContextFlags.Default);
            func_panel = new FuncPanel();
            timer = new Timer();

            timer.Tick += (o, e) => glc.Invalidate();
            timer.Interval = 16;
            timer.Start();

            glc.Name = "glc";
            glc.VSync = true;
            glc.Paint += glc_Paint;
            glc.MouseMove += glc_MouseMove;
            glc.MouseMove += (o, e) => glc.Focus();
            glc.MouseUp += glc_MouseUp;
            glc.MouseDown += glc_MouseDown;
            glc.MouseWheel += glc_MouseWheel;
            glc.KeyDown += ModifierKeyDown;
            glc.KeyUp += ModifierKeyUp;
            glc.TabIndex = 0;

            func_panel.MouseMove += (o, e) => func_panel.Focus();
            func_panel.ParentWindow = this;
            func_panel.KeyDown += ModifierKeyDown;
            func_panel.KeyUp += ModifierKeyUp;

            Text = "CubeSharp";
            Name = "CubeSharp";
            Size = new Size(800, 600);
            Font = new Font("sans", 10);

            this.Load += CubeSharp_Load;
            this.Resize += CubeSharp_Resize;
            Controls.Add(glc);
            Controls.Add(func_panel);
        }

        private void CubeSharp_Load(object sender, EventArgs e)
        {
            Console.WriteLine(GL.GetString(StringName.Version));

            Model = new BoxMeshFactory().GenerateMeshGraph();
            srdr = new ScreenRenderer();
            grdr = new GridRenderer();
            tcrdr = new TranslationControllerRenderer();
            crdr = new ControllerRenderer();
            cam = new Camera();
            ctl = new RenderTarget(PixelType.Float, Viewport);

            Model.EdgeData.UpdateData();
            Model.VertexData.UpdateData();
            Model.FacetData.UpdateData();
            cam.Tf.RotateY(-Math.PI / 6);
            cam.Tf.RotateX(-Math.PI / 6);
            cam.Tf.Translate(0, 0, 5);

            srdr.Camera = cam;
            srdr.Model = Model;
            crdr.Camera = cam;
            crdr.Model = Model;
            grdr.Camera = cam;
            tcrdr.Camera = cam;

            CubeSharp_Resize(null, null);
        }

        private void CubeSharp_Resize(object sender, EventArgs e)
        {
            glc.Location = new Point(0, 0);
            glc.Size = ClientSize - new Size(225, 0);
            func_panel.Location = glc.Location + new Size(glc.Size.Width, 0);
            func_panel.Size = new Size(225, glc.Size.Height);

            ctl.Size = Viewport;
            cam.Ratio = Viewport.Width / (double) Viewport.Height;
        }

        private void glc_Paint(object sender, EventArgs e)
        {
            glc.MakeCurrent();
            GL.Viewport(0, 0, Viewport.Width, Viewport.Height);

            ////////////////////////////////////
            RenderTarget.Screen.Use();

            GL.Enable(EnableCap.Multisample);
            GL.ClearColor(0.2f, 0.2f, 0.2f, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            tcrdr.ScreenMode = true;

            grdr.Render(RenderTarget.Screen);
            srdr.Render(RenderTarget.Screen);

            if(Model.SelectedVertices.Count > 0) {
                if(ControllerType == ControllerType.Translation) {
                    tcrdr.ControllerType = ControllerType.Translation;
                    tcrdr.Render(RenderTarget.Screen);
                }
                if(ControllerType == ControllerType.Scaling) {
                    tcrdr.ControllerType = ControllerType.Scaling;
                    tcrdr.Render(RenderTarget.Screen);
                }
            }

            ////////////////////////////////////
            ctl.Use();
            //RenderTarget.Screen.Use();

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            tcrdr.ScreenMode = false;

            crdr.Render(ctl);

            if(Model.SelectedVertices.Count > 0) {
                if(ControllerType == ControllerType.Translation) {
                    tcrdr.ControllerType = ControllerType.Translation;
                    tcrdr.Render(ctl);
                }
                if(ControllerType == ControllerType.Scaling) {
                    tcrdr.ControllerType = ControllerType.Scaling;
                    tcrdr.Render(ctl);
                }
            }

            glc.SwapBuffers();
        }

        private enum DragState {
            None,
            CameraRotation,
            CameraTranslation,
            Controller,
        }

        public Size Viewport {
            get { return glc.Size; }
        }

        private DragState drag_state = DragState.None;
        public ControllerType ControllerType = ControllerType.Translation;
        public Transformation prev_tf;
        private int prev_x;
        private int prev_y;
        private bool shift_down;
        private int tc_axis;
        private float tc_prev_dis;
        private Vector3d prev_insc;

        private void glc_MouseMove(Object sender, MouseEventArgs e)
        {
            int delta_x = e.X - prev_x;
            int delta_y = e.Y - prev_y;

            if(drag_state == DragState.CameraRotation) {
                Transformation tf = new Transformation(prev_tf);

                Matrix4d m = tf.Matrixd * Matrix4d.Identity; m.Transpose();
                Vector4d y = Vector4d.Transform(new Vector4d(0, 1, 0, 0), m);
                //Console.WriteLine(y);

                tf.Translate(0, 0, -5);
                tf.RotateAxis(y.Xyz, -delta_x * (float)Math.PI / 360);
                tf.RotateX(-delta_y * (float)Math.PI / 360);
                tf.Translate(0, 0, 5);
                cam.Tf = tf;
            }

            if(drag_state == DragState.CameraTranslation) {
                Transformation tf = new Transformation(prev_tf);
                tf.Translate(-delta_x / 120.0f, delta_y / 120.0f, 0);
                cam.Tf = tf;
            }

            if(drag_state == DragState.Controller) {

                Vector3 v = tcrdr.Position;
                Vector4 scr_org = Vector4.Transform(
                        new Vector4(
                            (float) prev_insc.X,
                            (float) prev_insc.Y,
                            (float) prev_insc.Z, 1),
                        cam.VPMatrix);
                Vector4 scr_end = new Vector4(v.X, v.Y, v.Z, 1);
                scr_end[tc_axis - 1] += 10;
                scr_end = Vector4.Transform(scr_end, cam.VPMatrix);

                Vector2 dir = (scr_end - scr_org).Normalized().Xy;
                float dis = Vector2.Dot(new Vector2(
                    delta_x, -delta_y), dir); 

                Console.WriteLine(dis);

                if(ControllerType == ControllerType.Translation) {
                    float tl = dis / 50;
                    float prev_tl = tc_prev_dis / 50;

                    Vector3 avg = new Vector3(0, 0, 0);
                    foreach(MeshVertex mv in Model.SelectedVertices) {
                        Vector3 pos = mv.Position;
                        pos[tc_axis - 1] += tl - prev_tl;
                        mv.Position = pos;
                        avg += pos;
                    }
                    avg /= Model.SelectedVertices.Count;

                    tcrdr.Position = avg;
                }

                if(ControllerType == ControllerType.Scaling) {
                    float sl = dis / 100;
                    float prev_sl = tc_prev_dis / 100;

                    Vector3 avg = new Vector3(0, 0, 0);
                    foreach(MeshVertex mv in Model.SelectedVertices)
                        avg += mv.Position;
                    avg /= Model.SelectedVertices.Count;

                    foreach(MeshVertex mv in Model.SelectedVertices) {
                        Vector3 pos = mv.Position;
                        float delta = pos[tc_axis - 1] - avg[tc_axis - 1];
                        float scaled_delta = delta / (1 + prev_sl) * (1 + sl);
                        pos[tc_axis - 1] += scaled_delta - delta;
                        mv.Position = pos;
                    }

                    tcrdr.Position = avg;
                }

                tc_prev_dis = dis;
                Model.UpdateAll();
            }
        }

        private void glc_MouseWheel(Object sender, MouseEventArgs e)
        {
            if(e.Delta > 0) {
                cam.Tf.Scale(0.95);
            } else {
                cam.Tf.Scale(1.05);
            }
        }

        private void glc_MouseUp(Object sender, MouseEventArgs e)
        {
            drag_state = DragState.None;
        }

        private void glc_MouseDown(Object sender, MouseEventArgs e)
        {
            prev_x = e.X;
            prev_y = e.Y;
            prev_tf = new Transformation(cam.Tf);

            if(e.Button == MouseButtons.Middle) {
                drag_state = DragState.CameraRotation;
            }

            if(e.Button == MouseButtons.Right) {
                drag_state = DragState.CameraTranslation;
            }

            if(e.Button == MouseButtons.Left) {
                float[,,] buf = new float[Viewport.Height, Viewport.Width, 4];

                GL.BindTexture(TextureTarget.Texture2D, ctl.TextureColor);
                GL.GetTexImage(TextureTarget.Texture2D, 0,
                        PixelFormat.Rgba, PixelType.Float, buf);

                double hfw = Viewport.Width / 2.0;
                double hfh = Viewport.Height / 2.0;
                int clicky = Viewport.Height - 1 - e.Y, clickx = e.X;

                int index = 0,
                    type = 0,
                    offx = -3,
                    offy = -3;
                float depth = 0;

                for(int i = -3; i <= 3; i++)
                for(int j = -3; j <= 3; j++) {
                    int Y = clicky + i, X = clickx + j;

                    if(buf[Y, X, 1] > type && offx * offx + offy * offy > i*i + j*j) {
                        index = (int) buf[Y, X, 0];
                        type = (int) buf[Y, X, 1];
                        depth = buf[Y, X, 2];

                        offy = i;
                        offx = j;
                    }
                }

                //Console.WriteLine(index + ", " + type + ", " + depth);

                if(type != 0) {
                    Vector4d film_pos = Vector4d.Transform(
                            new Vector4d(
                                (offx + clickx) / hfw - 1,
                                (offy + clicky) / hfh - 1,
                                depth, 1),
                            cam.VPMatrixd.Inverted());
                    film_pos /= film_pos.W;

                    prev_insc = film_pos.Xyz;

                    Console.WriteLine(film_pos.Xyz);
                }

                if(type == 4) {
                    drag_state = DragState.Controller;
                    ControllerType = ControllerType.Translation;
                    tc_axis = index;
                    tc_prev_dis = 0;
                } else if(type == 5) {
                    drag_state = DragState.Controller;
                    ControllerType = ControllerType.Scaling;
                    tc_axis = index;
                    tc_prev_dis = 0;
                } else if(type == 3) {
                    if(!shift_down) Model.DeselectAll();

                    Model.Vertices[index].Selected = true;

                    tcrdr.Position = Model.Vertices[index].Position;
                } else if(type == 2) {
                    if(!shift_down) Model.DeselectAll();

                    Model.Edges[index].Selected = true;
                    Model.Edges[index].V1.Selected = true;
                    Model.Edges[index].V2.Selected = true;

                    tcrdr.Position = (Model.Edges[index].V1.Position +
                        Model.Edges[index].V2.Position) / 2;
                } else if(type == 1) {
                    if(!shift_down) Model.DeselectAll();

                    Model.Facets[index].Selected = true;

                    Vector3 avg = new Vector3(0, 0, 0);
                    float count = 0;
                    foreach(MeshEdge fe in Model.Facets[index].Edges) {
                        fe.Selected = true;
                        fe.V1.Selected = true;
                        fe.V2.Selected = true;
                        avg += fe.V1.Position;
                        avg += fe.V2.Position;
                        count += 2;
                    }
                    avg /= count;
                    tcrdr.Position = avg;
                } else {
                    if(!shift_down) Model.DeselectAll();
                }

                Model.UpdateAll();
            }
        }

        protected void ModifierKeyDown(Object sender, KeyEventArgs e) {
            shift_down = e.Shift;
        }

        protected void ModifierKeyUp(Object sender, KeyEventArgs e) {
            shift_down = e.Shift;
        }

        static public void Main(string[] args)
        {
            var win = new MainWindow();
            Application.Run(win);
        }
    }
}

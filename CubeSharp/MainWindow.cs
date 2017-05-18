using System;
using System.Windows.Forms;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CubeSharp
{
    public class MainWindow : Form
    {
        GLControl glc;

        // Attrib
        MeshGraph msh;
        // Uniform
        Camera cam;
        // Output
        RenderTarget ctl;

        // Shaders
        ScreenRenderer srdr;
        GridRenderer grdr;
        ControllerRenderer crdr;
        TranslationControllerScreenRenderer tcsrdr;

        public MainWindow()
        {
            Text = "Simple";
            Size = new Size(900, 600);
            CenterToScreen();
           
            glc = new GLControl(new GraphicsMode(32, 24, 0, 4),
                    3, 3, GraphicsContextFlags.Default);
            glc.Location = new Point(0, 0);
            glc.Size = Size - new Size(100, 0);
            glc.VSync = true;
            glc.Paint += glc_Paint;
            glc.MouseMove += glc_MouseMove;
            glc.MouseUp += glc_MouseUp;
            glc.MouseDown += glc_MouseDown;

            Controls.Add(glc);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            OnResize(null);

            Console.WriteLine(GL.GetString(StringName.Version));

            msh = new BoxMeshFactory().GenerateMeshGraph();
            srdr = new ScreenRenderer();
            grdr = new GridRenderer();
            tcsrdr = new TranslationControllerScreenRenderer();
            crdr = new ControllerRenderer();
            cam = new Camera();
            ctl = new RenderTarget(PixelType.Float);

            msh.EdgeData.UpdateData();
            msh.VertexData.UpdateData();
            msh.FacetData.UpdateData();
            cam.Tf.Translate(0, 0, 5);

            srdr.Camera = cam;
            srdr.Model = msh;
            crdr.Camera = cam;
            crdr.Model = msh;
            grdr.Camera = cam;
            tcsrdr.Camera = cam;
        }

        protected override void OnResize(EventArgs e)
        {
            if (glc != null) {
                glc.Size = Size - new Size(100, 0);
            }
        }

        private void glc_Paint(object sender, EventArgs e)
        {
            glc.MakeCurrent();
            GL.Viewport(0, 0, 800, 600);

            ////////////////////////////////////
            RenderTarget.Screen.Use();

            GL.Enable(EnableCap.Multisample);
            GL.ClearColor(0.2f, 0.2f, 0.2f, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            grdr.Render(RenderTarget.Screen);
            srdr.Render(RenderTarget.Screen);
            tcsrdr.Render(RenderTarget.Screen);

            ////////////////////////////////////
            ctl.Use();
            //RenderTarget.Screen.Use();

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            crdr.Render(ctl);
            //crdr.Render(RenderTarget.Screen);

            glc.SwapBuffers();
        }

        private enum DragState {
            None,
            CameraRotation,
            CameraTranslation,
        }

        private DragState drag_state;
        private Transformation prev_tf;
        private int prev_x;
        private int prev_y;

        private void glc_MouseMove(Object sender, MouseEventArgs e)
        {
            int delta_x = e.X - prev_x;
            int delta_y = e.Y - prev_y;

            if(drag_state == DragState.CameraRotation) {
                Transformation tf = new Transformation(prev_tf);

                Matrix4 m = tf.Matrix * Matrix4.Identity; m.Transpose();
                Vector4 y = Vector4.Transform(new Vector4(0, 1, 0, 0), m);
                //Console.WriteLine(y);

                tf.Translate(0, 0, -5);
                tf.RotateAxis(y.Xyz, -delta_x * (float)Math.PI / 360);
                tf.RotateX(-delta_y * (float)Math.PI / 360);
                tf.Translate(0, 0, 5);
                cam.Tf = tf;

                glc.Invalidate();
            }

            if(drag_state == DragState.CameraTranslation) {
                Transformation tf = new Transformation(prev_tf);
                tf.Translate(-delta_x / 120.0f, delta_y / 120.0f, 0);
                cam.Tf = tf;

                glc.Invalidate();
            }
        }

        private void glc_MouseUp(Object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Middle &&
                    drag_state == DragState.CameraRotation)
                drag_state = DragState.None;

            if(e.Button == MouseButtons.Right &&
                    drag_state == DragState.CameraTranslation)
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
                float[,,] buf = new float[600, 800, 4];

                unsafe {
                    fixed(float* pbuf = buf) {
                        GL.BindTexture(TextureTarget.Texture2D,
                                ctl.TextureColor);
                        GL.GetTexImage(TextureTarget.Texture2D, 0,
                                PixelFormat.Rgba, PixelType.Float,
                                new IntPtr((void*) pbuf));
                    }
                }

                int d1 = 600 - 1 - e.Y, d2 = e.X;
                int res1 = 0, res2 = 0;
                for(int i = -3; i <= 3; i++)
                for(int j = -3; j <= 3; j++) {
                    int D1 = d1 + i, D2 = d2 + j;

                    if(buf[D1, D2, 1] > res2) {
                        res1 = (int) buf[D1, D2, 0];
                        res2 = (int) buf[D1, D2, 1];
                    }
                }

                if(res2 == 3) {
                    msh.DeselectAll();
                    msh.Vertices[res1].Selected = true;
                    msh.UpdateAll();

                    tcsrdr.Position = msh.Vertices[res1].Position;

                    glc.Invalidate();
                } else if(res2 == 2) {
                    msh.DeselectAll();
                    msh.Edges[res1].Selected = true;
                    msh.Edges[res1].V1.Selected = true;
                    msh.Edges[res1].V2.Selected = true;

                    tcsrdr.Position = (msh.Edges[res1].V1.Position +
                        msh.Edges[res1].V2.Position) / 2;

                    msh.UpdateAll();
                    glc.Invalidate();
                } else if(res2 == 1) {
                    msh.DeselectAll();
                    msh.Facets[res1].Selected = true;

                    Vector3 avg = new Vector3(0, 0, 0);
                    float count = 0;
                    foreach(MeshEdge fe in msh.Facets[res1].Edges) {
                        fe.Selected = true;
                        fe.V1.Selected = true;
                        avg += fe.V1.Position;
                        count += 1;
                    }
                    avg /= count;
                    tcsrdr.Position = avg;

                    msh.UpdateAll();
                    glc.Invalidate();
                }
            }
        }

        static public void Main(string[] args)
        {
            var win = new MainWindow();
            Application.Run(win);
        }
    }
}

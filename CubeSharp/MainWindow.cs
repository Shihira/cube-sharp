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
        Renderer rdr;
        MeshGraph msh;
        MeshFacetData mfd;
        RenderTarget ctl;

        public MainWindow()
        {
            Text = "Simple";
            Size = new Size(900, 600);
            CenterToScreen();
           
            glc = new GLControl(new GraphicsMode(32, 24, 0, 8),
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
            mfd = new MeshFacetData(msh);
            rdr = new Renderer(mfd);
            ctl = new RenderTarget();

            mfd.UpdateData();
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

            GL.ClearColor(0.2f, 0.2f, 0.2f, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            rdr.Mode = Renderer.ScreenMode;
            rdr.Render(RenderTarget.Screen);

            ////////////////////////////////////
            ctl.Use();

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            rdr.Mode = Renderer.ControlMode;
            //rdr.Render(RenderTarget.Screen);
            rdr.Render(ctl);

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
                rdr.CameraTranformation = tf;

                glc.Invalidate();
            }

            if(drag_state == DragState.CameraTranslation) {
                Transformation tf = new Transformation(prev_tf);
                tf.Translate(-delta_x / 120.0f, delta_y / 120.0f, 0);
                rdr.CameraTranformation = tf;

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
            prev_tf = new Transformation(rdr.CameraTranformation);

            if(e.Button == MouseButtons.Middle) {
                drag_state = DragState.CameraRotation;
            }

            if(e.Button == MouseButtons.Right) {
                drag_state = DragState.CameraTranslation;
            }
        }

        static public void Main(string[] args)
        {
            var win = new MainWindow();
            Application.Run(win);
        }
    }
}

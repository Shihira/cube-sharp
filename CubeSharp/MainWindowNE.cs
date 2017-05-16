using System;
using System.Drawing;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CubeSharp
{
    public class MainWindow : GameWindow
    {
        Renderer rdr;
        ScreenRenderer srdr;
        MeshGraph msh;

        public MainWindow() : base(800, 600, GraphicsMode.Default, "Simple",
                GameWindowFlags.Default, DisplayDevice.Default,
                3, 3, GraphicsContextFlags.ForwardCompatible)
        {
            Mouse.ButtonDown += OnMouseDown;
            Mouse.ButtonUp += OnMouseUp;
            Mouse.Move += OnMouseMove;

            Console.WriteLine(GL.GetString(StringName.Version));

            msh = new BoxMeshFactory().GenerateMeshGraph();
            rdr = new Renderer(msh);
            srdr = new ScreenRenderer(rdr.Target);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            OnResize(null);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            MakeCurrent();
            GL.Enable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            rdr.Render();
            srdr.Render();

            SwapBuffers();
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

        protected void OnMouseMove(object sender, MouseMoveEventArgs e)
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
            }

            if(drag_state == DragState.CameraTranslation) {
                Transformation tf = new Transformation(prev_tf);
                tf.Translate(-delta_x / 120.0f, delta_y / 120.0f, 0);
                rdr.CameraTranformation = tf;
            }
        }

        protected void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.Button == MouseButton.Middle &&
                    drag_state == DragState.CameraRotation)
                drag_state = DragState.None;

            if(e.Button == MouseButton.Right &&
                    drag_state == DragState.CameraTranslation)
                drag_state = DragState.None;
        }

        protected void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            prev_x = e.X;
            prev_y = e.Y;
            prev_tf = new Transformation(rdr.CameraTranformation);

            if(e.Button == MouseButton.Middle) {
                drag_state = DragState.CameraRotation;
            }

            if(e.Button == MouseButton.Right) {
                drag_state = DragState.CameraTranslation;
            }
        }

        static public void Main(string[] args)
        {
            new MainWindow().Run();
        }
    }
}

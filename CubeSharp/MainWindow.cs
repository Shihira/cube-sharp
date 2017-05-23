using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CubeSharp
{
    public enum TransformerType {
        TranslationTransformer,
        ScalingTransformer,
        RotationTransformer,
    }

    public enum ObjectType {
        None = 0,

        ModelFacet = 1,
        ModelEdge = 2,
        ModelVertex = 3,
        TranslationTransformer = 4,
        ScalingTransformer = 5,
        RotationTransformer = 6,
    }

    public enum DragState {
        None = 0,

        CameraRotation = 1,
        CameraTranslation = 2,
        TranslationTransformer = 3,
        ScalingTransformer = 4,
        RotationTransformer = 5,
        BoxSelect = 6,
    }

    public class ObjectMapElement {
        public int Index = 0;
        public ObjectType Type = ObjectType.None;
        public Vector3 Position;
        public int ScreenX = -1;
        public int ScreenY = -1;
    }

    public class MainWindow : Form
    {
        // Attrib
        public MeshGraph Model;
        // Uniform
        public Camera MainCamera;
        // Output
        RenderTarget obj_map;
        float[,,] obj_map_buffer;

        // Shaders
        ScreenRenderer srdr;
        ObjectMapRenderer crdr;
        GridRenderer grdr;
        TranslationTransformerRenderer tcrdr;
        ScalingTransformerRenderer scrdr;

        // Controls
        GLControl glc;
        FuncPanel func_panel;
        Timer timer;

        public MainWindow()
        {
            Fake_InitializeComponent();

            DragInfo = new DragInfo_();
            OpObject = new ObjectMapElement();
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

            srdr = new ScreenRenderer();
            crdr = new ObjectMapRenderer();
            grdr = new GridRenderer();
            tcrdr = new TranslationTransformerRenderer();
            scrdr = new ScalingTransformerRenderer();

            Model = new BoxMeshFactory().GenerateMeshGraph();
            MainCamera = new Camera();
            obj_map = new RenderTarget(PixelType.Float, Viewport);

            Model.EdgeData.UpdateData();
            Model.VertexData.UpdateData();
            Model.FacetData.UpdateData();
            MainCamera.Tf.RotateY(-Math.PI / 6);
            MainCamera.Tf.RotateX(-Math.PI / 6);
            MainCamera.Tf.Translate(0, 0, 5);

            srdr.Camera = MainCamera;
            srdr.Model = Model;
            crdr.Camera = MainCamera;
            crdr.Model = Model;
            grdr.Camera = MainCamera;
            tcrdr.Camera = MainCamera;
            tcrdr.Model = Model;
            scrdr.Camera = MainCamera;
            scrdr.Model = Model;

            CubeSharp_Resize(null, null);
        }

        private void CubeSharp_Resize(object sender, EventArgs e)
        {
            glc.Location = new Point(0, 0);
            glc.Size = ClientSize - new Size(225, 0);
            func_panel.Location = glc.Location + new Size(glc.Size.Width, 0);
            func_panel.Size = new Size(225, glc.Size.Height);

            obj_map.Size = Viewport;
            obj_map_buffer = null;
            MainCamera.Ratio = Viewport.Width / (double) Viewport.Height;
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

            grdr.Render(RenderTarget.Screen);
            srdr.Render(RenderTarget.Screen);

            if(Model.SelectedVertices.Count > 0) {
                tcrdr.ScreenMode = true;
                scrdr.ScreenMode = true;

                if(CurrentTransformer == TransformerType.TranslationTransformer)
                    tcrdr.Render(RenderTarget.Screen);
                if(CurrentTransformer == TransformerType.ScalingTransformer)
                    scrdr.Render(RenderTarget.Screen);
            }

            glc.SwapBuffers();
            MarkObjectMap(true);
        }

        void UpdateObjectMap() {
            if(obj_map_buffer == null) {
                obj_map_buffer = new float[Viewport.Height, Viewport.Width, 4];
                MarkObjectMap(true);
            }

            if(IsObjectMapDirty()) {
                obj_map.Use();

                GL.ClearColor(0, 0, 0, 0);
                GL.Clear(ClearBufferMask.ColorBufferBit);
                GL.Clear(ClearBufferMask.DepthBufferBit);

                crdr.Render(obj_map);

                if(Model.SelectedVertices.Count > 0) {
                    tcrdr.ScreenMode = false;
                    scrdr.ScreenMode = false;

                    if(CurrentTransformer == TransformerType.TranslationTransformer)
                        tcrdr.Render(obj_map);
                    if(CurrentTransformer == TransformerType.ScalingTransformer)
                        scrdr.Render(obj_map);
                }

                GL.BindTexture(TextureTarget.Texture2D, obj_map.TextureColor);
                GL.GetTexImage(TextureTarget.Texture2D, 0,
                        PixelFormat.Rgba, PixelType.Float, obj_map_buffer);
                MarkObjectMap(false);
            }
        }

        void MarkObjectMap(bool dirty) {
            if(obj_map_buffer != null)
                obj_map_buffer[0,0,3] = dirty ? -1 : 0;
        }

        bool IsObjectMapDirty() {
            return obj_map_buffer[0,0,3] == -1;
        }


        ObjectMapElement ObjectMapFuzzy(int x, int y, int r = 3) {
            int type = 0;
            int offx = -r, offy = -r;

            UpdateObjectMap();

            for(int i = -r; i <= r; i++)
            for(int j = -r; j <= r; j++) {
                int Y = Viewport.Height-y-1 + i, X = x + j;

                if(obj_map_buffer[Y, X, 1] > type &&
                        offx * offx + offy * offy > i*i + j*j) {
                    type = (int) obj_map_buffer[Y, X, 1];
                    offy = i;
                    offx = j;
                }
            }

            double hfw = Viewport.Width / 2.0, hfh = Viewport.Height / 2.0;
            int max_x = x + offx, max_y = Viewport.Height-y-1 + offy;

            ObjectMapElement me = new ObjectMapElement();
            // in controller map, index-0 means emptiness
            me.Index = ((int)obj_map_buffer[max_y, max_x, 0]) - 1;
            me.Type = (ObjectType)((int) obj_map_buffer[max_y, max_x, 1]);

            if(type != 0) {
                Vector4d film_pos = Vector4d.Transform(
                        new Vector4d(
                            max_x / hfw - 1,
                            max_y / hfh - 1,
                            obj_map_buffer[max_y, max_x, 2], 1),
                        MainCamera.VPMatrixd.Inverted());
                film_pos /= film_pos.W;
                me.Position = (Vector3) film_pos.Xyz;
            }

            me.ScreenX = max_x;
            me.ScreenY = Viewport.Height - max_y - 1;

            return me;
        }

        public Size Viewport {
            get { return glc.Size; }
        }

        ////////////////////////////////////////////////////////////////////////

        // Selection Relevant
        public bool DeselectOnClick = true;
        public bool IsSelecting = false;

        // Dragging Relevant
        public class DragInfo_ {
            public int StartX = -1;
            public int StartY = -1;
            public int CurrentX = -1;
            public int CurrentY = -1;
            public int DeltaX { get { return CurrentX - StartX; } }
            public int DeltaY { get { return CurrentY - StartY; } }

            public DragState State = DragState.None;
            public object StartInfo;
            public bool WasDragging = false;

            public void Reset() {
                State = DragState.None;
                StartInfo = null;
                WasDragging = false;
            }
        }
        public DragInfo_ DragInfo;

        // Misc.
        public TransformerType CurrentTransformer = TransformerType.TranslationTransformer;
        public ObjectMapElement OpObject; // Object Being Operated

        ////////////////////////////////////////////////////////////////////////
        // Raw Events

        protected void ModifierKeyDown(Object sender, KeyEventArgs e) {
            DeselectOnClick = false;
        }

        protected void ModifierKeyUp(Object sender, KeyEventArgs e) {
            DeselectOnClick = true;
        }

        private void glc_MouseMove(Object sender, MouseEventArgs e)
        {
            DragState s = DragInfo.State;

            if(s != DragState.None) {
                DragInfo.CurrentX = e.X;
                DragInfo.CurrentY = e.Y;

                if(s == DragState.CameraRotation)
                    CameraRotation_Drag();
                else if(s == DragState.CameraTranslation)
                    CameraTranslation_Drag();
                else if(s == DragState.TranslationTransformer)
                    TranslationTransformer_Drag();
                else if(s == DragState.ScalingTransformer)
                    ScalingTransformer_Drag();
                else
                    throw new Exception("Unhandled dragging type.");

                DragInfo.WasDragging = true;
            }
        }

        private void glc_MouseWheel(Object sender, MouseEventArgs e)
        {
            if(e.Delta > 0) {
                MainCamera.Tf.Scale(0.95);
            } else {
                MainCamera.Tf.Scale(1.05);
            }
        }

        private void glc_MouseUp(Object sender, MouseEventArgs e)
        {
            DragInfo.Reset();
        }

        private void glc_MouseDown(Object sender, MouseEventArgs e)
        {
            if(DeselectOnClick && Model.SelectedVertices.Count > 0) {
                Model.DeselectAll();
                Model.UpdateAll();
            }

            DragInfo.StartX = e.X;
            DragInfo.StartY = e.Y;

            if(e.Button == MouseButtons.Middle) {
                DragInfo.State = DragState.CameraRotation;
                DragInfo.StartInfo = new Transformation(MainCamera.Tf);
            }

            if(e.Button == MouseButtons.Right) {
                DragInfo.State = DragState.CameraTranslation;
                DragInfo.StartInfo = new Transformation(MainCamera.Tf);
            }

            if(e.Button == MouseButtons.Left) {
                OpObject = ObjectMapFuzzy(DragInfo.StartX, DragInfo.StartY);

                if(OpObject.Type == ObjectType.ModelVertex)
                    ModelVertex_Click();
                else if(OpObject.Type == ObjectType.ModelEdge)
                    ModelEdge_Click();
                else if(OpObject.Type == ObjectType.ModelFacet)
                    ModelFacet_Click();
                else {
                    if(OpObject.Type == ObjectType.RotationTransformer)
                        DragInfo.State = DragState.RotationTransformer;
                    else if(OpObject.Type == ObjectType.ScalingTransformer)
                        DragInfo.State = DragState.ScalingTransformer;
                    else if(OpObject.Type == ObjectType.TranslationTransformer)
                        DragInfo.State = DragState.TranslationTransformer;

                    var start_pos = new List<Tuple<int, Vector3>>();
                    foreach(MeshVertex v in Model.SelectedVertices)
                        start_pos.Add(new Tuple<int, Vector3>(v.Index, v.Position));
                    DragInfo.StartInfo = start_pos;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////
        /// Semantic Events

        void ModelVertex_Click() {
            Model.Vertices[OpObject.Index].Selected = true;
            Model.UpdateAll();
        }

        void ModelEdge_Click() {
            Model.Edges[OpObject.Index].Selected = true;
            Model.Edges[OpObject.Index].V1.Selected = true;
            Model.Edges[OpObject.Index].V2.Selected = true;
            Model.UpdateAll();
        }

        void ModelFacet_Click() {
            Model.Facets[OpObject.Index].Selected = true;

            foreach(MeshEdge fe in Model.Facets[OpObject.Index].Edges) {
                fe.Selected = true;
                fe.V1.Selected = true;
                fe.V2.Selected = true;
            }

            Model.UpdateAll();
        }

        void CameraRotation_Drag() {
            Transformation tf = new Transformation(
                    (Transformation)DragInfo.StartInfo);

            Matrix4d m = tf.Matrixd * Matrix4d.Identity;
            m.Transpose();
            Vector4d y = Vector4d.Transform(new Vector4d(0, 1, 0, 0), m);

            tf.Translate(0, 0, -5);
            tf.RotateAxis(y.Xyz, -DragInfo.DeltaX * (float)Math.PI / 360);
            tf.RotateX(-DragInfo.DeltaY * (float)Math.PI / 360);
            tf.Translate(0, 0, 5);
            MainCamera.Tf = tf;
        }

        void CameraTranslation_Drag() {
            Transformation tf = new Transformation(
                    (Transformation)DragInfo.StartInfo);
            tf.Translate(-DragInfo.DeltaX / 120.0f, DragInfo.DeltaY / 120.0f, 0);
            MainCamera.Tf = tf;
        }

        void TranslationTransformer_Drag() {
            Vector2 dir = tcrdr.ScreenVector(OpObject.Index);
            float dis = Vector2.Dot(new Vector2(
                        DragInfo.DeltaX, DragInfo.DeltaY), dir); 
            dis /= 50;

            foreach(var t in (List<Tuple<int, Vector3>>)DragInfo.StartInfo) {
                Vector3 pos = t.Item2;
                pos[OpObject.Index] += dis;
                Model.Vertices[t.Item1].Position = pos;
            }

            Model.UpdateAll();
        }

        void ScalingTransformer_Drag() {
            Vector2 dir = scrdr.ScreenVector(OpObject.Index);
            float dis = Vector2.Dot(new Vector2(
                        DragInfo.DeltaX, DragInfo.DeltaY), dir); 
            dis /= 50;
            Vector3 center = scrdr.Position;

            foreach(var t in (List<Tuple<int, Vector3>>)DragInfo.StartInfo) {
                Vector3 pos = t.Item2;
                float delta = pos[OpObject.Index] - center[OpObject.Index];
                pos[OpObject.Index] += dis * delta;
                Console.WriteLine(pos);
                Model.Vertices[t.Item1].Position = pos;
            }

            Model.UpdateAll();
        }

        ////////////////////////////////////////////////////////////////////////
        /// Main

        static public void Main(string[] args)
        {
            var win = new MainWindow();
            Application.Run(win);
        }
    }
}

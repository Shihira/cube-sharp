using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CubeSharp
{
    public class Transformation {

        public void Translate(double x, double y, double z)
        {
            matrix = Matrix4d.CreateTranslation(x, y, z) * matrix;
            inverse_matrix *= Matrix4d.CreateTranslation(-x, -y, -z);
        }

        public void Translate(Vector3d tl)
        {
            matrix = Matrix4d.CreateTranslation(tl) * matrix;
            inverse_matrix *= Matrix4d.CreateTranslation(-tl);
        }

        public void RotateX(double angle)
        {
            matrix = Matrix4d.CreateRotationX(angle) * matrix;
            inverse_matrix *= Matrix4d.CreateRotationX(-angle);
        }

        public void RotateY(double angle)
        {
            matrix = Matrix4d.CreateRotationY(angle) * matrix;
            inverse_matrix *= Matrix4d.CreateRotationY(-angle);
        }

        public void RotateZ(double angle)
        {
            matrix = Matrix4d.CreateRotationZ(angle) * matrix;
            inverse_matrix *= Matrix4d.CreateRotationZ(-angle);
        }

        public void RotateAxis(Vector3d axis, double angle)
        {
            matrix = Matrix4d.CreateFromAxisAngle(axis, angle) * matrix;
            inverse_matrix *= Matrix4d.CreateFromAxisAngle(axis, -angle);
        }

        public void Scale(double s)
        {
            matrix *= Matrix4d.Scale(s);
            inverse_matrix = Matrix4d.Scale(1 / s) * inverse_matrix;
        }

        private Matrix4d matrix;
        public Matrix4 Matrix {
            get { return Matrix4dToMatrix4(matrix); }
        }
        public Matrix4d Matrixd {
            get { return matrix; }
        }

        private Matrix4d inverse_matrix;
        public Matrix4 InverseMatrix {
            get { return Matrix4dToMatrix4(inverse_matrix); }
        }
        public Matrix4d InverseMatrixd {
            get { return inverse_matrix; }
        }

        public Transformation() {
            matrix = Matrix4d.Identity;
            inverse_matrix = Matrix4d.Identity;
        }

        public Transformation(Transformation other) {
            matrix = other.matrix * Matrix4d.Identity;
            inverse_matrix = other.inverse_matrix * Matrix4d.Identity;
        }

        static public Matrix4 Matrix4dToMatrix4(Matrix4d m) {
            Matrix4 new_m = new Matrix4();
            new_m.M11 = (float) m.M11; new_m.M12 = (float) m.M12; new_m.M13 = (float) m.M13; new_m.M14 = (float) m.M14;
            new_m.M21 = (float) m.M21; new_m.M22 = (float) m.M22; new_m.M23 = (float) m.M23; new_m.M24 = (float) m.M24;
            new_m.M31 = (float) m.M31; new_m.M32 = (float) m.M32; new_m.M33 = (float) m.M33; new_m.M34 = (float) m.M34;
            new_m.M41 = (float) m.M41; new_m.M42 = (float) m.M42; new_m.M43 = (float) m.M43; new_m.M44 = (float) m.M44;
            return new_m;
        }

        static public Matrix4d Matrix4ToMatrix4d(Matrix4 m) {
            Matrix4d new_m = new Matrix4d();
            new_m.M11 = m.M11; new_m.M12 = m.M12; new_m.M13 = m.M13; new_m.M14 = m.M14;
            new_m.M21 = m.M21; new_m.M22 = m.M22; new_m.M23 = m.M23; new_m.M24 = m.M24;
            new_m.M31 = m.M31; new_m.M32 = m.M32; new_m.M33 = m.M33; new_m.M34 = m.M34;
            new_m.M41 = m.M41; new_m.M42 = m.M42; new_m.M43 = m.M43; new_m.M44 = m.M44;
            return new_m;
        }
    }

    public class RenderTarget {
        private int framebuffer = -1;
        private int texture_color = -1;
        private int texture_depth = -1;

        Size size;

        public Size Size {
            get { return size; }
            set {
                if(framebuffer > 0) {
                    GL.DeleteFramebuffer(framebuffer);
                    GL.DeleteTexture(texture_color);
                    GL.DeleteTexture(texture_depth);

                    framebuffer = -1;
                    texture_color = -1;
                    texture_depth = -1;
                }

                size = value;
            }
        }

        PixelType type;

        public RenderTarget(PixelType t, Size sz) {
            type = t;
            size = sz;
        }

        private void init() {
            framebuffer = GL.GenFramebuffer();
            texture_color = GL.GenTexture();
            texture_depth = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texture_color);
            if(type == PixelType.Byte)
                GL.TexImage2D(TextureTarget.Texture2D, 0,
                        PixelInternalFormat.Rgba8, size.Width, size.Height, 0,
                        PixelFormat.Rgba, PixelType.Byte, (IntPtr)0);
            else if(type == PixelType.Float)
                GL.TexImage2D(TextureTarget.Texture2D, 0,
                        PixelInternalFormat.Rgba32f, size.Width, size.Height, 0,
                        PixelFormat.Rgba, PixelType.Float, (IntPtr)0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, texture_depth);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                    PixelInternalFormat.DepthComponent32f, size.Width, size.Height, 0,
                    PixelFormat.DepthComponent, PixelType.Float, (IntPtr)0);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.ColorAttachment0,
                    texture_color, 0);
            GL.FramebufferTexture(FramebufferTarget.Framebuffer,
                    FramebufferAttachment.DepthAttachment,
                    texture_depth, 0);
        }

        public int Framebuffer {
            get {
                if(framebuffer == -1) init();
                return framebuffer;
            }
        }

        public int TextureColor {
            get {
                if(IsScreen)
                    throw new Exception("Cannot get screen color");
                if(texture_color == -1) init();
                return texture_color;
            }
        }

        public int TextureDepth {
            get {
                if(IsScreen)
                    throw new Exception("Cannot get screen depth");
                if(texture_depth == -1) init();
                return texture_depth;
            }
        }

        public bool IsScreen {
            get { return framebuffer == 0; }
        }

        static RenderTarget screen;
        static public RenderTarget Screen {
            get {
                if(screen == null || screen.framebuffer != 0) {
                    screen = new RenderTarget(PixelType.Byte, new Size(0, 0));
                    screen.framebuffer = 0;
                }

                return screen;
            }
        }

        public void Use() {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
        }
    }

    public abstract class RendererBase {
        string vertex_shader_source;
        string fragment_shader_source;
        string geometry_shader_source;

        int vertex_shader = 0;
        int fragment_shader = 0;
        int geometry_shader = 0;
        int program = 0;

        public int Program {
            get {
                if(program != 0)
                    return program;
                if(vertex_shader_source.Length == 0)
                    throw new Exception("No Vertex Source");
                if(fragment_shader_source.Length == 0)
                    throw new Exception("No Fragment Source");

                vertex_shader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertex_shader, vertex_shader_source);
                GL.CompileShader(vertex_shader);
                string vs_log = GL.GetShaderInfoLog(vertex_shader);
                if(vs_log.Length > 0)
                    Console.WriteLine("Vertex Comp: " + vs_log);

                fragment_shader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragment_shader, fragment_shader_source);
                GL.CompileShader(fragment_shader);
                string fs_log = GL.GetShaderInfoLog(fragment_shader);
                if(fs_log.Length > 0)
                    Console.WriteLine("Fragment Comp: " + fs_log);

                if(geometry_shader_source.Length > 0) {
                    geometry_shader = GL.CreateShader(ShaderType.GeometryShader);
                    GL.ShaderSource(geometry_shader, geometry_shader_source);
                    GL.CompileShader(geometry_shader);
                    string gs_log = GL.GetShaderInfoLog(geometry_shader);
                    if(gs_log.Length > 0)
                        Console.WriteLine("Geometry Comp: " + gs_log);
                }

                program = GL.CreateProgram();
                GL.AttachShader(program, vertex_shader);
                GL.AttachShader(program, fragment_shader);
                if(geometry_shader != 0)
                    GL.AttachShader(program, geometry_shader);
                GL.LinkProgram(program);
                string ln_log = GL.GetProgramInfoLog(program);
                if(ln_log.Length > 0)
                    Console.WriteLine("Link: " + ln_log);

                return program;
            }
        }

        protected void SetSource(string vsrc, string gsrc, string fsrc) {
            vertex_shader_source = vsrc;
            geometry_shader_source = gsrc;
            fragment_shader_source = fsrc;
        }

        public abstract void Render(RenderTarget rt);

        Dictionary<string, int> uniforms;
        public int Uniform(string name) {
            if(uniforms == null)
                uniforms = new Dictionary<string, int>();

            int id;
            bool suc = uniforms.TryGetValue(name, out id);

            if(!suc) {
                GL.GetUniformIndices(Program, 1,
                    new string[] { name }, out id);
                uniforms[name] = id;
            }

            return id;
        }
    }

    public class Camera {
        public Transformation Tf;

        double view_angle = Math.PI / 2;
        public double ViewAngle {
            get { return view_angle; }
            set {
                if(value < Math.PI - 0.01)
                    view_angle = value;
                else
                    view_angle = Math.PI - 0.01;
            }
        }

        double ratio = 1;
        public double Ratio {
            get { return ratio; }
            set { ratio = value; }
        }

        public Camera() {
            Tf = new Transformation();
        }

        public Vector3 Position {
            get {
                return Tf.Matrix.ExtractTranslation();
            }
        }

        public Matrix4 VPMatrix {
            get {
                Matrix4 m = Tf.InverseMatrix;
                m *= Matrix4.CreatePerspectiveFieldOfView(
                        (float)ViewAngle, (float) ratio, 0.1f, 100);
                return m;
            }
        }

        public Matrix4d VPMatrixd {
            get {
                Matrix4d m = Tf.InverseMatrixd;
                m *= Matrix4d.CreatePerspectiveFieldOfView(
                        (float)ViewAngle, ratio, 0.1f, 100);
                return m;
            }
        }
    }

    public abstract class RendererWithCamera : RendererBase {
        public Camera Camera;
        Matrix4 prev_mat;

        public RendererWithCamera() { }

        protected void UpdateCamera(string name) {
            GL.UseProgram(Program);

            Matrix4 vp_mat = Camera.VPMatrix;
            if(prev_mat != vp_mat) {
                GL.UniformMatrix4(Uniform(name), false, ref vp_mat);
                prev_mat = vp_mat;
            }

        }
    }

    public enum DrawMode {
        Vertex = 1, Edge = 2, Facet = 4, All = 7,
    }

    public class WireframeRenderer : RendererWithCamera {
        string vertex_shader_source = @"
        #version 330 core

        uniform mat4 vp;

        layout(location = 0) in vec4 position;
        layout(location = 1) in vec4 info;

        out float selected;

        void main()
        {
            vec4 p = vec4(position.xyz, 1);
            gl_Position = vp * p;
            selected = info.x;
        }";

        string fragment_shader_source = @"
        #version 330 core

        uniform ivec4 mode;

        in float selected;
        out vec4 outColor;

        void main()
        {
            if(mode.x == 3)
                outColor = vec4(1.0, 1.0, 1.0, 1);
            else if(mode.x == 2)
                outColor = vec4(0.8, 0.8, 0.8, 1);
            else if(mode.x == 1)
                outColor = vec4(0.5, 0.5, 0.5, 1);
            else
                outColor = vec4(0, 0, 0, 1);

            if(selected > 0.99) {
                outColor.rgb = outColor.rgb * 0.3;
                outColor.r = 1;
            }
        }";

        public MeshGraph Model;

        DrawMode Mode = DrawMode.All;

        public WireframeRenderer()
        {
            SetSource(vertex_shader_source, "", fragment_shader_source);
        }

        public override void Render(RenderTarget rt) {
            rt.Use();
            GL.UseProgram(Program);
            UpdateCamera("vp");

            GL.Disable(EnableCap.CullFace);

            GL.Enable(EnableCap.DepthTest);
            if((Mode & DrawMode.Facet) != 0) {
                GL.BindVertexArray(Model.FacetData.GLAttrib);
                GL.Uniform4(Uniform("mode"), (int)ObjectType.ModelFacet, 0, 0, 0);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.DrawArrays(BeginMode.Triangles, 0, Model.FacetData.Count);
            }

            GL.Disable(EnableCap.DepthTest);
            if((Mode & DrawMode.Edge) != 0) {
                GL.BindVertexArray(Model.EdgeData.GLAttrib);
                GL.Uniform4(Uniform("mode"), (int)ObjectType.ModelEdge, 0, 0, 0);
                GL.LineWidth(1);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.DrawArrays(BeginMode.Lines, 0, Model.EdgeData.Count);
            }

            if((Mode & DrawMode.Vertex) != 0) {
                GL.BindVertexArray(Model.VertexData.GLAttrib);
                GL.Uniform4(Uniform("mode"), (int)ObjectType.ModelVertex, 0, 0, 0);
                GL.PointSize(3);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                GL.DrawArrays(BeginMode.Points, 0, Model.VertexData.Count);
            }
        }
    }

    public class ShadedRenderer : RendererWithCamera {
        string vertex_shader_source = @"
        #version 330 core

        uniform mat4 vp;

        layout(location = 0) in vec4 attr_position;
        layout(location = 1) in vec3 attr_normal;

        out vec3 normal;
        out vec4 position;

        void main()
        {
            position = attr_position;
            normal = attr_normal;
            vec4 p = vec4(position.xyz, 1);
            gl_Position = vp * p;
        }";

        string fragment_shader_source = @"
        #version 330 core

        uniform vec3 light;
        uniform mat4 vp;

        in vec3 normal;
        in vec4 position;
        out vec4 outColor;

        void main()
        {
            float c = dot(normalize(light - position.xyz), normal);
            if(c < 0) c = -c;
            c = clamp(c, 0.1, 1);
            outColor = vec4(c, c, c, 1);
        }";

        public MeshGraph Model;

        public ShadedRenderer()
        {
            SetSource(vertex_shader_source, "", fragment_shader_source);
        }

        public override void Render(RenderTarget rt) {
            rt.Use();
            GL.UseProgram(Program);
            UpdateCamera("vp");

            //GL.Enable(EnableCap.CullFace);
            GL.Disable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            Vector3 v = Camera.Tf.Matrix.ExtractTranslation();
            GL.Uniform3(Uniform("light"), ref v);
            GL.BindVertexArray(Model.FacetDataWithNormal.GLAttrib);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.DrawArrays(BeginMode.Triangles, 0, Model.FacetDataWithNormal.Count);
        }
    }

    public class ObjectMapRenderer : RendererWithCamera {
        string vertex_shader_source = @"
        #version 330 core

        uniform mat4 vp;

        layout(location = 0) in vec4 position;

        out float index;
        out vec4 pos;

        void main()
        {
            pos = vec4(position.xyz, 1);
            gl_Position = vp * pos;
            index = position.w;
        }";

        string fragment_shader_source = @"
        #version 330 core

        uniform ivec4 mode;

        in float index;
        in vec4 pos;
        out vec4 outColor;

        void main()
        {
            uint idx = uint(round(index) + 1);
            float val = uintBitsToFloat(uint(mode.x << 28) | idx);
            outColor = vec4(val, pos.x, pos.y, pos.z);
        }";

        public MeshGraph Model;

        DrawMode Mode = DrawMode.All;

        public ObjectMapRenderer()
        {
            SetSource(vertex_shader_source, "", fragment_shader_source);
        }

        public override void Render(RenderTarget rt) {
            rt.Use();
            GL.UseProgram(Program);
            UpdateCamera("vp");

            GL.Disable(EnableCap.CullFace);

            GL.Enable(EnableCap.DepthTest);
            if((Mode & DrawMode.Facet) != 0) {
                GL.BindVertexArray(Model.FacetData.GLAttrib);
                GL.Uniform4(Uniform("mode"), (int)ObjectType.ModelFacet, 0, 0, 0);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.DrawArrays(BeginMode.Triangles, 0, Model.FacetData.Count);
            }

            GL.Disable(EnableCap.DepthTest);
            if((Mode & DrawMode.Edge) != 0) {
                GL.BindVertexArray(Model.EdgeData.GLAttrib);
                GL.Uniform4(Uniform("mode"), (int)ObjectType.ModelEdge, 0, 0, 0);
                GL.LineWidth(1);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.DrawArrays(BeginMode.Lines, 0, Model.EdgeData.Count);
            }

            if((Mode & DrawMode.Vertex) != 0) {
                GL.BindVertexArray(Model.VertexData.GLAttrib);
                GL.Uniform4(Uniform("mode"), (int)ObjectType.ModelVertex, 0, 0, 0);
                GL.PointSize(3);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                GL.DrawArrays(BeginMode.Points, 0, Model.VertexData.Count);
            }
        }
    }

    public class GridRenderer : RendererWithCamera {
        MeshGraph grid;

        string vertex_shader_source = @"
        #version 330 core

        uniform mat4 vp;

        layout(location = 0) in vec4 position;

        void main()
        {
            vec4 p = vec4(position.xyz, 1);
            gl_Position = vp * p;
        }";

        string fragment_shader_source = @"
        #version 330 core

        out vec4 outColor;

        void main()
        {
            outColor = vec4(0.3, 0.3, 0.3, 1);
        }";

        public GridRenderer() {
            SetSource(vertex_shader_source, "", fragment_shader_source);

            grid = new MeshGraph();
            for(int i = -50; i < 50; i++) {
                MeshVertex v1 = grid.AddVertex(i, 0, -50);
                MeshVertex v2 = grid.AddVertex(i, 0, 50);
                grid.AddEdge(v1, v2);
            }

            for(int i = -50; i < 50; i++) {
                MeshVertex v1 = grid.AddVertex(-50, 0, i);
                MeshVertex v2 = grid.AddVertex(50, 0, i);
                grid.AddEdge(v1, v2);
            }

            grid.EdgeData.UpdateData();
        }

        public override void Render(RenderTarget rt) {
            rt.Use();
            GL.UseProgram(Program);
            UpdateCamera("vp");

            GL.Enable(EnableCap.DepthTest);
            GL.BindVertexArray(grid.EdgeData.GLAttrib);
            GL.LineWidth(1);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.DrawArrays(BeginMode.Lines, 0, grid.EdgeData.Count);
        }
    }

    public abstract class TransformerRenderer : RendererWithCamera {
        string vertex_shader_source = @"
        #version 330 core

        uniform mat4 vp;
        uniform mat4 mMat;

        layout(location = 0) in vec4 position;

        out vec4 pos;

        void main()
        {
            pos = vec4(position.xyz, 1);
            gl_Position = vp * mMat * pos;
        }";

        string fragment_shader_source = @"
        #version 330 core

        in vec4 pos;
        out vec4 outColor;
        uniform vec4 color;
        uniform ivec2 mode;

        void main()
        {
            if(mode.x == 1)
                outColor = color;
            else {
                uint idx = uint(dot(vec4(1, 2, 3, 0), color));
                float val = uintBitsToFloat(uint(mode.y << 28) | idx);
                outColor = vec4(val, pos.x, pos.y, pos.z);
            }
        }";

        protected MeshGraph axis;
        protected Vector4[] axisColors;

        public MeshGraph Model;
        public Vector3 Position {
            get { return Model.SelectedVerticesMidpoint; }
        }

        int type = 0;
        public bool ScreenMode = false;

        protected TransformerRenderer(ObjectType t) {
            SetSource(vertex_shader_source, "", fragment_shader_source);

            type = (int)t;
            axis = new MeshGraph();

            axisColors = new Vector4[3];
            axisColors[0] = new Vector4(1, 0, 0, 1);
            axisColors[1] = new Vector4(0, 1, 0, 1);
            axisColors[2] = new Vector4(0, 0, 1, 1);
        }

        public override void Render(RenderTarget rt) {
            rt.Use();
            GL.UseProgram(Program);
            UpdateCamera("vp");

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.CullFace);

            for(int i = 0; i < 3; i++) {
                Matrix4 m = Tf(i);
                GL.UniformMatrix4(Uniform("mMat"), false, ref m);
                GL.Uniform4(Uniform("color"), ref axisColors[i]);
                GL.Uniform2(Uniform("mode"), ScreenMode ? 1 : 0, type);

                if(axis.Edges.Count > 0) {
                    GL.BindVertexArray(axis.EdgeData.GLAttrib);
                    GL.LineWidth(1);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    GL.DrawArrays(BeginMode.Lines, 0, axis.EdgeData.Count);
                }

                if(axis.FacetData.Count > 0) {
                    GL.BindVertexArray(axis.FacetData.GLAttrib);
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                    GL.DrawArrays(BeginMode.Triangles, 0, axis.FacetData.Count);
                }
            }
        }

        protected abstract Matrix4 Tf(int i);
    }

    public class TranslationTransformerRenderer : TransformerRenderer {
        protected Matrix4[] axisTfs;

        public TranslationTransformerRenderer() :
                base(ObjectType.TranslationTransformer) {
            new ArrowFactory().AddMeshGraphUpon(ref axis, false);
            axis.EdgeData.UpdateData();
            axis.FacetData.UpdateData();

            axisTfs = new Matrix4[3];
            axisTfs[0] = Matrix4.CreateRotationY((float)Math.PI / 2);
            axisTfs[1] = Matrix4.CreateRotationX(-(float)Math.PI / 2);
            axisTfs[2] = Matrix4.Identity;
        }

        protected override Matrix4 Tf(int i) {
            return axisTfs[i] * Matrix4.CreateTranslation(Position);
        }

        public Vector2 ScreenVector(int axis) {
            Vector3 pos = Position;
            Vector4 v_org = Vector4.Transform(
                new Vector4(pos, 1), Camera.VPMatrix);
            v_org /= v_org.W;
            v_org.Y = -v_org.Y;

            pos[axis] = pos[axis] + 1;
            Vector4 v_end = Vector4.Transform(
                new Vector4(pos, 1), Camera.VPMatrix);
            v_end /= v_end.W;
            v_end.Y = -v_end.Y;

            return (v_end - v_org).Normalized().Xy;
        }
    }

    public class ScalingTransformerRenderer : TransformerRenderer {
        public Vector3 Scaling;
        protected Matrix4[] axisTfs;

        public ScalingTransformerRenderer() :
                base(ObjectType.ScalingTransformer) {
            new BoxMeshFactory(0.1f, 0.1f, 0.1f)
                .AddMeshGraphUpon(ref axis, false);
            foreach(MeshVertex v in axis.Vertices)
                v.Position = v.Position + new Vector3(0, 0, 3);

            MeshVertex vorg = axis.AddVertex(0, 0, 0);
            MeshVertex vhead = axis.AddVertex(0, 0, 3);
            axis.AddEdge(vorg, vhead);
            axis.EdgeData.UpdateData();
            axis.FacetData.UpdateData();

            axisTfs = new Matrix4[3];
            axisTfs[0] = Matrix4.CreateRotationY((float)Math.PI / 2);
            axisTfs[1] = Matrix4.CreateRotationX(-(float)Math.PI / 2);
            axisTfs[2] = Matrix4.Identity;

            Scaling = new Vector3(1, 1, 1);
        }

        protected override Matrix4 Tf(int i) {
            Vector3 s = new Vector3(1, 1, 1);
            s[i] = Scaling[i];
            return Matrix4.CreateScale(s) * axisTfs[i] *
                Matrix4.CreateTranslation(Position);
        }

        public Vector2 ScreenVector(int axis) {
            Vector3 pos = Position;
            Vector4 v_org = Vector4.Transform(
                new Vector4(pos, 1), Camera.VPMatrix);
            v_org /= v_org.W;
            v_org.Y = -v_org.Y;

            pos[axis] = pos[axis] + 1;
            Vector4 v_end = Vector4.Transform(
                new Vector4(pos, 1), Camera.VPMatrix);
            v_end /= v_end.W;
            v_end.Y = -v_end.Y;

            return (v_end - v_org).Normalized().Xy;
        }
    }

    public class RotationTransformerRenderer : TransformerRenderer {
        protected Matrix4[] axisTfs;

        public RotationTransformerRenderer() :
                base(ObjectType.RotationTransformer) {
            MeshVertex[] vs = new MeshVertex[32];
            for(int i = 0; i < 32; i++) {
                double a = 2.0 * Math.PI * i / 32.0;
                vs[i] = axis.AddVertex(1.5f * (float)Math.Cos(a), 0, 1.5f * (float)Math.Sin(a));
                if(i > 0) axis.AddEdge(vs[i-1], vs[i]);
            }
            axis.AddEdge(vs[31], vs[0]);
            axis.EdgeData.UpdateData();

            axisTfs = new Matrix4[3];
            axisTfs[0] = Matrix4.CreateRotationZ((float)Math.PI / 2); // x
            axisTfs[1] = Matrix4.Identity; // y
            axisTfs[2] = Matrix4.CreateRotationX((float)Math.PI / 2); // z
        }

        protected override Matrix4 Tf(int i) {
            return axisTfs[i] * Matrix4.CreateTranslation(Position);
        }

        public Vector2 ScreenVector(int axis, Vector3 startpt) {
            Vector3 pos = Position;
            Vector3 diff = startpt - pos;
            double a = diff[(axis + 1) % 3], b = diff[(axis + 2) % 3];
            double r = Math.Sqrt(a * a + b * b);
            a /= r; b /= r;
            double angle = Math.Atan2(b, a) + Math.PI / 4;
            double a_ = Math.Cos(angle), b_ = Math.Sin(angle);

            a *= 1.5; b *= 1.5;
            a_ *= 1.414213562 * 1.5; b_ *= 1.414213562 * 1.5;

            diff[axis] = 0;
            diff[(axis + 1) % 3] = (float)a;
            diff[(axis + 2) % 3] = (float)b;

            Console.WriteLine(diff);

            Vector4 v_org = Vector4.Transform(
                new Vector4(diff + pos, 1), Camera.VPMatrix);
            v_org /= v_org.W;
            v_org.Y = -v_org.Y;

            diff[axis] = 0;
            diff[(axis + 1) % 3] = (float)a_;
            diff[(axis + 2) % 3] = (float)b_;

            Console.WriteLine(diff);

            pos[axis] = pos[axis] + 1;
            Vector4 v_end = Vector4.Transform(
                new Vector4(diff + pos, 1), Camera.VPMatrix);
            v_end /= v_end.W;
            v_end.Y = -v_end.Y;

            return (v_end - v_org).Normalized().Xy;
        }
    }

    public class SelectionBoxRenderer : RendererBase {
        MeshGraph plane;
        MeshGraph line;

        public Vector2 StartPoint;
        public Vector2 EndPoint;

        string vertex_shader_source = @"
        #version 330 core

        uniform vec4 rect;

        layout(location = 0) in vec4 position;

        void main()
        {
            gl_Position = vec4(rect.zw * position.xy + rect.xy, 0, 1);
        }";

        string fragment_shader_source = @"
        #version 330 core

        out vec4 outColor;

        void main()
        {
            outColor = vec4(1, 1, 1, 0.2);
        }";

        public bool RenderPlane = true;

        public SelectionBoxRenderer() {
            SetSource(vertex_shader_source, "", fragment_shader_source);

            plane = new MeshGraph();
            var v1 = plane.AddVertex(0, 0, 0);
            var v2 = plane.AddVertex(0, 1, 0);
            var v3 = plane.AddVertex(1, 1, 0);
            var v4 = plane.AddVertex(1, 0, 0);

            plane.AddFacet(v1, v2, v3, v4);
            plane.FacetData.UpdateData();

            line = new MeshGraph();
            var l1 = line.AddVertex(0, 0, 0);
            var l2 = line.AddVertex(1, 1, 0);
            line.AddEdge(l1, l2);
            line.EdgeData.UpdateData();
        }

        public override void Render(RenderTarget rt) {
            rt.Use();
            GL.UseProgram(Program);

            Vector2 sp = StartPoint * 2 - new Vector2(1, 1);
            sp.Y = -sp.Y;
            Vector2 ep = EndPoint * 2 - new Vector2(1, 1);
            ep.Y = -ep.Y;
            Vector2 sz = ep - sp;

            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha,
                BlendingFactorDest.OneMinusSrcAlpha);

            if(RenderPlane) {
                GL.BindVertexArray(plane.FacetData.GLAttrib);
                GL.Uniform4(Uniform("rect"), sp.X, sp.Y, sz.X, sz.Y);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
                GL.DrawArrays(BeginMode.Triangles, 0, plane.FacetData.Count);
            } else {
                GL.BindVertexArray(line.EdgeData.GLAttrib);
                GL.Uniform4(Uniform("rect"), sp.X, sp.Y, sz.X, sz.Y);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.DrawArrays(BeginMode.Lines, 0, line.EdgeData.Count);
            }

            GL.Disable(EnableCap.Blend);
        }
    }
}

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CubeSharp
{
    public class Transformation {

        public void Translate(float x, float y, float z)
        {
            matrix = Matrix4.CreateTranslation(x, y, z) * matrix;
            inverse_matrix *= Matrix4.CreateTranslation(-x, -y, -z);
        }

        public void Translate(Vector3 tl)
        {
            matrix = Matrix4.CreateTranslation(tl) * matrix;
            inverse_matrix *= Matrix4.CreateTranslation(-tl);
        }

        public void RotateX(float angle)
        {
            matrix = Matrix4.CreateRotationX(angle) * matrix;
            inverse_matrix *= Matrix4.CreateRotationX(-angle);
        }

        public void RotateY(float angle)
        {
            matrix = Matrix4.CreateRotationY(angle) * matrix;
            inverse_matrix *= Matrix4.CreateRotationY(-angle);
        }

        public void RotateZ(float angle)
        {
            matrix = Matrix4.CreateRotationZ(angle) * matrix;
            inverse_matrix *= Matrix4.CreateRotationZ(-angle);
        }

        public void RotateAxis(Vector3 axis, float angle)
        {
            matrix = Matrix4.CreateFromAxisAngle(axis, angle) * matrix;
            inverse_matrix *= Matrix4.CreateFromAxisAngle(axis, -angle);
        }

        private Matrix4 matrix;
        public Matrix4 Matrix {
            get {
                return matrix;
            }
        }

        private Matrix4 inverse_matrix;
        public Matrix4 InverseMatrix {
            get {
                return inverse_matrix;
            }
        }

        public Transformation() {
            matrix = Matrix4.Identity;
            inverse_matrix = Matrix4.Identity;
        }

        public Transformation(Transformation other) {
            matrix = other.matrix * Matrix4.Identity;
            inverse_matrix = other.inverse_matrix * Matrix4.Identity;
        }
    }

    public class RenderTarget {
        private int framebuffer = -1;
        private int texture_color = -1;
        private int texture_depth = -1;

        private void init() {
            framebuffer = GL.GenFramebuffer();
            texture_color = GL.GenTexture();
            texture_depth = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, texture_color);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                    PixelInternalFormat.Rgba8, 800, 600, 0,
                    PixelFormat.Rgba, PixelType.Byte, (IntPtr)0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            GL.BindTexture(TextureTarget.Texture2D, texture_depth);
            GL.TexImage2D(TextureTarget.Texture2D, 0,
                    PixelInternalFormat.DepthComponent32f, 800, 600, 0,
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
                    screen = new RenderTarget();
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

    public class Renderer : RendererBase {
        string vertex_shader_source = @"
        #version 330 core

        uniform mat4 vp;

        layout(location = 0) in vec4 positions;
        layout(location = 1) in vec4 ctlInfo;

        out vec3 edgeMark;
        out vec3 edgeInfo;
        out float pointInfo;
        out float facetInfo;
        out vec3 modelPos;

        void main()
        {
            edgeMark = clamp(ctlInfo.xyz, 0, 1);
            edgeInfo = ctlInfo.xyz;
            pointInfo = positions.w;
            facetInfo = ctlInfo.w;

            modelPos = positions.xyz;
            vec4 p = vec4(positions.xyz, 1);
            gl_Position = vp * p;
        }";

        string fragment_shader_source = @"
        #version 330 core

        in vec3 edgeMark;
        in vec3 edgeInfo;
        in float pointInfo;
        in float facetInfo;
        in vec3 modelPos;

        #define SCR_POINT_MODE 0
        #define SCR_EDGE_MODE 1
        #define SCR_FACET_MODE 2
        #define CTL_POINT_MODE 4
        #define CTL_EDGE_MODE 5
        #define CTL_FACET_MODE 6

        uniform ivec4 mode;

        out vec4 outColor;

        void main()
        {
            bool is_12 = edgeMark.x > 0.999;
            bool is_23 = edgeMark.y > 0.999;
            bool is_31 = edgeMark.z > 0.999;

            if(!is_12 && !is_23 && !is_31) discard;

            float c = 0;
            if(mode.x == SCR_POINT_MODE) {
                c = 1;
            } else if(mode.x == SCR_EDGE_MODE) {
                c = 0.6;
            } else if(mode.x == CTL_POINT_MODE) {
                c = pointInfo;
            } else if(mode.x == CTL_EDGE_MODE) {
                if(is_12) c = edgeInfo.x - 1;
                if(is_23) c = edgeInfo.y - 1;
                if(is_31) c = edgeInfo.z - 1;
            }

            outColor = vec4(c, c, c, 1);
        }";

        public MeshFacetData Model;
        private Transformation camera_tranformation;

        public Transformation CameraTranformation {
            get {
                return camera_tranformation;
            }
            set {
                GL.UseProgram(Program);
                camera_tranformation = value;

                GL.UseProgram(Program);

                Matrix4 m = camera_tranformation.InverseMatrix;
                m *= Matrix4.CreatePerspectiveFieldOfView(
                        (float)Math.PI / 2, 1.333f, 1, 10);
                GL.UniformMatrix4(Uniform("vp"), false, ref m);
            }
        }

        public Renderer(MeshFacetData m)
        {
            SetSource(vertex_shader_source, "", fragment_shader_source);

            Model = m;
            Model.UpdateData();

            Transformation tf = new Transformation();
            tf.Translate(0, 0, 5);
            CameraTranformation = tf;
        }

        public const int ScreenMode = 0;
        public const int ControlMode = 1;
        public int Mode;

        public override void Render(RenderTarget rt) {
            rt.Use();
            GL.UseProgram(Program);

            if(Mode == ScreenMode) {
                GL.BindVertexArray(Model.GLAttrib);

                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.DepthTest);
                GL.Disable(EnableCap.DepthTest);
                GL.Uniform4(Uniform("mode"), 1, 0, 0, 0);
                GL.LineWidth(1);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.DrawArrays(BeginMode.Triangles, 0, Model.Count);

                GL.Uniform4(Uniform("mode"), 0, 0, 0, 0);
                GL.PointSize(3);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                GL.DrawArrays(BeginMode.Triangles, 0, Model.Count);
            } else if(Mode == ControlMode) {
                GL.BindVertexArray(Model.GLAttrib);

                GL.Disable(EnableCap.CullFace);
                GL.Enable(EnableCap.DepthTest);
                GL.Uniform4(Uniform("mode"), 5, 0, 0, 0);
                GL.LineWidth(4);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.DrawArrays(BeginMode.Triangles, 0, Model.Count);

                GL.Disable(EnableCap.DepthTest);
                GL.Uniform4(Uniform("mode"), 4, 0, 0, 0);
                GL.PointSize(5);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
                GL.DrawArrays(BeginMode.Triangles, 0, Model.Count);
            }
        }
    }

}

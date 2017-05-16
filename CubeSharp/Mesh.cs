using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CubeSharp
{
    public class MeshProperty<T> : IEnumerable<T> {
        private List<T> stor;
        private List<int> idx;

        internal MeshProperty(List<T> _stor) {
            stor = _stor;
            idx = new List<int>();
        }

        public T this[int i] {
            get { return stor[idx[i]]; }
            set { stor[idx[i]] = value; }
        }

        public List<int> Indices {
            get { return idx; }
        }

        public int Count {
            get { return idx.Count; }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator() {
            for(int i = 0; i < Count; i++)
                yield return this[i];
        }
    };

    public class Mesh
    {
        private List<Vector3> stor_positions;
        private List<Vector3> stor_normals;
        private List<Vector2> stor_uvs;

        public List<Vector3> PositionsStorage {
            get { return stor_positions; }
        }

        public List<Vector3> NormalsStorage {
            get { return stor_normals; }
        }

        public List<Vector2> UvsStorage {
            get { return stor_uvs; }
        }

        public MeshProperty<Vector3> Positions;
        public MeshProperty<Vector3> Normals;
        public MeshProperty<Vector2> Uvs;

        public Mesh() {
            stor_positions = new List<Vector3>();
            stor_normals = new List<Vector3>();
            stor_uvs = new List<Vector2>();

            Positions = new MeshProperty<Vector3>(stor_positions);
            Normals = new MeshProperty<Vector3>(stor_normals);
            Uvs = new MeshProperty<Vector2>(stor_uvs);
        }

        private int gl_buffer = 0;
        private int gl_buffer_size = 0;
        private int gl_attrib = 0;

        public bool BufferEnableNormals = false;
        public bool BufferEnableUvs = false;

        public int Count {
            get {
                return Positions.Indices.Count;
            }
        }

        public int GLBuffer {
            get {
                if(gl_buffer == 0)
                    gl_buffer = GL.GenBuffer();

                return gl_buffer;
            }
        }

        public int GLAttrib {
            get {
                if(gl_attrib == 0)
                    gl_attrib = GL.GenVertexArray();

                return gl_attrib;
            }
        }

        private void ReserveSpace(int size) {
            Debug.Assert(size > 0, "Attempt to allocate invalid size of space");

            if(gl_buffer_size < size) {
                int log = (int) Math.Floor(Math.Log(size, 2)) + 1;
                int new_size = (int) Math.Pow(2, log);

                unsafe {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);
                    GL.BufferData(BufferTarget.ArrayBuffer, new IntPtr(new_size),
                            new IntPtr(null), BufferUsageHint.StaticDraw);
                }
            }
        }

        internal void UpdateAttrib() {
            GL.BindVertexArray(GLAttrib);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);

            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, 20, 0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 20, 12);
        }

        public void Update() {
            ReserveSpace(Positions.Count * 5 * sizeof(float));

            unsafe {
                GL.BindBuffer(BufferTarget.ArrayBuffer, GLBuffer);
                float* buf = (float*) GL.MapBuffer(
                    BufferTarget.ArrayBuffer, BufferAccess.WriteOnly);

                int idx = 0;
                foreach(Vector3 v in Positions) {
                    buf[0] = v[0];
                    buf[1] = v[1];
                    buf[2] = v[2];
                    buf[3] = Positions.Indices[idx];
                    buf[4] = idx;

                    Console.WriteLine(Positions.Indices[idx]);

                    buf += 5;
                    idx += 1;
                }

                GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            }
        }
    }
}

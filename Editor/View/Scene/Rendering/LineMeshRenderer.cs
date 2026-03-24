using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class LineMeshManager
    {
        private sealed class LineMesh
        {
            public const int MAX_VERTEX_COUNT = 65000 - 4;

            public int VertexCount
            {
                get => vertexCount;
            }

            public readonly Vector3[] vertices1 = new Vector3[MAX_VERTEX_COUNT];
            public readonly Vector3[] vertices2 = new Vector3[MAX_VERTEX_COUNT];
            public readonly Vector4[] offsets = new Vector4[MAX_VERTEX_COUNT];
            public readonly Color[] colors = new Color[MAX_VERTEX_COUNT];
            public int vertexCount;
            private int[] _indices;

            private Mesh _mesh;

            public LineMesh()
            {
                Clear();
            }

            public void Clear()
            {
                vertexCount = 0;
            }

            public void AddLine(Vector3 A, Vector3 B, float thickness, float dashSize, Color color)
            {
                if(float.IsInfinity(A.x) || float.IsInfinity(A.y) || float.IsInfinity(A.z) ||
                   float.IsInfinity(B.x) || float.IsInfinity(B.y) || float.IsInfinity(B.z) ||
                   float.IsNaN(A.x) || float.IsNaN(A.y) || float.IsNaN(A.z) ||
                   float.IsNaN(B.x) || float.IsNaN(B.y) || float.IsNaN(B.z))
                {
                    return;
                }

                int n = vertexCount;
                vertices1[n] = B;
                vertices2[n] = A;
                offsets[n]   = new Vector4(thickness, -1, dashSize);
                colors[n]    = color;
                n++;
                vertices1[n] = B;
                vertices2[n] = A;
                offsets[n]   = new Vector4(thickness, +1, dashSize);
                colors[n]    = color;
                n++;
                vertices1[n] = A;
                vertices2[n] = B;
                offsets[n]   = new Vector4(thickness, -1, dashSize);
                colors[n]    = color;
                n++;
                vertices1[n] = A;
                vertices2[n] = B;
                offsets[n]   = new Vector4(thickness, +1, dashSize);
                colors[n]    = color;
                n++;
                vertexCount = n;
            }

            private readonly List<Vector3> _newVertices1 = new List<Vector3>(MAX_VERTEX_COUNT);
            private readonly List<Vector3> _newVertices2 = new List<Vector3>(MAX_VERTEX_COUNT);
            private readonly List<Vector4> _newOffsets = new List<Vector4>(MAX_VERTEX_COUNT);
            private readonly List<Color> _newColors = new List<Color>(MAX_VERTEX_COUNT);

            public void CommitMesh()
            {
                if(vertexCount == 0)
                {
                    if(_mesh != null && _mesh.vertexCount != 0)
                    {
                        _mesh.Clear(true);
                    }

                    return;
                }

                if(_mesh)
                {
                    _mesh.Clear(true);
                }
                else
                {
                    _mesh           = new Mesh();
                    _mesh.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
                    _mesh.MarkDynamic();
                }

                int reqSize = vertexCount * 6 / 4;

                if(_indices == null || _indices.Length != reqSize)
                {
                    _indices = new int[reqSize];
                }

                for(int i = 0, j = 0; i < vertexCount; i += 4, j += 6)
                {
                    _indices[j + 0] = i + 0;
                    _indices[j + 1] = i + 1;
                    _indices[j + 2] = i + 2;
                    _indices[j + 3] = i + 0;
                    _indices[j + 4] = i + 2;
                    _indices[j + 5] = i + 3;
                }

                // thanks unity API
                _newVertices1.Clear();
                _newVertices2.Clear();
                _newOffsets.Clear();
                _newColors.Clear();

                if(vertexCount == MAX_VERTEX_COUNT)
                {
                    _newVertices1.AddRange(vertices1);
                    _newVertices2.AddRange(vertices2);
                    _newOffsets.AddRange(offsets);
                    _newColors.AddRange(colors);
                }
                else
                {
                    _newVertices1.AddRange(vertices1.Take(vertexCount));
                    _newVertices2.AddRange(vertices2.Take(vertexCount));
                    _newOffsets.AddRange(offsets.Take(vertexCount));
                    _newColors.AddRange(colors.Take(vertexCount));
                }

                _mesh.SetVertices(_newVertices1);
                _mesh.SetUVs(0, _newVertices2);
                _mesh.SetUVs(1, _newOffsets);
                _mesh.SetColors(_newColors);
                _mesh.SetIndices(_indices, MeshTopology.Triangles, 0, false);
                _mesh.RecalculateBounds();
                _mesh.UploadMeshData(false);
            }

            public void Draw()
            {
                if(vertexCount == 0 || _mesh == null)
                {
                    return;
                }

                Graphics.DrawMeshNow(_mesh, MathConstants.identityMatrix);
            }

            internal void Destroy()
            {
                if(_mesh)
                {
                    Object.DestroyImmediate(_mesh);
                }

                _mesh    = null;
                _indices = null;
            }
        }

        public void Begin()
        {
            if(_lineMeshes == null || _lineMeshes.Count == 0)
            {
                return;
            }

            _currentLineMesh = 0;

            for(int i = 0; i < _lineMeshes.Count; i++)
            {
                _lineMeshes[i].Clear();
            }
        }

        public void End()
        {
            if(_lineMeshes == null || _lineMeshes.Count == 0)
            {
                return;
            }

            int max = Mathf.Min(_currentLineMesh, _lineMeshes.Count);

            for(int i = 0; i <= max; i++)
            {
                _lineMeshes[i].CommitMesh();
            }
        }

        public void Render(Material genericLineMaterial)
        {
            if(_lineMeshes == null || _lineMeshes.Count == 0 || !genericLineMaterial)
            {
                return;
            }

            MaterialUtility.InitGenericLineMaterial(genericLineMaterial);

            if(genericLineMaterial.SetPass(0))
            {
                int max = Mathf.Min(_currentLineMesh, _lineMeshes.Count - 1);

                for(int i = 0; i <= max; i++)
                {
                    _lineMeshes[i].Draw();
                }
            }
        }

        private readonly List<LineMesh> _lineMeshes = new List<LineMesh>();
        private int _currentLineMesh;

        public LineMeshManager()
        {
            _lineMeshes.Add(new LineMesh());
        }

        public void DrawLine(Vector3 a, Vector3 b, Color color, float thickness = 1.0f, float dashSize = 0.0f)
        {
            LineMesh lineMesh = _lineMeshes[_currentLineMesh];

            if(lineMesh.VertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
            {
                _currentLineMesh++;

                if(_currentLineMesh >= _lineMeshes.Count)
                {
                    _lineMeshes.Add(new LineMesh());
                }

                lineMesh = _lineMeshes[_currentLineMesh];
            }

            lineMesh.AddLine(a, b, thickness, dashSize, color);
        }

        public void DrawLines(
            Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color color, float thickness = 1.0f
          , float dashSize = 0.0f
        )
        {
            Vector4 corner1 = new Vector4(thickness, -1, dashSize);
            Vector4 corner2 = new Vector4(thickness, +1, dashSize);
            Vector4 corner3 = new Vector4(thickness, +1, dashSize);
            Vector4 corner4 = new Vector4(thickness, -1, dashSize);

            int lineMeshIndex = _currentLineMesh;

            while(lineMeshIndex >= _lineMeshes.Count)
            {
                _lineMeshes.Add(new LineMesh());
            }

            if(_lineMeshes[lineMeshIndex].VertexCount + indices.Length * 2 <= LineMesh.MAX_VERTEX_COUNT)
            {
                LineMesh  lineMesh  = _lineMeshes[lineMeshIndex];
                Vector3[] vertices1 = lineMesh.vertices1;
                Vector3[] vertices2 = lineMesh.vertices2;
                Vector4[] offsets   = lineMesh.offsets;
                Color[]   colors    = lineMesh.colors;

                int n = lineMesh.vertexCount;

                for(int i = 0; i < indices.Length; i += 2)
                {
                    Vector3 a = matrix.MultiplyPoint(vertices[indices[i + 0]]);
                    Vector3 b = matrix.MultiplyPoint(vertices[indices[i + 1]]);
                    vertices1[n] = b;
                    vertices2[n] = a;
                    offsets[n]   = corner1;
                    colors[n]    = color;
                    n++;
                    vertices1[n] = b;
                    vertices2[n] = a;
                    offsets[n]   = corner2;
                    colors[n]    = color;
                    n++;
                    vertices1[n] = a;
                    vertices2[n] = b;
                    offsets[n]   = corner3;
                    colors[n]    = color;
                    n++;
                    vertices1[n] = a;
                    vertices2[n] = b;
                    offsets[n]   = corner4;
                    colors[n]    = color;
                    n++;
                }

                lineMesh.vertexCount = n;
            }
            else
            {
                for(int i = 0; i < indices.Length; i += 2)
                {
                    LineMesh lineMesh    = _lineMeshes[lineMeshIndex];
                    int      vertexCount = lineMesh.vertexCount;

                    if(lineMesh.VertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
                    {
                        lineMeshIndex++;

                        if(lineMeshIndex >= _lineMeshes.Count)
                        {
                            _lineMeshes.Add(new LineMesh());
                        }

                        lineMesh    = _lineMeshes[lineMeshIndex];
                        vertexCount = lineMesh.vertexCount;
                    }

                    Vector3[] vertices1 = lineMesh.vertices1;
                    Vector3[] vertices2 = lineMesh.vertices2;
                    Vector4[] offsets   = lineMesh.offsets;
                    Color[]   colors    = lineMesh.colors;

                    Vector3 a = matrix.MultiplyPoint(vertices[indices[i + 0]]);
                    Vector3 b = matrix.MultiplyPoint(vertices[indices[i + 1]]);
                    vertices1[vertexCount] = b;
                    vertices2[vertexCount] = a;
                    offsets[vertexCount]   = corner1;
                    colors[vertexCount]    = color;
                    vertexCount++;
                    vertices1[vertexCount] = b;
                    vertices2[vertexCount] = a;
                    offsets[vertexCount]   = corner2;
                    colors[vertexCount]    = color;
                    vertexCount++;
                    vertices1[vertexCount] = a;
                    vertices2[vertexCount] = b;
                    offsets[vertexCount]   = corner3;
                    colors[vertexCount]    = color;
                    vertexCount++;
                    vertices1[vertexCount] = a;
                    vertices2[vertexCount] = b;
                    offsets[vertexCount]   = corner4;
                    colors[vertexCount]    = color;
                    vertexCount++;

                    lineMesh.vertexCount += 4;
                }

                _currentLineMesh = lineMeshIndex;
            }
        }

        public void DrawLines(Vector3[] vertices, int[] indices, Color color, float thickness = 1.0f, float dashSize = 0.0f) //2
        {
            Vector4 corner1 = new Vector4(thickness, -1, dashSize);
            Vector4 corner2 = new Vector4(thickness, +1, dashSize);
            Vector4 corner3 = new Vector4(thickness, +1, dashSize);
            Vector4 corner4 = new Vector4(thickness, -1, dashSize);

            int lineMeshIndex = _currentLineMesh;

            while(lineMeshIndex >= _lineMeshes.Count)
            {
                _lineMeshes.Add(new LineMesh());
            }

            if(_lineMeshes[lineMeshIndex].vertexCount + indices.Length * 2 <= LineMesh.MAX_VERTEX_COUNT)
            {
                LineMesh  lineMesh  = _lineMeshes[lineMeshIndex];
                Vector3[] vertices1 = lineMesh.vertices1;
                Vector3[] vertices2 = lineMesh.vertices2;
                Vector4[] offsets   = lineMesh.offsets;
                Color[]   colors    = lineMesh.colors;

                int n = lineMesh.vertexCount;

                for(int i = 0; i < indices.Length; i += 2)
                {
                    int index0 = indices[i + 0];
                    int index1 = indices[i + 1];

                    if(index0 < 0 || index0 >= vertices.Length || index1 < 0 || index1 >= vertices.Length)
                    {
                        continue;
                    }

                    Vector3 a = vertices[index0];
                    Vector3 b = vertices[index1];

                    if(float.IsInfinity(a.x) || float.IsInfinity(a.y) || float.IsInfinity(a.z) ||
                       float.IsInfinity(b.x) || float.IsInfinity(b.y) || float.IsInfinity(b.z) ||
                       float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(a.z) ||
                       float.IsNaN(b.x) || float.IsNaN(b.y) || float.IsNaN(b.z))
                    {
                        continue;
                    }

                    vertices1[n] = b;
                    vertices2[n] = a;
                    offsets[n]   = corner1;
                    colors[n]    = color;
                    n++;
                    vertices1[n] = b;
                    vertices2[n] = a;
                    offsets[n]   = corner2;
                    colors[n]    = color;
                    n++;
                    vertices1[n] = a;
                    vertices2[n] = b;
                    offsets[n]   = corner3;
                    colors[n]    = color;
                    n++;
                    vertices1[n] = a;
                    vertices2[n] = b;
                    offsets[n]   = corner4;
                    colors[n]    = color;
                    n++;
                }

                lineMesh.vertexCount = n;
            }
            else
            {
                for(int i = 0; i < indices.Length; i += 2)
                {
                    int index0 = indices[i + 0];
                    int index1 = indices[i + 1];

                    if(index0 < 0 || index0 >= vertices.Length || index1 < 0 || index1 >= vertices.Length)
                    {
                        continue;
                    }

                    Vector3 a = vertices[index0];
                    Vector3 b = vertices[index1];

                    if(float.IsInfinity(a.x) || float.IsInfinity(a.y) || float.IsInfinity(a.z) ||
                       float.IsInfinity(b.x) || float.IsInfinity(b.y) || float.IsInfinity(b.z) ||
                       float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(a.z) ||
                       float.IsNaN(b.x) || float.IsNaN(b.y) || float.IsNaN(b.z))
                    {
                        continue;
                    }

                    LineMesh lineMesh    = _lineMeshes[lineMeshIndex];
                    int      vertexCount = lineMesh.vertexCount;

                    if(vertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
                    {
                        lineMeshIndex++;

                        if(lineMeshIndex >= _lineMeshes.Count)
                        {
                            _lineMeshes.Add(new LineMesh());
                        }

                        lineMesh = _lineMeshes[lineMeshIndex];
                        lineMesh.Clear();
                        vertexCount = lineMesh.vertexCount;
                    }

                    Vector3[] vertices1 = lineMesh.vertices1;
                    Vector3[] vertices2 = lineMesh.vertices2;
                    Vector4[] offsets   = lineMesh.offsets;
                    Color[]   colors    = lineMesh.colors;

                    vertices1[vertexCount] = b;
                    vertices2[vertexCount] = a;
                    offsets[vertexCount]   = corner1;
                    colors[vertexCount]    = color;
                    vertexCount++;
                    vertices1[vertexCount] = b;
                    vertices2[vertexCount] = a;
                    offsets[vertexCount]   = corner2;
                    colors[vertexCount]    = color;
                    vertexCount++;
                    vertices1[vertexCount] = a;
                    vertices2[vertexCount] = b;
                    offsets[vertexCount]   = corner3;
                    colors[vertexCount]    = color;
                    vertexCount++;
                    vertices1[vertexCount] = a;
                    vertices2[vertexCount] = b;
                    offsets[vertexCount]   = corner4;
                    colors[vertexCount]    = color;
                    vertexCount++;
                    lineMesh.vertexCount = vertexCount;
                }

                _currentLineMesh = lineMeshIndex;
            }
        }

        public void DrawLines(Vector3[] vertices, int[] indices, Color[] colors, float thickness = 1.0f, float dashSize = 0.0f) //1
        {
            Vector4 corner1 = new Vector4(thickness, -1, dashSize);
            Vector4 corner2 = new Vector4(thickness, +1, dashSize);
            Vector4 corner3 = new Vector4(thickness, +1, dashSize);
            Vector4 corner4 = new Vector4(thickness, -1, dashSize);

            int lineMeshIndex = _currentLineMesh;

            while(lineMeshIndex >= _lineMeshes.Count)
            {
                _lineMeshes.Add(new LineMesh());
            }

            int prevVertexCount = _lineMeshes[lineMeshIndex].VertexCount;

            if(prevVertexCount + indices.Length * 2 <= LineMesh.MAX_VERTEX_COUNT)
            {
                LineMesh  lineMesh   = _lineMeshes[lineMeshIndex];
                Vector3[] vertices1  = lineMesh.vertices1;
                Vector3[] vertices2  = lineMesh.vertices2;
                Vector4[] offsets    = lineMesh.offsets;
                Color[]   meshColors = lineMesh.colors;

                int n = lineMesh.vertexCount;

                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int index0 = indices[i + 0];
                    int index1 = indices[i + 1];

                    if(index0 < 0 || index0 >= vertices.Length || index1 < 0 || index1 >= vertices.Length)
                    {
                        continue;
                    }

                    Vector3 a = vertices[index0];
                    Vector3 b = vertices[index1];

                    if(float.IsInfinity(a.x) || float.IsInfinity(a.y) || float.IsInfinity(a.z) ||
                       float.IsInfinity(b.x) || float.IsInfinity(b.y) || float.IsInfinity(b.z) ||
                       float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(a.z) ||
                       float.IsNaN(b.x) || float.IsNaN(b.y) || float.IsNaN(b.z))
                    {
                        continue;
                    }

                    Color color = colors[c];

                    vertices1[n]  = b;
                    vertices2[n]  = a;
                    offsets[n]    = corner1;
                    meshColors[n] = color;
                    n++;
                    vertices1[n]  = b;
                    vertices2[n]  = a;
                    offsets[n]    = corner2;
                    meshColors[n] = color;
                    n++;
                    vertices1[n]  = a;
                    vertices2[n]  = b;
                    offsets[n]    = corner3;
                    meshColors[n] = color;
                    n++;
                    vertices1[n]  = a;
                    vertices2[n]  = b;
                    offsets[n]    = corner4;
                    meshColors[n] = color;
                    n++;
                }

                lineMesh.vertexCount = n;
            }
            else
            {
                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int index0 = indices[i + 0];
                    int index1 = indices[i + 1];

                    if(index0 < 0 || index0 >= vertices.Length || index1 < 0 || index1 >= vertices.Length)
                    {
                        continue;
                    }

                    Vector3 a = vertices[index0];
                    Vector3 b = vertices[index1];

                    if(float.IsInfinity(a.x) || float.IsInfinity(a.y) || float.IsInfinity(a.z) ||
                       float.IsInfinity(b.x) || float.IsInfinity(b.y) || float.IsInfinity(b.z) ||
                       float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(a.z) ||
                       float.IsNaN(b.x) || float.IsNaN(b.y) || float.IsNaN(b.z))
                    {
                        continue;
                    }

                    Color color = colors[c];

                    LineMesh lineMesh    = _lineMeshes[lineMeshIndex];
                    int      vertexCount = lineMesh.vertexCount;

                    if(vertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
                    {
                        lineMeshIndex++;

                        if(lineMeshIndex >= _lineMeshes.Count)
                        {
                            _lineMeshes.Add(new LineMesh());
                        }

                        lineMesh = _lineMeshes[lineMeshIndex];
                        lineMesh.Clear();
                        vertexCount = lineMesh.vertexCount;
                    }

                    Vector3[] vertices1  = lineMesh.vertices1;
                    Vector3[] vertices2  = lineMesh.vertices2;
                    Vector4[] offsets    = lineMesh.offsets;
                    Color[]   meshColors = lineMesh.colors;

                    vertices1[vertexCount]  = b;
                    vertices2[vertexCount]  = a;
                    offsets[vertexCount]    = corner1;
                    meshColors[vertexCount] = color;
                    vertexCount++;
                    vertices1[vertexCount]  = b;
                    vertices2[vertexCount]  = a;
                    offsets[vertexCount]    = corner2;
                    meshColors[vertexCount] = color;
                    vertexCount++;
                    vertices1[vertexCount]  = a;
                    vertices2[vertexCount]  = b;
                    offsets[vertexCount]    = corner3;
                    meshColors[vertexCount] = color;
                    vertexCount++;
                    vertices1[vertexCount]  = a;
                    vertices2[vertexCount]  = b;
                    offsets[vertexCount]    = corner4;
                    meshColors[vertexCount] = color;
                    vertexCount++;
                    lineMesh.vertexCount = vertexCount;
                }
            }
        }

        public void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Color color, float thickness = 1.0f, float dashSize = 0.0f)
        {
            int      lineMeshIndex = _currentLineMesh;
            LineMesh lineMesh      = _lineMeshes[_currentLineMesh];

            for(int i = 0; i < vertices.Length; i += 2)
            {
                if(lineMesh.VertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
                {
                    _currentLineMesh++;

                    if(_currentLineMesh >= _lineMeshes.Count)
                    {
                        _lineMeshes.Add(new LineMesh());
                    }

                    lineMesh = _lineMeshes[_currentLineMesh];
                    lineMesh.Clear();
                }

                lineMesh.AddLine(matrix.MultiplyPoint(vertices[i + 0]), matrix.MultiplyPoint(vertices[i + 1]), thickness, dashSize, color);
            }

            _currentLineMesh = lineMeshIndex;
        }

        public void DrawLines(Vector3[] vertices, Color color, float thickness = 1.0f, float dashSize = 0.0f)
        {
            int      lineMeshIndex = _currentLineMesh;
            LineMesh lineMesh      = _lineMeshes[_currentLineMesh];

            for(int i = 0; i < vertices.Length; i += 2)
            {
                if(lineMesh.VertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
                {
                    _currentLineMesh++;

                    if(_currentLineMesh >= _lineMeshes.Count)
                    {
                        _lineMeshes.Add(new LineMesh());
                    }

                    lineMesh = _lineMeshes[_currentLineMesh];
                    lineMesh.Clear();
                }

                lineMesh.AddLine(vertices[i + 0], vertices[i + 1], thickness, dashSize, color);
            }

            _currentLineMesh = lineMeshIndex;
        }

        public void DrawLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color[] colors, float thickness = 1.0f, float dashSize = 0.0f)
        {
            int      lineMeshIndex = _currentLineMesh;
            LineMesh lineMesh      = _lineMeshes[_currentLineMesh];

            for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
            {
                if(lineMesh.VertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
                {
                    _currentLineMesh++;

                    if(_currentLineMesh >= _lineMeshes.Count)
                    {
                        _lineMeshes.Add(new LineMesh());
                    }

                    lineMesh = _lineMeshes[_currentLineMesh];
                    lineMesh.Clear();
                }

                int index0 = indices[i + 0];
                int index1 = indices[i + 1];

                if(index0 < 0 || index1 < 0 || index0 >= vertices.Length || index1 >= vertices.Length)
                {
                    continue;
                }

                lineMesh.AddLine(matrix.MultiplyPoint(vertices[index0]), matrix.MultiplyPoint(vertices[index1]), thickness, dashSize, colors[c]);
            }

            _currentLineMesh = lineMeshIndex;
        }

        public void DrawLines(Matrix4x4 matrix, Vector3[] vertices, Color[] colors, float thickness = 1.0f, float dashSize = 0.0f)
        {
            int      lineMeshIndex = _currentLineMesh;
            LineMesh lineMesh      = _lineMeshes[_currentLineMesh];

            for(int i = 0, c = 0; i < vertices.Length; i += 2, c++)
            {
                if(lineMesh.VertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
                {
                    _currentLineMesh++;

                    if(_currentLineMesh >= _lineMeshes.Count)
                    {
                        _lineMeshes.Add(new LineMesh());
                    }

                    lineMesh = _lineMeshes[_currentLineMesh];
                    lineMesh.Clear();
                }

                lineMesh.AddLine(matrix.MultiplyPoint(vertices[i + 0]), matrix.MultiplyPoint(vertices[i + 1]), thickness, dashSize, colors[c]);
            }

            _currentLineMesh = lineMeshIndex;
        }

        public void DrawLines(Vector3[] vertices, Color[] colors, float thickness = 1.0f, float dashSize = 0.0f)
        {
            int      lineMeshIndex = _currentLineMesh;
            LineMesh lineMesh      = _lineMeshes[_currentLineMesh];

            for(int i = 0, c = 0; i < vertices.Length; i += 2, c++)
            {
                if(lineMesh.VertexCount + 4 >= LineMesh.MAX_VERTEX_COUNT)
                {
                    _currentLineMesh++;

                    if(_currentLineMesh >= _lineMeshes.Count)
                    {
                        _lineMeshes.Add(new LineMesh());
                    }

                    lineMesh = _lineMeshes[_currentLineMesh];
                    lineMesh.Clear();
                }

                lineMesh.AddLine(vertices[i + 0], vertices[i + 1], thickness, dashSize, colors[c]);
            }

            _currentLineMesh = lineMeshIndex;
        }

        internal void Destroy()
        {
            for(int i = 0; i < _lineMeshes.Count; i++)
            {
                _lineMeshes[i].Destroy();
            }

            _lineMeshes.Clear();
            _currentLineMesh = 0;
        }

        internal void Clear()
        {
            _currentLineMesh = 0;

            for(int i = 0; i < _lineMeshes.Count; i++)
            {
                _lineMeshes[i].Clear();
            }
        }
    }
}

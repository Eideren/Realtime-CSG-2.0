using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class PolygonMeshManager
    {
        private sealed class TriangleMesh
        {
            public const int MAX_VERTEX_COUNT = 65000 - 3;
            public const int MAX_INDEX_COUNT = MAX_VERTEX_COUNT * 3;

            public int VertexCount
            {
                get => _vertexCount;
            }

            private Vector3[] _vertices = new Vector3[MAX_VERTEX_COUNT];
            private Color[] _colors = new Color [MAX_VERTEX_COUNT];
            private int[] _indices = new int [MAX_INDEX_COUNT];
            private int _vertexCount;
            private int _indexCount;

            private Mesh _mesh;

            public TriangleMesh()
            {
                Clear();
            }

            public void Clear()
            {
                _vertexCount = 0;
                _indexCount  = 0;
            }

            public void AddTriangles(Matrix4x4 matrix, Vector3[] triangleVertices, int[] triangleIndices, Color color)
            {
                if(triangleIndices.Length < 3 || triangleIndices.Length % 3 != 0)
                {
                    return;
                }

                int startIndex = _vertexCount;

                if(matrix.isIdentity)
                {
                    int v = _vertexCount;

                    for(int t = 0; t < triangleVertices.Length; t++, v++)
                    {
                        _vertices[v] = triangleVertices[t];
                        _colors[v]   = color;
                    }

                    _vertexCount = v;
                }
                else
                {
                    int v = _vertexCount;

                    for(int t = 0; t < triangleVertices.Length; t++, v++)
                    {
                        _vertices[v] = matrix.MultiplyPoint(triangleVertices[t]);
                        _colors[v]   = color;
                    }

                    _vertexCount = v;
                }

                int i = _indexCount;

                for(int t = 0; t < triangleIndices.Length; t++, i++)
                {
                    _indices[i] = triangleIndices[t] + startIndex;
                }

                _indexCount = i;
            }

            public void AddPolygon(Matrix4x4 matrix, Vector3[] polyVertices, int[] polyIndices, Color color)
            {
                if(polyIndices.Length < 3)
                {
                    return;
                }

                int startIndex = _vertexCount;

                if(matrix.isIdentity)
                {
                    for(int i = 0; i < polyIndices.Length; i++)
                    {
                        _vertices[_vertexCount] = polyVertices[polyIndices[i]];
                        _colors[_vertexCount]   = color;
                        _vertexCount++;
                    }
                }
                else
                {
                    for(int i = 0; i < polyIndices.Length; i++)
                    {
                        _vertices[_vertexCount] = matrix.MultiplyPoint(polyVertices[polyIndices[i]]);
                        _colors[_vertexCount]   = color;
                        _vertexCount++;
                    }
                }

                for(int i = 2; i < polyIndices.Length; i++)
                {
                    _indices[_indexCount + 0] =  startIndex + 0;
                    _indices[_indexCount + 1] =  startIndex + i - 1;
                    _indices[_indexCount + 2] =  startIndex + i;
                    _indexCount               += 3;
                }
            }

            public void AddPolygon(Vector3[] polyVertices, int[] polyIndices, Color color)
            {
                if(polyIndices.Length < 3)
                {
                    return;
                }

                int startIndex = _vertexCount;

                for(int i = 0; i < polyIndices.Length; i++)
                {
                    _vertices[_vertexCount] = polyVertices[polyIndices[i]];
                    _colors[_vertexCount]   = color;
                    _vertexCount++;
                }

                for(int i = 2; i < polyIndices.Length; i++)
                {
                    _indices[_indexCount + 0] =  startIndex + 0;
                    _indices[_indexCount + 1] =  startIndex + i - 1;
                    _indices[_indexCount + 2] =  startIndex + i;
                    _indexCount               += 3;
                }
            }

            public void CommitMesh()
            {
                if(_vertexCount == 0)
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

                Vector3[] newVertices;
                Color[]   newColors;

                if(_vertexCount == MAX_VERTEX_COUNT)
                {
                    newVertices = _vertices;
                    newColors   = _colors;
                }
                else
                {
                    newVertices = _vertices.Take(_vertexCount).ToArray();
                    newColors   = _colors.Take(_vertexCount).ToArray();
                }

                int[] newIndices = _indexCount == MAX_INDEX_COUNT ? _indices : _indices.Take(_indexCount).ToArray();

                _mesh.vertices = newVertices;
                _mesh.colors   = newColors;
                _mesh.SetIndices(newIndices, MeshTopology.Triangles, 0, false);
                _mesh.RecalculateBounds();
                _mesh.UploadMeshData(false);
            }

            public void Draw()
            {
                if(_vertexCount == 0 || _mesh == null)
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

                _mesh        = null;
                _vertices    = null;
                _colors      = null;
                _indices     = null;
                _vertexCount = 0;
                _indexCount  = 0;
            }
        }

        public void Begin()
        {
            if(_triangleMeshes == null || _triangleMeshes.Count == 0)
            {
                return;
            }

            _currentTriangleMesh = 0;

            for(int i = 0; i < _triangleMeshes.Count; i++)
            {
                _triangleMeshes[i].Clear();
            }
        }

        public void End()
        {
            if(_triangleMeshes == null || _triangleMeshes.Count == 0)
            {
                return;
            }

            int max = Mathf.Min(_currentTriangleMesh, _triangleMeshes.Count);

            for(int i = 0; i <= max; i++)
            {
                _triangleMeshes[i].CommitMesh();
            }
        }

        public void Render(Material genericMaterial)
        {
            if(_triangleMeshes == null || _triangleMeshes.Count == 0)
            {
                return;
            }

            if(genericMaterial && genericMaterial.SetPass(0))
            {
                int max = Mathf.Min(_currentTriangleMesh, _triangleMeshes.Count - 1);

                for(int i = 0; i <= max; i++)
                {
                    _triangleMeshes[i].Draw();
                }
            }
        }

        private readonly List<TriangleMesh> _triangleMeshes = new List<TriangleMesh>();
        private int _currentTriangleMesh;

        public PolygonMeshManager()
        {
            _triangleMeshes.Add(new TriangleMesh());
        }

        public void DrawTriangles(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color color)
        {
            int          triangleMeshIndex = _currentTriangleMesh;
            TriangleMesh triangleMesh      = _triangleMeshes[_currentTriangleMesh];

            if(triangleMesh.VertexCount + vertices.Length >= TriangleMesh.MAX_VERTEX_COUNT)
            {
                _currentTriangleMesh++;

                if(_currentTriangleMesh >= _triangleMeshes.Count)
                {
                    _triangleMeshes.Add(new TriangleMesh());
                }

                triangleMesh = _triangleMeshes[_currentTriangleMesh];
                triangleMesh.Clear();
            }

            triangleMesh.AddTriangles(matrix, vertices, indices, color);

            _currentTriangleMesh = triangleMeshIndex;
        }

        public void DrawPolygon(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color color)
        {
            int          triangleMeshIndex = _currentTriangleMesh;
            TriangleMesh triangleMesh      = _triangleMeshes[_currentTriangleMesh];

            if(triangleMesh.VertexCount + (indices.Length * 3 - 2) >= TriangleMesh.MAX_VERTEX_COUNT)
            {
                _currentTriangleMesh++;

                if(_currentTriangleMesh >= _triangleMeshes.Count)
                {
                    _triangleMeshes.Add(new TriangleMesh());
                }

                triangleMesh = _triangleMeshes[_currentTriangleMesh];
                triangleMesh.Clear();
            }

            triangleMesh.AddPolygon(matrix, vertices, indices, color);

            _currentTriangleMesh = triangleMeshIndex;
        }

        public void DrawPolygon(Vector3[] vertices, int[] indices, Color color)
        {
            int          triangleMeshIndex = _currentTriangleMesh;
            TriangleMesh triangleMesh      = _triangleMeshes[_currentTriangleMesh];

            if(triangleMesh.VertexCount + (indices.Length * 3 - 2) >= TriangleMesh.MAX_VERTEX_COUNT)
            {
                _currentTriangleMesh++;

                if(_currentTriangleMesh >= _triangleMeshes.Count)
                {
                    _triangleMeshes.Add(new TriangleMesh());
                }

                triangleMesh = _triangleMeshes[_currentTriangleMesh];
            }

            triangleMesh.AddPolygon(vertices, indices, color);

            _currentTriangleMesh = triangleMeshIndex;
        }

        internal void Destroy()
        {
            for(int i = 0; i < _triangleMeshes.Count; i++)
            {
                _triangleMeshes[i].Destroy();
            }

            _triangleMeshes.Clear();
            _currentTriangleMesh = 0;
        }

        internal void Clear()
        {
            _currentTriangleMesh = 0;

            for(int i = 0; i < _triangleMeshes.Count; i++)
            {
                _triangleMeshes[i].Clear();
            }
        }
    }
}

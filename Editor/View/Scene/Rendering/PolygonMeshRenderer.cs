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
                get => vertexCount;
            }

            public Vector3[] vertices = new Vector3[MAX_VERTEX_COUNT];
            public Color[] colors = new Color [MAX_VERTEX_COUNT];
            public int[] indices = new int [MAX_INDEX_COUNT];
            public int vertexCount;
            public int indexCount;

            private Mesh _mesh;

            public TriangleMesh()
            {
                Clear();
            }

            public void Clear()
            {
                vertexCount = 0;
                indexCount  = 0;
            }

            public void AddTriangles(Matrix4x4 matrix, Vector3[] triangleVertices, int[] triangleIndices, Color color)
            {
                if(triangleIndices.Length < 3 ||
                   triangleIndices.Length % 3 != 0)
                {
                    return;
                }

                int startIndex = vertexCount;

                if(matrix.isIdentity)
                {
                    int v = vertexCount;

                    for(int t = 0; t < triangleVertices.Length; t++, v++)
                    {
                        vertices[v] = triangleVertices[t];
                        colors[v]   = color;
                    }

                    vertexCount = v;
                }
                else
                {
                    int v = vertexCount;

                    for(int t = 0; t < triangleVertices.Length; t++, v++)
                    {
                        vertices[v] = matrix.MultiplyPoint(triangleVertices[t]);
                        colors[v]   = color;
                    }

                    vertexCount = v;
                }

                int i = indexCount;

                for(int t = 0; t < triangleIndices.Length; t++, i++)
                {
                    indices[i] = triangleIndices[t] + startIndex;
                }

                indexCount = i;
            }

            public void AddPolygon(Matrix4x4 matrix, Vector3[] polyVertices, int[] polyIndices, Color color)
            {
                if(polyIndices.Length < 3)
                {
                    return;
                }

                int startIndex = vertexCount;

                if(matrix.isIdentity)
                {
                    for(int i = 0; i < polyIndices.Length; i++)
                    {
                        vertices[vertexCount] = polyVertices[polyIndices[i]];
                        colors[vertexCount]   = color;
                        vertexCount++;
                    }
                }
                else
                {
                    for(int i = 0; i < polyIndices.Length; i++)
                    {
                        vertices[vertexCount] = matrix.MultiplyPoint(polyVertices[polyIndices[i]]);
                        colors[vertexCount]   = color;
                        vertexCount++;
                    }
                }

                for(int i = 2; i < polyIndices.Length; i++)
                {
                    indices[indexCount + 0] =  startIndex + 0;
                    indices[indexCount + 1] =  startIndex + i - 1;
                    indices[indexCount + 2] =  startIndex + i;
                    indexCount              += 3;
                }
            }

            public void AddPolygon(Vector3[] polyVertices, int[] polyIndices, Color color)
            {
                if(polyIndices.Length < 3)
                {
                    return;
                }

                int startIndex = vertexCount;

                for(int i = 0; i < polyIndices.Length; i++)
                {
                    vertices[vertexCount] = polyVertices[polyIndices[i]];
                    colors[vertexCount]   = color;
                    vertexCount++;
                }

                for(int i = 2; i < polyIndices.Length; i++)
                {
                    indices[indexCount + 0] =  startIndex + 0;
                    indices[indexCount + 1] =  startIndex + i - 1;
                    indices[indexCount + 2] =  startIndex + i;
                    indexCount              += 3;
                }
            }

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

                Vector3[] newVertices;
                Color[]   newColors;

                if(vertexCount == MAX_VERTEX_COUNT)
                {
                    newVertices = vertices;
                    newColors   = colors;
                }
                else
                {
                    newVertices = vertices.Take(vertexCount).ToArray();
                    newColors   = colors.Take(vertexCount).ToArray();
                }

                int[] newIndices = indexCount == MAX_INDEX_COUNT ? indices : indices.Take(indexCount).ToArray();

                _mesh.vertices = newVertices;
                _mesh.colors   = newColors;
                _mesh.SetIndices(newIndices, MeshTopology.Triangles, 0, false);
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

                _mesh       = null;
                vertices    = null;
                colors      = null;
                indices     = null;
                vertexCount = 0;
                indexCount  = 0;
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

            if(genericMaterial &&
               genericMaterial.SetPass(0))
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

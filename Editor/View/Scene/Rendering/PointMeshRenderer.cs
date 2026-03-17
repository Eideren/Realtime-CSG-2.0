using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class PointMeshManager
    {
        private sealed class PointMesh
        {
            public const int MAX_VERTEX_COUNT = 65000 - 4;
            public const int MAX_POINT_INDEX_COUNT = MAX_VERTEX_COUNT / 4 * 6;
            public const int MAX_LINE_INDEX_COUNT = MAX_VERTEX_COUNT / 4 * 8;

            public int VertexCount
            {
                get => vertexCount;
            }

            public readonly Vector3[] vertices = new Vector3[MAX_VERTEX_COUNT];

            public readonly int[] pointIndices = new int [MAX_POINT_INDEX_COUNT];
            public readonly Color[] pointColors = new Color [MAX_VERTEX_COUNT];

            public readonly int[] lineIndices = new int [MAX_LINE_INDEX_COUNT];
            public readonly Color[] lineColors = new Color [MAX_VERTEX_COUNT];

            public int vertexCount;
            public int pointIndexCount;
            public int lineIndexCount;

            private Mesh _pointMesh;
            private Mesh _lineMesh;

            public void Clear()
            {
                vertexCount     = 0;
                pointIndexCount = 0;
                lineIndexCount  = 0;
            }

            public void CommitMesh()
            {
                if(vertexCount == 0)
                {
                    if(_pointMesh != null && _pointMesh.vertexCount != 0)
                    {
                        _pointMesh.Clear(true);
                    }

                    if(_lineMesh != null && _lineMesh.vertexCount != 0)
                    {
                        _lineMesh.Clear(true);
                    }

                    return;
                }

                if(_pointMesh != null)
                {
                    _pointMesh.Clear(true);
                }
                else
                {
                    _pointMesh           = new Mesh();
                    _pointMesh.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
                    _pointMesh.MarkDynamic();
                }

                if(_lineMesh != null)
                {
                    _lineMesh.Clear(true);
                }
                else
                {
                    _lineMesh           = new Mesh();
                    _lineMesh.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
                    _lineMesh.MarkDynamic();
                }

                Vector3[] newVertices;
                Color[]   newPointColors;
                Color[]   newLineColors;
                int[]     newPointIndices;
                int[]     newLineIndices;

                if(vertexCount == MAX_VERTEX_COUNT)
                {
                    newVertices     = vertices;
                    newPointColors  = pointColors;
                    newLineColors   = lineColors;
                    newPointIndices = pointIndices;
                    newLineIndices  = lineIndices;
                }
                else
                {
                    newVertices     = vertices.Take(vertexCount).ToArray();
                    newPointColors  = pointColors.Take(vertexCount).ToArray();
                    newLineColors   = lineColors.Take(vertexCount).ToArray();
                    newPointIndices = pointIndices.Take(pointIndexCount).ToArray();
                    newLineIndices  = lineIndices.Take(lineIndexCount).ToArray();
                }

                if(pointIndexCount > 0)
                {
                    _pointMesh.vertices = newVertices;
                    _pointMesh.colors   = newPointColors;
                    _pointMesh.SetIndices(newPointIndices, MeshTopology.Triangles, 0, false);
                    _pointMesh.RecalculateBounds();
                    _pointMesh.UploadMeshData(false);
                }

                if(lineIndexCount > 0)
                {
                    _lineMesh.vertices = newVertices;
                    _lineMesh.colors   = newLineColors;
                    _lineMesh.SetIndices(newLineIndices, MeshTopology.Lines, 0, false);
                    _lineMesh.RecalculateBounds();
                    _lineMesh.UploadMeshData(false);
                }
            }

            public void DrawPoints()
            {
                if(vertexCount == 0 ||
                   pointIndexCount == 0)
                {
                    return;
                }

                Graphics.DrawMeshNow(_pointMesh, MathConstants.identityMatrix);
            }

            public void DrawLines()
            {
                if(vertexCount == 0 ||
                   lineIndexCount == 0)
                {
                    return;
                }

                Graphics.DrawMeshNow(_lineMesh, MathConstants.identityMatrix);
            }

            internal void Destroy()
            {
                if(_pointMesh)
                {
                    Object.DestroyImmediate(_pointMesh);
                }

                if(_lineMesh)
                {
                    Object.DestroyImmediate(_lineMesh);
                }

                _pointMesh = null;
                _lineMesh  = null;
            }
        }

        public void Begin()
        {
            _currentPointMesh = 0;

            for(int i = 0; i < _pointMeshes.Count; i++)
            {
                _pointMeshes[i].Clear();
            }
        }

        public void End()
        {
            for(int i = 0; i <= _currentPointMesh; i++)
            {
                _pointMeshes[i].CommitMesh();
            }
        }

        public void Render(Material pointMaterial, Material lineMaterial)
        {
            if(pointMaterial.SetPass(0))
            {
                for(int i = 0; i <= _currentPointMesh; i++)
                {
                    _pointMeshes[i].DrawPoints();
                }
            }

            if(lineMaterial.SetPass(0))
            {
                for(int i = 0; i <= _currentPointMesh; i++)
                {
                    _pointMeshes[i].DrawLines();
                }
            }
        }

        private readonly List<PointMesh> _pointMeshes = new List<PointMesh>();
        private int _currentPointMesh;

        public PointMeshManager()
        {
            _pointMeshes.Add(new PointMesh());
        }

        public void DrawPoint(Camera camera, Vector3 position, float size, Color innerColor, Color outerColor)
        {
            Vector3 right = camera.transform.right;
            Vector3 up    = camera.transform.up;

            Vector3 p0 = right + up;
            Vector3 p1 = right - up;
            Vector3 p2 = -right - up;
            Vector3 p3 = -right + up;

            PointMesh pointMesh      = _pointMeshes[_currentPointMesh];
            int       dstVertexCount = pointMesh.vertexCount;

            if(dstVertexCount + 4 >= PointMesh.MAX_VERTEX_COUNT)
            {
                _currentPointMesh++;

                if(_currentPointMesh >= _pointMeshes.Count)
                {
                    _pointMeshes.Add(new PointMesh());
                }

                pointMesh      = _pointMeshes[_currentPointMesh];
                dstVertexCount = pointMesh.vertexCount;
            }

            Vector3[] dstVertices     = pointMesh.vertices;
            Color[]   dstLineColors   = pointMesh.lineColors;
            Color[]   dstPointColors  = pointMesh.pointColors;
            int[]     dstPointIndices = pointMesh.pointIndices;
            int[]     dstLineIndices  = pointMesh.lineIndices;

            int index              = dstVertexCount;
            int dstPointIndexCount = pointMesh.pointIndexCount;
            int dstLineIndexCount  = pointMesh.lineIndexCount;

            dstVertices[dstVertexCount]    = position + p0 * size;
            dstPointColors[dstVertexCount] = innerColor;
            dstLineColors[dstVertexCount]  = outerColor;
            dstVertexCount++;

            dstVertices[dstVertexCount]    = position + p1 * size;
            dstPointColors[dstVertexCount] = innerColor;
            dstLineColors[dstVertexCount]  = outerColor;
            dstVertexCount++;

            dstVertices[dstVertexCount]    = position + p2 * size;
            dstPointColors[dstVertexCount] = innerColor;
            dstLineColors[dstVertexCount]  = outerColor;
            dstVertexCount++;

            dstVertices[dstVertexCount]    = position + p3 * size;
            dstPointColors[dstVertexCount] = innerColor;
            dstLineColors[dstVertexCount]  = outerColor;
            dstVertexCount++;

            dstPointIndices[dstPointIndexCount] = index + 0;
            dstPointIndexCount++;
            dstPointIndices[dstPointIndexCount] = index + 1;
            dstPointIndexCount++;
            dstPointIndices[dstPointIndexCount] = index + 2;
            dstPointIndexCount++;
            dstPointIndices[dstPointIndexCount] = index + 0;
            dstPointIndexCount++;
            dstPointIndices[dstPointIndexCount] = index + 2;
            dstPointIndexCount++;
            dstPointIndices[dstPointIndexCount] = index + 3;
            dstPointIndexCount++;

            dstLineIndices[dstLineIndexCount] = index + 0;
            dstLineIndexCount++;
            dstLineIndices[dstLineIndexCount] = index + 1;
            dstLineIndexCount++;
            dstLineIndices[dstLineIndexCount] = index + 1;
            dstLineIndexCount++;
            dstLineIndices[dstLineIndexCount] = index + 2;
            dstLineIndexCount++;
            dstLineIndices[dstLineIndexCount] = index + 2;
            dstLineIndexCount++;
            dstLineIndices[dstLineIndexCount] = index + 3;
            dstLineIndexCount++;
            dstLineIndices[dstLineIndexCount] = index + 3;
            dstLineIndexCount++;
            dstLineIndices[dstLineIndexCount] = index + 0;
            dstLineIndexCount++;

            pointMesh.vertexCount     = dstVertexCount;
            pointMesh.pointIndexCount = dstPointIndexCount;
            pointMesh.lineIndexCount  = dstLineIndexCount;
        }

        public void DrawPoints(Camera camera, Vector3[] vertices, float[] sizes, Color[] colors)
        {
            Vector3 right = camera.transform.right;
            Vector3 up    = camera.transform.up;

            Vector3 p0 = right + up;
            Vector3 p1 = right - up;
            Vector3 p2 = -right - up;
            Vector3 p3 = -right + up;

            int       pointMeshIndex     = _currentPointMesh;
            PointMesh pointMesh          = _pointMeshes[_currentPointMesh];
            Vector3[] dstVertices        = pointMesh.vertices;
            Color[]   dstLineColors      = pointMesh.lineColors;
            Color[]   dstPointColors     = pointMesh.pointColors;
            int[]     dstPointIndices    = pointMesh.pointIndices;
            int[]     dstLineIndices     = pointMesh.lineIndices;
            int       dstVertexCount     = pointMesh.vertexCount;
            int       dstPointIndexCount = pointMesh.pointIndexCount;
            int       dstLineIndexCount  = pointMesh.lineIndexCount;

            for(int i = 0, c = 0; i < vertices.Length; i++, c += 2)
            {
                int index = dstVertexCount;

                if(dstVertexCount + 4 >= PointMesh.MAX_VERTEX_COUNT)
                {
                    pointMesh.vertexCount     = dstVertexCount;
                    pointMesh.pointIndexCount = dstPointIndexCount;
                    pointMesh.lineIndexCount  = dstLineIndexCount;

                    _currentPointMesh++;

                    if(_currentPointMesh >= _pointMeshes.Count)
                    {
                        _pointMeshes.Add(new PointMesh());
                    }

                    pointMesh = _pointMeshes[_currentPointMesh];
                    pointMesh.Clear();

                    dstVertices        = pointMesh.vertices;
                    dstLineColors      = pointMesh.lineColors;
                    dstPointColors     = pointMesh.pointColors;
                    dstPointIndices    = pointMesh.pointIndices;
                    dstLineIndices     = pointMesh.lineIndices;
                    dstVertexCount     = pointMesh.vertexCount;
                    dstPointIndexCount = pointMesh.pointIndexCount;
                    dstLineIndexCount  = pointMesh.lineIndexCount;
                    index              = dstVertexCount;
                }

                Vector3 position   = vertices[i];
                Color   innerColor = colors[c + 1];
                Color   outerColor = colors[c + 0];
                float   size       = sizes[i];

                dstVertices[dstVertexCount]    = position + p0 * size;
                dstPointColors[dstVertexCount] = innerColor;
                dstLineColors[dstVertexCount]  = outerColor;
                dstVertexCount++;

                dstVertices[dstVertexCount]    = position + p1 * size;
                dstPointColors[dstVertexCount] = innerColor;
                dstLineColors[dstVertexCount]  = outerColor;
                dstVertexCount++;

                dstVertices[dstVertexCount]    = position + p2 * size;
                dstPointColors[dstVertexCount] = innerColor;
                dstLineColors[dstVertexCount]  = outerColor;
                dstVertexCount++;

                dstVertices[dstVertexCount]    = position + p3 * size;
                dstPointColors[dstVertexCount] = innerColor;
                dstLineColors[dstVertexCount]  = outerColor;
                dstVertexCount++;

                dstPointIndices[dstPointIndexCount] = index + 0;
                dstPointIndexCount++;
                dstPointIndices[dstPointIndexCount] = index + 1;
                dstPointIndexCount++;
                dstPointIndices[dstPointIndexCount] = index + 2;
                dstPointIndexCount++;
                dstPointIndices[dstPointIndexCount] = index + 0;
                dstPointIndexCount++;
                dstPointIndices[dstPointIndexCount] = index + 2;
                dstPointIndexCount++;
                dstPointIndices[dstPointIndexCount] = index + 3;
                dstPointIndexCount++;

                dstLineIndices[dstLineIndexCount] = index + 0;
                dstLineIndexCount++;
                dstLineIndices[dstLineIndexCount] = index + 1;
                dstLineIndexCount++;
                dstLineIndices[dstLineIndexCount] = index + 1;
                dstLineIndexCount++;
                dstLineIndices[dstLineIndexCount] = index + 2;
                dstLineIndexCount++;
                dstLineIndices[dstLineIndexCount] = index + 2;
                dstLineIndexCount++;
                dstLineIndices[dstLineIndexCount] = index + 3;
                dstLineIndexCount++;
                dstLineIndices[dstLineIndexCount] = index + 3;
                dstLineIndexCount++;
                dstLineIndices[dstLineIndexCount] = index + 0;
                dstLineIndexCount++;
            }

            pointMesh.vertexCount     = dstVertexCount;
            pointMesh.pointIndexCount = dstPointIndexCount;
            pointMesh.lineIndexCount  = dstLineIndexCount;
            _currentPointMesh         = pointMeshIndex;
        }

        internal void Destroy()
        {
            for(int i = 0; i < _pointMeshes.Count; i++)
            {
                _pointMeshes[i].Destroy();
            }

            _pointMeshes.Clear();
        }
    }
}

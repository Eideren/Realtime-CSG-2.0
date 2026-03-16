using UnityEngine;
using System.Collections.Generic;
using InternalRealtimeCSG;
using System.Linq;

namespace RealtimeCSG
{
    internal sealed class PointMeshManager
    {
        private sealed class PointMesh
        {
            public const int MaxVertexCount = 65000 - 4;
            public const int MaxPointIndexCount = MaxVertexCount / 4 * 6;
            public const int MaxLineIndexCount = MaxVertexCount / 4 * 8;

            public int VertexCount
            {
                get => vertexCount;
            }

            public Vector3[] vertices = new Vector3[MaxVertexCount];

            public int[] pointIndices = new int [MaxPointIndexCount];
            public Color[] pointColors = new Color [MaxVertexCount];

            public int[] lineIndices = new int [MaxLineIndexCount];
            public Color[] lineColors = new Color [MaxVertexCount];

            public int vertexCount = 0;
            public int pointIndexCount = 0;
            public int lineIndexCount = 0;

            private Mesh pointMesh;
            private Mesh lineMesh;

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
                    if(pointMesh != null && pointMesh.vertexCount != 0)
                    {
                        pointMesh.Clear(true);
                    }

                    if(lineMesh != null && lineMesh.vertexCount != 0)
                    {
                        lineMesh.Clear(true);
                    }

                    return;
                }

                if(pointMesh != null)
                {
                    pointMesh.Clear(true);
                }
                else
                {
                    pointMesh           = new Mesh();
                    pointMesh.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
                    pointMesh.MarkDynamic();
                }

                if(lineMesh != null)
                {
                    lineMesh.Clear(true);
                }
                else
                {
                    lineMesh           = new Mesh();
                    lineMesh.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
                    lineMesh.MarkDynamic();
                }

                Vector3[] newVertices;
                Color[]   newPointColors;
                Color[]   newLineColors;
                int[]     newPointIndices;
                int[]     newLineIndices;

                if(vertexCount == MaxVertexCount)
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
                    pointMesh.vertices = newVertices;
                    pointMesh.colors   = newPointColors;
                    pointMesh.SetIndices(newPointIndices, MeshTopology.Triangles, 0, false);
                    pointMesh.RecalculateBounds();
                    pointMesh.UploadMeshData(false);
                }

                if(lineIndexCount > 0)
                {
                    lineMesh.vertices = newVertices;
                    lineMesh.colors   = newLineColors;
                    lineMesh.SetIndices(newLineIndices, MeshTopology.Lines, 0, false);
                    lineMesh.RecalculateBounds();
                    lineMesh.UploadMeshData(false);
                }
            }

            public void DrawPoints()
            {
                if(vertexCount == 0 ||
                   pointIndexCount == 0)
                {
                    return;
                }

                Graphics.DrawMeshNow(pointMesh, MathConstants.identityMatrix);
            }

            public void DrawLines()
            {
                if(vertexCount == 0 ||
                   lineIndexCount == 0)
                {
                    return;
                }

                Graphics.DrawMeshNow(lineMesh, MathConstants.identityMatrix);
            }

            internal void Destroy()
            {
                if(pointMesh)
                {
                    UnityEngine.Object.DestroyImmediate(pointMesh);
                }

                if(lineMesh)
                {
                    UnityEngine.Object.DestroyImmediate(lineMesh);
                }

                pointMesh = null;
                lineMesh  = null;
            }
        }

        public void Begin()
        {
            currentPointMesh = 0;

            for(int i = 0; i < pointMeshes.Count; i++)
            {
                pointMeshes[i].Clear();
            }
        }

        public void End()
        {
            for(int i = 0; i <= currentPointMesh; i++)
            {
                pointMeshes[i].CommitMesh();
            }
        }

        public void Render(Material pointMaterial, Material lineMaterial)
        {
            if(pointMaterial.SetPass(0))
            {
                for(int i = 0; i <= currentPointMesh; i++)
                {
                    pointMeshes[i].DrawPoints();
                }
            }

            if(lineMaterial.SetPass(0))
            {
                for(int i = 0; i <= currentPointMesh; i++)
                {
                    pointMeshes[i].DrawLines();
                }
            }
        }

        private List<PointMesh> pointMeshes = new List<PointMesh>();
        private int currentPointMesh = 0;

        public PointMeshManager()
        {
            pointMeshes.Add(new PointMesh());
        }

        public void DrawPoint(Camera camera, Vector3 position, float size, Color innerColor, Color outerColor)
        {
            Vector3 right = camera.transform.right;
            Vector3 up    = camera.transform.up;

            Vector3 p0 = right + up;
            Vector3 p1 = right - up;
            Vector3 p2 = -right - up;
            Vector3 p3 = -right + up;

            PointMesh pointMesh      = pointMeshes[currentPointMesh];
            int       dstVertexCount = pointMesh.vertexCount;

            if(dstVertexCount + 4 >= PointMesh.MaxVertexCount)
            {
                currentPointMesh++;

                if(currentPointMesh >= pointMeshes.Count)
                {
                    pointMeshes.Add(new PointMesh());
                }

                pointMesh      = pointMeshes[currentPointMesh];
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

            int       pointMeshIndex     = currentPointMesh;
            PointMesh pointMesh          = pointMeshes[currentPointMesh];
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

                if(dstVertexCount + 4 >= PointMesh.MaxVertexCount)
                {
                    pointMesh.vertexCount     = dstVertexCount;
                    pointMesh.pointIndexCount = dstPointIndexCount;
                    pointMesh.lineIndexCount  = dstLineIndexCount;

                    currentPointMesh++;

                    if(currentPointMesh >= pointMeshes.Count)
                    {
                        pointMeshes.Add(new PointMesh());
                    }

                    pointMesh = pointMeshes[currentPointMesh];
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
            currentPointMesh          = pointMeshIndex;
        }

        internal void Destroy()
        {
            for(int i = 0; i < pointMeshes.Count; i++)
            {
                pointMeshes[i].Destroy();
            }

            pointMeshes.Clear();
        }
    }
}

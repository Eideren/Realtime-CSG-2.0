using System;
using System.Collections.Generic;
using System.Globalization;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal static class PaintUtility
    {
#region Materials

        private static Material m_CustomDotMaterial;

        public static Material CustomDotMaterial
        {
            get
            {
                if(!m_CustomDotMaterial)
                {
                    m_CustomDotMaterial = MaterialUtility.GenerateEditorMaterial("customDot");
                }

                return m_CustomDotMaterial;
            }
        }

        private static Material m_CustomWireMaterial;

        private static Material CustomWireMaterial
        {
            get
            {
                if(!m_CustomWireMaterial)
                {
                    m_CustomWireMaterial = MaterialUtility.GenerateEditorMaterial("customWire");
                }

                return m_CustomWireMaterial;
            }
        }

        private static Material m_CustomSurfaceNoDepthMaterial;

        public static Material CustomSurfaceNoDepthMaterial
        {
            get
            {
                if(!m_CustomSurfaceNoDepthMaterial)
                {
                    m_CustomSurfaceNoDepthMaterial = MaterialUtility.GenerateEditorMaterial("customNoDepthSurface");
                }

                return m_CustomSurfaceNoDepthMaterial;
            }
        }

        private static Material m_CustomWireDottedMaterial;

        private static Material CustomWireDottedMaterial
        {
            get
            {
                if(!m_CustomWireDottedMaterial)
                {
                    m_CustomWireDottedMaterial = MaterialUtility.GenerateEditorMaterial("customWireDotted");
                }

                return m_CustomWireDottedMaterial;
            }
        }

#endregion

        private static readonly PointMeshManager pointMeshManager = new PointMeshManager();

        public static void DrawDoubleDots(Camera camera, Vector3[] positions, float[] sizes, Color[] colors, int points)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(positions.Length < points || sizes.Length < points || colors.Length < points * 2)
            {
                return;
            }

            pointMeshManager.Begin();
            pointMeshManager.DrawPoints(camera, positions, sizes, colors);
            pointMeshManager.End();
            pointMeshManager.Render(CustomDotMaterial, CustomSurfaceNoDepthMaterial);
        }

        public static void DrawDoubleDots(Camera camera, Matrix4x4 matrix, Vector3[] positions, float[] sizes, Color[] colors, int points)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(positions.Length < points || sizes.Length < points || colors.Length < points * 2)
            {
                return;
            }

            Vector3 right = camera.transform.right;
            Vector3 up    = camera.transform.up;

            Vector3 p0 = right + up;
            Vector3 p1 = right - up;
            Vector3 p2 = -right - up;
            Vector3 p3 = -right + up;

            Material material = CustomDotMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.QUADS);

                for(int p = 0, d = 0; p < points; p++, d += 2)
                {
                    Vector3 position = matrix.MultiplyPoint(positions[p]);
                    float   size     = sizes[p];

                    GL.Color(colors[d + 1]);

                    GL.Vertex(position + p0 * size);
                    GL.Vertex(position + p1 * size);
                    GL.Vertex(position + p2 * size);
                    GL.Vertex(position + p3 * size);
                }

                GL.End();
            }

            material = CustomSurfaceNoDepthMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);

                for(int p = 0, d = 0; p < points; p++, d += 2)
                {
                    Vector3 position = matrix.MultiplyPoint(positions[p]);
                    float   size     = sizes[p];
                    Vector3 dp0      = position + p0 * size;
                    Vector3 dp1      = position + p1 * size;
                    Vector3 dp2      = position + p2 * size;
                    Vector3 dp3      = position + p3 * size;

                    GL.Color(colors[d + 0]);

                    GL.Vertex(dp0);
                    GL.Vertex(dp1);
                    GL.Vertex(dp1);
                    GL.Vertex(dp2);
                    GL.Vertex(dp2);
                    GL.Vertex(dp3);
                    GL.Vertex(dp3);
                    GL.Vertex(dp0);
                }

                GL.End();
            }
        }

        public static void SquareDotCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Camera  camera = Camera.current;
            Vector3 right  = camera.transform.right;
            Vector3 up     = camera.transform.up;

            Vector3 p0 = position + (right + up) * size;
            Vector3 p1 = position + (right - up) * size;
            Vector3 p2 = position + (-right - up) * size;
            Vector3 p3 = position + (-right + up) * size;

            position = Handles.matrix.MultiplyPoint(position);

            Color c = Handles.color * new Color(1, 1, 1, .5f) + (Handles.lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)) * new Color(1, 1, 1, 0.99f);

            Material material = CustomDotMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.QUADS);

                GL.Color(c);
                GL.Vertex(p0);
                GL.Vertex(p1);
                GL.Vertex(p2);
                GL.Vertex(p3);

                GL.End();
            }

            material = CustomSurfaceNoDepthMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);

                GL.Color(Color.black);
                GL.Vertex(p0);
                GL.Vertex(p1);
                GL.Vertex(p1);
                GL.Vertex(p2);
                GL.Vertex(p2);
                GL.Vertex(p3);
                GL.Vertex(p3);
                GL.Vertex(p0);

                GL.End();
            }
        }

        public static void DiamondDotCap(Vector3 position, float size)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Camera  camera = Camera.current;
            Vector3 right  = camera.transform.right;
            Vector3 up     = camera.transform.up;

            Vector3 p0 = position + -right * size;
            Vector3 p1 = position + -up * size;
            Vector3 p2 = position + right * size;
            Vector3 p3 = position + up * size;

            position = Handles.matrix.MultiplyPoint(position);

            Color c = Handles.color * new Color(1, 1, 1, .5f) + (Handles.lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)) * new Color(1, 1, 1, 0.99f);

            Material material = CustomDotMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.QUADS);

                GL.Color(c);
                GL.Vertex(p0);
                GL.Vertex(p1);
                GL.Vertex(p2);
                GL.Vertex(p3);

                GL.End();
            }

            material = CustomSurfaceNoDepthMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);

                {
                    GL.Color(Color.black);
                    GL.Vertex(p0);
                    GL.Vertex(p1);
                    GL.Vertex(p1);
                    GL.Vertex(p2);
                    GL.Vertex(p2);
                    GL.Vertex(p3);
                    GL.Vertex(p3);
                    GL.Vertex(p0);
                }

                GL.End();
            }
        }

        private static Vector2[] m_CirclePoints;

        private static void SetupCirclePoints()
        {
            const int steps = 16;
            m_CirclePoints = new Vector2[steps];

            for(int i = 0; i < steps; i++)
            {
                m_CirclePoints[i] = new Vector2(Mathf.Cos(i / (float)steps * Mathf.PI * 2), Mathf.Sin(i / (float)steps * Mathf.PI * 2));
            }
        }

        public static void CircleDotCap(Vector3 position, float size)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Camera camera = Camera.current;

            DrawFilledCameraAlignedCircle(camera, position, size);
        }

        public static void DrawCameraAlignedCircle(Camera camera, Vector3 position, float size, Color innerColor, Color outerColor)
        {
            Vector3 right = camera.transform.right;
            Vector3 up    = camera.transform.up;

            if(m_CirclePoints == null)
            {
                SetupCirclePoints();
            }

            Vector3[] points = new Vector3[m_CirclePoints.Length];

            for(int i = 0; i < m_CirclePoints.Length; i++)
            {
                Vector2 circle = m_CirclePoints[i];
                points[i] = position + (right * circle.x + up * circle.y) * size;
            }

            position = Handles.matrix.MultiplyPoint(position);

            Color c = outerColor * new Color(1, 1, 1, .5f) + (Handles.lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)) * new Color(1, 1, 1, 0.99f);

            Handles.color = c;

            for(int i = points.Length - 1, j = 0; j < points.Length; i = j, j++)
            {
                Handles.DrawAAPolyLine(6.0f, points[i], points[j]);
            }

            c = innerColor * new Color(1, 1, 1, .5f) + (Handles.lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)) * new Color(1, 1, 1, 0.99f);

            Handles.color = c;

            for(int i = points.Length - 1, j = 0; j < points.Length; i = j, j++)
            {
                Handles.DrawAAPolyLine(2.0f, points[i], points[j]);
            }
        }

        public static void DrawFilledCameraAlignedCircle(Camera camera, Vector3 position, float size)
        {
            Vector3 right = camera.transform.right;
            Vector3 up    = camera.transform.up;

            if(m_CirclePoints == null)
            {
                SetupCirclePoints();
            }

            Vector3[] points = new Vector3[m_CirclePoints.Length];

            for(int i = 0; i < m_CirclePoints.Length; i++)
            {
                Vector2 circle = m_CirclePoints[i];
                points[i] = position + (right * circle.x + up * circle.y) * size;
            }

            position = Handles.matrix.MultiplyPoint(position);

            Color c = Handles.color * new Color(1, 1, 1, .5f) + (Handles.lighting ? new Color(0, 0, 0, .5f) : new Color(0, 0, 0, 0)) * new Color(1, 1, 1, 0.99f);

            Material material = CustomDotMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.TRIANGLES);

                GL.Color(c);

                for(int i = 1; i < points.Length - 1; i++)
                {
                    GL.Vertex(points[0]);
                    GL.Vertex(points[i]);
                    GL.Vertex(points[i + 1]);
                }

                GL.End();
            }

            material = CustomSurfaceNoDepthMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);

                GL.Color(Color.black);
                GL.Vertex(points[0]);

                for(int i = 1; i < points.Length; i++)
                {
                    GL.Vertex(points[i]);
                    GL.Vertex(points[i]);
                }

                GL.Vertex(points[0]);

                GL.End();
            }
        }

        public static void DrawLine(Vector3 from, Vector3 to, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);
                GL.Color(c);
                GL.Vertex(from);
                GL.Vertex(to);
                GL.End();
            }
        }

        public static void DrawLine(Vector3 pt0, Vector3 pt1, float thickness, Color color)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = MaterialUtility.ZTestGenericLine;

            if(material == null)
            {
                return;
            }

            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.InitGenericLineMaterial(material);

            if(material.SetPass(0))
            {
                thickness *= EditorGUIUtility.pixelsPerPoint;
                thickness *= 100;

                Vector3 thickness0 = new Vector3(thickness, -1, 0);
                Vector3 thickness1 = new Vector3(thickness, 1, 0);
                Vector3 thickness2 = new Vector3(thickness, 1, 0);
                Vector3 thickness3 = new Vector3(thickness, -1, 0);

                GL.Begin(GL.QUADS);
                GL.Color(color);

                GL.MultiTexCoord(0, pt0);
                GL.MultiTexCoord(1, thickness0);
                GL.Vertex(pt1);
                GL.MultiTexCoord(0, pt0);
                GL.MultiTexCoord(1, thickness1);
                GL.Vertex(pt1);
                GL.MultiTexCoord(0, pt1);
                GL.MultiTexCoord(1, thickness2);
                GL.Vertex(pt0);
                GL.MultiTexCoord(0, pt1);
                GL.MultiTexCoord(1, thickness3);
                GL.Vertex(pt0);
                GL.End();
            }
        }

        public static void DrawLine(Matrix4x4 matrix, Vector3 pt0, Vector3 pt1, float thickness, Color color)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = MaterialUtility.ZTestGenericLine;

            if(material == null)
            {
                return;
            }

            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.InitGenericLineMaterial(material);

            if(material.SetPass(0))
            {
                thickness *= EditorGUIUtility.pixelsPerPoint;
                thickness *= 100;

                Vector3 thickness0 = new Vector3(thickness, -1, 0);
                Vector3 thickness1 = new Vector3(thickness, 1, 0);
                Vector3 thickness2 = new Vector3(thickness, 1, 0);
                Vector3 thickness3 = new Vector3(thickness, -1, 0);

                GL.PushMatrix();

                GL.Begin(GL.QUADS);
                GL.Color(color);

                pt0 = matrix.MultiplyPoint(pt0);
                pt1 = matrix.MultiplyPoint(pt1);

                GL.MultiTexCoord(0, pt0);
                GL.MultiTexCoord(1, thickness0);
                GL.Vertex(pt1);
                GL.MultiTexCoord(0, pt0);
                GL.MultiTexCoord(1, thickness1);
                GL.Vertex(pt1);
                GL.MultiTexCoord(0, pt1);
                GL.MultiTexCoord(1, thickness2);
                GL.Vertex(pt0);
                GL.MultiTexCoord(0, pt1);
                GL.MultiTexCoord(1, thickness3);
                GL.Vertex(pt0);

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawLine(Matrix4x4 matrix, Vector3 from, Vector3 to, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.LINES);
                GL.Color(c);
                GL.Vertex(from);
                GL.Vertex(to);
                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawDottedLine(Vector3 from, Vector3 to, Color c, float dotSize = 4.0f)
        {
            Handles.color = c;
            Handles.DrawDottedLine(from, to, dotSize);
        }

        public static void DrawDottedLine(Matrix4x4 matrix, Vector3 from, Vector3 to, Color c, float dotSize = 4.0f)
        {
            Matrix4x4 origMatrix = Handles.matrix;
            Color     prevColor  = Handles.color;
            Handles.matrix = matrix;
            Handles.color  = c;
            Handles.DrawDottedLine(from, to, dotSize);
            Handles.color  = prevColor;
            Handles.matrix = origMatrix;
        }

        public static void DrawDottedLines(Vector3[] lines, Color c, float dotSize = 4.0f)
        {
            Matrix4x4 origMatrix = Handles.matrix;
            Color     prevColor  = Handles.color;
            Handles.matrix = MathConstants.identityMatrix;
            Handles.color  = c;
            Handles.DrawDottedLines(lines, dotSize);
            Handles.color  = prevColor;
            Handles.matrix = origMatrix;
        }

        public static void DrawDottedLines(Matrix4x4 matrix, Vector3[] lines, Color c, float dotSize = 4.0f)
        {
            Handles.matrix = matrix;
            Handles.color  = c;
            Handles.DrawDottedLines(lines, dotSize);
        }

        public static void DrawDottedLines(Vector3[] vertices, int[] indices, Color[] colors, float sreenSpaceSize = 4.0f)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(indices.Length != colors.Length * 2)
            {
                Debug.LogWarning("indices.Length != colors.Length * 2");

                return;
            }

            Material material = CustomWireDottedMaterial;

            if(material && material.SetPass(0))
            {
                float dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
                GL.Begin(GL.LINES);

                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int i0 = indices[i + 0];
                    int i1 = indices[i + 1];

                    if(i0 < 0 || i0 >= vertices.Length || i1 < 0 || i1 >= vertices.Length)
                    {
                        continue;
                    }

                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.Color(colors[c]);
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt1);
                }

                GL.End();
            }
        }

        public static void DrawDottedLines(Vector3[] vertices, int[] indices, Color color, float sreenSpaceSize = 4.0f)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = CustomWireDottedMaterial;

            if(material && material.SetPass(0))
            {
                float dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
                GL.Begin(GL.LINES);
                GL.Color(color);

                for(int i = 0; i < indices.Length; i += 2)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt1);
                }

                GL.End();
            }
        }

        public static void DrawDottedLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color color, float sreenSpaceSize = 4.0f)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = CustomWireDottedMaterial;

            if(material && material.SetPass(0))
            {
                float dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.LINES);
                GL.Color(color);

                for(int i = 0; i < indices.Length; i += 2)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt1);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawDottedLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color[] colors, float sreenSpaceSize = 4.0f)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(indices.Length != colors.Length * 2)
            {
                Debug.LogWarning("indices.Length != colors.Length * 2");

                return;
            }

            Material material = CustomWireDottedMaterial;

            if(material && material.SetPass(0))
            {
                float dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.LINES);

                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.Color(colors[c]);
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt1);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawLines(Vector3[] lines, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if((lines.Length & 1) == 1)
            {
                Debug.LogWarning("Uneven number of vertices in line array!");
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);
                GL.Color(c);

                for(int i = 0; i < lines.Length; i += 2)
                {
                    Vector3 pt0 = lines[i + 0];
                    Vector3 pt1 = lines[i + 1];
                    GL.Vertex(pt0);
                    GL.Vertex(pt1);
                }

                GL.End();
            }
        }

        public static void DrawLines(Matrix4x4 matrix, Vector3[] lines, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if((lines.Length & 1) == 1)
            {
                Debug.LogWarning("Uneven number of vertices in line array!");
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.LINES);
                GL.Color(c);

                for(int i = 0; i < lines.Length; i += 2)
                {
                    Vector3 pt0 = lines[i + 0];
                    Vector3 pt1 = lines[i + 1];
                    GL.Vertex(pt0);
                    GL.Vertex(pt1);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawLines(Vector3[] vertices, int[] indices, Color[] colors)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(indices.Length != colors.Length * 2)
            {
                Debug.LogWarning("indices.Length != colors.Length * 2");

                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);

                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.Color(colors[c]);
                    GL.Vertex(pt0);
                    GL.Vertex(pt1);
                }

                GL.End();
            }
        }

        public static void DrawUnoccludedLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color color)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                Color     prevColor  = Handles.color;
                Matrix4x4 origMatrix = Handles.matrix;
                Handles.matrix = matrix;
                Handles.color  = color;

                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    Handles.DrawLine(pt0, pt1);
                }

                Handles.color  = prevColor;
                Handles.matrix = origMatrix;
            }
        }

        public static void DrawUnoccludedLines(Vector3[] vertices, int[] indices, Color color)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                Color prevColor = Handles.color;
                Handles.color = color;

                for(int i = 0; i < indices.Length; i += 2)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    Handles.DrawLine(pt0, pt1);
                }

                Handles.color = prevColor;
            }
        }

        public static void DrawLines(Vector3[] vertices, int[] indices, Color color)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);
                GL.Color(color);

                for(int i = 0; i < indices.Length; i += 2)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.Vertex(pt0);
                    GL.Vertex(pt1);
                }

                GL.End();
            }
        }

        public static void DrawLines(Vector3[] vertices, int[] indices, float thickness, Color[] colors)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(indices.Length != colors.Length * 2)
            {
                Debug.LogWarning("indices.Length != colors.Length * 2");

                return;
            }

            Material material = MaterialUtility.ZTestGenericLine;

            if(material == null)
            {
                return;
            }

            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.InitGenericLineMaterial(material);

            if(material.SetPass(0))
            {
                thickness *= EditorGUIUtility.pixelsPerPoint;
                thickness *= 100;

                Vector3 thickness0 = new Vector3(thickness, -1, 0);
                Vector3 thickness1 = new Vector3(thickness, 1, 0);
                Vector3 thickness2 = new Vector3(thickness, 1, 0);
                Vector3 thickness3 = new Vector3(thickness, -1, 0);

                GL.Begin(GL.QUADS);

                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int i0 = indices[i + 0];
                    int i1 = indices[i + 1];

                    if(i0 < 0 || i0 >= vertices.Length || i1 < 0 || i1 >= vertices.Length)
                    {
                        continue;
                    }

                    GL.Color(colors[c]);

                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];

                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness0);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness1);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness2);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness3);
                    GL.Vertex(pt0);
                }

                GL.End();
            }
        }

        public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color[] colors)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(indices.Length != colors.Length * 2)
            {
                Debug.LogWarning("indices.Length != colors.Length * 2");

                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.LINES);

                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.Color(colors[c]);
                    GL.Vertex(pt0);
                    GL.Vertex(pt1);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, int lineCount, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(lineCount == 0)
            {
                return;
            }

            if(indices.Length < lineCount * 2)
            {
                Debug.LogWarning("indices.Length < lineCount * 2");

                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.LINES);
                GL.Color(c);
                int indexCount = lineCount * 2;

                for(int i = 0; i < indexCount; i += 2)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.Vertex(pt0);
                    GL.Vertex(pt1);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if((indices.Length & 1) != 0)
            {
                Debug.LogWarning("indices.Length is uneven number");

                return;
            }

            Material material = CustomWireMaterial;

            if(material && material.SetPass(0))
            {
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.LINES);
                GL.Color(c);

                for(int i = 0; i < indices.Length; i += 2)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];
                    GL.Vertex(pt0);
                    GL.Vertex(pt1);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, float thickness, Color[] colors)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(indices.Length != colors.Length * 2)
            {
                Debug.LogWarning("indices.Length != colors.Length * 2");

                return;
            }

            Material material = MaterialUtility.ZTestGenericLine;

            if(material == null)
            {
                return;
            }

            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.InitGenericLineMaterial(material);

            if(material.SetPass(0))
            {
                thickness *= EditorGUIUtility.pixelsPerPoint;
                thickness *= 100;

                Vector3 thickness0 = new Vector3(thickness, -1, 0);
                Vector3 thickness1 = new Vector3(thickness, 1, 0);
                Vector3 thickness2 = new Vector3(thickness, 1, 0);
                Vector3 thickness3 = new Vector3(thickness, -1, 0);

                GL.PushMatrix();
                GL.MultMatrix(matrix);

                GL.Begin(GL.QUADS);

                for(int i = 0, c = 0; i < indices.Length; i += 2, c++)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];

                    GL.Color(colors[c]);

                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness0);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness1);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness2);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness3);
                    GL.Vertex(pt0);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawLines(Vector3[] vertices, float thickness, Color color)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = MaterialUtility.ZTestGenericLine;

            if(material == null)
            {
                return;
            }

            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.InitGenericLineMaterial(material);

            if(material.SetPass(0))
            {
                thickness *= EditorGUIUtility.pixelsPerPoint;
                thickness *= 100;

                Vector3 thickness0 = new Vector3(thickness, -1, 0);
                Vector3 thickness1 = new Vector3(thickness, 1, 0);
                Vector3 thickness2 = new Vector3(thickness, 1, 0);
                Vector3 thickness3 = new Vector3(thickness, -1, 0);

                GL.Begin(GL.QUADS);
                GL.Color(color);

                for(int i = 0; i < vertices.Length; i += 2)
                {
                    Vector3 pt0 = vertices[i + 0];
                    Vector3 pt1 = vertices[i + 1];

                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness0);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness1);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness2);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness3);
                    GL.Vertex(pt0);
                }

                GL.End();
            }
        }

        private const float K_HANDLE_SIZE = 80.0f;
        private const float K_MIN_SCREEN_DIST = 0.0001f;

        private static void GetHandleSizes(Camera cam, Matrix4x4 matrix, Vector3[] positions, float[] sizes, float thickness)
        {
            Matrix4x4 invMatrix  = matrix.inverse;
            Transform tr         = cam.transform;
            Vector3   camPos     = tr.position;
            Vector3   invCamPos  = invMatrix.MultiplyPoint(camPos);
            Vector3   forward    = tr.TransformDirection(MathConstants.forwardVector3);
            Vector3   right      = tr.TransformDirection(MathConstants.rightVector3);
            Vector3   invForward = invMatrix.MultiplyVector(forward);
            Matrix4x4 clipmatrix = cam.projectionMatrix * cam.worldToCameraMatrix;

            float width = cam.pixelRect.width * 0.5f;

            float m00 = clipmatrix.m00, m30 = clipmatrix.m30;
            float m01 = clipmatrix.m01, m31 = clipmatrix.m31;
            float m02 = clipmatrix.m02, m32 = clipmatrix.m32;
            float m03 = clipmatrix.m03, m33 = clipmatrix.m33;

            float pixelsPerPoint = thickness * K_HANDLE_SIZE * EditorGUIUtility.pixelsPerPoint;

            for(int i = 0; i < positions.Length; i++)
            {
                float   distance = Vector3.Dot(positions[i] - invCamPos, invForward);
                Vector3 p1       = camPos + forward * distance;
                Vector3 p2       = p1 + right;

                float w1 = m30 * p1.x + m31 * p1.y + m32 * p1.z + m33;
                float w2 = m30 * p2.x + m31 * p2.y + m32 * p2.z + m33;

                float invW1 = Mathf.Abs(w1) <= 1.0e-7f ? 0 : 1.0f / w1;
                float invW2 = Mathf.Abs(w2) <= 1.0e-7f ? 0 : 1.0f / w2;

                float p1X = m00 * p1.x + m01 * p1.y + m02 * p1.z + m03;
                float p2X = m00 * p2.x + m01 * p2.y + m02 * p2.z + m03;

                float x = (p1X * invW1 - p2X * invW2) * width;

                sizes[i] = pixelsPerPoint / Mathf.Max(K_MIN_SCREEN_DIST, Mathf.Sqrt(x * x));
            }
        }

        public static void DrawLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, float thickness, Color color) //1
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = MaterialUtility.ZTestGenericLine;

            if(!material)
            {
                return;
            }

            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.InitGenericLineMaterial(material);

            if(material.SetPass(0))
            {
                thickness *= EditorGUIUtility.pixelsPerPoint;
                thickness *= 100;

                Vector3 thickness0 = new Vector3(thickness, -1, 0);
                Vector3 thickness1 = new Vector3(thickness, +1, 0);
                Vector3 thickness2 = new Vector3(thickness, +1, 0);
                Vector3 thickness3 = new Vector3(thickness, -1, 0);

                GL.PushMatrix();

                GL.Begin(GL.QUADS);
                GL.Color(color);

                for(int i = 0; i < indices.Length; i += 2)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = matrix.MultiplyPoint(vertices[i0]);
                    Vector3 pt1 = matrix.MultiplyPoint(vertices[i1]);

                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness0);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness1);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness2);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness3);
                    GL.Vertex(pt0);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawLines(Vector3[] vertices, int[] indices, float thickness, Color color)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Material material = MaterialUtility.ZTestGenericLine;

            if(material == null)
            {
                return;
            }

            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.InitGenericLineMaterial(material);

            if(material.SetPass(0))
            {
                thickness *= EditorGUIUtility.pixelsPerPoint;
                thickness *= 100;

                Vector3 thickness0 = new Vector3(thickness, -1, 0);
                Vector3 thickness1 = new Vector3(thickness, 1, 0);
                Vector3 thickness2 = new Vector3(thickness, 1, 0);
                Vector3 thickness3 = new Vector3(thickness, -1, 0);

                GL.Begin(GL.QUADS);
                GL.Color(color);

                for(int i = 0; i < indices.Length; i += 2)
                {
                    int     i0  = indices[i + 0];
                    int     i1  = indices[i + 1];
                    Vector3 pt0 = vertices[i0];
                    Vector3 pt1 = vertices[i1];

                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness0);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt0);
                    GL.MultiTexCoord(1, thickness1);
                    GL.Vertex(pt1);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness2);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(0, pt1);
                    GL.MultiTexCoord(1, thickness3);
                    GL.Vertex(pt0);
                }

                GL.End();
            }
        }

        public static void DrawTriangles(Matrix4x4 matrix, Vector3[] vertices, int[] triangleIndices, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(triangleIndices.Length % 3 != 0)
            {
                Debug.LogWarning("triangleIndices.Length is not dividable by 3");

                return;
            }

            Material material = MaterialUtility.ColoredPolygonMaterial;

            if(material && material.SetPass(0))
            {
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.TRIANGLES);
                GL.Color(c);

                for(int i = 0; i < triangleIndices.Length; i++)
                {
                    int     i0  = triangleIndices[i];
                    Vector3 pt0 = vertices[i0];
                    GL.Vertex(pt0);
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawPolygon(Vector3[] vertices, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(vertices.Length < 3)
            {
                Debug.LogWarning("vertices.Length is smaller than 3");

                return;
            }

            Material material = MaterialUtility.ColoredPolygonMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.TRIANGLES);
                GL.Color(c);
                Vector3 v0 = vertices[0];
                Vector3 v1 = vertices[1];

                for(int i = 2; i < vertices.Length; i++)
                {
                    Vector3 v2 = vertices[i];
                    GL.Vertex(v0);
                    GL.Vertex(v1);
                    GL.Vertex(v2);
                    v1 = v2;
                }

                GL.End();
            }
        }

        public static void DrawPolygon(Matrix4x4 matrix, Vector3[] vertices, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(vertices.Length < 3)
            {
                Debug.LogWarning("vertices.Length is smaller than 3");

                return;
            }

            Material material = MaterialUtility.ColoredPolygonMaterial;

            if(material && material.SetPass(0))
            {
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.TRIANGLES);
                GL.Color(c);
                Vector3 v0 = vertices[0];
                Vector3 v1 = vertices[1];

                for(int i = 2; i < vertices.Length; i++)
                {
                    Vector3 v2 = vertices[i];
                    GL.Vertex(v0);
                    GL.Vertex(v1);
                    GL.Vertex(v2);
                    v1 = v2;
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawDottedWirePolygon(Vector3[] vertices, Color c, float sreenSpaceSize = 4.0f)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(vertices.Length < 3)
            {
                Debug.LogWarning("vertices.Length is smaller than 3");

                return;
            }

            Material material = CustomWireDottedMaterial;

            if(material && material.SetPass(0))
            {
                float dashSize = sreenSpaceSize * EditorGUIUtility.pixelsPerPoint;
                GL.Begin(GL.LINES);
                GL.Color(c);
                Vector3 pt0 = vertices[vertices.Length - 1];

                for(int i = 0; i < vertices.Length; i++)
                {
                    Vector3 pt1 = vertices[i];
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt0);
                    GL.MultiTexCoord(1, pt1);
                    GL.MultiTexCoord2(2, dashSize, 0);
                    GL.Vertex(pt1);
                    pt0 = pt1;
                }

                GL.End();
            }
        }

        public static void DrawWirePolygon(Vector3[] vertices, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(vertices.Length < 3)
            {
                Debug.LogWarning("vertices.Length is smaller than 3");

                return;
            }

            Material material = MaterialUtility.ColoredPolygonMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.LINES);
                GL.Color(c);
                Vector3 v0 = vertices[vertices.Length - 1];

                for(int i = 0; i < vertices.Length; i++)
                {
                    Vector3 v1 = vertices[i];
                    GL.Vertex(v0);
                    GL.Vertex(v1);
                    v0 = v1;
                }

                GL.End();
            }
        }

        public static void DrawWirePolygon(Matrix4x4 matrix, Vector3[] vertices, Color c)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(vertices.Length < 3)
            {
                Debug.LogWarning("vertices.Length is smaller than 3");

                return;
            }

            Material material = MaterialUtility.ColoredPolygonMaterial;

            if(material && material.SetPass(0))
            {
                GL.PushMatrix();
                GL.MultMatrix(matrix);
                GL.Begin(GL.LINES);
                GL.Color(c);
                Vector3 v0 = vertices[vertices.Length - 1];

                for(int i = 0; i < vertices.Length; i++)
                {
                    Vector3 v1 = vertices[i];
                    GL.Vertex(v0);
                    GL.Vertex(v1);
                    v0 = v1;
                }

                GL.End();
                GL.PopMatrix();
            }
        }

        public static void DrawDottedLines(Matrix4x4 matrix, Vector3[] vertices, int[] indices, int lineCount, Color c, float dotSize = 4.0f)
        {
            Handles.matrix = matrix;
            Handles.color  = c;
            Handles.DrawDottedLines(vertices, indices, dotSize);
        }

        public static void DrawWorldText(Camera camera, Vector3 center, GUIContent content, GUIStyle style = null)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(center);

            if(screenPoint.z < 0)
            {
                return;
            }

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            Vector2 pos2D = HandleUtility.WorldToGUIPoint(center);

            if(style == null)
            {
                style = CsgguiStyleUtility.SceneTextLabel;
            }

            Vector2 calcSize = style.CalcSize(content);
            Handles.BeginGUI();

            GUI.Label(new Rect(pos2D.x - calcSize.x * 0.5f, pos2D.y - calcSize.y * 0.5f, calcSize.x, calcSize.y), content, style);

            Handles.EndGUI();

            Handles.matrix = prevMatrix;
        }

        public static void DrawWorldText(Camera camera, Vector3 center, string text, GUIStyle style = null)
        {
            DrawWorldText(camera, center, new GUIContent(text), style);
        }

        public static void DrawScreenText(Vector2 center, GUIContent content, GUIStyle style = null)
        {
            Handles.BeginGUI();

            if(style == null)
            {
                style = CsgguiStyleUtility.SceneTextLabel;
            }

            Vector2 calcSize = style.CalcSize(content);

            GUI.Label(new Rect(center.x - calcSize.x * 0.5f, center.y - calcSize.y * 0.5f, calcSize.x + 4, calcSize.y + 4), content, style);

            Handles.EndGUI();
        }

        public static void DrawScreenText(Vector2 center, string text, GUIStyle style = null)
        {
            DrawScreenText(center, new GUIContent(text), style);
        }

        //HoverTextDistance * 2
        public static void DrawScreenText(Camera camera, Vector3 center, float distance, string text, GUIStyle style = null)
        {
            Vector3 screenPoint = camera.WorldToScreenPoint(center);

            if(screenPoint.z < 0)
            {
                return;
            }

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            Vector2 textCenter2D = HandleUtility.WorldToGUIPoint(center);
            textCenter2D.y += distance;

            Ray     textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
            Vector3 textCenter    = textCenterRay.origin + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

            Handles.color = Color.black;
            Handles.DrawLine(center, textCenter);
            DrawScreenText(textCenter2D, text);

            Handles.matrix = prevMatrix;
        }

        private const float HATCH_DISTANCE = 0.06f;

        private static int GetHatchCountForRadius(Vector3 center, float radius)
        {
            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            float handleSize    = HandleUtility.GetHandleSize(center);
            int   hatchCount    = 0;
            float circumference = (float)(2 * Math.PI * radius);

            if(circumference > handleSize * 2.0f)
            {
                int possibleStepsOfHatchDistance = Mathf.FloorToInt(circumference / (handleSize * HATCH_DISTANCE));

                if(possibleStepsOfHatchDistance > 0.0f)
                {
                    hatchCount = possibleStepsOfHatchDistance;

                    if(hatchCount >= 360)
                    {
                        hatchCount = 360;
                    }
                    else if(hatchCount >= 72)
                    {
                        hatchCount = 72;
                    }
                    else if(hatchCount >= 24)
                    {
                        hatchCount = 24;
                    }
                    else if(hatchCount >= 8)
                    {
                        hatchCount = 8;
                    }
                    else if(hatchCount >= 4)
                    {
                        hatchCount = 4;
                    }
                    else
                    {
                        hatchCount = 0;
                    }
                }
            }

            Handles.matrix = prevMatrix;

            return hatchCount;
        }

        public static void DrawRotateCirclePie(Vector3 center, Vector3 normal, Vector3 tangent, float radius, float startAngle, float endAngle, Color color)
        {
            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            Color innerColor = color;
            innerColor.a *= 0.75f;

            int     hatchCount    = GetHatchCountForRadius(center, radius);
            Vector3 innerDistance = tangent * radius;
            Vector3 outerDistance = tangent * (radius * 1.25f);
            float   deltaAngle    = endAngle - startAngle;

            if(deltaAngle > 360)
            {
                deltaAngle = 360;
            }

            if(deltaAngle < -360)
            {
                deltaAngle = -360;
            }

            endAngle = startAngle + deltaAngle;

            Vector3 startPointO = center + Quaternion.AngleAxis(startAngle, normal) * outerDistance;
            Vector3 endPointO   = center + Quaternion.AngleAxis(endAngle, normal) * outerDistance;

            Vector3 startPointI = center + Quaternion.AngleAxis(startAngle, normal) * innerDistance;
            Vector3 endPointI   = center + Quaternion.AngleAxis(endAngle, normal) * innerDistance;

            Handles.color = color;
            Handles.DrawLine(startPointI, startPointO);
            Handles.DrawLine(endPointI, endPointO);

            Material material = CustomSurfaceNoDepthMaterial;

            if(material && material.SetPass(0))
            {
                GL.Begin(GL.QUADS);
                GL.Color(innerColor);

                float step;

                if(hatchCount > 0)
                {
                    if(hatchCount < 2)
                    {
                        step = 360.0f;
                    }
                    else
                    {
                        step = 360.0f / hatchCount;
                    }
                }
                else
                {
                    step = 15;
                }

                if(deltaAngle < 0)
                {
                    Vector3 vectorI0 = center + Quaternion.AngleAxis(startAngle, normal) * innerDistance;
                    Vector3 vectorO0 = center + Quaternion.AngleAxis(startAngle, normal) * outerDistance;

                    float steps = Mathf.Floor(-deltaAngle / step * 4);

                    for(float a = 1; a <= steps; a++)
                    {
                        Vector3 vectorI1 = center + Quaternion.AngleAxis(startAngle + a * (-step * 0.25f), normal) * innerDistance;
                        Vector3 vectorO1 = center + Quaternion.AngleAxis(startAngle + a * (-step * 0.25f), normal) * outerDistance;

                        GL.Vertex(vectorO1);
                        GL.Vertex(vectorO0);
                        GL.Vertex(vectorI0);
                        GL.Vertex(vectorI1);

                        vectorI0 = vectorI1;
                        vectorO0 = vectorO1;
                    }

                    if(steps * step + deltaAngle > MathConstants.EqualityEpsilon)
                    {
                        GL.Vertex(vectorO0);
                        GL.Vertex(endPointO);
                        GL.Vertex(endPointI);
                        GL.Vertex(vectorI0);
                    }
                }
                else
                {
                    Vector3 vectorO0 = center + Quaternion.AngleAxis(startAngle, normal) * outerDistance;
                    Vector3 vectorI0 = center + Quaternion.AngleAxis(startAngle, normal) * innerDistance;
                    float   steps    = Mathf.Floor(deltaAngle / step * 4);

                    for(float a = 1; a <= steps; a++)
                    {
                        Vector3 vectorI1 = center + Quaternion.AngleAxis(startAngle + a * (step * 0.25f), normal) * innerDistance;
                        Vector3 vectorO1 = center + Quaternion.AngleAxis(startAngle + a * (step * 0.25f), normal) * outerDistance;

                        GL.Vertex(vectorO0);
                        GL.Vertex(vectorO1);
                        GL.Vertex(vectorI1);
                        GL.Vertex(vectorI0);

                        vectorO0 = vectorO1;
                        vectorI0 = vectorI1;
                    }

                    if(steps * step + deltaAngle > MathConstants.EqualityEpsilon)
                    {
                        Vector3 deltaI = center + Quaternion.AngleAxis(endAngle, normal) * innerDistance;
                        Vector3 deltaO = center + Quaternion.AngleAxis(endAngle, normal) * outerDistance;
                        GL.Vertex(vectorO0);
                        GL.Vertex(deltaO);
                        GL.Vertex(deltaI);
                        GL.Vertex(vectorI0);
                    }
                }

                GL.End();
            }

            Handles.matrix = originalMatrix;
        }

        public static Vector3 HandlePivot(Camera camera, Vector3 center, Quaternion rotation, Color color, bool active)
        {
            if(Event.current.type == EventType.Repaint)
            {
                float handleSize = CSGHandleUtility.GetHandleSize(center);

                Color originalColor = Handles.color;

                Handles.color = color;
                DrawFilledCameraAlignedCircle(camera, center, handleSize * GUIConstants.HANDLE_SCALE * 1.00f);

                DrawCameraAlignedCircle(camera, center, handleSize * GUIConstants.HANDLE_SCALE * 4.0f, Color.white, Color.black);

                Handles.color = originalColor;
            }

            return !active ? center : CSGHandles.PositionHandle(camera, center, rotation, CSGSettings.ActiveSnappingMode);
        }

        public static Vector3 HandlePosition(Camera camera, Vector3 center, Quaternion rotation, Vector3[] snapVertices = null, CSGHandles.InitFunction initFunction = null, CSGHandles.InitFunction shutdownFunction = null)
        {
            return CSGHandles.PositionHandle(camera, center, rotation, CSGSettings.ActiveSnappingMode, snapVertices, initFunction, shutdownFunction);
        }

        public static Vector3 HandleScale(Vector3 scale, Vector3 center, Quaternion rotation, CSGHandles.InitFunction initFunction = null, CSGHandles.InitFunction shutdownFunction = null)
        {
            return CSGHandles.ScaleHandle(scale, center, rotation, CSGSettings.ScaleSnapping, initFunction, shutdownFunction);
        }

        public static Quaternion HandleRotation(Camera camera, Vector3 center, Quaternion rotation, CSGHandles.InitFunction initFunction = null, CSGHandles.InitFunction shutdownFunction = null)
        {
            return CSGHandles.DoRotationHandle(camera, rotation, center, CSGSettings.RotationSnapping, initFunction, shutdownFunction);
        }

        public static void DrawProjectedPivot(Camera camera, Vector3 center, Color color)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            Color originalColor = Handles.color;

            Handles.color = color;
            DrawFilledCameraAlignedCircle(camera, center, CSGHandleUtility.GetHandleSize(center) * GUIConstants.HANDLE_SCALE * 1.00f);

            Handles.color = originalColor;
        }

        public static void DrawCircle(Vector3 center, Vector3 normal, Vector3 tangent, float radius, Color color)
        {
            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            Vector3 distance = tangent * radius;

            Handles.color = color;
            Vector3 vector0 = center + Quaternion.AngleAxis(-15, normal) * distance;

            for(int a = 0; a < 360; a += 15)
            {
                Vector3 vector1 = center + Quaternion.AngleAxis(a, normal) * distance;

                Handles.DrawAAPolyLine(vector0, vector1);
                vector0 = vector1;
            }

            Handles.matrix = originalMatrix;
        }

        public static void DrawRotateCircle(Camera camera, Vector3 center, Vector3 normal, Vector3 tangent, float radius, float originalAngle, float startAngle, float endAngle, Color color, string axisName = null, bool centerText = false)
        {
            Matrix4x4 originalMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            Color colorOuter = color;
            colorOuter.a *= 0.8f;
            Color innerColor = color;
            innerColor.a *= 0.3f;

            float   angleOffset   = startAngle - originalAngle;
            int     hatchCount    = GetHatchCountForRadius(center, radius);
            Vector3 innerDistance = tangent * radius;
            Vector3 outerDistance = tangent * (radius * 1.25f);

            if(hatchCount > 0.0f)
            {
                float handleSize = HandleUtility.GetHandleSize(center);
                float maxSize    = radius * 0.3f;

                Vector3[] tickDistance  = { outerDistance + tangent * Mathf.Min(Mathf.Min(handleSize * 0.05f, handleSize * radius * 0.075f), maxSize), outerDistance + tangent * Mathf.Min(Mathf.Min(handleSize * 0.1f, handleSize * radius * 0.15f), maxSize), outerDistance + tangent * Mathf.Min(Mathf.Min(handleSize * 0.5f, handleSize * radius * 0.2f), maxSize), outerDistance + tangent * Mathf.Min(Mathf.Min(handleSize * 0.6f, handleSize * radius * 0.3f), maxSize) };
                Vector3   labelDistance = outerDistance + tangent * Mathf.Min(Mathf.Min(handleSize, handleSize * radius * 0.5f), maxSize);

                int step = 360 / hatchCount;

                Color halfColorOuter = colorOuter;
                halfColorOuter.a *= 0.5f;

                List<Vector3> vertices = new List<Vector3>(360 / step * 2 * 4);

                Handles.matrix = MathConstants.identityMatrix;
                Vector3 vectorO0 = center + Quaternion.AngleAxis(-step + angleOffset, normal) * outerDistance;
                Vector3 vectorI0 = center + Quaternion.AngleAxis(-step + angleOffset, normal) * innerDistance;

                for(int a = 0; a < 360; a += step)
                {
                    Vector3 vectorO1 = center + Quaternion.AngleAxis(a + angleOffset, normal) * outerDistance;
                    Vector3 vectorI1 = center + Quaternion.AngleAxis(a + angleOffset, normal) * innerDistance;
                    Vector3 vectorO2 = center + Quaternion.AngleAxis(a + angleOffset - step * 0.5f, normal) * outerDistance;
                    Vector3 vectorI2 = center + Quaternion.AngleAxis(a + angleOffset - step * 0.5f, normal) * innerDistance;

                    int tickType;

                    if(a % 90 == 0)
                    {
                        tickType = 3;
                    }
                    else if(a % 45 == 0)
                    {
                        tickType = 2;
                    }
                    else if(a % 5 == 0)
                    {
                        tickType = 1;
                    }
                    else
                    {
                        tickType = 0;
                    }

                    Vector3 vectora = center + Quaternion.AngleAxis(a + angleOffset, normal) * tickDistance[tickType];

                    Handles.color = color;
                    Handles.DrawAAPolyLine(vectora, vectorO1);
                    DrawLine(vectorO0, vectorO2, GUIConstants.OLD_THIN_LINE_SCALE, colorOuter);
                    DrawLine(vectorO2, vectorO1, GUIConstants.OLD_THIN_LINE_SCALE, colorOuter);

                    DrawLine(vectorI0, vectorI2, GUIConstants.OLD_THIN_LINE_SCALE, colorOuter);
                    DrawLine(vectorI2, vectorI1, GUIConstants.OLD_THIN_LINE_SCALE, colorOuter);

                    DrawLine(vectora, vectorO1, GUIConstants.OLD_THIN_LINE_SCALE, color);

                    vertices.Add(vectorI2);
                    vertices.Add(vectorI0);
                    vertices.Add(vectorO0);
                    vertices.Add(vectorO2);

                    vertices.Add(vectorI1);
                    vertices.Add(vectorI2);
                    vertices.Add(vectorO2);
                    vertices.Add(vectorO1);

                    if(tickType >= 2)
                    {
                        if((tickType == 3 && hatchCount > 24) || hatchCount > 72)
                        {
                            DrawWorldText(camera, center + Quaternion.AngleAxis(a + angleOffset, normal) * labelDistance, Units.ToAngleString(Mathf.RoundToInt(a)));
                        }
                    }

                    vectorO0 = vectorO1;
                    vectorI0 = vectorI1;
                }

                Vector3 startI1 = center + Quaternion.AngleAxis(angleOffset, normal) * innerDistance;
                Vector3 startO1 = center + Quaternion.AngleAxis(angleOffset, normal) * outerDistance;

                Handles.color = colorOuter;
                Handles.DrawAAPolyLine(startI1, startO1);

                float snapStep = Mathf.Max(3.0f, CSGSettings.SnapRotation);

                for(float a = snapStep; a <= 180; a += snapStep)
                {
                    Vector3 vectorI1 = center + Quaternion.AngleAxis(angleOffset + a, normal) * innerDistance;
                    Vector3 vectorO1 = center + Quaternion.AngleAxis(angleOffset + a, normal) * outerDistance;

                    Vector3 vectorI2 = center + Quaternion.AngleAxis(angleOffset - a, normal) * innerDistance;
                    Vector3 vectorO2 = center + Quaternion.AngleAxis(angleOffset - a, normal) * outerDistance;

                    DrawLine(vectorI1, vectorO1, GUIConstants.OLD_THIN_LINE_SCALE, colorOuter);
                    DrawLine(vectorI2, vectorO2, GUIConstants.OLD_THIN_LINE_SCALE, colorOuter);
                }

                if(centerText)
                {
                    if(!string.IsNullOrEmpty(axisName))
                    {
                        DrawWorldText(camera, center, string.Format(CultureInfo.InvariantCulture, "{0}: Δ{1}", axisName, Units.ToRoundedAngleString(endAngle - startAngle)));
                    }
                    else
                    {
                        DrawWorldText(camera, center, string.Format(CultureInfo.InvariantCulture, "Δ{0}", Units.ToRoundedAngleString(endAngle - startAngle)));
                    }
                }
                else
                {
                    DrawWorldText(camera, center + Quaternion.AngleAxis(startAngle, normal) * labelDistance, Units.ToAngleString(originalAngle));

                    if(!string.IsNullOrEmpty(axisName))
                    {
                        DrawWorldText(camera, center + Quaternion.AngleAxis(endAngle, normal) * (innerDistance * 0.75f), string.Format(CultureInfo.InvariantCulture, "{0}: Δ{1}", axisName, Units.ToRoundedAngleString(endAngle - startAngle)));
                    }
                    else
                    {
                        DrawWorldText(camera, center + Quaternion.AngleAxis(endAngle, normal) * (innerDistance * 0.75f), string.Format(CultureInfo.InvariantCulture, "Δ{0}", Units.ToRoundedAngleString(endAngle - startAngle)));
                    }
                }

                Material material = CustomSurfaceNoDepthMaterial;

                if(material && material.SetPass(0))
                {
                    GL.Begin(GL.QUADS);
                    GL.Color(innerColor);

                    for(int i = 0; i < vertices.Count; i += 8)
                    {
                        GL.Vertex(vertices[i + 0]);
                        GL.Vertex(vertices[i + 1]);
                        GL.Vertex(vertices[i + 2]);
                        GL.Vertex(vertices[i + 3]);

                        GL.Vertex(vertices[i + 4]);
                        GL.Vertex(vertices[i + 5]);
                        GL.Vertex(vertices[i + 6]);
                        GL.Vertex(vertices[i + 7]);
                    }

                    GL.End();
                }
            }
            else
            {
                Handles.matrix = MathConstants.identityMatrix;
                Handles.color  = colorOuter;
                Vector3 vector0 = center + Quaternion.AngleAxis(-15 + angleOffset, normal) * outerDistance;

                for(int a = 0; a < 360; a += 15)
                {
                    Vector3 vector1 = center + Quaternion.AngleAxis(a + angleOffset, normal) * outerDistance;

                    Handles.DrawAAPolyLine(vector0, vector1);
                    vector0 = vector1;
                }
            }

            Handles.matrix = originalMatrix;
        }

        public static void DrawArrowCap(Vector3 position, Vector3 direction, float size)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(direction == MathConstants.zeroVector3)
            {
                return;
            }

            Handles.ConeHandleCap(-1, position + direction * size, Quaternion.LookRotation(direction), size * .2f, EventType.Repaint);
            Handles.DrawLine(position, position + direction * size * .9f);
        }

        public static void AddArrowCapControl(int controlID, Vector3 position, Vector3 direction, float size)
        {
            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            float lineDistance = HandleUtility.DistanceToLine(position, position + direction * size);
            float capDistance  = HandleUtility.DistanceToCircle(position + direction * size, size * .2f);
            HandleUtility.AddControl(controlID, lineDistance);
            HandleUtility.AddControl(controlID, capDistance);

            Handles.matrix = prevMatrix;
        }

        public static void DrawLength(Camera camera, string axisName, float handleSize, Matrix4x4 matrix, Vector3 sideNormal, Vector3 vertexA, Vector3 vertexB, Color color, GUIStyle style = null)
        {
            vertexA = matrix.MultiplyPoint(vertexA);
            vertexB = matrix.MultiplyPoint(vertexB);

            Vector3 normal = matrix.MultiplyVector(sideNormal);
            Vector3 delta  = vertexB - vertexA;
            float   length = delta.magnitude;
            delta /= length;

            Vector3 lineA3 = vertexA + delta * handleSize * 0.125f;
            Vector3 lineB3 = vertexB - delta * handleSize * 0.125f;

            Vector3 lineA4 = lineA3 - normal * handleSize * 0.0625f;
            Vector3 lineB4 = lineB3 - normal * handleSize * 0.0625f;

            Vector3 lineA5 = lineA3 + normal * handleSize * 0.0625f;
            Vector3 lineB5 = lineB3 + normal * handleSize * 0.0625f;

            Handles.color = color;
            Handles.DrawAAPolyLine(3.0f, lineA3, lineB3);
            Handles.DrawAAConvexPolygon(vertexA, lineA4, lineA5);
            Handles.DrawAAConvexPolygon(vertexB, lineB4, lineB5);

            Vector3 center = (vertexB + vertexA) * 0.5f;

            Vector3 screenPoint = camera.WorldToScreenPoint(center);

            if(screenPoint.z < 0)
            {
                return;
            }

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            Vector2    center2D = HandleUtility.WorldToGUIPoint(center);
            Vector2    offset   = HandleUtility.WorldToGUIPoint(center + normal);
            Vector2    offset2D = (offset - center2D).normalized;
            GUIContent content  = new GUIContent(axisName + Units.ToRoundedDistanceString(length));
            Handles.BeginGUI();

            if(style == null)
            {
                style = CsgguiStyleUtility.SceneTextLabel;
            }

            Vector2 calcSize = style.CalcSize(content);
            float   radius   = calcSize.magnitude * 0.5f + 4.0f;
            center2D += radius * offset2D;

            GUI.Label(new Rect(center2D.x - calcSize.x * 0.5f, center2D.y - calcSize.y * 0.5f, calcSize.x, calcSize.y), content, style);

            Handles.EndGUI();

            Handles.matrix = prevMatrix;
        }

        public static void DrawEdgeLength(Camera camera, string axisName, float handleSize, Vector3 normal, Vector3 vertexA, Vector3 vertexB, Color color, GUIStyle style = null)
        {
            Vector3 tangent = vertexB - vertexA;
            float   length  = tangent.magnitude;
            tangent /= length;

            float extrusion = Mathf.Min(2.0f, handleSize);

            Vector3 lineA1 = vertexA + normal * (extrusion * 0.5f);
            Vector3 lineB1 = vertexB + normal * (extrusion * 0.5f);

            Vector3 lineA2 = vertexA + normal * extrusion;
            Vector3 lineB2 = vertexB + normal * extrusion;

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            float handleSizeA = Mathf.Min(length / 3.0f, HandleUtility.GetHandleSize(vertexA) * 0.15f);
            float handleSizeB = Mathf.Min(length / 3.0f, HandleUtility.GetHandleSize(vertexB) * 0.15f);

            Handles.color = Color.black;
            Handles.DrawAAPolyLine(3.0f, vertexA, lineA2);
            Handles.DrawAAPolyLine(3.0f, vertexB, lineB2);

            Vector3 lineA3 = lineA1 + tangent * handleSizeA;
            Vector3 lineB3 = lineB1 - tangent * handleSizeB;

            Vector3 lineA4 = lineA3 - normal * (handleSizeA * 0.5f);
            Vector3 lineB4 = lineB3 - normal * (handleSizeB * 0.5f);

            Vector3 lineA5 = lineA3 + normal * (handleSizeA * 0.5f);
            Vector3 lineB5 = lineB3 + normal * (handleSizeB * 0.5f);

            Handles.DrawAAPolyLine(7.0f, lineA3, lineB3);

            Handles.color = color;
            Handles.DrawAAConvexPolygon(lineA1, lineA4, lineA5);
            Handles.DrawAAConvexPolygon(lineB1, lineB4, lineB5);

            Handles.color = Color.black;
            Handles.DrawAAPolyLine(2.0f, lineA1, lineA4, lineA5);
            Handles.DrawAAPolyLine(2.0f, lineB1, lineB4, lineB5);

            Handles.color = color;
            Handles.DrawAAPolyLine(5.0f, lineA3, lineB3);

            Vector3 center      = (lineB1 + lineA1) * 0.5f;
            Vector3 screenPoint = camera.WorldToScreenPoint(center);

            if(screenPoint.z < 0)
            {
                Handles.matrix = prevMatrix;

                return;
            }

            Vector2    center2D = HandleUtility.WorldToGUIPoint(center);
            Vector2    offset   = HandleUtility.WorldToGUIPoint(center + normal);
            Vector2    offset2D = (offset - center2D).normalized;
            GUIContent content  = new GUIContent(axisName + Units.ToRoundedDistanceString(length));
            Handles.BeginGUI();

            if(style == null)
            {
                style = CsgguiStyleUtility.SceneTextLabel;
            }

            Vector2 calcSize = style.CalcSize(content);
            float   radius   = calcSize.magnitude * 0.5f + 4.0f;
            center2D += radius * offset2D;

            GUI.Label(new Rect(center2D.x - calcSize.x * 0.5f, center2D.y - calcSize.y * 0.5f, calcSize.x, calcSize.y), content, style);

            Handles.EndGUI();

            Handles.matrix = prevMatrix;
        }

        private static void DrawBoundsEdgeLength(Camera camera, string axisName, float handleSize, Vector3 cameraPosition, Matrix4x4 matrix, Vector3[] vertices, int edgeIndex, Color color)
        {
            Vector3 vertexA = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][0]];
            Vector3 vertexB = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][1]];

            int sideIndexA = BoundsUtilities.aabbEdgeSides[edgeIndex][0];
            int sideIndexB = BoundsUtilities.aabbEdgeSides[edgeIndex][1];

            Vector3 sideNormalA = BoundsUtilities.aabbSideNormal[sideIndexA];
            Vector3 sideNormalB = BoundsUtilities.aabbSideNormal[sideIndexB];

            Vector3 edgePoint = GeometryUtility.ProjectPointOnInfiniteLine(cameraPosition, vertexA, (vertexB - vertexA).normalized);

            Vector3 orientation = matrix.MultiplyVector((edgePoint - cameraPosition).normalized);
            float   dotA        = Mathf.Abs(Vector3.Dot(orientation, matrix.MultiplyVector(sideNormalA)));
            float   dotB        = Mathf.Abs(Vector3.Dot(orientation, matrix.MultiplyVector(sideNormalB)));

            Vector3 sideNormal = dotA < dotB ? sideNormalA : sideNormalB;

            vertexA    = matrix.MultiplyPoint(vertexA);
            vertexB    = matrix.MultiplyPoint(vertexB);
            sideNormal = matrix.MultiplyVector(sideNormal).normalized;

            DrawEdgeLength(camera, axisName, handleSize, sideNormal, vertexA, vertexB, color);
        }

        private static void DrawBoundsEdgeLength(Camera camera, string axisName, float handleSize, Vector3 cameraPosition, Quaternion rotation, Vector3[] vertices, int edgeIndex, Color color)
        {
            Vector3 vertexA = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][0]];
            Vector3 vertexB = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][1]];

            int sideIndexA = BoundsUtilities.aabbEdgeSides[edgeIndex][0];
            int sideIndexB = BoundsUtilities.aabbEdgeSides[edgeIndex][1];

            Vector3 sideNormalA = BoundsUtilities.aabbSideNormal[sideIndexA];
            Vector3 sideNormalB = BoundsUtilities.aabbSideNormal[sideIndexB];

            Vector3 edgePoint = GeometryUtility.ProjectPointOnInfiniteLine(cameraPosition, vertexA, (vertexB - vertexA).normalized);

            Vector3 orientation = (edgePoint - cameraPosition).normalized;
            float   dotA        = Mathf.Abs(Vector3.Dot(orientation, rotation * sideNormalA));
            float   dotB        = Mathf.Abs(Vector3.Dot(orientation, rotation * sideNormalB));

            Vector3 sideNormal = dotA < dotB ? sideNormalA : sideNormalB;

            vertexA    = rotation * vertexA;
            vertexB    = rotation * vertexB;
            sideNormal = rotation * sideNormal;

            DrawEdgeLength(camera, axisName, handleSize, sideNormal, vertexA, vertexB, color);
        }

        private static void GetActiveEdges(Vector3 cameraPosition, Vector3 cameraDirection, bool ortho, Vector3[] vertices, ref int xAxis, ref int yAxis, ref int zAxis)
        {
            xAxis = -1;
            yAxis = -1;
            zAxis = -1;
            float prevXAxisDist = float.PositiveInfinity;
            float prevYAxisDist = float.PositiveInfinity;
            float prevZAxisDist = float.PositiveInfinity;

            bool[]     backfaced = new bool[6];     // [-X, +X, -Y, +Y, -Z, +Z]
            CSGPlane[] planes    = new CSGPlane[6]; // [-X, +X, -Y, +Y, -Z, +Z]

            for(int i = 0; i < BoundsUtilities.aabbSideNormal.Length; i++)
            {
                Vector3 vertex = vertices[BoundsUtilities.aabbSidePointIndices[i][0]];
                planes[i] = new CSGPlane(BoundsUtilities.aabbSideNormal[i], vertex);

                backfaced[i] = Vector3.Dot(planes[i].normal, cameraPosition - vertex) < 0;
            }

            bool disabledX = ortho && Mathf.Abs(Vector3.Dot(planes[0].normal, cameraDirection)) > 0.99f;
            bool disabledY = ortho && Mathf.Abs(Vector3.Dot(planes[2].normal, cameraDirection)) > 0.99f;
            bool disabledZ = ortho && Mathf.Abs(Vector3.Dot(planes[4].normal, cameraDirection)) > 0.99f;

            for(int i = 0; i < 4; i++)
            {
                int edgeIndex = BoundsUtilities.aabbxAxi[i];
                int side1     = BoundsUtilities.aabbEdgeSides[edgeIndex][0];
                int side2     = BoundsUtilities.aabbEdgeSides[edgeIndex][1];

                if(backfaced[side1] != backfaced[side2])
                {
                    Vector3 a = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][0]];
                    Vector3 b = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][1]];

                    if((a - b).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
                    {
                        Vector3 c    = (a + b) * 0.5f;
                        float   dist = cameraPosition.x - c.x;

                        if(xAxis == -1 || dist < prevXAxisDist)
                        {
                            xAxis         = -1;
                            prevXAxisDist = dist;
                        }

                        if(xAxis == -1)
                        {
                            xAxis = edgeIndex;
                        }
                    }
                }

                edgeIndex = BoundsUtilities.aabbzAxi[i];
                side1     = BoundsUtilities.aabbEdgeSides[edgeIndex][0];
                side2     = BoundsUtilities.aabbEdgeSides[edgeIndex][1];

                if(backfaced[side1] != backfaced[side2])
                {
                    Vector3 a = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][0]];
                    Vector3 b = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][1]];

                    if((a - b).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
                    {
                        Vector3 c    = (a + b) * 0.5f;
                        float   dist = cameraPosition.z - c.z;

                        if(zAxis == -1 || dist < prevZAxisDist)
                        {
                            zAxis         = -1;
                            prevZAxisDist = dist;
                        }

                        if(zAxis == -1)
                        {
                            zAxis = edgeIndex;
                        }
                    }
                }
            }

            if(disabledX)
            {
                xAxis = -1;
            }

            if(disabledZ)
            {
                zAxis = -1;
            }

            if(zAxis == -1 && xAxis == -1)
            {
                for(int i = 0; i < 4; i++)
                {
                    int edgeIndex = BoundsUtilities.aabbyAxi[3 - i];
                    int side1     = BoundsUtilities.aabbEdgeSides[edgeIndex][0];
                    int side2     = BoundsUtilities.aabbEdgeSides[edgeIndex][1];

                    if(backfaced[side1] != backfaced[side2])
                    {
                        Vector3 a = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][0]];
                        Vector3 b = vertices[BoundsUtilities.aabbEdgeIndices[edgeIndex][1]];

                        if((a - b).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
                        {
                            Vector3 c    = (a + b) * 0.5f;
                            float   dist = cameraPosition.y - c.y;

                            if(yAxis == -1 || dist < prevYAxisDist)
                            {
                                yAxis         = -1;
                                prevYAxisDist = dist;
                            }

                            if(yAxis == -1)
                            {
                                yAxis = edgeIndex;
                            }
                        }
                    }
                }
            }
            else
            {
                int xA = xAxis == -1 ? -1 : BoundsUtilities.aabbEdgeIndices[xAxis][0];
                int xB = xAxis == -1 ? -1 : BoundsUtilities.aabbEdgeIndices[xAxis][1];

                int zA = zAxis == -1 ? -1 : BoundsUtilities.aabbEdgeIndices[zAxis][0];
                int zB = zAxis == -1 ? -1 : BoundsUtilities.aabbEdgeIndices[zAxis][1];

                for(int i = 0; i < 4; i++)
                {
                    int edgeIndex = BoundsUtilities.aabbyAxi[3 - i];
                    int side1     = BoundsUtilities.aabbEdgeSides[edgeIndex][0];
                    int side2     = BoundsUtilities.aabbEdgeSides[edgeIndex][1];

                    if(backfaced[side1] != backfaced[side2])
                    {
                        int aIndex  = BoundsUtilities.aabbEdgeIndices[edgeIndex][0];
                        int bIndex  = BoundsUtilities.aabbEdgeIndices[edgeIndex][1];
                        int xAIndex = xAxis == -1 ? -1 : aIndex;
                        int xBIndex = xAxis == -1 ? -1 : bIndex;
                        int zAIndex = zAxis == -1 ? -1 : aIndex;
                        int zBIndex = zAxis == -1 ? -1 : bIndex;

                        if(((xAIndex == xA || xAIndex == xB) && (zBIndex == zA || zBIndex == zB)) ||
                           ((xBIndex == xA || xBIndex == xB) && (zAIndex == zA || zAIndex == zB)))
                        {
                            Vector3 a = vertices[aIndex];
                            Vector3 b = vertices[bIndex];

                            if((a - b).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
                            {
                                Vector3 c    = (a + b) * 0.5f;
                                float   dist = cameraPosition.y - c.y;

                                if(yAxis == -1 || dist < prevYAxisDist)
                                {
                                    yAxis         = -1;
                                    prevYAxisDist = dist;
                                }

                                if(yAxis == -1)
                                {
                                    yAxis = edgeIndex;
                                }
                            }
                        }
                    }
                }

                if(yAxis == -1)
                {
                    for(int i = 0; i < 4; i++)
                    {
                        int edgeIndex = BoundsUtilities.aabbyAxi[3 - i];
                        int side1     = BoundsUtilities.aabbEdgeSides[edgeIndex][0];
                        int side2     = BoundsUtilities.aabbEdgeSides[edgeIndex][1];

                        if(backfaced[side1] != backfaced[side2])
                        {
                            int aIndex = BoundsUtilities.aabbEdgeIndices[edgeIndex][0];
                            int bIndex = BoundsUtilities.aabbEdgeIndices[edgeIndex][1];

                            if(aIndex == xA || aIndex == zA || bIndex == zB || bIndex == xB ||
                               aIndex == xB || aIndex == zB || bIndex == zA || bIndex == xA)
                            {
                                Vector3 a = vertices[aIndex];
                                Vector3 b = vertices[bIndex];

                                if((a - b).sqrMagnitude > MathConstants.EqualityEpsilonSqr)
                                {
                                    Vector3 c    = (a + b) * 0.5f;
                                    float   dist = cameraPosition.y - c.y;

                                    if(yAxis == -1 || dist < prevYAxisDist)
                                    {
                                        yAxis         = -1;
                                        prevYAxisDist = dist;
                                    }

                                    if(yAxis == -1)
                                    {
                                        yAxis = edgeIndex;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if(disabledY)
            {
                yAxis = -1;
            }
        }

        public static void RenderBoundsSizes(Matrix4x4 invMatrix, Matrix4x4 matrix, Camera camera, Vector3[] vertices, Color colorAxisX, Color colorAxisY, Color colorAxisZ, bool showAxisX, bool showAxisY, bool showAxisZ)
        {
            int xAxis = -1;
            int yAxis = -1;
            int zAxis = -1;

            bool    ortho           = camera.orthographic;
            Vector3 cameraPosition  = invMatrix.MultiplyPoint(camera.transform.position);
            Vector3 cameraDirection = invMatrix.MultiplyVector(camera.transform.forward);
            GetActiveEdges(cameraPosition, cameraDirection, ortho, vertices, ref xAxis, ref yAxis, ref zAxis);

            float handleSize = 0;

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            // we still check the handleSizes for axi that are not shown, to make things look more consistent when turning axi on/off
            if(xAxis >= 0)
            {
                handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.aabbEdgeIndices[xAxis][0]])));
                handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.aabbEdgeIndices[xAxis][1]])));
            }

            if(yAxis >= 0)
            {
                handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.aabbEdgeIndices[yAxis][0]])));
                handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.aabbEdgeIndices[yAxis][1]])));
            }

            if(zAxis >= 0)
            {
                handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.aabbEdgeIndices[zAxis][0]])));
                handleSize = Mathf.Max(handleSize, HandleUtility.GetHandleSize(matrix.MultiplyPoint(vertices[BoundsUtilities.aabbEdgeIndices[zAxis][1]])));
            }

            if(showAxisX && xAxis >= 0)
            {
                DrawBoundsEdgeLength(camera, "x: ", handleSize, cameraPosition, matrix, vertices, xAxis, colorAxisX);
            }

            if(showAxisY && yAxis >= 0)
            {
                DrawBoundsEdgeLength(camera, "y: ", handleSize, cameraPosition, matrix, vertices, yAxis, colorAxisY);
            }

            if(showAxisZ && zAxis >= 0)
            {
                DrawBoundsEdgeLength(camera, "z: ", handleSize, cameraPosition, matrix, vertices, zAxis, colorAxisZ);
            }

            Handles.matrix = prevMatrix;
        }

        public static void RenderBoundsSizes(Quaternion invRotation, Quaternion rotation, Camera camera, Vector3[] vertices, Color colorAxisX, Color colorAxisY, Color colorAxisZ, bool showAxisX, bool showAxisY, bool showAxisZ)
        {
            int xAxis = -1;
            int yAxis = -1;
            int zAxis = -1;

            bool    ortho           = camera.orthographic;
            Vector3 cameraPosition  = invRotation * camera.transform.position;
            Vector3 cameraDirection = invRotation * camera.transform.forward;
            GetActiveEdges(cameraPosition, cameraDirection, ortho, vertices, ref xAxis, ref yAxis, ref zAxis);

            float   closestDistance = float.PositiveInfinity;
            float   distance;
            Vector3 vertex;
            Vector3 closestPointToCamera = Vector3.zero;

            if(xAxis >= 0)
            {
                vertex   = rotation * vertices[BoundsUtilities.aabbEdgeIndices[xAxis][0]];
                distance = (vertex - camera.transform.position).magnitude;

                if(closestDistance == float.PositiveInfinity || distance < closestDistance)
                {
                    closestDistance      = distance;
                    closestPointToCamera = vertex;
                }

                vertex   = rotation * vertices[BoundsUtilities.aabbEdgeIndices[xAxis][1]];
                distance = (vertex - camera.transform.position).magnitude;

                if(closestDistance == float.PositiveInfinity || distance < closestDistance)
                {
                    closestDistance      = distance;
                    closestPointToCamera = vertex;
                }
            }

            if(yAxis >= 0)
            {
                vertex   = rotation * vertices[BoundsUtilities.aabbEdgeIndices[yAxis][0]];
                distance = (vertex - camera.transform.position).magnitude;

                if(closestDistance == float.PositiveInfinity || distance < closestDistance)
                {
                    closestDistance      = distance;
                    closestPointToCamera = vertex;
                }

                vertex   = rotation * vertices[BoundsUtilities.aabbEdgeIndices[yAxis][1]];
                distance = (vertex - camera.transform.position).magnitude;

                if(closestDistance == float.PositiveInfinity || distance < closestDistance)
                {
                    closestDistance      = distance;
                    closestPointToCamera = vertex;
                }
            }

            if(zAxis >= 0)
            {
                vertex   = rotation * vertices[BoundsUtilities.aabbEdgeIndices[zAxis][0]];
                distance = (vertex - camera.transform.position).magnitude;

                if(closestDistance == float.PositiveInfinity || distance < closestDistance)
                {
                    closestDistance      = distance;
                    closestPointToCamera = vertex;
                }

                vertex   = rotation * vertices[BoundsUtilities.aabbEdgeIndices[zAxis][1]];
                distance = (vertex - camera.transform.position).magnitude;

                if(closestDistance == float.PositiveInfinity || distance < closestDistance)
                {
                    closestDistance      = distance;
                    closestPointToCamera = vertex;
                }
            }

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = Matrix4x4.identity;

            float handleSize = HandleUtility.GetHandleSize(closestPointToCamera);

            if(showAxisX && xAxis >= 0)
            {
                DrawBoundsEdgeLength(camera, "x: ", handleSize, cameraPosition, rotation, vertices, xAxis, colorAxisX);
            }

            if(showAxisY && yAxis >= 0)
            {
                DrawBoundsEdgeLength(camera, "y: ", handleSize, cameraPosition, rotation, vertices, yAxis, colorAxisY);
            }

            if(showAxisZ && zAxis >= 0)
            {
                DrawBoundsEdgeLength(camera, "z: ", handleSize, cameraPosition, rotation, vertices, zAxis, colorAxisZ);
            }

            Handles.matrix = prevMatrix;
        }

        public static void DrawSelectionRectangle(Rect rect)
        {
            Matrix4x4 origMatrix = Handles.matrix;
            Handles.matrix = origMatrix;

            if(rect.width >= 0 || rect.height >= 0)
            {
                Handles.BeginGUI();
                CsgguiStyleUtility.InitStyles();
                CsgguiStyleUtility.SelectionRectStyle.Draw(rect, GUIContent.none, false, false, false, false);
                Handles.EndGUI();
            }

            Handles.matrix = origMatrix;
        }
    }
}

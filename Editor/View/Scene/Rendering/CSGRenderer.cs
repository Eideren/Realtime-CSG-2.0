using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal static class CSGRenderer
    {
        public const float VISIBLE_OUTER_LINE_DOTS = 2.0f;
        public const float VISIBLE_INNER_LINE_DOTS = 4.0f;

        public const float INVISIBLE_OUTER_LINE_DOTS = 2.0f;
        public const float INVISIBLE_INNER_LINE_DOTS = 4.0f;

        public const float INVALID_LINE_DOTS = 2.0f;

        public const float UNSELECTED_FACTOR = 0.65f;
        public const float INNER_FACTOR = 0.70f;
        public const float OCCLUDED_FACTOR = 0.80f;

        public static void DrawSelectedBrush(int brushNodeID, Matrix4x4 transformation, Color wireframeColor, float thickness = -1)
        {
            Color selectedInnerColor         = wireframeColor * INNER_FACTOR;
            Color selectedOuterOccludedColor = wireframeColor * OCCLUDED_FACTOR;
            Color selectedInnerOccludedColor = selectedInnerColor * OCCLUDED_FACTOR;

            selectedOuterOccludedColor.a *= 0.5f;
            selectedInnerOccludedColor.a *= 0.5f;

            DrawOutlines(brushNodeID, transformation, wireframeColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }

        public static void DrawSelectedBrushes(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, int[] brushNodeIDs, Matrix4x4[] transformations, Color wireframeColor, float thickness = -1)
        {
            Color selectedInnerColor         = wireframeColor * INNER_FACTOR;
            Color selectedOuterOccludedColor = wireframeColor * OCCLUDED_FACTOR;
            Color selectedInnerOccludedColor = selectedInnerColor * OCCLUDED_FACTOR;

            selectedOuterOccludedColor.a *= 0.5f;
            selectedInnerOccludedColor.a *= 0.5f;

            GeometryWireframe[] wireframes = BrushOutlineManager.GetBrushOutlines(brushNodeIDs);

            DrawOutlines(zTestLineMeshManager, noZTestLineMeshManager, wireframes, transformations, wireframeColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }

        public static void DrawSelectedBrushes(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, GeometryWireframe[] wireframes, Matrix4x4[] transformations, Color wireframeColor, float thickness = -1)
        {
            Color selectedInnerColor         = wireframeColor * INNER_FACTOR;
            Color selectedOuterOccludedColor = wireframeColor * OCCLUDED_FACTOR;
            Color selectedInnerOccludedColor = selectedInnerColor * OCCLUDED_FACTOR;

            DrawOutlines(zTestLineMeshManager, noZTestLineMeshManager, wireframes, transformations, wireframeColor, selectedOuterOccludedColor, selectedInnerColor, selectedInnerOccludedColor, thickness);
        }

        public static void DrawSelectedBrush(int brushNodeID, Shape shape, Matrix4x4 transformation, Color wireframeColor, int surfaceIndex, bool selectAllSurfaces, float thickness = -1)
        {
            if(selectAllSurfaces)
            {
                Color selectedOuterOccludedColor = wireframeColor * OCCLUDED_FACTOR;
                Color selectedInnerOccludedColor = wireframeColor * OCCLUDED_FACTOR;

                selectedOuterOccludedColor.a *= 0.5f;
                selectedInnerOccludedColor.a *= 0.5f;

                DrawOutlines(brushNodeID, transformation, wireframeColor, selectedOuterOccludedColor, wireframeColor, selectedInnerOccludedColor, thickness);
            }
            else
            {
                Color unselectedOuterColor         = wireframeColor * UNSELECTED_FACTOR;
                Color unselectedInnerColor         = wireframeColor * (UNSELECTED_FACTOR * INNER_FACTOR);
                Color selectedInnerColor           = wireframeColor * INNER_FACTOR;
                Color unselectedOuterOccludedColor = unselectedOuterColor * OCCLUDED_FACTOR;
                Color unselectedInnerOccludedColor = unselectedInnerColor * OCCLUDED_FACTOR;

                unselectedOuterOccludedColor.a *= 0.5f;
                unselectedInnerOccludedColor.a *= 0.5f;

                if(surfaceIndex >= 0 && surfaceIndex < shape.surfaces.Length)
                {
                    DrawSurfaceOutlines(brushNodeID, shape, transformation, surfaceIndex, wireframeColor, selectedInnerColor);
                }

                DrawOutlines(brushNodeID, transformation, unselectedOuterColor, unselectedOuterOccludedColor, unselectedInnerColor, unselectedInnerOccludedColor, thickness);
            }
        }

        public static void DrawSurfaceOutlines(int brushNodeID, Shape shape, Matrix4x4 transformation, int surfaceIndex, Color outerColor, Color innerColor, float thickness = GUIConstants.OLD_THICK_LINE_SCALE)
        {
            // .. could be a prefab
            if(brushNodeID == CSGNode.INVALID_NODE_ID || surfaceIndex == -1)
            {
                return;
            }

            if(surfaceIndex < 0 || surfaceIndex >= shape.surfaces.Length)
            {
                return;
            }

            if(!InternalCSGModelManager.External.getSurfaceOutline(brushNodeID, surfaceIndex, out GeometryWireframe outline))
            {
                return;
            }

            Handles.matrix = transformation;

            if(outerColor.a > 0)
            {
                Handles.color = outerColor;

                if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
                {
                    Handles.DrawDottedLines(outline.vertices, outline.visibleOuterLines, VISIBLE_OUTER_LINE_DOTS);
                    PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, thickness, outerColor);
                }

                if(outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
                {
                    Handles.DrawDottedLines(outline.vertices, outline.invisibleOuterLines, INVISIBLE_OUTER_LINE_DOTS);
                }
            }

            if(innerColor.a > 0)
            {
                Handles.color = innerColor;

                if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
                {
                    Handles.DrawDottedLines(outline.vertices, outline.visibleInnerLines, VISIBLE_INNER_LINE_DOTS);
                    PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, thickness, innerColor);
                }

                if(outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
                {
                    Handles.DrawDottedLines(outline.vertices, outline.invisibleInnerLines, INVISIBLE_INNER_LINE_DOTS);
                }
#if TEST_ENABLED
                if (outline.invalidLines != null && outline.invalidLines.Length > 0)
                {
                    Handles.color = Color.red;
                    Handles.DrawDottedLines(outline.vertices, outline.invalidLines, invalidLineDots);
                }
#endif
            }
        }

        private const float K_MIN_ALPHA = 1 / 255.0f;

        public static void DrawSurfaceOutlines(LineMeshManager visibleLinesMeshManager, LineMeshManager invisibleLinesMeshManager, PolygonMeshManager visibleSurfaceMeshManager, GeometryWireframe[] outlines, Matrix4x4[] transformations, Color visibleInnerColor, Color visibleOuterColor, Color visibleOutlineColor, Color invisibleInnerColor, Color invisibleOuterColor, Color invisibleOutlineColor, Color surfaceColor, float thickness = GUIConstants.THICK_LINE_SCALE)
        {
            if(outlines == null)
            {
                return;
            }

            if(invisibleOutlineColor.a >= K_MIN_ALPHA)
            {
                for(int i = 0; i < outlines.Length; i++)
                {
                    GeometryWireframe outline        = outlines[i];
                    Matrix4x4         transformation = transformations[i];

                    if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
                    {
                        invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, invisibleOutlineColor);
                    }

                    if(outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
                    {
                        invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleOuterLines, invisibleOutlineColor);
                    }

                    if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
                    {
                        invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, invisibleOutlineColor);
                    }

                    if(outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
                    {
                        invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleInnerLines, invisibleOutlineColor);
                    }
                }
            }

            if(invisibleInnerColor.a >= K_MIN_ALPHA)
            {
                for(int i = 0; i < outlines.Length; i++)
                {
                    GeometryWireframe outline        = outlines[i];
                    Matrix4x4         transformation = transformations[i];

                    if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
                    {
                        invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, invisibleInnerColor, dashSize: VISIBLE_OUTER_LINE_DOTS);
                    }

                    if(outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
                    {
                        invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleOuterLines, invisibleInnerColor, dashSize: INVISIBLE_OUTER_LINE_DOTS);
                    }
                }
            }

            if(invisibleOuterColor.a >= K_MIN_ALPHA)
            {
                for(int i = 0; i < outlines.Length; i++)
                {
                    GeometryWireframe outline        = outlines[i];
                    Matrix4x4         transformation = transformations[i];

                    if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
                    {
                        invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, invisibleOuterColor, dashSize: VISIBLE_INNER_LINE_DOTS);
                    }

                    if(outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
                    {
                        invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleInnerLines, invisibleOuterColor, dashSize: INVISIBLE_INNER_LINE_DOTS);
                    }
                }
            }

#if TEST_ENABLED
            for (int i = 0; i < outlines.Length; i++)
            {
                var outline = outlines[i];
                if (outline.invalidLines == null || outline.invalidLines.Length == 0)
                    continue;

                var transformation = transformations[i];

                invisibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.invalidLines, Color.red, dashSize: invalidLineDots);
            }
#endif

            if(visibleOutlineColor.a >= K_MIN_ALPHA)
            {
                for(int i = 0; i < outlines.Length; i++)
                {
                    GeometryWireframe outline = outlines[i];

                    Matrix4x4 transformation = transformations[i];

                    if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length != 0)
                    {
                        visibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, visibleOutlineColor, thickness + 2.0f);
                    }

                    if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length != 0)
                    {
                        visibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, visibleOutlineColor, thickness + 2.0f);
                    }
                }
            }

            if(visibleOuterColor.a >= K_MIN_ALPHA || visibleInnerColor.a >= K_MIN_ALPHA)
            {
                for(int i = 0; i < outlines.Length; i++)
                {
                    GeometryWireframe outline = outlines[i];

                    Matrix4x4 transformation = transformations[i];

                    if(visibleInnerColor.a >= K_MIN_ALPHA && outline.visibleOuterLines != null && outline.visibleOuterLines.Length != 0)
                    {
                        visibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, visibleInnerColor, thickness * 0.5f);
                    }

                    if(visibleOuterColor.a >= K_MIN_ALPHA && outline.visibleInnerLines != null && outline.visibleInnerLines.Length != 0)
                    {
                        visibleLinesMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, visibleOuterColor, thickness * 0.5f);
                    }
                }
            }

            if(surfaceColor.a >= K_MIN_ALPHA)
            {
                for(int i = 0; i < outlines.Length; i++)
                {
                    GeometryWireframe outline = outlines[i];

                    if(outline.visibleTriangles == null || outline.visibleTriangles.Length == 0)
                    {
                        continue;
                    }

                    Matrix4x4 transformation = transformations[i];

                    visibleSurfaceMeshManager.DrawTriangles(transformation, outline.vertices, outline.visibleTriangles, surfaceColor);
                }
            }
        }

        public static void DrawOutlines(GeometryWireframe outline, Matrix4x4 transformation, Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, float thickness = -1)
        {
            if(outline == null ||
               outline.vertices == null ||
               outline.vertices.Length == 0 ||
               (outline.visibleOuterLines == null &&
                   outline.invisibleOuterLines == null &&
                   outline.visibleInnerLines == null &&
                   outline.invisibleInnerLines == null &&
                   outline.invalidLines == null))
            {
                return;
            }

            Handles.matrix = transformation;

            if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
            {
                if(thickness <= 0)
                {
                    PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor);
                }
                else
                {
                    PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, thickness, outerColor);
                }
            }

            if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
            {
                if(thickness <= 0)
                {
                    PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor);
                }
                else
                {
                    PaintUtility.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, thickness, innerColor);
                }
            }

            if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
            {
                Handles.color = outerColorOccluded;
                Handles.DrawDottedLines(outline.vertices, outline.visibleOuterLines, VISIBLE_OUTER_LINE_DOTS);
            }

            if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
            {
                Handles.color = innerColorOccluded;
                Handles.DrawDottedLines(outline.vertices, outline.visibleInnerLines, VISIBLE_INNER_LINE_DOTS);
            }

            if(outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
            {
                Handles.color = outerColorOccluded;
                Handles.DrawDottedLines(outline.vertices, outline.invisibleOuterLines, INVISIBLE_OUTER_LINE_DOTS);
            }

            if(outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
            {
                Handles.color = innerColor;
                Handles.DrawDottedLines(outline.vertices, outline.invisibleInnerLines, INVISIBLE_INNER_LINE_DOTS);
            }
#if TEST_ENABLED
            if (outline.invalidLines != null && outline.invalidLines.Length > 0)
            {
                Handles.color = Color.red;
                Handles.DrawDottedLines(outline.vertices, outline.invalidLines, invalidLineDots);
            }
#endif
        }

        public static void DrawOutlines(LineMeshManager zTestLineMeshManager, LineMeshManager noZTestLineMeshManager, GeometryWireframe[] outlines, Matrix4x4[] transformations, Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, float thickness = -1)
        {
            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(outlines == null || transformations == null || outlines.Length != transformations.Length)
            {
                zTestLineMeshManager.Clear();
                noZTestLineMeshManager.Clear();

                return;
            }

            zTestLineMeshManager.Begin();

            if(thickness <= 0)
            {
                for(int i = 0; i < outlines.Length; i++)
                {
                    GeometryWireframe outline = outlines[i];

                    if(outline == null || outline.vertices == null || outline.vertices.Length == 0)
                    {
                        continue;
                    }

                    Matrix4x4 transformation = transformations[i];

                    if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
                    {
                        zTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor);
                    }

                    if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
                    {
                        zTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor);
                    }
                }
            }
            else
            {
                for(int i = 0; i < outlines.Length; i++)
                {
                    GeometryWireframe outline = outlines[i];

                    if(outline == null || outline.vertices == null || outline.vertices.Length == 0)
                    {
                        continue;
                    }

                    Matrix4x4 transformation = transformations[i];

                    if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
                    {
                        zTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColor, thickness);
                    }

                    if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
                    {
                        zTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColor, thickness);
                    }
                }
            }

            zTestLineMeshManager.End();

            noZTestLineMeshManager.Begin();

            for(int i = 0; i < outlines.Length; i++)
            {
                GeometryWireframe outline = outlines[i];

                if(outline == null || outline.vertices == null || outline.vertices.Length == 0)
                {
                    continue;
                }

                Matrix4x4 transformation = transformations[i];
                Handles.matrix = transformation;

                if(outline.visibleOuterLines != null && outline.visibleOuterLines.Length > 0)
                {
                    noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleOuterLines, outerColorOccluded, dashSize: VISIBLE_OUTER_LINE_DOTS);
                }

                if(outline.visibleInnerLines != null && outline.visibleInnerLines.Length > 0)
                {
                    noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.visibleInnerLines, innerColorOccluded, dashSize: VISIBLE_INNER_LINE_DOTS);
                }

                if(outline.invisibleOuterLines != null && outline.invisibleOuterLines.Length > 0)
                {
                    noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleOuterLines, outerColorOccluded, dashSize: INVISIBLE_OUTER_LINE_DOTS);
                }

                if(outline.invisibleInnerLines != null && outline.invisibleInnerLines.Length > 0)
                {
                    noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.invisibleInnerLines, innerColor, dashSize: INVISIBLE_INNER_LINE_DOTS);
                }
#if TEST_ENABLED
                if (outline.invalidLines != null && outline.invalidLines.Length > 0)
                {
                    //Handles.color = Color.red;
                    //Handles.DrawDottedLines(outline.vertices, outline.invalidLines, invalidLineDots);   // internal
                    noZTestLineMeshManager.DrawLines(transformation, outline.vertices, outline.invalidLines, Color.red, dashSize: invalidLineDots);//, zTest: false);
                }
#endif
            }

            noZTestLineMeshManager.End();
        }

        public static void DrawOutlines(int brushNodeID, Matrix4x4 transformation, Color outerColor, Color outerColorOccluded, Color innerColor, Color innerColorOccluded, float thickness = -1)
        {
            // .. could be a prefab
            if(brushNodeID == CSGNode.INVALID_NODE_ID)
            {
                return;
            }

            GeometryWireframe outline = BrushOutlineManager.GetBrushOutline(brushNodeID);

            DrawOutlines(outline, transformation, outerColor, outerColorOccluded, innerColor, innerColorOccluded, thickness);
        }

        public static void DrawSimpleOutlines(LineMeshManager lineMeshManager, GeometryWireframe outline, Matrix4x4 transformation, Color color)
        {
            if(outline == null ||
               outline.vertices == null ||
               outline.vertices.Length == 0 ||
               (outline.visibleOuterLines == null &&
                   outline.invisibleOuterLines == null &&
                   outline.visibleInnerLines == null &&
                   outline.invisibleInnerLines == null &&
                   outline.invalidLines == null))
            {
                return;
            }

            Vector3[] vertices = outline.vertices;
            int[]     indices  = outline.visibleOuterLines;

            if(indices != null && indices.Length > 0 && (indices.Length & 1) == 0)
            {
                lineMeshManager.DrawLines(transformation, vertices, indices, color);
            }

            indices = outline.invisibleOuterLines;

            if(indices != null && indices.Length > 0 && (indices.Length & 1) == 0)
            {
                lineMeshManager.DrawLines(transformation, vertices, indices, color);
            }
        }

        public static void DrawSimpleOutlines(LineMeshManager lineMeshManager, int brushNodeID, Matrix4x4 transformation, Color color)
        {
            // .. could be a prefab
            if(brushNodeID == CSGNode.INVALID_NODE_ID)
            {
                return;
            }

            GeometryWireframe outline = BrushOutlineManager.GetBrushOutline(brushNodeID);
            DrawSimpleOutlines(lineMeshManager, outline, transformation, color);
        }
    }
}

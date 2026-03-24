using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
    public class CSGBounds
    {
        internal static readonly Vector3[] sidePoints = new Vector3[6];
        internal static readonly float[] sidePointSizes = new float[6];
        internal static readonly float[] sideSizes = new float[6];
        internal static readonly Color[] sideColors = new Color[12];
        internal static readonly bool[] sideBackfaced = new bool[6];
        internal static readonly int[] sideControlIDs = new int[6];

        internal static readonly Vector3[] edgePoints = new Vector3[12];
        internal static readonly float[] edgePointSizes = new float[12];
        internal static readonly float[] edgeSizes = new float[12];
        internal static readonly Color[] edgeColors = new Color[24];
        internal static readonly int[] edgeControlIDs = new int[12];

        internal static readonly Vector3[] cornerPoints = new Vector3[8];

        internal static int PrevNearestControl = 0;
        internal static AABB OriginalBounds;
        internal static readonly int boundsHash = "Bounds".GetHashCode();

        internal static AABB Do(Camera camera, AABB bounds, Quaternion worldToLocalRotation, bool showEdgePoints = true)
        {
            for(int i = 0; i < sideControlIDs.Length; i++)
            {
                sideControlIDs[i] = GUIUtility.GetControlID(boundsHash, FocusType.Passive);
            }

            for(int i = 0; i < edgeControlIDs.Length; i++)
            {
                edgeControlIDs[i] = GUIUtility.GetControlID(boundsHash, FocusType.Passive);
            }

            UpdateColors(camera, bounds, worldToLocalRotation, showEdgePoints);

            Event evt = Event.current;

            if(evt.type == EventType.Repaint)
            {
                Render(camera, worldToLocalRotation, showEdgePoints);
            }

            Quaternion localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
            Matrix4x4  localToWorld         = Matrix4x4.TRS(Vector3.zero, localToWorldRotation, Vector3.one);

            Matrix4x4 origMatrix = Handles.matrix;
            Handles.matrix = localToWorld;

            CSGHandles.InitFunction init     = delegate { OriginalBounds = bounds; };
            CSGHandles.InitFunction shutdown = delegate { OriginalBounds = bounds; };

            SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

            for(int i = 0; i < sidePoints.Length; i++)
            {
                Handles.color = sideColors[i * 2 + 1];

                Vector3 position = sidePoints[i];
                Vector3 normal   = BoundsUtilities.aabbSideNormal[i];
                float   size     = sideSizes[i];
                int     id       = sideControlIDs[i];
                EditorGUI.BeginChangeCheck();

                position = CSGSlider1D.Do(camera, id, position, normal, size, CSGHandles.HoverArrowHandleCap, activeSnappingMode, null, init, shutdown);

                if(EditorGUI.EndChangeCheck())
                {
                    Vector3 originalPoint = BoundsUtilities.GetBoundsSidePoint(OriginalBounds, i);
                    bounds = OriginalBounds;

                    float delta = Vector3.Dot(normal, position - originalPoint);

                    if(normal.x < 0)
                    {
                        bounds.MinX -= delta;
                    }
                    else if(normal.x > 0)
                    {
                        bounds.MaxX += delta;
                    }
                    else if(normal.y < 0)
                    {
                        bounds.MinY -= delta;
                    }
                    else if(normal.y > 0)
                    {
                        bounds.MaxY += delta;
                    }
                    else if(normal.z < 0)
                    {
                        bounds.MinZ -= delta;
                    }
                    else if(normal.z > 0)
                    {
                        bounds.MaxZ += delta;
                    }
                }
            }

            if(showEdgePoints)
            {
                for(int i = 0; i < edgePoints.Length; i++)
                {
                    Handles.color = edgeColors[i * 2 + 1];

                    Vector3 position    = edgePoints[i];
                    int[]   sideIndices = BoundsUtilities.aabbEdgeSides[i];
                    Vector3 normal      = BoundsUtilities.aabbEdgeTangents[i];
                    Vector3 direction1  = BoundsUtilities.aabbSideNormal[sideIndices[0]];
                    Vector3 direction2  = BoundsUtilities.aabbSideNormal[sideIndices[1]];
                    float   size        = edgeSizes[i] / 20.0f;
                    int     id          = edgeControlIDs[i];
                    EditorGUI.BeginChangeCheck();

                    position = CSGHandles.Slider2D(camera, id, position, Vector3.zero, normal, direction1, direction2, size, null, activeSnappingMode, null, init, shutdown);

                    if(EditorGUI.EndChangeCheck())
                    {
                        Vector3 originalPoint = BoundsUtilities.GetBoundsEdgePoint(OriginalBounds, i);
                        bounds = OriginalBounds;

                        Vector3    delta = position - originalPoint;
                        CubeSide[] sides = BoundsUtilities.aabbEdgeCubeSides[i];

                        switch(sides[0])
                        {
                            case CubeSide.NegativeX: bounds.MinX += delta.x; break;
                            case CubeSide.PositiveX: bounds.MaxX += delta.x; break;
                            case CubeSide.NegativeY: bounds.MinY += delta.y; break;
                            case CubeSide.PositiveY: bounds.MaxY += delta.y; break;
                            case CubeSide.NegativeZ: bounds.MinZ += delta.z; break;
                            case CubeSide.PositiveZ: bounds.MaxZ += delta.z; break;
                        }

                        switch(sides[1])
                        {
                            case CubeSide.NegativeX: bounds.MinX += delta.x; break;
                            case CubeSide.PositiveX: bounds.MaxX += delta.x; break;
                            case CubeSide.NegativeY: bounds.MinY += delta.y; break;
                            case CubeSide.PositiveY: bounds.MaxY += delta.y; break;
                            case CubeSide.NegativeZ: bounds.MinZ += delta.z; break;
                            case CubeSide.PositiveZ: bounds.MaxZ += delta.z; break;
                        }
                    }
                }
            }

            Handles.matrix = origMatrix;

            if(bounds.MinX > bounds.MaxX)
            {
                (bounds.MinX, bounds.MaxX) = (bounds.MaxX, bounds.MinX);
            }

            if(bounds.MinY > bounds.MaxY)
            {
                (bounds.MinY, bounds.MaxY) = (bounds.MaxY, bounds.MinY);
            }

            if(bounds.MinZ > bounds.MaxZ)
            {
                (bounds.MinZ, bounds.MaxZ) = (bounds.MaxZ, bounds.MinZ);
            }

            return bounds;
        }

        internal static void UpdateColors(Camera camera, AABB bounds, Quaternion worldToLocalRotation, bool showEdgePoints)
        {
            if(!camera)
            {
                return;
            }

            Quaternion localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);

            BoundsUtilities.GetBoundsCornerPoints(bounds, cornerPoints);
            BoundsUtilities.GetBoundsSidePoints(bounds, sidePoints);
            BoundsUtilities.GetBoundsEdgePoints(bounds, edgePoints);

            Color     boundOutlinesColor = ColorSettings.MeshEdgeOutline;
            Matrix4x4 localToWorld       = Matrix4x4.TRS(Vector3.zero, localToWorldRotation, Vector3.one);

            PaintUtility.DrawDottedLines(localToWorld, cornerPoints, BoundsUtilities.aabbLineIndices, boundOutlinesColor);
            PaintUtility.DrawLines(localToWorld, cornerPoints, BoundsUtilities.aabbLineIndices, GUIConstants.OLD_LINE_SCALE, boundOutlinesColor);

            bool isOrtho = camera.orthographic;

            if(isOrtho)
            {
                Vector3 cameraDirection = worldToLocalRotation * camera.transform.forward;

                for(int i = 0; i < BoundsUtilities.aabbSideNormal.Length; i++)
                {
                    sideBackfaced[i] = Mathf.Abs(Vector3.Dot(BoundsUtilities.aabbSideNormal[i], cameraDirection)) > 0.9999f;
                }
            }
            else
            {
                Vector3 cameraPosition = worldToLocalRotation * camera.transform.position;

                for(int i = 0; i < BoundsUtilities.aabbSideNormal.Length; i++)
                {
                    sideBackfaced[i] = Vector3.Dot(BoundsUtilities.aabbSideNormal[i], cameraPosition - sidePoints[i]) < 0;
                }
            }

            int nearestControl = HandleUtility.nearestControl;
            int hotControl     = GUIUtility.hotControl;

            for(int i = 0; i < sidePoints.Length; i++)
            {
                int         id     = sideControlIDs[i];
                SelectState state  = hotControl == id || nearestControl == id ? SelectState.Hovering : SelectState.None;
                Color       color1 = ColorSettings.MeshEdgeOutline;
                Color       color2 = ColorSettings.PolygonInnerStateColor[(int)state];

                bool disabled = false;

                switch(BoundsUtilities.aabbSideAxis[i])
                {
                    case PrincipleAxis.X: disabled = CSGSettings.LockAxisX; break;
                    case PrincipleAxis.Y: disabled = CSGSettings.LockAxisY; break;
                    case PrincipleAxis.Z: disabled = CSGSettings.LockAxisZ; break;
                }

                if(disabled)
                {
                    color2 = Color.red;
                }

                float handleSize      = HandleUtility.GetHandleSize(localToWorldRotation * sidePoints[i]);
                float pointHandleSize = handleSize / 20.0f;

                if(sideBackfaced[i])
                {
                    if(!isOrtho)
                    {
                        color1.a        *= GUIConstants.BACKFACE_TRANSPARENCY;
                        color2.a        *= GUIConstants.BACKFACE_TRANSPARENCY;
                        pointHandleSize *= GUIConstants.BACK_HANDLE_SCALE;
                    }
                    else
                    {
                        pointHandleSize = 0;
                    }
                }
                else
                {
                    pointHandleSize *= GUIConstants.HANDLE_SCALE;
                }

                sidePointSizes[i]     = pointHandleSize;
                sideSizes[i]          = handleSize;
                sideColors[i * 2 + 0] = color1;
                sideColors[i * 2 + 1] = color2;
            }

            if(showEdgePoints)
            {
                for(int i = 0; i < edgePoints.Length; i++)
                {
                    int id = edgeControlIDs[i];

                    SelectState state = hotControl == id || nearestControl == id ? SelectState.Hovering : SelectState.None;

                    Color color1 = ColorSettings.MeshEdgeOutline;
                    Color color2 = ColorSettings.PointInnerStateColor[(int)state];

                    bool disabled1 = BoundsUtilities.aabbEdgeAxis[i][0] switch
                    {
                        PrincipleAxis.X => CSGSettings.LockAxisX
                      , PrincipleAxis.Y => CSGSettings.LockAxisY
                      , PrincipleAxis.Z => CSGSettings.LockAxisZ
                      , var _           => false
                    };

                    bool disabled2 = BoundsUtilities.aabbEdgeAxis[i][1] switch
                    {
                        PrincipleAxis.X => CSGSettings.LockAxisX
                      , PrincipleAxis.Y => CSGSettings.LockAxisY
                      , PrincipleAxis.Z => CSGSettings.LockAxisZ
                      , var _           => false
                    };

                    if(disabled1 && disabled2)
                    {
                        color2 = Color.red;
                    }

                    float handleSize      = HandleUtility.GetHandleSize(localToWorldRotation * edgePoints[i]);
                    float pointHandleSize = handleSize / 20.0f;
                    int   side1           = BoundsUtilities.aabbEdgeSides[i][0];
                    int   side2           = BoundsUtilities.aabbEdgeSides[i][1];

                    if(isOrtho)
                    {
                        if(sideBackfaced[side1] || sideBackfaced[side2])
                        {
                            pointHandleSize = 0;
                        }
                        else
                        {
                            pointHandleSize *= GUIConstants.HANDLE_SCALE;
                        }
                    }
                    else
                    {
                        if(sideBackfaced[side1] && sideBackfaced[side2])
                        {
                            color1.a        *= GUIConstants.BACKFACE_TRANSPARENCY;
                            color2.a        *= GUIConstants.BACKFACE_TRANSPARENCY;
                            pointHandleSize *= GUIConstants.BACK_HANDLE_SCALE;
                        }
                        else
                        {
                            pointHandleSize *= GUIConstants.HANDLE_SCALE;
                        }
                    }

                    edgePointSizes[i]     = pointHandleSize;
                    edgeSizes[i]          = handleSize;
                    edgeColors[i * 2 + 0] = color1;
                    edgeColors[i * 2 + 1] = color2;
                }
            }
        }

        internal static void Render(Camera camera, Quaternion worldToLocalRotation, bool showEdgePoints)
        {
            if(!camera)
            {
                return;
            }

            Quaternion localToWorldRotation = Quaternion.Inverse(worldToLocalRotation);
            Matrix4x4  localToWorld         = Matrix4x4.TRS(Vector3.zero, localToWorldRotation, Vector3.one);

            PaintUtility.DrawDoubleDots(camera, localToWorld, sidePoints, sidePointSizes, sideColors, sidePoints.Length);

            if(showEdgePoints)
            {
                PaintUtility.DrawDoubleDots(camera, localToWorld, edgePoints, edgePointSizes, edgeColors, edgePoints.Length);
            }

            PaintUtility.RenderBoundsSizes(
                worldToLocalRotation, localToWorldRotation, camera, cornerPoints,
                CSGSettings.LockAxisX ? Color.red : Color.white,
                CSGSettings.LockAxisY ? Color.red : Color.white,
                CSGSettings.LockAxisZ ? Color.red : Color.white,
                true, true, true
            );
        }
    }
}

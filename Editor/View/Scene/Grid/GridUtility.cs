using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class SpaceMatrices
    {
        public Matrix4x4 activeLocalToWorld = MathConstants.identityMatrix;
        public Matrix4x4 activeWorldToLocal = MathConstants.identityMatrix;
        public Matrix4x4 modelLocalToWorld = MathConstants.identityMatrix;
        public Matrix4x4 modelWorldToLocal = MathConstants.identityMatrix;

        public static SpaceMatrices Create(Transform transform)
        {
            SpaceMatrices spaceMatrices = new SpaceMatrices();

            if(transform == null ||
               Tools.pivotRotation == PivotRotation.Global)
            {
                return spaceMatrices;
            }

            spaceMatrices.activeLocalToWorld = transform.localToWorldMatrix;
            spaceMatrices.activeWorldToLocal = transform.worldToLocalMatrix;

            Transform model = InternalCSGModelManager.FindModelTransform(transform);

            if(model != null)
            {
                spaceMatrices.modelLocalToWorld = model.localToWorldMatrix;
                spaceMatrices.modelWorldToLocal = model.worldToLocalMatrix;
            }

            return spaceMatrices;
        }
    };

    internal static class GridUtility
    {
        private const float EdgeFudgeFactor = 0.8f;
        private const float VertexFudgeFactor = EdgeFudgeFactor * 0.8f;
        private static readonly Vector3[] test_points = new Vector3[2];
        private static readonly List<LegacyBrushIntersection> worldIntersections = new List<LegacyBrushIntersection>();
        private static Vector3[] found_points = new Vector3[6];

        public static Vector3 CleanNormal(Vector3 normal)
        {
            if(normal.x >= -MathConstants.EqualityEpsilon && normal.x < MathConstants.EqualityEpsilon)
            {
                normal.x = 0;
            }

            if(normal.y >= -MathConstants.EqualityEpsilon && normal.y < MathConstants.EqualityEpsilon)
            {
                normal.y = 0;
            }

            if(normal.z >= -MathConstants.EqualityEpsilon && normal.z < MathConstants.EqualityEpsilon)
            {
                normal.z = 0;
            }

            if(normal.x >= 1 - MathConstants.EqualityEpsilon)
            {
                normal.x = 1;
            }

            if(normal.y >= 1 - MathConstants.EqualityEpsilon)
            {
                normal.y = 1;
            }

            if(normal.z >= 1 - MathConstants.EqualityEpsilon)
            {
                normal.z = 1;
            }

            if(normal.x <= -1 + MathConstants.EqualityEpsilon)
            {
                normal.x = -1;
            }

            if(normal.y <= -1 + MathConstants.EqualityEpsilon)
            {
                normal.y = -1;
            }

            if(normal.z <= -1 + MathConstants.EqualityEpsilon)
            {
                normal.z = -1;
            }

            return normal.normalized;
        }

        public static Vector3 CleanPosition(Vector3 position)
        {
            if(CSGProjectSettings.Instance.snapEverythingTo0001Grid)
            {
                float signX = Mathf.Sign(position.x);
                float signY = Mathf.Sign(position.y);
                float signZ = Mathf.Sign(position.z);

                float absPosX = position.x * signX;
                float absPosY = position.y * signY;
                float absPosZ = position.z * signZ;

                int intPosX = Mathf.FloorToInt(absPosX);
                int intPosY = Mathf.FloorToInt(absPosY);
                int intPosZ = Mathf.FloorToInt(absPosZ);

                float fractPosX = absPosX - intPosX;
                float fractPosY = absPosY - intPosY;
                float fractPosZ = absPosZ - intPosZ;

                fractPosX = Mathf.Round(fractPosX * 1000.0f) / 1000.0f;
                fractPosY = Mathf.Round(fractPosY * 1000.0f) / 1000.0f;
                fractPosZ = Mathf.Round(fractPosZ * 1000.0f) / 1000.0f;

                const float epsilon = MathConstants.EqualityEpsilon;

                if(fractPosX < epsilon)
                {
                    fractPosX = 0;
                }

                if(fractPosY < epsilon)
                {
                    fractPosY = 0;
                }

                if(fractPosZ < epsilon)
                {
                    fractPosZ = 0;
                }

                if(fractPosX >= 1 - epsilon)
                {
                    fractPosX = 1;
                }

                if(fractPosY >= 1 - epsilon)
                {
                    fractPosY = 1;
                }

                if(fractPosZ >= 1 - epsilon)
                {
                    fractPosZ = 1;
                }

                if(!float.IsNaN(fractPosX) && !float.IsInfinity(fractPosX))
                {
                    position.x = (intPosX + fractPosX) * signX;
                }

                if(!float.IsNaN(fractPosY) && !float.IsInfinity(fractPosY))
                {
                    position.y = (intPosY + fractPosY) * signY;
                }

                if(!float.IsNaN(fractPosZ) && !float.IsInfinity(fractPosZ))
                {
                    position.z = (intPosZ + fractPosZ) * signZ;
                }
            }

            return position;
        }

        public static float CleanFloat(float position)
        {
            float sign     = Mathf.Sign(position);
            float absPos   = position * sign;
            int   intPos   = Mathf.FloorToInt(absPos);
            float fractPos = absPos - intPos;

            fractPos = Mathf.Round(fractPos * 1000.0f) / 1000.0f;

            const float epsilon = MathConstants.EqualityEpsilon;

            if(fractPos < epsilon)
            {
                fractPos = 0;
            }

            if(fractPos >= 1 - epsilon)
            {
                fractPos = 1;
            }

            if(!float.IsNaN(fractPos) &&
               !float.IsInfinity(fractPos))
            {
                position = (intPos + fractPos) * sign;
            }

            return position;
        }

        public static Vector3 FixPosition(
            Vector3 currentPosition, Matrix4x4 worldToLocalMatrix, Matrix4x4 localToWorldMatrix
          , Vector3 previousPosition, bool ignoreAxisLocking = false
        )
        {
            if(currentPosition == previousPosition)
            {
                return currentPosition;
            }

            PivotRotation pivotRotation = Tools.pivotRotation;

            if(pivotRotation == PivotRotation.Local)
            {
                previousPosition = worldToLocalMatrix.MultiplyPoint(previousPosition);
                currentPosition  = worldToLocalMatrix.MultiplyPoint(currentPosition);

                if(!ignoreAxisLocking)
                {
                    if(CSGSettings.LockAxisX)
                    {
                        currentPosition.x = previousPosition.x;
                    }

                    if(CSGSettings.LockAxisY)
                    {
                        currentPosition.y = previousPosition.y;
                    }

                    if(CSGSettings.LockAxisZ)
                    {
                        currentPosition.z = previousPosition.z;
                    }
                }

                bool doGridSnapping = CSGSettings.GridSnapping;

                if(doGridSnapping)
                {
                    if(Mathf.Abs(currentPosition.x - previousPosition.x) < MathConstants.EqualityEpsilon)
                    {
                        currentPosition.x = previousPosition.x;
                    }

                    if(Mathf.Abs(currentPosition.y - previousPosition.y) < MathConstants.EqualityEpsilon)
                    {
                        currentPosition.y = previousPosition.y;
                    }

                    if(Mathf.Abs(currentPosition.z - previousPosition.z) < MathConstants.EqualityEpsilon)
                    {
                        currentPosition.z = previousPosition.z;
                    }

                    if(currentPosition.x != previousPosition.x)
                    {
                        currentPosition.x = Mathf.Round(currentPosition.x / CSGSettings.SnapVector.x)
                          * CSGSettings.SnapVector.x;
                    }

                    if(currentPosition.y != previousPosition.y)
                    {
                        currentPosition.y = Mathf.Round(currentPosition.y / CSGSettings.SnapVector.y)
                          * CSGSettings.SnapVector.y;
                    }

                    if(currentPosition.z != previousPosition.z)
                    {
                        currentPosition.z = Mathf.Round(currentPosition.z / CSGSettings.SnapVector.z)
                          * CSGSettings.SnapVector.z;
                    }
                }

                currentPosition = localToWorldMatrix.MultiplyPoint(currentPosition);
            }
            else
            {
                if(!ignoreAxisLocking)
                {
                    if(CSGSettings.LockAxisX)
                    {
                        currentPosition.x = previousPosition.x;
                    }

                    if(CSGSettings.LockAxisY)
                    {
                        currentPosition.y = previousPosition.y;
                    }

                    if(CSGSettings.LockAxisZ)
                    {
                        currentPosition.z = previousPosition.z;
                    }
                }

                if(CSGSettings.GridSnapping)
                {
                    if(currentPosition.x != previousPosition.x)
                    {
                        currentPosition.x = Mathf.Round(currentPosition.x / CSGSettings.SnapVector.x)
                          * CSGSettings.SnapVector.x;
                    }

                    if(currentPosition.y != previousPosition.y)
                    {
                        currentPosition.y = Mathf.Round(currentPosition.y / CSGSettings.SnapVector.y)
                          * CSGSettings.SnapVector.y;
                    }

                    if(currentPosition.z != previousPosition.z)
                    {
                        currentPosition.z = Mathf.Round(currentPosition.z / CSGSettings.SnapVector.z)
                          * CSGSettings.SnapVector.z;
                    }
                }
            }

            currentPosition = CleanPosition(currentPosition);

            return currentPosition;
        }

        public static Vector3 FixPosition(
            Vector3 currentPosition, Transform spaceTransform, Vector3 previousPosition, bool ignoreAxisLocking = false
        )
        {
            if(spaceTransform == null)
            {
                return FixPosition(
                    currentPosition, MathConstants.identityMatrix, MathConstants.identityMatrix, previousPosition
                  , ignoreAxisLocking
                );
            }

            return FixPosition(
                currentPosition, spaceTransform.worldToLocalMatrix, spaceTransform.localToWorldMatrix, previousPosition
              , ignoreAxisLocking
            );
        }

        public static float SnappedAngle(float currentAngle)
        {
            bool doRotationSnapping = CSGSettings.RotationSnapping;

            if(doRotationSnapping)
            {
                currentAngle = Mathf.RoundToInt(currentAngle / CSGSettings.SnapRotation) * CSGSettings.SnapRotation;
            }

            return currentAngle;
        }

        public static void HalfGridSize()
        {
            CSGSettings.SnapVector = CSGSettings.SnapVector * 0.5f;
            CSGSettings.Save();
        }

        public static void DoubleGridSize()
        {
            CSGSettings.SnapVector = CSGSettings.SnapVector * 2.0f;
            CSGSettings.Save();
        }

        public static void ToggleShowGrid()
        {
            CSGSettings.GridVisible = !CSGSettings.GridVisible;
            EditorPrefs.SetBool("ShowGrid", CSGSettings.GridVisible);
        }

        public static void ToggleSnapToGrid()
        {
            switch(CSGSettings.SnapMode)
            {
                case SnapMode.GridSnapping:     CSGSettings.SnapMode = SnapMode.RelativeSnapping; break;
                case SnapMode.RelativeSnapping: CSGSettings.SnapMode = SnapMode.None; break;
                default:
                case SnapMode.None: CSGSettings.SnapMode = SnapMode.GridSnapping; break;
            }

            EditorPrefs.SetInt("SnapMode", (int)CSGSettings.SnapMode);
        }

        private static List<Vector3> FindAllEdgesThatTouchPoint(CSGBrush brush, Vector3 point)
        {
            List<Vector3> lines = new List<Vector3>();

            if(!brush)
            {
                return lines;
            }

            GeometryWireframe outline = BrushOutlineManager.GetBrushOutline(brush.brushNodeID);

            if(outline == null)
            {
                return lines;
            }

            ControlMesh controlMesh = brush.ControlMesh;

            if(controlMesh == null)
            {
                return lines;
            }

            Matrix4x4 localToWorld = brush.transform.localToWorldMatrix;

            HalfEdge[] edges  = controlMesh.Edges;
            Vector3[]  points = controlMesh.Vertices;

            for(int e = 0; e < edges.Length; e++)
            {
                short   vertexIndex1 = edges[e].VertexIndex;
                Vector3 vertex1      = localToWorld.MultiplyPoint(points[vertexIndex1]);

                float distance = (point - vertex1).sqrMagnitude;

                if(distance < MathConstants.EqualityEpsilonSqr)
                {
                    int     twinIndex    = edges[e].TwinIndex;
                    short   vertexIndex2 = edges[twinIndex].VertexIndex;
                    Vector3 vertex2      = localToWorld.MultiplyPoint(points[vertexIndex2]);
                    lines.Add(vertex1);
                    lines.Add(vertex2);
                }
            }

            int[]     indices  = outline.visibleInnerLines;
            Vector3[] vertices = outline.vertices;

            if(indices != null && vertices != null)
            {
                for(int i = 0; i < indices.Length; i += 2)
                {
                    int     index1  = indices[i + 0];
                    int     index2  = indices[i + 1];
                    Vector3 vertex1 = localToWorld.MultiplyPoint(vertices[index1]);
                    Vector3 vertex2 = localToWorld.MultiplyPoint(vertices[index2]);

                    float distance1 = (point - vertex1).sqrMagnitude;
                    float distance2 = (point - vertex2).sqrMagnitude;

                    if(distance1 < MathConstants.EqualityEpsilonSqr ||
                       distance2 < MathConstants.EqualityEpsilonSqr)
                    {
                        lines.Add(vertex1);
                        lines.Add(vertex2);
                    }
                }
            }

            if((CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowCulledSurfaces)
            == HelperSurfaceFlags.ShowCulledSurfaces)
            {
                indices  = outline.invisibleInnerLines;
                vertices = outline.vertices;

                if(indices != null && vertices != null)
                {
                    for(int i = 0; i < indices.Length; i += 2)
                    {
                        int     index1  = indices[i + 0];
                        int     index2  = indices[i + 1];
                        Vector3 vertex1 = localToWorld.MultiplyPoint(vertices[index1]);
                        Vector3 vertex2 = localToWorld.MultiplyPoint(vertices[index2]);

                        float distance1 = (point - vertex1).sqrMagnitude;
                        float distance2 = (point - vertex2).sqrMagnitude;

                        if(distance1 < MathConstants.EqualityEpsilonSqr ||
                           distance2 < MathConstants.EqualityEpsilonSqr)
                        {
                            lines.Add(vertex1);
                            lines.Add(vertex2);
                        }
                    }
                }
            }

            return lines;
        }

        public static Vector3 SnapToWorld(
            Camera camera, CSGPlane snapPlane, Vector3 unsnappedPosition, Vector3 snappedPosition
          , ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush, CSGBrush[] ignoreBrushes = null
        )
        {
            snappedOnBrush = null;

            test_points[0] = unsnappedPosition;
            test_points[1] = snappedPosition;
            worldIntersections.Clear();

            for(int i = 0; i < test_points.Length; i++)
            {
                Vector2                 test_point2D = CameraUtility.WorldToGUIPoint(test_points[i]);
                LegacyBrushIntersection intersection;

                if(SceneQueryUtility.FindWorldIntersection(camera, test_point2D, out intersection))
                {
                    if(intersection.brush &&
                       intersection.brush.ControlMesh != null)
                    {
                        intersection.worldIntersection = GeometryUtility.ProjectPointOnPlane(
                            snapPlane, intersection.worldIntersection
                        );

                        worldIntersections.Add(intersection);
                    }
                }
            }

            Vector3 old_difference           = snappedPosition - unsnappedPosition;
            float   old_difference_magnitude = old_difference.magnitude * 1.5f;
            Vector3 newSnappedPoint          = snappedPosition;

            CSGPlane? snappingPlane = snapPlane;

            for(int i = 0; i < worldIntersections.Count; i++)
            {
                if(ignoreBrushes != null &&
                   ArrayUtility.Contains(ignoreBrushes, worldIntersections[i].brush))
                {
                    continue;
                }

                List<Vector3> outEdgePoints;
                Vector3       outPosition;

                if(SnapToVertices(
                       worldIntersections[i].brush,
                       snappingPlane, unsnappedPosition,
                       out outEdgePoints,
                       out outPosition
                   ))
                {
                    Vector3 new_difference           = outPosition - unsnappedPosition;
                    float   new_difference_magnitude = new_difference.magnitude;

                    if(new_difference_magnitude <= old_difference_magnitude + MathConstants.EqualityEpsilon)
                    {
                        old_difference_magnitude = new_difference_magnitude;
                        newSnappedPoint          = outPosition;
                        snappingEdges            = outEdgePoints;
                        snappedOnBrush           = worldIntersections[i].brush;
                    }
                }

                if(SnapToEdge(
                       camera,
                       worldIntersections[i].brush, snappingPlane ?? worldIntersections[i].worldPlane,
                       worldIntersections[i].worldIntersection,
                       out outEdgePoints,
                       out outPosition
                   ))
                {
                    Vector3 new_difference           = outPosition - unsnappedPosition;
                    float   new_difference_magnitude = new_difference.magnitude * 1.1f;

                    if(new_difference_magnitude <= old_difference_magnitude + MathConstants.EqualityEpsilon)
                    {
                        old_difference_magnitude = new_difference_magnitude;
                        newSnappedPoint          = outPosition;
                        snappingEdges            = outEdgePoints;
                        snappedOnBrush           = worldIntersections[i].brush;
                    }
                }
            }

            //snappingEdges = FindAllEdgesThatTouchPoint(snappedOnBrush, newSnappedPoint);
            return newSnappedPoint;
        }

        private struct SnapData
        {
            public CSGPlane? snapPlane;
            public Vector2 guiPoint;
            public Vector3 worldPoint;
            public float closestDistance;
            public float closestDistanceSqr;
            public List<Vector3> outEdge;
            public Vector3 snappedWorldPoint;
        }

        public static bool SnapToLine(
            Vector3 worldPoint, Vector3 worldVertex1, Vector3 worldVertex2, CSGPlane? snapPlane
          , out Vector3 worldSnappedPoint
        )
        {
            Vector3 localGridPoint = CSGGrid.PointToGridSpace(worldPoint);
            Vector3 localVertex1   = CSGGrid.PointToGridSpace(worldVertex1);
            Vector3 localVertex2   = CSGGrid.PointToGridSpace(worldVertex2);
            Vector3 snapVector     = CSGGrid.gridOrientation.gridSnapVector;

            float minx = Mathf.Min(localVertex1.x, localVertex2.x);
            float maxx = Mathf.Max(localVertex1.x, localVertex2.x);

            float miny = Mathf.Min(localVertex1.y, localVertex2.y);
            float maxy = Mathf.Max(localVertex1.y, localVertex2.y);

            float minz = Mathf.Min(localVertex1.z, localVertex2.z);
            float maxz = Mathf.Max(localVertex1.z, localVertex2.z);

            float localLengthX = maxx - minx;
            float localLengthY = maxy - miny;
            float localLengthZ = maxz - minz;

            if(localLengthX < MathConstants.AlignmentTestEpsilon &&
               localLengthY < MathConstants.AlignmentTestEpsilon &&
               localLengthZ < MathConstants.AlignmentTestEpsilon)
            {
                worldSnappedPoint = worldPoint;

                return false;
            }

            found_points = new Vector3[6];
            int point_count = 0;

            if(localLengthX > MathConstants.AlignmentTestEpsilon)
            {
                float xv = localGridPoint.x / snapVector.x;

                float x1 = Mathf.Floor(xv) * snapVector.x;

                if(x1 > minx && x1 < maxx)
                {
                    float xpos = x1;
                    float t    = (xpos - minx) / localLengthX;

                    if(t > 0 && t < 1.0f)
                    {
                        float   ypos              = localVertex1.y + t * (localVertex2.y - localVertex1.y);
                        float   zpos              = localVertex1.z + t * (localVertex2.z - localVertex1.z);
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, ypos, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        float dist = CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);

                        if(dist < MathConstants.DistanceEpsilon)
                        {
                            found_points[point_count] = worldIntersection;
                            point_count++;
                        }
                    }
                }

                float x2 = Mathf.Ceil(xv) * snapVector.x;

                if(x2 > minx && x2 < maxx)
                {
                    float xpos = x2;
                    float t    = (xpos - minx) / localLengthX;

                    if(t > 0 && t < 1.0f)
                    {
                        float   ypos              = localVertex1.y + t * (localVertex2.y - localVertex1.y);
                        float   zpos              = localVertex1.z + t * (localVertex2.z - localVertex1.z);
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, ypos, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        float dist = CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);

                        if(dist < MathConstants.DistanceEpsilon)
                        {
                            found_points[point_count] = worldIntersection;
                            point_count++;
                        }
                    }
                }
            }

            if(localLengthY > MathConstants.AlignmentTestEpsilon)
            {
                float yv = localGridPoint.y / snapVector.y;

                float y1 = Mathf.Floor(yv) * snapVector.y;

                if(y1 > miny && y1 < maxy)
                {
                    float ypos = y1;
                    float t    = (ypos - miny) / localLengthY;

                    if(t > 0 && t < 1.0f)
                    {
                        float   zpos              = localVertex1.z + t * localLengthZ;
                        float   xpos              = localVertex1.x + t * localLengthX;
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, ypos, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        float dist = CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);

                        if(dist < MathConstants.DistanceEpsilon)
                        {
                            found_points[point_count] = worldIntersection;
                            point_count++;
                        }
                    }
                }

                float y2 = Mathf.Ceil(yv) * snapVector.y;

                if(y2 > miny && y2 < maxy)
                {
                    float ypos = y2;
                    float t    = (ypos - miny) / localLengthY;

                    if(t > 0 && t < 1.0f)
                    {
                        float   zpos              = localVertex1.z + t * localLengthZ;
                        float   xpos              = localVertex1.x + t * localLengthX;
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, ypos, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        float dist = CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);

                        if(dist < MathConstants.DistanceEpsilon)
                        {
                            found_points[point_count] = worldIntersection;
                            point_count++;
                        }
                    }
                }
            }

            if(localLengthZ > MathConstants.AlignmentTestEpsilon)
            {
                float zv = localGridPoint.z / snapVector.z;

                float z1 = Mathf.Floor(zv) * snapVector.z;

                if(z1 > minz && z1 < maxz)
                {
                    float zpos = z1;
                    float t    = (zpos - minz) / localLengthZ;

                    if(t > 0 && t < 1.0f)
                    {
                        float   xpos              = localVertex1.x + t * (localVertex2.x - localVertex1.x);
                        float   ypos              = localVertex1.y + t * (localVertex2.y - localVertex1.y);
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, ypos, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        float dist = CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);

                        if(dist < MathConstants.DistanceEpsilon)
                        {
                            found_points[point_count] = worldIntersection;
                            point_count++;
                        }
                    }
                }

                float z2 = Mathf.Ceil(zv) * snapVector.z;

                if(z2 > minz && z2 < maxz)
                {
                    float zpos = z2;
                    float t    = (zpos - minz) / localLengthZ;

                    if(t > 0 && t < 1.0f)
                    {
                        float   xpos              = localVertex1.x + t * (localVertex2.x - localVertex1.x);
                        float   ypos              = localVertex1.y + t * (localVertex2.y - localVertex1.y);
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, ypos, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        float dist = CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);

                        if(dist < MathConstants.DistanceEpsilon)
                        {
                            found_points[point_count] = worldIntersection;
                            point_count++;
                        }
                    }
                }
            }

            if(point_count == 0)
            {
                worldSnappedPoint = MathConstants.zeroVector3;

                return false;
            }

            if(point_count == 1)
            {
                worldSnappedPoint = found_points[0];

                return true;
            }

            float found_dist  = (found_points[0] - worldPoint).sqrMagnitude;
            int   found_index = 0;

            for(int i = 1; i < point_count; i++)
            {
                float dist = (found_points[i] - worldPoint).sqrMagnitude;

                if(found_dist > dist)
                {
                    found_dist  = dist;
                    found_index = i;
                }
            }

            worldSnappedPoint = found_points[found_index];

            return true;
        }

        private static void SnapToLines(
            int[] indices, Vector3[] localVertices, Matrix4x4 localToWorld, ref SnapData snapData
        )
        {
            if(indices == null || localVertices == null)
            {
                return;
            }

            Vector3 worldVertex3 = MathConstants.zeroVector3;

            for(int i = 0; i < indices.Length; i += 2)
            {
                int     index1       = indices[i + 0];
                int     index2       = indices[i + 1];
                Vector3 worldVertex1 = localToWorld.MultiplyPoint(localVertices[index1]);
                Vector3 worldVertex2 = localToWorld.MultiplyPoint(localVertices[index2]);

                if(!SnapToLine(snapData.worldPoint, worldVertex1, worldVertex2, snapData.snapPlane, out worldVertex3))
                {
                    continue;
                }

                if(snapData.snapPlane.HasValue &&
                   Mathf.Abs(snapData.snapPlane.Value.Distance(worldVertex3)) >= MathConstants.DistanceEpsilon)
                {
                    continue;
                }

                Vector2 guiVertex2  = CameraUtility.WorldToGUIPoint(worldVertex3);
                float   guiDistance = (guiVertex2 - snapData.guiPoint).sqrMagnitude * EdgeFudgeFactor;

                if(guiDistance + MathConstants.DistanceEpsilon >= snapData.closestDistanceSqr)
                {
                    continue;
                }

                snapData.closestDistanceSqr = guiDistance;

                snapData.outEdge = new List<Vector3>
                {
                    worldVertex1
                  , worldVertex2
                };

                snapData.snappedWorldPoint = worldVertex3;
            }
        }

        private static void SnapToLines(Vector3[] worldVertices, int vertexCount, ref SnapData snapData)
        {
            if(worldVertices == null)
            {
                return;
            }

            Vector3 worldVertex3 = MathConstants.zeroVector3;

            for(int i = 0; i < vertexCount; i += 2)
            {
                Vector3 worldVertex1 = worldVertices[i + 0];
                Vector3 worldVertex2 = worldVertices[i + 1];

                if(!SnapToLine(snapData.worldPoint, worldVertex1, worldVertex2, snapData.snapPlane, out worldVertex3))
                {
                    continue;
                }

                if(snapData.snapPlane.HasValue &&
                   Mathf.Abs(snapData.snapPlane.Value.Distance(worldVertex3)) >= MathConstants.DistanceEpsilon)
                {
                    continue;
                }

                Vector2 guiVertex2  = CameraUtility.WorldToGUIPoint(worldVertex3);
                float   guiDistance = (guiVertex2 - snapData.guiPoint).sqrMagnitude * EdgeFudgeFactor;

                if(guiDistance + MathConstants.DistanceEpsilon >= snapData.closestDistanceSqr)
                {
                    continue;
                }

                snapData.closestDistanceSqr = guiDistance;

                snapData.outEdge = new List<Vector3>
                {
                    worldVertex1
                  , worldVertex2
                };

                snapData.snappedWorldPoint = worldVertex3;
            }
        }

        private static bool[] _internal_snapEdgesUsed;
        private static Vector3[] _internal_snapVertices;
        private static int _internal_snapVertexCount;

        public static bool SnapToEdge(
            Camera camera, CSGBrush brush, CSGPlane? _snapPlane, Vector3 _worldPoint, out List<Vector3> outEdgePoints,
            out Vector3 outPosition
        ) //, float _closestDistance = float.PositiveInfinity)
        {
            outPosition   = MathConstants.zeroVector3;
            outEdgePoints = null;

            if(!brush)
            {
                return false;
            }

            ControlMesh controlMesh = brush.ControlMesh;

            if(controlMesh == null || camera == null)
            {
                return false;
            }

            SnapData snapData = new SnapData
            {
                // Find an edge to snap against the point we're interested in
                worldPoint         = _worldPoint
              , guiPoint           = CameraUtility.WorldToGUIPoint(_worldPoint)
              , closestDistance    = float.PositiveInfinity
              , closestDistanceSqr = float.PositiveInfinity
              , snapPlane          = _snapPlane
              , outEdge            = null
              , snappedWorldPoint  = MathConstants.PositiveInfinityVector3
            };

            Vector3[]  points       = controlMesh.Vertices;
            HalfEdge[] edges        = controlMesh.Edges;
            Polygon[]  polygons     = controlMesh.Polygons;
            Matrix4x4  localToWorld = brush.transform.localToWorldMatrix;

            if(_internal_snapEdgesUsed == null ||
               _internal_snapEdgesUsed.Length < edges.Length)
            {
                _internal_snapEdgesUsed = new bool[edges.Length];
                _internal_snapVertices  = new Vector3[edges.Length * 2];
            }

            Array.Clear(_internal_snapEdgesUsed, 0, _internal_snapEdgesUsed.Length);

            _internal_snapVertexCount = 0;

            for(int p = 0; p < polygons.Length; p++)
            {
                int[] edgeIndices = polygons[p].EdgeIndices;

                for(int e = 0; e < edgeIndices.Length; e++)
                {
                    int edgeIndex = edgeIndices[e];

                    if(!edges[edgeIndex].HardEdge)
                    {
                        continue;
                    }

                    if(_internal_snapEdgesUsed[edgeIndex])
                    {
                        continue;
                    }

                    int twin = controlMesh.GetTwinEdgeIndex(edgeIndex);
                    _internal_snapEdgesUsed[edgeIndex] = true;
                    _internal_snapEdgesUsed[twin]      = true;

                    int   twinIndex    = edges[edgeIndex].TwinIndex;
                    short vertexIndex1 = edges[edgeIndex].VertexIndex;
                    short vertexIndex2 = edges[twinIndex].VertexIndex;

                    _internal_snapVertices[_internal_snapVertexCount + 0] =
                        localToWorld.MultiplyPoint(points[vertexIndex1]);

                    _internal_snapVertices[_internal_snapVertexCount + 1] =
                        localToWorld.MultiplyPoint(points[vertexIndex2]);

                    _internal_snapVertexCount += 2;
                }
            }

            if(_internal_snapVertexCount > 0)
            {
                SnapToLines(_internal_snapVertices, _internal_snapVertexCount, ref snapData);
            }

            GeometryWireframe outline = BrushOutlineManager.GetBrushOutline(brush.brushNodeID);

            if(outline != null)
            {
                Vector3[] vertices = outline.vertices;

                int[] indices = outline.visibleInnerLines;
                SnapToLines(indices, vertices, localToWorld, ref snapData);

                if((CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowCulledSurfaces)
                == HelperSurfaceFlags.ShowCulledSurfaces)
                {
                    indices = outline.invisibleInnerLines;
                    SnapToLines(indices, vertices, localToWorld, ref snapData);
                }
            }

            if(snapData.outEdge == null ||
               float.IsInfinity(snapData.closestDistanceSqr))
            {
                return false;
            }

            snapData.closestDistance = Mathf.Sqrt(snapData.closestDistanceSqr);
            outEdgePoints            = snapData.outEdge;
            outPosition              = snapData.snappedWorldPoint;

            return true;
        }

        public static bool SnapToVertices(
            CSGBrush brush, CSGPlane? snapPlane, Vector3 worldPosition, out List<Vector3> outEdgePoints,
            out Vector3 outPosition, float closestDistance = float.PositiveInfinity
        )
        {
            outPosition   = MathConstants.zeroVector3;
            outEdgePoints = null;

            if(!brush)
            {
                return false;
            }

            ControlMesh controlMesh = brush.ControlMesh;

            if(controlMesh == null)
            {
                return false;
            }

            Vector3? outPoint = null;

            // Find an edge to snap against the point we're interested in
            Vector2 guiPoint           = CameraUtility.WorldToGUIPoint(worldPosition);
            float   closestDistanceSqr = closestDistance * closestDistance;
            /*
            var points				= controlMesh.vertices;
            var edges				= controlMesh.edges;
            var polygons			= controlMesh.polygons;
            var localToWorld		= brush.transform.localToWorldMatrix;
            for(int p = 0; p < polygons.Length; p++)
            {
                var edgeIndices				= polygons[p].edgeIndices;
                for (int e = 0; e < edgeIndices.Length; e++)
                {
                    var edgeIndex			= edgeIndices[e];
                    if (!edges[edgeIndex].hardEdge)
                        continue;

                    var twinIndex			= edges[edgeIndex].twinIndex;

                    var vertexIndex1		= edges[edgeIndex].vertexIndex;
                    var vertexIndex2		= edges[twinIndex].vertexIndex;

                    var vertex1				= localToWorld.MultiplyPoint(points[vertexIndex1]);
                    var vertex2				= localToWorld.MultiplyPoint(points[vertexIndex2]);

                    if (!snapPlane.HasValue ||
                        Mathf.Abs(snapPlane.Value.Distance(vertex1)) < Constants.DistanceEpsilon)
                    {
                        var guiVertex1		= (Vector3)CameraUtility.WorldToGUIPoint(vertex1);
                        var guiDistance		= (guiVertex1 - guiPoint).magnitude * VertexFudgeFactor;
                        if (guiDistance + Constants.DistanceEpsilon < closestDistance)
                        {
                            closestDistance = guiDistance;
                            outPoint		= vertex1;
                        }
                    }

                    if (!snapPlane.HasValue ||
                        Mathf.Abs(snapPlane.Value.Distance(vertex2)) < Constants.DistanceEpsilon)
                    {
                        var guiVertex2		= (Vector3)CameraUtility.WorldToGUIPoint(vertex2);
                        var guiDistance		= (guiVertex2 - guiPoint).magnitude * VertexFudgeFactor;
                        if (guiDistance + Constants.DistanceEpsilon < closestDistance)
                        {
                            closestDistance = guiDistance;
                            outPoint		= vertex2;
                        }
                    }
                }
            }*/

            GeometryWireframe outline = BrushOutlineManager.GetBrushOutline(brush.brushNodeID);

            if(outline != null)
            {
                Matrix4x4 localToWorld = brush.transform.localToWorldMatrix;
                int[]     indices      = outline.visibleInnerLines;
                Vector3[] vertices     = outline.vertices;

                if(indices != null && vertices != null)
                {
                    for(int i = 0; i < indices.Length; i += 2)
                    {
                        int     index1  = indices[i + 0];
                        int     index2  = indices[i + 1];
                        Vector3 vertex1 = localToWorld.MultiplyPoint(vertices[index1]);
                        Vector3 vertex2 = localToWorld.MultiplyPoint(vertices[index2]);

                        if(!snapPlane.HasValue ||
                           Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DistanceEpsilon)
                        {
                            Vector2 guiVertex1  = CameraUtility.WorldToGUIPoint(vertex1);
                            float   guiDistance = (guiVertex1 - guiPoint).sqrMagnitude * VertexFudgeFactor;

                            if(guiDistance + MathConstants.DistanceEpsilon < closestDistanceSqr)
                            {
                                closestDistanceSqr = guiDistance;
                                outPoint           = vertex1;

                                continue;
                            }
                        }

                        if(!snapPlane.HasValue ||
                           Mathf.Abs(snapPlane.Value.Distance(vertex2)) < MathConstants.DistanceEpsilon)
                        {
                            Vector2 guiVertex2  = CameraUtility.WorldToGUIPoint(vertex2);
                            float   guiDistance = (guiVertex2 - guiPoint).sqrMagnitude * VertexFudgeFactor;

                            if(guiDistance + MathConstants.DistanceEpsilon < closestDistanceSqr)
                            {
                                closestDistanceSqr = guiDistance;
                                outPoint           = vertex2;

                                continue;
                            }
                        }
                    }
                }

                if((CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowCulledSurfaces)
                == HelperSurfaceFlags.ShowCulledSurfaces)
                {
                    indices  = outline.invisibleInnerLines;
                    vertices = outline.vertices;

                    if(indices != null && vertices != null)
                    {
                        for(int i = 0; i < indices.Length; i += 2)
                        {
                            int     index1  = indices[i + 0];
                            int     index2  = indices[i + 1];
                            Vector3 vertex1 = localToWorld.MultiplyPoint(vertices[index1]);
                            Vector3 vertex2 = localToWorld.MultiplyPoint(vertices[index2]);

                            if(!snapPlane.HasValue ||
                               Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DistanceEpsilon)
                            {
                                Vector2 guiVertex1  = CameraUtility.WorldToGUIPoint(vertex1);
                                float   guiDistance = (guiVertex1 - guiPoint).sqrMagnitude * VertexFudgeFactor;

                                if(guiDistance + MathConstants.DistanceEpsilon < closestDistanceSqr)
                                {
                                    closestDistanceSqr = guiDistance;
                                    outPoint           = vertex1;
                                }
                            }

                            if(!snapPlane.HasValue ||
                               Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DistanceEpsilon)
                            {
                                Vector2 guiVertex2  = CameraUtility.WorldToGUIPoint(vertex2);
                                float   guiDistance = (guiVertex2 - guiPoint).magnitude * VertexFudgeFactor;

                                if(guiDistance + MathConstants.DistanceEpsilon < closestDistanceSqr)
                                {
                                    closestDistanceSqr = guiDistance;
                                    outPoint           = vertex2;
                                }
                            }
                        }
                    }
                }
            }

            if(!outPoint.HasValue || float.IsInfinity(closestDistance))
            {
                return false;
            }

            closestDistance = Mathf.Sqrt(closestDistanceSqr);
            outPosition     = outPoint.Value;
            outEdgePoints   = FindAllEdgesThatTouchPoint(brush, outPosition);

            return true;
        }
    }
}

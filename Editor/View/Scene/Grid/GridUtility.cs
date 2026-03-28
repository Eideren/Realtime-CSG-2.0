using System;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal static class GridUtility
    {
        private const float EDGE_FUDGE_FACTOR = 0.8f;
        private const float VERTEX_FUDGE_FACTOR = EDGE_FUDGE_FACTOR * 0.8f;

        private static readonly Vector3[] testPoints = new Vector3[2];
        private static readonly List<LegacyBrushIntersection> worldIntersections = new List<LegacyBrushIntersection>();

        private static Vector3[] m_Found_Points = new Vector3[6];

        public static Vector3 CleanNormal(Vector3 normal)
        {
            if(normal.x >= -MathConstants.EQUALITY_EPSILON && normal.x < MathConstants.EQUALITY_EPSILON)
            {
                normal.x = 0;
            }

            if(normal.y >= -MathConstants.EQUALITY_EPSILON && normal.y < MathConstants.EQUALITY_EPSILON)
            {
                normal.y = 0;
            }

            if(normal.z >= -MathConstants.EQUALITY_EPSILON && normal.z < MathConstants.EQUALITY_EPSILON)
            {
                normal.z = 0;
            }

            if(normal.x >= 1 - MathConstants.EQUALITY_EPSILON)
            {
                normal.x = 1;
            }

            if(normal.y >= 1 - MathConstants.EQUALITY_EPSILON)
            {
                normal.y = 1;
            }

            if(normal.z >= 1 - MathConstants.EQUALITY_EPSILON)
            {
                normal.z = 1;
            }

            if(normal.x <= -1 + MathConstants.EQUALITY_EPSILON)
            {
                normal.x = -1;
            }

            if(normal.y <= -1 + MathConstants.EQUALITY_EPSILON)
            {
                normal.y = -1;
            }

            if(normal.z <= -1 + MathConstants.EQUALITY_EPSILON)
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

                const float epsilon = MathConstants.EQUALITY_EPSILON;

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

        public static Vector3 FixPosition(Vector3 currentPosition, Matrix4x4 worldToLocalMatrix, Matrix4x4 localToWorldMatrix, Vector3 previousPosition, bool ignoreAxisLocking = false)
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
                    if(Mathf.Abs(currentPosition.x - previousPosition.x) < MathConstants.EQUALITY_EPSILON)
                    {
                        currentPosition.x = previousPosition.x;
                    }

                    if(Mathf.Abs(currentPosition.y - previousPosition.y) < MathConstants.EQUALITY_EPSILON)
                    {
                        currentPosition.y = previousPosition.y;
                    }

                    if(Mathf.Abs(currentPosition.z - previousPosition.z) < MathConstants.EQUALITY_EPSILON)
                    {
                        currentPosition.z = previousPosition.z;
                    }

                    if(currentPosition.x != previousPosition.x)
                    {
                        currentPosition.x = Mathf.Round(currentPosition.x / CSGSettings.SnapVector.x) * CSGSettings.SnapVector.x;
                    }

                    if(currentPosition.y != previousPosition.y)
                    {
                        currentPosition.y = Mathf.Round(currentPosition.y / CSGSettings.SnapVector.y) * CSGSettings.SnapVector.y;
                    }

                    if(currentPosition.z != previousPosition.z)
                    {
                        currentPosition.z = Mathf.Round(currentPosition.z / CSGSettings.SnapVector.z) * CSGSettings.SnapVector.z;
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
                        currentPosition.x = Mathf.Round(currentPosition.x / CSGSettings.SnapVector.x) * CSGSettings.SnapVector.x;
                    }

                    if(currentPosition.y != previousPosition.y)
                    {
                        currentPosition.y = Mathf.Round(currentPosition.y / CSGSettings.SnapVector.y) * CSGSettings.SnapVector.y;
                    }

                    if(currentPosition.z != previousPosition.z)
                    {
                        currentPosition.z = Mathf.Round(currentPosition.z / CSGSettings.SnapVector.z) * CSGSettings.SnapVector.z;
                    }
                }
            }

            return CleanPosition(currentPosition);
        }

        public static Vector3 FixPosition(Vector3 currentPosition, Transform spaceTransform, Vector3 previousPosition, bool ignoreAxisLocking = false)
        {
            if(spaceTransform == null)
            {
                return FixPosition(currentPosition, MathConstants.identityMatrix, MathConstants.identityMatrix, previousPosition, ignoreAxisLocking);
            }

            return FixPosition(currentPosition, spaceTransform.worldToLocalMatrix, spaceTransform.localToWorldMatrix, previousPosition, ignoreAxisLocking);
        }

        public static float SnappedAngle(float currentAngle)
        {
            if(CSGSettings.RotationSnapping)
            {
                currentAngle = Mathf.RoundToInt(currentAngle / CSGSettings.SnapRotation) * CSGSettings.SnapRotation;
            }

            return currentAngle;
        }

        public static void HalfGridSize()
        {
            CSGSettings.SnapVector *= 0.5f;
            CSGSettings.Save();
        }

        public static void DoubleGridSize()
        {
            CSGSettings.SnapVector *= 2.0f;
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

            ControlMesh controlMesh = brush.controlMesh;

            if(controlMesh == null)
            {
                return lines;
            }

            Matrix4x4 localToWorld = brush.transform.localToWorldMatrix;

            HalfEdge[] edges  = controlMesh.edges;
            Vector3[]  points = controlMesh.vertices;

            for(int e = 0; e < edges.Length; e++)
            {
                short   vertexIndex1 = edges[e].vertexIndex;
                Vector3 vertex1      = localToWorld.MultiplyPoint(points[vertexIndex1]);

                float distance = (point - vertex1).sqrMagnitude;

                if(distance < MathConstants.EQUALITY_EPSILON_SQR)
                {
                    int     twinIndex    = edges[e].twinIndex;
                    short   vertexIndex2 = edges[twinIndex].vertexIndex;
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

                    if(distance1 < MathConstants.EQUALITY_EPSILON_SQR || distance2 < MathConstants.EQUALITY_EPSILON_SQR)
                    {
                        lines.Add(vertex1);
                        lines.Add(vertex2);
                    }
                }
            }

            if((CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowCulledSurfaces) == HelperSurfaceFlags.ShowCulledSurfaces)
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

                        if(distance1 < MathConstants.EQUALITY_EPSILON_SQR || distance2 < MathConstants.EQUALITY_EPSILON_SQR)
                        {
                            lines.Add(vertex1);
                            lines.Add(vertex2);
                        }
                    }
                }
            }

            return lines;
        }

        public static Vector3 SnapToWorld(Camera camera, CSGPlane snapPlane, Vector3 unsnappedPosition, Vector3 snappedPosition, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush, CSGBrush[] ignoreBrushes = null)
        {
            snappedOnBrush = null;

            testPoints[0] = unsnappedPosition;
            testPoints[1] = snappedPosition;
            worldIntersections.Clear();

            for(int i = 0; i < testPoints.Length; i++)
            {
                Vector2 testPoint2D = CameraUtility.WorldToGUIPoint(testPoints[i]);

                if(SceneQueryUtility.FindWorldIntersection(camera, testPoint2D, out LegacyBrushIntersection intersection))
                {
                    if(intersection.brush && intersection.brush.controlMesh != null)
                    {
                        intersection.worldIntersection = GeometryUtility.ProjectPointOnPlane(snapPlane, intersection.worldIntersection);

                        worldIntersections.Add(intersection);
                    }
                }
            }

            Vector3 oldDifference          = snappedPosition - unsnappedPosition;
            float   oldDifferenceMagnitude = oldDifference.magnitude * 1.5f;
            Vector3 newSnappedPoint        = snappedPosition;

            CSGPlane? snappingPlane = snapPlane;

            for(int i = 0; i < worldIntersections.Count; i++)
            {
                if(ignoreBrushes != null && ArrayUtility.Contains(ignoreBrushes, worldIntersections[i].brush))
                {
                    continue;
                }

                if(SnapToVertices(worldIntersections[i].brush, snappingPlane, unsnappedPosition, out List<Vector3> outEdgePoints, out Vector3 outPosition))
                {
                    Vector3 newDifference          = outPosition - unsnappedPosition;
                    float   newDifferenceMagnitude = newDifference.magnitude;

                    if(newDifferenceMagnitude <= oldDifferenceMagnitude + MathConstants.EQUALITY_EPSILON)
                    {
                        oldDifferenceMagnitude = newDifferenceMagnitude;
                        newSnappedPoint        = outPosition;
                        snappingEdges          = outEdgePoints;
                        snappedOnBrush         = worldIntersections[i].brush;
                    }
                }

                if(SnapToEdge(camera, worldIntersections[i].brush, snappingPlane, worldIntersections[i].worldIntersection, out outEdgePoints, out outPosition))
                {
                    Vector3 newDifference          = outPosition - unsnappedPosition;
                    float   newDifferenceMagnitude = newDifference.magnitude * 1.1f;

                    if(newDifferenceMagnitude <= oldDifferenceMagnitude + MathConstants.EQUALITY_EPSILON)
                    {
                        oldDifferenceMagnitude = newDifferenceMagnitude;
                        newSnappedPoint        = outPosition;
                        snappingEdges          = outEdgePoints;
                        snappedOnBrush         = worldIntersections[i].brush;
                    }
                }
            }

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

        public static bool SnapToLine(Vector3 worldPoint, Vector3 worldVertex1, Vector3 worldVertex2, CSGPlane? snapPlane, out Vector3 worldSnappedPoint)
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

            if(localLengthX < MathConstants.ALIGNMENT_TEST_EPSILON && localLengthY < MathConstants.ALIGNMENT_TEST_EPSILON && localLengthZ < MathConstants.ALIGNMENT_TEST_EPSILON)
            {
                worldSnappedPoint = worldPoint;

                return false;
            }

            m_Found_Points = new Vector3[6];
            int pointCount = 0;

            if(localLengthX > MathConstants.ALIGNMENT_TEST_EPSILON)
            {
                float xv = localGridPoint.x / snapVector.x;
                float x1 = Mathf.Floor(xv) * snapVector.x;

                if(x1 > minx && x1 < maxx)
                {
                    float t = (x1 - minx) / localLengthX;

                    if(t > 0 && t < 1.0f)
                    {
                        float   ypos              = localVertex1.y + t * (localVertex2.y - localVertex1.y);
                        float   zpos              = localVertex1.z + t * (localVertex2.z - localVertex1.z);
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(x1, ypos, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        float dist = CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2);

                        if(dist < MathConstants.DISTANCE_EPSILON)
                        {
                            m_Found_Points[pointCount] = worldIntersection;
                            pointCount++;
                        }
                    }
                }

                float x2 = Mathf.Ceil(xv) * snapVector.x;

                if(x2 > minx && x2 < maxx)
                {
                    float t = (x2 - minx) / localLengthX;

                    if(t > 0 && t < 1.0f)
                    {
                        float   ypos              = localVertex1.y + t * (localVertex2.y - localVertex1.y);
                        float   zpos              = localVertex1.z + t * (localVertex2.z - localVertex1.z);
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(x2, ypos, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        if(CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2) < MathConstants.DISTANCE_EPSILON)
                        {
                            m_Found_Points[pointCount] = worldIntersection;
                            pointCount++;
                        }
                    }
                }
            }

            if(localLengthY > MathConstants.ALIGNMENT_TEST_EPSILON)
            {
                float yv = localGridPoint.y / snapVector.y;
                float y1 = Mathf.Floor(yv) * snapVector.y;

                if(y1 > miny && y1 < maxy)
                {
                    float t = (y1 - miny) / localLengthY;

                    if(t > 0 && t < 1.0f)
                    {
                        float   zpos              = localVertex1.z + t * localLengthZ;
                        float   xpos              = localVertex1.x + t * localLengthX;
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, y1, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        if(CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2) < MathConstants.DISTANCE_EPSILON)
                        {
                            m_Found_Points[pointCount] = worldIntersection;
                            pointCount++;
                        }
                    }
                }

                float y2 = Mathf.Ceil(yv) * snapVector.y;

                if(y2 > miny && y2 < maxy)
                {
                    float t = (y2 - miny) / localLengthY;

                    if(t > 0 && t < 1.0f)
                    {
                        float   zpos              = localVertex1.z + t * localLengthZ;
                        float   xpos              = localVertex1.x + t * localLengthX;
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, y2, zpos));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        if(CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2) < MathConstants.DISTANCE_EPSILON)
                        {
                            m_Found_Points[pointCount] = worldIntersection;
                            pointCount++;
                        }
                    }
                }
            }

            if(localLengthZ > MathConstants.ALIGNMENT_TEST_EPSILON)
            {
                float zv = localGridPoint.z / snapVector.z;

                float z1 = Mathf.Floor(zv) * snapVector.z;

                if(z1 > minz && z1 < maxz)
                {
                    float t = (z1 - minz) / localLengthZ;

                    if(t > 0 && t < 1.0f)
                    {
                        float   xpos              = localVertex1.x + t * (localVertex2.x - localVertex1.x);
                        float   ypos              = localVertex1.y + t * (localVertex2.y - localVertex1.y);
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, ypos, z1));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        if(CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2) < MathConstants.DISTANCE_EPSILON)
                        {
                            m_Found_Points[pointCount] = worldIntersection;
                            pointCount++;
                        }
                    }
                }

                float z2 = Mathf.Ceil(zv) * snapVector.z;

                if(z2 > minz && z2 < maxz)
                {
                    float t = (z2 - minz) / localLengthZ;

                    if(t > 0 && t < 1.0f)
                    {
                        float   xpos              = localVertex1.x + t * (localVertex2.x - localVertex1.x);
                        float   ypos              = localVertex1.y + t * (localVertex2.y - localVertex1.y);
                        Vector3 worldIntersection = CSGGrid.PointFromGridSpace(new Vector3(xpos, ypos, z2));

                        if(snapPlane.HasValue)
                        {
                            worldIntersection = snapPlane.Value.Project(worldIntersection);
                        }

                        if(CameraUtility.DistancePointLine(worldIntersection, worldVertex1, worldVertex2) < MathConstants.DISTANCE_EPSILON)
                        {
                            m_Found_Points[pointCount] = worldIntersection;
                            pointCount++;
                        }
                    }
                }
            }

            if(pointCount == 0)
            {
                worldSnappedPoint = MathConstants.zeroVector3;

                return false;
            }

            if(pointCount == 1)
            {
                worldSnappedPoint = m_Found_Points[0];

                return true;
            }

            float foundDist  = (m_Found_Points[0] - worldPoint).sqrMagnitude;
            int   foundIndex = 0;

            for(int i = 1; i < pointCount; i++)
            {
                float dist = (m_Found_Points[i] - worldPoint).sqrMagnitude;

                if(foundDist > dist)
                {
                    foundDist  = dist;
                    foundIndex = i;
                }
            }

            worldSnappedPoint = m_Found_Points[foundIndex];

            return true;
        }

        private static void SnapToLines(int[] indices, Vector3[] localVertices, Matrix4x4 localToWorld, ref SnapData snapData)
        {
            if(indices == null || localVertices == null)
            {
                return;
            }

            for(int i = 0; i < indices.Length; i += 2)
            {
                int     index1       = indices[i + 0];
                int     index2       = indices[i + 1];
                Vector3 worldVertex1 = localToWorld.MultiplyPoint(localVertices[index1]);
                Vector3 worldVertex2 = localToWorld.MultiplyPoint(localVertices[index2]);

                if(!SnapToLine(snapData.worldPoint, worldVertex1, worldVertex2, snapData.snapPlane, out Vector3 worldVertex3))
                {
                    continue;
                }

                if(snapData.snapPlane.HasValue && Mathf.Abs(snapData.snapPlane.Value.Distance(worldVertex3)) >= MathConstants.DISTANCE_EPSILON)
                {
                    continue;
                }

                Vector2 guiVertex2  = CameraUtility.WorldToGUIPoint(worldVertex3);
                float   guiDistance = (guiVertex2 - snapData.guiPoint).sqrMagnitude * EDGE_FUDGE_FACTOR;

                if(guiDistance + MathConstants.DISTANCE_EPSILON >= snapData.closestDistanceSqr)
                {
                    continue;
                }

                snapData.closestDistanceSqr = guiDistance;

                snapData.outEdge = new List<Vector3>()
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

            for(int i = 0; i < vertexCount; i += 2)
            {
                Vector3 worldVertex1 = worldVertices[i + 0];
                Vector3 worldVertex2 = worldVertices[i + 1];

                if(!SnapToLine(snapData.worldPoint, worldVertex1, worldVertex2, snapData.snapPlane, out Vector3 worldVertex3))
                {
                    continue;
                }

                if(snapData.snapPlane.HasValue && Mathf.Abs(snapData.snapPlane.Value.Distance(worldVertex3)) >= MathConstants.DISTANCE_EPSILON)
                {
                    continue;
                }

                Vector2 guiVertex2  = CameraUtility.WorldToGUIPoint(worldVertex3);
                float   guiDistance = (guiVertex2 - snapData.guiPoint).sqrMagnitude * EDGE_FUDGE_FACTOR;

                if(guiDistance + MathConstants.DISTANCE_EPSILON >= snapData.closestDistanceSqr)
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

        private static bool[] m_Internal_SnapEdgesUsed;
        private static Vector3[] m_Internal_SnapVertices;
        private static int m_Internal_SnapVertexCount;

        public static bool SnapToEdge(Camera camera, CSGBrush brush, CSGPlane? snapPlane, Vector3 worldPoint, out List<Vector3> outEdgePoints, out Vector3 outPosition)
        {
            outPosition   = MathConstants.zeroVector3;
            outEdgePoints = null;

            if(!brush)
            {
                return false;
            }

            ControlMesh controlMesh = brush.controlMesh;

            if(controlMesh == null || camera == null)
            {
                return false;
            }

            SnapData snapData = new SnapData
            {
                // Find an edge to snap against the point we're interested in
                worldPoint         = worldPoint
              , guiPoint           = CameraUtility.WorldToGUIPoint(worldPoint)
              , closestDistance    = float.PositiveInfinity
              , closestDistanceSqr = float.PositiveInfinity
              , snapPlane          = snapPlane
              , outEdge            = null
              , snappedWorldPoint  = MathConstants.positiveInfinityVector3
            };

            Vector3[]  points       = controlMesh.vertices;
            HalfEdge[] edges        = controlMesh.edges;
            Polygon[]  polygons     = controlMesh.polygons;
            Matrix4x4  localToWorld = brush.transform.localToWorldMatrix;

            if(m_Internal_SnapEdgesUsed == null || m_Internal_SnapEdgesUsed.Length < edges.Length)
            {
                m_Internal_SnapEdgesUsed = new bool[edges.Length];
                m_Internal_SnapVertices  = new Vector3[edges.Length * 2];
            }

            Array.Clear(m_Internal_SnapEdgesUsed, 0, m_Internal_SnapEdgesUsed.Length);

            m_Internal_SnapVertexCount = 0;

            for(int p = 0; p < polygons.Length; p++)
            {
                int[] edgeIndices = polygons[p].edgeIndices;

                for(int e = 0; e < edgeIndices.Length; e++)
                {
                    int edgeIndex = edgeIndices[e];

                    if(!edges[edgeIndex].hardEdge)
                    {
                        continue;
                    }

                    if(m_Internal_SnapEdgesUsed[edgeIndex])
                    {
                        continue;
                    }

                    int twin = controlMesh.GetTwinEdgeIndex(edgeIndex);
                    m_Internal_SnapEdgesUsed[edgeIndex] = true;
                    m_Internal_SnapEdgesUsed[twin]      = true;

                    int   twinIndex    = edges[edgeIndex].twinIndex;
                    short vertexIndex1 = edges[edgeIndex].vertexIndex;
                    short vertexIndex2 = edges[twinIndex].vertexIndex;

                    m_Internal_SnapVertices[m_Internal_SnapVertexCount + 0] = localToWorld.MultiplyPoint(points[vertexIndex1]);
                    m_Internal_SnapVertices[m_Internal_SnapVertexCount + 1] = localToWorld.MultiplyPoint(points[vertexIndex2]);

                    m_Internal_SnapVertexCount += 2;
                }
            }

            if(m_Internal_SnapVertexCount > 0)
            {
                SnapToLines(m_Internal_SnapVertices, m_Internal_SnapVertexCount, ref snapData);
            }

            GeometryWireframe outline = BrushOutlineManager.GetBrushOutline(brush.brushNodeID);

            if(outline != null)
            {
                Vector3[] vertices = outline.vertices;

                int[] indices = outline.visibleInnerLines;

                SnapToLines(indices, vertices, localToWorld, ref snapData);

                if((CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowCulledSurfaces) == HelperSurfaceFlags.ShowCulledSurfaces)
                {
                    indices = outline.invisibleInnerLines;
                    SnapToLines(indices, vertices, localToWorld, ref snapData);
                }
            }

            if(snapData.outEdge == null || float.IsInfinity(snapData.closestDistanceSqr))
            {
                return false;
            }

            snapData.closestDistance = Mathf.Sqrt(snapData.closestDistanceSqr);
            outEdgePoints            = snapData.outEdge;
            outPosition              = snapData.snappedWorldPoint;

            return true;
        }

        public static bool SnapToVertices(CSGBrush brush, CSGPlane? snapPlane, Vector3 worldPosition, out List<Vector3> outEdgePoints, out Vector3 outPosition, float closestDistance = float.PositiveInfinity)
        {
            outPosition   = MathConstants.zeroVector3;
            outEdgePoints = null;

            if(!brush)
            {
                return false;
            }

            ControlMesh controlMesh = brush.controlMesh;

            if(controlMesh == null)
            {
                return false;
            }

            Vector3? outPoint = null;

            // Find an edge to snap against the point we're interested in
            Vector2 guiPoint           = CameraUtility.WorldToGUIPoint(worldPosition);
            float   closestDistanceSqr = closestDistance * closestDistance;

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

                        if(!snapPlane.HasValue || Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DISTANCE_EPSILON)
                        {
                            Vector2 guiVertex1  = CameraUtility.WorldToGUIPoint(vertex1);
                            float   guiDistance = (guiVertex1 - guiPoint).sqrMagnitude * VERTEX_FUDGE_FACTOR;

                            if(guiDistance + MathConstants.DISTANCE_EPSILON < closestDistanceSqr)
                            {
                                closestDistanceSqr = guiDistance;
                                outPoint           = vertex1;

                                continue;
                            }
                        }

                        if(!snapPlane.HasValue || Mathf.Abs(snapPlane.Value.Distance(vertex2)) < MathConstants.DISTANCE_EPSILON)
                        {
                            Vector2 guiVertex2  = CameraUtility.WorldToGUIPoint(vertex2);
                            float   guiDistance = (guiVertex2 - guiPoint).sqrMagnitude * VERTEX_FUDGE_FACTOR;

                            if(guiDistance + MathConstants.DISTANCE_EPSILON < closestDistanceSqr)
                            {
                                closestDistanceSqr = guiDistance;
                                outPoint           = vertex2;
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

                            if(!snapPlane.HasValue || Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DISTANCE_EPSILON)
                            {
                                Vector2 guiVertex1  = CameraUtility.WorldToGUIPoint(vertex1);
                                float   guiDistance = (guiVertex1 - guiPoint).sqrMagnitude * VERTEX_FUDGE_FACTOR;

                                if(guiDistance + MathConstants.DISTANCE_EPSILON < closestDistanceSqr)
                                {
                                    closestDistanceSqr = guiDistance;
                                    outPoint           = vertex1;
                                }
                            }

                            if(!snapPlane.HasValue || Mathf.Abs(snapPlane.Value.Distance(vertex1)) < MathConstants.DISTANCE_EPSILON)
                            {
                                Vector2 guiVertex2  = CameraUtility.WorldToGUIPoint(vertex2);
                                float   guiDistance = (guiVertex2 - guiPoint).magnitude * VERTEX_FUDGE_FACTOR;

                                if(guiDistance + MathConstants.DISTANCE_EPSILON < closestDistanceSqr)
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

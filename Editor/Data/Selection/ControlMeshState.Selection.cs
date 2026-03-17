using System;
using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal class ControlMeshSelection
    {
        [SerializeField] public SelectState[] points;
        [SerializeField] public SelectState[] edges;
        [SerializeField] public SelectState[] polygons;

        public void CopyTo(ControlMeshSelection other)
        {
            if(other == null)
            {
                return;
            }

            if(points != null)
            {
                if(other.points == null || points.Length != other.points.Length)
                {
                    other.points = new SelectState[points.Length];
                }

                Array.Copy(points, other.points, points.Length);
            }

            if(edges != null)
            {
                if(other.edges == null || edges.Length != other.edges.Length)
                {
                    other.edges = new SelectState[edges.Length];
                }

                Array.Copy(edges, other.edges, edges.Length);
            }

            if(polygons != null)
            {
                if(other.polygons == null || polygons.Length != other.polygons.Length)
                {
                    other.polygons = new SelectState[polygons.Length];
                }

                Array.Copy(polygons, other.polygons, polygons.Length);
            }
        }

        public ControlMeshSelection Clone()
        {
            ControlMeshSelection clone = new ControlMeshSelection();
            CopyTo(clone);

            return clone;
        }

        public void Clear()
        {
            points   = null;
            edges    = null;
            polygons = null;
        }

        public void DeselectAll()
        {
            if(points != null)
            {
                Array.Clear(points, 0, points.Length);
            }

            if(edges != null)
            {
                Array.Clear(edges, 0, edges.Length);
            }

            if(polygons != null)
            {
                Array.Clear(polygons, 0, polygons.Length);
            }
        }

        public bool Equals(ControlMeshSelection other)
        {
            if(points != null)
            {
                for(int p = 0; p < points.Length; p++)
                {
                    if(points[p] != other.points[p])
                    {
                        return false;
                    }
                }
            }

            if(other.edges != null)
            {
                for(int e = 0; e < edges.Length; e++)
                {
                    if(edges[e] != other.edges[e])
                    {
                        return false;
                    }
                }
            }

            if(other.polygons != null)
            {
                for(int e = 0; e < polygons.Length; e++)
                {
                    if(polygons[e] != other.polygons[e])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    internal partial class ControlMeshState
    {
        // selection

        public readonly ControlMeshSelection selection = new ControlMeshSelection();

        // backup stuff

        public readonly ControlMeshSelection selectionBackup = new ControlMeshSelection();

        public void BackupSelection()
        {
            selection.CopyTo(selectionBackup);
        }

        public void RevertSelection()
        {
            selectionBackup.CopyTo(selection);
        }

        public void DestroySelectionBackup()
        {
            selectionBackup.Clear();
        }

        public bool HasSelectionChanged()
        {
            return !selection.Equals(selectionBackup);
        }

        public float GetClosestEdgeDistance(CSGPlane cameraPlane, int pointIndex0, int pointIndex1)
        {
            if(pointIndex0 < 0 || pointIndex0 >= worldPoints.Length ||
               pointIndex1 < 0 || pointIndex1 >= worldPoints.Length)
            {
                return float.PositiveInfinity;
            }

            Vector3 point0 = worldPoints[pointIndex0];
            Vector3 point1 = worldPoints[pointIndex1];

            Vector2 mousePoint  = Event.current.mousePosition;
            float   minDistance = CameraUtility.DistanceToLine(cameraPlane, mousePoint, point0, point1) * 3.0f;

            if(!(Mathf.Abs(minDistance) < 4.0f))
            {
                return minDistance;
            }

            int surfaceIndex1 = edgeSurfaces[pointIndex0];
            int surfaceIndex2 = edgeSurfaces[pointIndex1];

            for(int p = 0; p < polygonCenterPoints.Length; p++)
            {
                if(p != surfaceIndex1 &&
                   p != surfaceIndex2)
                {
                    continue;
                }

                Vector3 polygonCenterPoint = polygonCenterPoints[p];

                Vector3 polygonCenterPointOnLine = GeometryUtility.ProjectPointOnInfiniteLine(
                    polygonCenterPoints[p], point0, (point1 - point0).normalized
                );

                Vector3 direction = (polygonCenterPointOnLine - polygonCenterPoint).normalized;

                Vector3 nudgedPoint0 = point0 - direction * 0.05f;
                Vector3 nudgedPoint1 = point1 - direction * 0.05f;

                float otherDistance = CameraUtility.DistanceToLine(cameraPlane, mousePoint, nudgedPoint0, nudgedPoint1);

                if(otherDistance < minDistance)
                {
                    minDistance = otherDistance;
                }
            }

            return minDistance;
        }

        public HashSet<short> GetSelectedPointIndices()
        {
            HashSet<short> indices = new HashSet<short>();

            indices.Clear();

            for(int p = 0; p < selection.points.Length; p++)
            {
                if((selection.points[p] & SelectState.Selected) != SelectState.Selected)
                {
                    continue;
                }

                indices.Add((short)p);
            }

            for(int e = 0; e < selection.edges.Length; e++)
            {
                if((selection.edges[e] & SelectState.Selected) != SelectState.Selected)
                {
                    continue;
                }

                indices.Add((short)edges[e * 2 + 0]);
                indices.Add((short)edges[e * 2 + 1]);
            }

            for(int p = 0; p < selection.polygons.Length; p++)
            {
                if((selection.polygons[p] & SelectState.Selected) != SelectState.Selected)
                {
                    continue;
                }

                int[] pointIndices = polygonPointIndices[p];

                for(int i = 0; i < pointIndices.Length; i++)
                {
                    indices.Add((short)pointIndices[i]);
                }
            }

            return indices;
        }
    }
}

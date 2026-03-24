using System;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class GeneratorBoxSettings : IShapeSettings
    {
        public Vector3[] backupVertices = Array.Empty<Vector3>();
        public Vector3[] vertices = Array.Empty<Vector3>();
        public int[] vertexIDs = Array.Empty<int>();
        public bool[] onGeometryVertices = Array.Empty<bool>();

        public void AddPoint(Vector3 position)
        {
            ArrayUtility.Add(ref vertices, position);
        }

        public void Reset()
        {
            backupVertices = Array.Empty<Vector3>();
            vertices       = Array.Empty<Vector3>();
        }

        public void CopyBackupVertices()
        {
            backupVertices = new Vector3[vertices.Length];
            Array.Copy(vertices, backupVertices, vertices.Length);
        }

        public void CalculatePlane(ref CSGPlane plane)
        {
        }

        public Vector3[] GetVertices(CSGPlane buildPlane, Vector3 worldPosition, Vector3 gridTangent, Vector3 gridBinormal, out bool isValid)
        {
            if(vertices.Length == 0)
            {
                isValid = false;

                return vertices;
            }

            Vector3 corner0         = vertices[0];
            Vector3 corner1         = vertices.Length == 2 ? vertices[1] : worldPosition;
            Vector3 delta           = corner1 - corner0;
            Vector3 projectedWidth  = Vector3.Project(delta, gridTangent);
            Vector3 projectedLength = Vector3.Project(delta, gridBinormal);

            isValid = projectedWidth.magnitude > MathConstants.EqualityEpsilon &&
                projectedLength.magnitude > MathConstants.EqualityEpsilon;

            return new[] { GeometryUtility.ProjectPointOnPlane(buildPlane, corner0), GeometryUtility.ProjectPointOnPlane(buildPlane, corner0 + projectedLength), GeometryUtility.ProjectPointOnPlane(buildPlane, corner0 + projectedWidth + projectedLength), GeometryUtility.ProjectPointOnPlane(buildPlane, corner0 + projectedWidth) };
        }

        public Vector3 GetCenter(CSGPlane plane)
        {
            return (vertices[0] + vertices[1]) * 0.5f;
        }

        public AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal)
        {
            AABB bounds = new AABB();
            bounds.Reset();

            if(vertices != null && vertices.Length == 2)
            {
                Vector3 center          = vertices[0];
                Vector3 corner          = vertices[1];
                Vector3 delta           = corner - center;
                Vector3 projectedWidth  = Vector3.Project(delta, gridTangent);
                Vector3 projectedLength = Vector3.Project(delta, gridBinormal);

                Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
                Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

                Vector3 point0 = rotation * center;
                Vector3 point1 = rotation * (center + projectedWidth);
                Vector3 point2 = rotation * (center + projectedLength);
                Vector3 point3 = rotation * corner;

                min.x = Mathf.Min(min.x, point0.x);
                min.y = Mathf.Min(min.y, point0.y);
                min.z = Mathf.Min(min.z, point0.z);
                max.x = Mathf.Max(max.x, point0.x);
                max.y = Mathf.Max(max.y, point0.y);
                max.z = Mathf.Max(max.z, point0.z);

                min.x = Mathf.Min(min.x, point1.x);
                min.y = Mathf.Min(min.y, point1.y);
                min.z = Mathf.Min(min.z, point1.z);
                max.x = Mathf.Max(max.x, point1.x);
                max.y = Mathf.Max(max.y, point1.y);
                max.z = Mathf.Max(max.z, point1.z);

                min.x = Mathf.Min(min.x, point2.x);
                min.y = Mathf.Min(min.y, point2.y);
                min.z = Mathf.Min(min.z, point2.z);
                max.x = Mathf.Max(max.x, point2.x);
                max.y = Mathf.Max(max.y, point2.y);
                max.z = Mathf.Max(max.z, point2.z);

                min.x = Mathf.Min(min.x, point3.x);
                min.y = Mathf.Min(min.y, point3.y);
                min.z = Mathf.Min(min.z, point3.z);
                max.x = Mathf.Max(max.x, point3.x);
                max.y = Mathf.Max(max.y, point3.y);
                max.z = Mathf.Max(max.z, point3.z);

                bounds.Min = min;
                bounds.Max = max;
            }

            return bounds;
        }

        public void ProjectShapeOnBuildPlane(CSGPlane plane)
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = plane.Project(vertices[i]);
            }
        }

        public void MoveShape(Vector3 offset)
        {
            for(int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = backupVertices[i] + offset;
            }
        }
    }
}

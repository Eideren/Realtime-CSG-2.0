using System;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class GeneratorSphereSettings : IShapeSettings
    {
        public Vector3[] vertices = Array.Empty<Vector3>();
        public int[] vertexIDs = Array.Empty<int>();
        public bool[] onGeometryVertices = Array.Empty<bool>();

        public int SphereSplits
        {
            get => Mathf.Max(1, CSGSettings.SphereSplits);
            set
            {
                if(CSGSettings.SphereSplits == value)
                {
                    return;
                }

                CSGSettings.SphereSplits = value;
                CSGSettings.Save();
            }
        }

        public float SphereOffset
        {
            get => CSGSettings.SphereOffset;
            set
            {
                if(CSGSettings.SphereOffset == value)
                {
                    return;
                }

                CSGSettings.SphereOffset = value;
                CSGSettings.Save();
            }
        }

        public bool SphereSmoothShading
        {
            get => CSGSettings.SphereSmoothShading;
            set
            {
                if(CSGSettings.SphereSmoothShading == value)
                {
                    return;
                }

                CSGSettings.SphereSmoothShading = value;
                CSGSettings.Save();
            }
        }

        public bool IsHemiSphere
        {
            get => CSGSettings.HemiSphereMode;
            set
            {
                if(CSGSettings.HemiSphereMode == value)
                {
                    return;
                }

                CSGSettings.HemiSphereMode = value;
                CSGSettings.Save();
            }
        }

        public float sphereRadius;

        public void AddPoint(Vector3 position)
        {
            ArrayUtility.Add(ref vertices, position);
        }

        public void CalculatePlane(ref CSGPlane plane)
        {
        }

        public float SphereRadius
        {
            get
            {
                if(vertices.Length <= 1)
                {
                    return 0;
                }

                return (vertices[1] - vertices[0]).magnitude;
            }
            set
            {
                if(vertices.Length <= 1)
                {
                    return;
                }

                Vector3 delta  = vertices[1] - vertices[0];
                float   radius = delta.magnitude;

                if(radius == value)
                {
                    return;
                }

                delta  /= radius;
                radius =  value;

                if(radius < MathConstants.MinimumScale)
                {
                    radius = MathConstants.MinimumScale;
                }

                delta *= radius;

                vertices[1] = vertices[0] + delta;
            }
        }

        public void Reset()
        {
            vertices = Array.Empty<Vector3>();
        }

        public Vector3[] GetVertices(CSGPlane buildPlane, Vector3 worldPosition, Vector3 gridTangent, out bool isValid)
        {
            if(vertices.Length < 1)
            {
                isValid = false;

                return vertices;
            }

            if(SphereSplits < 1)
            {
                SphereSplits = 1;
            }

            int     realSplits = (SphereSplits + 1) * 4;
            Vector3 vertex1    = vertices.Length > 1 ? vertices[1] : worldPosition;
            Vector3 delta      = vertex1 - vertices[0];

            sphereRadius = delta.magnitude;

            if(sphereRadius <= MathConstants.DistanceEpsilon)
            {
                isValid = false;

                return Array.Empty<Vector3>();
            }

            Matrix4x4 matrix       = GeometryUtility.Rotate2DToPlaneMatrix(buildPlane);
            Vector3[] realVertices = new Vector3[realSplits];

            float angleOffset = GeometryUtility.SignedAngle(gridTangent, delta / sphereRadius, buildPlane.normal);
            angleOffset -= 90;

            angleOffset += SphereOffset;
            angleOffset *= Mathf.Deg2Rad;

            Vector3 p1 = MathConstants.zeroVector3;

            for(int i = 0; i < realSplits; i++)
            {
                float angle = i * Mathf.PI * 2.0f / realSplits + angleOffset;

                p1.x = Mathf.Sin(angle) * sphereRadius;
                p1.z = Mathf.Cos(angle) * sphereRadius;

                realVertices[i] = p1;
            }

            for(int i = 0; i < realSplits; i++)
            {
                Vector3 point = realVertices[i];

                realVertices[i] = GeometryUtility.ProjectPointOnPlane(
                    buildPlane, vertices[0] + matrix.MultiplyPoint(point)
                );
            }

            isValid = true;

            return realVertices;
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
                vertices[i] += offset;
            }
        }

        public Vector3 GetCenter(CSGPlane plane)
        {
            return vertices[0];
        }

        public AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal)
        {
            AABB bounds = new AABB();
            bounds.Reset();
            float   radius = SphereRadius;
            Vector3 point1 = rotation * (vertices[0] + gridTangent * radius + gridBinormal * radius);
            Vector3 point2 = rotation * (vertices[0] - gridTangent * radius - gridBinormal * radius);
            Vector3 min    = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max    = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

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

            bounds.Min = min;
            bounds.Max = max;

            return bounds;
        }
    }
}

using System;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class GeneratorCylinderSettings : IShapeSettings
    {
        public Vector3[] backupVertices = Array.Empty<Vector3>();
        public Vector3[] vertices = Array.Empty<Vector3>();
        public int[] vertexIDs = Array.Empty<int>();
        public bool[] onGeometryVertices = Array.Empty<bool>();

        public int CircleSides
        {
            get => Mathf.Max(3, CSGSettings.CircleSides);
            set
            {
                if(CSGSettings.CircleSides == value)
                {
                    return;
                }

                CSGSettings.CircleSides = value;
                CSGSettings.Save();
            }
        }

        public float CircleOffset
        {
            get => CSGSettings.CircleOffset;
            set
            {
                if(CSGSettings.CircleOffset == value)
                {
                    return;
                }

                CSGSettings.CircleOffset = value;
                CSGSettings.Save();
            }
        }

        public bool CircleSmoothShading
        {
            get => CSGSettings.CircleSmoothShading;
            set
            {
                if(CSGSettings.CircleSmoothShading == value)
                {
                    return;
                }

                CSGSettings.CircleSmoothShading = value;
                CSGSettings.Save();
            }
        }

        public bool CircleSingleSurfaceEnds
        {
            get => CSGSettings.CircleSingleSurfaceEnds;
            set
            {
                if(CSGSettings.CircleSingleSurfaceEnds == value)
                {
                    return;
                }

                CSGSettings.CircleSingleSurfaceEnds = value;
                CSGSettings.Save();
            }
        }

        public bool CircleDistanceToSide
        {
            get => CSGSettings.CircleDistanceToSide;
            set
            {
                if(CSGSettings.CircleDistanceToSide == value)
                {
                    return;
                }

                CSGSettings.CircleDistanceToSide = value;
                CSGSettings.Save();
            }
        }

        public bool CircleRecenter
        {
            get => CSGSettings.CircleRecenter;
            set
            {
                if(CSGSettings.CircleRecenter == value)
                {
                    return;
                }

                CSGSettings.CircleRecenter = value;
                CSGSettings.Save();
            }
        }

        public float radiusA;
        public float radiusB;

        public void AddPoint(Vector3 position)
        {
            ArrayUtility.Add(ref vertices, position);
        }

        public void CalculatePlane(ref CSGPlane plane)
        {
        }

        public float RadiusA
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

        public float RadiusB { get; set; }

        public void Reset()
        {
            backupVertices = Array.Empty<Vector3>();
            vertices       = Array.Empty<Vector3>();

            CircleSides             = Mathf.Max(3, CSGSettings.CircleSides);
            CircleOffset            = CSGSettings.CircleOffset;
            CircleSmoothShading     = CSGSettings.CircleSmoothShading;
            CircleSingleSurfaceEnds = CSGSettings.CircleSingleSurfaceEnds;
            CircleDistanceToSide    = CSGSettings.CircleDistanceToSide;
            CircleRecenter          = CSGSettings.CircleRecenter;
        }

        public void CopyBackupVertices()
        {
            backupVertices = new Vector3[vertices.Length];
            Array.Copy(vertices, backupVertices, vertices.Length);
        }

        public Vector3[] GetVertices(
            CSGPlane buildPlane, Vector3 worldPosition, Vector3 gridTangent, Vector3 gridBinormal, out bool isValid
        )
        {
            if(vertices.Length < 1)
            {
                isValid = false;

                return vertices;
            }

            if(CircleSides < 3)
            {
                CircleSides = 3;
            }

            Vector3 vertex1 = vertices.Length > 1 ? vertices[1] : worldPosition;
            Vector3 delta   = vertex1 - vertices[0];
            radiusA = delta.magnitude;

            if(radiusA <= MathConstants.DistanceEpsilon)
            {
                isValid = false;

                return Array.Empty<Vector3>();
            }

            Matrix4x4 matrix       = GeometryUtility.Rotate2DToPlaneMatrix(buildPlane);
            Vector3[] realVertices = new Vector3[CircleSides];

            float angleOffset = GeometryUtility.SignedAngle(gridTangent, delta / radiusA, buildPlane.normal);

            if(CircleDistanceToSide)
            {
                angleOffset += 90;

                if((CircleSides & 1) != 1)
                {
                    angleOffset += 180.0f / CircleSides;
                }
            }
            else
            {
                angleOffset -= 90;
            }

            angleOffset += CircleOffset;
            angleOffset *= Mathf.Deg2Rad;

            Vector3 p1 = MathConstants.zeroVector3;

            for(int i = 0; i < CircleSides; i++)
            {
                float angle = i * Mathf.PI * 2.0f / CircleSides + angleOffset;

                p1.x = Mathf.Sin(angle) * radiusA;
                p1.z = Mathf.Cos(angle) * radiusA;

                realVertices[i] = p1;
            }

            if(CircleRecenter)
            {
                Vector3 dirx = matrix.MultiplyVector(delta.normalized);
                Vector3 dirz = Vector3.Cross(MathConstants.upVector3, dirx);

                float minx = float.PositiveInfinity;
                float minz = float.PositiveInfinity;
                float maxx = float.NegativeInfinity;
                float maxz = float.NegativeInfinity;

                for(int i = 0; i < CircleSides; i++)
                {
                    Vector3 point = realVertices[i];
                    float   x     = Vector3.Dot(point, dirx);
                    float   z     = Vector3.Dot(point, dirz);

                    minx = Mathf.Min(x, minx);
                    minz = Mathf.Min(z, minz);

                    maxx = Mathf.Max(x, maxx);
                    maxz = Mathf.Max(z, maxz);
                }

                float   scalex  = radiusA * 2 / (maxx - minx);
                float   scalez  = radiusA * 2 / (maxz - minz);
                Vector3 centerx = (maxx + minx) * -0.5f * dirx;
                Vector3 centerz = (maxz + minz) * -0.5f * dirz;

                for(int i = 0; i < CircleSides; i++)
                {
                    Vector3 point = realVertices[i];
                    float   x     = Vector3.Dot(point, dirx);
                    float   z     = Vector3.Dot(point, dirz);

                    Vector3 ptx = x * dirx;
                    Vector3 ptz = z * dirz;

                    ptx             += centerx;
                    ptz             += centerz;
                    ptx             *= scalex;
                    ptz             *= scalez;
                    realVertices[i] =  ptx + ptz;
                }
            }

            for(int i = 0; i < CircleSides; i++)
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
                vertices[i] = backupVertices[i] + offset;
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
            float   radius = Mathf.Max(RadiusA, RadiusB);
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

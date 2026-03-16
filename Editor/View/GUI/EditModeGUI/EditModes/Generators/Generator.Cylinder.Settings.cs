using System;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class GeneratorCylinderSettings : IShapeSettings
    {
        public Vector3[] backupVertices = new Vector3[0];
        public Vector3[] vertices = new Vector3[0];
        public int[] vertexIDs = new int[0];
        public bool[] onGeometryVertices = new bool[0];

        public int circleSides
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

        public float circleOffset
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

        public bool circleSmoothShading
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

        public bool circleSingleSurfaceEnds
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

        public bool circleDistanceToSide
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

        public bool circleRecenter
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

        public float radiusA = 0.0f;
        public float radiusB = 0.0f;

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
            backupVertices = new Vector3[0];
            vertices       = new Vector3[0];

            circleSides             = Mathf.Max(3, CSGSettings.CircleSides);
            circleOffset            = CSGSettings.CircleOffset;
            circleSmoothShading     = CSGSettings.CircleSmoothShading;
            circleSingleSurfaceEnds = CSGSettings.CircleSingleSurfaceEnds;
            circleDistanceToSide    = CSGSettings.CircleDistanceToSide;
            circleRecenter          = CSGSettings.CircleRecenter;
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

            if(circleSides < 3)
            {
                circleSides = 3;
            }

            //var tangent = GeometryUtility.CalculateTangent(buildPlane.normal);

            Vector3 vertex1 = vertices.Length > 1 ? vertices[1] : worldPosition;

            Vector3 delta = vertex1 - vertices[0];
            radiusA = delta.magnitude;

            if(radiusA <= MathConstants.DistanceEpsilon)
            {
                isValid = false;

                return new Vector3[0];
            }

            Matrix4x4 matrix       = GeometryUtility.Rotate2DToPlaneMatrix(buildPlane);
            Vector3[] realVertices = new Vector3[circleSides];

            float angle_offset = GeometryUtility.SignedAngle(gridTangent, delta / radiusA, buildPlane.normal);

            if(circleDistanceToSide)
            {
                angle_offset += 90;

                if((circleSides & 1) != 1)
                {
                    angle_offset += 180.0f / circleSides;
                }
            }
            else
            {
                angle_offset -= 90;
            }

            angle_offset += circleOffset;
            angle_offset *= Mathf.Deg2Rad;

            Vector3 p1 = MathConstants.zeroVector3;

            for(int i = 0; i < circleSides; i++)
            {
                float angle = i * Mathf.PI * 2.0f / (float)circleSides + angle_offset;

                p1.x = Mathf.Sin(angle) * radiusA;
                p1.z = Mathf.Cos(angle) * radiusA;

                realVertices[i] = p1;
            }

            if(circleRecenter)
            {
                Vector3 dirx = matrix.MultiplyVector(delta.normalized);
                Vector3 dirz = Vector3.Cross(MathConstants.upVector3, dirx);

                float minx = float.PositiveInfinity;
                float minz = float.PositiveInfinity;
                float maxx = float.NegativeInfinity;
                float maxz = float.NegativeInfinity;

                for(int i = 0; i < circleSides; i++)
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

                for(int i = 0; i < circleSides; i++)
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

            for(int i = 0; i < circleSides; i++)
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

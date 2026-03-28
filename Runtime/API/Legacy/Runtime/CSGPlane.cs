using System;
using System.Globalization;
using System.Runtime.InteropServices;
using InternalRealtimeCSG;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Ray = UnityEngine.Ray;
using Quaternion = UnityEngine.Quaternion;
using Mathf = UnityEngine.Mathf;

namespace RealtimeCSG.Legacy
{
    /// <summary>A geometric plane</summary>
    /// <remarks><note>This code will be replaced by [UnityEngine.Plane](https://docs.unity3d.com/ScriptReference/Plane.html) eventually. In older versions of Realtime-CSG [UnityEngine.Plane](https://docs.unity3d.com/ScriptReference/Plane.html), Unity's own Plane, was not serializable and wasn't an option.</note>
    /// <note>This class can be safely serialized.</note></remarks>
    /// <note>This plane class uses the plane equation ax+by+cz-d=0, while uses [UnityEngine.Plane](https://docs.unity3d.com/ScriptReference/Plane.html) ax+by+cz+d=0</note>
    [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct CSGPlane
    {
        /// <value>a in the plane equation: ax+by+cz-d=0</value>
        public float a;

        /// <value>b in the plane equation: ax+by+cz-d=0</value>
        public float b;

        /// <value>c in the plane equation: ax+by+cz-d=0</value>
        public float c;

        /// <value>d in the plane equation: ax+by+cz-d=0</value>
        public float d;

        /// <value>Gets or sets the normal of this plane</value>
        public Vector3 Normal
        {
            get => new Vector3(a, b, c);
            set
            {
                a = value.x;
                b = value.y;
                c = value.z;
            }
        }

        /// <value>Point on the plane</value>
        public Vector3 PointOnPlane
        {
            get => Normal * d;
        }

#region Constructors

        /// <summary>Create a plane with a normal and the distance to the origin</summary>
        /// <param name="plane"></param>
        public CSGPlane(Plane plane)
        {
            a = plane.normal.x;
            b = plane.normal.y;
            c = plane.normal.z;
            d = -plane.distance;
        }

        /// <summary>Create a plane with a normal and the distance to the origin</summary>
        /// <param name="inNormal">The normal of the plane</param>
        /// <param name="inD">The distance to the origin</param>
        public CSGPlane(Vector3 inNormal, float inD)
        {
            Vector3 normal = inNormal.normalized;
            a = normal.x;
            b = normal.y;
            c = normal.z;
            d = inD;
        }

        /// <summary>Create a plane with a point and a normal</summary>
        /// <param name="inNormal">The normal of the plane</param>
        /// <param name="pointOnPlane">The position of the plane</param>
        public CSGPlane(Vector3 inNormal, Vector3 pointOnPlane)
        {
            Vector3 normal = inNormal.normalized;
            a = normal.x;
            b = normal.y;
            c = normal.z;
            d = Vector3.Dot(normal, pointOnPlane);
        }

        /// <summary>Create a plane with a point and a quaternion</summary>
        /// <param name="inRotation">The orientation of the plane</param>
        /// <param name="pointOnPlane">The position of the plane</param>
        public CSGPlane(Quaternion inRotation, Vector3 pointOnPlane)
        {
            Vector3 normal = (inRotation * MathConstants.upVector3).normalized;
            a = normal.x;
            b = normal.y;
            c = normal.z;
            d = Vector3.Dot(normal, pointOnPlane);
        }

        /// <summary>Create a plane by giving the plane equation ax+by+cz-d=0.</summary>
        /// <param name="inA">a in the plane equation: ax+by+cz-d=0</param>
        /// <param name="inB">b in the plane equation: ax+by+cz-d=0</param>
        /// <param name="inC">c in the plane equation: ax+by+cz-d=0</param>
        /// <param name="inD">d in the plane equation: ax+by+cz-d=0</param>
        public CSGPlane(float inA, float inB, float inC, float inD)
        {
            a = inA;
            b = inB;
            c = inC;
            d = inD;
            Normalize();
        }

        /// <summary>Calculate a plane from 3 points</summary>
        /// <param name="point1">First point</param>
        /// <param name="point2">Second point</param>
        /// <param name="point3">Third point</param>
        public CSGPlane(Vector3 point1, Vector3 point2, Vector3 point3)
        {
            Vector3 ab = point2 - point1;
            Vector3 ac = point3 - point1;

            Vector3 normal = Vector3.Cross(ab, ac).normalized;

            a = normal.x;
            b = normal.y;
            c = normal.z;
            d = Vector3.Dot(normal, point1);
        }

#endregion

#region Ray Intersection

        /// <summary>Calculate  an intersection with a given ray and the plane.</summary>
        /// <param name="ray">The ray to find an intersection with</param>
        /// <returns>The intersection point.</returns>
        public Vector3 RayIntersection(Ray ray)
        {
            double startX = ray.origin.x;
            double startY = ray.origin.y;
            double startZ = ray.origin.z;

            double directionX = ray.direction.x;
            double directionY = ray.direction.y;
            double directionZ = ray.direction.z;

            double distanceA = a * startX + b * startY + c * startZ - d;

            double length = a * directionX + b * directionY + c * directionZ;

            double delta = distanceA / length;

            double x = startX - delta * directionX;
            double y = startY - delta * directionY;
            double z = startZ - delta * directionZ;

            return new Vector3((float)x, (float)y, (float)z);
        }

        /// <summary>Attempt to find an intersection with a given ray and the plane.</summary>
        /// <param name="ray">The ray to find an intersection with</param>
        /// <param name="intersection">The intersection point, if found</param>
        /// <returns><b>true</b> if we found an intersection, <b>false</b> if we didn't.</returns>
        public bool TryRayIntersection(Ray ray, out Vector3 intersection)
        {
            Vector3 start     = ray.origin;
            Vector3 end       = ray.origin + ray.direction * 1000.0f;
            float   distanceA = Distance(start);

            if(float.IsInfinity(distanceA) || float.IsNaN(distanceA))
            {
                intersection = MathConstants.zeroVector3;

                return false;
            }

            float distanceB = Distance(end);

            if(float.IsInfinity(distanceB) || float.IsNaN(distanceB))
            {
                intersection = MathConstants.zeroVector3;

                return false;
            }

            Vector3 vector = end - start;
            float   length = distanceB - distanceA;
            float   delta  = distanceB / length;
            intersection = end - delta * vector;

            if(float.IsInfinity(intersection.x) || float.IsNaN(intersection.x) ||
               float.IsInfinity(intersection.y) || float.IsNaN(intersection.y) ||
               float.IsInfinity(intersection.z) || float.IsNaN(intersection.z))
            {
                intersection = MathConstants.zeroVector3;

                return false;
            }

            return true;
        }

        /// <summary>Calculates an intersection between a line and the plane</summary>
        /// <param name="start">Start of the line</param>
        /// <param name="end">End of the line</param>
        /// <returns>Intersection point</returns>
        public Vector3 LineIntersection(Vector3 start, Vector3 end)
        {
            Vector3 vector = end - start;
            float   edist  = Distance(end);
            float   sdist  = Distance(start);
            float   length = edist - sdist;
            float   delta  = edist / length;

            return end - delta * vector;
        }

        /// <summary>Calculates the intersection point between 3 planes</summary>
        /// <param name="inPlane1">The first plane</param>
        /// <param name="inPlane2">The second plane</param>
        /// <param name="inPlane3">The third plane</param>
        /// <returns>The intersection point between the 3 planes.The returned point will consist out NaN values when there is no intersection.</returns>
        public static Vector3 Intersection(
            CSGPlane inPlane1,
            CSGPlane inPlane2,
            CSGPlane inPlane3
        )
        {
            try
            {
                decimal plane1A = (decimal)inPlane1.a;
                decimal plane1B = (decimal)inPlane1.b;
                decimal plane1C = (decimal)inPlane1.c;
                decimal plane1d = (decimal)inPlane1.d;

                decimal plane2A = (decimal)inPlane2.a;
                decimal plane2B = (decimal)inPlane2.b;
                decimal plane2C = (decimal)inPlane2.c;
                decimal plane2d = (decimal)inPlane2.d;

                decimal plane3A = (decimal)inPlane3.a;
                decimal plane3B = (decimal)inPlane3.b;
                decimal plane3C = (decimal)inPlane3.c;
                decimal plane3d = (decimal)inPlane3.d;

                decimal bc1 = plane1B * plane3C - plane3B * plane1C;
                decimal bc2 = plane2B * plane1C - plane1B * plane2C;
                decimal bc3 = plane3B * plane2C - plane2B * plane3C;

                decimal w = -(plane1A * bc3 + plane2A * bc1 + plane3A * bc2);

                decimal ad1 = plane1A * plane3d - plane3A * plane1d;
                decimal ad2 = plane2A * plane1d - plane1A * plane2d;
                decimal ad3 = plane3A * plane2d - plane2A * plane3d;

                decimal x = -(plane1d * bc3 + plane2d * bc1 + plane3d * bc2);
                decimal y = -(plane1C * ad3 + plane2C * ad1 + plane3C * ad2);
                decimal z = +(plane1B * ad3 + plane2B * ad1 + plane3B * ad2);

                x /= w;
                y /= w;
                z /= w;

                Vector3 result = new Vector3((float)x, (float)y, (float)z);

                if(float.IsNaN(result.x) || float.IsInfinity(result.x) ||
                   float.IsNaN(result.y) || float.IsInfinity(result.y) ||
                   float.IsNaN(result.z) || float.IsInfinity(result.z))
                {
                    return MathConstants.naNVector3;
                }

                return result;
            }
            catch
            {
                return MathConstants.naNVector3;
            }
        }

#endregion

        /// <summary>Calculates the distance to a point to this plane</summary>
        /// <param name="point">A point</param>
        /// <returns>The distance from the point to the plane</returns>
        public float Distance(Vector3 point)
        {
            return a * point.x + b * point.y + c * point.z - d;
        }

        /// <summary>Normalize the normal of the plane</summary>
        public void Normalize()
        {
            float magnitude = 1.0f / Mathf.Sqrt(a * a + b * b + c * c);
            a *= magnitude;
            b *= magnitude;
            c *= magnitude;
            d *= magnitude;
        }

        /// <summary>Transform the plane with the given transformation</summary>
        /// <param name="transformation">The transformation to transform the plane with</param>
        public void Transform(Matrix4x4 transformation)
        {
            Matrix4x4 ittrans = transformation.inverse.transpose;
            Vector4   vector  = ittrans * new Vector4(a, b, c, -d);
            a = vector.x;
            b = vector.y;
            c = vector.z;
            d = -vector.w;
        }

        /// <summary>Creates a copy of this plane that has its direction inverted</summary>
        /// <returns>A new plane that is flipped</returns>
        public CSGPlane Negated()
        {
            return new CSGPlane(-a, -b, -c, -d);
        }

        /// <summary>Translates a plane in space</summary>
        /// <param name="translation">A vector to translate the plane over</param>
        /// <returns>A plane translated by <paramref name="translation"/></returns>
        public CSGPlane Translated(Vector3 translation)
        {
            return new CSGPlane(
                a, b, c,
                // translated offset = Normal.Dotproduct(translation)
                // normal = A,B,C
                d + a * translation.x +
                b * translation.y +
                c * translation.z
            );
        }

        /// <summary>Project a point on this plane</summary>
        /// <param name="point">A point</param>
        /// <returns>The projected point</returns>
        public Vector3 Project(Vector3 point)
        {
            float px = point.x;
            float py = point.y;
            float pz = point.z;

            float nx = Normal.x;
            float ny = Normal.y;
            float nz = Normal.z;

            float ax  = (px - nx * d) * nx;
            float ay  = (py - ny * d) * ny;
            float az  = (pz - nz * d) * nz;
            float dot = ax + ay + az;

            float rx = px - dot * nx;
            float ry = py - dot * ny;
            float rz = pz - dot * nz;

            return new Vector3(rx, ry, rz);
        }

#region Equality

        public override int GetHashCode()
        {
            return a.GetHashCode() ^ b.GetHashCode() ^ c.GetHashCode() ^ d.GetHashCode();
        }

        public bool Equals(CSGPlane other)
        {
            if(ReferenceEquals(this, other))
            {
                return true;
            }

            if(ReferenceEquals(other, null))
            {
                return false;
            }

            return Mathf.Abs(Distance(other.PointOnPlane)) <= MathConstants.DISTANCE_EPSILON &&
                Mathf.Abs(other.Distance(PointOnPlane)) <= MathConstants.DISTANCE_EPSILON &&
                Mathf.Abs(a - other.a) <= MathConstants.NORMAL_EPSILON &&
                Mathf.Abs(b - other.b) <= MathConstants.NORMAL_EPSILON &&
                Mathf.Abs(c - other.c) <= MathConstants.NORMAL_EPSILON;
        }

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(this, obj))
            {
                return true;
            }

            if(!(obj is CSGPlane other))
            {
                return false;
            }

            if(ReferenceEquals(other, null))
            {
                return false;
            }

            return Mathf.Abs(Distance(other.PointOnPlane)) <= MathConstants.DISTANCE_EPSILON &&
                Mathf.Abs(other.Distance(PointOnPlane)) <= MathConstants.DISTANCE_EPSILON &&
                Mathf.Abs(a - other.a) <= MathConstants.NORMAL_EPSILON &&
                Mathf.Abs(b - other.b) <= MathConstants.NORMAL_EPSILON &&
                Mathf.Abs(c - other.c) <= MathConstants.NORMAL_EPSILON;
        }

        public static bool operator ==(CSGPlane left, CSGPlane right)
        {
            if(ReferenceEquals(left, right))
            {
                return true;
            }

            if(ReferenceEquals(left, null) ||
               ReferenceEquals(right, null))
            {
                return false;
            }

            return Mathf.Abs(left.Distance(right.PointOnPlane)) <= MathConstants.DISTANCE_EPSILON &&
                Mathf.Abs(right.Distance(left.PointOnPlane)) <= MathConstants.DISTANCE_EPSILON &&
                Mathf.Abs(left.a - right.a) <= MathConstants.NORMAL_EPSILON &&
                Mathf.Abs(left.b - right.b) <= MathConstants.NORMAL_EPSILON &&
                Mathf.Abs(left.c - right.c) <= MathConstants.NORMAL_EPSILON;
        }

        public static bool operator !=(CSGPlane left, CSGPlane right)
        {
            if(ReferenceEquals(left, right))
            {
                return false;
            }

            if(ReferenceEquals(left, null) ||
               ReferenceEquals(right, null))
            {
                return true;
            }

            return (Mathf.Abs(left.Distance(right.PointOnPlane)) > MathConstants.DISTANCE_EPSILON &&
                    Mathf.Abs(right.Distance(left.PointOnPlane)) > MathConstants.DISTANCE_EPSILON &&
                    Mathf.Abs(left.a - right.a) > MathConstants.NORMAL_EPSILON) ||
                Mathf.Abs(left.b - right.b) > MathConstants.NORMAL_EPSILON ||
                Mathf.Abs(left.c - right.c) > MathConstants.NORMAL_EPSILON;
        }

#endregion

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "({0}, {1}, {2}, {3})", a, b, c, d);
        }
    }
}

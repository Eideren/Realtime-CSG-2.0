using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal enum PrincipleAxis
    {
        X
      , Y
      , Z
    }

    internal enum PrincipleAxis2
    {
        XY
      , XZ
      , YZ
    }

    internal static class GeometryUtility
    {
        public static float SignedAngle(Vector2 v1, Vector2 v2)
        {
            //  Acute angle [0,180]
            float angle = Vector2.Angle(v1, v2);

            //  -Acute angle [180,-179]
            float sign        = Mathf.Sign(Vector3.Dot(MathConstants.forwardVector3, Vector3.Cross(v1, v2)));
            float signedAngle = angle * sign;

            //  360 angle
            return signedAngle;
        }

        private static double Angle2D(Vector2 from, Vector2 to)
        {
            double denominator = System.Math.Sqrt(from.sqrMagnitude * to.sqrMagnitude);

            if(denominator < 1e-15F)
            {
                return 0F;
            }

            double dot = (from.x * to.x + from.y * to.y) / denominator;

            if(dot < -1)
            {
                dot = -1;
            }

            if(dot > 1)
            {
                dot = 1;
            }

            return System.Math.Acos(dot) * (360.0 / (System.Math.PI * 2.0));
        }

        public static float SignedAngle2D(Vector2 from, Vector2 to)
        {
            double unsignedAngle = Angle2D(from, to);
            float  cross_z       = from.x * to.y - from.y * to.x;
            int    sign          = System.Math.Sign(cross_z);

            return (float)(unsignedAngle * sign);
        }

        public static float SignedAngle(Vector3 v1, Vector3 v2, Vector3 n)
        {
            //  Acute angle [0,180]
            float angle = Vector3.Angle(v1, v2);

            //  -Acute angle [180,-179]
            float sign        = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(v1, v2)));
            float signedAngle = angle * sign;

            //  360 angle
            return signedAngle;
        }

        public static Vector3 ProjectPointOnPlane(CSGPlane plane, Vector3 point)
        {
            float distance = -Vector3.Dot(plane.normal, point - plane.pointOnPlane);

            return point + plane.normal * distance;
        }

        public static float CounterClockwise(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
        }

        public static bool Intersects(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2)
        {
            if(CounterClockwise(A1, A2, B1) * CounterClockwise(A1, A2, B2) > 0)
            {
                return false;
            }

            if(CounterClockwise(B1, B2, A1) * CounterClockwise(B1, B2, A2) > 0)
            {
                return false;
            }

            return true;
        }

        private struct Line
        {
            public int index0;
            public int index1;
        }

        public static bool SelfIntersecting(List<Vector2> points)
        {
            Line[] lines = new Line[points.Count];

            for(int i = points.Count - 1, j = 0; j < points.Count; i = j, j++)
            {
                lines[j].index0 = i;
                lines[j].index1 = j;
            }

            for(int i = 0; i < lines.Length - 2; i++)
            {
                for(int j = i + 1; j < lines.Length - 1; j++)
                {
                    if(lines[i].index1 == lines[j].index0 ||
                       lines[i].index0 == lines[j].index1)
                    {
                        continue;
                    }

                    if(Intersects(
                           points[lines[i].index0], points[lines[i].index1],
                           points[lines[j].index0], points[lines[j].index1]
                       ))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool SelfIntersecting(Vector2[] points)
        {
            Line[] lines = new Line[points.Length];

            for(int i = points.Length - 1, j = 0; j < points.Length; i = j, j++)
            {
                lines[j].index0 = i;
                lines[j].index1 = j;
            }

            for(int i = 0; i < lines.Length - 2; i++)
            {
                for(int j = i + 1; j < lines.Length - 1; j++)
                {
                    if(lines[i].index1 == lines[j].index0 ||
                       lines[i].index0 == lines[j].index1)
                    {
                        continue;
                    }

                    if(Intersects(
                           points[lines[i].index0], points[lines[i].index1],
                           points[lines[j].index0], points[lines[j].index1]
                       ))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static Vector2[] RotatePlaneTo2D(Matrix4x4 extraMatrix, Vector3[] points, CSGPlane plane)
        {
            Matrix4x4 mat = RotatePlaneTo2DMatrix(plane) * extraMatrix;

            Vector2[] points2D = new Vector2[points.Length];

            for(int i = 0; i < points2D.Length; i++)
            {
                Vector3 point = mat.MultiplyPoint(points[i]);
                points2D[i].x = point.x;
                points2D[i].y = point.z;
            }

            return points2D;
        }

        public static Vector2[] RotatePlaneTo2D(Vector3[] points, CSGPlane plane)
        {
            Matrix4x4 mat = RotatePlaneTo2DMatrix(plane);

            Vector2[] points2D = new Vector2[points.Length];

            for(int i = 0; i < points2D.Length; i++)
            {
                Vector3 point = mat.MultiplyPoint(points[i]);
                points2D[i].x = point.x;
                points2D[i].y = point.z;
            }

            return points2D;
        }

        public static Vector3 RotatePointIntoPlaneSpace(CSGPlane plane, Vector3 point)
        {
            Matrix4x4 mat = RotatePlaneTo2DMatrix(plane);

            return mat.MultiplyPoint(point);
        }

        public static Matrix4x4 RotatePlaneTo2DMatrix(CSGPlane plane)
        {
            return Matrix4x4.TRS(
                MathConstants.zeroVector3, Quaternion.FromToRotation(plane.normal, MathConstants.upVector3)
              , MathConstants.oneVector3
            );
        }

        public static Matrix4x4 RotateSurfaceTo2DMatrix(Surface surface)
        {
            Quaternion from = Quaternion.LookRotation(surface.BiNormal, -surface.Plane.normal);

            return Matrix4x4.TRS(MathConstants.zeroVector3, Quaternion.Inverse(from), MathConstants.oneVector3);
        }

        public static Matrix4x4 Rotate2DToPlaneMatrix(CSGPlane plane)
        {
            return RotatePlaneTo2DMatrix(plane).inverse;
        }

        public static void RotateTransform2DToPlane(CSGPlane plane, Vector3 origin, Transform transform)
        {
            // rotate upward shape into direction of plane
            transform.rotation = Quaternion.FromToRotation(MathConstants.upVector3, plane.normal);
            transform.position = origin;
        }

        public static Vector3[] Rotate2DToPlane(Vector2[] points2D, CSGPlane plane)
        {
            Matrix4x4 mat    = Rotate2DToPlaneMatrix(plane);
            Vector3[] points = new Vector3[points2D.Length];

            for(int i = 0; i < points2D.Length; i++)
            {
                Vector3 input = new Vector3(points2D[i].x, 0, points2D[i].y);
                Vector3 point = mat.MultiplyPoint(input);
                points[i] = ProjectPointOnPlane(plane, point);
            }

            return points;
        }

        public static Vector3 Rotate2DToPlane(Vector2 point2D, CSGPlane plane)
        {
            Matrix4x4 mat   = Rotate2DToPlaneMatrix(plane);
            Vector3   input = new Vector3(point2D.x, 0, point2D.y);
            Vector3   point = mat.MultiplyPoint(input);

            return ProjectPointOnPlane(plane, point);
        }

        public static Vector3[] ToVector3XZ(List<Vector2> points2D)
        {
            Vector3[] points = new Vector3[points2D.Count];

            for(int i = 0; i < points2D.Count; i++)
            {
                points[i].x = points2D[i].x;
                points[i].z = points2D[i].y;
            }

            return points;
        }

        public static Vector3[] ToVector3XZReversed(List<Vector2> points2D)
        {
            Vector3[] points = new Vector3[points2D.Count];

            for(int i = 0, last = points2D.Count - 1; i <= last; i++)
            {
                points[last - i].x = points2D[i].x;
                points[last - i].z = points2D[i].y;
            }

            return points;
        }

        public static Vector3[] ToVector3XZ(Vector2[] points2D)
        {
            Vector3[] points = new Vector3[points2D.Length];

            for(int i = 0; i < points2D.Length; i++)
            {
                points[i].x = points2D[i].x;
                points[i].z = points2D[i].y;
            }

            return points;
        }

        public static float CleanLength(float length)
        {
            int intLength = Mathf.FloorToInt(length);

            float fractLength = length - intLength;

            fractLength = Mathf.Round(fractLength * 1000.0f) / 1000.0f;

            const float epsilon = MathConstants.EqualityEpsilon;

            if(fractLength >= -epsilon &&
               fractLength < epsilon)
            {
                fractLength = 0;
            }

            if(fractLength >= 1 - epsilon)
            {
                fractLength = 1;
            }

            if(fractLength <= -1 + epsilon)
            {
                fractLength = -1;
            }

            return intLength + fractLength;
        }

        public static bool IsNonConvex(List<Vector2> points)
        {
            for(int i = points.Count - 2, j = points.Count - 1, k = 0; k < points.Count; i = j, j = k, k++)
            {
                if(CounterClockwise(points[i], points[j], points[k]) < 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static CSGPlane CalcPolygonPlane(Vector3[] points)
        {
            if(points.Length == 3)
            {
                Vector3 v0 = points[0];
                Vector3 v1 = points[1];
                Vector3 v2 = points[2];

                return new CSGPlane(v0, v1, v2);
            }

            // newell's method to calculate a normal for a concave polygon
            Vector3 normal     = MathConstants.zeroVector3;
            int     prevIndex  = points.Length - 1;
            Vector3 prevVertex = points[prevIndex];

            for(int e = 0; e < points.Length; e++)
            {
                Vector3 currVertex = points[e];
                normal.x = normal.x + (prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z);
                normal.y = normal.y + (prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x);
                normal.z = normal.z + (prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y);

                prevVertex = currVertex;
            }

            normal = normal.normalized;

            float d     = 0.0f;
            int   count = 0;

            for(int e = 0; e < points.Length; e++)
            {
                Vector3 currVertex = points[e];
                d += Vector3.Dot(normal, currVertex);
                count++;
            }

            d /= count;

            return new CSGPlane(normal, d);
        }

        public static CSGPlane CalcPolygonPlane(Vector3[] points, int[] indices)
        {
            if(indices.Length == 3)
            {
                Vector3 v0 = points[indices[0]];
                Vector3 v1 = points[indices[1]];
                Vector3 v2 = points[indices[2]];

                return new CSGPlane(v0, v1, v2);
            }

            // newell's method to calculate a normal for a concave polygon
            Vector3 normal     = MathConstants.zeroVector3;
            int     prevIndex  = indices.Length - 1;
            Vector3 prevVertex = points[indices[prevIndex]];

            for(int e = 0; e < indices.Length; e++)
            {
                Vector3 currVertex = points[indices[e]];
                normal.x = normal.x + (prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z);
                normal.y = normal.y + (prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x);
                normal.z = normal.z + (prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y);

                prevVertex = currVertex;
            }

            normal = normal.normalized;

            float d     = 0.0f;
            int   count = 0;

            for(int e = 0; e < indices.Length; e++)
            {
                Vector3 currVertex = points[indices[e]];
                d += Vector3.Dot(normal, currVertex);
                count++;
            }

            d /= count;

            return new CSGPlane(normal, d);
        }

        public static float CalcPolygonSign(Vector2[] points)
        {
            if(points.Length == 3)
            {
                Vector2 v0 = points[0];
                Vector2 v1 = points[1];
                Vector2 v2 = points[2];

                Vector2 ab = v1 - v0;
                Vector2 ac = v2 - v0;

                return ab.x * ac.y - ab.y * ac.x;
            }

            float   length     = 0.0f;
            int     prevIndex  = points.Length - 1;
            Vector2 prevVertex = points[prevIndex];

            for(int e = 0; e < points.Length; e++)
            {
                Vector2 currVertex = points[e];
                length     += (prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y);
                prevVertex =  currVertex;
            }

            return length;
        }

        public static CSGPlane CalcPolygonPlane(ControlMesh controlMesh, short polygonIndex)
        {
            if(controlMesh == null ||
               controlMesh.Polygons == null ||
               polygonIndex >= controlMesh.Polygons.Length)
            {
                return new CSGPlane(MathConstants.upVector3, 0);
            }

            int[] edgeIndices = controlMesh.Polygons[polygonIndex].EdgeIndices;

            if(edgeIndices.Length == 3)
            {
                Vector3 v0 = controlMesh.GetVertex(edgeIndices[0]);
                Vector3 v1 = controlMesh.GetVertex(edgeIndices[1]);
                Vector3 v2 = controlMesh.GetVertex(edgeIndices[2]);

                return new CSGPlane(v0, v1, v2);
            }

            // newell's method to calculate a normal for a concave polygon
            Vector3 normal    = MathConstants.zeroVector3;
            int     prevIndex = edgeIndices.Length - 1;

            if(prevIndex < 0)
            {
                return new CSGPlane(MathConstants.upVector3, MathConstants.zeroVector3);
            }

            Vector3 prevVertex = controlMesh.GetVertex(edgeIndices[prevIndex]);

            for(int e = 0; e < edgeIndices.Length; e++)
            {
                Vector3 currVertex = controlMesh.GetVertex(edgeIndices[e]);
                normal.x += (prevVertex.y - currVertex.y) * (prevVertex.z + currVertex.z);
                normal.y += (prevVertex.z - currVertex.z) * (prevVertex.x + currVertex.x);
                normal.z += (prevVertex.x - currVertex.x) * (prevVertex.y + currVertex.y);

                prevVertex = currVertex;
            }

            normal = normal.normalized;

            float d     = 0.0f;
            int   count = 0;

            for(int e = 0; e < edgeIndices.Length; e++)
            {
                Vector3 currVertex = controlMesh.GetVertex(edgeIndices[e]);
                d += Vector3.Dot(normal, currVertex);
                count++;
            }

            d /= count;

            return new CSGPlane(normal, d);
        }

        public static PrincipleAxis GetPrincipleAxis(Vector3 vector)
        {
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);
            float absZ = Mathf.Abs(vector.z);

            if(absX > absY)
            {
                return absX > absZ ? PrincipleAxis.X : PrincipleAxis.Z;
            }

            return absY > absZ ? PrincipleAxis.Y : PrincipleAxis.Z;
        }

        public static Vector3 SnapToClosestAxis(Vector3 vector)
        {
            //Vector3 tangent;
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);
            float absZ = Mathf.Abs(vector.z);

            if(absX > absY)
            {
                return absX > absZ ? new Vector3(Mathf.Sign(vector.x), 0, 0) : new Vector3(0, 0, Mathf.Sign(vector.z));
            }

            return absY > absZ ? new Vector3(0, Mathf.Sign(vector.y), 0) : new Vector3(0, 0, Mathf.Sign(vector.z));
        }

        public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
        {
            // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
            Quaternion q;
            q.w =  Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
            q.x =  Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
            q.y =  Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
            q.z =  Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
            q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
            q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
            q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));

            return q;
        }

        private static readonly Vector3 negativeY = new Vector3(0, -1, 0);
        private static readonly Vector3 positiveZ = new Vector3(0, 0, 1);

        public static Vector3 CalculateTangent(Vector3 vector)
        {
            float absX = Mathf.Abs(vector.x);
            float absY = Mathf.Abs(vector.y);
            float absZ = Mathf.Abs(vector.z);

            if(absY > absX && absY > absZ)
            {
                return positiveZ;
            }

            return negativeY;
        }

        public static void CalculateTangents(Vector3 normal, out Vector3 tangent, out Vector3 binormal)
        {
            tangent  = CalculateTangent(normal);
            tangent  = Vector3.Cross(normal, tangent).normalized;
            binormal = Vector3.Cross(normal, tangent).normalized;
        }

        public static Vector3 ProjectPointOnInfiniteLine(
            Vector3 point, Vector3 lineOrigin, Vector3 normalizedLineDirection
        )
        {
            float dot = Vector3.Dot(normalizedLineDirection, point - lineOrigin);

            return lineOrigin + normalizedLineDirection * dot;
        }

        public static CSGPlane InverseTransformPlane(Matrix4x4 inverseMatrix, CSGPlane plane)
        {
            Matrix4x4 dstMatrix = Matrix4x4.Transpose(inverseMatrix);
            Vector4   dstPlaneV = dstMatrix * new Vector4(plane.a, plane.b, plane.c, -plane.d);

            return new CSGPlane(dstPlaneV.x, dstPlaneV.y, dstPlaneV.z, -dstPlaneV.w);
        }

        public static CSGPlane TransformPlane(Matrix4x4 matrix, CSGPlane plane)
        {
            Matrix4x4 dstMatrix = Matrix4x4.Transpose(Matrix4x4.Inverse(matrix));
            Vector4   dstPlaneV = dstMatrix * new Vector4(plane.a, plane.b, plane.c, -plane.d);

            return new CSGPlane(dstPlaneV.x, dstPlaneV.y, dstPlaneV.z, -dstPlaneV.w);
        }

        public static Quaternion GetOrientationForPlane(CSGPlane plane)
        {
            Vector3 normal = plane.normal;

            Vector3 tangent = Vector3.Cross(normal, Vector3.Cross(normal, CalculateTangent(normal)).normalized)
               .normalized;

            Quaternion q = Quaternion.LookRotation(tangent, normal);

            return q;
        }

        public static float DistancePointToCircle(
            SceneView sceneView, Vector3 point, Vector3 circleCenter, float circleRadius
        )
        {
            Vector2 pointCenter  = HandleUtility.WorldToGUIPoint(point);
            Vector2 screenCenter = HandleUtility.WorldToGUIPoint(circleCenter);
            Camera  cam          = sceneView.camera;

            if(cam)
            {
                Vector2 screenEdge = HandleUtility.WorldToGUIPoint(circleCenter + cam.transform.right * circleRadius);
                circleRadius = (screenCenter - screenEdge).magnitude;
            }

            float dist = (screenCenter - pointCenter).magnitude;

            if(dist < circleRadius)
            {
                return 0;
            }

            return dist - circleRadius;
        }

        public static void MoveControlMeshVertices(CSGBrush[] brushes, Vector3 offset)
        {
            for(int b = 0; b < brushes.Length; b++)
            {
                ControlMesh controlMesh = brushes[b].ControlMesh;

                if(controlMesh == null ||
                   controlMesh.Vertices == null)
                {
                    continue;
                }

                Vector3 localOffset = brushes[b].transform.worldToLocalMatrix.MultiplyVector(offset);

                for(int p = 0; p < controlMesh.Vertices.Length; p++)
                {
                    controlMesh.Vertices[p] = GridUtility.CleanPosition(controlMesh.Vertices[p] + localOffset);
                }
            }
        }

        public static void MoveControlMeshVertices(CSGBrush brush, Vector3 offset)
        {
            if(!brush)
            {
                return;
            }

            ControlMesh controlMesh = brush.ControlMesh;

            if(controlMesh == null || controlMesh.Vertices == null)
            {
                return;
            }

            Vector3 localOffset = brush.transform.worldToLocalMatrix.MultiplyVector(offset);

            for(int p = 0; p < controlMesh.Vertices.Length; p++)
            {
                controlMesh.Vertices[p] = GridUtility.CleanPosition(controlMesh.Vertices[p] + localOffset);
            }
        }

        public static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            Vector2 ret;

            // TODO: see if we can re-use calculations across list of points

            float t0 = t; // TODO: turn this into t * 0.5f?
            float t1 = t0 * t;
            float t2 = t1 * t;

            ret.x = 0.5f * // TODO: remove this?
            (
                p1.x * 2.0f + // TODO: change to 1.0f?
                (-p0.x + p2.x) * t0 +
                (2.0f * p0.x - 5.0f * p1.x + 4.0f * p2.x - p3.x) * t1 +
                (-p0.x + 3.0f * p1.x - 3.0f * p2.x + p3.x) * t2
            );

            ret.y = 0.5f * // TODO: remove this?
            (
                p1.y * 2.0f + // TODO: change to 1.0f?
                (-p0.y + p2.y) * t0 +
                (2.0f * p0.y - 5.0f * p1.y + 4.0f * p2.y - p3.y) * t1 +
                (-p0.y + 3.0f * p1.y - 3.0f * p2.y + p3.y) * t2
            );

            return ret;
        }

        public static Matrix4x4 Multiply3x3To4x4(Matrix4x4 left3x3, Matrix4x4 right3x3)
        {
            float lM00 = left3x3.m00, lM01 = left3x3.m01, lM02 = left3x3.m02, lM03 = left3x3.m03;
            float lM10 = left3x3.m10, lM11 = left3x3.m11, lM12 = left3x3.m12, lM13 = left3x3.m13;
            float lM20 = left3x3.m20, lM21 = left3x3.m21, lM22 = left3x3.m22, lM23 = left3x3.m23;
            float lM30 = left3x3.m30, lM31 = left3x3.m31, lM32 = left3x3.m32, lM33 = left3x3.m33;

            float rM00 = right3x3.m00, rM01 = right3x3.m01, rM02 = right3x3.m02;
            float rM10 = right3x3.m10, rM11 = right3x3.m11, rM12 = right3x3.m12;
            float rM20 = right3x3.m20, rM21 = right3x3.m21, rM22 = right3x3.m22;

            return new Matrix4x4
            {
                m00 = lM00 * rM00 + lM01 * rM10 + lM02 * rM20
              , m01 = lM00 * rM01 + lM01 * rM11 + lM02 * rM21
              , m02 = lM00 * rM02 + lM01 * rM12 + lM02 * rM22
              , m03 = lM03
              , m10 = lM10 * rM00 + lM11 * rM10 + lM12 * rM20
              , m11 = lM10 * rM01 + lM11 * rM11 + lM12 * rM21
              , m12 = lM10 * rM02 + lM11 * rM12 + lM12 * rM22
              , m13 = lM13
              , m20 = lM20 * rM00 + lM21 * rM10 + lM22 * rM20
              , m21 = lM20 * rM01 + lM21 * rM11 + lM22 * rM21
              , m22 = lM20 * rM02 + lM21 * rM12 + lM22 * rM22
              , m23 = lM23
              , m30 = lM30 * rM00 + lM31 * rM10 + lM32 * rM20
              , m31 = lM30 * rM01 + lM31 * rM11 + lM32 * rM21
              , m32 = lM30 * rM02 + lM31 * rM12 + lM32 * rM22
              , m33 = lM33
            };
        }

        public static bool ContinueTexGenFromSurfaceToSurface(CSGBrush brush, int srcSurfaceIndex, int dstSurfaceIndex)
        {
            if(srcSurfaceIndex < 0 || srcSurfaceIndex >= brush.Shape.TexGens.Length ||
               !brush)
            {
                return false;
            }

            Shape         shape       = brush.Shape;
            TexGen[]      texGens     = shape.TexGens;
            TexGenFlags[] texGenFlags = shape.TexGenFlags;
            Surface[]     surfaces    = shape.Surfaces;

            int srcTexGenIndex = surfaces[srcSurfaceIndex].TexGenIndex;
            int dstTexGenIndex = surfaces[dstSurfaceIndex].TexGenIndex;

            CSGPlane dstLocalPlane = surfaces[dstSurfaceIndex].Plane;
            CSGPlane srcLocalPlane = surfaces[srcSurfaceIndex].Plane;

            InternalCSGModelManager.FindParentOperationAndModel(
                brush.transform, out CSGOperation _, out CSGModel parentModel
            );

            Matrix4x4 localFromWorld = brush.transform.worldToLocalMatrix;
            Matrix4x4 worldFromModel = parentModel.transform.localToWorldMatrix;
            Matrix4x4 modelFromWorld = parentModel.transform.worldToLocalMatrix;
            Matrix4x4 localFromModel = localFromWorld * worldFromModel;

            // convert planes into worldspace
            CSGPlane dstWorldPlane = InverseTransformPlane(localFromWorld, dstLocalPlane);
            CSGPlane srcWorldPlane = InverseTransformPlane(localFromWorld, srcLocalPlane);

            Vector3 dstWorldNormal = dstWorldPlane.normal;
            Vector3 srcWorldNormal = srcWorldPlane.normal;

            shape.TexGens[dstSurfaceIndex].RenderMaterial = shape.TexGens[srcSurfaceIndex].RenderMaterial;
            Vector3 srcWorldPoint1, srcWorldPoint2;
            Vector3 dstWorldPoint1, dstWorldPoint2;
            Vector3 edgeDirection = Vector3.Cross(dstWorldNormal, srcWorldNormal);
            float   det           = edgeDirection.sqrMagnitude;

            if(det < MathConstants.AlignmentTestEpsilon)
            {
                // Find 2 points on intersection of 2 planes
                srcWorldPoint1 = srcWorldPlane.pointOnPlane;
                srcWorldPoint2 = ProjectPointOnPlane(srcWorldPlane, srcWorldPoint1 + MathConstants.oneVector3);

                dstWorldPoint1 = ProjectPointOnPlane(dstWorldPlane, srcWorldPoint1);
                dstWorldPoint2 = ProjectPointOnPlane(dstWorldPlane, srcWorldPoint2);
            }
            else
            {
                // Find 2 points on intersection of 2 planes
                srcWorldPoint1 = (Vector3.Cross(edgeDirection, srcWorldNormal) * -dstWorldPlane.d +
                    Vector3.Cross(dstWorldNormal, edgeDirection) * -srcWorldPlane.d) / det;

                srcWorldPoint2 = srcWorldPoint1 + edgeDirection;
                dstWorldPoint1 = srcWorldPoint1;
                dstWorldPoint2 = srcWorldPoint2;
            }

            Vector3 srcModelPoint1 = modelFromWorld.MultiplyPoint(srcWorldPoint1);
            Vector3 srcModelPoint2 = modelFromWorld.MultiplyPoint(srcWorldPoint2);

            Vector3 dstModelPoint1 = modelFromWorld.MultiplyPoint(dstWorldPoint1);
            Vector3 dstModelPoint2 = modelFromWorld.MultiplyPoint(dstWorldPoint2);

            bool result = SurfaceUtility.AlignTextureSpaces(
                localFromModel, texGens[srcTexGenIndex], texGenFlags[srcTexGenIndex], ref surfaces[srcSurfaceIndex]
              , srcModelPoint1, srcModelPoint2,
                localFromModel, ref texGens[dstTexGenIndex], texGenFlags[dstTexGenIndex], ref surfaces[dstSurfaceIndex]
              , dstModelPoint1, dstModelPoint2, false, Vector3.one
            );

            return result;
        }
    }
}

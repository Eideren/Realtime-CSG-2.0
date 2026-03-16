using System;
using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using System.Collections.Generic;

namespace RealtimeCSG
{
    internal sealed class Frustum
    {
        public readonly Plane[] Planes = new Plane[6];
    }

    internal static class CameraUtility
    {
        public static Frustum GetCameraSubFrustumScreen(Camera camera, Rect rect)
        {
            Matrix4x4 oldMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            float min_x = rect.x;
            float max_x = rect.x + rect.width;
            float min_y = rect.y;
            float max_y = rect.y + rect.height;

            Vector2 o0 = new Vector2(min_x, min_y);
            Vector2 o1 = new Vector2(max_x, min_y);
            Vector2 o2 = new Vector2(max_x, max_y);
            Vector2 o3 = new Vector2(min_x, max_y);

            Ray r0 = camera.ScreenPointToRay(o0);
            Ray r1 = camera.ScreenPointToRay(o1);
            Ray r2 = camera.ScreenPointToRay(o2);
            Ray r3 = camera.ScreenPointToRay(o3);
            Handles.matrix = oldMatrix;

            Vector3 n0 = r0.origin;
            Vector3 n1 = r1.origin;
            Vector3 n2 = r2.origin;
            Vector3 n3 = r3.origin;

            float   far = camera.farClipPlane;
            Vector3 f0  = n0 + r0.direction * far;
            Vector3 f1  = n1 + r1.direction * far;
            Vector3 f2  = n2 + r2.direction * far;
            Vector3 f3  = n3 + r3.direction * far;

            Frustum frustum = new Frustum();
            frustum.Planes[0] = new Plane(n2, n1, f1); // right  +
            frustum.Planes[1] = new Plane(f3, f0, n0); // left   -
            frustum.Planes[2] = new Plane(n1, n0, f0); // top    -
            frustum.Planes[3] = new Plane(n3, n2, f2); // bottom +
            frustum.Planes[4] = new Plane(n0, n1, n2); // near   -
            frustum.Planes[5] = new Plane(f2, f1, f0); // far    +

            return frustum;
        }

        public static Frustum GetCameraSubFrustumGUI(Camera camera, Rect rect)
        {
            Matrix4x4 oldMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            float min_x = rect.x;
            float max_x = rect.x + rect.width;
            float min_y = rect.y;
            float max_y = rect.y + rect.height;

            Vector2 o0 = new Vector2(min_x, min_y);
            Vector2 o1 = new Vector2(max_x, min_y);
            Vector2 o2 = new Vector2(max_x, max_y);
            Vector2 o3 = new Vector2(min_x, max_y);

            Ray r0 = HandleUtility.GUIPointToWorldRay(o0);
            Ray r1 = HandleUtility.GUIPointToWorldRay(o1);
            Ray r2 = HandleUtility.GUIPointToWorldRay(o2);
            Ray r3 = HandleUtility.GUIPointToWorldRay(o3);
            Handles.matrix = oldMatrix;

            Vector3 n0 = r0.origin;
            Vector3 n1 = r1.origin;
            Vector3 n2 = r2.origin;
            Vector3 n3 = r3.origin;

            float   far = camera.farClipPlane;
            Vector3 f0  = n0 + r0.direction * far;
            Vector3 f1  = n1 + r1.direction * far;
            Vector3 f2  = n2 + r2.direction * far;
            Vector3 f3  = n3 + r3.direction * far;

            Frustum frustum = new Frustum();
            frustum.Planes[0] = new Plane(n2, n1, f1); // right  +
            frustum.Planes[1] = new Plane(f3, f0, n0); // left   -
            frustum.Planes[2] = new Plane(n1, n0, f0); // top    -
            frustum.Planes[3] = new Plane(n3, n2, f2); // bottom +
            frustum.Planes[4] = new Plane(n0, n1, n2); // near   -
            frustum.Planes[5] = new Plane(f2, f1, f0); // far    +

            return frustum;
        }

        public static Rect PointsToRect(Vector2 start, Vector2 end)
        {
            start.x = Mathf.Max(start.x, 0);
            start.y = Mathf.Max(start.y, 0);
            end.x   = Mathf.Max(end.x, 0);
            end.y   = Mathf.Max(end.y, 0);
            Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);

            if(r.width < 0)
            {
                r.x     += r.width;
                r.width =  -r.width;
            }

            if(r.height < 0)
            {
                r.y      += r.height;
                r.height =  -r.height;
            }

            return r;
        }

        public static Vector2 PhysicalOffset(SceneView sceneview)
        {
            Vector2 slow = WorldToGUIPointSlow(sceneview.camera, Vector3.zero);
            Vector2 fast = HandleUtility.WorldToGUIPoint(Vector3.zero);

            return slow - fast;
        }

        private static Vector3 rightVector;
        private static Vector2 physicalOffset;
        private static Matrix4x4 guiInverseMatrix;
        private static Matrix4x4 handleMatrix;
        private static Matrix4x4 worldToClipMatrix;
        private static Rect cameraViewport;
        private static float pixelsPerPoint;
        private static float screenHeight;
        private static Matrix4x4 worldToScreenPointMatrix;

        private static Matrix4x4 localToGUIPointMatrix;
        //static Camera       prevCamera;

        private static void UpdateCameraValues(Camera camera, SceneView sceneview)
        {
            rightVector      = camera.transform.right;
            handleMatrix     = Handles.matrix;
            guiInverseMatrix = Matrix4x4.Inverse(GUI.matrix);

            cameraViewport = camera.pixelRect;

            worldToClipMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
            pixelsPerPoint    = 1.0f / EditorGUIUtility.pixelsPerPoint;
            screenHeight      = Screen.height;

            physicalOffset = Vector3.zero; // the call below uses this
            physicalOffset = PhysicalOffset(sceneview);

            float halfWidth  = cameraViewport.width * 0.5f;
            float halfHeight = cameraViewport.height * 0.5f;
            float viewportX  = cameraViewport.x;
            float viewportY  = cameraViewport.y;

            worldToScreenPointMatrix = Matrix4x4.TRS(
                    new Vector3(viewportX, viewportY, 0), Quaternion.identity, Vector3.one
                ) *
                Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(halfWidth, halfHeight, 1)) *
                Matrix4x4.TRS(new Vector3(1, 1, 0), Quaternion.identity, Vector3.one) *
                worldToClipMatrix;

            localToGUIPointMatrix = Matrix4x4.TRS(
                    new Vector3(-physicalOffset.x, -physicalOffset.y, 1), Quaternion.identity, Vector3.one
                ) *
                guiInverseMatrix *
                Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(pixelsPerPoint, pixelsPerPoint, 1)) *
                Matrix4x4.TRS(new Vector3(0, screenHeight, 0), Quaternion.identity, Vector3.one) *
                Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, -1, 1)) *
                worldToScreenPointMatrix *
                handleMatrix;
        }

        public static void InitDistanceChecks(SceneView sceneview)
        {
            Camera camera = sceneview.camera;
            UpdateCameraValues(camera, sceneview);
            //prevCamera = camera;
        }

        public static float DistanceToLine(CSGPlane cameraPlane, Vector2 mousePoint, Vector3 point1, Vector3 point2)
        {
            float dist1 = cameraPlane.Distance(point1);
            float dist2 = cameraPlane.Distance(point2);

            if(dist1 < 0 && dist2 > 0)
            {
                point1 = cameraPlane.LineIntersection(point1, point2);
            }
            else if(dist2 < 0 && dist1 > 0)
            {
                point2 = cameraPlane.LineIntersection(point1, point2);
            }

            float value = DistancePointLine(mousePoint, point1, point2) / 2.5f; // * 0.5f;

            return value;
        }

        private static Vector3 ProjectPointLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 relativePoint           = point - lineStart;
            Vector3 lineDirection           = lineEnd - lineStart;
            float   length                  = lineDirection.magnitude;
            Vector3 normalizedLineDirection = lineDirection;

            if(length > .000001f)
            {
                normalizedLineDirection /= length;
            }

            float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
            dot = Mathf.Clamp(dot, 0.0F, length);

            return lineStart + normalizedLineDirection * dot;
        }

        private static Vector2 ProjectPointLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            Vector3 relativePoint           = point - lineStart;
            Vector3 lineDirection           = lineEnd - lineStart;
            float   length                  = lineDirection.magnitude;
            Vector2 normalizedLineDirection = lineDirection;

            if(length > .000001f)
            {
                normalizedLineDirection /= length;
            }

            float dot = Vector3.Dot(normalizedLineDirection, relativePoint);
            dot = Mathf.Clamp(dot, 0.0F, length);

            return lineStart + normalizedLineDirection * dot;
        }

        public static float DistancePointLine(Vector2 point, Vector3 p1, Vector3 p2)
        {
            Vector2 p1B = WorldToGUIPoint(p1);
            Vector2 p2B = WorldToGUIPoint(p2);

            Vector2 proj   = ProjectPointLine(point, p1B, p2B) - point; // / (EditorGUIUtility.pixelsPerPoint * 2.5f);
            float   retval = Vector3.Magnitude(proj);                   // / (EditorGUIUtility.pixelsPerPoint * 2.5f);

            if(retval < 0)
            {
                retval = 0.0f;
            }

            return retval;
        }

        public static float DistancePointLine(Vector3 point, Vector3 p1, Vector3 p2)
        {
            float retval =
                Vector3.Magnitude(
                    ProjectPointLine(point, p1, p2) - point
                ); // / (EditorGUIUtility.pixelsPerPoint * 2.5f);

            if(retval < 0)
            {
                retval = 0.0f;
            }

            return retval;
        }

        // Pixel distance from mouse pointer to camera facing circle.
        public static float DistanceToCircle(Vector3 position, float radius)
        {
            Vector3 position2 = position + rightVector * radius;

            Vector2 screenCenter;
            Vector2 screenEdge;

            if(!PerspectiveMultiplyPoint2(localToGUIPointMatrix, position, out screenCenter))
            {
                screenCenter = Vector2.zero;
            }

            if(!PerspectiveMultiplyPoint2(localToGUIPointMatrix, position2, out screenEdge))
            {
                screenEdge = Vector2.zero;
            }

            radius = (screenCenter - screenEdge).sqrMagnitude;

            float dist = (screenCenter - Event.current.mousePosition).sqrMagnitude;

            if(dist < radius)
            {
                return 0;
            }

            return Mathf.Sqrt(dist - radius);
        }

        public static Vector2 WorldToGUIPoint(Vector3 local)
        {
            Vector2 pos;

            if(!PerspectiveMultiplyPoint2(localToGUIPointMatrix, local, out pos))
            {
                return Vector2.zero;
            }

            return pos;
        }

        private static Vector2 WorldToScreenPoint2D(Vector3 v)
        {
            Vector2 clipPoint;

            if(!PerspectiveMultiplyPoint2(worldToScreenPointMatrix, v, out clipPoint))
            {
                return Vector2.zero;
            }

            return clipPoint;
        }

        private static bool PerspectiveMultiplyPoint2(Matrix4x4 m, Vector2 v, out Vector2 output)
        {
            Vector3 res;
            float   w;
            res.x = m.m00 * v.x + m.m01 * v.y + m.m03;
            res.y = m.m10 * v.x + m.m11 * v.y + m.m13;
            w     = m.m30 * v.x + m.m31 * v.y + m.m33;

            if(Mathf.Abs(w) > 1.0e-7f)
            {
                float invW = 1.0f / w;
                output.x = res.x * invW;
                output.y = res.y * invW;

                return true;
            }
            else
            {
                output.x = 0.0f;
                output.y = 0.0f;

                return false;
            }
        }

        private static bool PerspectiveMultiplyPoint2(Matrix4x4 m, Vector3 v, out Vector2 output)
        {
            Vector3 res;
            float   w;
            res.x = m.m00 * v.x + m.m01 * v.y + m.m02 * v.z + m.m03;
            res.y = m.m10 * v.x + m.m11 * v.y + m.m12 * v.z + m.m13;
            w     = m.m30 * v.x + m.m31 * v.y + m.m32 * v.z + m.m33;

            if(Mathf.Abs(w) > 1.0e-7f)
            {
                float invW = 1.0f / w;
                output.x = res.x * invW;
                output.y = res.y * invW;

                return true;
            }
            else
            {
                output.x = 0.0f;
                output.y = 0.0f;

                return false;
            }
        }

        private static Vector2 WorldToGUIPointSlow(Camera camera, Vector3 world)
        {
            Vector3 pos = camera.WorldToScreenPoint(handleMatrix.MultiplyPoint(world));

            pos.y =  screenHeight - pos.y;
            pos.x *= pixelsPerPoint;
            pos.y *= pixelsPerPoint;

            Vector2 transformedPoint;
            PerspectiveMultiplyPoint2(guiInverseMatrix, new Vector3(pos.x, pos.y, 0.0F), out transformedPoint);

            return transformedPoint - physicalOffset;
        }
    }
}

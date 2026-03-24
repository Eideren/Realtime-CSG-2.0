using System;
using RealtimeCSG.Legacy;
using UnityEngine;

namespace RealtimeCSG
{
    internal class RotationCircle
    {
        public bool haveRotateStartAngle;
        public Vector3 rotateStartVector;
        public Vector3 rotateCenterPoint;
        public Vector3 rotateSurfaceNormal;
        public Vector3 rotateSurfaceTangent;
        public float rotateRadius;
        public float rotateOriginalAngle;
        public float rotateCurrentStartAngle;
        public float rotateCurrentSnappedAngle;

        public void Clear()
        {
            haveRotateStartAngle      = false;
            rotateRadius              = 0;
            rotateCurrentStartAngle   = 0;
            rotateCurrentSnappedAngle = 0;
        }

        public void Render(Camera camera)
        {
            float endAngle = rotateCurrentStartAngle + rotateCurrentSnappedAngle;

            if(haveRotateStartAngle)
            {
                PaintUtility.DrawRotateCirclePie(rotateCenterPoint, rotateSurfaceNormal, rotateSurfaceTangent, rotateRadius, rotateCurrentStartAngle, endAngle, ColorSettings.RotateCircleOutline);
            }

            PaintUtility.DrawRotateCircle(camera, rotateCenterPoint, rotateSurfaceNormal, rotateSurfaceTangent, rotateRadius, rotateOriginalAngle, rotateCurrentStartAngle, endAngle, ColorSettings.RotateCircleOutline);
        }

        public void Initialize(Transform transform, Surface surface, Vector3 centerPoint)
        {
            rotateSurfaceNormal  = transform.TransformVector(surface.Plane.normal);
            rotateSurfaceTangent = transform.TransformVector(surface.Tangent);
            rotateCenterPoint    = centerPoint;
            Clear();
        }

        public bool UpdateRadius(TexGen surfaceTexGen, Vector3 currentSurfacePoint)
        {
            float   handleSize     = CSGHandleUtility.GetHandleSize(rotateCenterPoint);
            Vector3 vectorToCenter = currentSurfacePoint - rotateCenterPoint;
            rotateRadius = vectorToCenter.magnitude;
            vectorToCenter.Normalize();

            float rotateCurrentAngle = 0.0f;

            if(!haveRotateStartAngle)
            {
                if(rotateRadius > handleSize * GUIConstants.MIN_ROTATE_RADIUS)
                {
                    haveRotateStartAngle = true;
                    rotateOriginalAngle  = surfaceTexGen.RotationAngle;
                    rotateStartVector    = vectorToCenter;

                    rotateCurrentStartAngle = GeometryUtility.SignedAngle(rotateSurfaceTangent, vectorToCenter, rotateSurfaceNormal);
                }

                rotateRadius = Math.Max(rotateRadius, handleSize * GUIConstants.MIN_ROTATE_RADIUS);
            }
            else
            {
                rotateCurrentAngle = GeometryUtility.SignedAngle(rotateStartVector, vectorToCenter, rotateSurfaceNormal);

                float minSize = handleSize * GUIConstants.MIN_ROTATE_RADIUS * 2;

                rotateRadius = Mathf.CeilToInt((rotateRadius - minSize) / minSize - 0.5f) * minSize + minSize;
            }

            // snap texture coordinates in world/local space
            rotateCurrentSnappedAngle = GridUtility.SnappedAngle(rotateOriginalAngle + rotateCurrentAngle) - rotateOriginalAngle;

            return haveRotateStartAngle;
        }
    }
}

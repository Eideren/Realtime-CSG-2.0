using System;
using UnityEngine;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal class RotationCircle
    {
        public bool HaveRotateStartAngle = false;
        public Vector3 RotateStartVector;
        public Vector3 RotateCenterPoint;
        public Vector3 RotateSurfaceNormal;
        public Vector3 RotateSurfaceTangent;
        public float RotateRadius;
        public float RotateOriginalAngle;
        public float RotateCurrentStartAngle;
        public float RotateCurrentSnappedAngle;

        public void Clear()
        {
            HaveRotateStartAngle      = false;
            RotateRadius              = 0;
            RotateCurrentStartAngle   = 0;
            RotateCurrentSnappedAngle = 0;
        }

        public void Render(Camera camera)
        {
            float endAngle = RotateCurrentStartAngle + RotateCurrentSnappedAngle;

            if(HaveRotateStartAngle)
            {
                PaintUtility.DrawRotateCirclePie(
                    RotateCenterPoint, RotateSurfaceNormal, RotateSurfaceTangent, RotateRadius,
                    RotateOriginalAngle, RotateCurrentStartAngle, endAngle,
                    ColorSettings.RotateCircleOutline
                ); //, RotateCirclePieFill, ColorSettings.RotateCirclePieOutline);
            }

            PaintUtility.DrawRotateCircle(
                camera, RotateCenterPoint, RotateSurfaceNormal, RotateSurfaceTangent, RotateRadius,
                RotateOriginalAngle, RotateCurrentStartAngle, endAngle,
                ColorSettings.RotateCircleOutline
            ); //, ColorSettings.RotateCircleHatches);
        }

        public void Initialize(Transform transform, Surface surface, Vector3 centerPoint)
        {
            RotateSurfaceNormal  = transform.TransformVector(surface.Plane.normal);
            RotateSurfaceTangent = transform.TransformVector(surface.Tangent);
            RotateCenterPoint    = centerPoint;
            Clear();
        }

        public bool UpdateRadius(TexGen surfaceTexGen, Vector3 currentSurfacePoint)
        {
            float   handleSize     = CSG_HandleUtility.GetHandleSize(RotateCenterPoint);
            Vector3 vectorToCenter = currentSurfacePoint - RotateCenterPoint;
            RotateRadius = vectorToCenter.magnitude;
            vectorToCenter.Normalize();

            float rotateCurrentAngle = 0.0f;

            if(!HaveRotateStartAngle)
            {
                if(RotateRadius > handleSize * GUIConstants.MIN_ROTATE_RADIUS)
                {
                    HaveRotateStartAngle = true;
                    RotateOriginalAngle  = surfaceTexGen.RotationAngle;
                    RotateStartVector    = vectorToCenter;

                    RotateCurrentStartAngle = GeometryUtility.SignedAngle(
                        RotateSurfaceTangent, vectorToCenter, RotateSurfaceNormal
                    );
                }

                RotateRadius = Math.Max(RotateRadius, handleSize * GUIConstants.MIN_ROTATE_RADIUS);
            }
            else
            {
                rotateCurrentAngle = GeometryUtility.SignedAngle(
                    RotateStartVector, vectorToCenter, RotateSurfaceNormal
                );

                float minSize        = handleSize * GUIConstants.MIN_ROTATE_RADIUS * 2;
                float radiusStepSize = minSize;

                RotateRadius = Mathf.CeilToInt((RotateRadius - minSize) / radiusStepSize - 0.5f) * radiusStepSize
                  + minSize;
            }

            // snap texture coordinates in world/local space
            RotateCurrentSnappedAngle = GridUtility.SnappedAngle(RotateOriginalAngle + rotateCurrentAngle)
              - RotateOriginalAngle;

            return HaveRotateStartAngle;
        }
    }
}

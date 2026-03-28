using UnityEngine;

namespace InternalRealtimeCSG
{
    internal static class TransformOperations
    {
        public static void Rotate(this TransformSelection selection, Vector3 rotationCenter, Vector3 rotationAxis, float rotationAngle)
        {
            Quaternion rotationQuaternion = Quaternion.AngleAxis(rotationAngle, rotationAxis);

            if(Mathf.Abs(rotationAngle) < MathConstants.EQUALITY_EPSILON)
            {
                for(int t = 0; t < selection.transforms.Length; t++)
                {
                    Transform targetTransform = selection.transforms[t];
                    targetTransform.position = selection.backupPositions[t];
                    targetTransform.rotation = selection.backupRotations[t];
                }
            }
            else
            {
                for(int t = 0; t < selection.transforms.Length; t++)
                {
                    Transform targetTransform = selection.transforms[t];
                    Vector3   originalCenter  = targetTransform.InverseTransformPoint(rotationCenter);
                    targetTransform.position = selection.backupPositions[t];
                    targetTransform.rotation = rotationQuaternion * selection.backupRotations[t];
                    Vector3 newCenter = targetTransform.TransformPoint(originalCenter);
                    targetTransform.position += rotationCenter - newCenter;
                }
            }
        }
    }
}

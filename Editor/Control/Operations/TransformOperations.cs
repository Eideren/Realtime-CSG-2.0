using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
    internal static class TransformOperations
    {
        public static void Rotate(
            this TransformSelection selection, Vector3 rotationCenter, Vector3 rotationAxis, float rotationAngle
        )
        {
            Quaternion rotationQuaternion = Quaternion.AngleAxis(rotationAngle, rotationAxis);

            if(Mathf.Abs(rotationAngle) < MathConstants.EqualityEpsilon)
            {
                for(int t = 0; t < selection.Transforms.Length; t++)
                {
                    Transform targetTransform = selection.Transforms[t];
                    targetTransform.position = selection.BackupPositions[t];
                    targetTransform.rotation = selection.BackupRotations[t];
                }
            }
            else
            {
                for(int t = 0; t < selection.Transforms.Length; t++)
                {
                    Transform targetTransform = selection.Transforms[t];
                    Vector3   originalCenter  = targetTransform.InverseTransformPoint(rotationCenter);
                    targetTransform.position = selection.BackupPositions[t];
                    targetTransform.rotation = rotationQuaternion * selection.BackupRotations[t];
                    Vector3 newCenter = targetTransform.TransformPoint(originalCenter);
                    targetTransform.position += rotationCenter - newCenter;
                }
            }
        }

        public static void Translate(this TransformSelection selection, Vector3 translation)
        {
            for(int t = 0; t < selection.Transforms.Length; t++)
            {
                selection.Transforms[t].position =
                    GridUtility.CleanPosition(selection.BackupPositions[t] + translation);
            }
        }
    }
}

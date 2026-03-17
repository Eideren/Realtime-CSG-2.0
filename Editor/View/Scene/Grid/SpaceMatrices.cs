// * * * * * * * * * * * * * * * * * * * * * *
//  Description:
// 
// * * * * * * * * * * * * * * * * * * * * * *

using System;
using InternalRealtimeCSG;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class SpaceMatrices
    {
        public Matrix4x4 activeLocalToWorld = MathConstants.identityMatrix;
        public Matrix4x4 activeWorldToLocal = MathConstants.identityMatrix;
        public Matrix4x4 modelLocalToWorld = MathConstants.identityMatrix;
        public Matrix4x4 modelWorldToLocal = MathConstants.identityMatrix;

        public static SpaceMatrices Create(Transform transform)
        {
            SpaceMatrices spaceMatrices = new SpaceMatrices();

            if(transform == null ||
               Tools.pivotRotation == PivotRotation.Global)
            {
                return spaceMatrices;
            }

            spaceMatrices.activeLocalToWorld = transform.localToWorldMatrix;
            spaceMatrices.activeWorldToLocal = transform.worldToLocalMatrix;

            Transform model = InternalCSGModelManager.FindModelTransform(transform);

            if(model != null)
            {
                spaceMatrices.modelLocalToWorld = model.localToWorldMatrix;
                spaceMatrices.modelWorldToLocal = model.worldToLocalMatrix;
            }

            return spaceMatrices;
        }
    };
}

using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEngine;

namespace InternalRealtimeCSG
{
    internal static class ControlMeshOperations
    {
        public static void TranslateControlPoints(this BrushSelection selection, Vector3 translation)
        {
            for(int t = 0; t < selection.brushes.Length; t++)
            {
                ControlMesh targetControlMesh = selection.backupControlMeshes[t].Clone();

                if(!targetControlMesh.Valid)
                {
                    targetControlMesh.Valid = ControlMeshUtility.Validate(targetControlMesh, selection.shapes[t]);
                }

                if(!targetControlMesh.Valid)
                {
                    continue;
                }

                CSGBrush         targetBrush     = selection.brushes[t];
                ControlMeshState targetMeshState = selection.states[t];

                if(targetMeshState.backupPoints == null)
                {
                    continue;
                }

                Shape     targetShape        = selection.shapes[t].Clone();
                Matrix4x4 worldToLocalMatrix = targetMeshState.brushTransform.worldToLocalMatrix;
                Vector3   localDeltaMovement = GridUtility.CleanPosition(worldToLocalMatrix.MultiplyVector(translation));

                HashSet<short> selectedIndices = targetMeshState.GetSelectedPointIndices();

                for(int p = 0; p < targetMeshState.worldPoints.Length; p++)
                {
                    if(!selectedIndices.Contains((short)p))
                    {
                        continue;
                    }

                    targetControlMesh.vertices[p] = GridUtility.CleanPosition(targetMeshState.backupPoints[p] + localDeltaMovement);
                }

                ControlMeshUtility.RebuildShapeFrom(targetBrush, targetControlMesh, targetShape);
                selection.controlMeshes[t] = targetControlMesh;
            }
        }

        public static void TransformControlPoints(this BrushSelection selection, Matrix4x4 transform)
        {
            for(int t = 0; t < selection.brushes.Length; t++)
            {
                ControlMesh targetControlMesh = selection.backupControlMeshes[t].Clone();

                if(!targetControlMesh.Valid)
                {
                    targetControlMesh.Valid = ControlMeshUtility.Validate(targetControlMesh, selection.shapes[t]);
                }

                if(!targetControlMesh.Valid)
                {
                    continue;
                }

                CSGBrush         targetBrush     = selection.brushes[t];
                ControlMeshState targetMeshState = selection.states[t];

                if(targetMeshState.backupPoints == null)
                {
                    continue;
                }

                Shape     targetShape        = selection.shapes[t].Clone();
                Matrix4x4 localToWorldMatrix = targetMeshState.brushTransform.localToWorldMatrix;
                Matrix4x4 worldToLocalMatrix = targetMeshState.brushTransform.worldToLocalMatrix;

                Matrix4x4 localCombinedMatrix = worldToLocalMatrix * transform * localToWorldMatrix;

                HashSet<short> selectedIndices = targetMeshState.GetSelectedPointIndices();

                for(int p = 0; p < targetMeshState.backupPoints.Length; p++)
                {
                    if(!selectedIndices.Contains((short)p))
                    {
                        continue;
                    }

                    Vector3 point = targetMeshState.backupPoints[p];

                    point = localCombinedMatrix.MultiplyPoint(point);

                    targetControlMesh.vertices[p] = GridUtility.CleanPosition(point);
                }

                selection.controlMeshes[t] = targetControlMesh;

                // This might create new controlmesh / shape
                ControlMeshUtility.MergeDuplicatePoints(targetBrush, ref targetControlMesh, ref targetShape);

                if(targetControlMesh.Valid)
                {
                    targetControlMesh.SetDirty();
                    ControlMeshUtility.RebuildShapeFrom(targetBrush, targetControlMesh, targetShape);
                }
            }
        }

        public static void RotateControlPoints(this BrushSelection selection, Vector3 center, Quaternion handleRotation, Quaternion rotationOffset)
        {
            Quaternion inverseHandleRotation = Quaternion.Inverse(handleRotation);

            Matrix4x4 rotationMatrix        = Matrix4x4.TRS(Vector3.zero, handleRotation, Vector3.one);
            Matrix4x4 inverseRotationMatrix = Matrix4x4.TRS(Vector3.zero, inverseHandleRotation, Vector3.one);
            Matrix4x4 actionMatrix          = Matrix4x4.TRS(Vector3.zero, rotationOffset, Vector3.one);
            Matrix4x4 moveMatrix            = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
            Matrix4x4 inverseMoveMatrix     = Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);

            Matrix4x4 combinedMatrix = moveMatrix * rotationMatrix * actionMatrix * inverseRotationMatrix * inverseMoveMatrix;

            selection.TransformControlPoints(combinedMatrix);
        }

        public static void ScaleControlPoints(this BrushSelection selection, Vector3 center, Quaternion rotation, Vector3 scale)
        {
            if(float.IsInfinity(scale.x) || float.IsNaN(scale.x) || float.IsInfinity(scale.y) || float.IsNaN(scale.y) || float.IsInfinity(scale.z) || float.IsNaN(scale.z))
            {
                scale = Vector3.zero;
            }

            if(scale.x <= MathConstants.EQUALITY_EPSILON)
            {
                scale.x = 0.0f;
            }

            if(scale.y <= MathConstants.EQUALITY_EPSILON)
            {
                scale.y = 0.0f;
            }

            if(scale.z <= MathConstants.EQUALITY_EPSILON)
            {
                scale.z = 0.0f;
            }

            Quaternion inverseRotation = Quaternion.Inverse(rotation);

            Matrix4x4 rotationMatrix        = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);
            Matrix4x4 inverseRotationMatrix = Matrix4x4.TRS(Vector3.zero, inverseRotation, Vector3.one);
            Matrix4x4 scaleMatrix           = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);
            Matrix4x4 moveMatrix            = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one);
            Matrix4x4 inverseMoveMatrix     = Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);

            Matrix4x4 combinedMatrix = moveMatrix * rotationMatrix * scaleMatrix * inverseRotationMatrix * inverseMoveMatrix;

            selection.TransformControlPoints(combinedMatrix);
        }

        public static void CleanPoints(this BrushSelection selection)
        {
            bool brushModified = false;

            for(int t = 0; t < selection.states.Length; t++)
            {
                CSGBrush brush = selection.brushes[t];

                if(!brush)
                {
                    continue;
                }

                ControlMesh controlMesh    = brush.controlMesh;
                Vector3[]   points         = controlMesh.vertices;
                bool        vertexModified = false;

                for(int i = 0; i < points.Length; i++)
                {
                    Vector3 cleanedPoint = GridUtility.CleanPosition(points[i]);

                    if(controlMesh.vertices[i].x == cleanedPoint.x && controlMesh.vertices[i].y == cleanedPoint.y && controlMesh.vertices[i].z == cleanedPoint.z)
                    {
                        continue;
                    }

                    controlMesh.vertices[i] = cleanedPoint;
                    vertexModified          = true;
                }

                if(vertexModified)
                {
                    brush.controlMesh.SetDirty();
                    brushModified = true;
                }
            }

            if(brushModified)
            {
                InternalCSGModelManager.CheckForChanges();
            }
        }

        public static void PointSnapToGrid(this BrushSelection selection, Camera camera)
        {
            Vector3 minWorldDeltaMovement = MathConstants.positiveInfinityVector3;

            for(int t = 0; t < selection.states.Length; t++)
            {
                CSGBrush brush = selection.brushes[t];

                if(!brush)
                {
                    continue;
                }

                ControlMeshState controlMeshState  = selection.states[t];
                Transform        brushTransform    = brush.GetComponent<Transform>();
                Matrix4x4        brushLocalToWorld = brushTransform.localToWorldMatrix;
                Matrix4x4        brushWorldToLocal = brushTransform.worldToLocalMatrix;

                ControlMesh controlMesh = brush.controlMesh;
                Vector3[]   points      = controlMesh.vertices;

                List<Vector3>  localPoints     = new List<Vector3>(points.Length);
                HashSet<short> selectedIndices = controlMeshState.GetSelectedPointIndices();

                foreach(short index in selectedIndices)
                {
                    localPoints.Add(points[index]);
                }

                if(localPoints.Count > 0)
                {
                    Vector3 worldDeltaMovement = CSGGrid.SnapLocalPointToWorldGridDelta(camera, brushLocalToWorld, brushWorldToLocal, localPoints.ToArray());

                    if(Mathf.Abs(worldDeltaMovement.x) < Mathf.Abs(minWorldDeltaMovement.x))
                    {
                        minWorldDeltaMovement.x = worldDeltaMovement.x;
                    }

                    if(Mathf.Abs(worldDeltaMovement.y) < Mathf.Abs(minWorldDeltaMovement.y))
                    {
                        minWorldDeltaMovement.y = worldDeltaMovement.y;
                    }

                    if(Mathf.Abs(worldDeltaMovement.z) < Mathf.Abs(minWorldDeltaMovement.z))
                    {
                        minWorldDeltaMovement.z = worldDeltaMovement.z;
                    }
                }
            }

            if(float.IsInfinity(minWorldDeltaMovement.x))
            {
                return; // nothing to do
            }

            bool brushModified = false;

            for(int t = 0; t < selection.states.Length; t++)
            {
                CSGBrush brush = selection.brushes[t];

                if(!brush)
                {
                    continue;
                }

                ControlMeshState controlMeshState = selection.states[t];

                Transform brushTransform    = brush.GetComponent<Transform>();
                Matrix4x4 brushLocalToWorld = brushTransform.localToWorldMatrix;
                Matrix4x4 brushWorldToLocal = brushTransform.worldToLocalMatrix;

                ControlMesh    controlMesh     = brush.controlMesh;
                Vector3[]      points          = controlMesh.vertices;
                HashSet<short> selectedIndices = controlMeshState.GetSelectedPointIndices();
                bool           vertexModified  = false;

                foreach(short index in selectedIndices)
                {
                    Vector3 worldPoint = brushLocalToWorld.MultiplyPoint(points[index]);
                    worldPoint += minWorldDeltaMovement;
                    Vector3 localPoint   = brushWorldToLocal.MultiplyPoint(worldPoint);
                    Vector3 snappedPoint = GridUtility.CleanPosition(localPoint);

                    if(controlMesh.vertices[index].x == snappedPoint.x && controlMesh.vertices[index].y == snappedPoint.y && controlMesh.vertices[index].z == snappedPoint.z)
                    {
                        continue;
                    }

                    controlMesh.vertices[index] = snappedPoint;
                    vertexModified              = true;
                }

                if(vertexModified)
                {
                    brush.controlMesh.SetDirty();
                    brushModified = true;
                }
            }

            if(brushModified)
            {
                InternalCSGModelManager.CheckForChanges();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal static class BrushOperations
    {
        public static void SnapToGrid(Camera camera, CSGBrush[] brushes)
        {
            Vector3 worldDeltaMovement = MathConstants.zeroVector3;

            List<Transform> transforms = new List<Transform>();

            for(int b = 0; b < brushes.Length; b++)
            {
                if(brushes[b] == null ||
                   !brushes[b])
                {
                    continue;
                }

                Transform brushTransform    = brushes[b].GetComponent<Transform>();
                Matrix4x4 brushLocalToWorld = brushTransform.localToWorldMatrix;

                transforms.Add(brushTransform);

                ControlMesh controlMesh = brushes[b].ControlMesh;
                Vector3[]   points      = controlMesh.Vertices;

                Vector3[] worldPoints = new Vector3[points.Length];

                for(int p = 0; p < points.Length; p++)
                {
                    worldPoints[p] = brushLocalToWorld.MultiplyPoint(points[p]);
                }

                worldDeltaMovement = CSGGrid.SnapDeltaToGrid(
                    camera, worldDeltaMovement, worldPoints.ToArray(), false, false
                );
            }

            if(worldDeltaMovement.x == 0.0f && worldDeltaMovement.y == 0.0f && worldDeltaMovement.z == 0.0f)
            {
                return;
            }

            for(int b = 0; b < transforms.Count; b++)
            {
                Transform transform = transforms[b];
                transform.position = GridUtility.CleanPosition(transform.position + worldDeltaMovement);
            }
        }

        public static void Flip(CSGBrush[] brushes, Matrix4x4 flipMatrix, string undoDescription = "Flip brushes")
        {
            bool fail = false;
            Undo.IncrementCurrentGroup();
            Undo.RegisterCompleteObjectUndo(brushes.ToArray<UnityEngine.Object>(), undoDescription);

            bool isGlobal = Tools.pivotRotation == PivotRotation.Global;

            Vector3 centerAll = BoundsUtilities.GetCenter(brushes);

            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush brush    = brushes[t];
                Vector3  position = brush.transform.position;

                Matrix4x4 brushFlip;
                Vector3   brushCenter;

                if(isGlobal)
                {
                    brushFlip = brush.transform.localToWorldMatrix *
                        flipMatrix *
                        brush.transform.worldToLocalMatrix;

                    brushCenter = brush.transform.InverseTransformPoint(centerAll) - position;
                }
                else
                {
                    brushFlip   = flipMatrix;
                    brushCenter = brush.transform.InverseTransformPoint(centerAll);
                }

                brushFlip = Matrix4x4.TRS(brushCenter, Quaternion.identity, Vector3.one) *
                    brushFlip *
                    Matrix4x4.TRS(-brushCenter, Quaternion.identity, Vector3.one);

                brush.EnsureInitialized();
                Shape shape = brush.Shape;

                for(int s = 0; s < shape.Surfaces.Length; s++)
                {
                    CSGPlane plane = shape.Surfaces[s].Plane;

                    Vector3 normal   = brushFlip.MultiplyVector(plane.normal);
                    Vector3 biNormal = brushFlip.MultiplyVector(shape.Surfaces[s].BiNormal);
                    Vector3 tangent  = brushFlip.MultiplyVector(shape.Surfaces[s].Tangent);

                    Vector3 pointOnPlane = plane.pointOnPlane;
                    pointOnPlane = brushFlip.MultiplyPoint(pointOnPlane);

                    shape.Surfaces[s].Plane    = new CSGPlane(normal, pointOnPlane);
                    shape.Surfaces[s].BiNormal = biNormal;
                    shape.Surfaces[s].Tangent  = tangent;
                }

                ControlMesh controlMesh = brush.ControlMesh;
                Vector3[]   vertices    = controlMesh.Vertices;

                for(int v = 0; v < vertices.Length; v++)
                {
                    vertices[v] = brushFlip.MultiplyPoint(vertices[v]);
                }

                Polygon[] polygons = controlMesh.Polygons;

                for(int p = 0; p < polygons.Length; p++)
                {
                    Array.Reverse(polygons[p].EdgeIndices);
                }

                HalfEdge[] edges        = controlMesh.Edges;
                short[]    twinVertices = new short[edges.Length];

                for(int e = 0; e < edges.Length; e++)
                {
                    twinVertices[e] = edges[edges[e].TwinIndex].VertexIndex;
                }

                for(int e = 0; e < edges.Length; e++)
                {
                    edges[e].VertexIndex = twinVertices[e];
                }

                brush.ControlMesh.SetDirty();
                EditorUtility.SetDirty(brush);

                InternalCSGModelManager.CheckSurfaceModifications(brush, true);

                ControlMeshUtility.RebuildShape(brush);
            }

            if(fail)
            {
                Debug.LogWarning("Failed to perform operation");
                Undo.RevertAllInCurrentGroup();
            }

            InternalCSGModelManager.CheckForChanges();
        }

        public static void FlipX(CSGBrush[] flipBrushes, string undoDescription = "Flip brushes on X Axis")
        {
            Matrix4x4 flipMatrix = MathConstants.identityMatrix;
            flipMatrix.m00 = -1;
            Flip(flipBrushes, flipMatrix, "Flip brushes on X Axis");
        }

        public static void FlipY(CSGBrush[] flipBrushes, string undoDescription = "Flip brushes on Y Axis")
        {
            Matrix4x4 flipMatrix = MathConstants.identityMatrix;
            flipMatrix.m11 = -1;
            Flip(flipBrushes, flipMatrix, "Flip brushes on Y Axis");
        }

        public static void FlipZ(CSGBrush[] flipBrushes, string undoDescription = "Flip brushes on Z Axis")
        {
            Matrix4x4 flipMatrix = MathConstants.identityMatrix;
            flipMatrix.m22 = -1;
            Flip(flipBrushes, flipMatrix, "Flip brushes on Z Axis");
        }
    }
}

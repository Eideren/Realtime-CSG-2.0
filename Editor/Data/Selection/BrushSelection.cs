using System;
using System.Collections.Generic;
using System.Linq;
using RealtimeCSG;
using UnityEngine;
using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal class BrushSelection
    {
        public CSGBrush[] brushes = Array.Empty<CSGBrush>();
        public Shape[] shapes = Array.Empty<Shape>();
        public ControlMesh[] controlMeshes = Array.Empty<ControlMesh>();
        public ControlMeshState[] states = Array.Empty<ControlMeshState>();

        public ControlMesh[] backupControlMeshes = Array.Empty<ControlMesh>();
        public Shape[] backupShapes = Array.Empty<Shape>();
        public Matrix4x4[] localToWorld = Array.Empty<Matrix4x4>();
        public Transform[] modelTransforms = Array.Empty<Transform>();

        public bool HaveSelection
        {
            get
            {
                for(int t = 0; t < brushes.Length; t++)
                {
                    if(states[t].HaveSelection)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Select(HashSet<CSGBrush> foundBrushes)
        {
            if(brushes == null || brushes.Length == 0)
            {
                brushes             = foundBrushes.ToArray();
                shapes              = new Shape[brushes.Length];
                controlMeshes       = new ControlMesh[brushes.Length];
                localToWorld        = new Matrix4x4[brushes.Length];
                backupControlMeshes = new ControlMesh[brushes.Length];
                backupShapes        = new Shape[brushes.Length];
                states              = new ControlMeshState[brushes.Length];
                modelTransforms     = new Transform[brushes.Length];

                for(int i = 0; i < foundBrushes.Count; i++)
                {
                    localToWorld[i] = MathConstants.identityMatrix;
                }
            }
            else
            {
                // remove brushes that are no longer selected
                for(int i = brushes.Length - 1; i >= 0; i--)
                {
                    if(foundBrushes.Contains(brushes[i]))
                    {
                        continue;
                    }

                    ArrayUtility.RemoveAt(ref brushes, i);
                    ArrayUtility.RemoveAt(ref shapes, i);
                    ArrayUtility.RemoveAt(ref controlMeshes, i);
                    ArrayUtility.RemoveAt(ref localToWorld, i);
                    ArrayUtility.RemoveAt(ref backupControlMeshes, i);
                    ArrayUtility.RemoveAt(ref backupShapes, i);
                    ArrayUtility.RemoveAt(ref states, i);
                    ArrayUtility.RemoveAt(ref modelTransforms, i);
                }

                // add new brushes that are added to the selection
                foreach(CSGBrush newBrush in foundBrushes)
                {
                    if(brushes.Contains(newBrush))
                    {
                        continue;
                    }

                    ArrayUtility.Add(ref brushes, newBrush);
                    ArrayUtility.Add(ref shapes, null);
                    ArrayUtility.Add(ref controlMeshes, null);
                    ArrayUtility.Add(ref localToWorld, MathConstants.identityMatrix);
                    ArrayUtility.Add(ref backupControlMeshes, null);
                    ArrayUtility.Add(ref backupShapes, null);
                    ArrayUtility.Add(ref states, null);
                    ArrayUtility.Add(ref modelTransforms, null);
                }
            }
        }

        public void ResetSelection()
        {
            if(brushes != null && states != null)
            {
                for(int i = 0; i < brushes.Length; i++)
                {
                    states[i] = null;
                }
            }
        }

        public void UpdateTargets()
        {
            for(int i = 0; i < brushes.Length; i++)
            {
                if(!brushes[i] || brushes[i].controlMesh == null)
                {
                    continue;
                }

                if(!brushes[i].controlMesh.Valid)
                {
                    brushes[i].controlMesh.Valid = ControlMeshUtility.Validate(brushes[i].controlMesh, brushes[i].shape);
                }

                localToWorld[i] = brushes[i].transform.localToWorldMatrix;

                if(states[i] != null)
                {
                    continue;
                }

                states[i]              = new ControlMeshState(brushes[i]);
                backupControlMeshes[i] = brushes[i].controlMesh != null ? brushes[i].controlMesh.Clone() : null;
                backupShapes[i]        = brushes[i].shape != null ? brushes[i].shape.Clone() : null;

                controlMeshes[i] = backupControlMeshes[i];
                shapes[i]        = backupShapes[i];
            }

            UpdateParentModelTransforms();
        }

        public void UpdateParentModelTransforms()
        {
            for(int i = 0; i < brushes.Length; i++)
            {
                if(modelTransforms[i] != null)
                {
                    continue;
                }

                if(brushes[i].childData == null || brushes[i].childData.modelTransform == null)
                {
                    continue;
                }

                modelTransforms[i] = brushes[i].childData.modelTransform;
            }
        }

        public void BackupSelection()
        {
            for(int t = 0; t < states.Length; t++)
            {
                if(states[t] == null)
                {
                    continue;
                }

                states[t].BackupSelection();
            }
        }

        public void RevertSelection()
        {
            for(int t = 0; t < states.Length; t++)
            {
                if(states[t] == null)
                {
                    continue;
                }

                states[t].RevertSelection();
            }
        }

        public void DestroySelectionBackup()
        {
            for(int t = 0; t < states.Length; t++)
            {
                if(states[t] == null)
                {
                    continue;
                }

                states[t].DestroySelectionBackup();
            }
        }

        public bool HasSelectionChanged()
        {
            for(int t = 0; t < states.Length; t++)
            {
                if(states[t] == null)
                {
                    continue;
                }

                if(states[t].HasSelectionChanged())
                {
                    return true;
                }
            }

            return false;
        }

        public void UnHoverAll()
        {
            for(int t = 0; t < states.Length; t++)
            {
                if(states[t] == null)
                {
                    continue;
                }

                states[t].UnHoverAll();
            }
        }

        public void UndoRedoPerformed()
        {
            for(int t = 0; t < states.Length; t++)
            {
                if(t >= brushes.Length || states[t] == null || !brushes[t])
                {
                    continue;
                }

                ControlMesh      controlMesh = brushes[t].controlMesh;
                ControlMeshState state       = states[t];
                state.UpdatePoints(controlMesh);
            }
        }

        public bool UpdateWorkControlMesh(bool forceUpdate = false)
        {
            for(int t = brushes.Length - 1; t >= 0; t--)
            {
                if(!brushes[t])
                {
                    ArrayUtility.RemoveAt(ref brushes, t);

                    continue;
                }

                if(!forceUpdate && controlMeshes[t] != null && !controlMeshes[t].Valid)
                {
                    continue;
                }

                shapes[t]              = brushes[t].shape.Clone();
                controlMeshes[t]       = brushes[t].controlMesh.Clone();
                backupShapes[t]        = shapes[t];
                backupControlMeshes[t] = controlMeshes[t];
            }

            for(int i = 0; i < brushes.Length; i++)
            {
                localToWorld[i] = brushes[i].transform.localToWorldMatrix;
            }

            return true;
        }

        public void FindClosestIntersection(SceneView sceneView, out int closestBrushNodeIndex, out int closestSurfaceIndex)
        {
            Ray     mouseWorldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 rayStart      = mouseWorldRay.origin;
            Vector3 rayVector     = mouseWorldRay.direction * (sceneView.camera.farClipPlane - sceneView.camera.nearClipPlane);

            Vector3 rayEnd = rayStart + rayVector;

            float minDistance = float.PositiveInfinity;
            closestBrushNodeIndex = -1;
            closestSurfaceIndex   = -1;

            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush brush = brushes[t];

                if(!brushes[t] || !brushes[t].isActiveAndEnabled)
                {
                    continue;
                }

                Transform parentModelTransform = modelTransforms[t];

                if(parentModelTransform == null)
                {
                    continue;
                }

                Matrix4x4 modelTransformation = parentModelTransform.localToWorldMatrix;

                if(!SceneQueryUtility.FindBrushIntersection(brush, modelTransformation, rayStart, rayEnd, out LegacyBrushIntersection intersection))
                {
                    continue;
                }

                float distance = (intersection.worldIntersection - rayStart).magnitude;

                if(distance > minDistance)
                {
                    continue;
                }

                minDistance           = distance;
                closestBrushNodeIndex = t;
                closestSurfaceIndex   = intersection.surfaceIndex;
            }
        }

        public Vector3[] GetSelectedWorldPoints(bool useBackupPoints = false)
        {
            HashSet<Vector3> points = new HashSet<Vector3>();

            for(int t = 0; t < states.Length; t++)
            {
                ControlMeshState meshState         = states[t];
                Matrix4x4        brushLocalToWorld = localToWorld[t];

                if(useBackupPoints)
                {
                    if(meshState.backupPoints == null)
                    {
                        continue;
                    }

                    foreach(short index in meshState.GetSelectedPointIndices())
                    {
                        points.Add(brushLocalToWorld.MultiplyPoint(meshState.backupPoints[index]));
                    }
                }
                else
                {
                    foreach(short index in meshState.GetSelectedPointIndices())
                    {
                        points.Add(meshState.worldPoints[index]);
                    }
                }
            }

            return points.ToArray();
        }

        public AABB GetSelectionBounds()
        {
            AABB newBounds = AABB.Empty;

            for(int t = 0; t < brushes.Length; t++)
            {
                ControlMeshState meshState = states[t];

                if(meshState == null)
                {
                    continue;
                }

                if(meshState.selection.points != null)
                {
                    for(int p = 0; p < meshState.selection.points.Length; p++)
                    {
                        if((meshState.selection.points[p] & SelectState.Selected) != SelectState.Selected)
                        {
                            continue;
                        }

                        newBounds.Extend(meshState.worldPoints[p]);
                    }
                }

                if(meshState.selection.edges != null)
                {
                    for(int e = 0; e < meshState.selection.edges.Length; e++)
                    {
                        if((meshState.selection.edges[e] & SelectState.Selected) != SelectState.Selected)
                        {
                            continue;
                        }

                        int index0 = meshState.edges[e * 2 + 0];
                        int index1 = meshState.edges[e * 2 + 1];

                        newBounds.Extend(meshState.worldPoints[index0]);
                        newBounds.Extend(meshState.worldPoints[index1]);
                    }
                }

                if(meshState.selection.polygons != null)
                {
                    for(int p = 0; p < meshState.selection.polygons.Length; p++)
                    {
                        if((meshState.selection.polygons[p] & SelectState.Selected) != SelectState.Selected)
                        {
                            continue;
                        }

                        int[] indices = meshState.polygonPointIndices[p];

                        for(int i = 0; i < indices.Length; i++)
                        {
                            int index = indices[i];

                            newBounds.Extend(meshState.worldPoints[index]);
                        }
                    }
                }
            }

            return newBounds;
        }
    }
}

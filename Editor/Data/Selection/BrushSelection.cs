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
        [SerializeField] public CSGBrush[] Brushes = new CSGBrush[0];
        [SerializeField] public Shape[] Shapes = new Shape[0];
        [SerializeField] public ControlMesh[] ControlMeshes = new ControlMesh[0];
        [SerializeField] public ControlMeshState[] States = new ControlMeshState[0];

        [SerializeField] public ControlMesh[] BackupControlMeshes = new ControlMesh[0];
        [SerializeField] public Shape[] BackupShapes = new Shape[0];
        [SerializeField] public Matrix4x4[] LocalToWorld = new Matrix4x4[0];
        [SerializeField] public Transform[] ModelTransforms = new Transform[0];

        public bool HaveSelection
        {
            get
            {
                for(int t = 0; t < Brushes.Length; t++)
                {
                    if(States[t].HaveSelection)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private bool HaveEdgeSelection
        {
            get
            {
                for(int t = 0; t < Brushes.Length; t++)
                {
                    if(States[t].HaveEdgeSelection)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Select(CSGBrush brush, int polygonIndex)
        {
            for(int i = Brushes.Length - 1; i >= 0; i--)
            {
                if(Brushes[i] == brush)
                {
                    SelectState[] polygons = States[i].Selection.Polygons;

                    for(int j = 0; j < polygons.Length; j++)
                    {
                        if(j == polygonIndex)
                        {
                            polygons[j] = SelectState.Selected;
                        }
                        else
                        {
                            polygons[j] = SelectState.None;
                        }
                    }
                }
            }
        }

        public void Select(HashSet<CSGBrush> foundBrushes)
        {
            if(Brushes == null || Brushes.Length == 0)
            {
                Brushes             = foundBrushes.ToArray();
                Shapes              = new Shape[Brushes.Length];
                ControlMeshes       = new ControlMesh[Brushes.Length];
                LocalToWorld        = new Matrix4x4[Brushes.Length];
                BackupControlMeshes = new ControlMesh[Brushes.Length];
                BackupShapes        = new Shape[Brushes.Length];
                States              = new ControlMeshState[Brushes.Length];
                ModelTransforms     = new Transform[Brushes.Length];

                for(int i = 0; i < foundBrushes.Count; i++)
                {
                    LocalToWorld[i] = MathConstants.identityMatrix;
                }
            }
            else
            {
                // remove brushes that are no longer selected
                for(int i = Brushes.Length - 1; i >= 0; i--)
                {
                    if(foundBrushes.Contains(Brushes[i]))
                    {
                        continue;
                    }

                    ArrayUtility.RemoveAt(ref Brushes, i);
                    ArrayUtility.RemoveAt(ref Shapes, i);
                    ArrayUtility.RemoveAt(ref ControlMeshes, i);
                    ArrayUtility.RemoveAt(ref LocalToWorld, i);
                    ArrayUtility.RemoveAt(ref BackupControlMeshes, i);
                    ArrayUtility.RemoveAt(ref BackupShapes, i);
                    ArrayUtility.RemoveAt(ref States, i);
                    ArrayUtility.RemoveAt(ref ModelTransforms, i);
                }

                // add new brushes that are added to the selection
                foreach(CSGBrush newBrush in foundBrushes)
                {
                    if(Brushes.Contains(newBrush))
                    {
                        continue;
                    }

                    ArrayUtility.Add(ref Brushes, newBrush);
                    ArrayUtility.Add(ref Shapes, null);
                    ArrayUtility.Add(ref ControlMeshes, null);
                    ArrayUtility.Add(ref LocalToWorld, MathConstants.identityMatrix);
                    ArrayUtility.Add(ref BackupControlMeshes, null);
                    ArrayUtility.Add(ref BackupShapes, null);
                    ArrayUtility.Add(ref States, null);
                    ArrayUtility.Add(ref ModelTransforms, null);
                }
            }
        }

        public void ResetSelection()
        {
            if(Brushes != null && States != null)
            {
                for(int i = 0; i < Brushes.Length; i++)
                {
                    States[i] = null;
                }
            }
        }

        public void UpdateTargets()
        {
            for(int i = 0; i < Brushes.Length; i++)
            {
                if(!Brushes[i] ||
                   Brushes[i].ControlMesh == null)
                {
                    continue;
                }

                if(!Brushes[i].ControlMesh.Valid)
                {
                    Brushes[i].ControlMesh.Valid = ControlMeshUtility.Validate(
                        Brushes[i].ControlMesh, Brushes[i].Shape
                    );
                }

                LocalToWorld[i] = Brushes[i].transform.localToWorldMatrix;

                if(States[i] != null)
                {
                    continue;
                }

                States[i]              = new ControlMeshState(Brushes[i]);
                BackupControlMeshes[i] = Brushes[i].ControlMesh != null ? Brushes[i].ControlMesh.Clone() : null;
                BackupShapes[i]        = Brushes[i].Shape != null ? Brushes[i].Shape.Clone() : null;

                ControlMeshes[i] = BackupControlMeshes[i];
                Shapes[i]        = BackupShapes[i];
            }

            UpdateParentModelTransforms();
        }

        public void UpdateParentModelTransforms()
        {
            for(int i = 0; i < Brushes.Length; i++)
            {
                if(ModelTransforms[i] != null)
                {
                    continue;
                }

                if(Brushes[i].ChildData == null ||
                   Brushes[i].ChildData.ModelTransform == null)
                {
                    continue;
                }

                ModelTransforms[i] = Brushes[i].ChildData.ModelTransform;
            }
        }

        public void BackupSelection()
        {
            for(int t = 0; t < States.Length; t++)
            {
                if(States[t] == null)
                {
                    continue;
                }

                States[t].BackupSelection();
            }
        }

        public void RevertSelection()
        {
            for(int t = 0; t < States.Length; t++)
            {
                if(States[t] == null)
                {
                    continue;
                }

                States[t].RevertSelection();
            }
        }

        public void DestroySelectionBackup()
        {
            for(int t = 0; t < States.Length; t++)
            {
                if(States[t] == null)
                {
                    continue;
                }

                States[t].DestroySelectionBackup();
            }
        }

        public bool HasSelectionChanged()
        {
            for(int t = 0; t < States.Length; t++)
            {
                if(States[t] == null)
                {
                    continue;
                }

                if(States[t].HasSelectionChanged())
                {
                    return true;
                }
            }

            return false;
        }

        public void UnHoverAll()
        {
            for(int t = 0; t < States.Length; t++)
            {
                if(States[t] == null)
                {
                    continue;
                }

                States[t].UnHoverAll();
            }
        }

        public void UndoRedoPerformed()
        {
            for(int t = 0; t < States.Length; t++)
            {
                if(t >= Brushes.Length ||
                   States[t] == null ||
                   !Brushes[t])
                {
                    continue;
                }

                ControlMesh      controlMesh = Brushes[t].ControlMesh;
                ControlMeshState state       = States[t];
                state.UpdatePoints(controlMesh);
            }
        }

        public bool UpdateWorkControlMesh(bool forceUpdate = false)
        {
            for(int t = Brushes.Length - 1; t >= 0; t--)
            {
                if(!Brushes[t])
                {
                    ArrayUtility.RemoveAt(ref Brushes, t);

                    continue;
                }

                if(!forceUpdate &&
                   ControlMeshes[t] != null &&
                   !ControlMeshes[t].Valid)
                {
                    continue;
                }

                Shapes[t]              = Brushes[t].Shape.Clone();
                ControlMeshes[t]       = Brushes[t].ControlMesh.Clone();
                BackupShapes[t]        = Shapes[t];
                BackupControlMeshes[t] = ControlMeshes[t];
            }

            for(int i = 0; i < Brushes.Length; i++)
            {
                LocalToWorld[i] = Brushes[i].transform.localToWorldMatrix;
            }

            return true;
        }

        public void FindClosestIntersection(
            SceneView sceneView, out int closestBrushNodeIndex, out int closestSurfaceIndex
        )
        {
            Ray     mouseWorldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            Vector3 rayStart      = mouseWorldRay.origin;

            Vector3 rayVector = mouseWorldRay.direction
              * (sceneView.camera.farClipPlane - sceneView.camera.nearClipPlane);

            Vector3 rayEnd = rayStart + rayVector;

            float minDistance = float.PositiveInfinity;
            closestBrushNodeIndex = -1;
            closestSurfaceIndex   = -1;

            for(int t = 0; t < Brushes.Length; t++)
            {
                CSGBrush brush = Brushes[t];

                if(!Brushes[t] || !Brushes[t].isActiveAndEnabled)
                {
                    continue;
                }

                Transform parentModelTransform = ModelTransforms[t];

                if(parentModelTransform == null)
                {
                    continue;
                }

                Matrix4x4 modelTransformation = parentModelTransform.localToWorldMatrix;

                LegacyBrushIntersection intersection;

                if(!SceneQueryUtility.FindBrushIntersection(
                       brush, modelTransformation, rayStart, rayEnd, out intersection
                   ))
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

            for(int t = 0; t < States.Length; t++)
            {
                ControlMeshState meshState         = States[t];
                Matrix4x4        brushLocalToWorld = LocalToWorld[t];

                if(useBackupPoints)
                {
                    if(meshState.BackupPoints == null)
                    {
                        continue;
                    }

                    foreach(short index in meshState.GetSelectedPointIndices())
                    {
                        points.Add(brushLocalToWorld.MultiplyPoint(meshState.BackupPoints[index]));
                    }
                }
                else
                {
                    foreach(short index in meshState.GetSelectedPointIndices())
                    {
                        points.Add(meshState.WorldPoints[index]);
                    }
                }
            }

            return points.ToArray();
        }

        public AABB GetSelectionBounds()
        {
            AABB newBounds = AABB.Empty;

            for(int t = 0; t < Brushes.Length; t++)
            {
                ControlMeshState meshState = States[t];

                if(meshState == null)
                {
                    continue;
                }

                if(meshState.Selection.Points != null)
                {
                    for(int p = 0; p < meshState.Selection.Points.Length; p++)
                    {
                        if((meshState.Selection.Points[p] & SelectState.Selected) != SelectState.Selected)
                        {
                            continue;
                        }

                        newBounds.Extend(meshState.WorldPoints[p]);
                    }
                }

                if(meshState.Selection.Edges != null)
                {
                    for(int e = 0; e < meshState.Selection.Edges.Length; e++)
                    {
                        if((meshState.Selection.Edges[e] & SelectState.Selected) != SelectState.Selected)
                        {
                            continue;
                        }

                        int index0 = meshState.Edges[e * 2 + 0];
                        int index1 = meshState.Edges[e * 2 + 1];

                        newBounds.Extend(meshState.WorldPoints[index0]);
                        newBounds.Extend(meshState.WorldPoints[index1]);
                    }
                }

                if(meshState.Selection.Polygons != null)
                {
                    for(int p = 0; p < meshState.Selection.Polygons.Length; p++)
                    {
                        if((meshState.Selection.Polygons[p] & SelectState.Selected) != SelectState.Selected)
                        {
                            continue;
                        }

                        int[] indices = meshState.PolygonPointIndices[p];

                        for(int i = 0; i < indices.Length; i++)
                        {
                            int index = indices[i];

                            newBounds.Extend(meshState.WorldPoints[index]);
                        }
                    }
                }
            }

            return newBounds;
        }

        internal void DebugLogData()
        {
            int pointCount   = 0;
            int polygonCount = 0;
            int edgeCount    = 0;

            for(int t = 0; t < States.Length; t++)
            {
                ControlMeshState     state     = States[t];
                ControlMeshSelection selection = state.Selection;

                for(int p = 0; p < selection.Points.Length; p++)
                {
                    if((selection.Points[p] & SelectState.Selected) == SelectState.Selected)
                    {
                        pointCount++;
                    }
                }

                for(int e = 0; e < selection.Edges.Length; e++)
                {
                    if((selection.Edges[e] & SelectState.Selected) == SelectState.Selected)
                    {
                        edgeCount++;
                    }
                }

                for(int e = 0; e < selection.Polygons.Length; e++)
                {
                    if((selection.Polygons[e] & SelectState.Selected) == SelectState.Selected)
                    {
                        polygonCount++;
                    }
                }
            }

            Debug.Log(pointCount + " " + edgeCount + " " + polygonCount);
        }
    }
}

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal sealed class SceneDragToolMeshDragOnSurface : ISceneDragTool
    {
        private SelectedBrushSurface hoverBrushSurface = null;
        private Vector3 hoverPosition;
        private Quaternion hoverRotation;
        private Transform hoverParent;
        private int hoverSiblingIndex;
        private List<GameObject> dragGameObjects = null;
        private List<GameObject> visualDragGameObject = null;
        private Vector3[] projectedBounds = null;
        private bool haveNoParent = false;
        private PrefabSourceAlignment sourceSurfaceAlignment = PrefabSourceAlignment.AlignedFront;
        private PrefabDestinationAlignment destinationSurfaceAlignment = PrefabDestinationAlignment.AlignToSurface;

        private Vector3 prevForcedGridCenter = MathConstants.zeroVector3;
        private Quaternion prevForcedGridRotation = MathConstants.identityQuaternion;

#region ValidateDrop

        public bool ValidateDrop(SceneView sceneView)
        {
            if(!sceneView)
            {
                return false;
            }

            Reset();

            if(DragAndDrop.objectReferences == null ||
               DragAndDrop.objectReferences.Length == 0)
            {
                dragGameObjects = null;

                return false;
            }

            bool found = false;
            dragGameObjects = new List<GameObject>();

            foreach(Object obj in DragAndDrop.objectReferences)
            {
                GameObject gameObject = obj as GameObject;

                if(gameObject == null)
                {
                    continue;
                }

                if(!CSGPrefabUtility.IsPrefabAsset(gameObject))
                {
                    continue;
                }

                dragGameObjects.Add(gameObject);

                if(gameObject.GetComponentInChildren<Renderer>() != null)
                {
                    found = true;
                }
            }

            if(!found || dragGameObjects.Count != 1)
            {
                dragGameObjects = null;

                return false;
            }

            AABB dragGameObjectBounds = new AABB();
            dragGameObjectBounds.Reset();

            foreach(GameObject gameObject in dragGameObjects)
            {
                Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();

                if(renderers.Length == 0)
                {
                    continue;
                }

                foreach(Renderer renderer in renderers)
                {
                    Bounds bounds = renderer.bounds;
                    dragGameObjectBounds.Extend(bounds.min);
                    dragGameObjectBounds.Extend(bounds.max);
                }
            }

            if(!dragGameObjectBounds.Valid)
            {
                dragGameObjectBounds.Extend(MathConstants.zeroVector3);
            }

            projectedBounds = new Vector3[8];
            BoundsUtilities.GetBoundsCornerPoints(dragGameObjectBounds, projectedBounds);
            /*
            var upPlane = new Plane(MathConstants.upVector3, MathConstants.zeroVector3);
            for (int i = 7; i >= 0; i--)
            {
                projectedBounds[i] = upPlane.Project(projectedBounds[i]);
                for (int j = i+1; j < projectedBounds.Length; j++)
                {
                    if (projectedBounds[i] == projectedBounds[j])
                    {
                        ArrayUtility.RemoveAt(ref projectedBounds, j);
                        break;
                    }
                }
            }*/

            haveNoParent = false;

            return true;
        }

#endregion

#region ValidateDropPoint

        public bool ValidateDropPoint(SceneView sceneView)
        {
            return true;
        }

#endregion

#region Reset

        public void Reset()
        {
            CleanUp();
            hoverBrushSurface = null;
            dragGameObjects   = null;

            hoverPosition     = MathConstants.zeroVector3;
            hoverRotation     = MathConstants.identityQuaternion;
            hoverParent       = null;
            hoverSiblingIndex = int.MaxValue;
        }

#endregion

        private void CleanUp()
        {
            if(visualDragGameObject != null)
            {
                for(int i = visualDragGameObject.Count - 1; i >= 0; i--)
                {
                    if(!visualDragGameObject[i])
                    {
                        continue;
                    }

                    GameObject.DestroyImmediate(visualDragGameObject[i]);
                }
            }

            visualDragGameObject = null;
        }

        private void DisableVisualObjects()
        {
            if(visualDragGameObject != null)
            {
                for(int i = visualDragGameObject.Count - 1; i >= 0; i--)
                {
                    GameObject obj = visualDragGameObject[i];

                    if(obj == null || !obj)
                    {
                        continue;
                    }

                    if(obj.activeSelf)
                    {
                        obj.SetActive(false);
                    }
                }
            }
        }

        private void EnableVisualObjects()
        {
            if(visualDragGameObject == null ||
               visualDragGameObject.Count != dragGameObjects.Count)
            {
                CreateVisualObjects();
            }
            else
            {
                for(int i = 0; i < dragGameObjects.Count; i++)
                {
                    if(visualDragGameObject[i] == null || !visualDragGameObject[i])
                    {
                        continue;
                    }

                    visualDragGameObject[i].SetActive(dragGameObjects[i].activeSelf);
                }
            }

            int counter = 0;

            foreach(GameObject obj in visualDragGameObject)
            {
                if(obj == null || !obj)
                {
                    continue;
                }

                Vector3    scale    = Vector3.one;
                Quaternion rotation = hoverRotation;
                Vector3    position = hoverPosition;
#if UNITY_2018_3_OR_NEWER
                if(CSGPrefabUtility.IsPrefabInstance(obj))
                {
                    GameObject outer       = CSGPrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                    GameObject prefabAsset = CSGPrefabUtility.GetPrefabAsset(outer);
                    Transform  transform   = prefabAsset.transform;

                    rotation *= transform.localRotation;
                    //position += transform.localPosition;
                    scale = transform.localScale;
                }
#endif
                obj.transform.rotation   = rotation;
                obj.transform.position   = position;
                obj.transform.localScale = scale;
                //	if (hoverParent != null && !CSGPrefabUtility.IsPrefabAsset(hoverParent.gameObject))
                //		obj.transform.SetParent(hoverParent, true);
                obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
                counter++;
            }
        }

        private void CreateVisualObjects()
        {
            CleanUp();

            prevForcedGridCenter   = CSGGrid.ForcedGridCenter;
            prevForcedGridRotation = CSGGrid.ForcedGridRotation;

            sourceSurfaceAlignment      = PrefabSourceAlignment.AlignedFront;
            destinationSurfaceAlignment = PrefabDestinationAlignment.Default;

            visualDragGameObject = new List<GameObject>();

            foreach(GameObject obj in dragGameObjects)
            {
                GameObject copy = CSGPrefabUtility.Instantiate(obj);

                if(!copy)
                {
                    continue;
                }

                copy.name = obj.name;
                visualDragGameObject.Add(copy);
            }
        }

#region DragUpdated

        public bool DragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
        {
            try
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                hoverBrushSurface = null;
                hoverParent       = transformInInspector == null ? null : transformInInspector.parent;

                hoverSiblingIndex = transformInInspector == null
                    ? int.MaxValue
                    : transformInInspector.transform.GetSiblingIndex();

                float middle = (selectionRect.yMax + selectionRect.yMin) * 0.5f;

                if(Event.current.mousePosition.y > middle)
                {
                    hoverSiblingIndex++;
                }

                hoverRotation = MathConstants.identityQuaternion;
                hoverPosition = MathConstants.zeroVector3;
                haveNoParent  = true;

                return true;
            }
            finally
            {
                if(!UpdateLoop.IsActive())
                {
                    UpdateLoop.ResetUpdateRoutine();
                }
            }
        }

        public bool DragUpdated(SceneView sceneView)
        {
            try
            {
                DisableVisualObjects();
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                Camera camera = sceneView.camera;

                LegacyBrushIntersection intersection =
                    SceneQueryUtility.FindMeshIntersection(camera, Event.current.mousePosition);

                Vector3 normal = intersection.worldPlane.normal;

                hoverPosition = intersection.worldIntersection;
                hoverParent   = SelectionUtility.FindParentToAssignTo(intersection);

                hoverBrushSurface = intersection.brush != null
                    ? new SelectedBrushSurface(intersection.brush, intersection.surfaceIndex, intersection.worldPlane)
                    : null;

                hoverRotation = SelectionUtility.FindDragOrientation(
                    sceneView, normal, sourceSurfaceAlignment, destinationSurfaceAlignment
                );

                haveNoParent      = hoverParent == null;
                hoverSiblingIndex = int.MaxValue;

                CSGGrid.SetForcedGrid(camera, intersection.worldPlane);

                // Since we're snapping points to grid and do not have a relative distance, relative snapping makes no sense
                bool doGridSnapping = CSGSettings.ActiveSnappingMode != SnapMode.None;

                if(doGridSnapping)
                {
                    Vector3[] localPoints = new Vector3[8];
                    CSGPlane  localPlane  = intersection.worldPlane;

                    for(int i = 0; i < localPoints.Length; i++)
                    {
                        localPoints[i] = GeometryUtility.ProjectPointOnPlane(
                            localPlane, hoverRotation * projectedBounds[i] + hoverPosition
                        );
                    }

                    hoverPosition += CSGGrid.SnapDeltaToGrid(camera, MathConstants.zeroVector3, localPoints);
                }

                hoverPosition =
                    GeometryUtility.ProjectPointOnPlane(intersection.worldPlane, hoverPosition); // + (normal * 0.01f);

                EnableVisualObjects();

                return true;
            }
            finally
            {
                if(!UpdateLoop.IsActive())
                {
                    UpdateLoop.ResetUpdateRoutine();
                }
            }
        }

#endregion

#region DragPerform

        public void DragPerform(SceneView sceneView)
        {
            try
            {
                if(haveNoParent)
                {
                    if(visualDragGameObject == null)
                    {
                        CreateVisualObjects();
                    }
                }

                if(visualDragGameObject != null)
                {
                    List<GameObject> selection = new List<GameObject>();

                    for(int i = visualDragGameObject.Count - 1; i >= 0; i--)
                    {
                        if(!visualDragGameObject[i])
                        {
                            continue;
                        }

                        if(visualDragGameObject[i].activeSelf)
                        {
                            Undo.RegisterCreatedObjectUndo(visualDragGameObject[i], "Instantiated prefab");
                            selection.Add(visualDragGameObject[i]);
                        }
                        else
                        {
                            GameObject.DestroyImmediate(visualDragGameObject[i]);
                        }
                    }

                    visualDragGameObject = null;

                    if(selection.Count > 0)
                    {
                        Selection.objects = selection.ToArray();
                    }
                }

                for(int i = 0; i < SceneView.sceneViews.Count; i++)
                {
                    SceneView sceneview = SceneView.sceneViews[i] as SceneView;

                    if(sceneview == null)
                    {
                        continue;
                    }

                    if(sceneview.camera.pixelRect.Contains(Event.current.mousePosition))
                    {
                        sceneview.Focus();
                    }
                }

                visualDragGameObject = null;
            }
            finally
            {
                CSGGrid.ForcedGridCenter   = prevForcedGridCenter;
                CSGGrid.ForcedGridRotation = prevForcedGridRotation;
                CSGGrid.ForceGrid          = false;
            }
        }

#endregion

#region DragExited

        public void DragExited(SceneView sceneView)
        {
            try
            {
                CleanUp();
                HandleUtility.Repaint();
            }
            finally
            {
                CSGGrid.ForcedGridCenter   = prevForcedGridCenter;
                CSGGrid.ForcedGridRotation = prevForcedGridRotation;
                CSGGrid.ForceGrid          = false;
            }
        }

#endregion

#region Paint

        public void OnPaint(Camera camera)
        {
            CSGGrid.RenderGrid(camera);

            if(hoverBrushSurface == null)
            {
                return;
            }

            CSGBrush brush = hoverBrushSurface.brush;

            if(brush.ChildData == null ||
               !brush.ChildData.ModelTransform)
            {
                return;
            }

            int       highlight_surface    = hoverBrushSurface.surfaceIndex;
            Matrix4x4 brush_transformation = brush.compareTransformation.localToWorldMatrix;

            CSGRenderer.DrawSelectedBrush(
                brush.brushNodeID, brush.Shape,
                brush_transformation, ColorSettings.WireframeOutline,
                highlight_surface,
                false, GUIConstants.OLD_LINE_SCALE
            );
        }

#endregion
    }
}

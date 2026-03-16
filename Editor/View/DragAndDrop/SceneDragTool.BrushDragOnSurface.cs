using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal sealed class SceneDragToolBrushDragOnSurface : ISceneDragTool
    {
        private SelectedBrushSurface hoverBrushSurface = null;
        private Vector3 hoverPosition;
        private Quaternion hoverRotation;
        private Transform hoverParent;
        private int hoverSiblingIndex;
        private bool containsModel = false;
        private List<GameObject> dragGameObjects = null;
        private List<GameObject> visualDragGameObject = null;
        private CSGBrush[] ignoreBrushes = null;
        private HashSet<Transform> ignoreTransforms = null;
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

            dragGameObjects = new List<GameObject>();
            containsModel   = false;

            foreach(Object obj in DragAndDrop.objectReferences)
            {
                GameObject gameObject = obj as GameObject;

                if(gameObject == null)
                {
                    continue;
                }

                if(gameObject.GetComponent<CSGNode>() == null)
                {
                    continue;
                }

                if(gameObject.GetComponentsInChildren<CSGBrush>() == null)
                {
                    continue;
                }

                if(!CSGPrefabUtility.IsPrefabAsset(gameObject))
                {
                    continue;
                }

                dragGameObjects.Add(gameObject);
                containsModel = containsModel || gameObject.GetComponent<CSGModel>() != null;
            }

            if(dragGameObjects.Count != 1)
            {
                dragGameObjects = null;

                return false;
            }

            AABB dragGameObjectBounds = new AABB();
            dragGameObjectBounds.Reset();

            foreach(GameObject gameObject in dragGameObjects)
            {
                CSGBrush[] brushes = gameObject.GetComponentsInChildren<CSGBrush>();

                if(brushes.Length == 0)
                {
                    continue;
                }

                dragGameObjectBounds.Add(
                    BoundsUtilities.GetLocalBounds(brushes, gameObject.transform.worldToLocalMatrix)
                );
            }

            if(!dragGameObjectBounds.Valid)
            {
                dragGameObjectBounds.Extend(MathConstants.zeroVector3);
            }

            projectedBounds = new Vector3[8];
            BoundsUtilities.GetBoundsCornerPoints(dragGameObjectBounds, projectedBounds);
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
            ignoreBrushes        = null;
        }

        public SelectedBrushSurface[] HoverOnBrush(CSGBrush hoverBrush, int surfaceIndex)
        {
            if(!hoverBrush)
            {
                return null;
            }

            return new SelectedBrushSurface[] { new SelectedBrushSurface(hoverBrush, surfaceIndex) };
        }

        private void EnableVisualObjects()
        {
            if(visualDragGameObject == null ||
               visualDragGameObject.Count != dragGameObjects.Count)
            {
                CreateVisualObjects();
            }

            Transform realParent = !hoverParent || CSGPrefabUtility.IsPrefabAsset(hoverParent.gameObject)
                ? null
                : hoverParent;

            int counter = 0;

            foreach(GameObject obj in visualDragGameObject)
            {
                if(!obj)
                {
                    continue;
                }

                obj.transform.rotation = hoverRotation;
                obj.transform.position = hoverPosition;

                if(realParent)
                {
                    obj.transform.SetParent(realParent, true);
                }
                else
                {
                    obj.transform.parent = null;
                }

                obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
                counter++;
            }

            InternalCSGModelManager.CheckTransformChanged();
            InternalCSGModelManager.OnHierarchyModified();
            InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
            MeshInstanceManager.UpdateHelperSurfaceVisibility();

            if(ignoreBrushes == null && visualDragGameObject != null)
            {
                List<CSGBrush> foundIgnoreBrushes = new List<CSGBrush>();

                foreach(GameObject obj in visualDragGameObject)
                {
                    foundIgnoreBrushes.AddRange(obj.GetComponentsInChildren<CSGBrush>());
                }

                ignoreBrushes = foundIgnoreBrushes.ToArray();
            }
        }

        private void CreateVisualObjects(bool inSceneView = false)
        {
            CleanUp();

            prevForcedGridCenter   = CSGGrid.ForcedGridCenter;
            prevForcedGridRotation = CSGGrid.ForcedGridRotation;

            sourceSurfaceAlignment      = PrefabSourceAlignment.AlignedFront;
            destinationSurfaceAlignment = PrefabDestinationAlignment.AlignToSurface;

            visualDragGameObject = new List<GameObject>();

            List<Transform> foundTransforms = new List<Transform>();

            foreach(GameObject obj in dragGameObjects)
            {
                foundTransforms.AddRange(obj.GetComponentsInChildren<Transform>());
                CSGNode node = obj.GetComponent<CSGNode>();

                if(!node)
                {
                    continue;
                }

                sourceSurfaceAlignment      = node.PrefabSourceAlignment;
                destinationSurfaceAlignment = node.PrefabDestinationAlignment;

                bool createCopyInsteadOfInstance = node && node.PrefabBehaviour == PrefabInstantiateBehaviour.Copy;

                GameObject copy = CSGPrefabUtility.Instantiate(obj, createCopyInsteadOfInstance);

                if(!copy)
                {
                    continue;
                }

                copy.name = obj.name;
                visualDragGameObject.Add(copy);
            }

            ignoreTransforms = new HashSet<Transform>(foundTransforms);

            if(inSceneView)
            {
                CSGModel model = SelectionUtility.LastUsedModel;

                if(model && !containsModel)
                {
                    Transform parent  = model.transform;
                    int       counter = 0;

                    foreach(GameObject obj in visualDragGameObject)
                    {
                        if(!obj)
                        {
                            continue;
                        }

                        if(obj.activeSelf)
                        {
                            obj.transform.SetParent(parent, false);
                            obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
                            counter++;
                        }
                    }
                }
            }
            else
            {
                Transform parent  = hoverParent;
                int       counter = 0;

                foreach(GameObject obj in visualDragGameObject)
                {
                    if(!obj)
                    {
                        continue;
                    }

                    if(obj.activeSelf)
                    {
                        obj.transform.SetParent(parent, false);
                        obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
                        counter++;
                    }
                }
            }
        }

#region DragUpdated

        public bool DragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
        {
            InternalCSGModelManager.skipCheckForChanges = true;

            try
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                hoverBrushSurface = null;
                hoverParent       = !transformInInspector ? null : transformInInspector.parent;

                hoverSiblingIndex = !transformInInspector
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
            InternalCSGModelManager.skipCheckForChanges = true;

            try
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                Camera camera = sceneView.camera;

                LegacyBrushIntersection intersection = SceneQueryUtility.FindMeshIntersection(
                    camera, Event.current.mousePosition, ignoreBrushes, ignoreTransforms
                );

                Vector3 normal = intersection.worldPlane.normal;

                hoverPosition = intersection.worldIntersection;
                hoverParent   = SelectionUtility.FindParentToAssignTo(intersection);

                hoverBrushSurface = intersection.brush
                    ? new SelectedBrushSurface(intersection.brush, intersection.surfaceIndex)
                    : null;

                hoverRotation = SelectionUtility.FindDragOrientation(
                    sceneView, normal, sourceSurfaceAlignment, destinationSurfaceAlignment
                );

                haveNoParent      = !hoverParent;
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
                InternalCSGModelManager.skipCheckForChanges = true;

                if(visualDragGameObject == null)
                {
                    CreateVisualObjects(sceneView != null);
                }

                if(sceneView && haveNoParent && !containsModel)
                {
                    CSGModel model = SelectionUtility.LastUsedModel;

                    if(!model)
                    {
                        model = OperationsUtility.CreateModelInstanceInScene(false);
                        InternalCSGModelManager.EnsureInitialized(model);
                        InternalCSGModelManager.CheckTransformChanged();
                        InternalCSGModelManager.OnHierarchyModified();
                    }

                    Transform parent = model.transform;

                    int counter = 0;

                    foreach(GameObject obj in visualDragGameObject)
                    {
                        if(!obj)
                        {
                            continue;
                        }

                        if(obj.activeSelf)
                        {
                            obj.transform.SetParent(parent, false);
                            obj.transform.SetSiblingIndex(hoverSiblingIndex + counter);
                            counter++;
                        }
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

                if(sceneView)
                {
                    for(int i = 0; i < SceneView.sceneViews.Count; i++)
                    {
                        SceneView sceneview = SceneView.sceneViews[i] as SceneView;

                        if(!sceneview)
                        {
                            continue;
                        }

                        if(sceneview.camera.pixelRect.Contains(Event.current.mousePosition))
                        {
                            sceneview.Focus();
                        }
                    }
                }

                visualDragGameObject = null;

                InternalCSGModelManager.CheckForChanges(true);
            }
            finally
            {
                InternalCSGModelManager.skipCheckForChanges = false;
                CSGGrid.ForcedGridCenter                    = prevForcedGridCenter;
                CSGGrid.ForcedGridRotation                  = prevForcedGridRotation;
                CSGGrid.ForceGrid                           = false;
            }
        }

#endregion

#region DragExited

        public void DragExited(SceneView sceneView)
        {
            try
            {
                InternalCSGModelManager.skipCheckForChanges = true;

                try
                {
                    CleanUp();
                }
                catch
                {
                }

                InternalCSGModelManager.CheckTransformChanged();
                InternalCSGModelManager.OnHierarchyModified();
                InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
                HandleUtility.Repaint();
            }
            finally
            {
                InternalCSGModelManager.skipCheckForChanges = false;
                CSGGrid.ForcedGridCenter                    = prevForcedGridCenter;
                CSGGrid.ForcedGridRotation                  = prevForcedGridRotation;
                CSGGrid.ForceGrid                           = false;
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
                false, GUIConstants.oldLineScale
            );
        }

#endregion
    }
}

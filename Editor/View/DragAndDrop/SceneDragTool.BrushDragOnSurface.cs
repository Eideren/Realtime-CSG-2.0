using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class SceneDragToolBrushDragOnSurface : ISceneDragTool
    {
        private SelectedBrushSurface _hoverBrushSurface;
        private Vector3 _hoverPosition;
        private Quaternion _hoverRotation;
        private Transform _hoverParent;
        private int _hoverSiblingIndex;
        private bool _containsModel;
        private List<GameObject> _dragGameObjects;
        private List<GameObject> _visualDragGameObject;
        private CSGBrush[] _ignoreBrushes;
        private HashSet<Transform> _ignoreTransforms;
        private Vector3[] _projectedBounds;
        private bool _haveNoParent;
        private PrefabSourceAlignment _sourceSurfaceAlignment = PrefabSourceAlignment.AlignedFront;
        private PrefabDestinationAlignment _destinationSurfaceAlignment = PrefabDestinationAlignment.AlignToSurface;

        private Vector3 _prevForcedGridCenter = MathConstants.zeroVector3;
        private Quaternion _prevForcedGridRotation = MathConstants.identityQuaternion;

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
                _dragGameObjects = null;

                return false;
            }

            _dragGameObjects = new List<GameObject>();
            _containsModel   = false;

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

                _dragGameObjects.Add(gameObject);
                _containsModel = _containsModel || gameObject.GetComponent<CSGModel>() != null;
            }

            if(_dragGameObjects.Count != 1)
            {
                _dragGameObjects = null;

                return false;
            }

            AABB dragGameObjectBounds = new AABB();
            dragGameObjectBounds.Reset();

            foreach(GameObject gameObject in _dragGameObjects)
            {
                CSGBrush[] brushes = gameObject.GetComponentsInChildren<CSGBrush>();

                if(brushes.Length == 0)
                {
                    continue;
                }

                dragGameObjectBounds.Add(BoundsUtilities.GetLocalBounds(brushes, gameObject.transform.worldToLocalMatrix));
            }

            if(!dragGameObjectBounds.Valid)
            {
                dragGameObjectBounds.Extend(MathConstants.zeroVector3);
            }

            _projectedBounds = new Vector3[8];
            BoundsUtilities.GetBoundsCornerPoints(dragGameObjectBounds, _projectedBounds);
            _haveNoParent = false;

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
            _hoverBrushSurface = null;
            _dragGameObjects   = null;

            _hoverPosition     = MathConstants.zeroVector3;
            _hoverRotation     = MathConstants.identityQuaternion;
            _hoverParent       = null;
            _hoverSiblingIndex = int.MaxValue;
        }

#endregion

        private void CleanUp()
        {
            if(_visualDragGameObject != null)
            {
                for(int i = _visualDragGameObject.Count - 1; i >= 0; i--)
                {
                    if(!_visualDragGameObject[i])
                    {
                        continue;
                    }

                    Object.DestroyImmediate(_visualDragGameObject[i]);
                }
            }

            _visualDragGameObject = null;
            _ignoreBrushes        = null;
        }

        public SelectedBrushSurface[] HoverOnBrush(CSGBrush hoverBrush, int surfaceIndex)
        {
            return !hoverBrush ? null : new[] { new SelectedBrushSurface(hoverBrush, surfaceIndex) };
        }

        private void EnableVisualObjects()
        {
            if(_visualDragGameObject == null || _visualDragGameObject.Count != _dragGameObjects.Count)
            {
                CreateVisualObjects();
            }

            Transform realParent = !_hoverParent || CSGPrefabUtility.IsPrefabAsset(_hoverParent.gameObject) ? null : _hoverParent;

            int counter = 0;

            foreach(GameObject obj in _visualDragGameObject)
            {
                if(!obj)
                {
                    continue;
                }

                obj.transform.rotation = _hoverRotation;
                obj.transform.position = _hoverPosition;

                if(realParent)
                {
                    obj.transform.SetParent(realParent, true);
                }
                else
                {
                    obj.transform.parent = null;
                }

                obj.transform.SetSiblingIndex(_hoverSiblingIndex + counter);
                counter++;
            }

            InternalCSGModelManager.CheckTransformChanged();
            InternalCSGModelManager.OnHierarchyModified();
            InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
            MeshInstanceManager.UpdateHelperSurfaceVisibility();

            if(_ignoreBrushes == null && _visualDragGameObject != null)
            {
                List<CSGBrush> foundIgnoreBrushes = new List<CSGBrush>();

                foreach(GameObject obj in _visualDragGameObject)
                {
                    foundIgnoreBrushes.AddRange(obj.GetComponentsInChildren<CSGBrush>());
                }

                _ignoreBrushes = foundIgnoreBrushes.ToArray();
            }
        }

        private void CreateVisualObjects(bool inSceneView = false)
        {
            CleanUp();

            _prevForcedGridCenter   = CSGGrid.ForcedGridCenter;
            _prevForcedGridRotation = CSGGrid.ForcedGridRotation;

            _sourceSurfaceAlignment      = PrefabSourceAlignment.AlignedFront;
            _destinationSurfaceAlignment = PrefabDestinationAlignment.AlignToSurface;

            _visualDragGameObject = new List<GameObject>();

            List<Transform> foundTransforms = new List<Transform>();

            foreach(GameObject obj in _dragGameObjects)
            {
                foundTransforms.AddRange(obj.GetComponentsInChildren<Transform>());
                CSGNode node = obj.GetComponent<CSGNode>();

                if(!node)
                {
                    continue;
                }

                _sourceSurfaceAlignment      = node.prefabSourceAlignment;
                _destinationSurfaceAlignment = node.prefabDestinationAlignment;

                bool createCopyInsteadOfInstance = node && node.prefabBehaviour == PrefabInstantiateBehaviour.Copy;

                GameObject copy = CSGPrefabUtility.Instantiate(obj, createCopyInsteadOfInstance);

                if(!copy)
                {
                    continue;
                }

                copy.name = obj.name;
                _visualDragGameObject.Add(copy);
            }

            _ignoreTransforms = new HashSet<Transform>(foundTransforms);

            if(inSceneView)
            {
                CSGModel model = SelectionUtility.LastUsedModel;

                if(model && !_containsModel)
                {
                    Transform parent  = model.transform;
                    int       counter = 0;

                    foreach(GameObject obj in _visualDragGameObject)
                    {
                        if(!obj)
                        {
                            continue;
                        }

                        if(obj.activeSelf)
                        {
                            obj.transform.SetParent(parent, false);
                            obj.transform.SetSiblingIndex(_hoverSiblingIndex + counter);
                            counter++;
                        }
                    }
                }
            }
            else
            {
                Transform parent  = _hoverParent;
                int       counter = 0;

                foreach(GameObject obj in _visualDragGameObject)
                {
                    if(!obj)
                    {
                        continue;
                    }

                    if(obj.activeSelf)
                    {
                        obj.transform.SetParent(parent, false);
                        obj.transform.SetSiblingIndex(_hoverSiblingIndex + counter);
                        counter++;
                    }
                }
            }
        }

#region DragUpdated

        public bool DragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
        {
            InternalCSGModelManager.SkipCheckForChanges = true;

            try
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                _hoverBrushSurface = null;
                _hoverParent       = !transformInInspector ? null : transformInInspector.parent;
                _hoverSiblingIndex = !transformInInspector ? int.MaxValue : transformInInspector.transform.GetSiblingIndex();

                float middle = (selectionRect.yMax + selectionRect.yMin) * 0.5f;

                if(Event.current.mousePosition.y > middle)
                {
                    _hoverSiblingIndex++;
                }

                _hoverRotation = MathConstants.identityQuaternion;
                _hoverPosition = MathConstants.zeroVector3;
                _haveNoParent  = true;

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
            InternalCSGModelManager.SkipCheckForChanges = true;

            try
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                Camera camera = sceneView.camera;

                LegacyBrushIntersection intersection = SceneQueryUtility.FindMeshIntersection(camera, Event.current.mousePosition, _ignoreBrushes, _ignoreTransforms);

                Vector3 normal = intersection.worldPlane.Normal;

                _hoverPosition     = intersection.worldIntersection;
                _hoverParent       = SelectionUtility.FindParentToAssignTo(intersection);
                _hoverBrushSurface = intersection.brush ? new SelectedBrushSurface(intersection.brush, intersection.surfaceIndex) : null;
                _hoverRotation     = SelectionUtility.FindDragOrientation(sceneView, normal, _sourceSurfaceAlignment, _destinationSurfaceAlignment);

                _haveNoParent      = !_hoverParent;
                _hoverSiblingIndex = int.MaxValue;

                CSGGrid.SetForcedGrid(camera, intersection.worldPlane);

                // Since we're snapping points to grid and do not have a relative distance, relative snapping makes no sense
                bool doGridSnapping = CSGSettings.ActiveSnappingMode != SnapMode.None;

                if(doGridSnapping)
                {
                    Vector3[] localPoints = new Vector3[8];
                    CSGPlane  localPlane  = intersection.worldPlane;

                    for(int i = 0; i < localPoints.Length; i++)
                    {
                        localPoints[i] = GeometryUtility.ProjectPointOnPlane(localPlane, _hoverRotation * _projectedBounds[i] + _hoverPosition);
                    }

                    _hoverPosition += CSGGrid.SnapDeltaToGrid(camera, MathConstants.zeroVector3, localPoints);
                }

                _hoverPosition = GeometryUtility.ProjectPointOnPlane(intersection.worldPlane, _hoverPosition);

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
                InternalCSGModelManager.SkipCheckForChanges = true;

                if(_visualDragGameObject == null)
                {
                    CreateVisualObjects(sceneView != null);
                }

                if(sceneView && _haveNoParent && !_containsModel)
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

                    foreach(GameObject obj in _visualDragGameObject)
                    {
                        if(!obj)
                        {
                            continue;
                        }

                        if(obj.activeSelf)
                        {
                            obj.transform.SetParent(parent, false);
                            obj.transform.SetSiblingIndex(_hoverSiblingIndex + counter);
                            counter++;
                        }
                    }
                }

                if(_visualDragGameObject != null)
                {
                    List<GameObject> selection = new List<GameObject>();

                    for(int i = _visualDragGameObject.Count - 1; i >= 0; i--)
                    {
                        if(!_visualDragGameObject[i])
                        {
                            continue;
                        }

                        if(_visualDragGameObject[i].activeSelf)
                        {
                            Undo.RegisterCreatedObjectUndo(_visualDragGameObject[i], "Instantiated prefab");
                            selection.Add(_visualDragGameObject[i]);
                        }
                        else
                        {
                            Object.DestroyImmediate(_visualDragGameObject[i]);
                        }
                    }

                    _visualDragGameObject = null;

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

                _visualDragGameObject = null;

                InternalCSGModelManager.CheckForChanges(true);
            }
            finally
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
                CSGGrid.ForcedGridCenter                    = _prevForcedGridCenter;
                CSGGrid.ForcedGridRotation                  = _prevForcedGridRotation;
                CSGGrid.ForceGrid                           = false;
            }
        }

#endregion

#region DragExited

        public void DragExited(SceneView sceneView)
        {
            try
            {
                InternalCSGModelManager.SkipCheckForChanges = true;

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
                InternalCSGModelManager.SkipCheckForChanges = false;

                CSGGrid.ForcedGridCenter   = _prevForcedGridCenter;
                CSGGrid.ForcedGridRotation = _prevForcedGridRotation;
                CSGGrid.ForceGrid          = false;
            }
        }

#endregion

#region Paint

        public void OnPaint(Camera camera)
        {
            CSGGrid.RenderGrid(camera);

            if(_hoverBrushSurface == null)
            {
                return;
            }

            CSGBrush brush = _hoverBrushSurface.brush;

            if(brush.childData == null || !brush.childData.modelTransform)
            {
                return;
            }

            int       highlightSurface    = _hoverBrushSurface.surfaceIndex;
            Matrix4x4 brushTransformation = brush.compareTransformation.localToWorldMatrix;

            CSGRenderer.DrawSelectedBrush(brush.brushNodeID, brush.shape, brushTransformation, ColorSettings.WireframeOutline, highlightSurface, false, GUIConstants.OLD_LINE_SCALE);
        }

#endregion
    }
}

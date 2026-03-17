using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class SceneDragToolMeshDragOnSurface : ISceneDragTool
    {
        private SelectedBrushSurface _hoverBrushSurface;
        private Vector3 _hoverPosition;
        private Quaternion _hoverRotation;
        private Transform _hoverParent;
        private int _hoverSiblingIndex;
        private List<GameObject> _dragGameObjects;
        private List<GameObject> _visualDragGameObject;
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

            bool found = false;
            _dragGameObjects = new List<GameObject>();

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

                _dragGameObjects.Add(gameObject);

                if(gameObject.GetComponentInChildren<Renderer>() != null)
                {
                    found = true;
                }
            }

            if(!found || _dragGameObjects.Count != 1)
            {
                _dragGameObjects = null;

                return false;
            }

            AABB dragGameObjectBounds = new AABB();
            dragGameObjectBounds.Reset();

            foreach(GameObject gameObject in _dragGameObjects)
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
        }

        private void DisableVisualObjects()
        {
            if(_visualDragGameObject != null)
            {
                for(int i = _visualDragGameObject.Count - 1; i >= 0; i--)
                {
                    GameObject obj = _visualDragGameObject[i];

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
            if(_visualDragGameObject == null ||
               _visualDragGameObject.Count != _dragGameObjects.Count)
            {
                CreateVisualObjects();
            }
            else
            {
                for(int i = 0; i < _dragGameObjects.Count; i++)
                {
                    if(_visualDragGameObject[i] == null || !_visualDragGameObject[i])
                    {
                        continue;
                    }

                    _visualDragGameObject[i].SetActive(_dragGameObjects[i].activeSelf);
                }
            }

            int counter = 0;

            foreach(GameObject obj in _visualDragGameObject)
            {
                if(obj == null || !obj)
                {
                    continue;
                }

                Vector3    scale    = Vector3.one;
                Quaternion rotation = _hoverRotation;
                Vector3    position = _hoverPosition;

                if(CSGPrefabUtility.IsPrefabInstance(obj))
                {
                    GameObject outer       = CSGPrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                    GameObject prefabAsset = CSGPrefabUtility.GetPrefabAsset(outer);
                    Transform  transform   = prefabAsset.transform;

                    rotation *= transform.localRotation;
                    scale    =  transform.localScale;
                }

                obj.transform.rotation   = rotation;
                obj.transform.position   = position;
                obj.transform.localScale = scale;

                obj.transform.SetSiblingIndex(_hoverSiblingIndex + counter);
                counter++;
            }
        }

        private void CreateVisualObjects()
        {
            CleanUp();

            _prevForcedGridCenter   = CSGGrid.ForcedGridCenter;
            _prevForcedGridRotation = CSGGrid.ForcedGridRotation;

            _sourceSurfaceAlignment      = PrefabSourceAlignment.AlignedFront;
            _destinationSurfaceAlignment = PrefabDestinationAlignment.Default;

            _visualDragGameObject = new List<GameObject>();

            foreach(GameObject obj in _dragGameObjects)
            {
                GameObject copy = CSGPrefabUtility.Instantiate(obj);

                if(!copy)
                {
                    continue;
                }

                copy.name = obj.name;
                _visualDragGameObject.Add(copy);
            }
        }

#region DragUpdated

        public bool DragUpdated(SceneView sceneView, Transform transformInInspector, Rect selectionRect)
        {
            try
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                _hoverBrushSurface = null;
                _hoverParent       = transformInInspector == null ? null : transformInInspector.parent;

                _hoverSiblingIndex = transformInInspector == null
                    ? int.MaxValue
                    : transformInInspector.transform.GetSiblingIndex();

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
            try
            {
                DisableVisualObjects();
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                Camera camera = sceneView.camera;

                LegacyBrushIntersection intersection =
                    SceneQueryUtility.FindMeshIntersection(camera, Event.current.mousePosition);

                Vector3 normal = intersection.worldPlane.normal;

                _hoverPosition = intersection.worldIntersection;
                _hoverParent   = SelectionUtility.FindParentToAssignTo(intersection);

                _hoverBrushSurface = intersection.brush != null
                    ? new SelectedBrushSurface(intersection.brush, intersection.surfaceIndex, intersection.worldPlane)
                    : null;

                _hoverRotation = SelectionUtility.FindDragOrientation(
                    sceneView, normal, _sourceSurfaceAlignment, _destinationSurfaceAlignment
                );

                _haveNoParent      = _hoverParent == null;
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
                        localPoints[i] = GeometryUtility.ProjectPointOnPlane(
                            localPlane, _hoverRotation * _projectedBounds[i] + _hoverPosition
                        );
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
                if(_haveNoParent)
                {
                    if(_visualDragGameObject == null)
                    {
                        CreateVisualObjects();
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

                _visualDragGameObject = null;
            }
            finally
            {
                CSGGrid.ForcedGridCenter   = _prevForcedGridCenter;
                CSGGrid.ForcedGridRotation = _prevForcedGridRotation;
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

            if(brush.ChildData == null || !brush.ChildData.ModelTransform)
            {
                return;
            }

            int       highlightSurface    = _hoverBrushSurface.surfaceIndex;
            Matrix4x4 brushTransformation = brush.compareTransformation.localToWorldMatrix;

            CSGRenderer.DrawSelectedBrush(
                brush.brushNodeID, brush.Shape,
                brushTransformation, ColorSettings.WireframeOutline,
                highlightSurface,
                false, GUIConstants.OLD_LINE_SCALE
            );
        }

#endregion
    }
}

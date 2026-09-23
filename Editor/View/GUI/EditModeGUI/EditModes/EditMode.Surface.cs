using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class EditModeSurface : ScriptableObject, IEditMode
    {
        private static readonly int surfaceSelectPaintControlToolHash = "surfaceSelectPaintControl".GetHashCode();
        private static readonly int textureCopyDragControlToolHash = "textureCopyDragControl".GetHashCode();
        private static readonly int textureSurfaceControlToolHash = "textureSurfaceControl".GetHashCode();

        public bool UsesUnitySelection
        {
            get => false;
        }

        public bool IgnoreUnityRect
        {
            get => true;
        }

        public bool UndoRedoPerformed()
        {
            _forceOutlineUpdate = true;

            return false;
        }

        private enum EditMode
        {
            None
          , TranslateSurface
          , RotateTexture
          , ScaleTexture
        }

        private enum DragMode
        {
            None
          , TextureCopy
          , SurfaceSelectAdd
          , SurfaceSelectRemove
          , SurfaceSelectToggle
        }

        [NonSerialized] private EditMode _editMode = EditMode.None;
        [NonSerialized] private DragMode _dragMode = DragMode.None;

        [NonSerialized] private bool _isEnabled;
        [NonSerialized] private bool _hideTool;
        [NonSerialized] private bool _forceOutlineUpdate;

        [NonSerialized] private int _surfaceSelectPaintControl = -1;
        [NonSerialized] private int _textureCopyDragControlID = -1;

        [NonSerialized] private int _hoverOnSurfaceIndex = -1;
        [NonSerialized] private int _hoverOnBrushNodeID = CSGNode.INVALID_NODE_ID;
        [NonSerialized] private int _hoverOnTarget = -1;
        [NonSerialized] private int _currentControl = -1;
        [NonSerialized] private int _hoverOnlyTarget = -1;
        [NonSerialized] private int _undoGroupIndex = -1;

        [NonSerialized] private readonly RotationCircle _rotationCircle = new RotationCircle();

        [NonSerialized] private Vector2 _deltaMovement;
        [NonSerialized] private Vector3 _originalSurfacePoint;
        [NonSerialized] private Vector3 _snappingOffset;
        [NonSerialized] private Vector3 _snappingOffset2;

        [NonSerialized] private SelectedBrushSurface _lastSelectedSurface;

        [SerializeField] private CSGBrush[] brushTargets = Array.Empty<CSGBrush>();
        [SerializeField] private TexGenState[] surfaceStates = Array.Empty<TexGenState>();
        private SelectedBrushSurface[] _selectedSurfaces = Array.Empty<SelectedBrushSurface>();

#region Selection

        public void SetTargets(FilteredSelection filteredSelection)
        {
            if(brushTargets == null)
            {
                brushTargets        = Array.Empty<CSGBrush>();
                surfaceStates       = Array.Empty<TexGenState>();
                _hoverOnBrushNodeID = CSGNode.INVALID_NODE_ID;
                _hoverOnTarget      = -1;
            }

            HashSet<CSGBrush> foundBrushes = filteredSelection.GetAllContainedBrushes();

            for(int i = surfaceStates.Length - 1; i >= 0; i--)
            {
                if(foundBrushes.Contains(brushTargets[i]))
                {
                    continue;
                }

                ArrayUtility.RemoveAt(ref surfaceStates, i);
                ArrayUtility.RemoveAt(ref brushTargets, i);

                if(_hoverOnTarget > i)
                {
                    _hoverOnTarget--;
                }
                else if(_hoverOnTarget == i)
                {
                    _hoverOnTarget      = -1;
                    _hoverOnBrushNodeID = CSGNode.INVALID_NODE_ID;
                }
            }

            foreach(CSGBrush brush in foundBrushes)
            {
                int index = ArrayUtility.IndexOf(brushTargets, brush);

                if(index != -1)
                {
                    continue;
                }

                ArrayUtility.Add(ref brushTargets, brush);
                TexGenState texGenState = new TexGenState(brush);
                texGenState.SelectAll();
                ArrayUtility.Add(ref surfaceStates, texGenState);
            }

            _hideTool = filteredSelection.nodeTargets.Length > 0;

            if(_isEnabled)
            {
                Tools.hidden = _hideTool;
            }

            UpdateSelectedSurfaces();
            UpdateLayouts();

            _forceOutlineUpdate = true;
        }

        public void SelectAllSurfacesOfSelectedBrushes()
        {
            Undo.RecordObject(this, "Select all surfaces of selected brushes");

            for(int i = surfaceStates.Length - 1; i >= 0; i--)
            {
                if (surfaceStates[i].HaveSurfaceSelection)
                {
                    surfaceStates[i].SelectAll();
                }
            }

            UpdateSelectedSurfaces();
            UpdateLayouts();

            _forceOutlineUpdate = true;
        }

        public void SelectSurfaces(SelectedBrushSurface[] brushSurfaces, HashSet<GameObject> gameObjects, bool selectAllSurfacesOnBrushes)
        {
            if(brushTargets != null && brushTargets.Length > 0)
            {
                for(int i = surfaceStates.Length - 1; i >= 0; i--)
                {
                    if(gameObjects == null || !gameObjects.Contains(brushTargets[i].gameObject))
                    {
                        ArrayUtility.RemoveAt(ref surfaceStates, i);
                        ArrayUtility.RemoveAt(ref brushTargets, i);

                        if(_hoverOnTarget > i)
                        {
                            _hoverOnTarget--;
                        }
                        else if(_hoverOnTarget == i)
                        {
                            _hoverOnTarget      = -1;
                            _hoverOnBrushNodeID = CSGNode.INVALID_NODE_ID;
                        }

                        continue;
                    }

                    surfaceStates[i].DeselectAll();
                }
            }
            else
            {
                brushTargets        = Array.Empty<CSGBrush>();
                surfaceStates       = Array.Empty<TexGenState>();
                _hoverOnTarget      = -1;
                _hoverOnBrushNodeID = CSGNode.INVALID_NODE_ID;
            }

            if(brushSurfaces != null && brushSurfaces.Length > 0)
            {
                for(int j = 0; j < brushSurfaces.Length; j++)
                {
                    int index = ArrayUtility.IndexOf(brushTargets, brushSurfaces[j].brush);

                    if(index != -1)
                    {
                        continue;
                    }

                    ArrayUtility.Add(ref brushTargets, brushSurfaces[j].brush);
                    ArrayUtility.Add(ref surfaceStates, new TexGenState(brushSurfaces[j].brush));
                }

                for(int i = surfaceStates.Length - 1; i >= 0; i--)
                {
                    CSGBrush    brushTarget  = brushTargets[i];
                    TexGenState surfaceState = surfaceStates[i];

                    for(int j = 0; j < brushSurfaces.Length; j++)
                    {
                        if(brushTarget != brushSurfaces[j].brush)
                        {
                            continue;
                        }

                        if(selectAllSurfacesOnBrushes)
                        {
                            for(int k = 0; k < surfaceState.surfaceSelectState.Length; k++)
                            {
                                surfaceState.surfaceSelectState[k] = SelectState.Selected;
                            }
                        }
                        else
                        {
                            int surfaceIndex = brushSurfaces[j].surfaceIndex;

                            if(surfaceIndex >= 0)
                            {
                                surfaceState.surfaceSelectState[surfaceIndex] = SelectState.Selected;
                            }
                        }
                    }
                }

                _lastSelectedSurface = brushSurfaces[0];
            }
            else
            {
                _lastSelectedSurface = null;
            }

            if(gameObjects == null)
            {
                Selection.activeTransform = null;
            }
            else
            {
                Selection.objects = gameObjects.ToArray();
            }

            UpdateSelectedSurfaces();
            UpdateLayouts();

            _forceOutlineUpdate = true;
        }

        private void UpdateLayouts()
        {
            for(int t = 0; t < brushTargets.Length; t++)
            {
                TexGenState surfaceState = surfaceStates[t];
                surfaceState.UpdateLayout(brushTargets[t]);
            }
        }

        private GameObject[] GetSelectedGameObjects()
        {
            GameObject[] gameObjects = new GameObject[brushTargets.Length];

            for(int i = 0; i < brushTargets.Length; i++)
            {
                if(surfaceStates[i].HaveSurfaceSelection)
                {
                    gameObjects[i] = brushTargets[i].gameObject;
                }
            }

            return gameObjects;
        }

        private void UpdateSelectedSurfaces()
        {
            if(surfaceStates == null || surfaceStates.Length == 0)
            {
                _selectedSurfaces = Array.Empty<SelectedBrushSurface>();

                return;
            }

            List<SelectedBrushSurface> selectedSurfaces = new List<SelectedBrushSurface>();

            for(int b = 0; b < surfaceStates.Length; b++)
            {
                CSGBrush brush = brushTargets[b];
                surfaceStates[b].UpdateTexGenState(brush);

                for(int s = 0; s < surfaceStates[b].surfaceSelectState.Length; s++)
                {
                    if((surfaceStates[b].surfaceSelectState[s] & SelectState.Selected) == SelectState.Selected)
                    {
                        selectedSurfaces.Add(new SelectedBrushSurface(brushTargets[b], s));
                    }
                }
            }

            _selectedSurfaces   = selectedSurfaces.ToArray();
            _forceOutlineUpdate = true;
        }

        public SelectedBrushSurface[] GetSelectedSurfaces()
        {
            return _selectedSurfaces;
        }

        private bool IsSelected(LegacyBrushIntersection intersection)
        {
            if(intersection.gameObject == null || intersection.surfaceIndex == -1)
            {
                return false;
            }

            CSGBrush brush      = intersection.gameObject.GetComponent<CSGBrush>();
            int      brushIndex = ArrayUtility.IndexOf(brushTargets, brush);

            if(brushIndex == -1)
            {
                return false;
            }

            return (surfaceStates[brushIndex].surfaceSelectState[intersection.surfaceIndex] & SelectState.Selected) == SelectState.Selected;
        }

        private bool RemoveSurfaceSelection(LegacyBrushIntersection intersection)
        {
            CSGBrush brush      = intersection.gameObject.GetComponent<CSGBrush>();
            int      brushIndex = ArrayUtility.IndexOf(brushTargets, brush);

            if(brushIndex == -1)
            {
                return false;
            }

            Undo.RecordObject(this, "Deselect surface selection");
            surfaceStates[brushIndex].surfaceSelectState[intersection.surfaceIndex] &= ~SelectState.Selected;

            if(brushTargets == null || brushTargets.Length == 0)
            {
                Selection.activeTransform = null;
            }
            else
            {
                Selection.objects = GetSelectedGameObjects();
            }

            UpdateSelectedSurfaces();

            return true;
        }

        private bool AddSurfaceSelection(LegacyBrushIntersection intersection)
        {
            if(intersection.gameObject == null || intersection.surfaceIndex == -1)
            {
                return false;
            }

            CSGBrush brush = intersection.gameObject.GetComponent<CSGBrush>();

            int brushIndex = ArrayUtility.IndexOf(brushTargets, brush);

            if(brushIndex == -1)
            {
                ArrayUtility.Add(ref brushTargets, brush);
                TexGenState newState = new TexGenState(brush);
                ArrayUtility.Add(ref surfaceStates, newState);

                return false;
            }

            Undo.RecordObject(this, "Add surface selection");
            TexGenState surfaceState = surfaceStates[brushIndex];
            surfaceState.surfaceSelectState[intersection.surfaceIndex] |= SelectState.Selected;

            if(brushTargets == null || brushTargets.Length == 0)
            {
                Selection.activeTransform = null;
            }
            else
            {
                Selection.objects = GetSelectedGameObjects();
            }

            UpdateSelectedSurfaces();

            return true;
        }

        private bool UpdateSelection(bool allowSubstraction = true)
        {
            if(_hoverOnTarget == -1)
            {
                return false;
            }

            TexGenState   surfaceState  = surfaceStates[_hoverOnTarget];
            SelectionType selectionType = SelectionUtility.GetEventSelectionType();

            if(!allowSubstraction)
            {
                if(selectionType == SelectionType.Toggle || selectionType == SelectionType.Subtractive)
                {
                    selectionType = SelectionType.Additive;
                }

                if(surfaceState.IsSurfaceSelected(_hoverOnSurfaceIndex))
                {
                    selectionType = SelectionType.Additive;
                }
            }

            Undo.RecordObject(this, "Update selection");

            if(selectionType == SelectionType.Replace)
            {
                for(int t = 0; t < brushTargets.Length; t++)
                {
                    surfaceStates[t].DeselectAll();
                }
            }

            bool repaint = false;

            for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
            {
                bool modified = surfaceState.SelectSurface(s, selectionType);

                repaint = modified || repaint;
            }

            if(brushTargets == null || brushTargets.Length == 0)
            {
                Selection.activeTransform = null;
            }
            else
            {
                Selection.objects = GetSelectedGameObjects();
            }

            UpdateSelectedSurfaces();

            return repaint;
        }

        public bool DeselectAll()
        {
            Selection.activeTransform = null;

            return true;
        }

#endregion

        public void OnEnableTool()
        {
            _isEnabled   = true;
            Tools.hidden = _hideTool;

            ResetTool();
            UpdateSelectedSurfaces();
            UpdateLayouts();
        }

        public void OnDisableTool()
        {
            _isEnabled   = false;
            Tools.hidden = false;

            ResetTool();
        }

        private void ResetTool()
        {
            _outlineGeneration = -1;
            _hoverOnlyTarget   = -1;
            _rotationCircle.Clear();
            _mouseIsDown               = false;
            _mouseIsDragging           = false;
            _firstDrag                 = false;
            _firstMove                 = false;
            _dragMode                  = DragMode.None;
            _textureCopyDragControlID  = -1;
            _surfaceSelectPaintControl = -1;
            _lastSelectedSurface       = null;
        }

        private void CopyBackupTexgens()
        {
            for(int t = 0; t < brushTargets.Length; t++)
            {
                CSGBrush targetBrush = brushTargets[t];
                Shape    targetShape = targetBrush.shape;

                surfaceStates[t].backupTexGens = new TexGen[targetShape.texGens.Length];

                Array.Copy(targetShape.texGens, surfaceStates[t].backupTexGens, targetShape.texGens.Length);
            }
        }

        private void RestoreBackupTexgens()
        {
            for(int t = 0; t < brushTargets.Length; t++)
            {
                CSGBrush targetBrush = brushTargets[t];
                Shape    targetShape = targetBrush.shape;

                for(int s = 0; s < targetShape.surfaces.Length; s++)
                {
                    int texGenIndex = targetShape.surfaces[s].TexGenIndex;

                    if(surfaceStates.Length <= t)
                    {
                        Debug.LogWarning("this.surfaceStates.Length <= t");

                        continue;
                    }

                    if(surfaceStates[t].backupTexGens.Length <= texGenIndex)
                    {
                        Debug.LogWarning("this.surfaceStates[t].backupTexGens.Length <= texGenIndex");

                        continue;
                    }

                    if(targetShape.texGens.Length <= texGenIndex)
                    {
                        Debug.LogWarning("targetShape.TexGens.Length <= texGenIndex");

                        continue;
                    }

                    targetShape.texGens[texGenIndex] = surfaceStates[t].backupTexGens[texGenIndex];
                }
            }
        }

        private EditMode SetHoverOn(EditMode editModeType, int target, int index = -1)
        {
            _hoverOnTarget = target;

            if(target == -1)
            {
                _hoverOnSurfaceIndex = -1;

                return EditMode.None;
            }

            _hoverOnBrushNodeID  = brushTargets[target].brushNodeID;
            _hoverOnSurfaceIndex = index;

            return editModeType;
        }

        private MouseCursor _currentCursor = MouseCursor.Arrow;

        private void UpdateMouseCursor()
        {
            if(_mouseIsDown)
            {
                return;
            }

            _currentCursor = CursorUtility.GetSelectionCursor(SelectionUtility.GetEventSelectionType());
        }

        private bool UpdateGrid(Camera camera)
        {
            CSGBrush brush      = brushTargets[_hoverOnTarget];
            Shape    brushShape = brush.shape;

            int hoverOnTexGenIndex = brushShape.surfaces[_hoverOnSurfaceIndex].TexGenIndex;

            Vector2 oldTranslation = brushShape.texGens[hoverOnTexGenIndex].Translation;
            brushShape.texGens[hoverOnTexGenIndex].Translation = MathConstants.zeroVector2;
            InternalCSGModelManager.SetBrushMeshSurfaces(brush);

            Vector3 zeroPoint = SurfaceUtility.ConvertTextureToModelSpace(brush, _hoverOnSurfaceIndex, MathConstants.zeroVector2);

            brushShape.texGens[hoverOnTexGenIndex].Translation = oldTranslation;
            InternalCSGModelManager.SetBrushMeshSurfaces(brush);

            CSGPlane  movePlane      = brushShape.surfaces[_hoverOnSurfaceIndex].Plane;
            Transform brushTransform = brush.GetComponent<Transform>();

            movePlane = GeometryUtility.InverseTransformPlane(brushTransform.worldToLocalMatrix, movePlane);
            movePlane = new CSGPlane(movePlane.Normal, zeroPoint);

            CSGGrid.SetForcedGrid(camera, movePlane);

            return true;
        }

        private bool DragTextureCopy(SceneView sceneView)
        {
            Camera camera = sceneView.camera;

            if(SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out LegacyBrushIntersection intersection))
            {
                if(_lastSelectedSurface != null && (_lastSelectedSurface.brush != intersection.brush || _lastSelectedSurface.surfaceIndex != intersection.surfaceIndex))
                {
                    if(_lastSelectedSurface.surfacePlane.HasValue)
                    {
                        SurfaceUtility.CopyLastMaterial(intersection.brush, intersection.surfaceIndex, _lastSelectedSurface.brush, _lastSelectedSurface.surfaceIndex);

                        InternalCSGModelManager.RefreshMeshes();
                    }

                    _lastSelectedSurface = null;
                }

                if(_lastSelectedSurface == null)
                {
                    _lastSelectedSurface = new SelectedBrushSurface(intersection.brush, intersection.surfaceIndex, intersection.worldPlane);
                }
            }

            return true;
        }

        private bool DragSurfaceSelect(SceneView sceneView)
        {
            Camera camera = sceneView.camera;

            if(SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out LegacyBrushIntersection intersection))
            {
                if(IsSelected(intersection))
                {
                    if(_dragMode == DragMode.SurfaceSelectToggle)
                    {
                        _dragMode = DragMode.SurfaceSelectRemove;
                    }

                    if(_dragMode == DragMode.SurfaceSelectRemove)
                    {
                        RemoveSurfaceSelection(intersection);
                        CSGEditorGUIUtility.RepaintAll();
                    }
                }
                else
                {
                    if(_dragMode == DragMode.SurfaceSelectToggle)
                    {
                        _dragMode = DragMode.SurfaceSelectAdd;
                    }

                    if(_dragMode == DragMode.SurfaceSelectAdd)
                    {
                        AddSurfaceSelection(intersection);
                        CSGEditorGUIUtility.RepaintAll();
                    }
                }
            }

            return true;
        }

        private bool DragTranslateTextureCoordinates(SceneView sceneView)
        {
            if(_hoverOnTarget == -1 || _hoverOnSurfaceIndex == -1)
            {
                return false;
            }

            CSGBrush brush = brushTargets[_hoverOnTarget];

            if(brush.childData == null || brush.childData.modelTransform == null)
            {
                return false;
            }

            Transform modelTransform      = brush.childData.modelTransform;
            Matrix4x4 modelTransformation = modelTransform.localToWorldMatrix;

            if(_firstMove)
            {
                UpdateSelection(false);
                CopyBackupTexgens();

                TexGenState surfaceState = surfaceStates[_hoverOnTarget];
                _originalSurfacePoint = surfaceState.rayIntersectionWorldPoint;
                _snappingOffset2      = surfaceState.rayIntersectionWorldPoint - _snappingOffset;
                _firstMove            = false;
            }

            Shape       hoverOnShape       = brushTargets[_hoverOnTarget].shape;
            int         hoverOnTexGenIndex = hoverOnShape.surfaces[_hoverOnSurfaceIndex].TexGenIndex;
            TexGenState hoverSurfaceState  = surfaceStates[_hoverOnTarget];
            TexGen      backupTexGen       = hoverSurfaceState.backupTexGens[hoverOnTexGenIndex];

            SelectedBrushSurface[] selectedSurfaces = GetSelectedSurfaces();

            if(selectedSurfaces.Length == 0)
            {
                return true;
            }

            Camera camera = sceneView.camera;

            if(!SceneQueryUtility.FindSurfaceIntersection(camera, brush, modelTransformation, _hoverOnSurfaceIndex, Event.current.mousePosition, out LegacySurfaceIntersection surfaceIntersection))
            {
                return true;
            }

            Vector3 oldWorldPosition = _originalSurfacePoint - _snappingOffset2;
            Vector3 newWorldPosition = surfaceIntersection.worldIntersection - _snappingOffset2;

            // snap texture coordinates in world/local space
            newWorldPosition = GridUtility.FixPosition(newWorldPosition, modelTransform, oldWorldPosition);

            using(new UndoGroup(selectedSurfaces, "Translating surface"))
            {
                RestoreBackupTexgens();

                if(SurfaceUtility.TranslateSurfaces(selectedSurfaces, oldWorldPosition, newWorldPosition))
                {
                    _deltaMovement = hoverOnShape.texGens[hoverOnTexGenIndex].Translation - backupTexGen.Translation;
                }
            }

            return true;
        }

        private bool DragRotateTextureCoordinates(SceneView sceneView)
        {
            if(_hoverOnTarget == -1 || _hoverOnSurfaceIndex == -1)
            {
                return false;
            }

            CSGBrush brush = brushTargets[_hoverOnTarget];

            if(brush.childData == null || brush.childData.modelTransform == null)
            {
                return false;
            }

            Transform modelTransform      = brush.childData.modelTransform;
            Matrix4x4 modelTransformation = modelTransform.localToWorldMatrix;

            if(_firstMove)
            {
                UpdateSelection(false);
                CopyBackupTexgens();

                _firstMove = false;
            }

            Shape       hoverOnShape       = brushTargets[_hoverOnTarget].shape;
            int         hoverOnTexGenIndex = hoverOnShape.surfaces[_hoverOnSurfaceIndex].TexGenIndex;
            TexGenState hoverSurfaceState  = surfaceStates[_hoverOnTarget];
            TexGen      backupTexGen       = hoverSurfaceState.backupTexGens[hoverOnTexGenIndex];

            Camera                 camera           = sceneView.camera;
            SelectedBrushSurface[] selectedSurfaces = GetSelectedSurfaces();

            if(selectedSurfaces.Length == 0 || !SceneQueryUtility.FindSurfaceIntersection(camera, brush, modelTransformation, _hoverOnSurfaceIndex, Event.current.mousePosition, out LegacySurfaceIntersection surfaceIntersection) || !_rotationCircle.UpdateRadius(backupTexGen, surfaceIntersection.worldIntersection))
            {
                return true;
            }

            using(new UndoGroup(selectedSurfaces, "Rotating surface"))
            {
                RestoreBackupTexgens();
                SurfaceUtility.RotateSurfaces(selectedSurfaces, _rotationCircle);
            }

            return true;
        }

        [NonSerialized] private readonly LineMeshManager _selectedVisibleLineMeshManager = new LineMeshManager();
        [NonSerialized] private readonly LineMeshManager _selectedInvisibleLineMeshManager = new LineMeshManager();

        [NonSerialized] private readonly LineMeshManager _hoverVisibleLineMeshManager = new LineMeshManager();
        [NonSerialized] private readonly LineMeshManager _hoverInvisibleLineMeshManager = new LineMeshManager();

        [NonSerialized] private readonly PolygonMeshManager _hoverVisiblePolygonMeshManager = new PolygonMeshManager();

        [NonSerialized]
        private readonly PolygonMeshManager _selectedVisiblePolygonMeshManager = new PolygonMeshManager();

        [NonSerialized] private int _outlineGeneration = -1;
        [NonSerialized] private int _prevHoverSurfaceIndex = -1;
        [NonSerialized] private int _prevHoverBrushNodeID = CSGNode.INVALID_NODE_ID;

        private void OnDestroy()
        {
            _selectedVisibleLineMeshManager.Destroy();
            _selectedInvisibleLineMeshManager.Destroy();
            _selectedVisiblePolygonMeshManager.Destroy();

            _hoverVisibleLineMeshManager.Destroy();
            _hoverInvisibleLineMeshManager.Destroy();
            _hoverVisiblePolygonMeshManager.Destroy();
        }

        private void DrawOutlines()
        {
            if(_outlineGeneration != InternalCSGModelManager.MeshGeneration)
            {
                _outlineGeneration  = InternalCSGModelManager.MeshGeneration;
                _forceOutlineUpdate = true;
            }

            if(_forceOutlineUpdate)
            {
                Color visibleInnerColor   = ColorSettings.PointInnerStateColor[(int)SelectState.Selected];
                Color visibleOuterColor   = ColorSettings.SurfaceOuterStateColor[(int)SelectState.Selected];
                Color visibleOuterline    = ColorSettings.MeshEdgeOutline;
                Color invisibleInnerColor = ColorSettings.PointInnerStateColor[(int)SelectState.Selected];
                Color invisibleOuterColor = ColorSettings.SurfaceOuterStateColor[(int)SelectState.Selected];
                Color invisibleOuterline  = ColorSettings.MeshEdgeOutline;
                Color surfaceColor        = ColorSettings.SurfaceTriangleStateColor[(int)SelectState.Selected];

                invisibleOuterline.a = 0.5f;
                visibleOuterline.a   = 0.5f;

                invisibleInnerColor.a = 1.0f;
                invisibleOuterColor.a = 1.0f;
                visibleOuterColor.a   = 1.0f;
                visibleInnerColor.a   = 1.0f;

                surfaceColor.a = 0.25f;

                List<SelectedBrushSurface> foundSelectedSurfaces = new List<SelectedBrushSurface>();
                List<Matrix4x4>            foundTransformations  = new List<Matrix4x4>();

                for(int t = 0; t < brushTargets.Length; t++)
                {
                    CSGBrush    brush        = brushTargets[t];
                    TexGenState surfaceState = surfaceStates[t];

                    if(!brush.childData.modelTransform)
                    {
                        continue;
                    }

                    Matrix4x4 brushTransformation = brush.compareTransformation.localToWorldMatrix;
                    Shape     brushShape          = brush.shape;

                    for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                    {
                        if(s < 0 || s >= brushShape.surfaces.Length)
                        {
                            continue;
                        }

                        int texGenIndex = brushShape.surfaces[s].TexGenIndex;

                        if(texGenIndex < 0 || texGenIndex >= surfaceState.surfaceSelectState.Length)
                        {
                            continue;
                        }

                        SelectState selectedState = surfaceState.surfaceSelectState[texGenIndex];

                        if((selectedState & SelectState.Selected) == SelectState.None)
                        {
                            continue;
                        }

                        foundSelectedSurfaces.Add(new SelectedBrushSurface(brush, s));
                        foundTransformations.Add(brushTransformation);
                    }
                }

                GeometryWireframe[] outlines = null;

                if(foundSelectedSurfaces.Count > 0)
                {
                    outlines = BrushOutlineManager.GetSurfaceOutlines(foundSelectedSurfaces.ToArray());

                    for(int i = outlines.Length - 1; i >= 0; i--)
                    {
                        if(outlines[i] != null && outlines[i].vertices != null && outlines[i].vertices.Length >= 0)
                        {
                            continue;
                        }

                        ArrayUtility.RemoveAt(ref outlines, i);
                    }
                }

                if(outlines != null && outlines.Length > 0)
                {
                    _selectedVisibleLineMeshManager.Begin();
                    _selectedInvisibleLineMeshManager.Begin();
                    _selectedVisiblePolygonMeshManager.Begin();

                    CSGRenderer.DrawSurfaceOutlines(
                        _selectedVisibleLineMeshManager, _selectedInvisibleLineMeshManager,
                        _selectedVisiblePolygonMeshManager,
                        outlines, foundTransformations.ToArray(),
                        visibleInnerColor, visibleOuterColor, visibleOuterline,
                        invisibleInnerColor, invisibleOuterColor, invisibleOuterline,
                        surfaceColor
                    );

                    _selectedVisiblePolygonMeshManager.End();
                    _selectedInvisibleLineMeshManager.End();
                    _selectedVisibleLineMeshManager.End();
                }
                else
                {
                    _selectedVisiblePolygonMeshManager.Clear();
                    _selectedInvisibleLineMeshManager.Clear();
                    _selectedVisibleLineMeshManager.Clear();
                }
            }

            if(_forceOutlineUpdate || _prevHoverSurfaceIndex != _hoverOnSurfaceIndex || _prevHoverBrushNodeID != _hoverOnBrushNodeID)
            {
                _prevHoverSurfaceIndex = _hoverOnSurfaceIndex;
                _prevHoverBrushNodeID  = _hoverOnBrushNodeID;

                Color visibleInnerColor   = ColorSettings.SurfaceInnerStateColor[(int)SelectState.Hovering];
                Color visibleOuterColor   = ColorSettings.SurfaceOuterStateColor[(int)SelectState.Hovering];
                Color visibleOuterline    = ColorSettings.MeshEdgeOutline;
                Color invisibleInnerColor = ColorSettings.SurfaceInnerStateColor[(int)SelectState.Hovering];
                Color invisibleOuterColor = ColorSettings.SurfaceOuterStateColor[(int)SelectState.Hovering];
                Color invisibleOuterline  = ColorSettings.MeshEdgeOutline;
                Color surfaceColor        = ColorSettings.SurfaceTriangleStateColor[(int)SelectState.Selected];

                invisibleOuterline.a = 0.5f;
                visibleOuterline.a   = 0.5f;

                invisibleInnerColor.a = 1.0f;
                invisibleOuterColor.a = 1.0f;
                visibleOuterColor.a   = 1.0f;
                visibleInnerColor.a   = 1.0f;

                List<SelectedBrushSurface> foundSelectedSurfaces = new List<SelectedBrushSurface>();
                List<Matrix4x4>            foundTransformations  = new List<Matrix4x4>();

                for(int t = 0; t < brushTargets.Length; t++)
                {
                    CSGBrush    brush        = brushTargets[t];
                    TexGenState surfaceState = surfaceStates[t];

                    if(!brush.childData.modelTransform)
                    {
                        continue;
                    }

                    Matrix4x4 brushTranslation = brush.compareTransformation.localToWorldMatrix;
                    Shape     brushShape       = brush.shape;

                    for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                    {
                        if(s >= brushShape.surfaces.Length)
                        {
                            continue;
                        }

                        int texGenIndex = brushShape.surfaces[s].TexGenIndex;

                        if(texGenIndex >= surfaceState.surfaceSelectState.Length)
                        {
                            continue;
                        }

                        if((surfaceState.surfaceSelectState[texGenIndex] & SelectState.Hovering) == SelectState.None)
                        {
                            continue;
                        }

                        foundSelectedSurfaces.Add(new SelectedBrushSurface(brush, s));
                        foundTransformations.Add(brushTranslation);
                    }
                }

                GeometryWireframe[] outlines = null;

                if(foundSelectedSurfaces.Count > 0)
                {
                    outlines = BrushOutlineManager.GetSurfaceOutlines(foundSelectedSurfaces.ToArray());

                    for(int i = outlines.Length - 1; i >= 0; i--)
                    {
                        if(outlines[i] != null && outlines[i].vertices != null && outlines[i].vertices.Length != 0)
                        {
                            continue;
                        }

                        ArrayUtility.RemoveAt(ref outlines, i);
                    }
                }

                if(outlines != null && outlines.Length > 0)
                {
                    _hoverVisibleLineMeshManager.Begin();
                    _hoverInvisibleLineMeshManager.Begin();
                    _hoverVisiblePolygonMeshManager.Begin();

                    CSGRenderer.DrawSurfaceOutlines(
                        _hoverVisibleLineMeshManager, _hoverInvisibleLineMeshManager,
                        _hoverVisiblePolygonMeshManager,
                        outlines, foundTransformations.ToArray(),
                        visibleInnerColor, visibleOuterColor, visibleOuterline,
                        invisibleInnerColor, invisibleOuterColor, invisibleOuterline,
                        surfaceColor
                    );

                    _hoverVisiblePolygonMeshManager.End();
                    _hoverInvisibleLineMeshManager.End();
                    _hoverVisibleLineMeshManager.End();
                }
                else
                {
                    _hoverVisiblePolygonMeshManager.Clear();
                    _hoverInvisibleLineMeshManager.Clear();
                    _hoverVisibleLineMeshManager.Clear();
                }
            }

            Material zTestGenericLineMaterial   = MaterialUtility.ZTestGenericLine;
            Material noZTestGenericLineMaterial = MaterialUtility.NoZTestGenericLine;
            Material coloredPolygonMaterial     = MaterialUtility.ColoredPolygonMaterial;

            _selectedVisiblePolygonMeshManager.Render(coloredPolygonMaterial);
            _hoverVisiblePolygonMeshManager.Render(coloredPolygonMaterial);

            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            _selectedInvisibleLineMeshManager.Render(noZTestGenericLineMaterial);
            _selectedVisibleLineMeshManager.Render(zTestGenericLineMaterial);

            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            _hoverInvisibleLineMeshManager.Render(noZTestGenericLineMaterial);
            _hoverVisibleLineMeshManager.Render(zTestGenericLineMaterial);

            _forceOutlineUpdate = false;
        }

        private void CreateControlIDs()
        {
            _surfaceSelectPaintControl = GUIUtility.GetControlID(surfaceSelectPaintControlToolHash, FocusType.Keyboard);
            _textureCopyDragControlID  = GUIUtility.GetControlID(textureCopyDragControlToolHash, FocusType.Keyboard);

            for(int t = 0; t < brushTargets.Length; t++)
            {
                TexGenState surfaceState = surfaceStates[t];

                if(surfaceState == null)
                {
                    return;
                }

                // assign IDs to every surface
                for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                {
                    surfaceState.surfaceControlID[s] = GUIUtility.GetControlID(textureSurfaceControlToolHash + t, FocusType.Keyboard);
                }
            }
        }

        [NonSerialized] private bool _mouseIsDown;
        [NonSerialized] private bool _mouseIsDragging;
        [NonSerialized] private bool _firstMove;
        [NonSerialized] private bool _firstDrag;

        [NonSerialized] private Vector2 _prevMousePos;

        private bool _clickedInScene;

        public void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera = sceneView.camera;

            EventType originalEventType = Event.current.type;

            if(originalEventType == EventType.MouseMove)
            {
                _mouseIsDragging = false;
            }
            else if(originalEventType == EventType.MouseDown)
            {
                _mouseIsDragging = false;
                _mouseIsDown     = true;
                _prevMousePos    = Event.current.mousePosition;
            }
            else if(originalEventType == EventType.MouseDrag)
            {
                _mouseIsDown = true;

                if(!_mouseIsDragging && (_prevMousePos - Event.current.mousePosition).magnitude > 4.0f)
                {
                    _mouseIsDragging = true;
                }
            }

            try
            {
                CreateControlIDs();

                switch(Event.current.type)
                {
                    case EventType.MouseDown:
                    {
                        _clickedInScene = false;

                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        Rect guiArea = GetLastSceneGUIRect();

                        if(guiArea.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        _clickedInScene = true;

                        break;
                    }
                    case EventType.MouseUp:
                    case EventType.MouseDrag:
                    {
                        if(!_clickedInScene || Event.current.button != 0 || GUIUtility.hotControl != 0)
                        {
                            break;
                        }

                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        Rect guiArea = GetLastSceneGUIRect();

                        if(guiArea.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        if(SceneQueryUtility.FindUnityWorldIntersection(camera, Event.current.mousePosition, out GameObject gameObject))
                        {
                            if(gameObject == null && Event.current.type == EventType.MouseUp)
                            {
                                SelectionUtility.DeselectAll();
                            }
                            else
                            {
                                SelectionUtility.DoSelectionClick(sceneView);
                            }

                            break;
                        }

                        if(_dragMode == DragMode.TextureCopy)
                        {
                            if(Event.current.modifiers == EventModifiers.None)
                            {
                                _firstDrag                        = true;
                                _currentControl                   = _textureCopyDragControlID;
                                GUIUtility.hotControl             = _textureCopyDragControlID;
                                GUIUtility.keyboardControl        = _textureCopyDragControlID;
                                EditorGUIUtility.editingTextField = false;

                                if(Event.current.type != EventType.MouseUp)
                                {
                                    Event.current.Use();
                                }
                            }

                            break;
                        }

                        SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                        _dragMode = selectionType switch
                        {
                            SelectionType.Additive    => DragMode.SurfaceSelectAdd
                          , SelectionType.Subtractive => DragMode.SurfaceSelectRemove
                          , SelectionType.Toggle      => DragMode.SurfaceSelectToggle
                          , SelectionType.Replace     => DragMode.None
                          , var _                     => _dragMode
                        };

                        if(_dragMode != DragMode.None)
                        {
                            _currentControl                   = _surfaceSelectPaintControl;
                            GUIUtility.hotControl             = _surfaceSelectPaintControl;
                            GUIUtility.keyboardControl        = _surfaceSelectPaintControl;
                            EditorGUIUtility.editingTextField = false;

                            if(Event.current.type != EventType.MouseUp)
                            {
                                Event.current.Use();

                                break;
                            }
                        }

                        if(Event.current.type != EventType.MouseUp && Event.current.modifiers != EventModifiers.None)
                        {
                            break;
                        }

                        if(_hoverOnTarget == -1 || _hoverOnSurfaceIndex == -1)
                        {
                            if(Event.current.type == EventType.MouseUp && !_mouseIsDragging)
                            {
                                // see if we clicked into empty space
                                if(_hoverOnTarget == -1 && _hoverOnSurfaceIndex == -1)
                                {
                                    SelectionUtility.DeselectAll();
                                }
                            }

                            break;
                        }

                        int newControlID = surfaceStates[_hoverOnTarget].surfaceControlID[_hoverOnSurfaceIndex];

                        if(newControlID <= 0)
                        {
                            break;
                        }

                        _currentControl                   = newControlID;
                        GUIUtility.hotControl             = newControlID;
                        GUIUtility.keyboardControl        = newControlID;
                        EditorGUIUtility.editingTextField = false;

                        if(Event.current.type != EventType.MouseUp)
                        {
                            Event.current.Use();
                        }

                        _firstMove = true;

                        Undo.IncrementCurrentGroup();
                        _undoGroupIndex = Undo.GetCurrentGroup();

                        if(Tools.current == Tool.Rotate)
                        {
                            CSGBrush brush = brushTargets[_hoverOnTarget];

                            _rotationCircle.Initialize(brush.GetComponent<Transform>(), brush.shape.surfaces[_hoverOnSurfaceIndex], surfaceStates[_hoverOnTarget].rayIntersectionWorldPoint);
                        }
                        else if(Tools.current == Tool.Move)
                        {
                            CSGBrush brush = brushTargets[_hoverOnTarget];
                            InternalCSGModelManager.SetBrushMeshSurfaces(brush);

                            _snappingOffset = SurfaceUtility.ConvertTextureToModelSpace(brush, _hoverOnSurfaceIndex, MathConstants.zeroVector2);
                        }

                        break;
                    }

                    case EventType.KeyDown:
                    {
                        SceneQueryUtility.ClearDeepClick();

                        if(Keys.cancelActionKey.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.copyMaterialTexGen.IsKeyPressed())
                        {
                            if(_dragMode == DragMode.None)
                            {
                                _dragMode = DragMode.TextureCopy;
                            }

                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.KeyUp:
                    {
                        if(Keys.cancelActionKey.IsKeyPressed())
                        {
                            SelectionUtility.DeselectAll();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.copyMaterialTexGen.IsKeyPressed())
                        {
                            if(_dragMode == DragMode.TextureCopy)
                            {
                                _dragMode = DragMode.None;
                            }

                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.ValidateCommand:
                    {
                        if(Keys.cancelActionKey.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.copyMaterialTexGen.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneValidate(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.Repaint:
                    {
                        if(SceneDragToolManager.IsDraggingObjectInScene)
                        {
                            break;
                        }

                        if(sceneView != null)
                        {
                            if(!Tools.viewToolActive)
                            {
                                Rect windowRect = new Rect(0, 0, sceneView.position.width, sceneView.position.height - CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT);

                                if(_currentCursor != MouseCursor.Arrow)
                                {
                                    EditorGUIUtility.AddCursorRect(windowRect, _currentCursor);
                                }
                            }
                        }

                        if(_currentControl == -1 || _currentControl == _surfaceSelectPaintControl)
                        {
                            DrawOutlines();
                        }

                        break;
                    }
                    case EventType.Layout:
                    {
                        UpdateMouseCursor();

                        if(_currentControl != -1 || camera == null || !camera.pixelRect.Contains(Event.current.mousePosition))
                        {
                            return;
                        }

                        bool repaint = false;

                        if(brushTargets == null)
                        {
                            brushTargets  = Array.Empty<CSGBrush>();
                            surfaceStates = Array.Empty<TexGenState>();
                        }

                        Matrix4x4 origMatrix = Handles.matrix;
                        Handles.matrix = MathConstants.identityMatrix;

                        int hoverControl = -1;
                        int hotControl   = GUIUtility.hotControl;

                        for(int t = 0; t < brushTargets.Length; t++)
                        {
                            CSGBrush    brush        = brushTargets[t];
                            TexGenState surfaceState = surfaceStates[t];
                            surfaceState.UpdateLayout(brush);

                            for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                            {
                                // see if this control ID is 'hot'
                                if(hotControl == surfaceState.surfaceControlID[s])
                                {
                                    hoverControl = surfaceState.surfaceControlID[s];
                                }
                            }
                        }

                        // check if we already have a controlID, otherwise try to see if we're hovering over a surface
                        if(hoverControl == -1)
                        {
                            _hoverOnSurfaceIndex = -1;
                            _hoverOnTarget       = -1;
                            CSGGrid.ForceGrid    = false;

                            int closestBrush   = -1;
                            int closestSurface = -1;

                            Ray     worldRay  = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            Vector3 rayStart  = worldRay.origin;
                            Vector3 rayVector = worldRay.direction * camera.farClipPlane;
                            Vector3 rayEnd    = rayStart + rayVector;

                            Rect                    guiArea        = GetLastSceneGUIRect();
                            bool                    wireframeShown = CSGSettings.IsWireframeShown(camera);
                            LegacyBrushIntersection intersection   = null;

                            if(!_mouseIsDragging && !guiArea.Contains(Event.current.mousePosition) && SceneQueryUtility.FindWorldIntersection(rayStart, rayEnd, out intersection, true, !wireframeShown))
                            {
                                CSGBrush[] targets = brushTargets;
                                CSGBrush   brush   = intersection.brush;

                                if(!ArrayUtility.Contains(targets, brush))
                                {
                                    if(_hoverOnlyTarget > 0 && _hoverOnlyTarget < surfaceStates.Length)
                                    {
                                        TexGenState surfaceState = surfaceStates[_hoverOnlyTarget];
                                        bool        haveSelected = false;

                                        for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                                        {
                                            if((surfaceState.surfaceSelectState[s] & SelectState.Selected) == SelectState.Selected)
                                            {
                                                haveSelected = true;
                                            }
                                        }

                                        if(haveSelected)
                                        {
                                            _hoverOnlyTarget = -1;
                                        }
                                    }

                                    if(_hoverOnlyTarget > 0 && _hoverOnlyTarget < surfaceStates.Length)
                                    {
                                        targets[_hoverOnlyTarget]       = brush;
                                        surfaceStates[_hoverOnlyTarget] = new TexGenState(brush);
                                    }
                                    else
                                    {
                                        ArrayUtility.Add(ref targets, brush);
                                        TexGenState newState = new TexGenState(brush);
                                        ArrayUtility.Add(ref surfaceStates, newState);

                                        brushTargets     = targets;
                                        _hoverOnlyTarget = targets.Length - 1;
                                    }
                                }
                            }

                            float minDistance = float.PositiveInfinity;

                            for(int t = 0; t < brushTargets.Length; t++)
                            {
                                CSGBrush    brush        = brushTargets[t];
                                TexGenState surfaceState = surfaceStates[t];

                                if(brush.childData == null || brush.childData.modelTransform == null)
                                {
                                    continue;
                                }

                                if(intersection == null || intersection.brushNodeID != brush.brushNodeID)
                                {
                                    surfaceState.rayIntersectionWorldPoint   = MathConstants.zeroVector3;
                                    surfaceState.rayIntersectionSurfacePoint = MathConstants.zeroVector3;

                                    continue;
                                }

                                float distance = (intersection.worldIntersection - rayStart).magnitude;

                                if(distance < minDistance)
                                {
                                    minDistance    = distance;
                                    closestBrush   = t;
                                    closestSurface = intersection.surfaceIndex;
                                }

                                surfaceState.rayIntersectionWorldPoint   = intersection.worldIntersection;
                                surfaceState.rayIntersectionSurfacePoint = intersection.surfaceIntersection;
                            }

                            for(int t = 0; t < brushTargets.Length; t++)
                            {
                                TexGenState surfaceState = surfaceStates[t];

                                for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                                {
                                    HandleUtility.AddControl(surfaceState.surfaceControlID[s], closestBrush == t && closestSurface == s ? 0.0f : float.PositiveInfinity);
                                }
                            }
                        }

                        int nearestControl = HandleUtility.nearestControl;

                        if(hoverControl != -1)
                        {
                            nearestControl = hoverControl;
                        }
                        else if(hotControl != 0)
                        {
                            nearestControl = -1;
                        }

                        SelectState[] oldSurfaceStates = Array.Empty<SelectState>();
                        EditMode      newEditMode      = EditMode.None;

                        for(int t = 0; t < brushTargets.Length; t++)
                        {
                            TexGenState surfaceState = surfaceStates[t];

                            if(!repaint)
                            {
                                if(oldSurfaceStates.Length < surfaceState.surfaceSelectState.Length)
                                {
                                    oldSurfaceStates = new SelectState[surfaceState.surfaceSelectState.Length];
                                }

                                Array.Copy(surfaceState.surfaceSelectState, oldSurfaceStates, surfaceState.surfaceSelectState.Length);
                            }

                            if(nearestControl != -1)
                            {
                                surfaceState.UnHoverAll();

                                if(newEditMode == EditMode.None)
                                {
                                    for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                                    {
                                        if(surfaceState.surfaceControlID[s] == nearestControl)
                                        {
                                            newEditMode                        =  SetHoverOn(EditMode.TranslateSurface, t, s);
                                            surfaceState.surfaceSelectState[s] |= SelectState.Hovering;

                                            if((surfaceState.surfaceSelectState[s] & SelectState.Selected) == SelectState.Selected && _currentCursor == MouseCursor.Arrow)
                                            {
                                                _currentCursor = CursorUtility.GetToolCursor();
                                            }

                                            break;
                                        }
                                    }
                                }
                            }

                            if(!repaint)
                            {
                                for(int p = 0; p < surfaceState.surfaceSelectState.Length; p++)
                                {
                                    if(surfaceState.surfaceSelectState[p] != oldSurfaceStates[p] || oldSurfaceStates[p] == SelectState.None)
                                    {
                                        repaint = true;
                                    }
                                }
                            }
                        }

                        _editMode      = newEditMode;
                        Handles.matrix = origMatrix;

                        if(_hoverOnTarget >= 0 && _hoverOnSurfaceIndex >= 0 && surfaceStates.Length > _hoverOnTarget && surfaceStates[_hoverOnTarget].surfaceSelectState.Length > _hoverOnSurfaceIndex)
                        {
                            if((surfaceStates[_hoverOnTarget].surfaceSelectState[_hoverOnSurfaceIndex] & SelectState.Selected) != SelectState.Selected)
                            {
                                if(_currentCursor == MouseCursor.Arrow)
                                {
                                    _currentCursor = MouseCursor.ArrowPlus;
                                }
                            }
                        }

                        if(repaint)
                        {
                            CSGEditorGUIUtility.RepaintAll();
                        }

                        break;
                    }
                }

                int currentHotControl = GUIUtility.hotControl;

                if(_dragMode != DragMode.TextureCopy)
                {
                    for(int t = 0; t < surfaceStates.Length; t++)
                    {
                        TexGenState surfaceState    = surfaceStates[t];
                        int[]       surfaceControls = surfaceState.surfaceControlID;

                        for(int s = 0; s < surfaceControls.Length; s++)
                        {
                            int controlID = surfaceStates[t].surfaceControlID[s];

                            if(currentHotControl != controlID)
                            {
                                continue;
                            }

                            switch(Event.current.GetTypeForControl(controlID))
                            {
                                case EventType.MouseDrag:
                                {
                                    if(Event.current.button != 0 || (Event.current.modifiers & ~EventModifiers.Control) != EventModifiers.None || GUIUtility.hotControl != controlID || controlID <= 0 || CSGSettings.AllowTextureCoordinateDragging == false)
                                    {
                                        break;
                                    }

                                    if(Tools.current == Tool.Move)
                                    {
                                        DragTranslateTextureCoordinates(sceneView);
                                    }
                                    else if(Tools.current == Tool.Rotate)
                                    {
                                        DragRotateTextureCoordinates(sceneView);
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Please change the Unity Tool to Move or Rotate");
                                    }

                                    break;
                                }
                                case EventType.MouseUp:
                                {
                                    if(Event.current.button != 0 || GUIUtility.hotControl != controlID)
                                    {
                                        break;
                                    }

                                    if(!_mouseIsDragging)
                                    {
                                        // see if we clicked into empty space
                                        if(_hoverOnTarget == -1 && _hoverOnSurfaceIndex == -1)
                                        {
                                            SelectionUtility.DeselectAll();
                                        }
                                        else
                                        {
                                            UpdateSelection();
                                        }
                                    }
                                    else
                                    {
                                        Undo.CollapseUndoOperations(_undoGroupIndex);
                                        Undo.IncrementCurrentGroup();
                                        Undo.FlushUndoRecordObjects();
                                    }

                                    GUIUtility.hotControl             = 0;
                                    GUIUtility.keyboardControl        = 0;
                                    EditorGUIUtility.editingTextField = false;
                                    _currentControl                   = -1;

                                    Event.current.Use();

                                    break;
                                }
                                case EventType.Repaint:
                                {
                                    if(GUIUtility.hotControl != controlID)
                                    {
                                        break;
                                    }

                                    if(Tools.current == Tool.Rotate)
                                    {
                                        if(_dragMode == DragMode.None)
                                        {
                                            if(_mouseIsDown && _hoverOnTarget != -1 && _hoverOnSurfaceIndex != -1)
                                            {
                                                _rotationCircle.Render(camera);
                                            }
                                        }
                                    }
                                    else if(Tools.current == Tool.Move)
                                    {
                                        if(_editMode == EditMode.TranslateSurface && _mouseIsDragging)
                                        {
                                            Vector2 textCenter2D = Event.current.mousePosition;
                                            textCenter2D.x += 25;
                                            textCenter2D.y += 60;

                                            PaintUtility.DrawScreenText(textCenter2D, Units.ToRoundedPixelsString(_deltaMovement));
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                EventType type = Event.current.GetTypeForControl(_textureCopyDragControlID);

                switch(type)
                {
                    case EventType.MouseDrag:
                    {
                        if(Event.current.button != 0 || Event.current.modifiers != EventModifiers.None || GUIUtility.hotControl != _textureCopyDragControlID || _textureCopyDragControlID <= 0)
                        {
                            break;
                        }

                        if(_dragMode == DragMode.TextureCopy)
                        {
                            if(_firstDrag)
                            {
                                _lastSelectedSurface = null;
                                _firstDrag           = false;
                            }

                            DragTextureCopy(sceneView);
                        }

                        Event.current.Use();

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(Event.current.button != 0 || GUIUtility.hotControl != _textureCopyDragControlID)
                        {
                            break;
                        }

                        if(_dragMode == DragMode.TextureCopy)
                        {
                            if(_mouseIsDragging)
                            {
                                _lastSelectedSurface = null;
                            }
                            else
                            {
                                DragTextureCopy(sceneView);
                            }
                        }

                        if(!_mouseIsDragging)
                        {
                            // see if we clicked into empty space
                            if(_hoverOnTarget == -1 && _hoverOnSurfaceIndex == -1)
                            {
                                SelectionUtility.DeselectAll();
                            }
                            else
                            {
                                UpdateSelection();
                            }
                        }

                        GUIUtility.hotControl             = 0;
                        GUIUtility.keyboardControl        = 0;
                        EditorGUIUtility.editingTextField = false;
                        _currentControl                   = -1;
                        Event.current.Use();
                        _firstDrag = false;

                        break;
                    }
                }

                type = Event.current.GetTypeForControl(_surfaceSelectPaintControl);

                switch(type)
                {
                    case EventType.MouseDrag:
                    {
                        if(Event.current.button != 0 || GUIUtility.hotControl != _surfaceSelectPaintControl || _surfaceSelectPaintControl <= 0)
                        {
                            break;
                        }

                        if(_dragMode == DragMode.SurfaceSelectAdd || _dragMode == DragMode.SurfaceSelectRemove || _dragMode == DragMode.SurfaceSelectToggle)
                        {
                            DragSurfaceSelect(sceneView);
                        }

                        Event.current.Use();

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(Event.current.button != 0 || GUIUtility.hotControl != _surfaceSelectPaintControl)
                        {
                            break;
                        }

                        if(!_mouseIsDragging)
                        {
                            // see if we clicked into empty space
                            if(_hoverOnTarget == -1 && _hoverOnSurfaceIndex == -1)
                            {
                                SelectionUtility.DeselectAll();
                            }
                            else
                            {
                                UpdateSelection();
                            }
                        }

                        GUIUtility.hotControl             = 0;
                        GUIUtility.keyboardControl        = 0;
                        EditorGUIUtility.editingTextField = false;
                        _currentControl                   = -1;

                        Event.current.Use();

                        break;
                    }
                }
            }
            finally
            {
                if(originalEventType == EventType.MouseUp || originalEventType == EventType.MouseMove)
                {
                    _mouseIsDragging = false;
                    _mouseIsDown     = false;
                }
            }
        }

        private static Vector2 m_ScrollPos;

        public void OnInspectorGUI(EditorWindow window, float height)
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            EditModeSurfaceGUI.OnInspectorGUI(window, height);
            EditorGUILayout.EndScrollView();
        }

        public Rect GetLastSceneGUIRect()
        {
            return EditModeSurfaceGUI.GetLastSceneGUIRect(this);
        }

        public bool OnSceneGUI(Rect windowRect)
        {
            if(brushTargets == null)
            {
                return false;
            }

            EditModeSurfaceGUI.OnSceneGUI(windowRect, this);

            return true;
        }
    }
}

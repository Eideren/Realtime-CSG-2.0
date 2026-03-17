using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal sealed class EditModeSurface : ScriptableObject, IEditMode
    {
        private static readonly int surfaceSelectPaintControlToolHash = "surfaceSelectPaintControl".GetHashCode();
        private static readonly int textureCopyDragControlToolHash = "textureCopyDragControl".GetHashCode();

        private static readonly int textureSurfaceControlToolHash = "textureSurfaceControl".GetHashCode();
        //static readonly int surfaceEditBrushToolHash			= "surfaceEditBrushTool".GetHashCode();

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
            forceOutlineUpdate = true;

            return false;
        }

        private enum EditMode
        {
            None
          , TranslateSurface
          , RotateTexture
          , ScaleTexture
        };

        private enum DragMode
        {
            None
          , TextureCopy
          , SurfaceSelectAdd
          , SurfaceSelectRemove
          , SurfaceSelectToggle
        };

        [NonSerialized] private EditMode editMode = EditMode.None;
        [NonSerialized] private DragMode dragMode = DragMode.None;

        [NonSerialized] private bool isEnabled = false;
        [NonSerialized] private bool hideTool = false;
        [NonSerialized] private bool forceOutlineUpdate = false;

        [NonSerialized] private int surfaceSelectPaintControl = -1;
        [NonSerialized] private int textureCopyDragControlID = -1;

        [NonSerialized] private int hoverOnSurfaceIndex = -1;
        [NonSerialized] private int hoverOnBrushNodeID = CSGNode.InvalidNodeID;
        [NonSerialized] private int hoverOnTarget = -1;
        [NonSerialized] private int currentControl = -1;
        [NonSerialized] private int hoverOnlyTarget = -1;
        [NonSerialized] private int undoGroupIndex = -1;

        [NonSerialized] private RotationCircle rotationCircle = new RotationCircle();

        [NonSerialized] private Vector2 deltaMovement;
        [NonSerialized] private Vector3 originalSurfacePoint;
        [NonSerialized] private Vector3 snappingOffset;
        [NonSerialized] private Vector3 snappingOffset2;

        [NonSerialized] private SelectedBrushSurface lastSelectedSurface = null;

        [SerializeField] private CSGBrush[] brushTargets = new CSGBrush[0];
        [SerializeField] private TexGenState[] surfaceStates = new TexGenState[0];
        [SerializeField] private SelectedBrushSurface[] selectedSurfaces = new SelectedBrushSurface[0];

#region Selection

        public void SetTargets(FilteredSelection filteredSelection)
        {
            if(brushTargets == null)
            {
                brushTargets       = new CSGBrush[0];
                surfaceStates      = new TexGenState[0];
                hoverOnBrushNodeID = CSGNode.InvalidNodeID;
                hoverOnTarget      = -1;
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

                if(hoverOnTarget > i)
                {
                    hoverOnTarget--;
                }
                else if(hoverOnTarget == i)
                {
                    hoverOnTarget      = -1;
                    hoverOnBrushNodeID = CSGNode.InvalidNodeID;
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

            hideTool = filteredSelection.nodeTargets.Length > 0;

            if(isEnabled)
            {
                Tools.hidden = hideTool;
            }

            UpdateSelectedSurfaces();
            UpdateLayouts();
            forceOutlineUpdate = true;
        }

        public void SelectAllSurfacesOfSelectedBrushes()
        {
            Undo.RecordObject(this, "Select all surfaces of selected brushes");

            for(int i = surfaceStates.Length - 1; i >= 0; i--)
            {
                surfaceStates[i].SelectAll();
            }

            UpdateSelectedSurfaces();
            UpdateLayouts();
            forceOutlineUpdate = true;
        }

        public void SelectSurfaces(
            SelectedBrushSurface[] brushSurfaces, HashSet<GameObject> gameObjects, bool selectAllSurfacesOnBrushes
        )
        {
            if(brushTargets != null &&
               brushTargets.Length > 0)
            {
                for(int i = surfaceStates.Length - 1; i >= 0; i--)
                {
                    if(gameObjects == null ||
                       !gameObjects.Contains(brushTargets[i].gameObject))
                    {
                        ArrayUtility.RemoveAt(ref surfaceStates, i);
                        ArrayUtility.RemoveAt(ref brushTargets, i);

                        if(hoverOnTarget > i)
                        {
                            hoverOnTarget--;
                        }
                        else if(hoverOnTarget == i)
                        {
                            hoverOnTarget      = -1;
                            hoverOnBrushNodeID = CSGNode.InvalidNodeID;
                        }

                        continue;
                    }

                    surfaceStates[i].DeselectAll();
                }
            }
            else
            {
                brushTargets       = new CSGBrush[0];
                surfaceStates      = new TexGenState[0];
                hoverOnTarget      = -1;
                hoverOnBrushNodeID = CSGNode.InvalidNodeID;
            }

            if(brushSurfaces != null &&
               brushSurfaces.Length > 0)
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

                lastSelectedSurface = brushSurfaces[0];
            }
            else
            {
                lastSelectedSurface = null;
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
            forceOutlineUpdate = true;
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
            if(surfaceStates == null ||
               surfaceStates.Length == 0)
            {
                selectedSurfaces = new SelectedBrushSurface[0];

                return;
            }

            List<SelectedBrushSurface> selected_surfaces = new List<SelectedBrushSurface>();

            for(int b = 0; b < surfaceStates.Length; b++)
            {
                CSGBrush brush = brushTargets[b];
                surfaceStates[b].UpdateTexGenState(brush);

                for(int s = 0; s < surfaceStates[b].surfaceSelectState.Length; s++)
                {
                    if((surfaceStates[b].surfaceSelectState[s] & SelectState.Selected) == SelectState.Selected)
                    {
                        selected_surfaces.Add(new SelectedBrushSurface(brushTargets[b], s));
                    }
                }
            }

            selectedSurfaces   = selected_surfaces.ToArray();
            forceOutlineUpdate = true;
        }

        public SelectedBrushSurface[] GetSelectedSurfaces()
        {
            return selectedSurfaces;
        }

        private bool IsSelected(LegacyBrushIntersection intersection)
        {
            if(intersection.gameObject == null ||
               intersection.surfaceIndex == -1)
            {
                return false;
            }

            CSGBrush brush      = intersection.gameObject.GetComponent<CSGBrush>();
            int      brushIndex = ArrayUtility.IndexOf(brushTargets, brush);

            if(brushIndex == -1)
            {
                return false;
            }

            if((surfaceStates[brushIndex].surfaceSelectState[intersection.surfaceIndex] & SelectState.Selected)
            == SelectState.Selected)
            {
                return true;
            }

            return false;
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
            if(intersection.gameObject == null ||
               intersection.surfaceIndex == -1)
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
            if(hoverOnTarget == -1)
            {
                return false;
            }

            TexGenState   surfaceState  = surfaceStates[hoverOnTarget];
            SelectionType selectionType = SelectionUtility.GetEventSelectionType();

            if(!allowSubstraction)
            {
                if(selectionType == SelectionType.Toggle ||
                   selectionType == SelectionType.Subtractive)
                {
                    selectionType = SelectionType.Additive;
                }

                if(surfaceState.IsSurfaceSelected(hoverOnSurfaceIndex))
                {
                    selectionType = SelectionType.Additive;
                }
            }

            if(selectionType == SelectionType.Replace ||
               selectionType == SelectionType.Additive)
            {
                //	if (surfaceState.IsSurfaceSelected(hoverOnSurfaceIndex))
                //		return false;
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

                repaint = modified || repaint; /*
                if (modified && (surfaceState.surfaceSelectState[s] & SelectState.Selected) == SelectState.Selected)
                {
                    lastSelectedSurface = new SelectedBrushSurface(brushTargets[hoverOnTarget], s);
                }*/
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
            isEnabled    = true;
            Tools.hidden = hideTool;
            ResetTool();
            UpdateSelectedSurfaces();
            UpdateLayouts();
        }

        public void OnDisableTool()
        {
            isEnabled    = false;
            Tools.hidden = false;
            ResetTool();
        }

        private void ResetTool()
        {
            outlineGeneration = -1;
            hoverOnlyTarget   = -1;
            rotationCircle.Clear();
            mouseIsDown               = false;
            mouseIsDragging           = false;
            firstDrag                 = false;
            firstMove                 = false;
            dragMode                  = DragMode.None;
            textureCopyDragControlID  = -1;
            surfaceSelectPaintControl = -1;
            lastSelectedSurface       = null;
        }

        private void CopyBackupTexgens()
        {
            for(int t = 0; t < brushTargets.Length; t++)
            {
                CSGBrush targetBrush = brushTargets[t];
                Shape    targetShape = targetBrush.Shape;

                surfaceStates[t].backupTexGens = new TexGen[targetShape.TexGens.Length];
                Array.Copy(targetShape.TexGens, surfaceStates[t].backupTexGens, targetShape.TexGens.Length);
            }
        }

        private void RestoreBackupTexgens()
        {
            for(int t = 0; t < brushTargets.Length; t++)
            {
                CSGBrush targetBrush = brushTargets[t];
                Shape    targetShape = targetBrush.Shape;

                for(int s = 0; s < targetShape.Surfaces.Length; s++)
                {
                    int texGenIndex = targetShape.Surfaces[s].TexGenIndex;

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

                    if(targetShape.TexGens.Length <= texGenIndex)
                    {
                        Debug.LogWarning("targetShape.TexGens.Length <= texGenIndex");

                        continue;
                    }

                    targetShape.TexGens[texGenIndex] = surfaceStates[t].backupTexGens[texGenIndex];
                }
            }
        }

        private EditMode SetHoverOn(EditMode editModeType, int target, int index = -1)
        {
            hoverOnTarget = target;

            if(target == -1)
            {
                hoverOnSurfaceIndex = -1;

                return EditMode.None;
            }

            hoverOnBrushNodeID  = brushTargets[target].brushNodeID;
            hoverOnSurfaceIndex = index;

            return editModeType;
        }

        private MouseCursor currentCursor = MouseCursor.Arrow;

        private void UpdateMouseCursor()
        {
            if(mouseIsDown)
            {
                return;
            }

            currentCursor = CursorUtility.GetSelectionCursor(SelectionUtility.GetEventSelectionType());
        }

        private bool UpdateGrid(Camera camera)
        {
            CSGBrush brush = brushTargets[hoverOnTarget];

            Shape brush_shape = brush.Shape;

            int hoverOnTexGenIndex = brush_shape.Surfaces[hoverOnSurfaceIndex].TexGenIndex;

            Vector2 old_translation = brush_shape.TexGens[hoverOnTexGenIndex].Translation;
            brush_shape.TexGens[hoverOnTexGenIndex].Translation = MathConstants.zeroVector2;
            InternalCSGModelManager.SetBrushMeshSurfaces(brush);

            Vector3 zero_point = SurfaceUtility.ConvertTextureToModelSpace(
                brush, hoverOnSurfaceIndex, MathConstants.zeroVector2
            );

            brush_shape.TexGens[hoverOnTexGenIndex].Translation = old_translation;
            InternalCSGModelManager.SetBrushMeshSurfaces(brush);

            CSGPlane  movePlane       = brush_shape.Surfaces[hoverOnSurfaceIndex].Plane;
            Transform brush_transform = brush.GetComponent<Transform>();

            movePlane = GeometryUtility.InverseTransformPlane(brush_transform.worldToLocalMatrix, movePlane);
            movePlane = new CSGPlane(movePlane.normal, zero_point);

            CSGGrid.SetForcedGrid(camera, movePlane);

            return true;
        }

        private bool DragTextureCopy(SceneView sceneView)
        {
            Camera                  camera = sceneView.camera;
            LegacyBrushIntersection intersection;

            if(SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out intersection))
            {
                if(lastSelectedSurface != null && (lastSelectedSurface.brush != intersection.brush
                    || lastSelectedSurface.surfaceIndex != intersection.surfaceIndex))
                {
                    if(lastSelectedSurface.surfacePlane.HasValue)
                    {
                        SurfaceUtility.CopyLastMaterial(
                            intersection.brush, intersection.surfaceIndex, intersection.worldPlane,
                            lastSelectedSurface.brush, lastSelectedSurface.surfaceIndex
                          , lastSelectedSurface.surfacePlane.Value
                        );

                        InternalCSGModelManager.RefreshMeshes();
                    }

                    lastSelectedSurface = null;
                }

                if(lastSelectedSurface == null)
                {
                    lastSelectedSurface = new SelectedBrushSurface(
                        intersection.brush, intersection.surfaceIndex, intersection.worldPlane
                    );
                }
            }

            return true;
        }

        private bool DragSurfaceSelect(SceneView sceneView)
        {
            Camera                  camera = sceneView.camera;
            LegacyBrushIntersection intersection;

            if(SceneQueryUtility.FindWorldIntersection(camera, Event.current.mousePosition, out intersection))
            {
                if(IsSelected(intersection))
                {
                    if(dragMode == DragMode.SurfaceSelectToggle)
                    {
                        dragMode = DragMode.SurfaceSelectRemove;
                    }

                    if(dragMode == DragMode.SurfaceSelectRemove)
                    {
                        RemoveSurfaceSelection(intersection);
                        CSG_EditorGUIUtility.RepaintAll();
                    }
                }
                else
                {
                    if(dragMode == DragMode.SurfaceSelectToggle)
                    {
                        dragMode = DragMode.SurfaceSelectAdd;
                    }

                    if(dragMode == DragMode.SurfaceSelectAdd)
                    {
                        AddSurfaceSelection(intersection);
                        CSG_EditorGUIUtility.RepaintAll();
                    }
                }
            }

            return true;
        }

        private bool DragTranslateTextureCoordinates(SceneView sceneView)
        {
            if(hoverOnTarget == -1 || hoverOnSurfaceIndex == -1)
            {
                return false;
            }

            CSGBrush brush = brushTargets[hoverOnTarget];

            if(brush.ChildData == null ||
               brush.ChildData.ModelTransform == null)
            {
                return false;
            }

            Transform modelTransform      = brush.ChildData.ModelTransform;
            Matrix4x4 modelTransformation = modelTransform.localToWorldMatrix;

            if(firstMove)
            {
                UpdateSelection(false);
                CopyBackupTexgens();
                TexGenState surfaceState = surfaceStates[hoverOnTarget];
                originalSurfacePoint = surfaceState.rayIntersectionWorldPoint;
                snappingOffset2      = surfaceState.rayIntersectionWorldPoint - snappingOffset;
                firstMove            = false;
            }

            Shape       hoverOnShape       = brushTargets[hoverOnTarget].Shape;
            int         hoverOnTexGenIndex = hoverOnShape.Surfaces[hoverOnSurfaceIndex].TexGenIndex;
            TexGenState hoverSurfaceState  = surfaceStates[hoverOnTarget];
            TexGen      backupTexGen       = hoverSurfaceState.backupTexGens[hoverOnTexGenIndex];

            LegacySurfaceIntersection surfaceIntersection;
            SelectedBrushSurface[]    selectedSurfaces = GetSelectedSurfaces();

            if(selectedSurfaces.Length == 0)
            {
                return true;
            }

            Camera camera = sceneView.camera;

            if(!SceneQueryUtility.FindSurfaceIntersection(
                   camera, brush, modelTransformation, hoverOnSurfaceIndex, Event.current.mousePosition,
                   out surfaceIntersection
               ))
            {
                return true;
            }

            Vector3 old_world_position = originalSurfacePoint - snappingOffset2;
            Vector3 new_world_position = surfaceIntersection.worldIntersection - snappingOffset2;

            // snap texture coordinates in world/local space
            new_world_position = GridUtility.FixPosition(new_world_position, modelTransform, old_world_position);

            using(new UndoGroup(selectedSurfaces, "Translating surface"))
            {
                RestoreBackupTexgens();

                if(SurfaceUtility.TranslateSurfaces(
                       selectedSurfaces, old_world_position, new_world_position
                   ))
                {
                    deltaMovement = hoverOnShape.TexGens[hoverOnTexGenIndex].Translation - backupTexGen.Translation;
                    //InternalCSGModelManager.Refresh();
                    //InternalCSGModelManager.UpdateMeshes();
                    //MeshInstanceManager.UpdateHelperSurfaceVisibility();
                    //CSG_EditorGUIUtility.UpdateSceneViews();
                }
            }

            return true;
        }

        private bool DragRotateTextureCoordinates(SceneView sceneView)
        {
            if(hoverOnTarget == -1 || hoverOnSurfaceIndex == -1)
            {
                return false;
            }

            CSGBrush brush = brushTargets[hoverOnTarget];

            if(brush.ChildData == null ||
               brush.ChildData.ModelTransform == null)
            {
                return false;
            }

            Transform modelTransform      = brush.ChildData.ModelTransform;
            Matrix4x4 modelTransformation = modelTransform.localToWorldMatrix;

            if(firstMove)
            {
                UpdateSelection(false);
                CopyBackupTexgens();
                firstMove = false;
            }

            Shape       hoverOnShape       = brushTargets[hoverOnTarget].Shape;
            int         hoverOnTexGenIndex = hoverOnShape.Surfaces[hoverOnSurfaceIndex].TexGenIndex;
            TexGenState hoverSurfaceState  = surfaceStates[hoverOnTarget];
            TexGen      backupTexGen       = hoverSurfaceState.backupTexGens[hoverOnTexGenIndex];

            Camera                    camera = sceneView.camera;
            LegacySurfaceIntersection surfaceIntersection;
            SelectedBrushSurface[]    selectedSurfaces = GetSelectedSurfaces();

            if(selectedSurfaces.Length == 0 ||
               !SceneQueryUtility.FindSurfaceIntersection(
                   camera, brush, modelTransformation, hoverOnSurfaceIndex, Event.current.mousePosition,
                   out surfaceIntersection
               ) ||
               !rotationCircle.UpdateRadius(backupTexGen, surfaceIntersection.worldIntersection))
            {
                return true;
            }

            using(new UndoGroup(selectedSurfaces, "Rotating surface"))
            {
                RestoreBackupTexgens();
                SurfaceUtility.RotateSurfaces(selectedSurfaces, rotationCircle);
            }

            return true;
        }

        [NonSerialized] private LineMeshManager selectedVisibleLineMeshManager = new LineMeshManager();
        [NonSerialized] private LineMeshManager selectedInvisibleLineMeshManager = new LineMeshManager();

        [NonSerialized] private LineMeshManager hoverVisibleLineMeshManager = new LineMeshManager();
        [NonSerialized] private LineMeshManager hoverInvisibleLineMeshManager = new LineMeshManager();

        [NonSerialized] private PolygonMeshManager hoverVisiblePolygonMeshManager = new PolygonMeshManager();
        [NonSerialized] private PolygonMeshManager selectedVisiblePolygonMeshManager = new PolygonMeshManager();

        [NonSerialized] private int outlineGeneration = -1;
        [NonSerialized] private int prevHoverSurfaceIndex = -1;
        [NonSerialized] private int prevHoverBrushNodeID = CSGNode.InvalidNodeID;

        private void OnDestroy()
        {
            selectedVisibleLineMeshManager.Destroy();
            selectedInvisibleLineMeshManager.Destroy();
            selectedVisiblePolygonMeshManager.Destroy();

            hoverVisibleLineMeshManager.Destroy();
            hoverInvisibleLineMeshManager.Destroy();
            hoverVisiblePolygonMeshManager.Destroy();
        }

        private void DrawOutlines()
        {
            if(outlineGeneration != InternalCSGModelManager.MeshGeneration)
            {
                outlineGeneration  = InternalCSGModelManager.MeshGeneration;
                forceOutlineUpdate = true;
            }

            if(forceOutlineUpdate)
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

                    if(!brush.ChildData.ModelTransform)
                    {
                        continue;
                    }

                    Matrix4x4 brush_transformation = brush.compareTransformation.localToWorldMatrix;
                    Shape     brush_shape          = brush.Shape;

                    for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                    {
                        if(s < 0 || s >= brush_shape.Surfaces.Length)
                        {
                            continue;
                        }

                        int texGen_index = brush_shape.Surfaces[s].TexGenIndex;

                        if(texGen_index < 0 || texGen_index >= surfaceState.surfaceSelectState.Length)
                        {
                            continue;
                        }

                        SelectState selected_state = surfaceState.surfaceSelectState[texGen_index];

                        if((selected_state & SelectState.Selected) == SelectState.None)
                        {
                            continue;
                        }

                        foundSelectedSurfaces.Add(new SelectedBrushSurface(brush, s));
                        foundTransformations.Add(brush_transformation);
                    }
                }

                GeometryWireframe[] outlines = null;

                if(foundSelectedSurfaces.Count > 0)
                {
                    outlines = BrushOutlineManager.GetSurfaceOutlines(foundSelectedSurfaces.ToArray());

                    for(int i = outlines.Length - 1; i >= 0; i--)
                    {
                        if(outlines[i] != null &&
                           outlines[i].vertices != null &&
                           outlines[i].vertices.Length >= 0)
                        {
                            continue;
                        }

                        ArrayUtility.RemoveAt(ref outlines, i);
                    }
                }

                if(outlines != null && outlines.Length > 0)
                {
                    selectedVisibleLineMeshManager.Begin();
                    selectedInvisibleLineMeshManager.Begin();
                    selectedVisiblePolygonMeshManager.Begin();

                    CSGRenderer.DrawSurfaceOutlines(
                        selectedVisibleLineMeshManager, selectedInvisibleLineMeshManager,
                        selectedVisiblePolygonMeshManager,
                        outlines, foundTransformations.ToArray(),
                        visibleInnerColor, visibleOuterColor, visibleOuterline,
                        invisibleInnerColor, invisibleOuterColor, invisibleOuterline,
                        surfaceColor,
                        GUIConstants.THICK_LINE_SCALE
                    );

                    selectedVisiblePolygonMeshManager.End();
                    selectedInvisibleLineMeshManager.End();
                    selectedVisibleLineMeshManager.End();
                }
                else
                {
                    selectedVisiblePolygonMeshManager.Clear();
                    selectedInvisibleLineMeshManager.Clear();
                    selectedVisibleLineMeshManager.Clear();
                }
            }

            if(forceOutlineUpdate ||
               prevHoverSurfaceIndex != hoverOnSurfaceIndex ||
               prevHoverBrushNodeID != hoverOnBrushNodeID)
            {
                prevHoverSurfaceIndex = hoverOnSurfaceIndex;
                prevHoverBrushNodeID  = hoverOnBrushNodeID;

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

                //surfaceColor.a = 1.0f;

                List<SelectedBrushSurface> foundSelectedSurfaces = new List<SelectedBrushSurface>();
                List<Matrix4x4>            foundTransformations  = new List<Matrix4x4>();

                for(int t = 0; t < brushTargets.Length; t++)
                {
                    CSGBrush    brush        = brushTargets[t];
                    TexGenState surfaceState = surfaceStates[t];

                    if(!brush.ChildData.ModelTransform)
                    {
                        continue;
                    }

                    Matrix4x4 brush_translation = brush.compareTransformation.localToWorldMatrix;
                    Shape     brush_shape       = brush.Shape;

                    for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                    {
                        if(s >= brush_shape.Surfaces.Length)
                        {
                            continue;
                        }

                        int texGen_index = brush_shape.Surfaces[s].TexGenIndex;

                        if(texGen_index >= surfaceState.surfaceSelectState.Length)
                        {
                            continue;
                        }

                        SelectState selected_state = surfaceState.surfaceSelectState[texGen_index];

                        if((selected_state & SelectState.Hovering) == SelectState.None)
                        {
                            continue;
                        }

                        foundSelectedSurfaces.Add(new SelectedBrushSurface(brush, s));
                        foundTransformations.Add(brush_translation);
                    }
                }

                GeometryWireframe[] outlines = null;

                if(foundSelectedSurfaces.Count > 0)
                {
                    outlines = BrushOutlineManager.GetSurfaceOutlines(foundSelectedSurfaces.ToArray());

                    for(int i = outlines.Length - 1; i >= 0; i--)
                    {
                        if(outlines[i] != null &&
                           outlines[i].vertices != null &&
                           outlines[i].vertices.Length != 0)
                        {
                            continue;
                        }

                        ArrayUtility.RemoveAt(ref outlines, i);
                    }
                }

                if(outlines != null && outlines.Length > 0)
                {
                    hoverVisibleLineMeshManager.Begin();
                    hoverInvisibleLineMeshManager.Begin();
                    hoverVisiblePolygonMeshManager.Begin();

                    CSGRenderer.DrawSurfaceOutlines(
                        hoverVisibleLineMeshManager, hoverInvisibleLineMeshManager,
                        hoverVisiblePolygonMeshManager,
                        outlines, foundTransformations.ToArray(),
                        visibleInnerColor, visibleOuterColor, visibleOuterline,
                        invisibleInnerColor, invisibleOuterColor, invisibleOuterline,
                        surfaceColor,
                        GUIConstants.LINE_SCALE
                    );

                    hoverVisiblePolygonMeshManager.End();
                    hoverInvisibleLineMeshManager.End();
                    hoverVisibleLineMeshManager.End();
                }
                else
                {
                    hoverVisiblePolygonMeshManager.Clear();
                    hoverInvisibleLineMeshManager.Clear();
                    hoverVisibleLineMeshManager.Clear();
                }
            }

            Material zTestGenericLineMaterial   = MaterialUtility.ZTestGenericLine;
            Material noZTestGenericLineMaterial = MaterialUtility.NoZTestGenericLine;
            Material coloredPolygonMaterial     = MaterialUtility.ColoredPolygonMaterial;

            selectedVisiblePolygonMeshManager.Render(coloredPolygonMaterial);
            hoverVisiblePolygonMeshManager.Render(coloredPolygonMaterial);

            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            selectedInvisibleLineMeshManager.Render(noZTestGenericLineMaterial);
            selectedVisibleLineMeshManager.Render(zTestGenericLineMaterial);

            MaterialUtility.LineDashMultiplier      = 1.0f;
            MaterialUtility.LineThicknessMultiplier = 1.0f;
            hoverInvisibleLineMeshManager.Render(noZTestGenericLineMaterial);
            hoverVisibleLineMeshManager.Render(zTestGenericLineMaterial);

            forceOutlineUpdate = false;
        }

        private void CreateControlIDs()
        {
            //int surfaceEditBrushToolID = GUIUtility.GetControlID(surfaceEditBrushToolHash, FocusType.Keyboard);
            //HandleUtility.AddDefaultControl(surfaceEditBrushToolID);

            surfaceSelectPaintControl = GUIUtility.GetControlID(surfaceSelectPaintControlToolHash, FocusType.Keyboard);
            textureCopyDragControlID  = GUIUtility.GetControlID(textureCopyDragControlToolHash, FocusType.Keyboard);

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
                    surfaceState.surfaceControlID[s] = GUIUtility.GetControlID(
                        textureSurfaceControlToolHash + t, FocusType.Keyboard
                    );
                }
            }
        }

        [NonSerialized] private bool mouseIsDown = false;
        [NonSerialized] private bool mouseIsDragging = false;
        [NonSerialized] private bool firstMove = false;
        [NonSerialized] private bool firstDrag = false;

        [NonSerialized] private Vector2 prevMousePos;

        private bool clickedInScene = false;

        public void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera = sceneView.camera;

            EventType originalEventType = Event.current.type;

            if(originalEventType == EventType.MouseMove)
            {
                mouseIsDragging = false;
            }
            else if(originalEventType == EventType.MouseDown)
            {
                mouseIsDragging = false;
                mouseIsDown     = true;
                prevMousePos    = Event.current.mousePosition;
            }
            else if(originalEventType == EventType.MouseDrag)
            {
                mouseIsDown = true;

                if(!mouseIsDragging && (prevMousePos - Event.current.mousePosition).magnitude > 4.0f)
                {
                    mouseIsDragging = true;
                }
            }

            try
            {
                //if (Event.current.type == EventType.Layout)
                CreateControlIDs();

                switch(Event.current.type)
                {
                    case EventType.MouseDown:
                    {
                        clickedInScene = false;

                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        Rect guiArea = GetLastSceneGUIRect();

                        if(guiArea.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        clickedInScene = true;

                        break;
                    }
                    case EventType.MouseUp:
                    case EventType.MouseDrag:
                    {
                        if(!clickedInScene ||
                           Event.current.button != 0 ||
                           GUIUtility.hotControl != 0)
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

                        GameObject gameObject = null;

                        if(SceneQueryUtility.FindUnityWorldIntersection(
                               camera, Event.current.mousePosition, out gameObject
                           ))
                        {
                            if(gameObject == null &&
                               Event.current.type == EventType.MouseUp)
                            {
                                SelectionUtility.DeselectAll();
                            }
                            else
                            {
                                SelectionUtility.DoSelectionClick(sceneView);
                            }

                            break;
                        }

                        if(dragMode == DragMode.TextureCopy)
                        {
                            if(Event.current.modifiers == EventModifiers.None)
                            {
                                firstDrag                         = true;
                                currentControl                    = textureCopyDragControlID;
                                GUIUtility.hotControl             = textureCopyDragControlID;
                                GUIUtility.keyboardControl        = textureCopyDragControlID;
                                EditorGUIUtility.editingTextField = false;

                                if(Event.current.type != EventType.MouseUp)
                                {
                                    Event.current.Use();
                                }
                            }

                            //if (Event.current.type != EventType.MouseUp)
                            break;
                        }
                        else
                        {
                            SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                            switch(selectionType)
                            {
                                case SelectionType.Additive:    dragMode = DragMode.SurfaceSelectAdd; break;
                                case SelectionType.Subtractive: dragMode = DragMode.SurfaceSelectRemove; break;
                                case SelectionType.Toggle:      dragMode = DragMode.SurfaceSelectToggle; break;
                                case SelectionType.Replace:     dragMode = DragMode.None; break;
                            }

                            if(dragMode != DragMode.None)
                            {
                                currentControl                    = surfaceSelectPaintControl;
                                GUIUtility.hotControl             = surfaceSelectPaintControl;
                                GUIUtility.keyboardControl        = surfaceSelectPaintControl;
                                EditorGUIUtility.editingTextField = false;

                                if(Event.current.type != EventType.MouseUp)
                                {
                                    Event.current.Use();

                                    break;
                                }
                            }
                        }

                        if(Event.current.type != EventType.MouseUp &&
                           Event.current.modifiers != EventModifiers.None)
                        {
                            break;
                        }

                        if(hoverOnTarget == -1 || hoverOnSurfaceIndex == -1)
                        {
                            if(Event.current.type == EventType.MouseUp &&
                               !mouseIsDragging)
                            {
                                // see if we clicked into empty space
                                if(hoverOnTarget == -1 &&
                                   hoverOnSurfaceIndex == -1)
                                {
                                    SelectionUtility.DeselectAll();
                                }
                            }

                            break;
                        }

                        int newControlID = surfaceStates[hoverOnTarget].surfaceControlID[hoverOnSurfaceIndex];

                        if(newControlID <= 0)
                        {
                            break;
                        }

                        currentControl                    = newControlID;
                        GUIUtility.hotControl             = newControlID;
                        GUIUtility.keyboardControl        = newControlID;
                        EditorGUIUtility.editingTextField = false;

                        if(Event.current.type != EventType.MouseUp)
                        {
                            Event.current.Use();
                        }

                        firstMove = true;

                        Undo.IncrementCurrentGroup();
                        undoGroupIndex = Undo.GetCurrentGroup();

                        if(Tools.current == Tool.Rotate)
                        {
                            CSGBrush brush = brushTargets[hoverOnTarget];

                            rotationCircle.Initialize(
                                brush.GetComponent<Transform>(), brush.Shape.Surfaces[hoverOnSurfaceIndex]
                              , surfaceStates[hoverOnTarget].rayIntersectionWorldPoint
                            );
                        }
                        else if(Tools.current == Tool.Move)
                        {
                            CSGBrush brush = brushTargets[hoverOnTarget];
                            InternalCSGModelManager.SetBrushMeshSurfaces(brush);

                            snappingOffset = SurfaceUtility.ConvertTextureToModelSpace(
                                brush, hoverOnSurfaceIndex, MathConstants.zeroVector2
                            );
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
                            if(dragMode == DragMode.None)
                            {
                                dragMode = DragMode.TextureCopy;
                            }

                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();

                            break;
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
                            if(dragMode == DragMode.TextureCopy)
                            {
                                dragMode = DragMode.None;
                            }

                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();

                            break;
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

                            break;
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
#if UNITY_2020_2_OR_NEWER
                            if(!Tools.viewToolActive)
#else
							if (Tools.current != Tool.View && !Event.current.alt && Event.current.button != 1 && Event.current.button != 2)
#endif
                            {
                                Rect windowRect = new Rect(
                                    0, 0, sceneView.position.width
                                  , sceneView.position.height - CSG_GUIStyleUtility.BottomToolBarHeight
                                );

                                if(currentCursor != MouseCursor.Arrow)
                                {
                                    EditorGUIUtility.AddCursorRect(windowRect, currentCursor);
                                }
                            }
                        }

                        if(currentControl == -1 ||
                           currentControl == surfaceSelectPaintControl)
                        {
                            DrawOutlines();
                        }

                        break;
                    }
                    /*
                    case EventType.MouseMove:
                    {
                        if (currentControl != -1 ||
                            camera == null || !camera.pixelRect.Contains(Event.current.mousePosition))
                            return;

                        var world_ray		= HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        var ray_start		= world_ray.origin;
                        var ray_vector		= world_ray.direction * camera.farClipPlane;
                        var ray_end			= ray_start + ray_vector;

                        var wireframeShown	= RealtimeCSG.CSGSettings.IsWireframeShown(sceneView);

                        BrushIntersection intersection = null;
                        if (!mouseIsDragging &&
                            SceneQueryUtility.FindWorldIntersection(ray_start, ray_end, out intersection, ignoreInvisible: true, ignoreUnrenderables: !wireframeShown))
                        {
                            //if (hoverOnSurfaceIndex != intersection.surfaceIndex || hoverOnTarget != intersection.brushNodeID)
                            {
                                CSG_EditorGUIUtility.UpdateSceneViews();
                            }
                        }
                        break;
                    }
                    */
                    case EventType.Layout:
                    {
                        UpdateMouseCursor();

                        if(currentControl != -1 ||
                           camera == null || !camera.pixelRect.Contains(Event.current.mousePosition))
                        {
                            return;
                        }

                        bool repaint = false;

                        if(brushTargets == null)
                        {
                            brushTargets  = new CSGBrush[0];
                            surfaceStates = new TexGenState[0];
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
                            hoverOnSurfaceIndex = -1;
                            hoverOnTarget       = -1;
                            CSGGrid.ForceGrid   = false;

                            int closest_brush   = -1;
                            int closest_surface = -1;

                            Ray     world_ray  = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                            Vector3 ray_start  = world_ray.origin;
                            Vector3 ray_vector = world_ray.direction * camera.farClipPlane;
                            Vector3 ray_end    = ray_start + ray_vector;

                            Rect                    guiArea        = GetLastSceneGUIRect();
                            bool                    wireframeShown = CSGSettings.IsWireframeShown(camera);
                            LegacyBrushIntersection intersection   = null;

                            if(!mouseIsDragging &&
                               !guiArea.Contains(Event.current.mousePosition) &&
                               SceneQueryUtility.FindWorldIntersection(
                                   ray_start, ray_end,
                                   out intersection, true,
                                   !wireframeShown
                               ))
                            {
                                CSGBrush[] targets = brushTargets;
                                CSGBrush   brush   = intersection.brush;

                                if(!ArrayUtility.Contains(targets, brush))
                                {
                                    if(hoverOnlyTarget > 0 && hoverOnlyTarget < surfaceStates.Length)
                                    {
                                        TexGenState surfaceState  = surfaceStates[hoverOnlyTarget];
                                        bool        have_selected = false;

                                        for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                                        {
                                            if((surfaceState.surfaceSelectState[s] & SelectState.Selected)
                                            == SelectState.Selected)
                                            {
                                                have_selected = true;
                                            }
                                        }

                                        if(have_selected)
                                        {
                                            hoverOnlyTarget = -1;
                                        }
                                    }

                                    if(hoverOnlyTarget > 0 && hoverOnlyTarget < surfaceStates.Length)
                                    {
                                        targets[hoverOnlyTarget]       = brush;
                                        surfaceStates[hoverOnlyTarget] = new TexGenState(brush);
                                    }
                                    else
                                    {
                                        ArrayUtility.Add(ref targets, brush);
                                        TexGenState newState = new TexGenState(brush);
                                        ArrayUtility.Add(ref surfaceStates, newState);
                                        brushTargets    = targets;
                                        hoverOnlyTarget = targets.Length - 1;
                                    }
                                }
                            }

                            float min_distance = float.PositiveInfinity;

                            for(int t = 0; t < brushTargets.Length; t++)
                            {
                                CSGBrush    brush        = brushTargets[t];
                                TexGenState surfaceState = surfaceStates[t];

                                if(brush.ChildData == null ||
                                   brush.ChildData.ModelTransform == null)
                                {
                                    continue;
                                }

                                if(intersection == null || intersection.brushNodeID != brush.brushNodeID)
                                {
                                    surfaceState.rayIntersectionWorldPoint   = MathConstants.zeroVector3;
                                    surfaceState.rayIntersectionSurfacePoint = MathConstants.zeroVector3;

                                    continue;
                                }

                                float distance = (intersection.worldIntersection - ray_start).magnitude;

                                if(distance < min_distance)
                                {
                                    min_distance    = distance;
                                    closest_brush   = t;
                                    closest_surface = intersection.surfaceIndex;
                                }

                                surfaceState.rayIntersectionWorldPoint   = intersection.worldIntersection;
                                surfaceState.rayIntersectionSurfacePoint = intersection.surfaceIntersection;
                            }

                            for(int t = 0; t < brushTargets.Length; t++)
                            {
                                TexGenState surfaceState = surfaceStates[t];

                                for(int s = 0; s < surfaceState.surfaceSelectState.Length; s++)
                                {
                                    int surfaceControlID = surfaceState.surfaceControlID[s];

                                    HandleUtility.AddControl(
                                        surfaceControlID
                                      , closest_brush == t && closest_surface == s ? 0.0f : float.PositiveInfinity
                                    );
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

                        SelectState[] oldSurfaceStates = new SelectState[0];
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

                                Array.Copy(
                                    surfaceState.surfaceSelectState, oldSurfaceStates
                                  , surfaceState.surfaceSelectState.Length
                                );
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
                                            newEditMode = SetHoverOn(EditMode.TranslateSurface, t, s);
                                            surfaceState.surfaceSelectState[s] |= SelectState.Hovering;

                                            if((surfaceState.surfaceSelectState[s] & SelectState.Selected)
                                            == SelectState.Selected &&
                                               currentCursor == MouseCursor.Arrow)
                                            {
                                                currentCursor = CursorUtility.GetToolCursor();
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
                                    if(surfaceState.surfaceSelectState[p] != oldSurfaceStates[p]
                                    || oldSurfaceStates[p] == SelectState.None)
                                    {
                                        repaint = true;
                                    }
                                }
                            }
                        }

                        editMode       = newEditMode;
                        Handles.matrix = origMatrix;

                        if(hoverOnTarget >= 0 && hoverOnSurfaceIndex >= 0 && surfaceStates.Length > hoverOnTarget &&
                           surfaceStates[hoverOnTarget].surfaceSelectState.Length > hoverOnSurfaceIndex)
                        {
                            if((surfaceStates[hoverOnTarget].surfaceSelectState[hoverOnSurfaceIndex]
                                 & SelectState.Selected) != SelectState.Selected)
                            {
                                if(currentCursor == MouseCursor.Arrow)
                                {
                                    currentCursor = MouseCursor.ArrowPlus;
                                }
                            }
                        }

                        if(repaint)
                        {
                            CSG_EditorGUIUtility.RepaintAll();
                        }

                        break;
                    }
                }

                int currentHotControl = GUIUtility.hotControl;

                if(dragMode != DragMode.TextureCopy)
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

                            EventType eventType = Event.current.GetTypeForControl(controlID);

                            switch(eventType)
                            {
                                case EventType.MouseDrag:
                                {
                                    if(Event.current.button != 0 ||
                                       (Event.current.modifiers & ~EventModifiers.Control) != EventModifiers.None ||
                                       GUIUtility.hotControl != controlID ||
                                       controlID <= 0)
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
                                    if(Event.current.button != 0 ||
                                       GUIUtility.hotControl != controlID)
                                    {
                                        break;
                                    }

                                    if(!mouseIsDragging)
                                    {
                                        // see if we clicked into empty space
                                        if(hoverOnTarget == -1 &&
                                           hoverOnSurfaceIndex == -1)
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
                                        Undo.CollapseUndoOperations(undoGroupIndex);
                                        Undo.IncrementCurrentGroup();
                                        Undo.FlushUndoRecordObjects();
                                    }

                                    GUIUtility.hotControl             = 0;
                                    GUIUtility.keyboardControl        = 0;
                                    EditorGUIUtility.editingTextField = false;
                                    currentControl                    = -1;
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
                                        if(dragMode == DragMode.None)
                                        {
                                            if(mouseIsDown &&
                                               hoverOnTarget != -1 &&
                                               hoverOnSurfaceIndex != -1)
                                            {
                                                rotationCircle.Render(camera);
                                            }
                                        }
                                    }
                                    else if(Tools.current == Tool.Move)
                                    {
                                        if(editMode == EditMode.TranslateSurface &&
                                           mouseIsDragging)
                                        {
                                            Vector2 textCenter2D = Event.current.mousePosition;
                                            textCenter2D.x += 25;
                                            textCenter2D.y += 60;

                                            PaintUtility.DrawScreenText(
                                                textCenter2D,
                                                Units.ToRoundedPixelsString(deltaMovement)
                                            );
                                        }
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                EventType type = Event.current.GetTypeForControl(textureCopyDragControlID);

                switch(type)
                {
                    case EventType.MouseDrag:
                    {
                        if(Event.current.button != 0 ||
                           Event.current.modifiers != EventModifiers.None ||
                           GUIUtility.hotControl != textureCopyDragControlID ||
                           textureCopyDragControlID <= 0)
                        {
                            break;
                        }

                        if(dragMode == DragMode.TextureCopy)
                        {
                            if(firstDrag)
                            {
                                lastSelectedSurface = null;
                                firstDrag           = false;
                            }

                            DragTextureCopy(sceneView);
                        }

                        Event.current.Use();

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(Event.current.button != 0 ||
                           GUIUtility.hotControl != textureCopyDragControlID)
                        {
                            break;
                        }

                        if(dragMode == DragMode.TextureCopy)
                        {
                            if(mouseIsDragging)
                            {
                                lastSelectedSurface = null;
                            }
                            else
                            {
                                DragTextureCopy(sceneView);
                            }
                        }

                        if(!mouseIsDragging)
                        {
                            // see if we clicked into empty space
                            if(hoverOnTarget == -1 &&
                               hoverOnSurfaceIndex == -1)
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
                        currentControl                    = -1;
                        Event.current.Use();
                        firstDrag = false;

                        break;
                    }
                }

                type = Event.current.GetTypeForControl(surfaceSelectPaintControl);

                switch(type)
                {
                    case EventType.MouseDrag:
                    {
                        if(Event.current.button != 0 ||
                           GUIUtility.hotControl != surfaceSelectPaintControl ||
                           surfaceSelectPaintControl <= 0)
                        {
                            break;
                        }

                        if(dragMode == DragMode.SurfaceSelectAdd ||
                           dragMode == DragMode.SurfaceSelectRemove ||
                           dragMode == DragMode.SurfaceSelectToggle)
                        {
                            DragSurfaceSelect(sceneView);
                        }

                        Event.current.Use();

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(Event.current.button != 0 ||
                           GUIUtility.hotControl != surfaceSelectPaintControl)
                        {
                            break;
                        }

                        if(!mouseIsDragging)
                        {
                            // see if we clicked into empty space
                            if(hoverOnTarget == -1 &&
                               hoverOnSurfaceIndex == -1)
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
                        currentControl                    = -1;
                        Event.current.Use();

                        break;
                    }
                }
            }
            finally
            {
                if(originalEventType == EventType.MouseUp ||
                   originalEventType == EventType.MouseMove)
                {
                    mouseIsDragging = false;
                    mouseIsDown     = false;
                }
            }
        }

        private static Vector2 scrollPos;

        public void OnInspectorGUI(EditorWindow window, float height)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            {
                EditModeSurfaceGUI.OnInspectorGUI(window, height);
            }

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

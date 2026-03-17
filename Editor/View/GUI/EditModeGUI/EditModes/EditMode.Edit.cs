using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using RealtimeCSG.Helpers;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal sealed class EditModeMeshEdit : ScriptableObject, IEditMode
    {
        private static readonly int RectSelectionHash = "vertexRectSelection".GetHashCode();
        private static readonly int meshEdgeChamferHash = "meshEdgeChamfer".GetHashCode();
        private static readonly int MeshEditBrushToolHash = "meshEditBrushTool".GetHashCode();
        private static readonly int MeshEditBrushPointHash = "meshEditBrushPoint".GetHashCode();
        private static readonly int MeshEditBrushEdgeHash = "meshEditBrushEdge".GetHashCode();
        private static readonly int MeshEditBrushPolygonHash = "meshEditBrushPolygon".GetHashCode();

        private const float HoverTextDistance = 25.0f;

        public bool UsesUnitySelection
        {
            get => true;
        }

        public bool IgnoreUnityRect
        {
            get => true;
        }

        private bool HavePointSelection
        {
            get
            {
                for(int t = 0; t < _brushSelection.brushes.Length; t++)
                {
                    if(_brushSelection.states[t].HaveSelection)
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
                for(int t = 0; t < _brushSelection.brushes.Length; t++)
                {
                    if(_brushSelection.states[t].HaveEdgeSelection)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

#region Tool edit modes

        private enum EditMode
        {
            None
          , MovingPoint
          , MovingEdge
          , MovingPolygon
          , ScalePolygon
          , RotateEdge
        }

        [NonSerialized] private EditMode _editMode = EditMode.None;

        [NonSerialized] private bool _doMarquee; //= false;

#endregion

        [NonSerialized] private Transform _rotateBrushParent; //= null;
        [NonSerialized] private Vector3 _rotateStart = MathConstants.zeroVector3;
        [NonSerialized] private Vector3 _rotateCenter = MathConstants.zeroVector3;
        [NonSerialized] private Vector3 _rotateTangent = MathConstants.zeroVector3;
        [NonSerialized] private Vector3 _rotateNormal = MathConstants.zeroVector3;
        [NonSerialized] private CSGPlane _rotatePlane;
        [NonSerialized] private float _rotateRadius;              //= 0;
        [NonSerialized] private float _rotateStartAngle;          //= 0;
        [NonSerialized] private float _rotateCurrentAngle;        //= 0;
        [NonSerialized] private float _rotateCurrentSnappedAngle; //= 0;
        [NonSerialized] private int _rotationUndoGroupIndex = -1;

        [NonSerialized] private bool _movePlaneInNormalDirection; //= false;
        [NonSerialized] private Vector3 _movePolygonOrigin;
        [NonSerialized] private Vector3 _movePolygonDirection;
        [NonSerialized] private Vector3 _worldDeltaMovement;
        [NonSerialized] private Vector3 _extraDeltaMovement = MathConstants.zeroVector3;

        [SerializeField] private bool useHandleCenter; //= false;
        [SerializeField] private Vector3 handleCenter;
        [SerializeField] private Vector3 startHandleCenter;
        [SerializeField] private Vector3 startHandleDirection;
        [SerializeField] private Vector3 handleScale = Vector3.one;
        [SerializeField] private Vector3 dragEdgeScale = Vector3.one;
        [SerializeField] private Quaternion dragEdgeRotation;
        [SerializeField] private Vector3[] handleWorldPoints; //= null;

        [NonSerialized] private int _hoverOnEdgeIndex = -1;
        [NonSerialized] private int _hoverOnPointIndex = -1;
        [NonSerialized] private int _hoverOnPolygonIndex = -1;
        [NonSerialized] private int _hoverOnTarget = -1;

        [NonSerialized] private int _rectSelectionId = -1;
        [NonSerialized] private int _chamferId = -1;

        [NonSerialized] private bool _nonZeroMouseIsDown; //= false;
        [NonSerialized] private bool _mouseIsDragging;    //= false;
        [NonSerialized] private bool _mouseIsDown;        //= false;
        [NonSerialized] private bool _showMarquee;        //= false;
        [NonSerialized] private bool _firstMove;          //= false;

        [NonSerialized] private Camera _startCamera;
        [NonSerialized] private Vector2 _startMousePoint;
        [NonSerialized] private Vector3 _originalPoint;
        [NonSerialized] private Vector2 _mousePosition;
        [NonSerialized] private CSGPlane _movePlane;
        [NonSerialized] private bool _usingControl; //= false

        private readonly TransformSelection _transformSelection = new TransformSelection();
        private readonly BrushSelection _brushSelection = new BrushSelection();

        [SerializeField] private Object[] undoAbleTransforms = Array.Empty<Object>();
        [SerializeField] private Object[] undoAbleBrushes = Array.Empty<Object>();

        [SerializeField] private SpaceMatrices activeSpaceMatrices = new SpaceMatrices();

        [NonSerialized] private bool _isEnabled; //= false;
        [NonSerialized] private bool _hideTool;  //= false;

        public void SetTargets(FilteredSelection filteredSelection)
        {
            if(filteredSelection == null)
            {
                return;
            }

            HashSet<CSGBrush> foundBrushes = filteredSelection.GetAllContainedBrushes();
            _brushSelection.Select(foundBrushes);

            HashSet<Transform> foundTransforms = new HashSet<Transform>();

            if(filteredSelection.nodeTargets != null)
            {
                for(int i = 0; i < filteredSelection.nodeTargets.Length; i++)
                {
                    if(filteredSelection.nodeTargets[i])
                    {
                        foundTransforms.Add(filteredSelection.nodeTargets[i].transform);
                    }
                }
            }

            if(filteredSelection.otherTargets != null)
            {
                for(int i = 0; i < filteredSelection.otherTargets.Length; i++)
                {
                    if(filteredSelection.otherTargets[i])
                    {
                        foundTransforms.Add(filteredSelection.otherTargets[i]);
                    }
                }
            }

            _transformSelection.Select(foundTransforms.ToArray());
            List<Object> transformsAsObjects = _transformSelection.transforms.ToList<Object>();
            transformsAsObjects.Add(this);
            undoAbleTransforms = transformsAsObjects.ToArray();

            List<Object> brushesAsObjects = _brushSelection.brushes.ToList<Object>();
            brushesAsObjects.Add(this);
            undoAbleBrushes = brushesAsObjects.ToArray();

            _hideTool = filteredSelection.nodeTargets != null && filteredSelection.nodeTargets.Length > 0;

            if(!_isEnabled)
            {
                return;
            }

            ForceLineUpdate();
            _brushSelection.UpdateTargets();
            CenterPositionHandle();
            Tools.hidden = _hideTool;
        }

        public void OnEnableTool()
        {
            _isEnabled    = true;
            _usingControl = false;
            Tools.hidden  = _hideTool;

            ForceLineUpdate();
            _brushSelection.ResetSelection();
            _brushSelection.UpdateTargets();
            CenterPositionHandle();
            ResetTool();
        }

        public void OnDisableTool()
        {
            _isEnabled    = false;
            Tools.hidden  = false;
            _usingControl = false;
            ResetTool();
        }

        private void ResetTool()
        {
            _usingControl = false;

            CSGGrid.ForceGrid = false;
        }

        public bool UndoRedoPerformed()
        {
            _brushSelection.UpdateTargets();
            UpdateWorkControlMesh();
            _brushSelection.UndoRedoPerformed();

            CenterPositionHandle();
            ForceLineUpdate();
            CSGEditorGUIUtility.RepaintAll();

            return false;
        }

#region Selection & Hover

        private void SelectMarquee(Camera camera, Rect rect, SelectionType selectionType)
        {
            if(rect.width <= 0 || rect.height <= 0)
            {
                return;
            }

            try
            {
                Frustum frustum            = CameraUtility.GetCameraSubFrustumGUI(camera, rect);
                bool    ignoreHiddenPoints = CSGSettings.HiddenSurfacesNotSelectable;

                PointSelection[] selectedPoints = SceneQueryUtility.GetPointsInFrustum(
                    camera, frustum.planes, _brushSelection.brushes, _brushSelection.states, ignoreHiddenPoints
                );

                Undo.RecordObject(this, "Select points");
                _brushSelection.RevertSelection();
                ControlMeshState.SelectFrustumPoints(_brushSelection.states, selectedPoints, selectionType, false);
                ForceLineUpdate();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        public bool DeselectAll()
        {
            try
            {
                if(_brushSelection.states == null ||
                   _brushSelection.states.Length == 0)
                {
                    return false;
                }

                Undo.RecordObject(this, "Deselect All");

                if(GUIUtility.hotControl == _rectSelectionId)
                {
                    GUIUtility.hotControl             = 0;
                    GUIUtility.keyboardControl        = 0;
                    EditorGUIUtility.editingTextField = false;
                    _brushSelection.RevertSelection();
                    _brushSelection.DestroySelectionBackup();
                    ForceLineUpdate();
                    CSGEditorGUIUtility.RepaintAll();

                    return true;
                }

                if(!ControlMeshState.DeselectAll(_brushSelection.states))
                {
                    Selection.activeTransform = null;

                    return false;
                }

                ForceLineUpdate();
                CSGEditorGUIUtility.RepaintAll();

                return true;
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        public void SelectPolygon(CSGBrush brush, int polygonIndex)
        {
            UpdateTransformMatrices();
            int foundTarget = -1;

            for(int i = 0; i < _brushSelection.states.Length; i++)
            {
                if(_brushSelection.brushes[i] == brush)
                {
                    foundTarget = i;
                }
            }

            ControlMeshState hoverMeshState = _brushSelection.states[foundTarget];
            ControlMeshState.DeselectAll(_brushSelection.states);
            hoverMeshState.selection.polygons[polygonIndex] |= SelectState.Selected;

            UpdateWorkControlMesh();
            UpdateBackupPoints();
            UpdateMeshStates();
            CenterPositionHandle();
            CSGEditorGUIUtility.RepaintAll();
        }

        private bool UpdateSelection(bool allowSubstraction = true)
        {
            try
            {
                if(_hoverOnTarget <= -1 || _hoverOnTarget >= _brushSelection.states.Length)
                {
                    return false;
                }

                ControlMeshState hoverMeshState = _brushSelection.states[_hoverOnTarget];
                SelectionType    selectionType  = SelectionUtility.GetEventSelectionType();

                if(!allowSubstraction)
                {
                    selectionType = SelectionType.Replace;

                    switch(_editMode)
                    {
                        case EditMode.MovingPoint:
                        {
                            if(hoverMeshState.IsPointSelectedIndirectly(_hoverOnPointIndex))
                            {
                                selectionType = SelectionType.Additive;
                            }

                            break;
                        }
                        case EditMode.RotateEdge:
                        case EditMode.MovingEdge:
                        {
                            if(hoverMeshState.IsEdgeSelectedIndirectly(_hoverOnEdgeIndex))
                            {
                                selectionType = SelectionType.Additive;
                            }

                            break;
                        }
                        case EditMode.MovingPolygon:
                        {
                            if(hoverMeshState.IsPolygonSelectedIndirectly(_hoverOnPolygonIndex))
                            {
                                selectionType = SelectionType.Additive;
                            }

                            break;
                        }
                    }
                }

                bool needRepaint = false;

                Undo.RecordObject(this, "Update selection");

                if(selectionType == SelectionType.Replace)
                {
                    needRepaint = ControlMeshState.DeselectAll(_brushSelection.states);
                }

                for(int p = 0; p < hoverMeshState.selection.polygons.Length; p++)
                {
                    needRepaint = hoverMeshState.SelectPolygon(p, selectionType) || needRepaint;
                }

                for(int e = 0; e < hoverMeshState.selection.edges.Length; e++)
                {
                    needRepaint = hoverMeshState.SelectEdge(e, selectionType) || needRepaint;
                }

                for(int b = 0; b < _brushSelection.states.Length; b++)
                {
                    ControlMeshState curMeshState = _brushSelection.states[b];

                    for(int p = 0; p < curMeshState.worldPoints.Length; p++)
                    {
                        needRepaint = curMeshState.SelectPoint(p, selectionType) || needRepaint;
                    }
                }

                ForceLineUpdate();

                return needRepaint;
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private MouseCursor _currentCursor = MouseCursor.Arrow;

        private void UpdateMouseCursor()
        {
            if(GUIUtility.hotControl == _rectSelectionId && !_movePlaneInNormalDirection && GUIUtility.hotControl != 0)
            {
                return;
            }

            _currentCursor = CursorUtility.GetSelectionCursor(SelectionUtility.GetEventSelectionType());
        }

        private EditMode SetHoverOn(EditMode editModeType, int target, int index = -1)
        {
            _hoverOnTarget = target;

            if(target <= -1 || _hoverOnTarget >= _brushSelection.states.Length)
            {
                _hoverOnEdgeIndex    = -1;
                _hoverOnPolygonIndex = -1;
                _hoverOnPointIndex   = -1;

                return EditMode.None;
            }

            _hoverOnEdgeIndex    = -1;
            _hoverOnPolygonIndex = -1;
            _hoverOnPointIndex   = -1;

            if(index == -1)
            {
                return EditMode.None;
            }

            switch(editModeType)
            {
                case EditMode.RotateEdge:
                case EditMode.MovingEdge: _hoverOnEdgeIndex = index; break;
                case EditMode.MovingPoint:   _hoverOnPointIndex   = index; break;
                case EditMode.MovingPolygon: _hoverOnPolygonIndex = index; break;
            }

            return editModeType;
        }

        private EditMode HoverOnPoint(ControlMeshState meshState, int brushNodeIndex, int pointIndex)
        {
            EditMode editMode = SetHoverOn(EditMode.MovingPoint, brushNodeIndex, pointIndex);
            meshState.selection.points[pointIndex] |= SelectState.Hovering;
            _currentCursor                         =  MouseCursor.MoveArrow;

            return editMode;
        }

        private EditMode HoverOnPolygon(ControlMeshState meshState, int brushNodeIndex, int polygonIndex)
        {
            EditMode editMode = SetHoverOn(EditMode.MovingPolygon, brushNodeIndex, polygonIndex);
            meshState.selection.polygons[polygonIndex] |= SelectState.Hovering;

            if(Tools.current == Tool.Scale ||
               SelectionUtility.CurrentModifiers == EventModifiers.Control)
            {
                return editMode;
            }

            Vector2 point1 = HandleUtility.WorldToGUIPoint(meshState.polygonCenterPoints[polygonIndex]);

            Vector2 point2 = HandleUtility.WorldToGUIPoint(
                meshState.polygonCenterPoints[polygonIndex] + meshState.polygonCenterPlanes[polygonIndex].normal * 10.0f
            );

            Vector2 delta = (point2 - point1).normalized;

            _currentCursor = CursorUtility.GetCursorForDirection(delta);

            return editMode;
        }

        private EditMode HoverOnEdge(ControlMeshState meshState, int brushIndex, int edgeIndex)
        {
            CSGBrush  brush    = _brushSelection.brushes[brushIndex];
            Surface[] surfaces = brush.Shape.Surfaces;

            int vertexIndex1 = meshState.edges[edgeIndex * 2 + 0];
            int vertexIndex2 = meshState.edges[edgeIndex * 2 + 1];

            int surfaceIndex1 = meshState.edgeSurfaces[edgeIndex * 2 + 0];
            int surfaceIndex2 = meshState.edgeSurfaces[edgeIndex * 2 + 1];

            if(surfaceIndex1 < 0 || surfaceIndex1 >= surfaces.Length ||
               surfaceIndex2 < 0 || surfaceIndex2 >= surfaces.Length)
            {
                return EditMode.None;
            }

            EditMode editMode = EditMode.None;

            if(Tools.current != Tool.Rotate)
            {
                editMode = SetHoverOn(EditMode.MovingEdge, brushIndex, edgeIndex);

                Vector2 point1 = HandleUtility.WorldToGUIPoint(meshState.worldPoints[vertexIndex1]);
                Vector2 point2 = HandleUtility.WorldToGUIPoint(meshState.worldPoints[vertexIndex2]);
                Vector2 delta  = (point2 - point1).normalized;

                _currentCursor = CursorUtility.GetCursorForEdge(delta);
            }
            else if(Tools.current == Tool.Rotate)
            {
                editMode       = SetHoverOn(EditMode.RotateEdge, brushIndex, edgeIndex);
                _currentCursor = MouseCursor.RotateArrow;
            }

            meshState.selection.edges[edgeIndex] |= SelectState.Hovering;

            return editMode;
        }

#endregion

#region Actions

        public void SnapToGrid(Camera camera)
        {
            try
            {
                if(HavePointSelection)
                {
                    Undo.RecordObjects(undoAbleBrushes, "Snap points to grid");
                    _brushSelection.PointSnapToGrid(camera);
                    UpdateWorkControlMesh(true);
                }
                else
                {
                    Undo.IncrementCurrentGroup();
                    int groupIndex = Undo.GetCurrentGroup();
                    Undo.RecordObjects(undoAbleTransforms, "Snap brushes to grid");
                    Undo.RecordObjects(undoAbleBrushes, "Snap points to grid");
                    BrushOperations.SnapToGrid(camera, _brushSelection.brushes);
                    _brushSelection.CleanPoints();
                    Undo.CollapseUndoOperations(groupIndex);
                }
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void ShapeCancelled()
        {
            EditModeManager.EditMode        =  ToolEditMode.Edit;
            EditModeGenerate.ShapeCancelled -= ShapeCancelled;
            EditModeGenerate.ShapeCommitted -= ShapeCommitted;
        }

        private void ShapeCommitted()
        {
            EditModeManager.EditMode        =  ToolEditMode.Edit;
            EditModeGenerate.ShapeCancelled -= ShapeCancelled;
            EditModeGenerate.ShapeCommitted -= ShapeCommitted;
        }

        private void ExtrudeSurface(Camera camera, bool drag)
        {
            EditModeGenerate.ShapeCancelled += ShapeCancelled;
            EditModeGenerate.ShapeCommitted += ShapeCommitted;

            ControlMeshState targetMeshState = _brushSelection.states[_hoverOnTarget];

            Vector3 localNormal = targetMeshState.polygonCenterPlanes[_hoverOnPolygonIndex].normal;

            Vector3 worldNormal = targetMeshState.brushTransform.localToWorldMatrix.MultiplyVector(localNormal)
               .normalized;

            if(Tools.pivotRotation == PivotRotation.Global)
            {
                worldNormal = GeometryUtility.SnapToClosestAxis(worldNormal);
            }

            CSGBrush brush = _brushSelection.brushes[_hoverOnTarget];

            if(brush == null)
            {
                return;
            }

            ControlMesh controlMesh = brush.ControlMesh;
            Shape       shape       = brush.Shape;

            if(controlMesh == null || shape == null)
            {
                return;
            }

            Vector3[] points       = targetMeshState.worldPoints;
            int[]     pointIndices = targetMeshState.polygonPointIndices[_hoverOnPolygonIndex];

            CSGPlane polygonPlane = GeometryUtility.CalcPolygonPlane(points, pointIndices);

            Polygon polygon = controlMesh.Polygons[_hoverOnPolygonIndex];

            if(polygon == null)
            {
                return;
            }

            int[] edgeIndices = polygon.EdgeIndices;

            if(edgeIndices == null ||
               edgeIndices.Length == 0)
            {
                return;
            }

            uint[] smoothingGroups = new uint[edgeIndices.Length];

            for(int e = 0; e < edgeIndices.Length; e++)
            {
                int   edgeIndex          = edgeIndices[e];
                int   twinIndex          = controlMesh.Edges[edgeIndex].TwinIndex;
                short twinPolygonIndex   = controlMesh.Edges[twinIndex].PolygonIndex;
                int   twinTexGenIndex    = controlMesh.Polygons[twinPolygonIndex].TexGenIndex;
                uint  twinSmoothingGroup = shape.TexGens[twinTexGenIndex].SmoothingGroup;
                smoothingGroups[e] = twinSmoothingGroup;
            }

            CSGOperationType forceDragSource = brush.OperationType;

            EditModeManager.GenerateFromSurface(
                camera, brush, polygonPlane, worldNormal, points, pointIndices, smoothingGroups, drag, forceDragSource
              , CSGSettings.AutoCommitExtrusion
            );
        }

        private void MergeDuplicatePoints()
        {
            if(_editMode == EditMode.RotateEdge)
            {
                return;
            }

            try
            {
                Undo.RegisterCompleteObjectUndo(undoAbleBrushes, "Merging vertices");
                ControlMeshUtility.MergeDuplicatePoints(_brushSelection.brushes, _brushSelection.states);
                UpdateWorkControlMesh();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void MergeHoverEdgePoints()
        {
            try
            {
                Undo.RegisterCompleteObjectUndo(undoAbleBrushes, "Merge edge-points");

                ControlMeshUtility.MergeHoverEdgePoints(
                    _brushSelection.brushes[_hoverOnTarget], _brushSelection.states[_hoverOnTarget], _hoverOnEdgeIndex
                );

                UpdateWorkControlMesh();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void MergeHoverPolygonPoints()
        {
            try
            {
                Undo.RegisterCompleteObjectUndo(undoAbleBrushes, "Merge edge-points");

                ControlMeshUtility.MergeHoverPolygonPoints(
                    _brushSelection.brushes[_hoverOnTarget], _hoverOnPolygonIndex
                );

                UpdateWorkControlMesh();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void MergeSelected()
        {
            if(!HaveEdgeSelection)
            {
                if(_editMode == EditMode.MovingEdge &&
                   _hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.states.Length &&
                   _hoverOnEdgeIndex != -1)
                {
                    MergeHoverEdgePoints();
                }
                else if(_editMode == EditMode.MovingPolygon &&
                        _hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.states.Length &&
                        _hoverOnPolygonIndex != -1)
                {
                    MergeHoverPolygonPoints();
                }
            }

            MergeSelectedEdgePoints();
        }

        private void DoRotateBrushes(Vector3 rotationCenter, Vector3 rotationAxis, float rotationAngle)
        {
            try
            {
                Undo.RecordObjects(undoAbleTransforms, "Transform brushes");
                _transformSelection.Rotate(rotationCenter, rotationAxis, rotationAngle);
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void DoMoveControlPoints(Vector3 worldOffset)
        {
            try
            {
                Undo.RecordObjects(undoAbleBrushes, "Move control-points");
                _brushSelection.TranslateControlPoints(worldOffset);
                ForceLineUpdate();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void DoRotateControlPoints(Vector3 center, Quaternion handleRotation, Quaternion rotationOffset)
        {
            try
            {
                Undo.RecordObjects(undoAbleBrushes, "Rotate control-points");
                _brushSelection.RotateControlPoints(center, handleRotation, rotationOffset);
                ForceLineUpdate();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void DoScaleControlPoints(Quaternion rotation, Vector3 scale, Vector3 center)
        {
            try
            {
                Undo.RegisterCompleteObjectUndo(undoAbleBrushes, "Scale control-points");
                _brushSelection.ScaleControlPoints(center, rotation, scale);
                ForceLineUpdate();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void DeleteSelectedPoints()
        {
            try
            {
                Undo.RegisterCompleteObjectUndo(undoAbleBrushes, "Delete control-points");
                ControlMeshUtility.DeleteSelectedPoints(_brushSelection.brushes, _brushSelection.states);
                UpdateWorkControlMesh();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        public void FlipX()
        {
            try
            {
                BrushOperations.FlipX(_brushSelection.brushes);
                UpdateWorkControlMesh();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        public void FlipY()
        {
            try
            {
                BrushOperations.FlipY(_brushSelection.brushes);
                UpdateWorkControlMesh();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        public void FlipZ()
        {
            try
            {
                BrushOperations.FlipZ(_brushSelection.brushes);
                UpdateWorkControlMesh();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

        private void MergeSelectedEdgePoints()
        {
            try
            {
                Undo.RegisterCompleteObjectUndo(undoAbleBrushes, "Merge edge-points");
                ControlMeshUtility.MergeSelectedEdgePoints(_brushSelection.brushes, _brushSelection.states);
                UpdateWorkControlMesh();
            }
            finally
            {
                CenterPositionHandle();
            }
        }

#endregion

        private bool UpdateWorkControlMesh(bool forceUpdate = false)
        {
            _brushSelection.UpdateWorkControlMesh(forceUpdate);
            _transformSelection.Update();

            return true;
        }

        private void UpdateBackupPoints()
        {
            for(int t = 0; t < _brushSelection.brushes.Length; t++)
            {
                ControlMesh workControlMesh = _brushSelection.controlMeshes[t];
                _brushSelection.states[t].backupPoints = new Vector3[workControlMesh.Vertices.Length];

                if(workControlMesh.Vertices.Length > 0)
                {
                    Array.Copy(
                        workControlMesh.Vertices,
                        _brushSelection.states[t].backupPoints,
                        workControlMesh.Vertices.Length
                    );
                }

                _brushSelection.states[t].backupPolygonCenterPoints =
                    new Vector3[_brushSelection.states[t].polygonCenterPoints.Length];

                if(_brushSelection.states[t].polygonCenterPoints.Length > 0)
                {
                    Array.Copy(
                        _brushSelection.states[t].polygonCenterPoints,
                        _brushSelection.states[t].backupPolygonCenterPoints,
                        _brushSelection.states[t].polygonCenterPoints.Length
                    );
                }

                _brushSelection.states[t].backupPolygonCenterPlanes =
                    new CSGPlane[_brushSelection.states[t].polygonCenterPlanes.Length];

                if(_brushSelection.states[t].polygonCenterPlanes.Length > 0)
                {
                    Array.Copy(
                        _brushSelection.states[t].polygonCenterPlanes,
                        _brushSelection.states[t].backupPolygonCenterPlanes,
                        _brushSelection.states[t].polygonCenterPlanes.Length
                    );
                }
            }
        }

        private void UpdateTransformMatrices()
        {
            activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);
        }

        private bool UpdateRotationCircle(SceneView sceneView)
        {
            switch(_editMode)
            {
                case EditMode.RotateEdge:
                {
                    _rotateBrushParent = _brushSelection.modelTransforms[_hoverOnTarget];

                    if(_rotateBrushParent == null)
                    {
                        return false;
                    }

                    ControlMeshState meshState = _brushSelection.states[_hoverOnTarget];

                    _rotateCenter = MathConstants.zeroVector3;

                    for(int p = 0; p < meshState.worldPoints.Length; p++)
                    {
                        _rotateCenter += meshState.worldPoints[p];
                    }

                    _rotateCenter /= meshState.worldPoints.Length;

                    int pointIndex1 = meshState.edges[_hoverOnEdgeIndex * 2 + 0];
                    int pointIndex2 = meshState.edges[_hoverOnEdgeIndex * 2 + 1];

                    Vector3 vertex1 = meshState.worldPoints[pointIndex1];
                    Vector3 vertex2 = meshState.worldPoints[pointIndex2];

                    Camera camera = sceneView.camera;

                    _rotateNormal = camera.orthographic
                        ? camera.transform.forward.normalized
                        : (vertex2 - vertex1).normalized;

                    if(Tools.pivotRotation == PivotRotation.Global)
                    {
                        _rotateNormal = GeometryUtility.SnapToClosestAxis(_rotateNormal);
                    }

                    _rotatePlane = new CSGPlane(_rotateNormal, _rotateCenter);
                    _rotateStart = (vertex2 + vertex1) * 0.5f;
                    _rotateStart = GeometryUtility.ProjectPointOnPlane(_rotatePlane, _rotateStart);
                    Vector3 delta = _rotateCenter - _rotateStart;
                    _rotateTangent = -delta.normalized;

                    Ray     ray         = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                    Vector3 newMousePos = _rotatePlane.RayIntersection(ray);

                    _rotateStartAngle = GeometryUtility.SignedAngle(
                        _rotateCenter - _rotateStart, _rotateCenter - newMousePos, _rotateNormal
                    );

                    float handleSize = HandleUtility.GetHandleSize(_rotateCenter);
                    _rotateRadius = Math.Max(delta.magnitude, handleSize);

                    return true;
                }
            }

            return false;
        }

        private void UpdateGrid(Camera camera)
        {
            if(_hoverOnTarget <= -1 || _hoverOnTarget >= _brushSelection.states.Length ||
               !camera)
            {
                return;
            }

            if(_hoverOnPolygonIndex != -1 &&
               _editMode == EditMode.MovingPolygon &&
               (SelectionUtility.CurrentModifiers & EventModifiers.Control) != EventModifiers.Control)
            {
                ControlMeshState targetMeshState   = _brushSelection.states[_hoverOnTarget];
                Matrix4x4        brushLocalToWorld = targetMeshState.brushTransform.localToWorldMatrix;
                Vector3          worldOrigin       = targetMeshState.polygonCenterPoints[_hoverOnPolygonIndex];

                Vector3 worldDirection = brushLocalToWorld.MultiplyVector(
                        targetMeshState.polygonCenterPlanes[_hoverOnPolygonIndex].normal
                    )
                   .normalized;

                if(Tools.pivotRotation == PivotRotation.Global)
                {
                    worldDirection = GeometryUtility.SnapToClosestAxis(worldDirection);
                }

                CSGGrid.SetupRayWorkPlane(camera, worldOrigin, worldDirection, ref _movePlane);

                _movePlaneInNormalDirection = true;
                _movePolygonOrigin          = worldOrigin;
                _movePolygonDirection       = worldDirection;
            }
            else if(_hoverOnEdgeIndex != -1 &&
                    _editMode == EditMode.MovingEdge &&
                    (SelectionUtility.CurrentModifiers & EventModifiers.Control) != EventModifiers.Control)
            {
                ControlMeshState targetMeshState   = _brushSelection.states[_hoverOnTarget];
                Matrix4x4        brushLocalToWorld = targetMeshState.brushTransform.localToWorldMatrix;
                int              pointIndex1       = targetMeshState.edges[_hoverOnEdgeIndex * 2 + 0];
                int              pointIndex2       = targetMeshState.edges[_hoverOnEdgeIndex * 2 + 1];
                Vector3          vertex1           = targetMeshState.worldPoints[pointIndex1];
                Vector3          vertex2           = targetMeshState.worldPoints[pointIndex2];

                Vector3 worldOrigin    = _originalPoint;
                Vector3 worldDirection = brushLocalToWorld.MultiplyVector(vertex2 - vertex1).normalized;

                if(Tools.current == Tool.Scale)
                {
                    worldDirection = camera.transform.forward;
                }

                if(Tools.pivotRotation == PivotRotation.Global)
                {
                    worldDirection = GeometryUtility.SnapToClosestAxis(worldDirection);
                }

                CSGGrid.SetupWorkPlane(camera, worldOrigin, worldDirection, ref _movePlane);

                _movePlaneInNormalDirection = true;
                _movePolygonOrigin          = worldOrigin;
                _movePolygonDirection       = worldDirection;
            }
            else
            {
                CSGGrid.SetupWorkPlane(camera, _originalPoint, ref _movePlane);

                _movePlaneInNormalDirection = false;
            }
        }

        private Vector3 SnapMovementToPlane(Vector3 offset)
        {
            if(Math.Abs(_movePlane.a) > 1 - MathConstants.NormalEpsilon)
            {
                offset.x = 0.0f;
            }

            if(Math.Abs(_movePlane.b) > 1 - MathConstants.NormalEpsilon)
            {
                offset.y = 0.0f;
            }

            if(Math.Abs(_movePlane.c) > 1 - MathConstants.NormalEpsilon)
            {
                offset.z = 0.0f;
            }

            if(float.IsNaN(offset.x) || float.IsNaN(offset.y) || float.IsNaN(offset.z))
            {
                offset = MathConstants.zeroVector3;
            }

            return offset;
        }

        internal class BrushOutlineInfo
        {
            public readonly BrushOutlineRenderer brushOutlineRenderer = new BrushOutlineRenderer();
            public int lastLineMeshGeneration = -1;
            public int lastHandleGeneration = -1;

            internal void Destroy()
            {
                brushOutlineRenderer.Destroy();
            }
        }

        private readonly PointMeshManager _pointMeshManager = new PointMeshManager();

        private readonly Dictionary<Camera, BrushOutlineInfo> _brushOutlineInfos =
            new Dictionary<Camera, BrushOutlineInfo>();

        internal BrushOutlineInfo GetBrushOutLineInfo(Camera camera)
        {
            if(_brushOutlineInfos.TryGetValue(camera, out BrushOutlineInfo brushOutlineInfo))
            {
                return brushOutlineInfo;
            }

            brushOutlineInfo           = new BrushOutlineInfo();
            _brushOutlineInfos[camera] = brushOutlineInfo;

            return brushOutlineInfo;
        }

        internal void OnDestroy()
        {
            foreach(BrushOutlineInfo brushOutlineInfo in _brushOutlineInfos.Values)
            {
                brushOutlineInfo.Destroy();
            }

            _brushOutlineInfos.Clear();
            _pointMeshManager.Destroy();
        }

        private void ForceLineUpdate()
        {
            int          currentMeshGeneration = InternalCSGModelManager.MeshGeneration;
            List<Camera> removeKeys            = new List<Camera>();

            foreach(KeyValuePair<Camera, BrushOutlineInfo> brushOutlineInfo in _brushOutlineInfos)
            {
                if(!brushOutlineInfo.Key)
                {
                    brushOutlineInfo.Value.Destroy();
                    removeKeys.Add(brushOutlineInfo.Key);

                    continue;
                }

                brushOutlineInfo.Value.lastLineMeshGeneration = currentMeshGeneration - 1000;
            }

            foreach(Camera key in removeKeys)
            {
                _brushOutlineInfos.Remove(key);
            }
        }

        private void UpdateMeshStates()
        {
            for(int t = 0; t < _brushSelection.brushes.Length; t++)
            {
                if(_brushSelection.states[t] == null)
                {
                    _brushSelection.UpdateTargets();

                    break;
                }

                _brushSelection.states[t]
                   .UpdateMesh(
                        _brushSelection.backupControlMeshes[t],
                        _brushSelection.controlMeshes[t].Vertices
                    );
            }
        }

        private void UpdatePointSizes(SceneView sceneView)
        {
            Camera camera = sceneView.camera;

            if(!camera)
            {
                return;
            }

            for(int t = 0; t < _brushSelection.brushes.Length; t++)
            {
                _brushSelection.states[t].UpdateHandles(camera, _brushSelection.backupControlMeshes[t]);
            }
        }

        [NonSerialized] private ViewTool _preViewTool = ViewTool.None;
        [NonSerialized] private Tool _prevTool = Tool.None;

        private void UpdateLineMeshes(SceneView sceneView)
        {
            Camera camera = sceneView.camera;

            if(!camera)
            {
                return;
            }

            BrushOutlineInfo brushOutlineInfo = GetBrushOutLineInfo(camera);

            if(brushOutlineInfo == null)
            {
                return;
            }

            BrushOutlineRenderer brushOutlineRenderer = brushOutlineInfo.brushOutlineRenderer;

            int currentMeshGeneration = InternalCSGModelManager.MeshGeneration;

            for(int t = 0; t < _brushSelection.brushes.Length; t++)
            {
                CSGBrush         brush     = _brushSelection.brushes[t];
                ControlMeshState meshState = _brushSelection.states[t];

                if(!brush ||
                   meshState == null)
                {
                    continue;
                }

                Transform modelTransform = _brushSelection.modelTransforms[t];

                if(modelTransform &&
                   meshState.worldPoints.Length != 0 &&
                   meshState.edges.Length != 0)
                {
                    continue;
                }

                brushOutlineInfo.lastLineMeshGeneration = currentMeshGeneration - 1000;
                _brushSelection.UpdateParentModelTransforms();

                break;
            }

            if(brushOutlineInfo.lastLineMeshGeneration == currentMeshGeneration)
            {
                if(_preViewTool != Tools.viewTool ||
                   _prevTool != Tools.current)
                {
                    _preViewTool = Tools.viewTool;
                    _prevTool    = Tools.current;
                    UpdatePointSizes(sceneView);
                }

                return;
            }

            _preViewTool = Tools.viewTool;
            _prevTool    = Tools.current;

            brushOutlineInfo.lastLineMeshGeneration = currentMeshGeneration;

            UpdateMeshStates();
            UpdatePointSizes(sceneView);

            brushOutlineRenderer.Update(
                camera, _brushSelection.brushes, _brushSelection.controlMeshes, _brushSelection.states
            );
        }

        private void DrawPointText(SceneView sceneView, Vector3 brushCenter, Vector3 vertex, string text)
        {
            Vector2 brushCenter2D = HandleUtility.WorldToGUIPoint(brushCenter);

            Vector2 vertex2d    = HandleUtility.WorldToGUIPoint(vertex);
            Vector2 centerDelta = brushCenter2D - vertex2d;

            centerDelta.Normalize();
            centerDelta *= HoverTextDistance;

            Vector2 textCenter2D = vertex2d;

            textCenter2D.x -= centerDelta.y;
            textCenter2D.y -= centerDelta.x;

            Ray textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);

            Vector3 textCenter = textCenterRay.origin + textCenterRay.direction
              * ((sceneView.camera.farClipPlane + sceneView.camera.nearClipPlane) * 0.5f);

            Handles.color = Color.black;
            Handles.DrawLine(vertex, textCenter);

            PaintUtility.DrawScreenText(textCenter2D, text);
        }

        private void DrawEdgeText(
            SceneView sceneView, Vector3 brushCenter, Vector3 vertexA, Vector3 vertexB, string text
        )
        {
            Vector3 lineCenter    = (vertexB + vertexA) * 0.5f;
            Vector2 textCenter2D  = HandleUtility.WorldToGUIPoint(lineCenter);
            Vector2 brushCenter2D = HandleUtility.WorldToGUIPoint(brushCenter);

            Vector2 vertex2dA   = HandleUtility.WorldToGUIPoint(vertexA);
            Vector2 vertex2dB   = HandleUtility.WorldToGUIPoint(vertexB);
            Vector2 line2DDelta = vertex2dB - vertex2dA;
            Vector2 centerDelta = brushCenter2D - vertex2dA;

            float dot   = line2DDelta.x * centerDelta.x + line2DDelta.y * centerDelta.y;
            float det   = line2DDelta.x * centerDelta.y - line2DDelta.y * centerDelta.x;
            float angle = Mathf.Atan2(det, dot);

            if(Mathf.Sign(angle) < 0)
            {
                line2DDelta = -line2DDelta;
            }

            line2DDelta.y = -line2DDelta.y;
            line2DDelta.Normalize();
            line2DDelta *= HoverTextDistance;

            textCenter2D.x -= line2DDelta.y;
            textCenter2D.y -= line2DDelta.x;

            Ray textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);

            Vector3 textCenter = textCenterRay.origin + textCenterRay.direction
              * ((sceneView.camera.farClipPlane + sceneView.camera.nearClipPlane) * 0.5f);

            Handles.color = Color.black;
            Handles.DrawLine(lineCenter, textCenter);

            PaintUtility.DrawScreenText(textCenter2D, text);
        }

        private bool ShouldDrawBrushPoints()
        {
            if(_showMarquee)
            {
                return false;
            }

            if(_editMode == EditMode.MovingPoint || _editMode == EditMode.MovingEdge)
            {
                return !_mouseIsDragging || !_nonZeroMouseIsDown;
            }

            return !_mouseIsDragging && !_nonZeroMouseIsDown;
        }

        private bool ShouldDrawPolygonPoints(Camera camera)
        {
            if(_showMarquee || !camera || camera.orthographic)
            {
                return false;
            }

            if(_editMode == EditMode.MovingPolygon)
            {
                return !_mouseIsDragging || !_nonZeroMouseIsDown;
            }

            return !_mouseIsDragging && !_nonZeroMouseIsDown;
        }

        private bool ShouldDrawAnyPoints(Camera camera)
        {
            return ShouldDrawBrushPoints() || ShouldDrawPolygonPoints(camera);
        }

        private void OnPaint(SceneView sceneView)
        {
            Camera camera = sceneView.camera;

            if(!camera)
            {
                return;
            }

            BrushOutlineInfo brushOutlineInfo = GetBrushOutLineInfo(camera);

            if(brushOutlineInfo == null)
            {
                return;
            }

            if(_movePlaneInNormalDirection &&
               _hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.states.Length &&
               _hoverOnPolygonIndex != -1)
            {
                _currentCursor = CursorUtility.GetCursorForDirection(_movePolygonDirection, 90);
            }

            if(sceneView != null)
            {
                if(!Tools.viewToolActive)
                {
                    Rect windowRect = new Rect(
                        0, 0, sceneView.position.width
                      , sceneView.position.height - CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT
                    );

                    EditorGUIUtility.AddCursorRect(windowRect, _currentCursor);
                }
            }

            Matrix4x4 origMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            Tool currentTool = Tools.current;

            {
                brushOutlineInfo.brushOutlineRenderer.RenderOutlines();

                if(ShouldDrawAnyPoints(camera))
                {
                    _pointMeshManager.Begin();

                    if(ShouldDrawBrushPoints() && CSGSettings.SelectionVertex)
                    {
                        for(int t = 0; t < _brushSelection.brushes.Length; t++)
                        {
                            ControlMeshState meshState      = _brushSelection.states[t];
                            Transform        modelTransform = _brushSelection.modelTransforms[t];

                            if(modelTransform == null ||
                               meshState == null ||
                               meshState.worldPoints == null)
                            {
                                continue;
                            }

                            SceneMeshState cameraState = _brushSelection.states[t].GetCameraState(camera, false);

                            if(cameraState == null)
                            {
                                continue;
                            }

                            _pointMeshManager.DrawPoints(
                                camera, meshState.worldPoints,
                                cameraState.worldPointSizes,
                                cameraState.worldPointColors
                            );
                        }
                    }

                    if(ShouldDrawPolygonPoints(camera) && CSGSettings.SelectionSurface)
                    {
                        for(int t = 0; t < _brushSelection.brushes.Length; t++)
                        {
                            ControlMeshState meshState      = _brushSelection.states[t];
                            Transform        modelTransform = _brushSelection.modelTransforms[t];

                            if(modelTransform == null)
                            {
                                continue;
                            }

                            SceneMeshState cameraState = _brushSelection.states[t].GetCameraState(camera, false);

                            if(cameraState == null)
                            {
                                continue;
                            }

                            _pointMeshManager.DrawPoints(
                                camera, meshState.polygonCenterPoints,
                                cameraState.polygonCenterPointSizes,
                                meshState.polygonCenterColors
                            );
                        }
                    }

                    _pointMeshManager.End();
                    _pointMeshManager.Render(PaintUtility.CustomDotMaterial, PaintUtility.CustomSurfaceNoDepthMaterial);
                }

                if(currentTool == Tool.Rotate && _editMode == EditMode.RotateEdge)
                {
                    if(_mouseIsDragging)
                    {
                        PaintUtility.DrawRotateCircle(
                            camera, _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0
                          , _rotateStartAngle, _rotateCurrentSnappedAngle,
                            ColorSettings.RotateCircleOutline
                        );

                        PaintUtility.DrawRotateCirclePie(
                            _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, _rotateStartAngle
                          , _rotateCurrentSnappedAngle,
                            ColorSettings.RotateCircleOutline
                        );
                    }
                    else
                    {
                        bool inCamera = camera.pixelRect.Contains(Event.current.mousePosition);

                        if(inCamera && UpdateRotationCircle(sceneView))
                        {
                            PaintUtility.DrawRotateCircle(
                                camera, _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0
                              , _rotateStartAngle, _rotateStartAngle,
                                ColorSettings.RotateCircleOutline
                            );
                        }
                    }
                }

                if(Tools.current != Tool.Scale &&
                   Tools.current != Tool.Rotate &&
                   SelectionUtility.CurrentModifiers == EventModifiers.Shift
                && SelectionUtility.CurrentModifiers != EventModifiers.Control
                && _hoverOnTarget > -1 && _hoverOnPolygonIndex != -1
                  )
                {
                    int t = _hoverOnTarget;
                    int p = _hoverOnPolygonIndex;

                    if(t > -1 && t < _brushSelection.states.Length)
                    {
                        ControlMeshState targetMeshState = _brushSelection.states[t];
                        Transform        modelTransform  = _brushSelection.modelTransforms[t];

                        if(modelTransform != null)
                        {
                            if(_hoverOnTarget == t &&
                               p == _hoverOnPolygonIndex)
                            {
                                Handles.color =
                                    ColorSettings.PolygonInnerStateColor[
                                        (int)(SelectState.Selected | SelectState.Hovering)];
                            }

                            if(p < targetMeshState.polygonCenterPoints.Length)
                            {
                                Vector3 origin = targetMeshState.polygonCenterPoints[p];

                                Vector3 localNormal = targetMeshState.polygonCenterPlanes[p].normal;

                                Vector3 worldNormal = targetMeshState.brushTransform.localToWorldMatrix
                                   .MultiplyVector(localNormal)
                                   .normalized;

                                Handles.matrix = MathConstants.identityMatrix;

                                if(Tools.pivotRotation == PivotRotation.Global)
                                {
                                    worldNormal = GeometryUtility.SnapToClosestAxis(worldNormal);
                                }

                                PaintUtility.DrawArrowCap(origin, worldNormal, HandleUtility.GetHandleSize(origin));
                                Handles.matrix = MathConstants.identityMatrix;
                            }
                        }
                    }
                }

                if(SelectionUtility.CurrentModifiers == EventModifiers.Shift &&
                   camera.pixelRect.Contains(Event.current.mousePosition) &&
                   _hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.states.Length)
                {
                    ControlMeshState meshState      = _brushSelection.states[_hoverOnTarget];
                    Transform        modelTransform = _brushSelection.modelTransforms[_hoverOnTarget];

                    if(modelTransform != null)
                    {
                        if(_hoverOnPolygonIndex != -1)
                        {
                            Vector3 origin = meshState.polygonCenterPoints[_hoverOnPolygonIndex];

                            Vector2 textCenter2D = HandleUtility.WorldToGUIPoint(origin);
                            textCenter2D.y += HoverTextDistance * 2;

                            Ray textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);

                            Vector3 textCenter = textCenterRay.origin + textCenterRay.direction
                              * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

                            Handles.color = Color.black;

                            Handles.DrawLine(origin, textCenter);

                            PaintUtility.DrawScreenText(
                                textCenter2D
                              , Tools.pivotRotation == PivotRotation.Local
                                    ? "Normal Aligned Extrude [local]"
                                    : "Axis Aligned Extrude [global]"
                            );
                        }
                        else if(_hoverOnEdgeIndex != -1 &&
                                _hoverOnEdgeIndex * 2 + 1 < meshState.edges.Length)
                        {
                            int     pointIndex1 = meshState.edges[_hoverOnEdgeIndex * 2 + 0];
                            int     pointIndex2 = meshState.edges[_hoverOnEdgeIndex * 2 + 1];
                            Vector3 vertexA     = meshState.worldPoints[pointIndex1];
                            Vector3 vertexB     = meshState.worldPoints[pointIndex2];

                            DrawEdgeText(sceneView, meshState.brushCenter, vertexA, vertexB, "Chamfer");
                        }
                        else if(_hoverOnPointIndex != -1)
                        {
                            Vector3 vertex = meshState.worldPoints[_hoverOnPointIndex];

                            DrawPointText(sceneView, meshState.brushCenter, vertex, "Chamfer");
                        }
                    }
                }

                if(SelectionUtility.CurrentModifiers != EventModifiers.Shift &&
                   currentTool != Tool.Rotate && currentTool != Tool.Scale)
                {
                    if(_hoverOnEdgeIndex != -1 &&
                       _hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.states.Length)
                    {
                        ControlMeshState meshState = _brushSelection.states[_hoverOnTarget];

                        if(_hoverOnEdgeIndex * 2 + 1 < meshState.edges.Length)
                        {
                            Handles.matrix = origMatrix;
                            int     pointIndex1 = meshState.edges[_hoverOnEdgeIndex * 2 + 0];
                            int     pointIndex2 = meshState.edges[_hoverOnEdgeIndex * 2 + 1];
                            Vector3 vertexA     = meshState.worldPoints[pointIndex1];
                            Vector3 vertexB     = meshState.worldPoints[pointIndex2];

                            Vector3 lineDelta = vertexB - vertexA;
                            float   length    = lineDelta.magnitude;

                            DrawEdgeText(
                                sceneView, meshState.brushCenter, vertexA, vertexB
                              , Units.ToRoundedDistanceString(length)
                            );

                            Handles.matrix = MathConstants.identityMatrix;
                        }
                    }
                }
            }

            Handles.matrix = origMatrix;
        }

        private void RenderOffsetText()
        {
            Vector3 delta = _worldDeltaMovement + _extraDeltaMovement;

            if(Tools.pivotRotation == PivotRotation.Local)
            {
                if(activeSpaceMatrices == null)
                {
                    activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);
                }

                delta = GridUtility.CleanPosition(
                    activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta).normalized
                );
            }

            Vector2 textCenter2D = Event.current.mousePosition;
            textCenter2D.y += HoverTextDistance * 2;

            bool lockX = Mathf.Abs(delta.x) < MathConstants.ConsideredZero && (CSGSettings.LockAxisX
             || Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon);

            bool lockY = Mathf.Abs(delta.y) < MathConstants.ConsideredZero && (CSGSettings.LockAxisY
             || Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon);

            bool lockZ = Mathf.Abs(delta.z) < MathConstants.ConsideredZero && (CSGSettings.LockAxisZ
             || Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon);

            string text = Units.ToRoundedDistanceString(delta, lockX, lockY, lockZ);
            PaintUtility.DrawScreenText(textCenter2D, text);
        }

        private int _meshEditBrushToolId;

        private void CreateControlIDs()
        {
            _meshEditBrushToolId = GUIUtility.GetControlID(MeshEditBrushToolHash, FocusType.Keyboard);
            HandleUtility.AddDefaultControl(_meshEditBrushToolId);

            _rectSelectionId = GUIUtility.GetControlID(RectSelectionHash, FocusType.Keyboard);
            _chamferId       = GUIUtility.GetControlID(meshEdgeChamferHash, FocusType.Keyboard);

            if(_brushSelection.states == null)
            {
                return;
            }

            for(int t = 0; t < _brushSelection.states.Length; t++)
            {
                ControlMeshState meshState = _brushSelection.states[t];

                if(meshState == null)
                {
                    continue;
                }

                for(int p = 0; p < meshState.worldPoints.Length; p++)
                {
                    meshState.pointControlId[p] = GUIUtility.GetControlID(MeshEditBrushPointHash, FocusType.Keyboard);
                }

                for(int e = 0; e < meshState.edges.Length / 2; e++)
                {
                    meshState.edgeControlId[e] = GUIUtility.GetControlID(MeshEditBrushEdgeHash, FocusType.Keyboard);
                }

                for(int p = 0; p < meshState.polygonCenterPoints.Length; p++)
                {
                    meshState.polygonControlId[p] = GUIUtility.GetControlID(
                        MeshEditBrushPolygonHash, FocusType.Keyboard
                    );
                }
            }
        }

        private void CenterPositionHandle()
        {
            if(_brushSelection.brushes.Length <= 0)
            {
                useHandleCenter = false;

                return;
            }

            AABB bounds = _brushSelection.GetSelectionBounds();

            if(!bounds.Valid)
            {
                useHandleCenter = false;

                return;
            }

            handleCenter    = bounds.Center;
            handleScale     = Vector3.one;
            useHandleCenter = true;
        }

        private Quaternion GetRealHandleRotation()
        {
            Quaternion rotation = Tools.handleRotation;

            if(Tools.pivotRotation == PivotRotation.Local)
            {
                int polygonSelectedCount = 0;

                for(int t = 0; t < _brushSelection.states.Length; t++)
                {
                    ControlMeshState targetMeshState = _brushSelection.states[t];

                    for(int p = 0; p < targetMeshState.PolygonCount; p++)
                    {
                        if(!targetMeshState.IsPolygonSelected(p))
                        {
                            continue;
                        }

                        polygonSelectedCount++;
                        Vector3 localNormal = targetMeshState.polygonCenterPlanes[p].normal;

                        Vector3 worldNormal = targetMeshState.brushTransform.localToWorldMatrix
                           .MultiplyVector(localNormal)
                           .normalized;

                        if(worldNormal.sqrMagnitude < MathConstants.EqualityEpsilonSqr)
                        {
                            continue;
                        }

                        rotation = Quaternion.LookRotation(worldNormal);

                        if(Vector3.Dot(rotation * MathConstants.forwardVector3, worldNormal) < 0)
                        {
                            rotation = Quaternion.Inverse(rotation);
                        }

                        if(polygonSelectedCount > 1)
                        {
                            break;
                        }
                    }

                    if(polygonSelectedCount > 1)
                    {
                        break;
                    }
                }

                if(polygonSelectedCount != 1)
                {
                    rotation = Tools.handleRotation;
                }
            }

            if(rotation.x <= MathConstants.EqualityEpsilon &&
               rotation.y <= MathConstants.EqualityEpsilon &&
               rotation.z <= MathConstants.EqualityEpsilon &&
               rotation.w <= MathConstants.EqualityEpsilon)
            {
                rotation = Quaternion.identity;
            }

            return rotation;
        }

        private void DrawScaleBounds(
            Camera camera, Quaternion rotation, Vector3 scale, Vector3 center, Vector3[] worldPoints
        )
        {
            bool lockX = Mathf.Abs(scale.x) - 1 < MathConstants.ConsideredZero && (CSGSettings.LockAxisX
             || Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon);

            bool lockY = Mathf.Abs(scale.y) - 1 < MathConstants.ConsideredZero && (CSGSettings.LockAxisY
             || Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon);

            bool lockZ = Mathf.Abs(scale.z) - 1 < MathConstants.ConsideredZero && (CSGSettings.LockAxisZ
             || Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon);

            string text = Units.ToRoundedScaleString(scale, lockX, lockY, lockZ);
            PaintUtility.DrawScreenText(camera, handleCenter, HoverTextDistance * 3, text);

            AABB      bounds         = BoundsUtilities.GetBounds(worldPoints, rotation, scale, center);
            Vector3[] outputVertices = new Vector3[8];
            BoundsUtilities.GetBoundsCornerPoints(bounds, outputVertices);

            PaintUtility.RenderBoundsSizes(
                Quaternion.Inverse(rotation), rotation, camera, outputVertices, Color.white, Color.white, Color.white
              , true, true, true
            );
        }

        private CSGPlane[] _chamferPlanes = new CSGPlane[2];
        private readonly Vector3[] _chamferEdgePoints = new Vector3[2];
        private Vector3 _chamferDirection = Vector3.zero;
        private Vector3 _chamferIntersectionPoint = Vector3.zero;
        private bool[] _chamferBrushModified = Array.Empty<bool>();
        private Shape[] _chamferShapes = Array.Empty<Shape>();
        private ControlMesh[] _chamferControlMesh = Array.Empty<ControlMesh>();
        private ControlMeshSelection[] _chamferSelection = Array.Empty<ControlMeshSelection>();
        private Vector3 _chamferVertex = Vector3.zero;
        private ChamferMode _chamferMode = ChamferMode.Point;

        private enum ChamferMode
        {
            Point
          , Edge
        }

        private bool StartChamfer(SceneView sceneView)
        {
            if(_hoverOnTarget <= -1 ||
               _hoverOnTarget >= _brushSelection.states.Length)
            {
                return false;
            }

            UpdateBackupPoints();

            CSGBrush         hoverBrush         = _brushSelection.brushes[_hoverOnTarget];
            ControlMeshState hoverMeshState     = _brushSelection.states[_hoverOnTarget];
            Transform        transform          = hoverBrush.transform;
            ControlMesh      controlMesh        = hoverBrush.ControlMesh;
            Shape            shape              = hoverBrush.Shape;
            Matrix4x4        localToWorldMatrix = transform.localToWorldMatrix;

            if(_hoverOnEdgeIndex != -1)
            {
                _chamferMode = ChamferMode.Edge;
                int   halfEdgEIndex = hoverMeshState.edgeStateToHalfEdge[_hoverOnEdgeIndex];
                int   twinIndex     = controlMesh.Edges[halfEdgEIndex].TwinIndex;
                short polygonIndex1 = controlMesh.Edges[halfEdgEIndex].PolygonIndex;
                short polygonIndex2 = controlMesh.Edges[twinIndex].PolygonIndex;
                int   texGenIndex1  = controlMesh.Polygons[polygonIndex1].TexGenIndex;
                int   texGenIndex2  = controlMesh.Polygons[polygonIndex2].TexGenIndex;
                _chamferPlanes    = new CSGPlane[2];
                _chamferPlanes[0] = shape.Surfaces[texGenIndex1].Plane;
                _chamferPlanes[1] = shape.Surfaces[texGenIndex2].Plane;

                for(int p = 0; p < _chamferPlanes.Length; p++)
                {
                    _chamferPlanes[p].Transform(localToWorldMatrix);
                }

                int pointIndex1 = hoverMeshState.edges[_hoverOnEdgeIndex * 2 + 0];
                int pointIndex2 = hoverMeshState.edges[_hoverOnEdgeIndex * 2 + 1];

                _chamferEdgePoints[0] = hoverMeshState.worldPoints[pointIndex1];
                _chamferEdgePoints[1] = hoverMeshState.worldPoints[pointIndex2];

                _chamferDirection = (_chamferEdgePoints[1] - _chamferEdgePoints[0]).normalized;
            }
            else if(_hoverOnPointIndex != -1)
            {
                _chamferVertex = hoverMeshState.worldPoints[_hoverOnPointIndex];
                _chamferMode   = ChamferMode.Point;
                List<CSGPlane> foundPlanes = new List<CSGPlane>();

                for(int e = 0; e < controlMesh.Edges.Length; e++)
                {
                    if(controlMesh.Edges[e].VertexIndex != _hoverOnPointIndex)
                    {
                        continue;
                    }

                    short    polygonIndex = controlMesh.Edges[e].PolygonIndex;
                    int      texGenIndex  = controlMesh.Polygons[polygonIndex].TexGenIndex;
                    CSGPlane plane        = shape.Surfaces[texGenIndex].Plane;
                    plane.Transform(localToWorldMatrix);
                    foundPlanes.Add(plane);
                }

                _chamferPlanes = foundPlanes.ToArray();
            }
            else
            {
                return false;
            }

            GUIUtility.hotControl             = _chamferId;
            GUIUtility.keyboardControl        = _chamferId;
            EditorGUIUtility.editingTextField = false;
            Event.current.Use();

            _chamferBrushModified = new bool[_brushSelection.brushes.Length];
            _chamferShapes        = new Shape[_brushSelection.brushes.Length];
            _chamferControlMesh   = new ControlMesh[_brushSelection.brushes.Length];
            _chamferSelection     = new ControlMeshSelection[_brushSelection.brushes.Length];

            for(int b = 0; b < _brushSelection.brushes.Length; b++)
            {
                _chamferShapes[b]      = _brushSelection.brushes[b].Shape.Clone();
                _chamferControlMesh[b] = _brushSelection.brushes[b].ControlMesh.Clone();
                _chamferSelection[b]   = _brushSelection.states[b].selection.Clone();
            }

            UpdateChamferIntersectionPoint(sceneView);

            return true;
        }

        private void UpdateChamferIntersectionPoint(SceneView sceneView)
        {
            Camera camera   = sceneView.camera;
            Ray    mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            Vector3 intersection = Vector3.zero;
            bool    found        = false;

            for(int p0 = 0; p0 < _chamferPlanes.Length; p0++)
            {
                Vector3 tryIntersection = _chamferPlanes[p0].RayIntersection(mouseRay);
                bool    tryFound        = true;

                for(int p1 = 0; p1 < _chamferPlanes.Length; p1++)
                {
                    if(p0 == p1)
                    {
                        continue;
                    }

                    if(_chamferPlanes[p1].Distance(tryIntersection) >= MathConstants.DistanceEpsilon)
                    {
                        tryFound = false;

                        break;
                    }
                }

                if(!tryFound)
                {
                    continue;
                }

                CSGGrid.SetForcedGrid(camera, _chamferPlanes[p0]);
                intersection = tryIntersection;
                found        = true;

                break;
            }

            if(!found)
            {
                return;
            }

            SnapMode activeSnapMode = CSGSettings.ActiveSnappingMode;
            Vector3  projectedPoint;

            switch(_chamferMode)
            {
                default:
                {
                    return;
                }
                case ChamferMode.Point:
                {
                    projectedPoint = _chamferVertex;

                    switch(activeSnapMode)
                    {
                        case SnapMode.GridSnapping:
                        {
                            _chamferIntersectionPoint = intersection + CSGGrid.SnapDeltaToGrid(
                                camera, intersection - projectedPoint, new[] { intersection }
                            );

                            break;
                        }
                        case SnapMode.RelativeSnapping:
                        {
                            _chamferIntersectionPoint = intersection + CSGGrid.SnapDeltaRelative(
                                camera, intersection - projectedPoint
                            );

                            break;
                        }
                        default:
                        case SnapMode.None:
                        {
                            _chamferIntersectionPoint =
                                intersection + CSGGrid.HandleLockedAxi(intersection - projectedPoint);

                            break;
                        }
                    }

                    break;
                }
                case ChamferMode.Edge:
                {
                    projectedPoint = GeometryUtility.ProjectPointOnInfiniteLine(
                        intersection, _chamferEdgePoints[0], _chamferDirection
                    );

                    switch(activeSnapMode)
                    {
                        case SnapMode.GridSnapping:
                        {
                            _chamferIntersectionPoint = intersection + CSGGrid.SnapDeltaToGrid(
                                camera, intersection - projectedPoint, new[] { intersection }
                            );

                            break;
                        }
                        case SnapMode.RelativeSnapping:
                        {
                            _chamferIntersectionPoint = intersection + CSGGrid.SnapDeltaRelative(
                                camera, intersection - projectedPoint
                            );

                            break;
                        }
                        default:
                        case SnapMode.None:
                        {
                            _chamferIntersectionPoint =
                                intersection + CSGGrid.HandleLockedAxi(intersection - projectedPoint);

                            break;
                        }
                    }

                    projectedPoint = GeometryUtility.ProjectPointOnInfiniteLine(
                        _chamferIntersectionPoint, _chamferEdgePoints[0], _chamferDirection
                    );

                    break;
                }
            }

            float distance = (_chamferIntersectionPoint - projectedPoint).magnitude;

            Undo.RecordObjects(undoAbleBrushes, "Chamfer brushes");

            for(int t = 0; t < _brushSelection.brushes.Length; t++)
            {
                _brushSelection.states[t].UpdateMesh(_chamferControlMesh[t]);
                _chamferSelection[t].CopyTo(_brushSelection.states[t].selection);
            }

            bool anyBrushModified = false;

            List<int>         selectedEdges = new List<int>();
            HashSet<CSGPlane> planes        = new HashSet<CSGPlane>();
            List<Vector3>     foundPoints   = new List<Vector3>();

            for(int b = 0; b < _brushSelection.brushes.Length; b++)
            {
                bool             brushModified = false;
                CSGBrush         brush         = _brushSelection.brushes[b];
                ControlMesh      controlMesh   = _chamferControlMesh[b].Clone();
                Shape            shape         = _chamferShapes[b].Clone();
                ControlMeshState meshState     = _brushSelection.states[b];

                Transform transform = brush.transform;

                planes.Clear();

                if(Math.Abs(distance) > MathConstants.EqualityEpsilon)
                {
                    for(int e = 0; e < meshState.selection.edges.Length; e++)
                    {
                        int   halfEdgeIndex = meshState.edgeStateToHalfEdge[e];
                        int   twinIndex     = controlMesh.Edges[halfEdgeIndex].TwinIndex;
                        short polygonIndex1 = controlMesh.Edges[halfEdgeIndex].PolygonIndex;
                        short polygonIndex2 = controlMesh.Edges[twinIndex].PolygonIndex;

                        if((meshState.selection.edges[e] & (SelectState.Selected | SelectState.Hovering))
                        == SelectState.None &&
                           (meshState.selection.polygons[polygonIndex1] & SelectState.Selected)
                        != SelectState.Selected &&
                           (meshState.selection.polygons[polygonIndex2] & SelectState.Selected)
                        != SelectState.Selected)
                        {
                            continue;
                        }

                        int pointIndex1 = meshState.edges[e * 2 + 0];
                        int pointIndex2 = meshState.edges[e * 2 + 1];

                        int      texGenIndex1   = controlMesh.Polygons[polygonIndex1].TexGenIndex;
                        int      texGenIndex2   = controlMesh.Polygons[polygonIndex2].TexGenIndex;
                        CSGPlane chamferPlanes0 = shape.Surfaces[texGenIndex1].Plane;
                        CSGPlane chamferPlanes1 = shape.Surfaces[texGenIndex2].Plane;

                        Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
                        chamferPlanes0.Transform(localToWorldMatrix);
                        chamferPlanes1.Transform(localToWorldMatrix);

                        Vector3 chamferEdgePoints0 = meshState.worldPoints[pointIndex1];
                        Vector3 chamferEdgePoints1 = meshState.worldPoints[pointIndex2];

                        Vector3 chamferEdge = (chamferEdgePoints1 - chamferEdgePoints0).normalized;

                        Vector3 chamferTangents0 = Vector3.Cross(chamferPlanes0.normal, chamferEdge);
                        Vector3 chamferTangents1 = Vector3.Cross(chamferPlanes1.normal, chamferEdge);

                        if(chamferPlanes1.Distance(chamferEdgePoints0 + chamferTangents0) > 0)
                        {
                            chamferTangents0 = -chamferTangents0;
                        }

                        if(chamferPlanes0.Distance(chamferEdgePoints0 + chamferTangents1) > 0)
                        {
                            chamferTangents1 = -chamferTangents1;
                        }

                        Vector3 delta          = chamferTangents0 * distance;
                        Vector3 chamferPoints0 = chamferEdgePoints0 + delta;
                        Vector3 chamferPoints1 = chamferEdgePoints1 + delta;

                        delta = chamferTangents1 * distance;
                        Vector3 chamferPoints2 = chamferEdgePoints0 + delta;

                        CSGPlane chamferPlane = new CSGPlane(chamferPoints0, chamferPoints1, chamferPoints2);

                        CSGPlane localCuttingPlane = GeometryUtility.InverseTransformPlane(
                            brush.transform.localToWorldMatrix, chamferPlane
                        );

                        planes.Add(localCuttingPlane);
                    }

                    HashSet<int> indirectPoints = new HashSet<int>();

                    for(int p = 0; p < meshState.selection.polygons.Length; p++)
                    {
                        if((meshState.selection.polygons[p] & SelectState.Selected) != SelectState.Selected)
                        {
                            continue;
                        }

                        int[] pointIndices = meshState.polygonPointIndices[p];

                        for(int i = 0; i < pointIndices.Length; i++)
                        {
                            indirectPoints.Add((short)pointIndices[i]);
                        }
                    }

                    for(int p = 0; p < meshState.selection.points.Length; p++)
                    {
                        if((meshState.selection.points[p] & (SelectState.Selected | SelectState.Hovering))
                        == SelectState.None &&
                           !indirectPoints.Contains(p))
                        {
                            continue;
                        }

                        Matrix4x4 localToWorldMatrix = brush.transform.localToWorldMatrix;

                        int     currPointIndex = p;
                        Vector3 currPoint      = localToWorldMatrix.MultiplyPoint(controlMesh.Vertices[currPointIndex]);

                        foundPoints.Clear();

                        for(int e = 0; e < controlMesh.Edges.Length; e++)
                        {
                            if(controlMesh.Edges[e].VertexIndex != p)
                            {
                                continue;
                            }

                            int   twinIndex      = controlMesh.Edges[e].TwinIndex;
                            short twinPointIndex = controlMesh.Edges[twinIndex].VertexIndex;

                            Vector3 twinPoint =
                                localToWorldMatrix.MultiplyPoint(controlMesh.Vertices[twinPointIndex]);

                            Vector3 direction = (twinPoint - currPoint).normalized;

                            foundPoints.Add(currPoint + direction * distance);
                        }

                        CSGPlane chamferPlane = GeometryUtility.CalcPolygonPlane(foundPoints.ToArray());

                        if(chamferPlane.Distance(currPoint) < 0)
                        {
                            chamferPlane = chamferPlane.Negated();
                        }

                        CSGPlane localCuttingPlane = GeometryUtility.InverseTransformPlane(
                            brush.transform.localToWorldMatrix, chamferPlane
                        );

                        planes.Add(localCuttingPlane);
                    }

                    foreach(CSGPlane plane in planes)
                    {
                        selectedEdges.Clear();

                        if(ControlMeshUtility.CutMesh(controlMesh, shape, plane, ref selectedEdges))
                        {
                            List<int> edgeLoop = ControlMeshUtility.FindEdgeLoop(controlMesh, ref selectedEdges);

                            if(edgeLoop != null)
                            {
                                if(ControlMeshUtility.SplitEdgeLoop(controlMesh, shape, edgeLoop))
                                {
                                    if(ControlMeshUtility.FindAndDetachSeparatePiece(
                                           controlMesh, shape, plane, out ControlMesh foundControlMesh
                                         , out Shape foundShape
                                       ))
                                    {
                                        controlMesh   = foundControlMesh;
                                        shape         = foundShape;
                                        brushModified = true;
                                    }
                                }
                            }
                        }
                    }
                }

                if(brushModified || _chamferBrushModified[b])
                {
                    anyBrushModified         = true;
                    _chamferBrushModified[b] = brushModified;
                    brush.ControlMesh        = controlMesh;
                    brush.Shape              = shape;
                    brush.ControlMesh.SetDirty();
                    ControlMeshUtility.RebuildShape(brush);
                    InternalCSGModelManager.CheckSurfaceModifications(brush, true);
                    UpdateWorkControlMesh();
                }
            }

            for(int t = 0; t < _brushSelection.brushes.Length; t++)
            {
                _brushSelection.states[t].UpdateMesh(_chamferControlMesh[t]);
                _brushSelection.states[t].selection.DeselectAll();
            }

            if(anyBrushModified)
            {
                UpdateLineMeshes(sceneView);
                InternalCSGModelManager.CheckForChanges(true);
            }
        }

        private void HandleChamfering(SceneView sceneView)
        {
            EventType type = Event.current.GetTypeForControl(_chamferId);

            switch(type)
            {
                case EventType.MouseMove:
                case EventType.MouseUp:
                {
                    _brushSelection.UpdateTargets();
                    UpdateWorkControlMesh();

                    Camera camera = sceneView.camera;

                    if(camera)
                    {
                        BrushOutlineInfo brushOutlineInfo = GetBrushOutLineInfo(camera);

                        if(brushOutlineInfo != null)
                        {
                            brushOutlineInfo.lastLineMeshGeneration--;
                        }
                    }

                    UpdateLineMeshes(sceneView);
                    CSGGrid.ForceGrid                 = false;
                    GUIUtility.hotControl             = 0;
                    GUIUtility.keyboardControl        = 0;
                    EditorGUIUtility.editingTextField = false;
                    Event.current.Use();

                    break;
                }
                case EventType.MouseDrag:
                {
                    UpdateChamferIntersectionPoint(sceneView);

                    Event.current.Use();

                    break;
                }
                case EventType.Repaint:
                {
                    Handles.color = ColorSettings.HoverOutlines;

                    Camera camera = sceneView.camera;

                    if(camera)
                    {
                        BrushOutlineInfo brushOutlineInfo = GetBrushOutLineInfo(camera);

                        if(brushOutlineInfo != null)
                        {
                            brushOutlineInfo.brushOutlineRenderer.RenderOutlines();
                        }
                    }

                    PaintUtility.SquareDotCap(
                        -1, _chamferIntersectionPoint, MathConstants.identityQuaternion
                      , CSGHandleUtility.GetHandleSize(_chamferIntersectionPoint) * GUIConstants.HANDLE_SCALE
                    );

                    break;
                }
            }
        }

        [NonSerialized] private Vector2 _prevMousePos;

        public void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            EventType originalEventType = Event.current.type;

            switch(originalEventType)
            {
                case EventType.MouseMove:
                {
                    _mouseIsDragging    = false;
                    _mouseIsDown        = false;
                    _nonZeroMouseIsDown = false;

                    break;
                }
                case EventType.MouseDown:
                {
                    _mouseIsDragging = false;
                    _mouseIsDown     = true;

                    if(Event.current.button != 0)
                    {
                        _nonZeroMouseIsDown = true;
                    }

                    _prevMousePos = Event.current.mousePosition;

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(Event.current.button != 0)
                    {
                        _nonZeroMouseIsDown = true;
                    }

                    if(!_mouseIsDragging && (_prevMousePos - Event.current.mousePosition).magnitude > 4.0f)
                    {
                        _mouseIsDragging = true;
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    _mouseIsDown        = false;
                    _nonZeroMouseIsDown = false;

                    break;
                }
            }

            if(Event.current.type == EventType.Repaint)
            {
                UpdateLineMeshes(sceneView);
            }

            int currentHotControl = GUIUtility.hotControl;

            if(currentHotControl != _chamferId &&
               !SceneDragToolManager.IsDraggingObjectInScene &&
               Event.current.GetTypeForControl(_meshEditBrushToolId) == EventType.Repaint)
            {
                OnPaint(sceneView);
            }

            CreateControlIDs();

            Camera camera = sceneView.camera;

            if(SelectionUtility.CurrentModifiers == EventModifiers.None &&
               currentHotControl != _chamferId &&
               useHandleCenter)
            {
                CSGHandles.InitFunction init = delegate
                {
                    UpdateTransformMatrices();
                    UpdateSelection(false);
                    UpdateWorkControlMesh();
                    UpdateBackupPoints();
                    UpdateGrid(_startCamera);
                    handleWorldPoints = _brushSelection.GetSelectedWorldPoints(true);
                    CenterPositionHandle();
                    startHandleCenter = handleCenter;
                    _usingControl     = true;
                };

                if(GUIUtility.hotControl == 0)
                {
                    handleWorldPoints = null;
                    handleScale       = Vector3.one;
                }

                switch(Tools.current)
                {
                    case Tool.None:
                    case Tool.Rect: break;
                    case Tool.Rotate:
                    {
                        void shutdown()
                        {
                            MergeDuplicatePoints();

                            if(UpdateWorkControlMesh(true))
                            {
                                UpdateBackupPoints();
                            }
                            else
                            {
                                Undo.PerformUndo();
                            }

                            InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.brushes);
                            _usingControl = false;
                        }

                        Quaternion handleRotation = GetRealHandleRotation();

                        Quaternion newRotation = PaintUtility.HandleRotation(
                            camera, handleCenter, handleRotation, init, shutdown
                        );

                        if(GUI.changed)
                        {
                            ForceLineUpdate();

                            DoRotateControlPoints(
                                handleCenter, handleRotation, Quaternion.Inverse(handleRotation) * newRotation
                            );

                            ControlMeshUtility.MergeDuplicatePoints(_brushSelection.brushes, _brushSelection.states);
                            GUI.changed = false;
                        }

                        break;
                    }
                    case Tool.Scale:
                    {
                        Quaternion rotation = GetRealHandleRotation();

                        void shutdown()
                        {
                            MergeDuplicatePoints();

                            if(UpdateWorkControlMesh())
                            {
                                if(_editMode == EditMode.ScalePolygon)
                                {
                                    _brushSelection.brushes = null;
                                    SetTargets(EditModeManager.FilteredSelection);
                                }
                                else
                                {
                                    UpdateBackupPoints();
                                }
                            }
                            else
                            {
                                Undo.PerformUndo();
                            }

                            _usingControl = false;
                        }

                        Vector3 newHandleScale = PaintUtility.HandleScale(
                            handleScale, handleCenter, rotation, init, shutdown
                        );

                        if(GUI.changed)
                        {
                            Vector3 newScale = newHandleScale;

                            if(float.IsInfinity(newScale.x) || float.IsNaN(newScale.x) ||
                               float.IsInfinity(newScale.y) || float.IsNaN(newScale.y) ||
                               float.IsInfinity(newScale.z) || float.IsNaN(newScale.z))
                            {
                                newScale = Vector3.zero;
                            }

                            if(newScale.x <= MathConstants.EqualityEpsilon)
                            {
                                newScale.x = 0.0f;
                            }

                            if(newScale.y <= MathConstants.EqualityEpsilon)
                            {
                                newScale.y = 0.0f;
                            }

                            if(newScale.z <= MathConstants.EqualityEpsilon)
                            {
                                newScale.z = 0.0f;
                            }

                            DoScaleControlPoints(rotation, newScale, startHandleCenter);
                            ControlMeshUtility.MergeDuplicatePoints(_brushSelection.brushes, _brushSelection.states);
                            handleScale = newScale;
                            GUI.changed = false;
                        }

                        if(_usingControl)
                        {
                            DrawScaleBounds(camera, rotation, handleScale, startHandleCenter, handleWorldPoints);
                        }

                        break;
                    }
                    case Tool.Move:
                    {
                        Quaternion rotation = GetRealHandleRotation();

                        void shutdown()
                        {
                            MergeDuplicatePoints();

                            if(UpdateWorkControlMesh(true))
                            {
                                UpdateBackupPoints();
                            }
                            else
                            {
                                Undo.PerformUndo();
                            }

                            InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.brushes);
                            _usingControl = false;
                        }

                        Vector3 newHandleCenter = PaintUtility.HandlePosition(
                            camera, handleCenter, rotation, handleWorldPoints, init, shutdown
                        );

                        if(GUI.changed)
                        {
                            ForceLineUpdate();
                            Vector3 offset = newHandleCenter - handleCenter;
                            _worldDeltaMovement += offset;
                            DoMoveControlPoints(_worldDeltaMovement);
                            ControlMeshUtility.MergeDuplicatePoints(_brushSelection.brushes, _brushSelection.states);
                            handleCenter = startHandleCenter + _worldDeltaMovement;
                            GUI.changed  = false;
                        }

                        if(_usingControl)
                        {
                            bool lockX = Mathf.Abs(_worldDeltaMovement.x) < MathConstants.ConsideredZero
                             && (CSGSettings.LockAxisX || Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon);

                            bool lockY = Mathf.Abs(_worldDeltaMovement.y) < MathConstants.ConsideredZero
                             && (CSGSettings.LockAxisY || Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon);

                            bool lockZ = Mathf.Abs(_worldDeltaMovement.z) < MathConstants.ConsideredZero
                             && (CSGSettings.LockAxisZ || Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon);

                            string text = Units.ToRoundedDistanceString(_worldDeltaMovement, lockX, lockY, lockZ);
                            PaintUtility.DrawScreenText(camera, handleCenter, HoverTextDistance * 3, text);
                        }

                        break;
                    }
                }
            }

            if(Event.current.type == EventType.Repaint)
            {
                if(_showMarquee &&
                   GUIUtility.hotControl == _rectSelectionId &&
                   camera.pixelRect.Contains(_startMousePoint))
                {
                    PaintUtility.DrawSelectionRectangle(
                        CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition)
                    );
                }
            }

            try
            {
                switch(Event.current.type)
                {
                    case EventType.MouseDown:
                    {
                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        if(GUIUtility.hotControl != 0 ||
                           Event.current.button != 0 ||
                           (SelectionUtility.CurrentModifiers & EventModifiers.Alt) == EventModifiers.Alt)
                        {
                            break;
                        }

                        if(SelectionUtility.CurrentModifiers == EventModifiers.Shift)
                        {
                            if(_editMode == EditMode.MovingPolygon ||
                               _editMode == EditMode.MovingPoint ||
                               _editMode == EditMode.MovingEdge)
                            {
                                _doMarquee   = false;
                                _showMarquee = false;
                                Event.current.Use();

                                break;
                            }
                        }

                        _doMarquee          = false;
                        _showMarquee        = false;
                        _firstMove          = true;
                        _extraDeltaMovement = MathConstants.zeroVector3;
                        _worldDeltaMovement = MathConstants.zeroVector3;

                        int newControlId = -1;

                        if(_hoverOnTarget > -1 && _hoverOnTarget < _brushSelection.states.Length)
                        {
                            UpdateWorkControlMesh();

                            switch(_editMode)
                            {
                                case EditMode.RotateEdge:
                                {
                                    newControlId = _brushSelection.states[_hoverOnTarget]
                                       .edgeControlId[_hoverOnEdgeIndex];

                                    if(!UpdateRotationCircle(sceneView))
                                    {
                                        break;
                                    }

                                    _rotateCurrentAngle        = _rotateStartAngle;
                                    _rotateCurrentSnappedAngle = _rotateStartAngle;

                                    Undo.IncrementCurrentGroup();
                                    _rotationUndoGroupIndex = Undo.GetCurrentGroup();

                                    break;
                                }
                                case EditMode.MovingEdge:
                                {
                                    newControlId = _brushSelection.states[_hoverOnTarget]
                                       .edgeControlId[_hoverOnEdgeIndex];

                                    break;
                                }
                                case EditMode.MovingPoint:
                                {
                                    newControlId = _brushSelection.states[_hoverOnTarget]
                                       .pointControlId[_hoverOnPointIndex];

                                    break;
                                }
                                case EditMode.MovingPolygon:
                                {
                                    newControlId = _brushSelection.states[_hoverOnTarget]
                                       .polygonControlId[_hoverOnPolygonIndex];

                                    if(Tools.current == Tool.Scale)
                                    {
                                        _editMode = EditMode.ScalePolygon;
                                    }

                                    break;
                                }
                            }
                        }

                        if(newControlId != -1)
                        {
                            GUIUtility.hotControl             = newControlId;
                            GUIUtility.keyboardControl        = newControlId;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();
                        }
                        else
                        {
                            _doMarquee       = true;
                            _startMousePoint = Event.current.mousePosition;

                            CSGEditorGUIUtility.RepaintAll();
                        }

                        break;
                    }

                    case EventType.MouseDrag:
                    {
                        if(_doMarquee)
                        {
                            if(GUIUtility.hotControl == 0)
                            {
                                _doMarquee                        = true;
                                GUIUtility.hotControl             = _rectSelectionId;
                                GUIUtility.keyboardControl        = _rectSelectionId;
                                EditorGUIUtility.editingTextField = false;
                            }
                            else
                            {
                                _doMarquee = false;
                            }
                        }

                        if(Event.current.button != 0)
                        {
                            break;
                        }

                        if(SelectionUtility.CurrentModifiers != EventModifiers.Shift)
                        {
                            break;
                        }

                        switch(_editMode)
                        {
                            case EditMode.MovingPolygon:
                            {
                                ExtrudeSurface(camera, true);
                                Event.current.Use();

                                break;
                            }
                            case EditMode.MovingPoint:
                            case EditMode.MovingEdge:
                            {
                                if(GUIUtility.hotControl != 0)
                                {
                                    break;
                                }

                                if(!StartChamfer(sceneView))
                                {
                                    GUIUtility.hotControl             = 0;
                                    GUIUtility.keyboardControl        = 0;
                                    EditorGUIUtility.editingTextField = false;
                                }

                                Event.current.Use();
                                ;

                                break;
                            }
                        }

                        break;
                    }

                    case EventType.MouseUp:
                    {
                        if(_mouseIsDragging || _showMarquee)
                        {
                            break;
                        }

                        if(!UpdateSelection())
                        {
                            if(_brushSelection.HaveSelection)
                            {
                                DeselectAll();
                            }
                            else
                            {
                                SelectionUtility.DoSelectionClick(sceneView);
                            }
                        }
                        else
                        {
                            CSGEditorGUIUtility.RepaintAll();
                        }

                        break;
                    }

                    case EventType.KeyDown:
                    {
                        if(GUIUtility.hotControl != 0)
                        {
                            break;
                        }

                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed()
                        || Keys.cancelActionKey.IsKeyPressed() || Keys.snapToGridKey.IsKeyPressed()
                        || Keys.mergeEdgePoints.IsKeyPressed() && HaveEdgeSelection
                        || Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false)
                        || Keys.flipSelectionX.IsKeyPressed() || Keys.flipSelectionY.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionZ.IsKeyPressed())
                        {
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.KeyUp:
                    {
                        if(GUIUtility.hotControl != 0)
                        {
                            break;
                        }

                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed())
                        {
                            Event.current.Use();
                            DeleteSelectedPoints();

                            break;
                        }

                        if(Keys.snapToGridKey.IsKeyPressed())
                        {
                            SnapToGrid(camera);
                            Event.current.Use();

                            break;
                        }

                        if(Keys.mergeEdgePoints.IsKeyPressed() && HaveEdgeSelection)
                        {
                            MergeSelected();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionX.IsKeyPressed())
                        {
                            FlipX();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionY.IsKeyPressed())
                        {
                            FlipY();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionZ.IsKeyPressed())
                        {
                            FlipZ();
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.ValidateCommand:
                    {
                        if(GUIUtility.hotControl != 0)
                        {
                            break;
                        }

                        SceneQueryUtility.ClearDeepClick();

                        if(HavePointSelection && (Event.current.commandName == "SoftDelete"
                            || Event.current.commandName == "Delete") || Keys.cancelActionKey.IsKeyPressed()
                        || Keys.snapToGridKey.IsKeyPressed() || Keys.mergeEdgePoints.IsKeyPressed() && HaveEdgeSelection
                        || Keys.HandleSceneValidate(EditModeManager.CurrentTool, false)
                        || Keys.flipSelectionX.IsKeyPressed() || Keys.flipSelectionY.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionZ.IsKeyPressed())
                        {
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.ExecuteCommand:
                    {
                        if(GUIUtility.hotControl != 0)
                        {
                            break;
                        }

                        if(HavePointSelection &&
                           (Event.current.commandName == "SoftDelete" ||
                               Event.current.commandName == "Delete"))
                        {
                            DeleteSelectedPoints();
                            Event.current.Use();
                        }

                        break;
                    }

                    case EventType.Layout:
                    {
                        UpdateMouseCursor();

                        if(_brushSelection.brushes == null)
                        {
                            break;
                        }

                        if(_brushSelection.states.Length != _brushSelection.brushes.Length)
                        {
                            break;
                        }

                        Matrix4x4 origMatrix = Handles.matrix;
                        Handles.matrix = MathConstants.identityMatrix;

                        try
                        {
                            Tool currentTool = Tools.current;

                            bool inCamera = camera && camera.pixelRect.Contains(Event.current.mousePosition);

                            if(!inCamera || _mouseIsDragging || GUIUtility.hotControl != 0)
                            {
                                break;
                            }

                            if(_mouseIsDown || !ShouldDrawAnyPoints(camera))
                            {
                                break;
                            }

                            _hoverOnEdgeIndex    = -1;
                            _hoverOnPointIndex   = -1;
                            _hoverOnPolygonIndex = -1;
                            _hoverOnTarget       = -1;

                            CSGPlane cameraPlane = CSGHandleUtility.GetNearPlane(camera);

                            int hoverControl = 0;
                            int hotControl   = GUIUtility.hotControl;

                            for(int t = 0; t < _brushSelection.brushes.Length; t++)
                            {
                                ControlMeshState meshState = _brushSelection.states[t];

                                if(meshState == null)
                                {
                                    continue;
                                }

                                for(int j = 0, e = 0; j < meshState.edges.Length; e++, j += 2)
                                {
                                    int newControlId = meshState.edgeControlId[e];

                                    if(hotControl == newControlId)
                                    {
                                        hoverControl = newControlId;
                                    }

                                    float distance = meshState.GetClosestEdgeDistance(
                                        cameraPlane, meshState.edges[j + 0], meshState.edges[j + 1]
                                    );

                                    HandleUtility.AddControl(newControlId, distance);
                                }

                                SceneMeshState cameraState = _brushSelection.states[t].GetCameraState(camera, false);

                                if(cameraState == null)
                                {
                                    continue;
                                }

                                for(int p = 0; p < meshState.polygonCenterPoints.Length; p++)
                                {
                                    int newControlId = meshState.polygonControlId[p];

                                    if(hotControl == newControlId)
                                    {
                                        hoverControl = newControlId;
                                    }

                                    if(camera.orthographic || p >= cameraState.polygonCenterPointSizes.Length ||
                                       cameraState.polygonCenterPointSizes[p] <= 0)
                                    {
                                        continue;
                                    }

                                    Vector3 center = meshState.polygonCenterPoints[p];

                                    if(useHandleCenter &&
                                       (handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr)
                                    {
                                        continue;
                                    }

                                    float radius         = cameraState.polygonCenterPointSizes[p] * 1.2f;
                                    float centerDistance = CameraUtility.DistanceToCircle(center, radius);
                                    HandleUtility.AddControl(newControlId, centerDistance);
                                }

                                for(int p = 0; p < meshState.worldPoints.Length; p++)
                                {
                                    int newControlId = meshState.pointControlId[p];

                                    if(hotControl == newControlId)
                                    {
                                        hoverControl = newControlId;
                                    }

                                    Vector3 center = meshState.worldPoints[p];

                                    if((useHandleCenter &&
                                           (handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr) ||
                                       p >= cameraState.worldPointSizes.Length)
                                    {
                                        continue;
                                    }

                                    float radius   = cameraState.worldPointSizes[p] * 1.2f;
                                    float distance = CameraUtility.DistanceToCircle(center, radius);
                                    HandleUtility.AddControl(newControlId, distance);
                                }
                            }

                            try
                            {
                                _brushSelection.FindClosestIntersection(
                                    sceneView, out int closestBrushIndex, out int closestSurfaceIndex
                                );

                                if(closestBrushIndex != -1)
                                {
                                    ControlMeshState meshState    = _brushSelection.states[closestBrushIndex];
                                    int              newControlId = meshState.polygonControlId[closestSurfaceIndex];
                                    HandleUtility.AddControl(newControlId, 5.0f);
                                }
                            }
                            catch
                            {
                            }

                            int nearestControl = HandleUtility.nearestControl;

                            if(nearestControl == _meshEditBrushToolId)
                            {
                                nearestControl = 0; // liar
                            }

                            if(hoverControl != 0)
                            {
                                nearestControl = hoverControl;
                            }
                            else if(hotControl != 0)
                            {
                                nearestControl = 0;
                            }

                            bool doRepaint = false;

                            if(nearestControl == 0)
                            {
                                if(hotControl == 0)
                                {
                                    _brushSelection.BackupSelection();
                                    _brushSelection.UnHoverAll();
                                    doRepaint = _brushSelection.HasSelectionChanged();
                                }

                                _editMode = EditMode.None;
                            }
                            else
                            {
                                EditMode newEditMode = EditMode.None;
                                _brushSelection.BackupSelection();
                                _brushSelection.UnHoverAll();

                                for(int t = 0; t < _brushSelection.brushes.Length; t++)
                                {
                                    ControlMeshState meshState = _brushSelection.states[t];

                                    if(newEditMode == EditMode.None)
                                    {
                                        if(newEditMode == EditMode.None &&
                                           currentTool != Tool.Rotate &&
                                           CSGSettings.SelectionVertex)
                                        {
                                            for(int p = 0; p < meshState.worldPoints.Length; p++)
                                            {
                                                if(meshState.pointControlId[p] != nearestControl)
                                                {
                                                    continue;
                                                }

                                                Vector3 worldPoint = meshState.worldPoints[p];

                                                for(int t2 = 0; t2 < _brushSelection.brushes.Length; t2++)
                                                {
                                                    if(t2 == t)
                                                    {
                                                        continue;
                                                    }

                                                    ControlMeshState meshState2 = _brushSelection.states[t2];

                                                    for(int p2 = 0; p2 < meshState2.worldPoints.Length; p2++)
                                                    {
                                                        Vector3 worldPoint2 = meshState2.worldPoints[p2];

                                                        if((worldPoint - worldPoint2).sqrMagnitude
                                                         < MathConstants.EqualityEpsilonSqr)
                                                        {
                                                            meshState2.selection.points[p2] |= SelectState.Hovering;

                                                            break;
                                                        }
                                                    }
                                                }

                                                newEditMode = HoverOnPoint(meshState, t, p);

                                                break;
                                            }
                                        }

                                        if(newEditMode == EditMode.None && CSGSettings.SelectionSurface)
                                        {
                                            for(int p = 0; p < meshState.polygonCenterPoints.Length; p++)
                                            {
                                                if(meshState.polygonControlId[p] != nearestControl)
                                                {
                                                    continue;
                                                }

                                                newEditMode = HoverOnPolygon(meshState, t, p);

                                                break;
                                            }
                                        }

                                        if(newEditMode == EditMode.None && CSGSettings.SelectionEdge)
                                        {
                                            for(int e = 0; e < meshState.edgeControlId.Length; e++)
                                            {
                                                if(meshState.edgeControlId[e] != nearestControl)
                                                {
                                                    continue;
                                                }

                                                newEditMode = HoverOnEdge(meshState, t, e);

                                                break;
                                            }
                                        }
                                    }
                                }

                                doRepaint = _brushSelection.HasSelectionChanged();
                                _editMode = newEditMode;
                            }

                            if(doRepaint)
                            {
                                ForceLineUpdate();
                                CSGEditorGUIUtility.RepaintAll();
                            }
                        }
                        finally
                        {
                            Handles.matrix = origMatrix;
                        }

                        break;
                    }
                }

                if(currentHotControl == _rectSelectionId)
                {
                    EventType type = Event.current.GetTypeForControl(_rectSelectionId);

                    switch(type)
                    {
                        case EventType.MouseDrag:
                        {
                            if(Event.current.button != 0)
                            {
                                break;
                            }

                            if(!_showMarquee)
                            {
                                if((_startMousePoint - Event.current.mousePosition).magnitude >
                                   MathConstants.MinimumMouseMovement * MathConstants.MinimumMouseMovement)
                                {
                                    _brushSelection.BackupSelection();
                                    _showMarquee = true;
                                }

                                break;
                            }

                            Event.current.Use();

                            if(_brushSelection != null
                            && _brushSelection.states.Length == _brushSelection.brushes.Length)
                            {
                                SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                                SelectMarquee(
                                    camera, CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition)
                                  , selectionType
                                );
                            }

                            CSGEditorGUIUtility.RepaintAll();

                            break;
                        }
                        case EventType.MouseUp:
                        {
                            _movePlaneInNormalDirection = false;
                            _doMarquee                  = false;
                            _showMarquee                = false;

                            _startCamera      = null;
                            CSGGrid.ForceGrid = false;

                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();

                            if(!_mouseIsDragging || !_showMarquee)
                            {
                                break;
                            }

                            if(_brushSelection != null
                            && _brushSelection.states.Length == _brushSelection.brushes.Length)
                            {
                                SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                                SelectMarquee(
                                    camera, CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition)
                                  , selectionType
                                );
                            }

                            break;
                        }
                    }
                }
                else if(currentHotControl == _chamferId)
                {
                    HandleChamfering(sceneView);
                }
                else if(_brushSelection.states != null)
                {
                    for(int t = 0; t < _brushSelection.states.Length; t++)
                    {
                        ControlMeshState meshState = _brushSelection.states[t];

                        if(meshState == null)
                        {
                            continue;
                        }

                        for(int p = 0; p < meshState.worldPoints.Length; p++)
                        {
                            if(currentHotControl == meshState.pointControlId[p])
                            {
                                EventType type = Event.current.GetTypeForControl(meshState.pointControlId[p]);

                                switch(type)
                                {
                                    case EventType.KeyUp:
                                    {
                                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed())
                                        {
                                            Event.current.Use();
                                            DeleteSelectedPoints();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed())
                                        {
                                            MergeSelected();
                                            Event.current.Use();
                                        }

                                        break;
                                    }
                                    case EventType.KeyDown:
                                    {
                                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed())
                                        {
                                            Event.current.Use();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed() && HaveEdgeSelection)
                                        {
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.ValidateCommand:
                                    {
                                        if((Event.current.commandName == "SoftDelete"
                                            || Event.current.commandName == "Delete") && HavePointSelection)
                                        {
                                            Event.current.Use();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed())
                                        {
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.ExecuteCommand:
                                    {
                                        if(HavePointSelection &&
                                           (Event.current.commandName == "SoftDelete"
                                            || Event.current.commandName == "Delete"))
                                        {
                                            DeleteSelectedPoints();
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.MouseDrag:
                                    {
                                        if(Event.current.button != 0)
                                        {
                                            break;
                                        }

                                        if(Tools.current == Tool.Scale)
                                        {
                                            break;
                                        }

                                        Event.current.Use();

                                        if(_firstMove)
                                        {
                                            _extraDeltaMovement = MathConstants.zeroVector3;
                                            _worldDeltaMovement = MathConstants.zeroVector3;
                                            _startCamera        = camera;
                                            UpdateTransformMatrices();
                                            UpdateSelection(false);
                                        }

                                        if(_firstMove)
                                        {
                                            if(_firstMove)
                                            {
                                                _originalPoint = meshState.worldPoints[p];
                                            }

                                            UpdateWorkControlMesh();
                                            UpdateBackupPoints();
                                            UpdateGrid(_startCamera);
                                            _firstMove          =  true;
                                            _extraDeltaMovement += _worldDeltaMovement;
                                        }

                                        bool lockX = CSGSettings.LockAxisX
                                         || Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon;

                                        bool lockY = CSGSettings.LockAxisY
                                         || Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon;

                                        bool lockZ = CSGSettings.LockAxisZ
                                         || Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon;

                                        if(CSGSettings.SnapVector == MathConstants.zeroVector3)
                                        {
                                            EditModeManager.ShowMessage(
                                                "Positional snapping is set to zero, cannot move."
                                            );

                                            break;
                                        }

                                        if(lockX && lockY && lockZ)
                                        {
                                            EditModeManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");

                                            break;
                                        }

                                        EditModeManager.ResetMessage();

                                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                                        Vector3 intersection = _movePlane.RayIntersection(mouseRay);

                                        if(float.IsNaN(intersection.x) || float.IsNaN(intersection.y)
                                        || float.IsNaN(intersection.z))
                                        {
                                            break;
                                        }

                                        intersection = GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);

                                        if(_firstMove)
                                        {
                                            _originalPoint      = intersection;
                                            _worldDeltaMovement = MathConstants.zeroVector3;
                                            _firstMove          = false;
                                        }
                                        else
                                        {
                                            _worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
                                        }

                                        // try to snap selected points against non-selected points
                                        SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                                        switch(activeSnappingMode)
                                        {
                                            case SnapMode.GridSnapping:
                                            {
                                                Vector3[] worldPoints = _brushSelection.GetSelectedWorldPoints(true);

                                                _worldDeltaMovement = CSGGrid.SnapDeltaToGrid(
                                                    camera, _worldDeltaMovement, worldPoints, snapToSelf: true
                                                );

                                                break;
                                            }
                                            case SnapMode.RelativeSnapping:
                                            {
                                                _worldDeltaMovement = CSGGrid.SnapDeltaRelative(
                                                    camera, _worldDeltaMovement
                                                );

                                                break;
                                            }
                                            default:
                                            case SnapMode.None:
                                            {
                                                _worldDeltaMovement = CSGGrid.HandleLockedAxi(_worldDeltaMovement);

                                                break;
                                            }
                                        }

                                        DoMoveControlPoints(_worldDeltaMovement);
                                        CenterPositionHandle();
                                        CSGEditorGUIUtility.RepaintAll();

                                        break;
                                    }
                                    case EventType.MouseUp:
                                    {
                                        _movePlaneInNormalDirection = false;

                                        _startCamera      = null;
                                        CSGGrid.ForceGrid = false;

                                        GUIUtility.hotControl             = 0;
                                        GUIUtility.keyboardControl        = 0;
                                        EditorGUIUtility.editingTextField = false;
                                        Event.current.Use();

                                        if(!_mouseIsDragging)
                                        {
                                            break;
                                        }

                                        MergeDuplicatePoints();

                                        if(!UpdateWorkControlMesh())
                                        {
                                            Undo.PerformUndo();
                                        }
                                        else
                                        {
                                            UpdateBackupPoints();
                                        }

                                        break;
                                    }
                                    case EventType.Repaint:
                                    {
                                        if(Tools.current == Tool.Scale)
                                        {
                                            break;
                                        }

                                        if(_editMode != EditMode.ScalePolygon)
                                        {
                                            RenderOffsetText();
                                        }

                                        break;
                                    }
                                }

                                break;
                            }
                        }

                        for(int e = 0; e < meshState.edges.Length / 2; e++)
                        {
                            if(currentHotControl == meshState.edgeControlId[e])
                            {
                                EventType type = Event.current.GetTypeForControl(meshState.edgeControlId[e]);

                                switch(type)
                                {
                                    case EventType.KeyDown:
                                    {
                                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed())
                                        {
                                            Event.current.Use();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed() && HaveEdgeSelection)
                                        {
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.ValidateCommand:
                                    {
                                        if((Event.current.commandName == "SoftDelete"
                                            || Event.current.commandName == "Delete") && HavePointSelection)
                                        {
                                            Event.current.Use();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed())
                                        {
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.KeyUp:
                                    {
                                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed())
                                        {
                                            Event.current.Use();
                                            DeleteSelectedPoints();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed())
                                        {
                                            MergeSelected();
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.ExecuteCommand:
                                    {
                                        if(HavePointSelection &&
                                           (Event.current.commandName == "SoftDelete"
                                            || Event.current.commandName == "Delete"))
                                        {
                                            DeleteSelectedPoints();
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.MouseDrag:
                                    {
                                        if(Event.current.button != 0)
                                        {
                                            break;
                                        }

                                        Event.current.Use();

                                        if(_editMode == EditMode.RotateEdge)
                                        {
                                            if(_rotateBrushParent == null)
                                            {
                                                break;
                                            }

                                            if(CSGSettings.SnapRotation % 360 <= 0)
                                            {
                                                EditModeManager.ShowMessage(
                                                    "Rotational snapping is set to zero, cannot rotate."
                                                );

                                                break;
                                            }

                                            EditModeManager.ResetMessage();

                                            Ray     ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                                            Vector3 newMousePos = _rotatePlane.RayIntersection(ray);

                                            _rotateCurrentAngle = GeometryUtility.SignedAngle(
                                                _rotateCenter - _rotateStart, _rotateCenter - newMousePos, _rotateNormal
                                            );

                                            _rotateCurrentSnappedAngle =
                                                GridUtility.SnappedAngle(_rotateCurrentAngle - _rotateStartAngle)
                                              + _rotateStartAngle;

                                            DoRotateBrushes(
                                                _rotateCenter, _rotateNormal
                                              , _rotateCurrentSnappedAngle - _rotateStartAngle
                                            );

                                            CSGEditorGUIUtility.RepaintAll();

                                            break;
                                        }

                                        if(Tools.current != Tool.Move &&
                                           Tools.current != Tool.Scale)
                                        {
                                            break;
                                        }

                                        if(_firstMove)
                                        {
                                            _extraDeltaMovement = MathConstants.zeroVector3;
                                            _worldDeltaMovement = MathConstants.zeroVector3;
                                            _startCamera        = camera;
                                            UpdateTransformMatrices();
                                            UpdateSelection(false);
                                        }

                                        if(_firstMove)
                                        {
                                            int     originalVertexIndex1 = meshState.edges[e * 2 + 0];
                                            int     originalVertexIndex2 = meshState.edges[e * 2 + 1];
                                            Vector3 originalVertex1      = meshState.worldPoints[originalVertexIndex1];
                                            Vector3 originalVertex2      = meshState.worldPoints[originalVertexIndex2];

                                            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                                            _originalPoint = MathUtils.ClosestPtSegmentRay(
                                                originalVertex1, originalVertex2, ray, out float _, out float s
                                              , out Vector3 _
                                            );

                                            UpdateWorkControlMesh();
                                            UpdateBackupPoints();
                                            UpdateGrid(_startCamera);
                                            _firstMove          =  true;
                                            _extraDeltaMovement += _worldDeltaMovement;
                                        }

                                        bool lockX = CSGSettings.LockAxisX
                                         || Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon;

                                        bool lockY = CSGSettings.LockAxisY
                                         || Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon;

                                        bool lockZ = CSGSettings.LockAxisZ
                                         || Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon;

                                        if(CSGSettings.SnapVector == MathConstants.zeroVector3)
                                        {
                                            EditModeManager.ShowMessage(
                                                "Positional snapping is set to zero, cannot move."
                                            );

                                            break;
                                        }

                                        if(lockX && lockY && lockZ)
                                        {
                                            EditModeManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");

                                            break;
                                        }

                                        EditModeManager.ResetMessage();

                                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                                        Vector3 intersection = _movePlane.RayIntersection(mouseRay);

                                        if(float.IsNaN(intersection.x) || float.IsNaN(intersection.y)
                                        || float.IsNaN(intersection.z))
                                        {
                                            break;
                                        }

                                        intersection = GeometryUtility.ProjectPointOnPlane(_movePlane, intersection);

                                        if(_firstMove)
                                        {
                                            _originalPoint      = intersection;
                                            _worldDeltaMovement = MathConstants.zeroVector3;

                                            handleWorldPoints = _brushSelection.GetSelectedWorldPoints(true);
                                            dragEdgeScale     = Vector3.one;
                                            dragEdgeRotation  = GetRealHandleRotation();

                                            Quaternion rotation        = dragEdgeRotation;
                                            Quaternion inverseRotation = Quaternion.Inverse(rotation);

                                            Vector3 delta    = _originalPoint - handleCenter;
                                            float   distance = delta.magnitude;
                                            startHandleDirection = delta / distance;

                                            startHandleDirection =
                                                GeometryUtility.SnapToClosestAxis(
                                                    inverseRotation * startHandleDirection
                                                );

                                            startHandleDirection = rotation * startHandleDirection;

                                            startHandleCenter = handleCenter;

                                            _firstMove = false;
                                        }
                                        else
                                        {
                                            _worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
                                        }

                                        // try to snap selected points against non-selected points
                                        SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                                        switch(activeSnappingMode)
                                        {
                                            case SnapMode.GridSnapping:
                                            {
                                                Vector3[] worldPoints = handleWorldPoints;

                                                _worldDeltaMovement = CSGGrid.SnapDeltaToGrid(
                                                    camera, _worldDeltaMovement, worldPoints, snapToSelf: true
                                                );

                                                break;
                                            }
                                            case SnapMode.RelativeSnapping:
                                            {
                                                _worldDeltaMovement = CSGGrid.SnapDeltaRelative(
                                                    camera, _worldDeltaMovement
                                                );

                                                break;
                                            }
                                            default:
                                            case SnapMode.None:
                                            {
                                                _worldDeltaMovement = CSGGrid.HandleLockedAxi(_worldDeltaMovement);

                                                break;
                                            }
                                        }

                                        if(Tools.current == Tool.Move)
                                        {
                                            DoMoveControlPoints(_worldDeltaMovement);
                                        }

                                        if(Tools.current == Tool.Scale)
                                        {
                                            Quaternion rotation        = dragEdgeRotation;
                                            Quaternion inverseRotation = Quaternion.Inverse(rotation);

                                            Vector3 start = GeometryUtility.ProjectPointOnInfiniteLine(
                                                _originalPoint, startHandleCenter, startHandleDirection
                                            );

                                            Vector3 end = GeometryUtility.ProjectPointOnInfiniteLine(
                                                intersection, startHandleCenter, startHandleDirection
                                            );

                                            Vector3 oldDistance = inverseRotation * (start - startHandleCenter);
                                            Vector3 newDistance = inverseRotation * (end - startHandleCenter);

                                            if(Mathf.Abs(oldDistance.x) > MathConstants.DistanceEpsilon)
                                            {
                                                dragEdgeScale.x = newDistance.x / oldDistance.x;
                                            }

                                            if(Mathf.Abs(oldDistance.y) > MathConstants.DistanceEpsilon)
                                            {
                                                dragEdgeScale.y = newDistance.y / oldDistance.y;
                                            }

                                            if(Mathf.Abs(oldDistance.z) > MathConstants.DistanceEpsilon)
                                            {
                                                dragEdgeScale.z = newDistance.z / oldDistance.z;
                                            }

                                            if(float.IsNaN(dragEdgeScale.x) || float.IsInfinity(dragEdgeScale.x))
                                            {
                                                dragEdgeScale.x = 1.0f;
                                            }

                                            if(float.IsNaN(dragEdgeScale.y) || float.IsInfinity(dragEdgeScale.y))
                                            {
                                                dragEdgeScale.y = 1.0f;
                                            }

                                            if(float.IsNaN(dragEdgeScale.z) || float.IsInfinity(dragEdgeScale.z))
                                            {
                                                dragEdgeScale.z = 1.0f;
                                            }

                                            dragEdgeScale.x = Mathf.Round(dragEdgeScale.x / CSGSettings.SnapScale)
                                              * CSGSettings.SnapScale;

                                            dragEdgeScale.y = Mathf.Round(dragEdgeScale.y / CSGSettings.SnapScale)
                                              * CSGSettings.SnapScale;

                                            dragEdgeScale.z = Mathf.Round(dragEdgeScale.z / CSGSettings.SnapScale)
                                              * CSGSettings.SnapScale;

                                            DoScaleControlPoints(rotation, dragEdgeScale, startHandleCenter);
                                        }

                                        CenterPositionHandle();
                                        CSGEditorGUIUtility.RepaintAll();

                                        break;
                                    }
                                    case EventType.MouseUp:
                                    {
                                        _movePlaneInNormalDirection = false;

                                        _startCamera      = null;
                                        CSGGrid.ForceGrid = false;
                                        EditorGUIUtility.SetWantsMouseJumping(0);
                                        GUIUtility.hotControl             = 0;
                                        GUIUtility.keyboardControl        = 0;
                                        EditorGUIUtility.editingTextField = false;
                                        Event.current.Use();

                                        if(!_mouseIsDragging)
                                        {
                                            break;
                                        }

                                        if(_editMode == EditMode.RotateEdge)
                                        {
                                            Undo.CollapseUndoOperations(_rotationUndoGroupIndex);
                                            UpdateWorkControlMesh();
                                        }

                                        MergeDuplicatePoints();

                                        if(!UpdateWorkControlMesh(true))
                                        {
                                            Undo.PerformUndo();
                                        }
                                        else
                                        {
                                            UpdateBackupPoints();
                                        }

                                        InternalCSGModelManager.CheckSurfaceModifications(_brushSelection.brushes);

                                        break;
                                    }
                                    case EventType.Repaint:
                                    {
                                        if(Tools.current == Tool.Move)
                                        {
                                            if(_editMode != EditMode.RotateEdge)
                                            {
                                                RenderOffsetText();
                                            }
                                        }
                                        else if(Tools.current == Tool.Scale)
                                        {
                                            if(handleWorldPoints == null)
                                            {
                                                return;
                                            }

                                            Vector3 realScale = dragEdgeScale;

                                            if(realScale.x < 0)
                                            {
                                                realScale.x = 0;
                                            }

                                            if(realScale.y < 0)
                                            {
                                                realScale.y = 0;
                                            }

                                            if(realScale.z < 0)
                                            {
                                                realScale.z = 0;
                                            }

                                            DrawScaleBounds(
                                                camera, dragEdgeRotation, realScale, startHandleCenter
                                              , handleWorldPoints
                                            );
                                        }

                                        break;
                                    }
                                }

                                break;
                            }
                        }

                        for(int p = 0; p < meshState.polygonCenterPoints.Length; p++)
                        {
                            if(currentHotControl == meshState.polygonControlId[p])
                            {
                                EventType type = Event.current.GetTypeForControl(meshState.polygonControlId[p]);

                                switch(type)
                                {
                                    case EventType.KeyDown:
                                    {
                                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed())
                                        {
                                            Event.current.Use();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed() && HaveEdgeSelection)
                                        {
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.ValidateCommand:
                                    {
                                        if((Event.current.commandName == "SoftDelete"
                                            || Event.current.commandName == "Delete") && HavePointSelection)
                                        {
                                            Event.current.Use();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed())
                                        {
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.KeyUp:
                                    {
                                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed())
                                        {
                                            Event.current.Use();
                                            DeleteSelectedPoints();

                                            break;
                                        }

                                        if(Keys.mergeEdgePoints.IsKeyPressed())
                                        {
                                            MergeSelected();
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.ExecuteCommand:
                                    {
                                        if(HavePointSelection &&
                                           (Event.current.commandName == "SoftDelete"
                                            || Event.current.commandName == "Delete"))
                                        {
                                            DeleteSelectedPoints();
                                            Event.current.Use();
                                        }

                                        break;
                                    }

                                    case EventType.MouseDrag:
                                    {
                                        if(Event.current.button != 0)
                                        {
                                            break;
                                        }

                                        Event.current.Use();

                                        if(_firstMove)
                                        {
                                            EditorGUIUtility.SetWantsMouseJumping(1);
                                            _mousePosition      = Event.current.mousePosition;
                                            _extraDeltaMovement = MathConstants.zeroVector3;
                                            _worldDeltaMovement = MathConstants.zeroVector3;
                                            _startCamera        = camera;
                                            UpdateTransformMatrices();
                                            UpdateSelection(false);
                                        }
                                        else
                                        {
                                            _mousePosition += Event.current.delta;
                                        }

                                        if(_firstMove)
                                        {
                                            if(_firstMove)
                                            {
                                                _originalPoint = meshState.polygonCenterPoints[p];
                                            }

                                            UpdateBackupPoints();
                                            UpdateGrid(_startCamera);
                                            _firstMove          =  true;
                                            _extraDeltaMovement += _worldDeltaMovement;
                                        }

                                        bool lockX = CSGSettings.LockAxisX
                                         || Mathf.Abs(_movePlane.a) >= 1 - MathConstants.EqualityEpsilon;

                                        bool lockY = CSGSettings.LockAxisY
                                         || Mathf.Abs(_movePlane.b) >= 1 - MathConstants.EqualityEpsilon;

                                        bool lockZ = CSGSettings.LockAxisZ
                                         || Mathf.Abs(_movePlane.c) >= 1 - MathConstants.EqualityEpsilon;

                                        if(CSGSettings.SnapVector == MathConstants.zeroVector3)
                                        {
                                            EditModeManager.ShowMessage(
                                                "Positional snapping is set to zero, cannot move."
                                            );

                                            break;
                                        }

                                        if(lockX && lockY && lockZ)
                                        {
                                            EditModeManager.ShowMessage("All axi are disabled (X Y Z), cannot move.");

                                            break;
                                        }

                                        EditModeManager.ResetMessage();

                                        Ray     mouseRay     = HandleUtility.GUIPointToWorldRay(_mousePosition);
                                        Vector3 intersection = _movePlane.RayIntersection(mouseRay);

                                        if(float.IsNaN(intersection.x) || float.IsNaN(intersection.y)
                                        || float.IsNaN(intersection.z))
                                        {
                                            break;
                                        }

                                        if(_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
                                        {
                                            intersection = GridUtility.CleanPosition(intersection);

                                            intersection = GeometryUtility.ProjectPointOnInfiniteLine(
                                                intersection, _movePolygonOrigin, _movePolygonDirection
                                            );
                                        }
                                        else
                                        {
                                            intersection = GeometryUtility.ProjectPointOnPlane(
                                                _movePlane, intersection
                                            );
                                        }

                                        if(_firstMove)
                                        {
                                            _originalPoint      = intersection;
                                            _worldDeltaMovement = MathConstants.zeroVector3;
                                            _firstMove          = false;
                                        }
                                        else
                                        {
                                            _worldDeltaMovement = SnapMovementToPlane(intersection - _originalPoint);
                                        }

                                        // try to snap selected points against non-selected points
                                        SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

                                        switch(activeSnappingMode)
                                        {
                                            case SnapMode.GridSnapping:
                                            {
                                                Vector3[] worldPoints = _brushSelection.GetSelectedWorldPoints(true);

                                                if(_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
                                                {
                                                    Vector3 worldLineOrg = _movePolygonOrigin;
                                                    Vector3 worldLineDir = _movePolygonDirection;

                                                    _worldDeltaMovement = CSGGrid.SnapDeltaToRayGrid(
                                                        camera, new Ray(worldLineOrg, worldLineDir), _worldDeltaMovement
                                                      , worldPoints
                                                    );
                                                }
                                                else
                                                {
                                                    _worldDeltaMovement = CSGGrid.SnapDeltaToGrid(
                                                        camera, _worldDeltaMovement, worldPoints, snapToSelf: true
                                                    );
                                                }

                                                break;
                                            }
                                            case SnapMode.RelativeSnapping:
                                            {
                                                if(_movePlaneInNormalDirection && _editMode != EditMode.ScalePolygon)
                                                {
                                                    Vector3 worldLineOrg = _movePolygonOrigin;
                                                    Vector3 worldLineDir = _movePolygonDirection;

                                                    _worldDeltaMovement = CSGGrid.SnapDeltaToRayRelative(
                                                        camera, new Ray(worldLineOrg, worldLineDir), _worldDeltaMovement
                                                    );
                                                }
                                                else
                                                {
                                                    _worldDeltaMovement = CSGGrid.SnapDeltaRelative(
                                                        camera, _worldDeltaMovement
                                                    );
                                                }

                                                break;
                                            }
                                            default:
                                            case SnapMode.None:
                                            {
                                                _worldDeltaMovement = CSGGrid.HandleLockedAxi(_worldDeltaMovement);

                                                break;
                                            }
                                        }

                                        switch(_editMode)
                                        {
                                            case EditMode.MovingPolygon:
                                                DoMoveControlPoints(_worldDeltaMovement); break;
                                        }

                                        CenterPositionHandle();
                                        CSGEditorGUIUtility.RepaintAll();

                                        break;
                                    }
                                    case EventType.MouseUp:
                                    {
                                        _movePlaneInNormalDirection = false;

                                        _startCamera      = null;
                                        CSGGrid.ForceGrid = false;

                                        EditorGUIUtility.SetWantsMouseJumping(0);
                                        GUIUtility.hotControl             = 0;
                                        GUIUtility.keyboardControl        = 0;
                                        EditorGUIUtility.editingTextField = false;
                                        Event.current.Use();

                                        if(!_mouseIsDragging)
                                        {
                                            break;
                                        }

                                        MergeDuplicatePoints();

                                        if(!UpdateWorkControlMesh())
                                        {
                                            Undo.PerformUndo();
                                        }
                                        else
                                        {
                                            if(_editMode == EditMode.ScalePolygon)
                                            {
                                                _brushSelection.brushes = null;
                                                SetTargets(EditModeManager.FilteredSelection);
                                            }
                                            else
                                            {
                                                UpdateBackupPoints();
                                            }
                                        }

                                        break;
                                    }
                                    case EventType.Repaint:
                                    {
                                        if(_editMode != EditMode.ScalePolygon)
                                        {
                                            RenderOffsetText();
                                        }

                                        break;
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }
            finally
            {
                if(originalEventType == EventType.MouseUp)
                {
                    _mouseIsDragging = false;
                }
            }
        }

        private static Vector2 m_ScrollPos;

        public void OnInspectorGUI(EditorWindow window, float height)
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            {
                EditModeMeshModeGUI.OnInspectorGUI();
            }

            EditorGUILayout.EndScrollView();
        }

        public Rect GetLastSceneGUIRect()
        {
            return EditModeMeshModeGUI.GetLastSceneGUIRect();
        }

        public bool OnSceneGUI(Rect windowRect)
        {
            if(_brushSelection.brushes == null)
            {
                return false;
            }

            EditModeMeshModeGUI.OnSceneGUI(this);

            return true;
        }
    }
}

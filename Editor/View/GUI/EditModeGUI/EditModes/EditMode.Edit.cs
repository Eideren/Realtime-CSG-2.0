using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
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
        };

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

        [SerializeField] private bool _useHandleCenter; //= false;
        [SerializeField] private Vector3 _handleCenter;
        [SerializeField] private Vector3 _startHandleCenter;
        [SerializeField] private Vector3 _startHandleDirection;
        [SerializeField] private Vector3 _handleScale = Vector3.one;
        [SerializeField] private Vector3 _dragEdgeScale = Vector3.one;
        [SerializeField] private Quaternion _dragEdgeRotation;
        [SerializeField] private Vector3[] _handleWorldPoints; //= null;

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

        [SerializeField] private readonly TransformSelection _transformSelection = new TransformSelection();
        [SerializeField] private readonly BrushSelection _brushSelection = new BrushSelection();

        [SerializeField] private UnityEngine.Object[] _undoAbleTransforms = new UnityEngine.Object[0];
        [SerializeField] private UnityEngine.Object[] _undoAbleBrushes = new UnityEngine.Object[0];

        [SerializeField] private SpaceMatrices _activeSpaceMatrices = new SpaceMatrices();

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
            List<Object> transformsAsObjects = _transformSelection.transforms.ToList<UnityEngine.Object>();
            transformsAsObjects.Add(this);
            _undoAbleTransforms = transformsAsObjects.ToArray();

            List<Object> brushesAsObjects = _brushSelection.brushes.ToList<UnityEngine.Object>();
            brushesAsObjects.Add(this);
            _undoAbleBrushes = brushesAsObjects.ToArray();

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
            //UpdateTransformMatrices();
            //UpdateSelection(allowSubstraction: false);
            UpdateWorkControlMesh();
            //UpdateBackupPoints();
            _brushSelection.UndoRedoPerformed();

            CenterPositionHandle();
            ForceLineUpdate();
            CSG_EditorGUIUtility.RepaintAll();

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
                Frustum frustum = CameraUtility.GetCameraSubFrustumGUI(camera, rect);

                //var isOrthoCamera		= camera.orthographic;
                bool ignoreHiddenPoints =
                    CSGSettings
                       .HiddenSurfacesNotSelectable; // && (!isOrthoCamera || !CSGSettings.HiddenSurfacesOrthoSelectable);

                PointSelection[] selectedPoints = SceneQueryUtility.GetPointsInFrustum(
                    camera, frustum.Planes, _brushSelection.brushes, _brushSelection.states, ignoreHiddenPoints
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
                    CSG_EditorGUIUtility.RepaintAll();

                    return true;
                }

                if(!ControlMeshState.DeselectAll(_brushSelection.states))
                {
                    Selection.activeTransform = null;

                    return false;
                }

                ForceLineUpdate();
                CSG_EditorGUIUtility.RepaintAll();

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
            //hoverMeshState.SelectAll();
            hoverMeshState.selection.polygons[polygonIndex] |= SelectState.Selected;
            //hoverMeshState.SelectPolygon(polygonIndex, SelectionType.Replace, onlyOnHover: false);

            UpdateWorkControlMesh();
            UpdateBackupPoints();
            UpdateMeshStates();
            CenterPositionHandle();
            CSG_EditorGUIUtility.RepaintAll();
        }

        private bool UpdateSelection(bool allowSubstraction = true)
        {
            try
            {
                if(_hoverOnTarget <= -1 ||
                   _hoverOnTarget >= _brushSelection.states.Length)
                {
                    return false;
                }

                ControlMeshState hoverMeshState = _brushSelection.states[_hoverOnTarget];
                SelectionType    selectionType  = SelectionUtility.GetEventSelectionType();

                if(allowSubstraction == false)
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
                    needRepaint = hoverMeshState.SelectPolygon(p, selectionType, true) || needRepaint;
                }

                for(int e = 0; e < hoverMeshState.selection.edges.Length; e++)
                {
                    needRepaint = hoverMeshState.SelectEdge(e, selectionType, true) || needRepaint;
                }

                for(int b = 0; b < _brushSelection.states.Length; b++)
                {
                    ControlMeshState curMeshState = _brushSelection.states[b];

                    for(int p = 0; p < curMeshState.worldPoints.Length; p++)
                    {
                        needRepaint = curMeshState.SelectPoint(p, selectionType, true) || needRepaint;
                    }
                }
                //_brushSelection.DebugLogData();

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
            if(GUIUtility.hotControl == _rectSelectionId &&
               !_movePlaneInNormalDirection &&
               GUIUtility.hotControl != 0)
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
                case EditMode.RotateEdge:    _hoverOnEdgeIndex    = index; break;
                case EditMode.MovingEdge:    _hoverOnEdgeIndex    = index; break;
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
            /*
            var brush = meshState.BrushTransform.GetComponent<CSGBrush>();
            var controlMesh = brush.ControlMesh;
            var polygon = controlMesh.Polygons[polygonIndex];
            var polygonVertices = new Vector3[meshState.PolygonPointIndices[polygonIndex].Length];
            for (int i=0;i<meshState.PolygonPointIndices[polygonIndex].Length;i++)
            {
                var pointIndex = meshState.PolygonPointIndices[polygonIndex][i];
                polygonVertices[i] = meshState.WorldPoints[pointIndex];
            }
            var calcPlane = GeometryUtility.CalcPolygonPlane(polygonVertices);
            Vector3 tangent, binormal;
            GeometryUtility.CalculateTangents(calcPlane.normal, out tangent, out binormal);
            var texGenIndex = polygon.TexGenIndex;
            var currPlane=  brush.Shape.Surfaces[texGenIndex];
            */

            _currentCursor = CursorUtility.GetCursorForDirection(delta, 0);

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
                    Undo.RecordObjects(_undoAbleBrushes, "Snap points to grid");
                    _brushSelection.PointSnapToGrid(camera);
                    UpdateWorkControlMesh(true);
                }
                else
                {
                    Undo.IncrementCurrentGroup();
                    int groupIndex = Undo.GetCurrentGroup();
                    Undo.RecordObjects(_undoAbleTransforms, "Snap brushes to grid");
                    Undo.RecordObjects(_undoAbleBrushes, "Snap points to grid");
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
//			var brushLocalToWorld = targetMeshState.BrushTransform.localToWorldMatrix;

            //var polygonPlane = targetMeshState.PolygonCenterPlanes[_hoverOnPolygonIndex];
            //polygonPlane.Transform(brushLocalToWorld);

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
                Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merging vertices");
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
                Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");

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
                Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");

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
                Undo.RecordObjects(_undoAbleTransforms, "Transform brushes");
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
                Undo.RecordObjects(_undoAbleBrushes, "Move control-points");
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
                Undo.RecordObjects(_undoAbleBrushes, "Rotate control-points");
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
                Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Scale control-points");
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
                Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Delete control-points");
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
                Undo.RegisterCompleteObjectUndo(_undoAbleBrushes, "Merge edge-points");
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
            _activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);
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

                    _rotateCenter = _rotateCenter / meshState.worldPoints.Length;

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
            public readonly BrushOutlineRenderer BrushOutlineRenderer = new BrushOutlineRenderer();
            public int LastLineMeshGeneration = -1;
            public int LastHandleGeneration = -1;

            internal void Destroy()
            {
                BrushOutlineRenderer.Destroy();
            }
        }

        private readonly PointMeshManager pointMeshManager = new PointMeshManager();

        private readonly Dictionary<Camera, BrushOutlineInfo> _brushOutlineInfos =
            new Dictionary<Camera, BrushOutlineInfo>();

        internal BrushOutlineInfo GetBrushOutLineInfo(Camera camera)
        {
            BrushOutlineInfo brushOutlineInfo;

            if(_brushOutlineInfos.TryGetValue(camera, out brushOutlineInfo))
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
            pointMeshManager.Destroy();
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

                brushOutlineInfo.Value.LastLineMeshGeneration = currentMeshGeneration - 1000;
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

            BrushOutlineRenderer brushOutlineRenderer = brushOutlineInfo.BrushOutlineRenderer;

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

                brushOutlineInfo.LastLineMeshGeneration = currentMeshGeneration - 1000;
                _brushSelection.UpdateParentModelTransforms();

                break;
            }

            if(brushOutlineInfo.LastLineMeshGeneration == currentMeshGeneration)
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

            brushOutlineInfo.LastLineMeshGeneration = currentMeshGeneration;

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
            Vector2 centerDelta = brushCenter2D - vertex2d; //textCenter2D;

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
            Vector2 centerDelta = brushCenter2D - vertex2dA; //textCenter2D;

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
                if(_mouseIsDragging && _nonZeroMouseIsDown)
                {
                    return false;
                }

                return true;
            }

            if(_mouseIsDragging || _nonZeroMouseIsDown)
            {
                return false;
            }

            return true;
        }

        private bool ShouldDrawPolygonPoints(Camera camera)
        {
            if(_showMarquee || !camera || camera.orthographic)
            {
                return false;
            }

            if(_editMode == EditMode.MovingPolygon)
            {
                if(_mouseIsDragging && _nonZeroMouseIsDown)
                {
                    return false;
                }

                return true;
            }

            if(_mouseIsDragging || _nonZeroMouseIsDown)
            {
                return false;
            }

            return true;
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

                    EditorGUIUtility.AddCursorRect(windowRect, _currentCursor);
                }
            }

            Matrix4x4 origMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            Tool currentTool = Tools.current;

            {
                brushOutlineInfo.BrushOutlineRenderer.RenderOutlines();

                if(ShouldDrawAnyPoints(camera))
                {
                    pointMeshManager.Begin();

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

                            SceneMeshState CameraState = _brushSelection.states[t].GetCameraState(camera, false);

                            if(CameraState == null)
                            {
                                continue;
                            }

                            pointMeshManager.DrawPoints(
                                camera, meshState.worldPoints,
                                CameraState.worldPointSizes,
                                CameraState.worldPointColors
                            );
                            //PaintUtility.DrawDoubleDots(meshState.WorldPoints, 
                            //							cameraState.WorldPointSizes, 
                            //							cameraState.WorldPointColors, 
                            //							meshState.WorldPoints.Length);
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

                            pointMeshManager.DrawPoints(
                                camera, meshState.polygonCenterPoints,
                                cameraState.polygonCenterPointSizes,
                                meshState.polygonCenterColors
                            );
                            //PaintUtility.DrawDoubleDots(meshState.PolygonCenterPoints, 
                            //							cameraState.PolygonCenterPointSizes, 
                            //							meshState.PolygonCenterColors, 
                            //							meshState.PolygonCenterPoints.Length);
                        }
                    }

                    pointMeshManager.End();
                    pointMeshManager.Render(PaintUtility.CustomDotMaterial, PaintUtility.CustomSurfaceNoDepthMaterial);
                }

                if(currentTool == Tool.Rotate && _editMode == EditMode.RotateEdge)
                {
                    //if (rotateBrushParent != null)
                    {
                        if(_mouseIsDragging)
                        {
                            PaintUtility.DrawRotateCircle(
                                camera, _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0
                              , _rotateStartAngle, _rotateCurrentSnappedAngle,
                                ColorSettings.RotateCircleOutline
                            );

                            PaintUtility.DrawRotateCirclePie(
                                _rotateCenter, _rotateNormal, _rotateTangent, _rotateRadius, 0, _rotateStartAngle
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

                            if(Tools.pivotRotation == PivotRotation.Local)
                            {
                                PaintUtility.DrawScreenText(textCenter2D, "Normal Aligned Extrude [local]");
                            }
                            else
                            {
                                PaintUtility.DrawScreenText(textCenter2D, "Axis Aligned Extrude [global]");
                            }
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
                if(_activeSpaceMatrices == null)
                {
                    _activeSpaceMatrices = SpaceMatrices.Create(Selection.activeTransform);
                }

                delta = GridUtility.CleanPosition(
                    _activeSpaceMatrices.activeLocalToWorld.MultiplyVector(delta).normalized
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
                _useHandleCenter = false;

                return;
            }

            AABB bounds = _brushSelection.GetSelectionBounds();

            if(!bounds.Valid)
            {
                _useHandleCenter = false;

                return;
            }

            _handleCenter    = bounds.Center;
            _handleScale     = Vector3.one;
            _useHandleCenter = true;
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
            PaintUtility.DrawScreenText(camera, _handleCenter, HoverTextDistance * 3, text);

            AABB      bounds         = BoundsUtilities.GetBounds(worldPoints, rotation, scale, center);
            Vector3[] outputVertices = new Vector3[8];
            BoundsUtilities.GetBoundsCornerPoints(bounds, outputVertices);

            PaintUtility.RenderBoundsSizes(
                Quaternion.Inverse(rotation), rotation, camera, outputVertices, Color.white, Color.white, Color.white
              , true, true, true
            );
        }

        private CSGPlane[] chamferPlanes = new CSGPlane[2];
        private Vector3[] chamferEdgePoints = new Vector3[2];
        private Vector3 chamferDirection = Vector3.zero;
        private Vector3 chamferIntersectionPoint = Vector3.zero;
        private bool[] chamferBrushModified = new bool[0];
        private Shape[] chamferShapes = new Shape[0];
        private ControlMesh[] chamferControlMesh = new ControlMesh[0];
        private ControlMeshSelection[] chamferSelection = new ControlMeshSelection[0];
        private Vector3 chamferVertex = Vector3.zero;
        private ChamferMode chamferMode = ChamferMode.Point;

        private enum ChamferMode
        {
            Point
          , Edge
        }

        //Vector3[]	chamferTangents		= new Vector3[2];
        //Vector3[]	chamferLines		= new Vector3[0];

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
                chamferMode = ChamferMode.Edge;
                int   halfEdgEIndex = hoverMeshState.edgeStateToHalfEdge[_hoverOnEdgeIndex];
                int   twinIndex     = controlMesh.Edges[halfEdgEIndex].TwinIndex;
                short polygonIndex1 = controlMesh.Edges[halfEdgEIndex].PolygonIndex;
                short polygonIndex2 = controlMesh.Edges[twinIndex].PolygonIndex;
                int   texGenIndex1  = controlMesh.Polygons[polygonIndex1].TexGenIndex;
                int   texGenIndex2  = controlMesh.Polygons[polygonIndex2].TexGenIndex;
                chamferPlanes    = new CSGPlane[2];
                chamferPlanes[0] = shape.Surfaces[texGenIndex1].Plane;
                chamferPlanes[1] = shape.Surfaces[texGenIndex2].Plane;

                for(int p = 0; p < chamferPlanes.Length; p++)
                {
                    chamferPlanes[p].Transform(localToWorldMatrix);
                }

                int pointIndex1 = hoverMeshState.edges[_hoverOnEdgeIndex * 2 + 0];
                int pointIndex2 = hoverMeshState.edges[_hoverOnEdgeIndex * 2 + 1];

                chamferEdgePoints[0] = hoverMeshState.worldPoints[pointIndex1];
                chamferEdgePoints[1] = hoverMeshState.worldPoints[pointIndex2];

                chamferDirection = (chamferEdgePoints[1] - chamferEdgePoints[0]).normalized;
            }
            else if(_hoverOnPointIndex != -1)
            {
                chamferVertex = hoverMeshState.worldPoints[_hoverOnPointIndex];
                chamferMode   = ChamferMode.Point;
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

                chamferPlanes = foundPlanes.ToArray();
            }
            else
            {
                return false;
            }

            GUIUtility.hotControl             = _chamferId;
            GUIUtility.keyboardControl        = _chamferId;
            EditorGUIUtility.editingTextField = false;
            Event.current.Use();

            chamferBrushModified = new bool[_brushSelection.brushes.Length];
            chamferShapes        = new Shape[_brushSelection.brushes.Length];
            chamferControlMesh   = new ControlMesh[_brushSelection.brushes.Length];
            chamferSelection     = new ControlMeshSelection[_brushSelection.brushes.Length];

            for(int b = 0; b < _brushSelection.brushes.Length; b++)
            {
                chamferShapes[b]      = _brushSelection.brushes[b].Shape.Clone();
                chamferControlMesh[b] = _brushSelection.brushes[b].ControlMesh.Clone();
                chamferSelection[b]   = _brushSelection.states[b].selection.Clone();
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

            for(int p0 = 0; p0 < chamferPlanes.Length; p0++)
            {
                Vector3 tryIntersection = chamferPlanes[p0].RayIntersection(mouseRay);
                bool    tryFound        = true;

                for(int p1 = 0; p1 < chamferPlanes.Length; p1++)
                {
                    if(p0 == p1)
                    {
                        continue;
                    }

                    if(chamferPlanes[p1].Distance(tryIntersection) >= MathConstants.DistanceEpsilon)
                    {
                        tryFound = false;

                        break;
                    }
                }

                if(!tryFound)
                {
                    continue;
                }

                CSGGrid.SetForcedGrid(camera, chamferPlanes[p0]);
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

            switch(chamferMode)
            {
                default:
                {
                    return;
                }
                case ChamferMode.Point:
                {
                    projectedPoint = chamferVertex;

                    switch(activeSnapMode)
                    {
                        case SnapMode.GridSnapping:
                        {
                            chamferIntersectionPoint = intersection + CSGGrid.SnapDeltaToGrid(
                                camera, intersection - projectedPoint, new Vector3[] { intersection }
                            ); //, snapToSelf: true);

                            break;
                        }
                        case SnapMode.RelativeSnapping:
                        {
                            chamferIntersectionPoint = intersection + CSGGrid.SnapDeltaRelative(
                                camera, intersection - projectedPoint
                            );

                            break;
                        }
                        default:
                        case SnapMode.None:
                        {
                            chamferIntersectionPoint =
                                intersection + CSGGrid.HandleLockedAxi(intersection - projectedPoint);

                            break;
                        }
                    }

                    break;
                }
                case ChamferMode.Edge:
                {
                    projectedPoint = GeometryUtility.ProjectPointOnInfiniteLine(
                        intersection, chamferEdgePoints[0], chamferDirection
                    );

                    switch(activeSnapMode)
                    {
                        case SnapMode.GridSnapping:
                        {
                            chamferIntersectionPoint = intersection + CSGGrid.SnapDeltaToGrid(
                                camera, intersection - projectedPoint, new Vector3[] { intersection }
                            ); //, snapToSelf: true);

                            break;
                        }
                        case SnapMode.RelativeSnapping:
                        {
                            chamferIntersectionPoint = intersection + CSGGrid.SnapDeltaRelative(
                                camera, intersection - projectedPoint
                            );

                            break;
                        }
                        default:
                        case SnapMode.None:
                        {
                            chamferIntersectionPoint =
                                intersection + CSGGrid.HandleLockedAxi(intersection - projectedPoint);

                            break;
                        }
                    }

                    projectedPoint = GeometryUtility.ProjectPointOnInfiniteLine(
                        chamferIntersectionPoint, chamferEdgePoints[0], chamferDirection
                    );

                    break;
                }
            }

            float distance = (chamferIntersectionPoint - projectedPoint).magnitude;

            Undo.RecordObjects(_undoAbleBrushes, "Chamfer brushes");

            for(int t = 0; t < _brushSelection.brushes.Length; t++)
            {
                _brushSelection.states[t].UpdateMesh(chamferControlMesh[t]);
                chamferSelection[t].CopyTo(_brushSelection.states[t].selection);
            }

            bool anyBrushModified = false;
            //var render_lines = new List<Vector3>();
            List<int>         selected_edges = new List<int>();
            HashSet<CSGPlane> planes         = new HashSet<CSGPlane>();
            List<Vector3>     found_points   = new List<Vector3>();

            for(int b = 0; b < _brushSelection.brushes.Length; b++)
            {
                bool             brushModified = false;
                CSGBrush         brush         = _brushSelection.brushes[b];
                ControlMesh      controlMesh   = chamferControlMesh[b].Clone();
                Shape            shape         = chamferShapes[b].Clone();
                ControlMeshState meshState     = _brushSelection.states[b];

                Transform transform = brush.transform;

                planes.Clear();

                if(Math.Abs(distance) > MathConstants.EqualityEpsilon)
                {
                    //if (chamferMode == ChamferMode.Edge)
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
                            //var chamferPoints3 = chamferEdgePoints1 + delta;

                            //render_lines.Add(chamferPoints0);
                            //render_lines.Add(chamferPoints1);
                            //render_lines.Add(chamferPoints2);
                            //render_lines.Add(chamferPoints3);

                            CSGPlane chamferPlane = new CSGPlane(chamferPoints0, chamferPoints1, chamferPoints2);

                            CSGPlane localCuttingPlane = GeometryUtility.InverseTransformPlane(
                                brush.transform.localToWorldMatrix, chamferPlane
                            );

                            planes.Add(localCuttingPlane);
                        }
                    }

                    //if (chamferMode == ChamferMode.Point)
                    {
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
                            Vector3 currPoint = localToWorldMatrix.MultiplyPoint(controlMesh.Vertices[currPointIndex]);

                            found_points.Clear();

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

                                found_points.Add(currPoint + direction * distance);
                            }

                            CSGPlane chamferPlane = GeometryUtility.CalcPolygonPlane(found_points.ToArray());

                            if(chamferPlane.Distance(currPoint) < 0)
                            {
                                chamferPlane = chamferPlane.Negated();
                            }

                            CSGPlane localCuttingPlane = GeometryUtility.InverseTransformPlane(
                                brush.transform.localToWorldMatrix, chamferPlane
                            );

                            planes.Add(localCuttingPlane);
                        }
                    }

                    foreach(CSGPlane plane in planes)
                    {
                        selected_edges.Clear();

                        if(ControlMeshUtility.CutMesh(controlMesh, shape, plane, ref selected_edges))
                        {
                            List<int> edge_loop = ControlMeshUtility.FindEdgeLoop(controlMesh, ref selected_edges);

                            if(edge_loop != null)
                            {
                                if(ControlMeshUtility.SplitEdgeLoop(controlMesh, shape, edge_loop))
                                {
                                    Shape       foundShape;
                                    ControlMesh foundControlMesh;

                                    if(ControlMeshUtility.FindAndDetachSeparatePiece(
                                           controlMesh, shape, plane, out foundControlMesh, out foundShape
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

                if(brushModified || chamferBrushModified[b])
                {
                    anyBrushModified        = true;
                    chamferBrushModified[b] = brushModified;
                    brush.ControlMesh       = controlMesh;
                    brush.Shape             = shape;
                    brush.ControlMesh.SetDirty();
                    ControlMeshUtility.RebuildShape(brush);
                    InternalCSGModelManager.CheckSurfaceModifications(brush, true);
                    UpdateWorkControlMesh();
                }
            }

            for(int t = 0; t < _brushSelection.brushes.Length; t++)
            {
                _brushSelection.states[t].UpdateMesh(chamferControlMesh[t]);
                _brushSelection.states[t].selection.DeselectAll();
            }

            //chamferLines = render_lines.ToArray();

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
                            brushOutlineInfo.LastLineMeshGeneration--;
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
                            brushOutlineInfo.BrushOutlineRenderer.RenderOutlines();
                        }
                    }

                    PaintUtility.SquareDotCap(
                        -1, chamferIntersectionPoint, MathConstants.identityQuaternion
                      , CSG_HandleUtility.GetHandleSize(chamferIntersectionPoint) * GUIConstants.HANDLE_SCALE
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

            //if (Event.current.type == EventType.Layout)
            CreateControlIDs();

            Camera camera = sceneView.camera;

            if(SelectionUtility.CurrentModifiers == EventModifiers.None &&
               currentHotControl != _chamferId &&
               _useHandleCenter)
            {
                Helpers.CSGHandles.InitFunction init = delegate
                {
                    UpdateTransformMatrices();
                    UpdateSelection(false);
                    UpdateWorkControlMesh();
                    UpdateBackupPoints();
                    UpdateGrid(_startCamera);
                    _handleWorldPoints = _brushSelection.GetSelectedWorldPoints(true);
                    CenterPositionHandle();
                    _startHandleCenter = _handleCenter;
                    _usingControl      = true;
                };

                if(GUIUtility.hotControl == 0)
                {
                    _handleWorldPoints = null;
                    _handleScale       = Vector3.one;
                }

                switch(Tools.current)
                {
                    case Tool.None:
                    case Tool.Rect: break;
                    case Tool.Rotate:
                    {
                        Helpers.CSGHandles.InitFunction shutdown = delegate
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
                        };

                        Quaternion handleRotation = GetRealHandleRotation();

                        Quaternion newRotation = PaintUtility.HandleRotation(
                            camera, _handleCenter, handleRotation, init, shutdown
                        );

                        if(GUI.changed)
                        {
                            ForceLineUpdate();

                            DoRotateControlPoints(
                                _handleCenter, handleRotation, Quaternion.Inverse(handleRotation) * newRotation
                            );

                            ControlMeshUtility.MergeDuplicatePoints(_brushSelection.brushes, _brushSelection.states);
                            GUI.changed = false;
                        }

                        break;
                    }
                    case Tool.Scale:
                    {
                        Quaternion rotation = GetRealHandleRotation();

                        Helpers.CSGHandles.InitFunction shutdown = delegate
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
                        };

                        Vector3 newHandleScale = PaintUtility.HandleScale(
                            _handleScale, _handleCenter, rotation, init, shutdown
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

                            DoScaleControlPoints(rotation, newScale, _startHandleCenter);
                            ControlMeshUtility.MergeDuplicatePoints(_brushSelection.brushes, _brushSelection.states);
                            _handleScale = newScale;
                            GUI.changed  = false;
                        }

                        if(_usingControl)
                        {
                            DrawScaleBounds(camera, rotation, _handleScale, _startHandleCenter, _handleWorldPoints);
                        }

                        break;
                    }
                    case Tool.Move:
                    {
                        Quaternion rotation = GetRealHandleRotation();

                        Helpers.CSGHandles.InitFunction shutdown = delegate
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
                        };

                        Vector3 newHandleCenter = PaintUtility.HandlePosition(
                            camera, _handleCenter, rotation, _handleWorldPoints, init, shutdown
                        );

                        if(GUI.changed)
                        {
                            ForceLineUpdate();
                            Vector3 offset = newHandleCenter - _handleCenter;
                            _worldDeltaMovement += offset;
                            DoMoveControlPoints(_worldDeltaMovement);
                            ControlMeshUtility.MergeDuplicatePoints(_brushSelection.brushes, _brushSelection.states);
                            _handleCenter = _startHandleCenter + _worldDeltaMovement;
                            GUI.changed   = false;
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
                            PaintUtility.DrawScreenText(camera, _handleCenter, HoverTextDistance * 3, text);
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
                            //if (!doCloneDragging)
                        {
                            _doMarquee       = true;
                            _startMousePoint = Event.current.mousePosition;

                            CSG_EditorGUIUtility.RepaintAll();
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

                        /*
                        if (_editMode == EditMode.MovingPolygon)
                        {
                            if (SelectionUtility.CurrentModifiers == EventModifiers.Shift)
                            {
                                if (Tools.current == Tool.Move)
                                {
                                    ExtrudeSurface(drag: false);
                                    Event.current.Use();
                                    break;
                                }
                            }
                        }*/
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
                            CSG_EditorGUIUtility.RepaintAll();
                        }

                        break;
                    }

                    case EventType.KeyDown:
                    {
                        if(GUIUtility.hotControl != 0)
                        {
                            break;
                        }

                        if(HavePointSelection && Keys.deleteSelectionKey.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.cancelActionKey.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.snapToGridKey.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.mergeEdgePoints.IsKeyPressed() && HaveEdgeSelection)
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionX.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionY.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionZ.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }
                        else
                        {
                            break;
                        }
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

                            break;
                        }
                        else
                        {
                            break;
                        }
                    }

                    case EventType.ValidateCommand:
                    {
                        if(GUIUtility.hotControl != 0)
                        {
                            break;
                        }

                        SceneQueryUtility.ClearDeepClick();

                        if(HavePointSelection && (Event.current.commandName == "SoftDelete"
                            || Event.current.commandName == "Delete"))
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.cancelActionKey.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.snapToGridKey.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.mergeEdgePoints.IsKeyPressed() && HaveEdgeSelection)
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneValidate(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionX.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionY.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.flipSelectionZ.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }
                        else
                        {
                            break;
                        }
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

                            break;
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

                            CSGPlane cameraPlane = CSG_HandleUtility.GetNearPlane(camera);

                            int hoverControl = 0;
                            int hotControl   = GUIUtility.hotControl;

                            for(int t = 0; t < _brushSelection.brushes.Length; t++)
                            {
                                //var brush = _brushSelection.Brushes[t];
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

                                    if(_useHandleCenter &&
                                       (_handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr)
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

                                    if((_useHandleCenter &&
                                           (_handleCenter - center).sqrMagnitude <= MathConstants.EqualityEpsilonSqr) ||
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
                                int closestBrushIndex   = -1;
                                int closestSurfaceIndex = -1;

                                _brushSelection.FindClosestIntersection(
                                    sceneView, out closestBrushIndex, out closestSurfaceIndex
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

                                        if(newEditMode == EditMode.None &&
                                           CSGSettings.SelectionEdge)
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
                                CSG_EditorGUIUtility.RepaintAll();
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

                            if(_brushSelection != null &&
                               _brushSelection.states.Length == _brushSelection.brushes.Length)
                            {
                                SelectionType selectionType = SelectionUtility.GetEventSelectionType();

                                SelectMarquee(
                                    camera, CameraUtility.PointsToRect(_startMousePoint, Event.current.mousePosition)
                                  , selectionType
                                );
                            }

                            CSG_EditorGUIUtility.RepaintAll();

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

                            if(_brushSelection != null &&
                               _brushSelection.states.Length == _brushSelection.brushes.Length)
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

                                            break;
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

                                            break;
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

                                            break;
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

                                            break;
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

                                        if( //_prevYMode != RealtimeCSG.Grid.YMoveModeActive || 
                                           _firstMove)
                                        {
                                            //_prevYMode = RealtimeCSG.Grid.YMoveModeActive;
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
                                        else if(lockX && lockY && lockZ)
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

                                                //for (int i = 0; i < worldPoints.Length; i++)
                                                //	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
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
                                        CSG_EditorGUIUtility.RepaintAll();

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

                                            break;
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

                                            break;
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

                                            break;
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

                                            break;
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

                                            CSG_EditorGUIUtility.RepaintAll();

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

                                        if( //_prevYMode != RealtimeCSG.Grid.YMoveModeActive || 
                                           _firstMove)
                                        {
                                            //_prevYMode = RealtimeCSG.Grid.YMoveModeActive;
                                            //if (_firstMove)
                                            {
                                                int     originalVertexIndex1 = meshState.edges[e * 2 + 0];
                                                int     originalVertexIndex2 = meshState.edges[e * 2 + 1];
                                                Vector3 originalVertex1 = meshState.worldPoints[originalVertexIndex1];
                                                Vector3 originalVertex2 = meshState.worldPoints[originalVertexIndex2];

                                                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                                                float   squaredDist, s;
                                                Vector3 closestRay;

                                                _originalPoint = MathUtils.ClosestPtSegmentRay(
                                                    originalVertex1, originalVertex2, ray, out squaredDist, out s
                                                  , out closestRay
                                                );
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
                                        else if(lockX && lockY && lockZ)
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

                                            _handleWorldPoints = _brushSelection.GetSelectedWorldPoints(true);
                                            _dragEdgeScale     = Vector3.one;
                                            _dragEdgeRotation  = GetRealHandleRotation();

                                            Quaternion rotation        = _dragEdgeRotation;
                                            Quaternion inverseRotation = Quaternion.Inverse(rotation);

                                            Vector3 delta    = _originalPoint - _handleCenter;
                                            float   distance = delta.magnitude;
                                            _startHandleDirection = delta / distance;

                                            _startHandleDirection =
                                                GeometryUtility.SnapToClosestAxis(
                                                    inverseRotation * _startHandleDirection
                                                );

                                            _startHandleDirection = rotation * _startHandleDirection;

                                            _startHandleCenter = _handleCenter;

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
                                                Vector3[] worldPoints = _handleWorldPoints;

                                                //for (int i = 0; i < worldPoints.Length; i++)
                                                //	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
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
                                            Quaternion rotation        = _dragEdgeRotation;
                                            Quaternion inverseRotation = Quaternion.Inverse(rotation);

                                            Vector3 start = GeometryUtility.ProjectPointOnInfiniteLine(
                                                _originalPoint, _startHandleCenter, _startHandleDirection
                                            );

                                            Vector3 end = GeometryUtility.ProjectPointOnInfiniteLine(
                                                intersection, _startHandleCenter, _startHandleDirection
                                            );

                                            Vector3 oldDistance = inverseRotation * (start - _startHandleCenter);
                                            Vector3 newDistance = inverseRotation * (end - _startHandleCenter);

                                            if(Mathf.Abs(oldDistance.x) > MathConstants.DistanceEpsilon)
                                            {
                                                _dragEdgeScale.x = newDistance.x / oldDistance.x;
                                            }

                                            if(Mathf.Abs(oldDistance.y) > MathConstants.DistanceEpsilon)
                                            {
                                                _dragEdgeScale.y = newDistance.y / oldDistance.y;
                                            }

                                            if(Mathf.Abs(oldDistance.z) > MathConstants.DistanceEpsilon)
                                            {
                                                _dragEdgeScale.z = newDistance.z / oldDistance.z;
                                            }

                                            if(float.IsNaN(_dragEdgeScale.x) || float.IsInfinity(_dragEdgeScale.x))
                                            {
                                                _dragEdgeScale.x = 1.0f;
                                            }

                                            if(float.IsNaN(_dragEdgeScale.y) || float.IsInfinity(_dragEdgeScale.y))
                                            {
                                                _dragEdgeScale.y = 1.0f;
                                            }

                                            if(float.IsNaN(_dragEdgeScale.z) || float.IsInfinity(_dragEdgeScale.z))
                                            {
                                                _dragEdgeScale.z = 1.0f;
                                            }

                                            _dragEdgeScale.x = Mathf.Round(_dragEdgeScale.x / CSGSettings.SnapScale)
                                              * CSGSettings.SnapScale;

                                            _dragEdgeScale.y = Mathf.Round(_dragEdgeScale.y / CSGSettings.SnapScale)
                                              * CSGSettings.SnapScale;

                                            _dragEdgeScale.z = Mathf.Round(_dragEdgeScale.z / CSGSettings.SnapScale)
                                              * CSGSettings.SnapScale;

                                            DoScaleControlPoints(rotation, _dragEdgeScale, _startHandleCenter);
                                        }

                                        CenterPositionHandle();
                                        CSG_EditorGUIUtility.RepaintAll();

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

                                        InternalCSGModelManager.CheckSurfaceModifications(
                                            _brushSelection.brushes
                                        );

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
                                            if(_handleWorldPoints == null)
                                            {
                                                return;
                                            }

                                            Vector3 realScale = _dragEdgeScale;

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
                                                camera, _dragEdgeRotation, realScale, _startHandleCenter
                                              , _handleWorldPoints
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

                                            break;
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

                                            break;
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

                                            break;
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

                                            break;
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

                                        if( //_prevYMode != RealtimeCSG.Grid.YMoveModeActive || 
                                           _firstMove)
                                        {
                                            //_prevYMode = RealtimeCSG.Grid.YMoveModeActive;
                                            if(_firstMove)
                                            {
                                                _originalPoint = meshState.polygonCenterPoints[p];
                                                //UpdateWorkControlMesh();
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
                                        else if(lockX && lockY && lockZ)
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
                                                    //for (int i = 0; i < worldPoints.Length; i++)
                                                    //	worldPoints[i] = GeometryUtility.ProjectPointOnPlane(movePlane, worldPoints[i]);// - center));
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
                                            //	case EditMode.ScalePolygon:  DoScaleControlPoints(worldDeltaMovement, meshState.polygonCenterPoints[p]); break;
                                        }

                                        CenterPositionHandle();
                                        CSG_EditorGUIUtility.RepaintAll();

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

        private static Vector2 scrollPos;

        public void OnInspectorGUI(EditorWindow window, float height)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            {
                EditModeMeshModeGUI.OnInspectorGUI(window, height);
            }

            EditorGUILayout.EndScrollView();
        }

        public Rect GetLastSceneGUIRect()
        {
            return EditModeMeshModeGUI.GetLastSceneGUIRect(this);
        }

        public bool OnSceneGUI(Rect windowRect)
        {
            if(_brushSelection.brushes == null)
            {
                return false;
            }

            EditModeMeshModeGUI.OnSceneGUI(windowRect, this);

            return true;
        }
    }
}

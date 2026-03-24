using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class BackupBrushData
    {
        public Shape shape;
        public CSGModel model;
        public ControlMesh controlMesh;
        public CSGBrush brush; // to ensure that we don't copy things to wrong brush
        public CSGBrush brushCopy;
        public bool keepCopy;
        public bool active;
        public Matrix4x4 localToWorldMatrix;
        public GeometryWireframe wireFrame;
        public bool removeOnCommit;
    }

    internal sealed class EditModeClip : ScriptableObject, IEditMode
    {
        public bool UsesUnitySelection
        {
            get => false;
        }

        public bool IgnoreUnityRect
        {
            get => true;
        }

        [SerializeField] private BackupBrushData[] backupData = Array.Empty<BackupBrushData>();

        [SerializeField] private AABB bounds;
        [SerializeField] private Vector3 boundsCenter;

        [SerializeField] private CSGBrush[] clipBrushes = Array.Empty<CSGBrush>();
        [SerializeField] private Vector3[] points = new Vector3[3];
        [SerializeField] private int pointsUsed;
        [SerializeField] private int pointsShown;

        [SerializeField] private CSGPlane clipPlane;

        [SerializeField] private bool clipPlaneValid;
        [SerializeField] private Vector3 clipPlaneTangent;
        [SerializeField] private Vector3 clipPlaneBinormal;

        [SerializeField] private CSGPlane movePlane;
        [NonSerialized] private CSGPlane _prevMovePlane;

        [NonSerialized] private bool _commitOnClick;
        [NonSerialized] private bool _mouseDragged;
        [NonSerialized] private int _planeCreationID = -1;

        [NonSerialized]
        private List<Vector3> _visualSnappedLines; // used to visualize lines which we're snapping against

        [NonSerialized] private CSGBrush _currentOnBrush;
        [NonSerialized] private Vector3? _currentMousePoint;
        [NonSerialized] private Vector3? _prevMousePoint;
        [NonSerialized] private bool _forceWireframeUpdate;

        [NonSerialized] private bool _isEnabled;
        [NonSerialized] private bool _hideTool;

        internal enum EditMode
        {
            None
          , CreatingPoint1
          , CreatingPoint2
          , EditPoints
        }

        [SerializeField] internal EditMode editMode = EditMode.None;
        [SerializeField] internal ClipMode clipMode = ClipMode.RemovePositive;

        private static readonly int planeCreationHash = "planeCreation".GetHashCode();
        private static readonly int clipEditBrushToolHash = "ClipEditBrushTool".GetHashCode();

        public int ClipBrushCount
        {
            get => clipBrushes.Length;
        }

        public void SetTargets(FilteredSelection filteredSelection)
        {
            HashSet<CSGBrush> foundBrushes = filteredSelection.GetAllContainedBrushes();
            clipBrushes           = foundBrushes.ToArray();
            _forceWireframeUpdate = true;
            _hideTool             = filteredSelection.nodeTargets.Length > 0;

            if(!_isEnabled)
            {
                return;
            }

            if(backupData.Length > 0) // check if we have -any- brushes at all
            {
                // check if we already have a clip-plane, so we need to add/remove brushes
                if(clipPlaneValid)
                {
                    HashSet<int> removedBrushes = new HashSet<int>();

                    for(int i = backupData.Length - 1; i >= 0; i--)
                    {
                        if(foundBrushes.Contains(backupData[i].brush))
                        {
                            continue;
                        }

                        removedBrushes.Add(i);
                    }

                    if(removedBrushes.Count > 0)
                    {
                        RestoreTargetData(false, removedBrushes);

                        foreach(int index in removedBrushes)
                        {
                            ArrayUtility.RemoveAt(ref backupData, index);
                        }
                    }

                    List<CSGBrush> addedBrushes = new List<CSGBrush>();

                    foreach(CSGBrush foundBrush in foundBrushes)
                    {
                        bool found = false;

                        for(int i = 0; i < backupData.Length; i++)
                        {
                            if(backupData[i].brush == foundBrush)
                            {
                                found = true;

                                break;
                            }
                        }

                        if(!found)
                        {
                            addedBrushes.Add(foundBrush);
                        }
                    }

                    if(addedBrushes.Count > 0)
                    {
                        CSGBrush[] addedBrushesArray = addedBrushes.ToArray();
                        int        offset            = RememberTargetData(addedBrushesArray);

                        UpdateTargetClipping(offset);
                    }
                }
                else
                {
                    backupData = Array.Empty<BackupBrushData>();
                }
            }

            Tools.hidden = _hideTool;
            UpdateBounds();

            // if we have a clip-plane but no brushes, then just start normally
            if(clipPlaneValid && backupData.Length == 0)
            {
                RememberTargetData(); // need to remember this before doing undo because unity will mess it up the undo and we'll need to fix it
                RecordAllObjects("Updated selection");
                Undo.FlushUndoRecordObjects();
                UpdateTargetClipping();
            }

            CSGEditorGUIUtility.RepaintAll();
        }

        public void OnEnableTool()
        {
            _isEnabled   = true;
            Tools.hidden = _hideTool;
            clipMode     = CSGSettings.ClipMode;

            ResetTool();
            UpdateBounds();

            _forceWireframeUpdate = true;
        }

        public void OnDisableTool()
        {
            if(editMode != EditMode.None)
            {
                Cancel();
            }

            //needUpdate = false;
            Tools.hidden = false;
            _isEnabled   = false;

            ResetTool();
        }

        private void ResetTool()
        {
            _prevMousePoint    = null;
            _currentMousePoint = null;

            ResetEditMode();

            if(CSGGrid.ForceGrid)
            {
                CSGGrid.ForceGrid = false;
                CSGEditorGUIUtility.RepaintAll();
            }

            _forceWireframeUpdate = true;
        }

        private void ResetEditMode()
        {
            editMode              = EditMode.None;
            clipPlaneValid        = false;
            pointsUsed            = 0;
            _forceWireframeUpdate = true;
        }

        private void UpdateBounds()
        {
            bounds = BoundsUtilities.GetBounds(clipBrushes);

            boundsCenter = bounds.Center;
            bounds.MinX  = (bounds.MinX - boundsCenter.x) * 4.0f + boundsCenter.x;
            bounds.MinY  = (bounds.MinY - boundsCenter.y) * 4.0f + boundsCenter.y;
            bounds.MinZ  = (bounds.MinZ - boundsCenter.z) * 4.0f + boundsCenter.z;
            bounds.MaxX  = (bounds.MaxX - boundsCenter.x) * 4.0f + boundsCenter.x;
            bounds.MaxY  = (bounds.MaxY - boundsCenter.y) * 4.0f + boundsCenter.y;
            bounds.MaxZ  = (bounds.MaxZ - boundsCenter.z) * 4.0f + boundsCenter.z;
        }

        public bool DeselectAll()
        {
            if(editMode != EditMode.None)
            {
                _forceWireframeUpdate = true;

                Cancel();

                return true;
            }

            if(clipBrushes != null && clipBrushes.Length != 0)
            {
                ResetEditMode();
            }

            Selection.activeTransform = null;
            _forceWireframeUpdate     = true;

            return true;
        }

        public void Commit()
        {
            InternalCSGModelManager.SkipCheckForChanges = true;

            try
            {
                Undo.FlushUndoRecordObjects();
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(this, "Finalized clipping");
                List<Object> selectedObjects = new List<Object>();

                if(backupData.Length > 0)
                {
                    for(int t = 0; t < backupData.Length; t++)
                    {
                        if(backupData[t].brush)
                        {
                            backupData[t].brush.gameObject.hideFlags = HideFlags.None;

                            if(backupData[t].removeOnCommit)
                            {
                                Undo.DestroyObjectImmediate(backupData[t].brush.gameObject);

                                continue;
                            }

                            selectedObjects.Add(backupData[t].brush.gameObject);
                        }

                        if(backupData[t].brushCopy)
                        {
                            backupData[t].brushCopy.gameObject.hideFlags = HideFlags.None;

                            if(!backupData[t].brushCopy.isActiveAndEnabled)
                            {
                                Undo.DestroyObjectImmediate(backupData[t].brushCopy.gameObject);
                            }
                            else
                            {
                                selectedObjects.Add(backupData[t].brushCopy.gameObject);
                            }
                        }
                    }
                }

                InternalCSGModelManager.CheckForChanges();

                for(int t = 0; t < clipBrushes.Length; t++)
                {
                    if(!clipBrushes[t])
                    {
                        continue;
                    }

                    BrushOutlineManager.ForceUpdateOutlines(clipBrushes[t].brushNodeID);
                }

                UpdateBounds();
                ResetEditMode();

                if(selectedObjects.Count > 0)
                {
                    Selection.objects = selectedObjects.ToArray();
                }
            }
            finally
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
                CSGEditorGUIUtility.RepaintAll();
                _forceWireframeUpdate = true;
            }
        }

        public void Cancel()
        {
            InternalCSGModelManager.SkipCheckForChanges = true;

            try
            {
                Undo.FlushUndoRecordObjects();
                Undo.IncrementCurrentGroup();
                Undo.RegisterCompleteObjectUndo(this, "Canceling clipping");

                for(int t = 0; t < backupData.Length; t++)
                {
                    if(backupData[t].brush)
                    {
                        backupData[t].brush.gameObject.hideFlags = HideFlags.None;
                    }

                    if(backupData[t].brushCopy)
                    {
                        backupData[t].brushCopy.gameObject.hideFlags = HideFlags.None;
                    }
                }

                RestoreTargetData();

                InternalCSGModelManager.CheckForChanges();

                for(int t = 0; t < clipBrushes.Length; t++)
                {
                    BrushOutlineManager.ForceUpdateOutlines(clipBrushes[t].brushNodeID);
                }

                UpdateBounds();
                ResetEditMode();
            }
            finally
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
                CSGEditorGUIUtility.RepaintAll();

                _forceWireframeUpdate = true;
            }
        }

        private int RememberTargetData(CSGBrush[] addedBrushes = null)
        {
            if(addedBrushes != null)
            {
                if(addedBrushes.Length == 0)
                {
                    return 0;
                }
            }
            else if(clipBrushes == null || clipBrushes.Length == 0)
            {
                backupData = Array.Empty<BackupBrushData>();

                return 0;
            }

            CSGBrush[] srcBrushes;
            int        offset = 0;

            if(addedBrushes != null)
            {
                srcBrushes = addedBrushes;

                if(backupData.Length == 0)
                {
                    backupData = new BackupBrushData[addedBrushes.Length];
                }
                else
                {
                    offset = backupData.Length;
                    Array.Resize(ref backupData, backupData.Length + addedBrushes.Length);
                }
            }
            else
            {
                srcBrushes = clipBrushes;
                backupData = new BackupBrushData[clipBrushes.Length];
            }

            for(int srcIndex = srcBrushes.Length - 1; srcIndex >= 0; srcIndex--)
            {
                int      dstIndex = srcIndex + offset;
                CSGBrush srcBrush = srcBrushes[srcIndex];

                if(!srcBrush)
                {
                    ArrayUtility.RemoveAt(ref backupData, dstIndex);

                    continue;
                }

                backupData[dstIndex]                = new BackupBrushData();
                backupData[dstIndex].brush          = srcBrush;
                backupData[dstIndex].removeOnCommit = false;
                backupData[dstIndex].active         = srcBrush.gameObject.activeSelf;
                backupData[dstIndex].controlMesh    = srcBrush.ControlMesh.Clone();
                backupData[dstIndex].shape          = srcBrush.Shape.Clone();
                backupData[dstIndex].wireFrame      = BrushOutlineManager.GetBrushOutline(srcBrush.brushNodeID);

                if(srcBrush.ChildData == null || !srcBrush.ChildData.Model)
                {
                    backupData[dstIndex].localToWorldMatrix = Matrix4x4.identity;
                    backupData[dstIndex].model              = null;

                    continue;
                }

                backupData[dstIndex].localToWorldMatrix = srcBrush.compareTransformation.localToWorldMatrix;
                backupData[dstIndex].model              = srcBrush.ChildData.Model;
            }

            return offset;
        }

        private void RestoreTargetData(bool hoverMode = false, HashSet<int> selectedIndices = null)
        {
            if(backupData.Length == 0)
            {
                return;
            }

            try
            {
                InternalCSGModelManager.SkipCheckForChanges = true;
                bool needRefresh = false;

                for(int t = 0; t < backupData.Length; t++)
                {
                    if(selectedIndices != null && !selectedIndices.Contains(t))
                    {
                        continue;
                    }

                    CSGModel model = backupData[t].model;

                    if(!model)
                    {
                        continue;
                    }

                    CSGBrush brush = backupData[t].brush;

                    if(!brush)
                    {
                        continue;
                    }

                    if(brush.gameObject.activeSelf != backupData[t].active)
                    {
                        brush.gameObject.SetActive(backupData[t].active);
                    }

                    if(!backupData[t].brush.IsRegistered && backupData[t].brush.isActiveAndEnabled)
                    {
                        InternalCSGModelManager.OnCreated(backupData[t].brush);
                        needRefresh = true;
                    }
                }

                if(needRefresh)
                {
                    InternalCSGModelManager.CheckForChanges(true);
                }

                for(int t = backupData.Length - 1; t >= 0; t--)
                {
                    if(selectedIndices != null && !selectedIndices.Contains(t))
                    {
                        continue;
                    }

                    CSGModel model = backupData[t].model;

                    if(!model)
                    {
                        continue;
                    }

                    CSGBrush brush = backupData[t].brush;

                    if(!brush)
                    {
                        continue;
                    }

                    if(backupData[t].brushCopy)
                    {
                        if(!hoverMode)
                        {
                            Undo.DestroyObjectImmediate(backupData[t].brushCopy.gameObject);
                        }
                        else
                        {
                            if(backupData[t].brushCopy.gameObject.activeSelf)
                            {
                                backupData[t].brushCopy.gameObject.SetActive(false);
                            }
                        }
                    }

                    brush.ControlMesh = backupData[t].controlMesh.Clone();
                    brush.Shape       = backupData[t].shape.Clone();
                    ControlMeshUtility.RebuildShape(brush);

                    if(brush.ControlMesh != null)
                    {
                        brush.ControlMesh.SetDirty();
                    }

                    InternalCSGModelManager.CheckSurfaceModifications(brush, true);
                }

                if(!hoverMode)
                {
                    InternalCSGModelManager.CheckForChanges(true);
                }
                else
                {
                    InternalCSGModelManager.CheckTransformChanged();
                    InternalCSGModelManager.OnHierarchyModified();
                    InternalCSGModelManager.UpdateRemoteMeshes();
                }
            }
            finally
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
            }
        }

        private enum ClipSide
        {
            Invalid
          , CompletelyInside
          , CompletelyOutside
          , Split
        }

        private void UpdateTargetClipping(int offset = 0)
        {
            if(!clipPlaneValid || backupData.Length == 0)
            {
                return;
            }

            CSGPlane cuttingPlane = clipPlane;
            bool     needCopies   = false;

            switch(clipMode)
            {
                case ClipMode.Split:
                {
                    needCopies = true;

                    break;
                }
                case ClipMode.RemovePositive:
                {
                    break;
                }
                case ClipMode.RemoveNegative:
                {
                    cuttingPlane = cuttingPlane.Negated();

                    break;
                }
            }

            bool       foundValidBrushes = false;
            ClipSide[] clipSides         = new ClipSide[backupData.Length];
            CSGPlane[] clipLocalPlane    = new CSGPlane[backupData.Length];

            for(int t = backupData.Length - 1; t >= offset; t--)
            {
                CSGModel model = backupData[t].model;

                if(!model)
                {
                    clipSides[t] = ClipSide.Invalid;

                    continue;
                }

                CSGBrush brush = backupData[t].brush;

                if(!brush)
                {
                    clipSides[t] = ClipSide.Invalid;

                    continue;
                }

                backupData[t].removeOnCommit = false;

                Transform brushTransform    = brush.GetComponent<Transform>();
                Matrix4x4 matrix            = brushTransform.localToWorldMatrix;
                CSGPlane  localCuttingPlane = GeometryUtility.InverseTransformPlane(matrix, cuttingPlane);

                Vector3[] vertices = backupData[t].controlMesh.Vertices;

                int inFrontCount = 0;
                int behindCount  = 0;

                for(int p = 0; p < vertices.Length; p++)
                {
                    if(localCuttingPlane.Distance(vertices[p]) > MathConstants.DistanceEpsilon)
                    {
                        behindCount++;
                    }
                    else
                    {
                        inFrontCount++;
                    }
                }

                if(inFrontCount != 0 && behindCount != 0)
                {
                    clipSides[t] = ClipSide.Split;
                }
                else if(inFrontCount == 0 && behindCount > 0)
                {
                    clipSides[t] = ClipSide.CompletelyInside;
                }
                else
                {
                    clipSides[t] = ClipSide.CompletelyOutside;
                }

                clipLocalPlane[t] = localCuttingPlane;
                foundValidBrushes = true;
            }

            if(!foundValidBrushes)
            {
                return;
            }

            try
            {
                InternalCSGModelManager.SkipCheckForChanges = true;

                for(int t = backupData.Length - 1; t >= offset; t--)
                {
                    if(clipSides[t] == ClipSide.Invalid)
                    {
                        continue;
                    }

                    CSGBrush brush             = backupData[t].brush;
                    CSGPlane localCuttingPlane = clipLocalPlane[t];

                    brush.ControlMesh = backupData[t].controlMesh.Clone();
                    brush.Shape       = backupData[t].shape.Clone();

                    if(brush.gameObject.activeSelf != backupData[t].active)
                    {
                        brush.gameObject.SetActive(backupData[t].active);
                    }

                    backupData[t].keepCopy = false;

                    if(clipSides[t] == ClipSide.CompletelyOutside)
                    {
                        continue;
                    }

                    if(clipSides[t] == ClipSide.Split)
                    {
                        List<int> selectedEdges = new List<int>();

                        if(ControlMeshUtility.CutMesh(brush.ControlMesh, brush.Shape, localCuttingPlane, ref selectedEdges))
                        {
                            List<int> edgeLoop = ControlMeshUtility.FindEdgeLoop(brush.ControlMesh, ref selectedEdges);

                            if(edgeLoop != null)
                            {
                                if(ControlMeshUtility.SplitEdgeLoop(brush.ControlMesh, brush.Shape, edgeLoop))
                                {
                                    if(ControlMeshUtility.FindAndDetachSeparatePiece(brush.ControlMesh, brush.Shape, localCuttingPlane, out ControlMesh foundControlMesh, out Shape foundShape))
                                    {
                                        if(needCopies)
                                        {
                                            if(backupData[t].brushCopy == null)
                                            {
                                                backupData[t].brushCopy = GetDeepCopy(brush);
                                            }

                                            backupData[t].brushCopy.Shape       = brush.Shape.Clone();
                                            backupData[t].brushCopy.ControlMesh = brush.ControlMesh.Clone();
                                            backupData[t].brushCopy.ControlMesh.SetDirty();

                                            backupData[t].brushCopy.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;

                                            backupData[t].keepCopy = true;
                                        }

                                        brush.Shape       = foundShape;
                                        brush.ControlMesh = foundControlMesh;
                                        brush.ControlMesh.SetDirty();
                                    }
                                }
                            }
                        }

                        if(backupData[t].brushCopy != null)
                        {
                            bool newActive = needCopies && backupData[t].keepCopy && backupData[t].active;

                            if(backupData[t].brushCopy.gameObject.activeSelf != newActive)
                            {
                                backupData[t].brushCopy.gameObject.SetActive(newActive);
                            }
                        }
                    }
                    else
                    {
                        if(backupData[t].brushCopy != null)
                        {
                            if(backupData[t].brushCopy.gameObject.activeSelf != needCopies)
                            {
                                backupData[t].brushCopy.gameObject.SetActive(needCopies);
                            }
                        }

                        if(clipSides[t] == ClipSide.CompletelyInside)
                        {
                            backupData[t].removeOnCommit = true;

                            if(brush.gameObject.activeSelf)
                            {
                                brush.gameObject.SetActive(false);
                            }
                        }
                    }
                }

                bool haveCopy = false;

                for(int t = offset; t < backupData.Length; t++)
                {
                    if(clipSides[t] == ClipSide.Invalid)
                    {
                        continue;
                    }

                    if(backupData[t].brush && !backupData[t].brush.IsRegistered && backupData[t].brush.isActiveAndEnabled)
                    {
                        InternalCSGModelManager.OnCreated(backupData[t].brush);
                        haveCopy = true;
                    }

                    if(needCopies && backupData[t].brushCopy && !backupData[t].brushCopy.IsRegistered && backupData[t].brushCopy.isActiveAndEnabled)
                    {
                        InternalCSGModelManager.OnCreated(backupData[t].brushCopy);

                        haveCopy = true;
                    }
                }

                if(haveCopy)
                {
                    InternalCSGModelManager.CheckForChanges(true);
                }

                for(int t = offset; t < backupData.Length; t++)
                {
                    if(clipSides[t] == ClipSide.Invalid)
                    {
                        continue;
                    }

                    if(needCopies && backupData[t].keepCopy && backupData[t].brushCopy && backupData[t].brushCopy.gameObject.activeSelf)
                    {
                        ControlMeshUtility.RebuildShape(backupData[t].brushCopy);
                        InternalCSGModelManager.CheckSurfaceModifications(backupData[t].brushCopy, true);

                        if(backupData[t].brushCopy.ControlMesh != null)
                        {
                            backupData[t].brushCopy.ControlMesh.SetDirty();
                        }

                        InternalCSGModelManager.CheckSurfaceModifications(backupData[t].brushCopy, true);
                    }

                    ControlMeshUtility.RebuildShape(backupData[t].brush);

                    if(backupData[t].removeOnCommit)
                    {
                        continue;
                    }

                    if(backupData[t].brush.ControlMesh != null)
                    {
                        backupData[t].brush.ControlMesh.SetDirty();
                    }

                    InternalCSGModelManager.CheckSurfaceModifications(backupData[t].brush, true);
                }

                InternalCSGModelManager.CheckForChanges(true);
            }
            finally
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
            }
        }

        private static CSGBrush GetDeepCopy(CSGBrush brush)
        {
            GameObject srcGameObject = brush.gameObject;
            Transform  srcTransform  = brush.transform;
            GameObject dstGameObject = Instantiate(srcGameObject, srcTransform.parent, false);
            dstGameObject.transform.SetSiblingIndex(srcTransform.GetSiblingIndex() + 1);
            dstGameObject.transform.localPosition = srcTransform.localPosition;
            dstGameObject.transform.localRotation = srcTransform.localRotation;
            dstGameObject.transform.localScale    = srcTransform.localScale;
            dstGameObject.name                    = GameObjectUtility.GetUniqueNameForSibling(srcTransform.parent, srcGameObject.name);

            CSGBrush copy = dstGameObject.GetComponent<CSGBrush>();

            Undo.RegisterCreatedObjectUndo(dstGameObject, "Created copy");

            return copy;
        }

        private void RecordAllObjects(string text)
        {
            Undo.RegisterCompleteObjectUndo(this, text);
        }

        public void SetClipMode(ClipMode newClipMode)
        {
            if(clipMode == newClipMode)
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(this, "Updated clip mode");

            try
            {
                clipMode             = newClipMode;
                CSGSettings.ClipMode = clipMode;
                CSGSettings.Save();

                if(clipPlaneValid)
                {
                    UpdateTargetClipping();
                }
                else
                {
                    RestoreTargetData();
                }

                Undo.FlushUndoRecordObjects();
            }
            finally
            {
                CSGEditorGUIUtility.RepaintAll();
            }
        }

        public bool UndoRedoPerformed()
        {
            try
            {
                if(clipPlaneValid)
                {
                    UpdateTargetClipping();
                }
                else
                {
                    RestoreTargetData();
                }

                _forceWireframeUpdate = true;
            }
            finally
            {
                CSGEditorGUIUtility.RepaintAll();
            }

            return true;
        }

        private void UpdateClipPlane()
        {
            if(!CreateClipPlane())
            {
                points[2] = points[1] - movePlane.normal * 2.0f;

                CreateClipPlane();
            }

            if(!clipPlaneValid)
            {
                RestoreTargetData();
            }
            else
            {
                UpdateTargetClipping();
            }

            CSGEditorGUIUtility.RepaintAll();
        }

        private bool CreateClipPlane()
        {
            if(Mathf.Abs(movePlane.Distance(points[0])) < MathConstants.DistanceEpsilon &&
               Mathf.Abs(movePlane.Distance(points[1])) < MathConstants.DistanceEpsilon &&
               Mathf.Abs(movePlane.Distance(points[2])) < MathConstants.DistanceEpsilon)
            {
                return false;
            }

            CSGPlane newClipPlane = new CSGPlane(points[1], points[0], points[2]);

            if(float.IsNaN(newClipPlane.a) || float.IsInfinity(newClipPlane.a) ||
               float.IsNaN(newClipPlane.b) || float.IsInfinity(newClipPlane.b) ||
               float.IsNaN(newClipPlane.c) || float.IsInfinity(newClipPlane.c) ||
               float.IsNaN(newClipPlane.d) || float.IsInfinity(newClipPlane.d) ||
               newClipPlane.normal.magnitude < 0.75f)
            {
                clipPlaneValid = false;

                return false;
            }

            clipPlane      = newClipPlane;
            clipPlaneValid = true;

            GeometryUtility.CalculateTangents(clipPlane.normal, out clipPlaneTangent, out clipPlaneBinormal);

            return true;
        }

        private void UpdateMouseIntersection(SceneView sceneView)
        {
            Camera camera       = sceneView.camera;
            bool   assume2DView = CSGSettings.Assume2DView(camera);

            Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            _currentOnBrush = null;

            if(SceneQueryUtility.FindWorldIntersection(camera, worldRay, out LegacyBrushIntersection intersection))
            {
                movePlane          = intersection.worldPlane;
                _currentOnBrush    = intersection.brush;
                _currentMousePoint = intersection.worldIntersection;
                CSGGrid.SetForcedGrid(camera, movePlane);
            }
            else
            {
                CSGGrid.ForceGrid = false;

                if(pointsUsed > 0 && camera != null && assume2DView)
                {
                    movePlane = new CSGPlane(camera.transform.forward, points[0]);
                }
                else
                {
                    movePlane = CSGGrid.CurrentGridPlane;
                }

                if(movePlane.TryRayIntersection(worldRay, out Vector3 intersectionPoint))
                {
                    _currentMousePoint = intersectionPoint;
                }
                else
                {
                    _currentMousePoint = null;
                }
            }

            _visualSnappedLines = null;

            if(_currentMousePoint.HasValue)
            {
                _currentMousePoint = EditModeManager.SnapPointToGrid(camera, _currentMousePoint.Value, movePlane, ref _visualSnappedLines, out CSGBrush snapOnBrush, null);

                if(snapOnBrush != null)
                {
                    _currentOnBrush = snapOnBrush;
                }
            }

            if(_prevMovePlane != movePlane || _currentMousePoint != _prevMousePoint)
            {
                _prevMovePlane = movePlane;
                CSGEditorGUIUtility.RepaintAll();
                _prevMousePoint = _currentMousePoint;
            }
        }

        private MouseCursor _currentCursor = MouseCursor.Arrow;

        private void UpdateMouseCursor()
        {
            if(GUIUtility.hotControl != 0)
            {
                return;
            }

            _currentCursor = SelectionUtility.GetEventSelectionType() switch
            {
                SelectionType.Additive    => MouseCursor.ArrowPlus
              , SelectionType.Subtractive => MouseCursor.ArrowMinus
              , _                         => MouseCursor.Arrow
            };
        }

        private Vector3 HandleMovement(SceneView sceneView)
        {
            RestoreTargetData(true);
            UpdateMouseIntersection(sceneView);

            return _currentMousePoint.HasValue ? _currentMousePoint.Value : MathConstants.zeroVector3;
        }

        private void CreateFirstPoint(SceneView sceneView)
        {
            if(!_currentMousePoint.HasValue)
            {
                return;
            }

            Camera camera       = sceneView.camera;
            bool   assume2DView = CSGSettings.Assume2DView(camera);

            Vector3 intersectionPoint = _currentMousePoint.Value;

            points[0] = intersectionPoint;
            points[1] = intersectionPoint;
            points[2] = intersectionPoint;

            Vector3 normal = movePlane.normal;

            if(camera != null && assume2DView)
            {
                normal = -camera.transform.forward;
            }

            if(_currentOnBrush != null)
            {
                if(_currentOnBrush.ChildData != null && _currentOnBrush.ChildData.ModelTransform != null)
                {
                    Vector3 rayStart = intersectionPoint - normal * camera.farClipPlane;
                    Vector3 rayEnd   = intersectionPoint + normal * camera.farClipPlane;

                    if(SceneQueryUtility.FindBrushIntersection(_currentOnBrush, _currentOnBrush.ChildData.ModelTransform.localToWorldMatrix, rayStart, rayEnd, out LegacyBrushIntersection intersectionFar))
                    {
                        points[2] = intersectionFar.worldIntersection;
                    }
                }
            }

            if((clipBrushes == null || clipBrushes.Length == 0) && _currentOnBrush)
            {
                Selection.objects = new Object[1] { _currentOnBrush.gameObject };
                EditModeManager.UpdateSelection();
            }

            RememberTargetData(); // need to remember this before doing undo because unity will mess it up the undo and we'll need to fix it

            RecordAllObjects("Created clip plane");

            if((points[2] - points[1]).magnitude < 0.1f)
            {
                Vector3 direction  = -normal;
                Vector3 snapVector = CSGGrid.CurrentGridSnapVector;

                direction.x *= Mathf.Max(1.0f, snapVector.x);
                direction.y *= Mathf.Max(1.0f, snapVector.y);
                direction.z *= Mathf.Max(1.0f, snapVector.z);
                points[2]   =  points[1] + direction;
            }

            pointsUsed  = 1;
            pointsShown = 1;
            editMode    = EditMode.CreatingPoint1;
        }

        private void CreateControlIDs()
        {
            int meshEditBrushToolID = GUIUtility.GetControlID(clipEditBrushToolHash, FocusType.Passive);
            HandleUtility.AddDefaultControl(meshEditBrushToolID);
            _planeCreationID = GUIUtility.GetControlID(planeCreationHash, FocusType.Keyboard);
        }

        public void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            EventType originalEventType = Event.current.type;

            if(originalEventType == EventType.MouseDown || originalEventType == EventType.MouseMove)
            {
                _mouseDragged = false;
            }
            else if(originalEventType == EventType.MouseDrag)
            {
                _mouseDragged = true;
            }

            CreateControlIDs();

            switch(editMode)
            {
                case EditMode.None:
                case EditMode.CreatingPoint1:
                case EditMode.CreatingPoint2:
                {
                    HandleCreatePointEvents(sceneView, sceneRect);

                    break;
                }
                case EditMode.EditPoints:
                {
                    HandleEditPointEvents(sceneView, sceneRect);

                    break;
                }
            }

            if(originalEventType == EventType.MouseUp)
            {
                _mouseDragged = false;
            }
        }

        [NonSerialized] private readonly LineMeshManager _zTestLineMeshManager2 = new LineMeshManager();
        [NonSerialized] private readonly LineMeshManager _noZTestLineMeshManager2 = new LineMeshManager();
        [NonSerialized] private readonly LineMeshManager _zTestLineMeshManager1 = new LineMeshManager();
        [NonSerialized] private readonly LineMeshManager _noZTestLineMeshManager1 = new LineMeshManager();
        [NonSerialized] private int _lastLineMeshGeneration = -1;

        private void OnDestroy()
        {
            _zTestLineMeshManager1.Destroy();
            _noZTestLineMeshManager1.Destroy();
            _zTestLineMeshManager2.Destroy();
            _noZTestLineMeshManager2.Destroy();
        }

        private void OnPaint(SceneView sceneView)
        {
            if(sceneView != null)
            {
                Rect windowRect = new Rect(0, 0, sceneView.position.width, sceneView.position.height - CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT);

                if(_currentCursor != MouseCursor.Arrow)
                {
                    EditorGUIUtility.AddCursorRect(windowRect, _currentCursor);
                }
            }

            if(_lastLineMeshGeneration != InternalCSGModelManager.MeshGeneration)
            {
                _forceWireframeUpdate   = true;
                _lastLineMeshGeneration = InternalCSGModelManager.MeshGeneration;
            }

            if(_forceWireframeUpdate)
            {
                _zTestLineMeshManager2.Clear();
                _noZTestLineMeshManager2.Clear();

                if(clipPlaneValid && backupData.Length > 0)
                {
                    List<GeometryWireframe> wireframes      = new List<GeometryWireframe>(backupData.Length);
                    List<Matrix4x4>         transformations = new List<Matrix4x4>(backupData.Length);

                    for(int i = 0; i < backupData.Length; i++)
                    {
                        CSGModel model = backupData[i].model;

                        if(!model)
                        {
                            continue;
                        }

                        CSGBrush brush = backupData[i].brush;

                        if(!brush)
                        {
                            continue;
                        }

                        Matrix4x4 brushTransformation = backupData[i].localToWorldMatrix;
                        wireframes.Add(backupData[i].wireFrame);
                        transformations.Add(brushTransformation);
                    }

                    if(wireframes.Count > 0)
                    {
                        CSGRenderer.DrawSelectedBrushes(_zTestLineMeshManager2, _noZTestLineMeshManager2, wireframes.ToArray(), transformations.ToArray(), ColorSettings.SelectedOutlines, GUIConstants.THICK_LINE_SCALE);
                    }
                }

                _zTestLineMeshManager1.Clear();
                _noZTestLineMeshManager1.Clear();

                if(clipBrushes != null)
                {
                    List<int>       ids             = new List<int>(clipBrushes.Length);
                    List<Matrix4x4> transformations = new List<Matrix4x4>(clipBrushes.Length);

                    for(int i = 0; i < clipBrushes.Length; i++)
                    {
                        CSGBrush brush = clipBrushes[i];

                        if(!brush)
                        {
                            continue;
                        }

                        if(brush.ChildData == null || brush.ChildData.ModelTransform == null)
                        {
                            continue;
                        }

                        Matrix4x4 brushTransformation = brush.compareTransformation.localToWorldMatrix;
                        ids.Add(brush.brushNodeID);
                        transformations.Add(brushTransformation);
                    }

                    if(ids.Count > 0)
                    {
                        CSGRenderer.DrawSelectedBrushes(_zTestLineMeshManager1, _noZTestLineMeshManager1, ids.ToArray(), transformations.ToArray(), ColorSettings.SelectedOutlines, GUIConstants.THICK_LINE_SCALE);
                    }
                }
            }

            _forceWireframeUpdate = false;

            MaterialUtility.LineAlphaMultiplier     = 1.0f;
            MaterialUtility.LineDashMultiplier      = 2.0f;
            MaterialUtility.LineThicknessMultiplier = 2.0f;

            _noZTestLineMeshManager2.Render(MaterialUtility.NoZTestGenericLine);
            _noZTestLineMeshManager1.Render(MaterialUtility.NoZTestGenericLine);

            MaterialUtility.LineThicknessMultiplier = 1.0f;

            _zTestLineMeshManager2.Render(MaterialUtility.ZTestGenericLine);
            _zTestLineMeshManager1.Render(MaterialUtility.ZTestGenericLine);

            if(clipPlaneValid && pointsUsed == 3)
            {
                Vector3 pop;
                Vector3 xDir = clipPlaneTangent * 1.5f;
                Vector3 yDir = clipPlaneBinormal * 1.5f;

                if(backupData.Length > 0)
                {
                    pop = GeometryUtility.ProjectPointOnPlane(clipPlane, boundsCenter);
                    float sizeX, sizeY;

                    switch(GeometryUtility.GetPrincipleAxis(xDir))
                    {
                        default:
                        case PrincipleAxis.X: sizeX = bounds.MaxX - bounds.MinX; break;
                        case PrincipleAxis.Y: sizeX = bounds.MaxY - bounds.MinY; break;
                        case PrincipleAxis.Z: sizeX = bounds.MaxZ - bounds.MinZ; break;
                    }

                    xDir *= Mathf.Max(100, sizeX);

                    switch(GeometryUtility.GetPrincipleAxis(yDir))
                    {
                        default:
                        case PrincipleAxis.X: sizeY = bounds.MaxX - bounds.MinX; break;
                        case PrincipleAxis.Y: sizeY = bounds.MaxY - bounds.MinY; break;
                        case PrincipleAxis.Z: sizeY = bounds.MaxZ - bounds.MinZ; break;
                    }

                    yDir *= Mathf.Max(100, sizeY);
                }
                else
                {
                    pop  =  points[0];
                    xDir *= 100.0f;
                    yDir *= 100.0f;
                }

                Vector3[] polygonPoints = { pop - xDir - yDir, pop + xDir - yDir, pop + xDir + yDir, pop - xDir + yDir };

                PaintUtility.DrawPolygon(polygonPoints, ColorSettings.ClipPlaneFill);
                PaintUtility.DrawWirePolygon(polygonPoints, ColorSettings.ClipPlaneOutline);
            }
        }

        public void HandleCreatePointEvents(SceneView sceneView, Rect sceneRect)
        {
            switch(Event.current.type)
            {
                case EventType.MouseDrag:
                {
                    if(!sceneRect.Contains(Event.current.mousePosition))
                    {
                        break;
                    }

                    if(GUIUtility.hotControl == 0 && Event.current.button == 0)
                    {
                        if(Event.current.modifiers == EventModifiers.None)
                        {
                            RememberTargetData();
                            GUIUtility.hotControl             = _planeCreationID;
                            GUIUtility.keyboardControl        = _planeCreationID;
                            EditorGUIUtility.editingTextField = false;

                            _visualSnappedLines = null;
                            _commitOnClick      = false;

                            editMode    = EditMode.None;
                            pointsUsed  = 0;
                            pointsShown = 0;

                            if(pointsUsed != 3)
                            {
                                ResetEditMode();
                            }

                            RestoreTargetData(true);
                            UpdateMouseIntersection(sceneView);
                        }
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl == 0 && Event.current.button == 0)
                    {
                        Event.current.Use();

                        if(!_mouseDragged)
                        {
                            SelectionUtility.DoSelectionClick(sceneView);
                        }
                    }

                    break;
                }
                case EventType.Repaint:
                {
                    if(SceneDragToolManager.IsDraggingObjectInScene)
                    {
                        break;
                    }

                    OnPaint(sceneView);

                    if(_currentMousePoint.HasValue)
                    {
                        Vector3 intersectionPoint = _currentMousePoint.Value;

                        Matrix4x4 prevMatrix = Handles.matrix;
                        Handles.matrix = MathConstants.identityMatrix;
                        float handleSize = CSGHandleUtility.GetHandleSize(intersectionPoint);
                        Handles.color = ColorSettings.PointInnerStateColor[3];

                        if(_currentOnBrush == null)
                        {
                            PaintUtility.DiamondDotCap(intersectionPoint, handleSize * 1.2f * GUIConstants.HANDLE_SCALE);
                        }
                        else
                        {
                            PaintUtility.SquareDotCap(-1, intersectionPoint, MathConstants.identityQuaternion, handleSize * GUIConstants.HANDLE_SCALE);
                        }

                        Handles.matrix = prevMatrix;
                    }

                    if(_visualSnappedLines != null)
                    {
                        PaintUtility.DrawLines(_visualSnappedLines.ToArray(), GUIConstants.OLD_THIN_LINE_SCALE * 2.0f, ColorSettings.SnappedEdges);
                    }

                    break;
                }
                case EventType.MouseMove:
                {
                    MouseCursor prevCursor = _currentCursor;
                    UpdateMouseCursor();
                    UpdateMouseIntersection(sceneView);

                    if(_currentCursor != MouseCursor.ArrowPlus && clipBrushes != null && _currentOnBrush && !ArrayUtility.Contains(clipBrushes, _currentOnBrush))
                    {
                        _currentCursor = MouseCursor.ArrowPlus;
                    }

                    if(prevCursor != _currentCursor)
                    {
                        CSGEditorGUIUtility.RepaintAll();
                    }

                    break;
                }
                case EventType.ValidateCommand:
                case EventType.KeyDown:
                {
                    if(GUIUtility.hotControl == 0)
                    {
                        if(Keys.cycleClipModes.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();
                        }
                    }

                    break;
                }

                case EventType.KeyUp:
                {
                    if(GUIUtility.hotControl == 0)
                    {
                        if(Keys.cycleClipModes.IsKeyPressed())
                        {
                            CycleClipMode();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();
                        }
                    }

                    break;
                }
            }

            EventType type = Event.current.GetTypeForControl(_planeCreationID);

            switch(type)
            {
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl == _planeCreationID && Event.current.button == 0)
                    {
                        GUIUtility.hotControl             = 0;
                        GUIUtility.keyboardControl        = 0;
                        EditorGUIUtility.editingTextField = false;
                        Event.current.Use();

                        _visualSnappedLines = null;

                        if(!_mouseDragged)
                        {
                            SelectionUtility.DoSelectionClick(sceneView);

                            break;
                        }

                        if(pointsUsed == 3 && _currentMousePoint.HasValue)
                        {
                            editMode = EditMode.EditPoints;
                        }
                        else
                        {
                            ResetEditMode();
                        }
                    }

                    break;
                }

                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == _planeCreationID)
                    {
                        if(_commitOnClick)
                        {
                            ResetEditMode();
                            _commitOnClick = false;
                        }

                        Event.current.Use();

                        if(pointsUsed == 0)
                        {
                            CreateFirstPoint(sceneView);
                        }

                        if(pointsUsed != 0)
                        {
                            RestoreTargetData(true);
                        }

                        UpdateMouseIntersection(sceneView);

                        if(_currentMousePoint.HasValue)
                        {
                            points[0] = _currentMousePoint.Value;

                            if(editMode == EditMode.CreatingPoint2)
                            {
                                pointsUsed  = 3;
                                pointsShown = 2;
                            }
                            else
                            {
                                pointsUsed  = 1;
                                pointsShown = 1;
                            }
                        }
                        else
                        {
                            if(editMode == EditMode.CreatingPoint2)
                            {
                                pointsUsed  = 2;
                                pointsShown = 2;
                            }
                            else
                            {
                                pointsUsed  = 0;
                                pointsShown = 0;
                            }
                        }

                        if(editMode == EditMode.CreatingPoint1)
                        {
                            float handleSize = HandleUtility.GetHandleSize(points[0]);
                            float magnitude  = (points[0] - points[1]).magnitude;

                            if(magnitude > handleSize * (GUIConstants.HANDLE_SCALE / 10.0f))
                            {
                                editMode    = EditMode.CreatingPoint2;
                                pointsUsed  = 3;
                                pointsShown = 2;
                            }
                        }

                        if(pointsUsed == 3)
                        {
                            UpdateClipPlane();
                        }

                        CSGEditorGUIUtility.RepaintAll();
                    }

                    break;
                }

                case EventType.ValidateCommand:
                case EventType.KeyDown:
                {
                    if(GUIUtility.hotControl == _planeCreationID)
                    {
                        if(Keys.cancelActionKey.IsKeyPressed() || Keys.performActionKey.IsKeyPressed() || Keys.cycleClipModes.IsKeyPressed())
                        {
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();
                        }
                    }

                    break;
                }

                case EventType.KeyUp:
                {
                    if(GUIUtility.hotControl == _planeCreationID)
                    {
                        if(Keys.cancelActionKey.IsKeyPressed())
                        {
                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();
                            SelectionUtility.DeselectAll();

                            break;
                        }

                        if(Keys.performActionKey.IsKeyPressed())
                        {
                            GUIUtility.hotControl             = 0;
                            GUIUtility.keyboardControl        = 0;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();
                            Cancel();

                            break;
                        }

                        if(Keys.cycleClipModes.IsKeyPressed())
                        {
                            CycleClipMode();
                            Event.current.Use();

                            break;
                        }

                        if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
                        {
                            Event.current.Use();
                        }
                    }

                    break;
                }
                case EventType.Repaint:
                {
                    if(SceneDragToolManager.IsDraggingObjectInScene)
                    {
                        break;
                    }

                    if(GUIUtility.hotControl == _planeCreationID)
                    {
                        if(_visualSnappedLines != null)
                        {
                            PaintUtility.DrawLines(_visualSnappedLines.ToArray(), GUIConstants.OLD_THIN_LINE_SCALE * 2.0f, ColorSettings.SnappedEdges);
                        }

                        if(editMode != EditMode.CreatingPoint1)
                        {
                            Matrix4x4 prevMatrix = Handles.matrix;
                            Handles.matrix = MathConstants.identityMatrix;

                            for(int i = 0; i < pointsShown; i++)
                            {
                                float handleSize = CSGHandleUtility.GetHandleSize(points[i]);

                                Handles.color = i == 0 ? ColorSettings.PointInnerStateColor[3] : ColorSettings.PointInnerStateColor[0];

                                PaintUtility.SquareDotCap(-1, points[i], MathConstants.identityQuaternion, handleSize * GUIConstants.HANDLE_SCALE);
                            }

                            Handles.matrix = prevMatrix;
                        }

                        if(_visualSnappedLines != null)
                        {
                            PaintUtility.DrawLines(_visualSnappedLines.ToArray(), GUIConstants.OLD_THIN_LINE_SCALE * 2.0f, ColorSettings.SnappedEdges);
                        }
                    }

                    break;
                }
            }
        }

        private void CycleClipMode()
        {
            ClipMode newClipMode = (ClipMode)((int)clipMode + 1);

            if(newClipMode > ClipMode.Split)
            {
                newClipMode = ClipMode.RemovePositive;
            }

            SetClipMode(newClipMode);
        }

        public void HandleEditPointEvents(SceneView sceneView, Rect sceneRect)
        {
            Camera camera       = sceneView.camera;
            bool   assume2DView = CSGSettings.Assume2DView(camera);

            int[]   ids   = new int[3];
            float[] sizes = new float[3];

            for(int i = 0; i < 3; i++)
            {
                sizes[i] = CSGHandleUtility.GetHandleSize(points[i]) * GUIConstants.HANDLE_SCALE;
                ids[i]   = GUIUtility.GetControlID(SnappedPoint.snappedPointHash, FocusType.Keyboard);
            }

            Matrix4x4 prevMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            try
            {
                switch(Event.current.type)
                {
                    case EventType.Layout:
                    {
                        for(int i = 0; i < pointsShown; i++)
                        {
                            HandleUtility.AddControl(ids[i], HandleUtility.DistanceToCircle(points[i], sizes[i] * 1.2f));
                        }

                        break;
                    }
                    case EventType.MouseDown:
                    {
                        if(!sceneRect.Contains(Event.current.mousePosition))
                        {
                            break;
                        }

                        if(GUIUtility.hotControl == 0 && Event.current.button == 0)
                        {
                            _visualSnappedLines = null;

                            for(int i = 0; i < pointsShown; i++)
                            {
                                if(HandleUtility.nearestControl == ids[i])
                                {
                                    Undo.RegisterCompleteObjectUndo(this, "Updated clip plane");

                                    movePlane = CSGGrid.CurrentGridPlane;
                                    movePlane = new CSGPlane(movePlane.normal, points[i]);

                                    GUIUtility.hotControl             = ids[i];
                                    GUIUtility.keyboardControl        = ids[i];
                                    EditorGUIUtility.editingTextField = false;
                                    EditorGUIUtility.SetWantsMouseJumping(1);
                                    Event.current.Use();

                                    break;
                                }
                            }
                        }

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(GUIUtility.hotControl == 0 && Event.current.button == 0)
                        {
                            if((camera == null || !assume2DView) && pointsShown == 2 && _currentMousePoint.HasValue && Event.current.modifiers == EventModifiers.None)
                            {
                                pointsShown = 3;
                                Vector3 intersectionPoint = _currentMousePoint.Value;

                                if(camera != null && assume2DView)
                                {
                                    intersectionPoint = new CSGPlane(camera.transform.forward, points[0]).Project(intersectionPoint);
                                }

                                points[2] = intersectionPoint;
                            }
                            else if(!_mouseDragged)
                            {
                                SelectionUtility.DoSelectionClick(sceneView);
                            }
                        }

                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        if(GUIUtility.hotControl == 0 && Event.current.button == 0)
                        {
                            ResetEditMode();
                            Cancel();
                        }

                        break;
                    }
                    case EventType.MouseMove:
                    {
                        UpdateMouseCursor();
                        _visualSnappedLines = null;

                        if(GUIUtility.hotControl == 0)
                        {
                            RestoreTargetData(true);
                            UpdateMouseIntersection(sceneView);
                            UpdateClipPlane();
                        }
                        else
                        {
                            UpdateMouseIntersection(sceneView);
                        }

                        break;
                    }
                    case EventType.ValidateCommand:
                    case EventType.KeyDown:
                    {
                        if(GUIUtility.hotControl == 0)
                        {
                            if(Keys.cancelActionKey.IsKeyPressed() || Keys.performActionKey.IsKeyPressed() || Keys.cycleClipModes.IsKeyPressed())
                            {
                                Event.current.Use();

                                break;
                            }

                            if(Keys.HandleSceneKeyDown(EditModeManager.CurrentTool, false))
                            {
                                Event.current.Use();
                            }
                        }

                        break;
                    }
                    case EventType.KeyUp:
                    {
                        if(GUIUtility.hotControl == 0)
                        {
                            if(Keys.cancelActionKey.IsKeyPressed())
                            {
                                SelectionUtility.DeselectAll();
                                Event.current.Use();

                                break;
                            }

                            if(Keys.performActionKey.IsKeyPressed())
                            {
                                Commit();
                                Event.current.Use();

                                break;
                            }

                            if(Keys.cycleClipModes.IsKeyPressed())
                            {
                                CycleClipMode();
                                Event.current.Use();

                                break;
                            }

                            if(Keys.HandleSceneKeyUp(EditModeManager.CurrentTool, false))
                            {
                                Event.current.Use();
                            }
                        }

                        break;
                    }
                    case EventType.Repaint:
                    {
                        if(SceneDragToolManager.IsDraggingObjectInScene)
                        {
                            break;
                        }

                        OnPaint(sceneView);

                        Color temp = Handles.color;

                        for(int i = 0; i < pointsShown; i++)
                        {
                            bool isSelected = ids[i] == GUIUtility.keyboardControl || ids[i] == GUIUtility.hotControl;

                            Handles.color = isSelected ? Handles.selectedColor : Color.gray;
                            PaintUtility.SquareDotCap(ids[i], points[i], camera.transform.rotation, sizes[i]);
                        }

                        Handles.color = temp;

                        if((camera == null || !assume2DView) && pointsShown == 2 && GUIUtility.hotControl == 0)
                        {
                            Vector3 intersectionPoint = _currentMousePoint.Value;

                            if(camera != null && assume2DView)
                            {
                                intersectionPoint = new CSGPlane(camera.transform.forward, points[0]).Project(intersectionPoint);
                            }

                            Matrix4x4 tempMatrix = Handles.matrix;
                            Handles.matrix = MathConstants.identityMatrix;
                            float handleSize = CSGHandleUtility.GetHandleSize(intersectionPoint);
                            Handles.color = ColorSettings.PointInnerStateColor[3];

                            PaintUtility.SquareDotCap(-1, intersectionPoint, MathConstants.identityQuaternion, handleSize * GUIConstants.HANDLE_SCALE);

                            Handles.matrix = tempMatrix;
                        }

                        if(_visualSnappedLines != null)
                        {
                            PaintUtility.DrawLines(_visualSnappedLines.ToArray(), GUIConstants.OLD_THIN_LINE_SCALE * 2.0f, ColorSettings.SnappedEdges);
                        }

                        break;
                    }
                }

                for(int i = 0; i < pointsShown; i++)
                {
                    EventType type = Event.current.GetTypeForControl(ids[i]);

                    switch(type)
                    {
                        case EventType.MouseUp:
                        {
                            if(GUIUtility.hotControl == ids[i] && Event.current.button == 0)
                            {
                                GUIUtility.hotControl             = 0;
                                GUIUtility.keyboardControl        = 0;
                                EditorGUIUtility.editingTextField = false;
                                Event.current.Use();
                                EditorGUIUtility.SetWantsMouseJumping(0);

                                _visualSnappedLines = null;
                            }

                            break;
                        }

                        case EventType.MouseDrag:
                        {
                            if(GUIUtility.hotControl == ids[i])
                            {
                                Vector3 newPoint = HandleMovement(sceneView);

                                if(camera != null && assume2DView)
                                {
                                    newPoint = new CSGPlane(camera.transform.forward, points[i]).Project(newPoint);
                                }

                                Vector3 delta = newPoint - points[i];

                                if(pointsShown != 3 && i == 1)
                                {
                                    points[2] += delta;
                                }

                                points[i] += delta;

                                GUI.changed = true;
                                Event.current.Use();
                                UpdateClipPlane();
                            }

                            break;
                        }
                    }
                }
            }
            finally
            {
                Handles.matrix = prevMatrix;
            }
        }

        private static Vector2 m_ScrollPos;

        public void OnInspectorGUI(EditorWindow window, float height)
        {
            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            EditModeClipGUI.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        public Rect GetLastSceneGUIRect()
        {
            return EditModeClipGUI.GetLastSceneGUIRect();
        }

        public bool OnSceneGUI(Rect windowRect)
        {
            EditModeClipGUI.OnSceneGUI(this);

            return true;
        }
    }
}

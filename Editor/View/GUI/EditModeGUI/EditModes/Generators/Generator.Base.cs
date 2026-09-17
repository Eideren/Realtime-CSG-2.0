using System;
using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal abstract class GeneratorBase : ScriptableObject
    {
        protected const float HOVER_TEXT_DISTANCE = 25.0f;

        internal static readonly int shapeBuilderShapeHash = "CSGShapeBuilderShape".GetHashCode();
        internal static readonly int shapeBuilderCenterHash = "CSGShapeBuilderCenter".GetHashCode();
        internal static readonly int shapeBuilderPointHash = "CSGShapeBuilderPoint".GetHashCode();
        internal static readonly int shapeBuilderBoxHash = "CSGShapeBuilderBox".GetHashCode();
        internal static readonly int shapeBuilderTangentHash = "CSGShapeBuilderTangent".GetHashCode();
        internal static readonly int shapeEditIDHash = "shapeEditID".GetHashCode();

        protected virtual bool IgnoreForcedGridForTangents
        {
            get => false;
        }

        public delegate Vector3 SnapToGridFunction(Camera camera, Vector3 worldPosition, CSGPlane plane, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush, CSGBrush[] ignoreBrushes, bool ignoreAllBrushes = false);

        public delegate Vector3 SnapToRayFunction(Camera camera, Vector3 worldPosition, Ray ray, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush);

        [NonSerialized] internal SnapToGridFunction snapFunction = null;
        [NonSerialized] internal SnapToRayFunction raySnapFunction = null;
        [NonSerialized] internal Action shapeCommitted = null;
        [NonSerialized] internal Action shapeCancelled = null;

        private const CSGOperationType INVALID_CSG_OPERATION_TYPE = (CSGOperationType)99;

        [Serializable]
        protected enum EditMode
        {
            CreatePlane
          , CreateShape
          , EditShape
          , ExtrudeShape
        }

        [SerializeField] protected EditMode editMode = EditMode.CreatePlane;
        [SerializeField] protected EditMode prevEditMode = EditMode.CreatePlane;

        protected bool IgnoreDepthForRayCasts(Camera camera)
        {
            return CSGSettings.Assume2DView(camera);
        }

        public bool CanCommit
        {
            get => editMode != EditMode.CreatePlane;
        }

        public bool HaveBrushes
        {
            get => editMode != EditMode.CreatePlane;
        }

        protected abstract IShapeSettings ShapeSettings { get; }

        [SerializeField] protected CSGOperationType currentCSGOperationType = INVALID_CSG_OPERATION_TYPE;
        [SerializeField] protected CSGOperationType forceCurrentCSGOperationType = INVALID_CSG_OPERATION_TYPE;

        [SerializeField] protected Transform parentTransform;
        [SerializeField] protected GameObject parentGameObject;
        [SerializeField] protected CSGModel parentModel;
        [SerializeField] protected GameObject operationGameObject;
        [NonSerialized] protected CSGModel geometryModel;

        [SerializeField] protected GameObject[] generatedGameObjects;
        [SerializeField] protected CSGBrush[] generatedBrushes;

        [NonSerialized] protected int undoGroupIndex;

        [NonSerialized] protected List<Vector3> visualSnappedEdges; // used to visualize edges which we're snapping against
        [NonSerialized] protected List<Vector3> visualSnappedGrid; // used to visualize grid lines we're snapping against
        [NonSerialized] protected CSGBrush visualSnappedBrush; // used to visualize the brush we're snapping on

        [NonSerialized] protected CSGPlane buildPlane = new CSGPlane(0, 1, 0, 0);

        [SerializeField] protected Object[] prevSelection;
        [NonSerialized] protected bool planeOnGeometry = false;
        [NonSerialized] protected bool shapeIsValid = true;
        [NonSerialized] protected bool isFinished;
        [NonSerialized] protected bool mouseIsDragging;
        [NonSerialized] protected bool ignoreOrbit = false;
        [NonSerialized] private ViewTool _previousViewTool = ViewTool.Pan;

        [NonSerialized] private Quaternion _originalGridRotation;
        [NonSerialized] private Vector3 _originalGridCenter;
        [NonSerialized] private bool _originalForceGrid;

        [SerializeField] protected Vector3 brushPosition;
        protected Quaternion? brushRotation;
        [SerializeField] protected bool ignoreModelRotation;

        [NonSerialized] protected int shapeEditId;
        [NonSerialized] protected int shapeId;

        [SerializeField] protected Vector3 gridNormal;
        [SerializeField] protected Vector3 gridTangent;
        [SerializeField] protected Vector3 gridBinormal;
        [SerializeField] protected Matrix4x4 toGridMatrix = Matrix4x4.identity;
        [SerializeField] protected Matrix4x4 fromGridMatrix = Matrix4x4.identity;
        [SerializeField] protected Quaternion toGridQuaternion = Quaternion.identity;
        [SerializeField] protected Quaternion fromGridQuaternion = Quaternion.identity;

        public static Material MatchedMaterial = null;

        public static Vector2 MatchedUVScale = Vector2.one;

        public CSGOperationType CurrentCSGOperationType
        {
            get => forceCurrentCSGOperationType == INVALID_CSG_OPERATION_TYPE ? currentCSGOperationType : forceCurrentCSGOperationType;
            set
            {
                if (forceCurrentCSGOperationType == value)
                {
                    return;
                }

                forceCurrentCSGOperationType = value;
                UpdateOperationType(value);
            }
        }

        protected void UpdateOperationType(CSGOperationType opType)
        {
            if (operationGameObject != null)
            {
                CSGOperation operation = operationGameObject.GetComponent<CSGOperation>();

                if (operation != null)
                {
                    operation.operationType = opType;
                }

                CSGBrush brush = operationGameObject.GetComponent<CSGBrush>();

                if (brush != null)
                {
                    brush.operationType = opType;
                }
            }

            InternalCSGModelManager.CheckForChanges();
            EditorApplication.RepaintHierarchyWindow();
        }

        public virtual void Init()
        {
            prevSelection = new List<Object>(Selection.objects).ToArray();

            Reset();

            shapeIsValid = true;
            isFinished = false;

            _originalGridRotation = CSGGrid.ForcedGridRotation;
            _originalGridCenter = CSGGrid.ForcedGridCenter;
            _originalForceGrid = CSGGrid.ForceGrid;

            geometryModel = null;
        }

        public virtual void Reset()
        {
            editMode = EditMode.CreatePlane;
            brushPosition = MathConstants.zeroVector3;
            brushRotation = null;
            ignoreModelRotation = false;
            parentGameObject = null;
            parentModel = null;
            operationGameObject = null;
            parentTransform = null;
            generatedGameObjects = null;
            generatedBrushes = null;
            undoGroupIndex = -1;
            forceCurrentCSGOperationType = INVALID_CSG_OPERATION_TYPE;
            currentCSGOperationType = INVALID_CSG_OPERATION_TYPE;
        }

        protected virtual bool StartEditMode(Camera camera)
        {
            if (editMode == EditMode.EditShape || editMode == EditMode.ExtrudeShape)
            {
                return false;
            }

            Undo.RecordObject(this, "Created shape");

            if (GUIUtility.hotControl == shapeId)
            {
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
                EditorGUIUtility.editingTextField = false;
            }

            CalculateWorldSpaceTangents(camera);
            brushPosition = buildPlane.Project(ShapeSettings.GetCenter(buildPlane));
            editMode = EditMode.EditShape;

            CSGPlane newPlane = buildPlane;
            ShapeSettings.CalculatePlane(ref newPlane);

            if (newPlane.Normal.sqrMagnitude != 0)
            {
                buildPlane = newPlane;
            }

            if (ModelTraits.IsModelEditable(geometryModel))
            {
                SelectionUtility.LastUsedModel = geometryModel;
            }

            UpdateBaseShape();

            return true;
        }

        protected void RevertToEditVertices()
        {
            if (editMode != EditMode.ExtrudeShape)
            {
                return;
            }

            editMode = EditMode.EditShape;
        }

        protected void CleanupGrid()
        {
            CSGGrid.ForcedGridRotation = _originalGridRotation;
            CSGGrid.ForcedGridCenter = _originalGridCenter;
            CSGGrid.ForceGrid = _originalForceGrid;
        }

        protected void ResetVisuals()
        {
            visualSnappedEdges = null;
            visualSnappedGrid = null;
            visualSnappedBrush = null;
        }

        protected void PaintSnapVisualisation()
        {
            if (visualSnappedEdges != null)
            {
                PaintUtility.DrawLines(visualSnappedEdges.ToArray(), GUIConstants.OLD_THICK_LINE_SCALE, ColorSettings.SnappedEdges);
            }

            Matrix4x4 origMatrix = Handles.matrix;
            Handles.matrix = MathConstants.identityMatrix;

            if (visualSnappedGrid != null)
            {
                PaintUtility.DrawDottedLines(visualSnappedGrid.ToArray(), ColorSettings.SnappedEdges);
            }

            if (visualSnappedBrush != null)
            {
                if (visualSnappedBrush.compareTransformation != null && visualSnappedBrush.childData != null && visualSnappedBrush.childData.modelTransform)
                {
                    Color color = ColorSettings.HoverOutlines;
                    Matrix4x4 brushTransformation = visualSnappedBrush.compareTransformation.localToWorldMatrix;

                    CSGRenderer.DrawOutlines(visualSnappedBrush.brushNodeID, brushTransformation, color, color, color, color, GUIConstants.OLD_THICK_LINE_SCALE);
                }
            }

            Handles.matrix = origMatrix;
        }

        protected void SetBrushTransformation(Transform transform)
        {
            Vector3 direction = buildPlane.Normal;
            Quaternion rotation = brushRotation.HasValue ? brushRotation.Value : Quaternion.FromToRotation(MathConstants.upVector3, direction);

            if (!ignoreModelRotation)
            {
                rotation *= Quaternion.Inverse(parentModel.transform.rotation);
            }

            transform.rotation = rotation;
            transform.position = brushPosition;
        }

        protected void UpdateBrushPosition()
        {
            if (operationGameObject)
            {
                Transform transform = operationGameObject.transform;
                SetBrushTransformation(transform);
            }
        }

        protected virtual void CreateControlIDs()
        {
            shapeEditId = GUIUtility.GetControlID(shapeEditIDHash, FocusType.Passive);
            HandleUtility.AddDefaultControl(shapeEditId);

            shapeId = GUIUtility.GetControlID(shapeBuilderShapeHash, FocusType.Passive);
        }

        protected void HandleKeyboard(EventType type)
        {
            if (EditorGUIUtility.editingTextField)
            {
                return;
            }

            Camera camera = Camera.current;

            switch (type)
            {
                case EventType.ValidateCommand:
                case EventType.KeyDown:
                    {
                        SceneQueryUtility.ClearDeepClick();

                        if (Keys.performActionKey.IsKeyPressed() || Keys.deleteSelectionKey.IsKeyPressed() || Keys.cancelActionKey.IsKeyPressed())
                        {
                            Event.current.Use();
                        }

                        break;
                    }
                case EventType.KeyUp:
                    {
                        if (Keys.performActionKey.IsKeyPressed())
                        {
                            Commit(camera);
                            Event.current.Use();
                        }
                        else if (Keys.cancelActionKey.IsKeyPressed())
                        {
                            PerformDeselectAll();
                            Event.current.Use();
                        }
                        else if (Keys.deleteSelectionKey.IsKeyPressed())
                        {
                            PerformDelete();
                            Event.current.Use();
                        }

                        break;
                    }
            }
        }

        protected void PaintSideLength(Camera camera, Vector3 edgeCenter, Vector3 circleCenter, float length, string name)
        {
            Vector2 textCenter2Da = HandleUtility.WorldToGUIPoint(circleCenter);
            Vector2 textCenter2Db = HandleUtility.WorldToGUIPoint(edgeCenter);
            Vector2 normal2D = (textCenter2Db - textCenter2Da).normalized;

            Vector2 textCenter2D = textCenter2Db;
            textCenter2D += normal2D * (HOVER_TEXT_DISTANCE * 2);

            Ray textCenterRay = HandleUtility.GUIPointToWorldRay(textCenter2D);
            Vector3 textCenter = textCenterRay.origin + textCenterRay.direction * ((camera.farClipPlane + camera.nearClipPlane) * 0.5f);

            PaintUtility.DrawLine(edgeCenter, textCenter, Color.black);
            PaintUtility.DrawDottedLine(edgeCenter, textCenter, ColorSettings.SnappedEdges);

            if (float.IsNaN(length) || float.IsInfinity(length))
            {
                PaintUtility.DrawScreenText(textCenter2D, $"{name} --");
            }
            else
            {
                PaintUtility.DrawScreenText(textCenter2D, name + Units.ToRoundedDistanceString(length));
            }
        }

        protected void CalculateWorldSpaceTangents(Camera camera)
        {
            bool hadForcedGrid = false;

            if (IgnoreForcedGridForTangents)
            {
                hadForcedGrid = CSGGrid.ForceGrid;
            }

            if (hadForcedGrid)
            {
                CSGGrid.ForceGrid = false;
                CSGGrid.UpdateGridOrientation(camera);
            }

            Vector3 gridSpaceNormal = CSGGrid.VectorToGridSpace(buildPlane.Normal);

            GeometryUtility.CalculateTangents(gridSpaceNormal, out Vector3 gridSpaceTangent, out Vector3 gridSpaceBinormal);

            gridNormal = CSGGrid.VectorFromGridSpace(gridSpaceNormal);
            gridTangent = CSGGrid.VectorFromGridSpace(gridSpaceTangent);
            gridBinormal = CSGGrid.VectorFromGridSpace(gridSpaceBinormal);

            toGridMatrix = CSGGrid.ToGridSpaceMatrix();
            fromGridMatrix = CSGGrid.FromGridSpaceMatrix();
            toGridQuaternion = CSGGrid.ToGridSpaceQuaternion();
            fromGridQuaternion = CSGGrid.FromGridSpaceQuaternion();

            if (hadForcedGrid)
            {
                CSGGrid.ForceGrid = true;
                CSGGrid.UpdateGridOrientation(camera);
            }
        }

        protected void HideGenerateBrushes()
        {
            if (generatedBrushes != null)
            {
                for (int i = 0; i < generatedBrushes.Length; i++)
                {
                    if (generatedBrushes[i] && generatedBrushes[i].gameObject && generatedBrushes[i].gameObject.activeSelf)
                    {
                        generatedBrushes[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        protected bool GenerateBrushObjects(int brushObjectCount, bool inGridSpace = true)
        {
            Undo.IncrementCurrentGroup();
            undoGroupIndex = Undo.GetCurrentGroup();

            CSGModel lastUsedModel = SelectionUtility.LastUsedModel;

            if (!ModelTraits.IsModelEditable(lastUsedModel))
            {
                lastUsedModel = null;
            }

            Transform lastUsedModelTransform = !lastUsedModel ? null : lastUsedModel.transform;

            if (!lastUsedModelTransform || !lastUsedModel.isActiveAndEnabled)
            {
                if (prevSelection != null && prevSelection.Length > 0)
                {
                    for (int i = 0; i < prevSelection.Length; i++)
                    {
                        Object obj = prevSelection[i];
                        CSGBrush brush = obj as CSGBrush;
                        MonoBehaviour mono = obj as MonoBehaviour;
                        GameObject go = obj as GameObject;

                        if (!brush)
                        {
                            if (mono)
                            {
                                brush = mono.GetComponentInChildren<CSGBrush>();
                            }

                            if (go)
                            {
                                brush = go.GetComponentInChildren<CSGBrush>();
                            }
                        }

                        if (!brush)
                        {
                            continue;
                        }

                        if ((brush.gameObject.hideFlags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) != 0)
                        {
                            continue;
                        }

                        if (brush.childData == null || brush.childData.modelTransform == null)
                        {
                            continue;
                        }

                        CSGModel model = brush.childData.model;

                        if (!model || !model.isActiveAndEnabled)
                        {
                            continue;
                        }

                        lastUsedModelTransform = brush.childData.modelTransform;

                        break;
                    }
                }
            }

            if (generatedBrushes != null && generatedBrushes.Length > 0)
            {
                for (int i = generatedBrushes.Length - 1; i >= 0; i--)
                {
                    if (generatedBrushes[i])
                    {
                        continue;
                    }

                    ArrayUtility.RemoveAt(ref generatedBrushes, i);
                }

                for (int i = generatedGameObjects.Length - 1; i >= 0; i--)
                {
                    if (generatedGameObjects[i])
                    {
                        CSGBrush brush = generatedGameObjects[i].GetComponentInChildren<CSGBrush>();

                        if (brush && ArrayUtility.Contains(generatedBrushes, brush))
                        {
                            continue;
                        }
                    }

                    ArrayUtility.RemoveAt(ref generatedGameObjects, i);
                }
            }

            if (generatedGameObjects == null || generatedGameObjects.Length != brushObjectCount)
            {
                if (generatedBrushes != null && generatedBrushes.Length > 0)
                {
                    for (int i = 0; i < generatedBrushes.Length; i++)
                    {
                        InternalCSGModelManager.OnDestroyed(generatedBrushes[i]);
                        DestroyImmediate(generatedBrushes[i]);
                    }
                }

                if (generatedGameObjects != null && generatedGameObjects.Length > 0)
                {
                    for (int i = 0; i < generatedGameObjects.Length; i++)
                    {
                        DestroyImmediate(generatedGameObjects[i]);
                    }
                }

                if (parentGameObject != null)
                {
                    DestroyImmediate(parentGameObject);
                }

                if (lastUsedModelTransform == null)
                {
                    parentGameObject = OperationsUtility.CreateGameObject(lastUsedModelTransform, "Model", true);
                    InternalCSGModelManager.CreateCSGModel(parentGameObject);
                    parentModel = parentGameObject.GetComponent<CSGModel>();
                    Undo.RegisterCreatedObjectUndo(parentGameObject, "Created model");

                    if (brushObjectCount > 1)
                    {
                        operationGameObject = OperationsUtility.CreateGameObject(parentGameObject.transform, "Operation", true);

                        Transform transform = operationGameObject.transform;
                        SetBrushTransformation(transform);

                        CSGOperation operation = operationGameObject.AddComponent<CSGOperation>();

                        if (CurrentCSGOperationType != INVALID_CSG_OPERATION_TYPE)
                        {
                            operation.operationType = CurrentCSGOperationType;
                        }

                        operation.handleAsOne = true;
                        Undo.RegisterCreatedObjectUndo(operationGameObject, "Created operation");
                        parentTransform = operationGameObject.transform;
                    }
                    else
                    {
                        parentTransform = parentGameObject.transform;
                    }
                }
                else if (brushObjectCount > 1)
                {
                    parentModel = lastUsedModelTransform.GetComponent<CSGModel>();
                    parentGameObject = OperationsUtility.CreateGameObject(lastUsedModelTransform, "Brushes", true);
                    Transform transform = parentGameObject.transform;
                    SetBrushTransformation(transform);

                    operationGameObject = parentGameObject;
                    CSGOperation operation = operationGameObject.AddComponent<CSGOperation>();

                    if (CurrentCSGOperationType != INVALID_CSG_OPERATION_TYPE)
                    {
                        operation.operationType = CurrentCSGOperationType;
                    }

                    operation.handleAsOne = true;
                    parentTransform = operationGameObject.transform;
                    Undo.RegisterCreatedObjectUndo(parentGameObject, "Created brush");
                }
                else
                {
                    parentGameObject = null;
                    operationGameObject = null;
                    parentTransform = lastUsedModelTransform;
                    parentModel = lastUsedModelTransform.GetComponent<CSGModel>();
                }

                generatedGameObjects = new GameObject[brushObjectCount];
                generatedBrushes = new CSGBrush[brushObjectCount];

                for (int p = 0; p < brushObjectCount; p++)
                {
                    string brushName = brushObjectCount == 1 ? "Brush" : $"Brush ({p})";

                    GameObject gameObject = OperationsUtility.CreateGameObject(parentTransform, brushName, false);
                    gameObject.SetActive(false);

                    CSGBrush brushComponent = gameObject.AddComponent<CSGBrush>();

                    if (operationGameObject == null)
                    {
                        if (CurrentCSGOperationType != INVALID_CSG_OPERATION_TYPE)
                        {
                            brushComponent.operationType = CurrentCSGOperationType;
                        }

                        operationGameObject = gameObject;
                        Transform transform = gameObject.transform;
                        SetBrushTransformation(transform);
                    }

                    generatedBrushes[p] = brushComponent;
                    generatedBrushes[p].controlMesh = new ControlMesh();
                    generatedBrushes[p].shape = new Shape();
                    Undo.RegisterCreatedObjectUndo(gameObject, "Created brush");
                    generatedGameObjects[p] = gameObject;
                }
            }
            else
            {
                UpdateBrushPosition();
            }

            return generatedBrushes != null && generatedBrushes.Length > 0 && generatedBrushes.Length == brushObjectCount;
        }

        internal void MarkAllBrushesDirty()
        {
            for (int i = 0; i < generatedBrushes.Length; i++)
            {
                CSGBrush brush = generatedBrushes[i];

                if (!brush)
                {
                    continue;
                }

                EditorUtility.SetDirty(brush);
            }
        }

        public abstract AABB GetShapeBounds();

        public bool UndoRedoPerformed()
        {
            if (UpdateBaseShape(false))
            {
                return false;
            }

            Cancel();

            return true;
        }

        public virtual void HandleEvents(SceneView sceneView, Rect sceneRect)
        {
            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseMove)
            {
                mouseIsDragging = false;
            }
            else if (Event.current.type == EventType.MouseDrag)
            {
                mouseIsDragging = true;
            }

            CreateControlIDs();

            // pretend we dragged so we don't click if we just changed edit modes
            if (prevEditMode != editMode)
            {
                prevEditMode = editMode;
                mouseIsDragging = true;
            }

            switch (editMode)
            {
                default:
                case EditMode.CreatePlane:
                case EditMode.CreateShape: HandleCreateShapeEvents(sceneView, sceneRect); break;
                case EditMode.ExtrudeShape:
                case EditMode.EditShape: HandleEditShapeEvents(sceneView, sceneRect); break;
            }
        }

        public abstract bool Commit(Camera camera);
        internal abstract bool UpdateBaseShape(bool registerUndo = true);
        protected abstract void HandleCreateShapeEvents(SceneView sceneView, Rect sceneRect);
        protected abstract void HandleEditShapeEvents(SceneView sceneView, Rect sceneRect);
        protected abstract void MoveShape(Vector3 offset);
        public abstract void PerformDeselectAll();
        public abstract void PerformDelete();

        private void Cleanup()
        {
            if (prevSelection != null)
            {
                Selection.objects = prevSelection;
            }
            else
            {
                Selection.activeTransform = null;
            }

            prevSelection = null;

            if (GUIUtility.hotControl == shapeId)
            {
                GUIUtility.hotControl = 0;
                GUIUtility.keyboardControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
                EditorGUIUtility.editingTextField = false;
            }

            isFinished = true;
            CSGEditorGUIUtility.RepaintAll();
        }

        public virtual void Cancel()
        {
            ResetVisuals();

            CleanupGrid();

            {
                if (generatedBrushes != null && generatedBrushes.Length > 0)
                {
                    for (int i = 0; i < generatedBrushes.Length; i++)
                    {
                        InternalCSGModelManager.OnDestroyed(generatedBrushes[i]);
                        DestroyImmediate(generatedBrushes[i]);
                    }
                }

                if (generatedGameObjects != null && generatedGameObjects.Length > 0)
                {
                    for (int i = 0; i < generatedGameObjects.Length; i++)
                    {
                        DestroyImmediate(generatedGameObjects[i]);
                    }
                }

                if (parentGameObject != null)
                {
                    DestroyImmediate(parentGameObject);
                }
            }

            Cleanup();
            Reset();

            if (shapeCancelled != null)
            {
                shapeCancelled();
            }
        }

        public void EndCommit()
        {
            if (generatedBrushes == null)
            {
                return;
            }

            AABB bounds = BoundsUtilities.GetBounds(generatedBrushes);

            if (!bounds.IsEmpty())
            {
                ControlMeshUtility.RebuildShapes(generatedBrushes);
                CSGModel[] models = operationGameObject.GetComponentsInParent<CSGModel>(true);
                CSGModel model = models.Length == 0 ? null : models[0];
                model.forceUpdate = true;

                InternalCSGModelManager.CheckForChanges(true);
                Undo.CollapseUndoOperations(undoGroupIndex);
                Cleanup();

                if (generatedGameObjects != null && generatedGameObjects.Length > 0)
                {
                    Selection.objects = generatedGameObjects;
                }

                Reset();
            }

            if (shapeCommitted != null)
            {
                shapeCommitted();
            }
        }

        // unity bug workaround

        [NonSerialized] private bool _doCommit;
        [NonSerialized] private bool _doCancel;

        public void StartGUI()
        {
            _doCommit = false;
            _doCancel = false;
        }

        public void DoCancel()
        {
            _doCancel = true;
        }

        public void DoCommit()
        {
            _doCommit = true;
        }

        public void FinishGUI()
        {
            Camera camera = Camera.current;

            if (_doCommit)
            {
                Commit(camera);
            }

            if (_doCancel)
            {
                Cancel();
            }
        }

        public void MatchSurfaceMaterial(Camera _camera, CSGBrush _vertexOnBrush, Vector3 _hoverBuildPlaneNormal)
        {
            if(editMode != EditMode.CreatePlane)
            {
                return;
            }

            MatchedMaterial = null;

            //Select material on generation surface
            bool MatchSurfaceMaterial = EditorPrefs.GetBool("GenerateUsingSurfaceMaterial", false);

            if (MatchSurfaceMaterial == true)
            {
                if (_vertexOnBrush != null)
                {
                    int closestIndex = -1;
                    float closestDistance = float.MaxValue;

                    for (int i = 0; i < _vertexOnBrush.shape.texGens.Length; i++)
                    {
                        bool intersects = SceneQueryUtility.FindSurfaceIntersection(_camera, _vertexOnBrush, _vertexOnBrush.childData.modelTransform.localToWorldMatrix, i, Event.current.mousePosition, out LegacySurfaceIntersection _surfaceIntersection);

                        if (intersects)
                        {
                            Vector3 SurfacePlaneNormal = _surfaceIntersection.localPlane.Normal;

                            if(_vertexOnBrush.operationType == CSGOperationType.Subtractive)
                            {
                                SurfacePlaneNormal *= -1;
                            }

                            if (SurfacePlaneNormal == _hoverBuildPlaneNormal)
                            {
                                if (_surfaceIntersection.distance < closestDistance)
                                {
                                    closestIndex = i;
                                    closestDistance = _surfaceIntersection.distance;
                                }
                            }
                        }
                    }

                    if (closestIndex > -1)
                    {
                        MatchedMaterial = _vertexOnBrush.shape.texGens[closestIndex].RenderMaterial;
                        MatchedUVScale = _vertexOnBrush.shape.texGens[closestIndex].Scale;
                    }
                }
                else
                {
                    MatchedUVScale = new Vector2(CSGSettings.DefaultUVScale, CSGSettings.DefaultUVScale);
                }
            }
        }
    }
}

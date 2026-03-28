using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal sealed class EditModeManager : ScriptableObject
    {
        private static EditModeManager m_Instance;

        [NonSerialized] private bool _generateMode;
        [NonSerialized] private readonly FilteredSelection _filteredSelection = new FilteredSelection();

        [SerializeField] private ToolEditMode editMode = ToolEditMode.Place;
        private IEditMode _activeTool;

        private static IEditMode[] m_BrushTools;

        public static FilteredSelection FilteredSelection
        {
            get
            {
                InitTools();

                return m_Instance._filteredSelection;
            }
        }

        public static IEditMode ActiveTool
        {
            get
            {
                InitTools();

                return m_Instance._activeTool;
            }
        }

        public static ToolEditMode EditMode
        {
            get
            {
                InitTools();

                return m_Instance.editMode;
            }
            set
            {
                if(m_Instance.editMode == value)
                {
                    return;
                }

                Undo.RecordObject(m_Instance, "Changed edit mode");

                m_Instance.editMode      = value;
                m_Instance._generateMode = false;

                CSGSettings.EditMode = m_Instance.editMode;
                CSGSettings.Save();

                if(ActiveTool != null)
                {
                    CSGEditorGUIUtility.RepaintAll();
                }
            }
        }

        public static IEditMode CurrentTool
        {
            get
            {
                InitTools();

                if(m_Instance._generateMode)
                {
                    return m_BrushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
                }

                ToolEditMode editMode = m_Instance.editMode;

                if(editMode < m_FirstEditMode || editMode > m_LastEditModes)
                {
                    return m_BrushTools[0];
                }

                return m_BrushTools[(int)editMode];
            }
        }

        private static ToolEditMode m_FirstEditMode;
        private static ToolEditMode m_LastEditModes;

        private const HideFlags SCRIPTABLE_OBJECT_HIDEFLAGS = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor
          | HideFlags.NotEditable | HideFlags.HideInHierarchy;

        private static void InitTools()
        {
            if(m_Instance)
            {
                return;
            }

            List<ToolEditMode> values = Enum.GetValues(typeof(ToolEditMode)).Cast<ToolEditMode>().ToList();
            values.Sort();
            m_FirstEditMode = values[0];
            m_LastEditModes = values[values.Count - 1];

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.undoRedoPerformed += UndoRedoPerformed;

            EditorApplication.modifierKeysChanged -= OnModifierKeysChanged;
            EditorApplication.modifierKeysChanged += OnModifierKeysChanged;

            EditModeManager[] managers = Resources.FindObjectsOfTypeAll<EditModeManager>().ToArray();

            for(int i = 0; i < managers.Length; i++)
            {
                DestroyImmediate(managers[i]);
            }

            m_Instance           = CreateInstance<EditModeManager>();
            m_Instance.hideFlags = SCRIPTABLE_OBJECT_HIDEFLAGS;

            Type[] types = { typeof(EditModePlace), typeof(EditModeGenerate), typeof(EditModeMeshEdit), typeof(EditModeClip), typeof(EditModeSurface) };

            if(types.Length != values.Count)
            {
                Debug.LogWarning("types.Length != values.Count");
            }

            m_BrushTools = new IEditMode[values.Count];

            for(int j = 0; j < types.Length; j++)
            {
                Object[] objects = Resources.FindObjectsOfTypeAll(types[j]).ToArray();

                for(int i = 0; i < objects.Length; i++)
                {
                    DestroyImmediate(objects[i]);
                }

                ScriptableObject obj = CreateInstance(types[j]);
                m_BrushTools[j] = obj as IEditMode;

                if(m_BrushTools[j] == null)
                {
                    Debug.LogWarning("brushTools[j] == null");

                    continue;
                }

                if(!(m_BrushTools[j] is ScriptableObject))
                {
                    Debug.LogWarning("!(brushTools[j] is ScriptableObject)");

                    continue;
                }

                obj.hideFlags = SCRIPTABLE_OBJECT_HIDEFLAGS;
            }

            EditModeGenerate.ShapeCommitted -= OnShapeCommittedEvent;
            EditModeGenerate.ShapeCommitted += OnShapeCommittedEvent;
            EditModeGenerate.ShapeCancelled -= OnShapeCancelledEvent;
            EditModeGenerate.ShapeCancelled += OnShapeCancelledEvent;

            CSGSettings.Reload();
            m_Instance.editMode = CSGSettings.EditMode;

            UpdateSelection(true);
            InitTargets();
        }

        private static HashSet<CSGNode> m_SelectedNodes = new HashSet<CSGNode>();
        private static HashSet<Transform> m_SelectedOthers = new HashSet<Transform>();

        public static void UpdateSelection(bool forceUpdate = false)
        {
            InitTools();

            GetTargetSelection(ref m_SelectedNodes, ref m_SelectedOthers);

            if(!m_Instance._filteredSelection.UpdateSelection(m_SelectedNodes, m_SelectedOthers) && !forceUpdate)
            {
                return;
            }

            InternalCSGModelManager.SkipCheckForChanges = true;

            try
            {
                InternalCSGModelManager.CheckForChanges();
            }
            finally
            {
                InternalCSGModelManager.SkipCheckForChanges = false;
            }

            foreach(IEditMode tool in m_BrushTools)
            {
                tool.SetTargets(m_Instance._filteredSelection);
            }
        }

        public static void ResetMessage()
        {
            if(SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.RemoveNotification();

                return;
            }

            if(SceneView.currentDrawingSceneView != null)
            {
                SceneView.lastActiveSceneView.RemoveNotification();
            }
        }

        public static void ShowMessage(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                return;
            }

            if(SceneView.lastActiveSceneView != null || SceneView.currentDrawingSceneView != null)
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message));

                return;
            }

            Debug.LogWarning(message);
        }

        private static void OnEnableTool(IEditMode tool)
        {
            if(tool != null)
            {
                tool.OnEnableTool();
            }
        }

        private static void OnDisableTool(IEditMode tool)
        {
            if(tool != null)
            {
                tool.OnDisableTool();
            }
        }

        public static bool DeselectAll()
        {
            return m_Instance._activeTool != null && m_Instance._activeTool.DeselectAll();
        }

        private static void UndoRedoPerformed()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if(m_Instance._activeTool != null)
            {
                m_Instance._activeTool.UndoRedoPerformed();
            }

            UpdateSelection(true);
        }

        private static void OnModifierKeysChanged()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            CSGEditorGUIUtility.RepaintAll();
        }

        private static bool GetTargetSelection(ref HashSet<CSGNode> nodes, ref HashSet<Transform> others)
        {
            m_SelectedNodes.Clear();
            m_SelectedOthers.Clear();

            if(Selection.gameObjects == null)
            {
                return false;
            }

            foreach(GameObject gameObject in Selection.gameObjects)
            {
                if(!gameObject)
                {
                    continue;
                }

                CSGNode node = gameObject.GetComponent<CSGNode>();

                if(node && node.enabled && (node.hideFlags & HideFlags.HideInInspector) == HideFlags.None)
                {
                    nodes.Add(node);
                }
                else
                {
                    others.Add(gameObject.transform);
                }
            }

            return true;
        }

        private static void InitTargets()
        {
            IEditMode newTool = CurrentTool;

            if(newTool == m_Instance._activeTool)
            {
                if(m_Instance._filteredSelection.nodeTargets != null && m_Instance._filteredSelection.nodeTargets.Length > 0)
                {
                    OnEnableTool(m_Instance._activeTool);
                }
                else
                {
                    OnDisableTool(m_Instance._activeTool);
                }
            }
            else
            {
                UpdateTool();
            }
        }

        private static void NextEditMode()
        {
            InitTools();

            if(m_Instance.editMode == m_LastEditModes)
            {
                EditMode = m_FirstEditMode;
            }
            else
            {
                EditMode = m_Instance.editMode + 1;
            }
        }

        private static void PrevEditMode()
        {
            InitTools();

            if(m_Instance.editMode == m_FirstEditMode)
            {
                EditMode = m_LastEditModes;
            }
            else
            {
                EditMode = m_Instance.editMode - 1;
            }
        }

        public static void UpdateTool()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            IEditMode newTool = !CSGSettings.EnableRealtimeCSG ? null : CurrentTool;

            if(m_Instance._activeTool != newTool)
            {
                if(m_Instance._activeTool != null)
                {
                    OnDisableTool(m_Instance._activeTool);
                }

                m_Instance._activeTool = newTool;

                if(newTool != null)
                {
                    OnEnableTool(newTool);
                }
            }
        }

        private static void FinishShapeBuilder()
        {
            Tools.hidden = false;
        }

        public static Vector3 SnapPointToGrid(Camera camera, Vector3 point, CSGPlane plane, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush, CSGBrush[] ignoreBrushes, bool ignoreAllBrushes = false)
        {
            snappedOnBrush = null;
            // Note: relative snapping wouldn't make sense here since it's a single point that's being snapped and there is no relative movement
            SnapMode activeSnappingMode = CSGSettings.ActiveSnappingMode;

            Vector3 snappedPoint = point;

            switch(activeSnappingMode)
            {
                case SnapMode.RelativeSnapping: // TODO: fixme
                case SnapMode.GridSnapping:
                {
                    snappedPoint += CSGGrid.ForceSnapDeltaToGrid(camera, MathConstants.zeroVector3, snappedPoint);

                    snappedPoint = CSGGrid.PointFromGridSpace(CSGGrid.CubeProject(camera, CSGGrid.PlaneToGridSpace(plane), CSGGrid.PointToGridSpace(snappedPoint)));

                    // snap twice to get rid of some tiny movements caused by the projection in depth
                    snappedPoint += CSGGrid.ForceSnapDeltaToGrid(camera, MathConstants.zeroVector3, snappedPoint);

                    snappedPoint = CSGGrid.PointFromGridSpace(CSGGrid.CubeProject(camera, CSGGrid.PlaneToGridSpace(plane), CSGGrid.PointToGridSpace(snappedPoint)));

                    return !ignoreAllBrushes ? GridUtility.SnapToWorld(camera, plane, point, snappedPoint, ref snappingEdges, out snappedOnBrush, ignoreBrushes) : snappedPoint;
                }
                default:
                case SnapMode.None:
                {
                    snappedPoint = GeometryUtility.ProjectPointOnPlane(plane, snappedPoint);

                    return snappedPoint;
                }
            }
        }

        public static Vector3 SnapPointToRay(Camera camera, Vector3 point, Ray ray, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush)
        {
            snappedOnBrush = null;

            Vector3 snappedPoint = point;

            snappingEdges = null;
            // Note: relative snapping wouldn't make sense here since it's a single point that's being snapped and there is no relative movement
            bool doGridSnapping = CSGSettings.ActiveSnappingMode != SnapMode.None;

            if(doGridSnapping)
            {
                Vector3 delta = CSGGrid.ForceSnapDeltaToRay(camera, ray, MathConstants.zeroVector3, snappedPoint);
                snappedPoint += delta;
            }

            return snappedPoint;
        }

        private static void OnShapeCancelledEvent()
        {
            if(!m_Instance._generateMode)
            {
                return;
            }

            m_Instance._generateMode = false;
        }

        private static void OnShapeCommittedEvent()
        {
            m_Instance._generateMode = false;
        }

        private static bool HandleBuilderEvents()
        {
            if(CSGSettings.EditMode != m_Instance.editMode)
            {
                CSGSettings.EditMode = m_Instance.editMode;
                CSGSettings.Save();
            }

            switch(Event.current.type)
            {
                case EventType.KeyDown:
                {
                    if(GUIUtility.hotControl == 0 && Keys.freeBuilderMode.IsKeyPressed() && !m_Instance._generateMode)
                    {
                        EditModeGenerate generateBrushTool = m_BrushTools[(int)ToolEditMode.Generate] as EditModeGenerate;

                        generateBrushTool.BuilderMode = ShapeMode.FreeDraw;
                        m_Instance._generateMode      = true;
                        Event.current.Use();

                        return true;
                    }

                    if(Keys.cylinderBuilderMode.IsKeyPressed() && !m_Instance._generateMode)
                    {
                        EditModeGenerate generateBrushTool = m_BrushTools[(int)ToolEditMode.Generate] as EditModeGenerate;

                        generateBrushTool.BuilderMode = ShapeMode.Cylinder;
                        m_Instance._generateMode      = true;
                        Event.current.Use();

                        return true;
                    }

                    if(Keys.boxBuilderMode.IsKeyPressed() && !m_Instance._generateMode)
                    {
                        EditModeGenerate generateBrushTool = m_BrushTools[(int)ToolEditMode.Generate] as EditModeGenerate;

                        generateBrushTool.BuilderMode = ShapeMode.Box;
                        m_Instance._generateMode      = true;
                        Event.current.Use();

                        return true;
                    }

                    if(Keys.sphereBuilderMode.IsKeyPressed() && !m_Instance._generateMode)
                    {
                        EditModeGenerate generateBrushTool = m_BrushTools[(int)ToolEditMode.Generate] as EditModeGenerate;

                        generateBrushTool.BuilderMode = ShapeMode.Sphere;
                        m_Instance._generateMode      = true;
                        Event.current.Use();

                        return true;
                    }

                    if(Keys.switchToObjectEditMode.IsKeyPressed()
                    || Keys.switchToGenerateEditMode.IsKeyPressed()
                    || Keys.switchToMeshEditMode.IsKeyPressed()
                    || Keys.switchToClipEditMode.IsKeyPressed()
                    || Keys.switchToSurfaceEditMode.IsKeyPressed())
                    {
                        Event.current.Use();

                        return true;
                    }

                    break;
                }
                case EventType.KeyUp:
                {
                    if(m_Instance._generateMode && (Keys.freeBuilderMode.IsKeyPressed() || Keys.cylinderBuilderMode.IsKeyPressed() || Keys.boxBuilderMode.IsKeyPressed() || Keys.sphereBuilderMode.IsKeyPressed()))
                    {
                        Event.current.Use();

                        EditModeGenerate generateBrushTool = m_BrushTools[(int)ToolEditMode.Generate] as EditModeGenerate;

                        if(!generateBrushTool.HotKeyReleased())
                        {
                            m_Instance._generateMode = false;
                        }
                    }
                    else if(Keys.switchToObjectEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Place;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.switchToGenerateEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Generate;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.switchToMeshEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Edit;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.switchToClipEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Clip;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.switchToSurfaceEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Surfaces;
                        Event.current.Use();

                        return true;
                    }

                    break;
                }
                case EventType.ValidateCommand:
                {
                    if((GUIUtility.hotControl == 0 && Keys.freeBuilderMode.IsKeyPressed())
                    || Keys.cylinderBuilderMode.IsKeyPressed()
                    || Keys.sphereBuilderMode.IsKeyPressed()
                    || Keys.boxBuilderMode.IsKeyPressed()
                    || Keys.switchToObjectEditMode.IsKeyPressed()
                    || Keys.switchToGenerateEditMode.IsKeyPressed()
                    || Keys.switchToMeshEditMode.IsKeyPressed()
                    || Keys.switchToClipEditMode.IsKeyPressed()
                    || Keys.switchToSurfaceEditMode.IsKeyPressed())
                    {
                        Event.current.Use();

                        return true;
                    }

                    break;
                }
            }

            return false;
        }

        [NonSerialized] private readonly LineMeshManager _zTestLineMeshManager = new LineMeshManager();
        [NonSerialized] private readonly LineMeshManager _noZTestLineMeshManager = new LineMeshManager();

        private void OnDestroy()
        {
            _zTestLineMeshManager.Destroy();
            _noZTestLineMeshManager.Destroy();
        }

        private static readonly int sceneWindowHash = "SceneWindowHash".GetHashCode();

        private static List<EditModeToolWindowSceneGUI> m_CurrentEditorWindows = new List<EditModeToolWindowSceneGUI>();

        public static bool InitSceneGUI()
        {
            m_CurrentEditorWindows = EditModeToolWindowSceneGUI.GetEditorWindows();

            return m_CurrentEditorWindows.Count == 0;
        }

        public static void OnSceneGUI(SceneView sceneView)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            CameraUtility.InitDistanceChecks(sceneView);
            SelectionUtility.HandleEvents();
            InitTools();

            HandleBuilderEvents();

            UpdateTool();

            if(m_Instance._activeTool != null)
            {
                if(CSGSettings.EnableRealtimeCSG)
                {
                    // handle the tool
                    Vector2 sceneSize = sceneView.position.size;
                    Rect    sceneRect = new Rect(0, 0, sceneSize.x, sceneSize.y - (CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 4 + 17));

                    // This helps prevent weird issues with overlapping sceneviews + avoid some performance issues with multiple sceneviews open
                    if(EditorWindow.mouseOverWindow == sceneView || (Event.current.type != EventType.MouseMove && Event.current.type != EventType.Layout))
                    {
                        m_Instance._activeTool.HandleEvents(sceneView, sceneRect);
                    }
                }
                else
                {
                    if(Event.current.type == EventType.Repaint)
                    {
                        CSGBrush[]              brushes         = m_Instance._filteredSelection.brushTargets;
                        List<GeometryWireframe> wireframes      = new List<GeometryWireframe>(brushes.Length);
                        List<Matrix4x4>         transformations = new List<Matrix4x4>(brushes.Length);

                        for(int i = 0; i < brushes.Length; i++)
                        {
                            CSGBrush brush = brushes[i];

                            if(!brush)
                            {
                                continue;
                            }

                            if(brush.childData == null ||
                               !brush.childData.model)
                            {
                                continue;
                            }

                            Matrix4x4 brushTransformation = brush.compareTransformation.localToWorldMatrix;

                            wireframes.Add(BrushOutlineManager.GetBrushOutline(brushes[i].brushNodeID));
                            transformations.Add(brushTransformation);
                        }

                        if(wireframes.Count > 0)
                        {
                            CSGRenderer.DrawSelectedBrushes(m_Instance._zTestLineMeshManager, m_Instance._noZTestLineMeshManager, wireframes.ToArray(), transformations.ToArray(), ColorSettings.SelectedOutlines, GUIConstants.THICK_LINE_SCALE);
                        }

                        MaterialUtility.LineDashMultiplier      = 1.0f;
                        MaterialUtility.LineThicknessMultiplier = 1.0f;
                        MaterialUtility.LineAlphaMultiplier     = 1.0f;

                        m_Instance._zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);
                        m_Instance._zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);
                    }
                }
            }

            int       sceneWindowId   = GUIUtility.GetControlID(sceneWindowHash, FocusType.Passive);
            EventType sceneWindowType = Event.current.GetTypeForControl(sceneWindowId);

            if(sceneWindowType == EventType.Repaint)
            {
                if(m_CurrentEditorWindows.Count > 0)
                {
                    for(int i = 0; i < m_CurrentEditorWindows.Count; i++)
                    {
                        m_CurrentEditorWindows[i].Repaint();
                    }

                    return;
                }
            }

            if(sceneWindowType == EventType.MouseMove)
            {
                SceneDragToolManager.IsDraggingObjectInScene = false;
            }

            if(CSGSettings.EnableRealtimeCSG)
            {
                if(sceneView && sceneWindowType != EventType.Used &&
                   (!SceneDragToolManager.IsDraggingObjectInScene || EditMode == ToolEditMode.Surfaces))
                {
                    if(m_CurrentEditorWindows.Count == 0)
                    {
                        try
                        {
                            Handles.BeginGUI();
                            Rect windowRect = new Rect(Vector2.zero, sceneView.position.size);
                            EditModeSelectionGUI.HandleWindowGUI(windowRect);
                        }
                        finally
                        {
                            Handles.EndGUI();
                        }
                    }
                }
            }
        }

        public static void GenerateFromSurface(Camera camera, CSGBrush cSgBrush, CSGPlane polygonPlane, Vector3 direction, Vector3[] points, int[] pointIndices, uint[] smoothingGroups, bool drag, CSGOperationType forceDragSource, bool autoCommitExtrusion)
        {
            EditMode = ToolEditMode.Generate;
            UpdateTool();
            EditModeGenerate generateBrushTool = m_BrushTools[(int)ToolEditMode.Generate] as EditModeGenerate;

            generateBrushTool.GenerateFromPolygon(camera, cSgBrush, polygonPlane, direction, points, pointIndices, smoothingGroups, drag, forceDragSource, autoCommitExtrusion);
        }

        public delegate void SetTransformation(Transform newTransform, Transform originalTransform);

        public static Transform[] CloneTargets(SetTransformation setTransform = null)
        {
            if(m_Instance._filteredSelection.nodeTargets.Length == 0)
            {
                return Array.Empty<Transform>();
            }

            int groupId = Undo.GetCurrentGroup();

            GameObject[] newTargets    = new GameObject[m_Instance._filteredSelection.nodeTargets.Length];
            Transform[]  newTransforms = new Transform[m_Instance._filteredSelection.nodeTargets.Length];

            for(int i = 0; i < m_Instance._filteredSelection.nodeTargets.Length; i++)
            {
                GameObject originalGameObject = m_Instance._filteredSelection.nodeTargets[i].gameObject;
                Transform  originalTransform  = originalGameObject.GetComponent<Transform>();

                newTargets[i] = CSGPrefabUtility.Instantiate(originalGameObject);
                Transform newTransform = newTargets[i].GetComponent<Transform>();

                if(originalTransform.parent != null)
                {
                    newTransform.SetParent(originalTransform.parent, false);
                    newTransform.SetSiblingIndex(originalTransform.GetSiblingIndex() + 1);

                    newTransform.name = GameObjectUtility.GetUniqueNameForSibling(originalTransform.parent, originalTransform.name);
                }

                if(setTransform == null)
                {
                    newTransform.localScale    = originalTransform.localScale;
                    newTransform.localPosition = originalTransform.localPosition;
                    newTransform.localRotation = originalTransform.localRotation;
                }
                else
                {
                    setTransform(newTransform, originalTransform);
                }

                CSGBrush[] childBrushes = newTargets[i].GetComponentsInChildren<CSGBrush>();

                Dictionary<uint, uint> uniqueSmoothingGroups = new Dictionary<uint, uint>();

                foreach(CSGBrush childBrush in childBrushes)
                {
                    for(int g = 0; g < childBrush.shape.texGens.Length; g++)
                    {
                        uint smoothingGroup = childBrush.shape.texGens[g].SmoothingGroup;

                        if(smoothingGroup == 0)
                        {
                            continue;
                        }

                        if(!uniqueSmoothingGroups.TryGetValue(smoothingGroup, out uint newSmoothingGroup))
                        {
                            newSmoothingGroup                     = SurfaceUtility.FindUnusedSmoothingGroupIndex();
                            uniqueSmoothingGroups[smoothingGroup] = newSmoothingGroup;
                        }

                        childBrush.shape.texGens[g].SmoothingGroup = newSmoothingGroup;
                    }
                }

                newTransforms[i] = newTransform;
                Undo.RegisterCreatedObjectUndo(newTargets[i], $"Created clone of {originalGameObject.name}");
            }

            Selection.objects = newTargets;
            Undo.CollapseUndoOperations(groupId);

            return newTransforms;
        }
    }
}

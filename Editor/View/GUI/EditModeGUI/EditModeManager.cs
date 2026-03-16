using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal sealed class EditModeManager : ScriptableObject
    {
        private static EditModeManager instance = null;

        [NonSerialized] private bool generateMode = false;
        [NonSerialized] private FilteredSelection filteredSelection = new FilteredSelection();

        [SerializeField] private ToolEditMode editMode = ToolEditMode.Place;
        [SerializeField] private IEditMode activeTool = null;

        private static IEditMode[] brushTools = null;

        public static FilteredSelection FilteredSelection
        {
            get
            {
                InitTools();

                return instance.filteredSelection;
            }
        }

        public static IEditMode ActiveTool
        {
            get
            {
                InitTools();

                return instance.activeTool;
            }
        }

        public static ToolEditMode EditMode
        {
            get
            {
                InitTools();

                return instance.editMode;
            }
            set
            {
                if(instance.editMode == value)
                {
                    return;
                }

                Undo.RecordObject(instance, "Changed edit mode");

                instance.editMode     = value;
                instance.generateMode = false;

                CSGSettings.EditMode = instance.editMode;
                CSGSettings.Save();

                if(ActiveTool != null)
                {
                    CSG_EditorGUIUtility.RepaintAll();
                }
            }
        }

        public static IEditMode CurrentTool
        {
            get
            {
                InitTools();

                if(instance.generateMode)
                {
                    return brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
                }

                ToolEditMode editMode = instance.editMode;

                if(editMode < firstEditMode ||
                   editMode > lastEditModes)
                {
                    return brushTools[0];
                }

                return brushTools[(int)editMode];
            }
        }

        private static ToolEditMode firstEditMode;
        private static ToolEditMode lastEditModes;

        private const HideFlags scriptableObjectHideflags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor
          | HideFlags.NotEditable | HideFlags.HideInHierarchy;

        private static void InitTools()
        {
            if(instance)
            {
                return;
            }

            List<ToolEditMode> values = Enum.GetValues(typeof(ToolEditMode)).Cast<ToolEditMode>().ToList();
            values.Sort();
            firstEditMode = values[0];
            lastEditModes = values[values.Count - 1];

            Undo.undoRedoPerformed -= UndoRedoPerformed;
            Undo.undoRedoPerformed += UndoRedoPerformed;

            EditorApplication.modifierKeysChanged -= OnModifierKeysChanged;
            EditorApplication.modifierKeysChanged += OnModifierKeysChanged;

            EditModeManager[] managers = Resources.FindObjectsOfTypeAll<EditModeManager>().ToArray();

            for(int i = 0; i < managers.Length; i++)
            {
                DestroyImmediate(managers[i]);
            }

            instance           = CreateInstance<EditModeManager>();
            instance.hideFlags = scriptableObjectHideflags;

            Type[] types = new Type[]
            {
                typeof(EditModePlace), typeof(EditModeGenerate), typeof(EditModeMeshEdit), typeof(EditModeClip)
              , typeof(EditModeSurface)
            };

            if(types.Length != values.Count)
            {
                Debug.LogWarning("types.Length != values.Count");
            }

            brushTools = new IEditMode[values.Count];

            for(int j = 0; j < types.Length; j++)
            {
                Object[] objects = Resources.FindObjectsOfTypeAll(types[j]).ToArray();

                for(int i = 0; i < objects.Length; i++)
                {
                    DestroyImmediate(objects[i]);
                }

                ScriptableObject obj = CreateInstance(types[j]);
                brushTools[j] = obj as IEditMode;

                if(brushTools[j] == null)
                {
                    Debug.LogWarning("brushTools[j] == null");

                    continue;
                }

                if(!(brushTools[j] is ScriptableObject))
                {
                    Debug.LogWarning("!(brushTools[j] is ScriptableObject)");

                    continue;
                }

                obj.hideFlags = scriptableObjectHideflags;
            }

            EditModeGenerate.ShapeCommitted -= OnShapeCommittedEvent;
            EditModeGenerate.ShapeCommitted += OnShapeCommittedEvent;
            EditModeGenerate.ShapeCancelled -= OnShapeCancelledEvent;
            EditModeGenerate.ShapeCancelled += OnShapeCancelledEvent;

            CSGSettings.Reload();
            instance.editMode = CSGSettings.EditMode;

            UpdateSelection(true);
            InitTargets();
        }

        private static HashSet<CSGNode> selectedNodes = new HashSet<CSGNode>();
        private static HashSet<Transform> selectedOthers = new HashSet<Transform>();

        public static void UpdateSelection(bool forceUpdate = false)
        {
            InitTools();

            GetTargetSelection(ref selectedNodes, ref selectedOthers);

            if(!instance.filteredSelection.UpdateSelection(selectedNodes, selectedOthers) &&
               !forceUpdate)
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

            foreach(IEditMode tool in brushTools)
            {
                tool.SetTargets(instance.filteredSelection);
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

                return;
            }
        }

        public static void ShowMessage(string message)
        {
            if(string.IsNullOrEmpty(message))
            {
                return;
            }

            if(SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.ShowNotification(new GUIContent(message));

                return;
            }

            if(SceneView.currentDrawingSceneView != null)
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
            if(instance.activeTool != null &&
               instance.activeTool.DeselectAll())
            {
                return true;
            }

            return false;
        }

        private static void UndoRedoPerformed()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if(instance.activeTool != null)
            {
                instance.activeTool.UndoRedoPerformed();
            }

            UpdateSelection(true);
        }

        private static void OnModifierKeysChanged()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            CSG_EditorGUIUtility.RepaintAll();
        }

        private static bool GetTargetSelection(ref HashSet<CSGNode> nodes, ref HashSet<Transform> others)
        {
            selectedNodes.Clear();
            selectedOthers.Clear();

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

            if(newTool == instance.activeTool)
            {
                if(instance.filteredSelection.NodeTargets != null &&
                   instance.filteredSelection.NodeTargets.Length > 0)
                {
                    OnEnableTool(instance.activeTool);
                }
                else
                {
                    OnDisableTool(instance.activeTool);
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

            if(instance.editMode == lastEditModes)
            {
                EditMode = firstEditMode;
            }
            else
            {
                EditMode = (ToolEditMode)(instance.editMode + 1);
            }
        }

        private static void PrevEditMode()
        {
            InitTools();

            if(instance.editMode == firstEditMode)
            {
                EditMode = lastEditModes;
            }
            else
            {
                EditMode = (ToolEditMode)(instance.editMode - 1);
            }
        }

        public static void UpdateTool()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            IEditMode newTool;

            if(!CSGSettings.EnableRealtimeCSG)
            {
                newTool = null;
            }
            else
            {
                newTool = CurrentTool;
            }

            if(instance.activeTool != newTool)
            {
                if(instance.activeTool != null)
                {
                    OnDisableTool(instance.activeTool);
                }

                instance.activeTool = newTool;

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

        public static Vector3 SnapPointToGrid(
            Camera camera, Vector3 point, CSGPlane plane, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush
          , CSGBrush[] ignoreBrushes, bool ignoreAllBrushes = false
        )
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
                    snappedPoint = snappedPoint + CSGGrid.ForceSnapDeltaToGrid(
                        camera, MathConstants.zeroVector3, snappedPoint
                    );

                    snappedPoint = CSGGrid.PointFromGridSpace(
                        CSGGrid.CubeProject(
                            camera, CSGGrid.PlaneToGridSpace(plane), CSGGrid.PointToGridSpace(snappedPoint)
                        )
                    );

                    // snap twice to get rid of some tiny movements caused by the projection in depth	
                    snappedPoint = snappedPoint + CSGGrid.ForceSnapDeltaToGrid(
                        camera, MathConstants.zeroVector3, snappedPoint
                    );

                    snappedPoint = CSGGrid.PointFromGridSpace(
                        CSGGrid.CubeProject(
                            camera, CSGGrid.PlaneToGridSpace(plane), CSGGrid.PointToGridSpace(snappedPoint)
                        )
                    );

                    if(!ignoreAllBrushes)
                    {
                        return GridUtility.SnapToWorld(
                            camera, plane, point, snappedPoint, ref snappingEdges, out snappedOnBrush, ignoreBrushes
                        );
                    }

                    return snappedPoint;
                }
                default:
                case SnapMode.None:
                {
                    snappedPoint = GeometryUtility.ProjectPointOnPlane(plane, snappedPoint);

                    return snappedPoint;
                }
            }
        }

        public static Vector3 SnapPointToRay(
            Camera camera, Vector3 point, Ray ray, ref List<Vector3> snappingEdges, out CSGBrush snappedOnBrush
        )
        {
            snappedOnBrush = null;

            Vector3 snappedPoint = point;

            snappingEdges = null;
            // Note: relative snapping wouldn't make sense here since it's a single point that's being snapped and there is no relative movement
            bool doGridSnapping = CSGSettings.ActiveSnappingMode != SnapMode.None;

            if(doGridSnapping)
            {
                Vector3 delta = CSGGrid.ForceSnapDeltaToRay(camera, ray, MathConstants.zeroVector3, snappedPoint);
                snappedPoint = snappedPoint + delta;
            }

            return snappedPoint;
        }

        private static void OnShapeCancelledEvent()
        {
            if(!instance.generateMode)
            {
                return;
            }

            instance.generateMode = false;
        }

        private static void OnShapeCommittedEvent()
        {
            instance.generateMode = false;
        }

        private static bool HandleBuilderEvents()
        {
            if(CSGSettings.EditMode != instance.editMode)
            {
                CSGSettings.EditMode = instance.editMode;
                CSGSettings.Save();
            }

            switch(Event.current.type)
            {
                case EventType.KeyDown:
                {
                    if(GUIUtility.hotControl == 0 &&
                       Keys.FreeBuilderMode.IsKeyPressed() && !instance.generateMode)
                    {
                        EditModeGenerate generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
                        generateBrushTool.BuilderMode = ShapeMode.FreeDraw;
                        instance.generateMode         = true;
                        Event.current.Use();

                        return true;
                    }

                    if(Keys.CylinderBuilderMode.IsKeyPressed() && !instance.generateMode)
                    {
                        EditModeGenerate generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
                        generateBrushTool.BuilderMode = ShapeMode.Cylinder;
                        instance.generateMode         = true;
                        Event.current.Use();

                        return true;
                    }

                    if(Keys.BoxBuilderMode.IsKeyPressed() && !instance.generateMode)
                    {
                        EditModeGenerate generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
                        generateBrushTool.BuilderMode = ShapeMode.Box;
                        instance.generateMode         = true;
                        Event.current.Use();

                        return true;
                    }

                    if(Keys.SphereBuilderMode.IsKeyPressed() && !instance.generateMode)
                    {
                        EditModeGenerate generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;
                        generateBrushTool.BuilderMode = ShapeMode.Sphere;
                        instance.generateMode         = true;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToObjectEditMode.IsKeyPressed())
                    {
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToGenerateEditMode.IsKeyPressed())
                    {
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToMeshEditMode.IsKeyPressed())
                    {
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToClipEditMode.IsKeyPressed())
                    {
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToSurfaceEditMode.IsKeyPressed())
                    {
                        Event.current.Use();

                        return true;
                    }

                    break;
                }
                case EventType.KeyUp:
                {
                    if(instance.generateMode &&
                       (Keys.FreeBuilderMode.IsKeyPressed() ||
                           Keys.CylinderBuilderMode.IsKeyPressed() ||
                           Keys.BoxBuilderMode.IsKeyPressed() ||
                           Keys.SphereBuilderMode.IsKeyPressed()))
                    {
                        Event.current.Use();
                        EditModeGenerate generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;

                        if(!generateBrushTool.HotKeyReleased())
                        {
                            instance.generateMode = false;
                        }
                    }
                    else if(Keys.SwitchToObjectEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Place;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToGenerateEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Generate;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToMeshEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Edit;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToClipEditMode.IsKeyPressed())
                    {
                        InitTools();
                        EditMode = ToolEditMode.Clip;
                        Event.current.Use();

                        return true;
                    }
                    else if(Keys.SwitchToSurfaceEditMode.IsKeyPressed())
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
                    if((GUIUtility.hotControl == 0 && Keys.FreeBuilderMode.IsKeyPressed()) ||
                       Keys.CylinderBuilderMode.IsKeyPressed() ||
                       Keys.SphereBuilderMode.IsKeyPressed() ||
                       Keys.BoxBuilderMode.IsKeyPressed() ||
                       Keys.SwitchToObjectEditMode.IsKeyPressed() ||
                       Keys.SwitchToGenerateEditMode.IsKeyPressed() ||
                       Keys.SwitchToMeshEditMode.IsKeyPressed() ||
                       Keys.SwitchToClipEditMode.IsKeyPressed() ||
                       Keys.SwitchToSurfaceEditMode.IsKeyPressed())
                    {
                        Event.current.Use();

                        return true;
                    }

                    break;
                }
            }

            return false;
        }

        [NonSerialized] private LineMeshManager zTestLineMeshManager = new LineMeshManager();
        [NonSerialized] private LineMeshManager noZTestLineMeshManager = new LineMeshManager();

        private void OnDestroy()
        {
            zTestLineMeshManager.Destroy();
            noZTestLineMeshManager.Destroy();
        }

        private static readonly int SceneWindowHash = "SceneWindowHash".GetHashCode();

        private static List<EditModeToolWindowSceneGUI> currentEditorWindows = new List<EditModeToolWindowSceneGUI>();

        public static bool InitSceneGUI(SceneView sceneView)
        {
            currentEditorWindows = EditModeToolWindowSceneGUI.GetEditorWindows();

            return currentEditorWindows.Count == 0;
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

            {
                UpdateTool();

                if(instance.activeTool != null)
                {
                    if(CSGSettings.EnableRealtimeCSG)
                    {
                        // handle the tool
                        Vector2 sceneSize = sceneView.position.size;

                        Rect sceneRect = new Rect(
                            0, 0, sceneSize.x, sceneSize.y - (CSG_GUIStyleUtility.BottomToolBarHeight + 4 + 17)
                        );

                        // This helps prevent weird issues with overlapping sceneviews + avoid some performance issues with multiple sceneviews open
                        if(EditorWindow.mouseOverWindow == sceneView || (Event.current.type != EventType.MouseMove
                            && Event.current.type != EventType.Layout))
                        {
                            instance.activeTool.HandleEvents(sceneView, sceneRect);
                        }
                    }
                    else
                    {
                        if(Event.current.type == EventType.Repaint)
                        {
                            CSGBrush[]              brushes         = instance.filteredSelection.BrushTargets;
                            List<GeometryWireframe> wireframes      = new List<GeometryWireframe>(brushes.Length);
                            List<Matrix4x4>         transformations = new List<Matrix4x4>(brushes.Length);

                            for(int i = 0; i < brushes.Length; i++)
                            {
                                CSGBrush brush = brushes[i];

                                if(!brush)
                                {
                                    continue;
                                }

                                if(brush.ChildData == null ||
                                   !brush.ChildData.Model)
                                {
                                    continue;
                                }

                                Matrix4x4 brushTransformation = brush.compareTransformation.localToWorldMatrix;

                                wireframes.Add(BrushOutlineManager.GetBrushOutline(brushes[i].brushNodeID));
                                transformations.Add(brushTransformation);
                            }

                            if(wireframes.Count > 0)
                            {
                                CSGRenderer.DrawSelectedBrushes(
                                    instance.zTestLineMeshManager, instance.noZTestLineMeshManager,
                                    wireframes.ToArray(), transformations.ToArray(),
                                    ColorSettings.SelectedOutlines, GUIConstants.thickLineScale
                                );
                            }

                            MaterialUtility.LineDashMultiplier      = 1.0f;
                            MaterialUtility.LineThicknessMultiplier = 1.0f;
                            MaterialUtility.LineAlphaMultiplier     = 1.0f;
                            instance.zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);
                            instance.zTestLineMeshManager.Render(MaterialUtility.ZTestGenericLine);
                        }
                    }
                }
            }

            int       sceneWindowId   = GUIUtility.GetControlID(SceneWindowHash, FocusType.Passive);
            EventType sceneWindowType = Event.current.GetTypeForControl(sceneWindowId);

            if(sceneWindowType == EventType.Repaint)
            {
                if(currentEditorWindows.Count > 0)
                {
                    for(int i = 0; i < currentEditorWindows.Count; i++)
                    {
                        currentEditorWindows[i].Repaint();
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
                    if(currentEditorWindows.Count == 0)
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

        public static void GenerateFromSurface(
            Camera camera, CSGBrush cSGBrush, CSGPlane polygonPlane, Vector3 direction, Vector3[] points
          , int[] pointIndices, uint[] smoothingGroups, bool drag, CSGOperationType forceDragSource
          , bool autoCommitExtrusion
        )
        {
            EditMode = ToolEditMode.Generate;
            UpdateTool();
            EditModeGenerate generateBrushTool = brushTools[(int)ToolEditMode.Generate] as EditModeGenerate;

            generateBrushTool.GenerateFromPolygon(
                camera, cSGBrush, polygonPlane, direction, points, pointIndices, smoothingGroups, drag, forceDragSource
              , autoCommitExtrusion
            );
        }

        public delegate void SetTransformation(Transform newTransform, Transform originalTransform);

        public static Transform[] CloneTargets(SetTransformation setTransform = null)
        {
            if(instance.filteredSelection.NodeTargets.Length == 0)
            {
                return new Transform[0];
            }

            int groupId = Undo.GetCurrentGroup();
            //Undo.IncrementCurrentGroup();

            GameObject[] newTargets    = new GameObject[instance.filteredSelection.NodeTargets.Length];
            Transform[]  newTransforms = new Transform[instance.filteredSelection.NodeTargets.Length];

            for(int i = 0; i < instance.filteredSelection.NodeTargets.Length; i++)
            {
                GameObject originalGameObject = instance.filteredSelection.NodeTargets[i].gameObject;
                Transform  originalTransform  = originalGameObject.GetComponent<Transform>();

                newTargets[i] = CSGPrefabUtility.Instantiate(originalGameObject);
                Transform newTransform = newTargets[i].GetComponent<Transform>();

                if(originalTransform.parent != null)
                {
                    newTransform.SetParent(originalTransform.parent, false);
                    newTransform.SetSiblingIndex(originalTransform.GetSiblingIndex() + 1);

                    newTransform.name = GameObjectUtility.GetUniqueNameForSibling(
                        originalTransform.parent, originalTransform.name
                    );
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
                    for(int g = 0; g < childBrush.Shape.TexGens.Length; g++)
                    {
                        uint smoothingGroup = childBrush.Shape.TexGens[g].SmoothingGroup;

                        if(smoothingGroup == 0)
                        {
                            continue;
                        }

                        uint newSmoothingGroup;

                        if(!uniqueSmoothingGroups.TryGetValue(smoothingGroup, out newSmoothingGroup))
                        {
                            newSmoothingGroup                     = SurfaceUtility.FindUnusedSmoothingGroupIndex();
                            uniqueSmoothingGroups[smoothingGroup] = newSmoothingGroup;
                        }

                        childBrush.Shape.TexGens[g].SmoothingGroup = newSmoothingGroup;
                    }
                }

                newTransforms[i] = newTransform;
                Undo.RegisterCreatedObjectUndo(newTargets[i], "Created clone of " + originalGameObject.name);
            }

            Selection.objects = newTargets;
            Undo.CollapseUndoOperations(groupId);

            return newTransforms;
        }
    }
}

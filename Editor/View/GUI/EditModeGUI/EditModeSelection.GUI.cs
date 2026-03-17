using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal sealed partial class EditModeSelectionGUI
    {
        private static int SceneViewBrushEditorOverlayHash = "SceneViewBrushEditorOverlay".GetHashCode();

        public static float OnEditModeSelectionGUI()
        {
            CSG_GUIStyleUtility.InitStyles();

            EditorGUI.BeginChangeCheck();
            Rect editModeBounds;

            ToolEditMode newEditMode = (ToolEditMode)CSG_EditorGUIUtility.ToolbarWrapped(
                (int)EditModeManager.EditMode, ref editModeRects, out editModeBounds
              , CSG_GUIStyleUtility.brushEditModeContent, CSG_GUIStyleUtility.brushEditModeTooltips
            );

            if(EditorGUI.EndChangeCheck())
            {
                EditModeManager.EditMode = newEditMode;
                CSG_EditorGUIUtility.RepaintAll();
            }

            GUILayout.Space(editModeBounds.height);

            return editModeBounds.height;
        }

        private static GUIStyle sceneViewOverlayTransparentBackground = "SceneViewOverlayTransparentBackground";
        private static GUI.WindowFunction windowFunction = new GUI.WindowFunction(HandleSceneGUI);

        public static void HandleWindowGUI(Rect windowRect)
        {
            GUILayout.Window(
                SceneViewBrushEditorOverlayHash,
                windowRect,
                windowFunction,
                string.Empty, sceneViewOverlayTransparentBackground,
                CSG_GUIStyleUtility.ContentEmpty
            );
        }

        private static void HandleSceneGUI(int id)
        {
            SceneView sceneView = SceneView.currentDrawingSceneView;
            TooltipUtility.InitToolTip(sceneView);
            GUISkin originalSkin = GUI.skin;

            {
                if(!SceneDragToolManager.IsDraggingObjectInScene)
                {
                    OnEditModeSelectionSceneGUI();
                }
#if UNITY_2018_3_OR_NEWER
                Rect viewRect = new Rect(
                    4, 0, sceneView.rootVisualElement.contentRect.width
                  , sceneView.rootVisualElement.contentRect.height - (CSG_GUIStyleUtility.BottomToolBarHeight + 4)
                );
#else
                var viewRect =
 new Rect( 4, 0, sceneView.position.width, sceneView.position.height - ( CSG_GUIStyleUtility.BottomToolBarHeight + 4 ) );
#endif
                GUILayout.BeginArea(viewRect);

                if(EditModeManager.ActiveTool != null)
                {
                    EditModeManager.ActiveTool.OnSceneGUI(viewRect);
                }

                GUILayout.EndArea();

                if(CSGSettings.EnableRealtimeCSG && !SceneDragToolManager.IsDraggingObjectInScene)
                {
                    SceneViewBottomBarGUI.ShowGUI(sceneView, false);
                }
            }

            GUI.skin = originalSkin;
            Handles.BeginGUI();
            TooltipUtility.DrawToolTip(false);
            Handles.EndGUI();
        }

        //static Vector2 scrollPos;

        public static void HandleWindowGUI(EditorWindow window)
        {
            TooltipUtility.InitToolTip(window);
            GUISkin originalSkin = GUI.skin;

            {
                float height = OnEditModeSelectionGUI();
                //var applyOffset = !TooltipUtility.FoundToolTip();

                EditorGUILayout.Space();

                ShowRealtimeCSGDisabledMessage();

                //scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                {
                    EditorGUI.BeginDisabledGroup(!CSGSettings.EnableRealtimeCSG);

                    {
                        if(EditModeManager.ActiveTool != null)
                        {
                            EditModeManager.ActiveTool.OnInspectorGUI(window, height);
                        }
                    }

                    EditorGUI.EndDisabledGroup();
                }
                //EditorGUILayout.EndScrollView();
                //if (applyOffset)
                //	TooltipUtility.HandleAreaOffset(scrollPos);

                GUILayout.Label(VersionLabel, EditorStyles.miniLabel);
            }

            GUI.skin = originalSkin;
            TooltipUtility.DrawToolTip();
        }

        private static Rect[] editModeRects;

        private static void OnEditModeSelectionSceneGUI()
        {
            CSG_GUIStyleUtility.InitStyles();

            if(CSG_GUIStyleUtility.brushEditModeNames == null ||
               CSG_GUIStyleUtility.brushEditModeNames.Length == 0)
            {
                return;
            }

            GUISkin oldSkin = GUI.skin;
            CSG_GUIStyleUtility.SetDefaultGUISkin();
            GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

            {
                GUIStyle windowStyle = GUI.skin.window;

                float topBarSize = 20;
#if UNITY_2018_3_OR_NEWER
                if(CSGPrefabUtility.AreInPrefabMode())
                {
                    topBarSize += 25;
                }
#endif

                Rect bounds = new Rect(10, 10 + topBarSize, 500, 40);

                GUILayout.BeginArea(bounds, ContentTitleLabel, windowStyle);

                {
                    //GUILayout.Space(bounds.height);
                    Rect editModeBounds;

                    CSG_GUIStyleUtility.InitStyles();
                    EditorGUI.BeginChangeCheck();

                    ToolEditMode newEditMode = (ToolEditMode)CSG_EditorGUIUtility.ToolbarWrapped(
                        (int)EditModeManager.EditMode, ref editModeRects, out editModeBounds
                      , CSG_GUIStyleUtility.brushEditModeContent, CSG_GUIStyleUtility.brushEditModeTooltips, 20
                      , bounds.width
                    );

                    //var newEditMode = (ToolEditMode)GUILayout.Toolbar((int)CSGBrushEditorManager.EditMode, GUIStyleUtility.brushEditModeContent, GUIStyleUtility.brushEditModeTooltips);
                    if(EditorGUI.EndChangeCheck())
                    {
                        EditModeManager.EditMode = newEditMode;
                        CSG_EditorGUIUtility.RepaintAll();
                    }

                    Rect buttonArea = bounds;
                    buttonArea.x      = bounds.width - 17;
                    buttonArea.y      = 2;
                    buttonArea.height = 13;
                    buttonArea.width  = 13;

                    if(GUI.Button(buttonArea, GUIContent.none, "SearchCancelButton"))
                    {
                        EditModeToolWindowSceneGUI.GetWindow();
                    }

                    TooltipUtility.SetToolTip(CSG_GUIStyleUtility.PopOutTooltip, buttonArea);

                    Vector2 versionWidth = CSG_GUIStyleUtility.versionLabelStyle.CalcSize(VersionLabel);
                    Rect    versionArea  = bounds;
                    versionArea.x      = bounds.width - (17 + versionWidth.x);
                    versionArea.y      = 1;
                    versionArea.height = 15;
                    versionArea.width  = versionWidth.x;
                    GUI.Label(versionArea, VersionLabel, CSG_GUIStyleUtility.versionLabelStyle);
                }

                GUILayout.EndArea();

                int controlID = GUIUtility.GetControlID(SceneViewBrushEditorOverlayHash, FocusType.Keyboard, bounds);

                switch(Event.current.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                    {
                        if(bounds.Contains(Event.current.mousePosition))
                        {
                            GUIUtility.hotControl             = controlID;
                            GUIUtility.keyboardControl        = controlID;
                            EditorGUIUtility.editingTextField = false;
                            Event.current.Use();
                        }

                        break;
                    }
                    case EventType.MouseMove:
                    {
                        if(bounds.Contains(Event.current.mousePosition))
                        {
                            Event.current.Use();
                        }

                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if(GUIUtility.hotControl == controlID)
                        {
                            GUIUtility.hotControl      = 0;
                            GUIUtility.keyboardControl = 0;
                            Event.current.Use();
                        }

                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        if(GUIUtility.hotControl == controlID)
                        {
                            Event.current.Use();
                        }

                        break;
                    }
                    case EventType.ScrollWheel:
                    {
                        if(bounds.Contains(Event.current.mousePosition))
                        {
                            Event.current.Use();
                        }

                        break;
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUI.skin = oldSkin;
        }

        private static Camera MainCamera
        {
            get
            {
                Camera mainCamera = Camera.main;

                if(mainCamera != null)
                {
                    return mainCamera;
                }

                Camera[] allCameras = Camera.allCameras;

                if(allCameras != null && allCameras.Length == 1)
                {
                    return allCameras[0];
                }

                return null;
            }
        }

        public static void ShowRealtimeCSGDisabledMessage()
        {
            if(!CSGSettings.EnableRealtimeCSG)
            {
                GUILayout.BeginVertical(CSG_GUIStyleUtility.redTextArea);

                {
                    GUILayout.Label(DisabledLabelContent, CSG_GUIStyleUtility.redTextLabel);

                    if(GUILayout.Button(EnableRealtimeCSGContent))
                    {
                        CSGSettings.SetRealtimeCSGEnabled(true);
                    }
                }

                GUILayout.EndVertical();
            }
        }

        private static bool OpenSurfaces = false;

        public static void OnInspectorGUI(Editor editor, Object[] targets)
        {
            TooltipUtility.InitToolTip(editor);

            try
            {
                CSGModel[] models = new CSGModel[targets.Length];

                for(int i = targets.Length - 1; i >= 0; i--)
                {
                    models[i] = targets[i] as CSGModel;

                    if(!models[i])
                    {
                        ArrayUtility.RemoveAt(ref models, i);
                    }
                }

                CSG_GUIStyleUtility.InitStyles();
                ShowRealtimeCSGDisabledMessage();

                if(models.Length > 0 && models.Length == targets.Length)
                {
                    CSGModelComponentInspectorGUI.OnInspectorGUI(targets);

                    return;
                }

                FilteredSelection filteredSelection = EditModeManager.FilteredSelection;
                CSGNode[]         targetNodes       = filteredSelection.nodeTargets;
                CSGModel[]        targetModels      = filteredSelection.modelTargets;
                CSGBrush[]        targetBrushes     = filteredSelection.brushTargets;
                CSGOperation[]    targetOperations  = filteredSelection.operationTargets;

                if(targetNodes == null)
                {
                    return;
                }

                bool?                       isPrefab                   = false;
                PrefabInstantiateBehaviour? prefabBehaviour            = PrefabInstantiateBehaviour.Reference;
                PrefabSourceAlignment?      prefabSourceAlignment      = PrefabSourceAlignment.AlignedTop;
                PrefabDestinationAlignment? prefabDestinationAlignment = PrefabDestinationAlignment.AlignToSurface;

                if(targetNodes.Length > 0)
                {
                    GameObject gameObject = targetNodes[0].gameObject;
                    isPrefab                   = CSGPrefabUtility.IsPrefabAsset(gameObject);
                    prefabBehaviour            = targetNodes[0].PrefabBehaviour;
                    prefabSourceAlignment      = targetNodes[0].PrefabSourceAlignment;
                    prefabDestinationAlignment = targetNodes[0].PrefabDestinationAlignment;

                    for(int i = 1; i < targetNodes.Length; i++)
                    {
                        gameObject = targetNodes[i].gameObject;

                        bool                       currentIsPrefab = CSGPrefabUtility.IsPrefabAsset(gameObject);
                        PrefabInstantiateBehaviour currentPrefabBehaviour = targetNodes[i].PrefabBehaviour;
                        PrefabSourceAlignment      currentPrefabSourceAlignment = targetNodes[i].PrefabSourceAlignment;

                        PrefabDestinationAlignment currentPrefabDestinationAlignment =
                            targetNodes[i].PrefabDestinationAlignment;

                        if(isPrefab.HasValue && isPrefab.Value != currentIsPrefab)
                        {
                            isPrefab = null;
                        }

                        if(prefabBehaviour.HasValue && prefabBehaviour.Value != currentPrefabBehaviour)
                        {
                            prefabBehaviour = null;
                        }

                        if(prefabSourceAlignment.HasValue
                        && prefabSourceAlignment.Value != currentPrefabSourceAlignment)
                        {
                            prefabSourceAlignment = null;
                        }

                        if(prefabDestinationAlignment.HasValue
                        && prefabDestinationAlignment.Value != currentPrefabDestinationAlignment)
                        {
                            prefabDestinationAlignment = null;
                        }
                    }
                }

                GUILayout.BeginVertical(GUI.skin.box);

                {
                    if(isPrefab.HasValue && isPrefab.Value)
                    {
                        EditorGUILayout.LabelField(PrefabLabelContent);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(RaySnappingLabelContent);
                        TooltipUtility.SetToolTip(RaySnappingBehaviourTooltip);
                    }

                    EditorGUI.indentLevel++;

                    {
                        if(isPrefab.HasValue && isPrefab.Value)
                        {
                            EditorGUI.showMixedValue = !prefabBehaviour.HasValue;

                            PrefabInstantiateBehaviour prefabBehavour = prefabBehaviour.HasValue
                                ? prefabBehaviour.Value
                                : PrefabInstantiateBehaviour.Reference;

                            EditorGUI.BeginChangeCheck();

                            {
                                prefabBehavour = (PrefabInstantiateBehaviour)EditorGUILayout.EnumPopup(
                                    PrefabInstantiateBehaviourContent, prefabBehavour
                                );

                                TooltipUtility.SetToolTip(PrefabInstantiateBehaviourTooltip);
                            }

                            if(EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                                for(int i = 0; i < targetNodes.Length; i++)
                                {
                                    targetNodes[i].PrefabBehaviour = prefabBehavour;
                                }
                            }

                            EditorGUI.showMixedValue = false;
                        }

                        EditorGUI.showMixedValue = !prefabDestinationAlignment.HasValue;

                        PrefabDestinationAlignment destinationAlignment = prefabDestinationAlignment.HasValue
                            ? prefabDestinationAlignment.Value
                            : PrefabDestinationAlignment.AlignToSurface;

                        EditorGUI.BeginChangeCheck();

                        {
                            destinationAlignment = (PrefabDestinationAlignment)EditorGUILayout.EnumPopup(
                                DestinationAlignmentContent, destinationAlignment
                            );

                            TooltipUtility.SetToolTip(DestinationAlignmentTooltip);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                            for(int i = 0; i < targetNodes.Length; i++)
                            {
                                targetNodes[i].PrefabDestinationAlignment = destinationAlignment;
                            }
                        }

                        EditorGUI.showMixedValue = false;

                        EditorGUI.showMixedValue = !prefabSourceAlignment.HasValue;

                        PrefabSourceAlignment sourceAlignment = prefabSourceAlignment.HasValue
                            ? prefabSourceAlignment.Value
                            : PrefabSourceAlignment.AlignedFront;

                        EditorGUI.BeginChangeCheck();

                        {
                            sourceAlignment = (PrefabSourceAlignment)EditorGUILayout.EnumPopup(
                                SourceAlignmentContent, sourceAlignment
                            );

                            TooltipUtility.SetToolTip(SourceAlignmentTooltip);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                            for(int i = 0; i < targetNodes.Length; i++)
                            {
                                targetNodes[i].PrefabSourceAlignment = sourceAlignment;
                            }
                        }

                        EditorGUI.showMixedValue = false;
                    }

                    EditorGUI.indentLevel--;
                }

                GUILayout.EndVertical();
                GUILayout.Space(10);

                if(targetModels.Length == 0)
                {
                    int              invalidOperationType = 999;
                    bool?            handleAsOne          = null;
                    bool             selMixedValues       = false;
                    CSGOperationType operationType        = (CSGOperationType)invalidOperationType;
                    bool             opMixedValues        = false;

                    if(targetBrushes.Length > 0)
                    {
                        operationType = targetBrushes[0].OperationType;
                    }

                    for(int b = 1; b < targetBrushes.Length; b++)
                    {
                        CSGBrush brush = targetBrushes[b];

                        if(operationType != brush.OperationType)
                        {
                            opMixedValues = true;
                        }
                    }

                    foreach(CSGOperation operation in targetOperations)
                    {
                        if(operationType == (CSGOperationType)invalidOperationType)
                        {
                            operationType = operation.OperationType;
                        }
                        else if(operationType != operation.OperationType)
                        {
                            opMixedValues = true;
                        }

                        if(!handleAsOne.HasValue)
                        {
                            handleAsOne = operation.HandleAsOne;
                        }
                        else if(handleAsOne.Value != operation.HandleAsOne)
                        {
                            selMixedValues = true;
                        }
                    }

                    GUILayout.BeginVertical(GUI.skin.box);

                    {
                        bool passThroughValue = false;

                        if(targetBrushes.Length == 0 && targetOperations.Length > 0) // only operations
                        {
                            bool? passThrough = targetOperations[0].PassThrough;

                            for(int i = 1; i < targetOperations.Length; i++)
                            {
                                if(passThrough.HasValue && passThrough.Value != targetOperations[i].PassThrough)
                                {
                                    passThrough = null;

                                    break;
                                }
                            }

                            opMixedValues = !passThrough.HasValue || passThrough.Value;

                            bool ptMixedValues = !passThrough.HasValue;
                            passThroughValue = passThrough.HasValue ? passThrough.Value : false;

                            if(CSG_EditorGUIUtility.PassThroughButton(passThroughValue, ptMixedValues))
                            {
                                Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                                foreach(CSGOperation operation in targetOperations)
                                {
                                    operation.PassThrough = true;
                                }

                                InternalCSGModelManager.CheckForChanges();
                                EditorApplication.RepaintHierarchyWindow();
                            }

                            if(passThroughValue)
                            {
                                operationType = (CSGOperationType)255;
                            }
                        }

                        EditorGUI.BeginChangeCheck();

                        {
                            operationType = CSG_EditorGUIUtility.ChooseOperation(operationType, opMixedValues);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                            foreach(CSGBrush brush in targetBrushes)
                            {
                                brush.OperationType = operationType;
                            }

                            foreach(CSGOperation operation in targetOperations)
                            {
                                operation.PassThrough   = false;
                                operation.OperationType = operationType;
                            }

                            InternalCSGModelManager.CheckForChanges();
                            EditorApplication.RepaintHierarchyWindow();
                        }
                    }

                    GUILayout.EndVertical();

                    if(targetOperations.Length == 0 && targetModels.Length == 0)
                    {
                        GUILayout.Space(10);

                        if(targetBrushes.Length == 1)
                        {
                            GUILayout.BeginVertical(GUI.skin.box);

                            {
                                EditorGUI.indentLevel++;
                                OpenSurfaces = EditorGUILayout.Foldout(OpenSurfaces, SurfacesContent);
                                EditorGUI.indentLevel--;

                                if(OpenSurfaces)
                                {
                                    Shape         targetShape     = targetBrushes[0].Shape;
                                    TexGen[]      texGens         = targetShape.TexGens;
                                    TexGenFlags[] texGenFlagArray = targetShape.TexGenFlags;

                                    for(int t = 0; t < texGens.Length; t++)
                                    {
                                        GUILayout.Space(2);

                                        TexGenFlags texGenFlags = texGenFlagArray[t];
                                        Material    material    = targetShape.TexGens[t].RenderMaterial;
                                        EditorGUI.BeginChangeCheck();

                                        {
                                            GUILayout.BeginHorizontal();

                                            {
                                                GUILayout.Space(4);
                                                material = CSG_EditorGUIUtility.MaterialImage(material);
                                                GUILayout.Space(2);
                                                GUILayout.BeginVertical();

                                                {
                                                    EditorGUI.BeginDisabledGroup(texGenFlags != TexGenFlags.None);

                                                    {
                                                        material = EditorGUILayout.ObjectField(
                                                            material, typeof(Material), true
                                                        ) as Material;
                                                    }

                                                    EditorGUI.EndDisabledGroup();

                                                    texGenFlags = EditModeCommonGUI.OnSurfaceFlagButtons(texGenFlags);
                                                }

                                                GUILayout.EndVertical();
                                                GUILayout.Space(4);
                                            }

                                            GUILayout.EndHorizontal();
                                        }

                                        if(EditorGUI.EndChangeCheck())
                                        {
                                            SelectedBrushSurface[] selectedBrushSurfaces = new[]
                                            {
                                                new SelectedBrushSurface(targetBrushes[0], t)
                                            };

                                            using(new UndoGroup(selectedBrushSurfaces, "discarding surface"))
                                            {
                                                texGenFlagArray[t]                    = texGenFlags;
                                                targetShape.TexGens[t].RenderMaterial = material;
                                            }
                                        }

                                        GUILayout.Space(4);
                                    }
                                }
                            }

                            GUILayout.EndVertical();
                        }
                    }

                    if(handleAsOne.HasValue)
                    {
                        EditorGUI.BeginChangeCheck();

                        {
                            EditorGUI.showMixedValue = selMixedValues;
                            handleAsOne              = EditorGUILayout.Toggle(HandleAsOneLabel, handleAsOne.Value);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(targetNodes, "Changed CSG operation 'Handle as one object'");

                            foreach(CSGOperation operation in targetOperations)
                            {
                                operation.HandleAsOne = handleAsOne.Value;
                            }

                            EditorApplication.RepaintHierarchyWindow();
                        }
                    }
                }

#if false
				if (targetNodes.Length == 1)
				{
					var node = targetNodes[0];
					var brush = node as CSGBrush;
					if (brush != null)
					{
						var brush_cache = CSGSceneManager.GetBrushCache(brush);
						if (brush_cache == null ||
							brush_cache.childData == null ||
							brush_cache.childData.modelTransform == null)
						{
							EditorGUILayout.LabelField("brush-cache: null");
						} else
						{
							EditorGUILayout.LabelField("node-id: " + brush.brushNodeID);
						}
					}
					var operation = node as CSGOperation;
					if (operation != null)
					{
						var operation_cache = CSGSceneManager.GetOperationCache(operation);
						if (operation_cache == null ||
							operation_cache.childData == null ||
							operation_cache.childData.modelTransform == null)
						{
							EditorGUILayout.LabelField("operation-cache: null");
						} else
						{
							EditorGUILayout.LabelField("operation-id: " + operation.operationNodeID);
						}
					}
					var model = node as CSGModel;
					if (model != null)
					{
						var model_cache = CSGSceneManager.GetModelCache(model);
						if (model_cache == null ||
							model_cache.meshContainer == null)
						{
							EditorGUILayout.LabelField("model-cache: null");
						}  else
						{
							EditorGUILayout.LabelField("model-id: " + model.modelNodeID);
						}
					}
				}
#endif
            }
            finally
            {
                TooltipUtility.DrawToolTip(true, true);
            }
        }
    }
}

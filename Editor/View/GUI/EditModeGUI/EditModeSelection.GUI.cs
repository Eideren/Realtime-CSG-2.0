using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditModeSelectionGUI
    {
        private static readonly int sceneViewBrushEditorOverlayHash = "SceneViewBrushEditorOverlay".GetHashCode();

        public static float OnEditModeSelectionGUI()
        {
            EditorGUI.BeginChangeCheck();

            ToolEditMode newEditMode = (ToolEditMode)CSGEditorGUIUtility.ToolbarWrapped((int)EditModeManager.EditMode, ref m_EditModeRects, out Rect editModeBounds, CsgguiStyleUtility.BrushEditModeContent, CsgguiStyleUtility.BrushEditModeTooltips);

            if(EditorGUI.EndChangeCheck())
            {
                EditModeManager.EditMode = newEditMode;
                CSGEditorGUIUtility.RepaintAll();
            }

            GUILayout.Space(editModeBounds.height);

            return editModeBounds.height;
        }

        private static readonly GUIStyle sceneViewOverlayTransparentBackground = "SceneViewOverlayTransparentBackground";

        private static readonly GUI.WindowFunction windowFunction = HandleSceneGUI;

        public static void HandleWindowGUI(Rect windowRect)
        {
            GUILayout.Window(sceneViewBrushEditorOverlayHash, windowRect, windowFunction, string.Empty, sceneViewOverlayTransparentBackground, CsgguiStyleUtility.contentEmpty);
        }

        private static void HandleSceneGUI(int id)
        {
            SceneView sceneView = SceneView.currentDrawingSceneView;
            TooltipUtility.InitToolTip(sceneView);
            GUISkin originalSkin = GUI.skin;

            if(!SceneDragToolManager.IsDraggingObjectInScene)
            {
                OnEditModeSelectionSceneGUI();
            }

            GUI.skin = originalSkin;
            Handles.BeginGUI();
            TooltipUtility.DrawToolTip(false);
            Handles.EndGUI();
        }

        public static void HandleWindowGUI(EditorWindow window)
        {
            TooltipUtility.InitToolTip(window);
            GUISkin originalSkin = GUI.skin;

            float height = OnEditModeSelectionGUI();

            EditorGUILayout.Space();

            ShowRealtimeCSGDisabledMessage();

            EditorGUI.BeginDisabledGroup(!CSGSettings.EnableRealtimeCSG);

            if(EditModeManager.ActiveTool != null)
            {
                EditModeManager.ActiveTool.OnInspectorGUI(window, height);
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.Label(versionLabel, EditorStyles.miniLabel);

            GUI.skin = originalSkin;
            TooltipUtility.DrawToolTip();
        }

        private static Rect[] m_EditModeRects;

        private static void OnEditModeSelectionSceneGUI()
        {
            if(CsgguiStyleUtility.BrushEditModeNames == null || CsgguiStyleUtility.BrushEditModeNames.Length == 0)
            {
                return;
            }

            GUISkin oldSkin = GUI.skin;
            CsgguiStyleUtility.SetDefaultGUISkin();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUIStyle windowStyle = GUI.skin.window;

            float topBarSize = 5;

            Rect bounds = new Rect(6, topBarSize, 500, 40);

            GUILayout.BeginArea(bounds, contentTitleLabel, windowStyle);

            EditorGUI.BeginChangeCheck();

            ToolEditMode newEditMode = (ToolEditMode)CSGEditorGUIUtility.ToolbarWrapped((int)EditModeManager.EditMode, ref m_EditModeRects, out Rect _, CsgguiStyleUtility.BrushEditModeContent, CsgguiStyleUtility.BrushEditModeTooltips, 20, bounds.width);

            if(EditorGUI.EndChangeCheck())
            {
                EditModeManager.EditMode = newEditMode;
                CSGEditorGUIUtility.RepaintAll();
            }

            Rect buttonArea = bounds;
            buttonArea.x      = bounds.width - 17;
            buttonArea.y      = 2;
            buttonArea.height = 13;
            buttonArea.width  = 13;

            if (GUI.Button(buttonArea, EditorGUIUtility.IconContent("d_SearchJump Icon"), GUIStyle.none))
            {
                EditModeToolWindowSceneGUI.GetWindow();
            }

            TooltipUtility.SetToolTip(CsgguiStyleUtility.popOutTooltip, buttonArea);

            Vector2 versionWidth = CsgguiStyleUtility.VersionLabelStyle.CalcSize(versionLabel);
            Rect    versionArea  = bounds;
            versionArea.x      = bounds.width - (17 + versionWidth.x);
            versionArea.y      = 1;
            versionArea.height = 15;
            versionArea.width  = versionWidth.x;
            GUI.Label(versionArea, versionLabel, CsgguiStyleUtility.VersionLabelStyle);

            GUILayout.EndArea();

            int controlID = GUIUtility.GetControlID(sceneViewBrushEditorOverlayHash, FocusType.Keyboard, bounds);

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
                GUILayout.BeginVertical(CsgguiStyleUtility.RedTextArea);

                GUILayout.Label(disabledLabelContent, CsgguiStyleUtility.RedTextLabel);

                if(GUILayout.Button(enableRealtimeCSGContent))
                {
                    CSGSettings.SetRealtimeCSGEnabled(true);
                }

                GUILayout.EndVertical();
            }
        }

        private static bool m_OpenSurfaces;

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
                    prefabBehaviour            = targetNodes[0].prefabBehaviour;
                    prefabSourceAlignment      = targetNodes[0].prefabSourceAlignment;
                    prefabDestinationAlignment = targetNodes[0].prefabDestinationAlignment;

                    for(int i = 1; i < targetNodes.Length; i++)
                    {
                        gameObject = targetNodes[i].gameObject;

                        bool currentIsPrefab = CSGPrefabUtility.IsPrefabAsset(gameObject);

                        PrefabInstantiateBehaviour currentPrefabBehaviour            = targetNodes[i].prefabBehaviour;
                        PrefabSourceAlignment      currentPrefabSourceAlignment      = targetNodes[i].prefabSourceAlignment;
                        PrefabDestinationAlignment currentPrefabDestinationAlignment = targetNodes[i].prefabDestinationAlignment;

                        if(isPrefab.HasValue && isPrefab.Value != currentIsPrefab)
                        {
                            isPrefab = null;
                        }

                        if(prefabBehaviour.HasValue && prefabBehaviour.Value != currentPrefabBehaviour)
                        {
                            prefabBehaviour = null;
                        }

                        if(prefabSourceAlignment.HasValue && prefabSourceAlignment.Value != currentPrefabSourceAlignment)
                        {
                            prefabSourceAlignment = null;
                        }

                        if(prefabDestinationAlignment.HasValue && prefabDestinationAlignment.Value != currentPrefabDestinationAlignment)
                        {
                            prefabDestinationAlignment = null;
                        }
                    }
                }

                GUILayout.BeginVertical(GUI.skin.box);

                {
                    if(isPrefab.HasValue && isPrefab.Value)
                    {
                        EditorGUILayout.LabelField(prefabLabelContent);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(raySnappingLabelContent);
                        TooltipUtility.SetToolTip(raySnappingBehaviourTooltip);
                    }

                    EditorGUI.indentLevel++;

                    if(isPrefab.HasValue && isPrefab.Value)
                    {
                        EditorGUI.showMixedValue = !prefabBehaviour.HasValue;

                        PrefabInstantiateBehaviour prefabBehavour = prefabBehaviour.HasValue ? prefabBehaviour.Value : PrefabInstantiateBehaviour.Reference;

                        EditorGUI.BeginChangeCheck();

                        prefabBehavour = (PrefabInstantiateBehaviour)EditorGUILayout.EnumPopup(prefabInstantiateBehaviourContent, prefabBehavour);

                        TooltipUtility.SetToolTip(prefabInstantiateBehaviourTooltip);

                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                            for(int i = 0; i < targetNodes.Length; i++)
                            {
                                targetNodes[i].prefabBehaviour = prefabBehavour;
                            }
                        }

                        EditorGUI.showMixedValue = false;
                    }

                    EditorGUI.showMixedValue = !prefabDestinationAlignment.HasValue;

                    PrefabDestinationAlignment destinationAlignment = prefabDestinationAlignment.HasValue ? prefabDestinationAlignment.Value : PrefabDestinationAlignment.AlignToSurface;

                    EditorGUI.BeginChangeCheck();

                    destinationAlignment = (PrefabDestinationAlignment)EditorGUILayout.EnumPopup(destinationAlignmentContent, destinationAlignment);

                    TooltipUtility.SetToolTip(destinationAlignmentTooltip);

                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                        for(int i = 0; i < targetNodes.Length; i++)
                        {
                            targetNodes[i].prefabDestinationAlignment = destinationAlignment;
                        }
                    }

                    EditorGUI.showMixedValue = false;

                    EditorGUI.showMixedValue = !prefabSourceAlignment.HasValue;

                    PrefabSourceAlignment sourceAlignment = prefabSourceAlignment.HasValue ? prefabSourceAlignment.Value : PrefabSourceAlignment.AlignedFront;

                    EditorGUI.BeginChangeCheck();

                    sourceAlignment = (PrefabSourceAlignment)EditorGUILayout.EnumPopup(sourceAlignmentContent, sourceAlignment);

                    TooltipUtility.SetToolTip(sourceAlignmentTooltip);

                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                        for(int i = 0; i < targetNodes.Length; i++)
                        {
                            targetNodes[i].prefabSourceAlignment = sourceAlignment;
                        }
                    }

                    EditorGUI.showMixedValue = false;

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
                        operationType = targetBrushes[0].operationType;
                    }

                    for(int b = 1; b < targetBrushes.Length; b++)
                    {
                        CSGBrush brush = targetBrushes[b];

                        if(operationType != brush.operationType)
                        {
                            opMixedValues = true;
                        }
                    }

                    foreach(CSGOperation operation in targetOperations)
                    {
                        if(operationType == (CSGOperationType)invalidOperationType)
                        {
                            operationType = operation.operationType;
                        }
                        else if(operationType != operation.operationType)
                        {
                            opMixedValues = true;
                        }

                        if(!handleAsOne.HasValue)
                        {
                            handleAsOne = operation.handleAsOne;
                        }
                        else if(handleAsOne.Value != operation.handleAsOne)
                        {
                            selMixedValues = true;
                        }
                    }

                    GUILayout.BeginVertical(GUI.skin.box);

                    {
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

                            bool ptMixedValues    = !passThrough.HasValue;
                            bool passThroughValue = passThrough.HasValue && passThrough.Value;

                            if(CSGEditorGUIUtility.PassThroughButton(passThroughValue, ptMixedValues))
                            {
                                Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                                foreach(CSGOperation operation in targetOperations)
                                {
                                    operation.PassThrough = true;
                                }

                                InternalCSGModelManager.CheckForChanges();
                                EditorApplication.RepaintHierarchyWindow();
#if UNITY_6000_5_OR_NEWER
                                HierarchyWindowItemGUI.RepaintHierarchy();
#endif
                            }

                            if (passThroughValue)
                            {
                                operationType = (CSGOperationType)255;
                            }
                        }

                        EditorGUI.BeginChangeCheck();

                        operationType = CSGEditorGUIUtility.ChooseOperation(operationType, opMixedValues);

                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(targetNodes, "Changed CSG operation of nodes");

                            foreach(CSGBrush brush in targetBrushes)
                            {
                                brush.operationType = operationType;
                            }

                            foreach(CSGOperation operation in targetOperations)
                            {
                                operation.PassThrough   = false;
                                operation.operationType = operationType;
                            }

                            InternalCSGModelManager.CheckForChanges();
                            EditorApplication.RepaintHierarchyWindow();
#if UNITY_6000_5_OR_NEWER
                            HierarchyWindowItemGUI.RepaintHierarchy();
#endif
                        }
                    }

                    GUILayout.EndVertical();

                    if(targetOperations.Length == 0 && targetModels.Length == 0)
                    {
                        GUILayout.Space(10);

                        if(targetBrushes.Length == 1)
                        {
                            GUILayout.BeginVertical(GUI.skin.box);

                            EditorGUI.indentLevel++;
                            m_OpenSurfaces = EditorGUILayout.Foldout(m_OpenSurfaces, surfacesContent);
                            EditorGUI.indentLevel--;

                            if(m_OpenSurfaces)
                            {
                                Shape         targetShape     = targetBrushes[0].shape;
                                TexGen[]      texGens         = targetShape.texGens;
                                TexGenFlags[] texGenFlagArray = targetShape.texGenFlags;

                                for(int t = 0; t < texGens.Length; t++)
                                {
                                    GUILayout.Space(2);

                                    TexGenFlags texGenFlags = texGenFlagArray[t];
                                    Material    material    = targetShape.texGens[t].RenderMaterial;
                                    EditorGUI.BeginChangeCheck();

                                    GUILayout.BeginHorizontal();

                                    GUILayout.Space(4);
                                    material = CSGEditorGUIUtility.MaterialImage(material);
                                    GUILayout.Space(2);
                                    GUILayout.BeginVertical();

                                    EditorGUI.BeginDisabledGroup(texGenFlags != TexGenFlags.None);

                                    material = EditorGUILayout.ObjectField(material, typeof(Material), true) as Material;

                                    EditorGUI.EndDisabledGroup();

                                    texGenFlags = EditModeCommonGUI.OnSurfaceFlagButtons(texGenFlags);

                                    GUILayout.EndVertical();
                                    GUILayout.Space(4);

                                    GUILayout.EndHorizontal();

                                    if(EditorGUI.EndChangeCheck())
                                    {
                                        SelectedBrushSurface[] selectedBrushSurfaces = { new SelectedBrushSurface(targetBrushes[0], t) };

                                        using(new UndoGroup(selectedBrushSurfaces, "discarding surface"))
                                        {
                                            texGenFlagArray[t]                    = texGenFlags;
                                            targetShape.texGens[t].RenderMaterial = material;
                                        }
                                    }

                                    GUILayout.Space(4);
                                }
                            }

                            GUILayout.EndVertical();
                        }
                    }

                    if(handleAsOne.HasValue)
                    {
                        EditorGUI.BeginChangeCheck();

                        EditorGUI.showMixedValue = selMixedValues;
                        handleAsOne              = EditorGUILayout.Toggle(handleAsOneLabel, handleAsOne.Value);

                        if(EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObjects(targetNodes, "Changed CSG operation 'Handle as one object'");

                            foreach(CSGOperation operation in targetOperations)
                            {
                                operation.handleAsOne = handleAsOne.Value;
                            }

                            EditorApplication.RepaintHierarchyWindow();
                        }
                    }
                }
            }
            finally
            {
                TooltipUtility.DrawToolTip(true, true);
            }
        }
    }
}

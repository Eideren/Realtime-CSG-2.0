using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditModeMeshModeGUI
    {
        private static readonly int sceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

        private static Rect m_LastGuiRect;

        public static Rect GetLastSceneGUIRect()
        {
            return m_LastGuiRect;
        }

        public static void OnSceneGUI(EditModeMeshEdit tool)
        {
            CsgguiStyleUtility.InitStyles();
            InitLocalStyles();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.FlexibleSpace();

            CsgguiStyleUtility.ResetGUIState();

            GUILayout.BeginVertical(m_ContentMeshLabel, CsgguiStyleUtility.UnpaddedWindow, CsgguiStyleUtility.contentEmpty);

            OnGUIContents(true, tool);

            GUILayout.EndVertical();

            Rect currentArea = GUILayoutUtility.GetLastRect();
            m_LastGuiRect = currentArea;

            Rect buttonArea = currentArea;
            buttonArea.x      += buttonArea.width - 17;
            buttonArea.y      += 2;
            buttonArea.height =  13;
            buttonArea.width  =  13;

            if(GUI.Button(buttonArea, GUIContent.none, "SearchCancelButton"))
            {
                EditModeToolWindowSceneGUI.GetWindow();
            }

            TooltipUtility.SetToolTip(CsgguiStyleUtility.popOutTooltip, buttonArea);

            int controlID = GUIUtility.GetControlID(sceneViewMeshOverlayHash, FocusType.Keyboard, currentArea);

            switch(Event.current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                {
                    if(currentArea.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl      = controlID;
                        GUIUtility.keyboardControl = controlID;
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.MouseMove:
                {
                    if(currentArea.Contains(Event.current.mousePosition))
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
                    if(currentArea.Contains(Event.current.mousePosition))
                    {
                        Event.current.Use();
                    }

                    break;
                }
            }

            GUILayout.EndVertical();

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }

        public static void OnInspectorGUI()
        {
            m_LastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
            EditModeMeshEdit tool = EditModeManager.ActiveTool as EditModeMeshEdit;
            CsgguiStyleUtility.InitStyles();
            InitLocalStyles();
            OnGUIContents(false, tool);
        }

        private static void ShowCSGOperations(bool isSceneGUI, EditModeMeshEdit tool, FilteredSelection filteredSelection)
        {
            bool operationsEnabled = tool != null && filteredSelection.nodeTargets.Length > 0 && filteredSelection.modelTargets.Length == 0;

            EditorGUI.BeginDisabledGroup(!operationsEnabled);

            bool mixedValues = tool == null || (filteredSelection.brushTargets.Length == 0 && filteredSelection.operationTargets.Length == 0);

            CSGOperationType operationType = CSGOperationType.Additive;

            if(tool != null)
            {
                if(filteredSelection.brushTargets.Length > 0)
                {
                    operationType = filteredSelection.brushTargets[0].OperationType;

                    for(int i = 1; i < filteredSelection.brushTargets.Length; i++)
                    {
                        if(filteredSelection.brushTargets[i].OperationType != operationType)
                        {
                            mixedValues = true;
                        }
                    }
                }
                else if(filteredSelection.operationTargets.Length > 0)
                {
                    operationType = filteredSelection.operationTargets[0].OperationType;
                }

                if(filteredSelection.operationTargets.Length > 0)
                {
                    for(int i = 0; i < filteredSelection.operationTargets.Length; i++)
                    {
                        if(filteredSelection.operationTargets[i].OperationType != operationType)
                        {
                            mixedValues = true;
                        }
                    }
                }
            }

            GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);

            {
                if(tool != null && filteredSelection.operationTargets.Length > 0 && filteredSelection.operationTargets.Length == filteredSelection.nodeTargets.Length) // only operations
                {
                    bool? passThrough = filteredSelection.operationTargets[0].PassThrough;

                    for(int i = 1; i < filteredSelection.operationTargets.Length; i++)
                    {
                        if(passThrough.HasValue && passThrough.Value != filteredSelection.operationTargets[i].PassThrough)
                        {
                            passThrough = null;

                            break;
                        }
                    }

                    mixedValues = !passThrough.HasValue || passThrough.Value;

                    bool ptMixedValues    = !passThrough.HasValue;
                    bool passThroughValue = passThrough.HasValue && passThrough.Value;

                    if(CSGEditorGUIUtility.PassThroughButton(passThroughValue, ptMixedValues))
                    {
                        Undo.RecordObjects(filteredSelection.operationTargets, "Changed CSG operation of nodes");

                        foreach(CSGOperation operation in filteredSelection.operationTargets)
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
                    operationType = CSGEditorGUIUtility.ChooseOperation(operationType, mixedValues);
                }

                if(EditorGUI.EndChangeCheck() && tool != null)
                {
                    Undo.RecordObjects(filteredSelection.nodeTargets, "Changed CSG operation of nodes");

                    for(int i = 0; i < filteredSelection.brushTargets.Length; i++)
                    {
                        filteredSelection.brushTargets[i].OperationType = operationType;
                    }

                    for(int i = 0; i < filteredSelection.operationTargets.Length; i++)
                    {
                        filteredSelection.operationTargets[i].PassThrough   = false;
                        filteredSelection.operationTargets[i].OperationType = operationType;
                    }

                    InternalCSGModelManager.CheckForChanges();
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            GUILayout.EndVertical();

            EditorGUI.EndDisabledGroup();
        }

        private static void OnGUIContents(bool isSceneGUI, EditModeMeshEdit tool)
        {
            EditModeCommonGUI.StartToolGUI();

            FilteredSelection filteredSelection = EditModeManager.FilteredSelection;

            GUIStyle left   = EditorStyles.miniButtonLeft;
            GUIStyle middle = EditorStyles.miniButtonMid;
            GUIStyle right  = EditorStyles.miniButtonRight;
            GUIStyle button = GUI.skin.button;

            Material defaultMaterial = CSGSettings.DefaultMaterial;
            GUILayout.BeginVertical(isSceneGUI ? inSceneWidth : CsgguiStyleUtility.contentEmpty);

            ShowCSGOperations(isSceneGUI, tool, filteredSelection);
            GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);

            bool selectionIgnoreBackfaced = CSGSettings.HiddenSurfacesNotSelectable;
            bool selectionVertex          = CSGSettings.SelectionVertex;
            bool selectionEdge            = CSGSettings.SelectionEdge;
            bool selectionSurface         = CSGSettings.SelectionSurface;

            EditorGUILayout.LabelField(contentSelection);
            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            selectionIgnoreBackfaced = GUILayout.Toggle(selectionIgnoreBackfaced, contentIgnoreHidden, button);

            TooltipUtility.SetToolTip(tooltipIgnoreHidden);

            GUILayout.EndHorizontal();

            if(!selectionVertex && !selectionEdge && !selectionSurface)
            {
                GUILayout.Label(
                    "No selection mode has been selected and nothing can be selected"
                  , CsgguiStyleUtility.RedTextArea
                );
            }

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            selectionVertex = GUILayout.Toggle(selectionVertex, contentVertex, button);
            TooltipUtility.SetToolTip(tooltipVertex);

            selectionEdge = GUILayout.Toggle(selectionEdge, contentEdge, button);
            TooltipUtility.SetToolTip(tooltipEdge);

            selectionSurface = GUILayout.Toggle(selectionSurface, contentSurface, button);
            TooltipUtility.SetToolTip(tooltipSurface);

            GUILayout.EndHorizontal();

            if(EditorGUI.EndChangeCheck())
            {
                CSGSettings.HiddenSurfacesNotSelectable = selectionIgnoreBackfaced;
                CSGSettings.SelectionVertex             = selectionVertex;
                CSGSettings.SelectionEdge               = selectionEdge;
                CSGSettings.SelectionSurface            = selectionSurface;
                CSGSettings.Save();
            }

            GUILayout.Space(3);

            GUILayout.EndVertical();
            GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);

            bool autoCommitExtrusion = CSGSettings.AutoCommitExtrusion;
            EditorGUI.BeginChangeCheck();

            autoCommitExtrusion = GUILayout.Toggle(autoCommitExtrusion, contentAutoCommitExtrusion, button);
            TooltipUtility.SetToolTip(tooltipAutoCommitExtrusion);

            if(EditorGUI.EndChangeCheck())
            {
                CSGSettings.AutoCommitExtrusion = autoCommitExtrusion;
                CSGSettings.Save();
            }

            GUILayout.EndVertical();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            if(isSceneGUI)
            {
                GUILayout.Space(4);
                GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

                EditorGUI.BeginChangeCheck();

                defaultMaterial = CSGEditorGUIUtility.MaterialImage(defaultMaterial);

                if(EditorGUI.EndChangeCheck() && defaultMaterial)
                {
                    CSGSettings.DefaultMaterial = defaultMaterial;
                    CSGSettings.Save();
                }

                GUILayout.EndVertical();
            }

            bool haveNodes = tool != null && filteredSelection.nodeTargets.Length > 0;

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            EditorGUI.BeginDisabledGroup(!haveNodes);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUILayout.LabelField(contentFlip, isSceneGUI ? labelWidth : largeLabelWidth);

            if(GUILayout.Button(contentFlipX, left))
            {
                tool.FlipX();
            }

            TooltipUtility.SetToolTip(tooltipFlipX);

            if(GUILayout.Button(contentFlipY, middle))
            {
                tool.FlipY();
            }

            TooltipUtility.SetToolTip(tooltipFlipY);

            if(GUILayout.Button(contentFlipZ, right))
            {
                tool.FlipZ();
            }

            TooltipUtility.SetToolTip(tooltipFlipZ);

            GUILayout.EndHorizontal();

            if(GUILayout.Button(contentSnapToGrid))
            {
                tool.SnapToGrid(Camera.current);
            }

            TooltipUtility.SetToolTip(tooltipSnapToGrid);

            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            if(!isSceneGUI)
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                EditorGUILayout.LabelField(contentDefaultMaterial, largeLabelWidth);
                GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

                EditorGUI.BeginChangeCheck();

                defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;

                if(EditorGUI.EndChangeCheck() && defaultMaterial)
                {
                    CSGSettings.DefaultMaterial = defaultMaterial;
                    CSGSettings.Save();
                }

                GUILayout.Space(2);
                GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                GUILayout.Space(5);
                defaultMaterial = CSGEditorGUIUtility.MaterialImage(defaultMaterial, false);

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
            else
            {
                EditorGUI.BeginChangeCheck();

                defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;

                if(EditorGUI.EndChangeCheck() && defaultMaterial)
                {
                    CSGSettings.DefaultMaterial = defaultMaterial;
                    CSGSettings.Save();
                }
            }

            GUILayout.EndVertical();
            EditorGUI.showMixedValue = false;
        }
    }
}

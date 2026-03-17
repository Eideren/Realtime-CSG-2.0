using InternalRealtimeCSG;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
    internal sealed partial class EditmodePlaceGUI
    {
        private static int SceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

        private static Rect lastGuiRect;

        public static Rect GetLastSceneGUIRect(EditModePlace tool)
        {
            return lastGuiRect;
        }

        public static void OnSceneGUI(Rect windowRect, EditModePlace tool)
        {
            CSG_GUIStyleUtility.InitStyles();
            InitLocalStyles();
            GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

            {
                GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);

                {
                    GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);

                    {
                        GUILayout.FlexibleSpace();

                        CSG_GUIStyleUtility.ResetGUIState();

                        GUIStyle windowStyle = GUI.skin.window;
                        GUILayout.BeginVertical(ContentTitleLabel, windowStyle, CSG_GUIStyleUtility.ContentEmpty);

                        {
                            OnGUIContents(true, tool);
                        }

                        GUILayout.EndVertical();

                        Rect currentArea = GUILayoutUtility.GetLastRect();
                        lastGuiRect = currentArea;

                        Rect buttonArea = currentArea;
                        buttonArea.x      += buttonArea.width - 17;
                        buttonArea.y      += 2;
                        buttonArea.height =  13;
                        buttonArea.width  =  13;

                        if(GUI.Button(buttonArea, GUIContent.none, "WinBtnClose"))
                        {
                            EditModeToolWindowSceneGUI.GetWindow();
                        }

                        TooltipUtility.SetToolTip(CSG_GUIStyleUtility.PopOutTooltip, buttonArea);

                        int controlID = GUIUtility.GetControlID(
                            SceneViewMeshOverlayHash, FocusType.Keyboard, currentArea
                        );

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
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndHorizontal();
        }

        public static void OnInspectorGUI(EditorWindow window, float height)
        {
            lastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
            EditModePlace tool = EditModeManager.ActiveTool as EditModePlace;

            CSG_GUIStyleUtility.InitStyles();
            InitLocalStyles();
            OnGUIContents(false, tool);
        }

        private static void ShowCSGOperations(bool isSceneGUI, EditModePlace tool, FilteredSelection filteredSelection)
        {
            bool operations_enabled = tool != null &&
                filteredSelection.nodeTargets.Length > 0 &&
                filteredSelection.nodeTargets.Length
             == filteredSelection.brushTargets.Length + filteredSelection.operationTargets.Length;

            EditorGUI.BeginDisabledGroup(!operations_enabled);

            {
                bool mixedValues = tool == null || (filteredSelection.brushTargets.Length == 0
                 && filteredSelection.operationTargets.Length == 0);

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
                    bool passThroughValue = false;

                    if(tool != null &&
                       filteredSelection.brushTargets.Length == 0 &&
                       filteredSelection.operationTargets.Length > 0 &&
                       filteredSelection.operationTargets.Length
                    == filteredSelection.nodeTargets.Length) // only operations
                    {
                        bool? passThrough = filteredSelection.operationTargets[0].PassThrough;

                        for(int i = 1; i < filteredSelection.operationTargets.Length; i++)
                        {
                            if(passThrough.HasValue
                            && passThrough.Value != filteredSelection.operationTargets[i].PassThrough)
                            {
                                passThrough = null;

                                break;
                            }
                        }

                        mixedValues = !passThrough.HasValue || passThrough.Value;

                        bool ptMixedValues = !passThrough.HasValue;
                        passThroughValue = passThrough.HasValue ? passThrough.Value : false;

                        if(CSG_EditorGUIUtility.PassThroughButton(passThroughValue, ptMixedValues))
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
                        operationType = CSG_EditorGUIUtility.ChooseOperation(operationType, mixedValues);
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
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void OnGUIContents(bool isSceneGUI, EditModePlace tool)
        {
            EditModeCommonGUI.StartToolGUI();

            FilteredSelection filteredSelection   = EditModeManager.FilteredSelection;
            Vector3           defaultMoveOffset   = CSGSettings.DefaultMoveOffset;
            Vector3           defaultRotateOffset = CSGSettings.DefaultRotateOffset;

            Vector3 displayNewCenter = GridUtility.CleanPosition(
                Tools.pivotRotation == PivotRotation.Local ? tool.LocalSpacePivotCenter : tool.WorldSpacePivotCenter
            );

            GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);

            {
                GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

                {
                    ShowCSGOperations(isSceneGUI, tool, filteredSelection);
                    GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);

                    {
                        EditorGUI.BeginDisabledGroup(!tool.HaveSelection);

                        {
                            if(Tools.current == Tool.Move)
                            {
                                EditorGUI.BeginDisabledGroup(
                                    defaultMoveOffset.sqrMagnitude < MathConstants.EqualityEpsilonSqr
                                );

                                {
                                    if(GUILayout.Button(MoveByOffsetContent))
                                    {
                                        tool.MoveByOffset(CSGSettings.DefaultMoveOffset);
                                    }

                                    TooltipUtility.SetToolTip(MoveByOffsetTooltip);

                                    if(GUILayout.Button(CloneMoveByOffsetContent))
                                    {
                                        tool.CloneMoveByOffset(CSGSettings.DefaultMoveOffset);
                                    }

                                    TooltipUtility.SetToolTip(CloneMoveByOffsetTooltip);
                                }

                                EditorGUI.EndDisabledGroup();
                            }
                            else if(Tools.current == Tool.Rotate)
                            {
                                EditorGUI.BeginDisabledGroup(
                                    defaultMoveOffset.sqrMagnitude < MathConstants.EqualityEpsilonSqr
                                );

                                {
                                    if(GUILayout.Button(RotateByOffsetContent))
                                    {
                                        tool.RotateByOffset(Quaternion.Euler(CSGSettings.DefaultRotateOffset));
                                    }

                                    TooltipUtility.SetToolTip(RotateByOffsetTooltip);

                                    if(GUILayout.Button(CloneRotateByOffsetContent))
                                    {
                                        tool.CloneRotateByOffset(Quaternion.Euler(CSGSettings.DefaultRotateOffset));
                                    }

                                    TooltipUtility.SetToolTip(CloneRotateByOffsetTooltip);
                                }

                                EditorGUI.EndDisabledGroup();

                                if(GUILayout.Button(RecenterPivotContent))
                                {
                                    tool.RecenterPivot();
                                }

                                TooltipUtility.SetToolTip(RecenterPivotTooltip);
                            }
                        }

                        EditorGUI.EndDisabledGroup();
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(5);

                if(Tools.current == Tool.Move)
                {
                    GUILayoutOption[] doubleFieldOptions = isSceneGUI ? MaxWidth150 : CSG_GUIStyleUtility.ContentEmpty;
                    EditorGUI.BeginDisabledGroup(!tool.HaveSelection);

                    {
                        EditorGUI.BeginChangeCheck();

                        {
                            GUILayout.Label(MoveOffsetContent);

                            defaultMoveOffset = CSG_EditorGUIUtility.DistanceVector3Field(
                                defaultMoveOffset, false, doubleFieldOptions
                            );
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            CSGSettings.DefaultMoveOffset = defaultMoveOffset;
                            CSGSettings.Save();
                        }
                    }

                    EditorGUI.EndDisabledGroup();
                }
                else if(Tools.current == Tool.Rotate)
                {
                    GUILayoutOption[] doubleFieldOptions = isSceneGUI ? MaxWidth150 : CSG_GUIStyleUtility.ContentEmpty;
                    EditorGUI.BeginDisabledGroup(Tools.pivotMode == PivotMode.Center || !tool.HaveSelection);

                    {
                        EditorGUI.BeginChangeCheck();

                        {
                            GUILayout.Label(RotationCenterContent);
                            defaultRotateOffset = CSG_EditorGUIUtility.EulerDegreeField(defaultRotateOffset);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            CSGSettings.DefaultRotateOffset = defaultRotateOffset;
                            CSGSettings.Save();
                        }

                        EditorGUI.BeginChangeCheck();

                        {
                            GUILayout.Label(PivotCenterContent);

                            displayNewCenter = CSG_EditorGUIUtility.DistanceVector3Field(
                                displayNewCenter, false, doubleFieldOptions
                            );

                            TooltipUtility.SetToolTip(PivotVectorTooltip);
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            if(Tools.pivotRotation == PivotRotation.Local)
                            {
                                tool.LocalSpacePivotCenter = displayNewCenter;
                            }
                            else
                            {
                                tool.WorldSpacePivotCenter = displayNewCenter;
                            }
                        }
                    }

                    EditorGUI.EndDisabledGroup();
                }
            }

            GUILayout.EndVertical();
            EditorGUI.showMixedValue = false;
        }
    }
}

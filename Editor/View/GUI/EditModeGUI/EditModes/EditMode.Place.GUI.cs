using InternalRealtimeCSG;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
    internal sealed partial class EditmodePlaceGUI
    {
        private static readonly int sceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

        private static Rect m_LastGuiRect;

        public static Rect GetLastSceneGUIRect()
        {
            return m_LastGuiRect;
        }

        public static void OnSceneGUI(EditModePlace tool)
        {
            CsgguiStyleUtility.InitStyles();
            InitLocalStyles();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.FlexibleSpace();

            CsgguiStyleUtility.ResetGUIState();

            GUIStyle windowStyle = GUI.skin.window;
            GUILayout.BeginVertical(m_ContentTitleLabel, windowStyle, CsgguiStyleUtility.contentEmpty);

            OnGUIContents(true, tool);

            GUILayout.EndVertical();

            Rect currentArea = GUILayoutUtility.GetLastRect();
            m_LastGuiRect = currentArea;

            Rect buttonArea = currentArea;
            buttonArea.x      += buttonArea.width - 17;
            buttonArea.y      += 2;
            buttonArea.height =  13;
            buttonArea.width  =  13;

            /*
            if(GUI.Button(buttonArea, GUIContent.none, "ToolbarSearchCancelButton"))
            {
                EditModeToolWindowSceneGUI.GetWindow();
            }
            */

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
            EditModePlace tool = EditModeManager.ActiveTool as EditModePlace;

            CsgguiStyleUtility.InitStyles();
            InitLocalStyles();
            OnGUIContents(false, tool);
        }

        private static void ShowCSGOperations(bool isSceneGUI, EditModePlace tool, FilteredSelection filteredSelection)
        {
            bool operationsEnabled = tool != null && filteredSelection.nodeTargets.Length > 0 && filteredSelection.nodeTargets.Length == filteredSelection.brushTargets.Length + filteredSelection.operationTargets.Length;

            EditorGUI.BeginDisabledGroup(!operationsEnabled);

            bool mixedValues = tool == null || (filteredSelection.brushTargets.Length == 0 && filteredSelection.operationTargets.Length == 0);

            CSGOperationType operationType = CSGOperationType.Additive;

            if(tool != null)
            {
                if(filteredSelection.brushTargets.Length > 0)
                {
                    operationType = filteredSelection.brushTargets[0].operationType;

                    for(int i = 1; i < filteredSelection.brushTargets.Length; i++)
                    {
                        if(filteredSelection.brushTargets[i].operationType != operationType)
                        {
                            mixedValues = true;
                        }
                    }
                }
                else if(filteredSelection.operationTargets.Length > 0)
                {
                    operationType = filteredSelection.operationTargets[0].operationType;
                }

                if(filteredSelection.operationTargets.Length > 0)
                {
                    for(int i = 0; i < filteredSelection.operationTargets.Length; i++)
                    {
                        if(filteredSelection.operationTargets[i].operationType != operationType)
                        {
                            mixedValues = true;
                        }
                    }
                }
            }

            GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);

            // only operations
            if(tool != null && filteredSelection.brushTargets.Length == 0 && filteredSelection.operationTargets.Length > 0 && filteredSelection.operationTargets.Length == filteredSelection.nodeTargets.Length)
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

            {
                operationType = CSGEditorGUIUtility.ChooseOperation(operationType, mixedValues);
            }

            if(EditorGUI.EndChangeCheck() && tool != null)
            {
                Undo.RecordObjects(filteredSelection.nodeTargets, "Changed CSG operation of nodes");

                for(int i = 0; i < filteredSelection.brushTargets.Length; i++)
                {
                    filteredSelection.brushTargets[i].operationType = operationType;
                }

                for(int i = 0; i < filteredSelection.operationTargets.Length; i++)
                {
                    filteredSelection.operationTargets[i].PassThrough   = false;
                    filteredSelection.operationTargets[i].operationType = operationType;
                }

                InternalCSGModelManager.CheckForChanges();
                EditorApplication.RepaintHierarchyWindow();
#if UNITY_6000_5_OR_NEWER
                HierarchyWindowItemGUI.RepaintHierarchy();
#endif
            }

            GUILayout.EndVertical();

            EditorGUI.EndDisabledGroup();
        }

        private static void OnGUIContents(bool isSceneGUI, EditModePlace tool)
        {
            EditModeCommonGUI.StartToolGUI();

            FilteredSelection filteredSelection = EditModeManager.FilteredSelection;

            Vector3 defaultMoveOffset   = CSGSettings.DefaultMoveOffset;
            Vector3 defaultRotateOffset = CSGSettings.DefaultRotateOffset;

            Vector3 displayNewCenter = GridUtility.CleanPosition(Tools.pivotRotation == PivotRotation.Local ? tool.LocalSpacePivotCenter : tool.WorldSpacePivotCenter);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            ShowCSGOperations(isSceneGUI, tool, filteredSelection);
            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            EditorGUI.BeginDisabledGroup(!tool.HaveSelection);

            if(Tools.current == Tool.Move)
            {
                EditorGUI.BeginDisabledGroup(defaultMoveOffset.sqrMagnitude < MathConstants.EQUALITY_EPSILON_SQR);

                if(GUILayout.Button(moveByOffsetContent))
                {
                    tool.MoveByOffset(CSGSettings.DefaultMoveOffset);
                }

                TooltipUtility.SetToolTip(moveByOffsetTooltip);

                if(GUILayout.Button(cloneMoveByOffsetContent))
                {
                    tool.CloneMoveByOffset(CSGSettings.DefaultMoveOffset);
                }

                TooltipUtility.SetToolTip(cloneMoveByOffsetTooltip);

                EditorGUI.EndDisabledGroup();
            }
            else if(Tools.current == Tool.Rotate)
            {
                EditorGUI.BeginDisabledGroup(defaultMoveOffset.sqrMagnitude < MathConstants.EQUALITY_EPSILON_SQR);

                {
                    if(GUILayout.Button(rotateByOffsetContent))
                    {
                        tool.RotateByOffset(Quaternion.Euler(CSGSettings.DefaultRotateOffset));
                    }

                    TooltipUtility.SetToolTip(rotateByOffsetTooltip);

                    if(GUILayout.Button(cloneRotateByOffsetContent))
                    {
                        tool.CloneRotateByOffset(Quaternion.Euler(CSGSettings.DefaultRotateOffset));
                    }

                    TooltipUtility.SetToolTip(cloneRotateByOffsetTooltip);
                }

                EditorGUI.EndDisabledGroup();

                if(GUILayout.Button(recenterPivotContent))
                {
                    tool.RecenterPivot();
                }

                TooltipUtility.SetToolTip(recenterPivotTooltip);
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if(Tools.current == Tool.Move)
            {
                GUILayoutOption[] doubleFieldOptions = isSceneGUI ? maxWidth150 : CsgguiStyleUtility.contentEmpty;
                EditorGUI.BeginDisabledGroup(!tool.HaveSelection);

                EditorGUI.BeginChangeCheck();

                GUILayout.Label(moveOffsetContent);

                defaultMoveOffset = CSGEditorGUIUtility.DistanceVector3Field(defaultMoveOffset, false, doubleFieldOptions);

                if(EditorGUI.EndChangeCheck())
                {
                    CSGSettings.DefaultMoveOffset = defaultMoveOffset;
                    CSGSettings.Save();
                }

                EditorGUI.EndDisabledGroup();
            }
            else if(Tools.current == Tool.Rotate)
            {
                GUILayoutOption[] doubleFieldOptions = isSceneGUI ? maxWidth150 : CsgguiStyleUtility.contentEmpty;
                EditorGUI.BeginDisabledGroup(Tools.pivotMode == PivotMode.Center || !tool.HaveSelection);

                EditorGUI.BeginChangeCheck();

                GUILayout.Label(rotationCenterContent);
                defaultRotateOffset = CSGEditorGUIUtility.EulerDegreeField(defaultRotateOffset);

                if(EditorGUI.EndChangeCheck())
                {
                    CSGSettings.DefaultRotateOffset = defaultRotateOffset;
                    CSGSettings.Save();
                }

                EditorGUI.BeginChangeCheck();

                GUILayout.Label(pivotCenterContent);

                displayNewCenter = CSGEditorGUIUtility.DistanceVector3Field(displayNewCenter, false, doubleFieldOptions);

                TooltipUtility.SetToolTip(pivotVectorTooltip);

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

                EditorGUI.EndDisabledGroup();
            }

            GUILayout.EndVertical();
            EditorGUI.showMixedValue = false;
        }
    }
}

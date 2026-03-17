using InternalRealtimeCSG;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditModeClipGUI
    {
        private static readonly int sceneViewMeshOverlayHash = "SceneViewClipOverlay".GetHashCode();

        private static void InitLocalStyles()
        {
            if(m_ContentClipLabel != null)
            {
                return;
            }

            m_ContentClipLabel = new GUIContent(CsgguiStyleUtility.BrushEditModeNames[(int)ToolEditMode.Clip]);
        }

        private static bool m_DoCommit; // unity bug workaround
        private static bool m_DoCancel; // unity bug workaround

        private static Rect m_LastGuiRect;

        public static Rect GetLastSceneGUIRect()
        {
            return m_LastGuiRect;
        }

        public static void OnSceneGUI(EditModeClip tool)
        {
            m_DoCommit = false; // unity bug workaround
            m_DoCancel = false; // unity bug workaround

            CsgguiStyleUtility.InitStyles();
            InitLocalStyles();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            {
                GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

                {
                    GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

                    {
                        GUILayout.FlexibleSpace();

                        CsgguiStyleUtility.ResetGUIState();

                        GUIStyle windowStyle = GUI.skin.window;
                        GUILayout.BeginVertical(m_ContentClipLabel, windowStyle, CsgguiStyleUtility.contentEmpty);

                        {
                            OnGUIContents(true, tool);
                        }

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

                        int controlID = GUIUtility.GetControlID(
                            sceneViewMeshOverlayHash, FocusType.Passive, currentArea
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

            if(tool != null)
            {
                if(m_DoCommit)
                {
                    tool.Commit(); // unity bug workaround
                }

                if(m_DoCancel)
                {
                    tool.Cancel(); // unity bug workaround
                }
            }
        }

        private static void OnGUIContents(bool isSceneGUI, EditModeClip tool)
        {
            EditModeCommonGUI.StartToolGUI();

            if(tool.ClipBrushCount == 0)
            {
                GUILayout.Label("no brushes selected", CsgguiStyleUtility.RedTextArea);
            }
            else
            {
                GUILayout.Label(
                    tool.ClipBrushCount == 1
                        ? $"{tool.ClipBrushCount} brush selected"
                        : $"{tool.ClipBrushCount} brushes selected"
                );
            }

            EditorGUILayout.Space();
            EditorGUI.BeginDisabledGroup(tool == null);

            {
                GUILayout.BeginVertical(isSceneGUI ? GUI.skin.box : GUIStyle.none);

                {
                    ClipMode newClipMode = tool != null ? tool.clipMode : (ClipMode)999;
                    CSGSkin skin        = CsgguiStyleUtility.Skin;

                    for(int i = 0; i < clipModeValues.Length; i++)
                    {
                        bool       selected = newClipMode == clipModeValues[i];
                        GUIContent content;
                        GUIStyle   style;

                        if(selected)
                        {
                            style   = CsgguiStyleUtility.SelectedIconLabelStyle;
                            content = skin.clipNamesOn[i];
                        }
                        else
                        {
                            style   = CsgguiStyleUtility.UnselectedIconLabelStyle;
                            content = skin.clipNames[i];
                        }

                        if(GUILayout.Toggle(selected, content, style))
                        {
                            newClipMode = clipModeValues[i];
                        }

                        TooltipUtility.SetToolTip(CsgguiStyleUtility.clipTooltips[i]);
                    }

                    if(tool != null && tool.clipMode != newClipMode)
                    {
                        tool.SetClipMode(newClipMode);
                    }
                }

                GUILayout.EndVertical();

                if(!isSceneGUI)
                {
                    GUILayout.Space(10);
                }

                bool disabled = tool == null || tool.editMode != EditModeClip.EditMode.EditPoints;

                Material defaultMaterial = CSGSettings.DefaultMaterial;
                GUILayout.BeginVertical(isSceneGUI ? materialSceneWidth : CsgguiStyleUtility.contentEmpty);

                {
                    GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                    {
                        if(isSceneGUI)
                        {
                            EditorGUI.BeginChangeCheck();

                            {
                                defaultMaterial = CSGEditorGUIUtility.MaterialImage(defaultMaterial);
                            }

                            if(EditorGUI.EndChangeCheck() && defaultMaterial)
                            {
                                CSGSettings.DefaultMaterial = defaultMaterial;
                                CSGSettings.Save();
                            }

                            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);
                        }

                        {
                            EditorGUI.BeginDisabledGroup(disabled);

                            {
                                if(GUILayout.Button(contentCancel))
                                {
                                    m_DoCancel = true;
                                }

                                TooltipUtility.SetToolTip(cancelTooltip);

                                if(GUILayout.Button(contentCommit))
                                {
                                    m_DoCommit = true;
                                }

                                TooltipUtility.SetToolTip(commitTooltip);
                            }

                            EditorGUI.EndDisabledGroup();
                        }

                        if(isSceneGUI)
                        {
                            GUILayout.EndVertical();
                        }
                    }

                    GUILayout.EndHorizontal();

                    if(isSceneGUI)
                    {
                        GUILayout.Space(2);
                        EditorGUI.BeginChangeCheck();

                        {
                            defaultMaterial =
                                EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;
                        }

                        if(EditorGUI.EndChangeCheck() && defaultMaterial)
                        {
                            CSGSettings.DefaultMaterial = defaultMaterial;
                            CSGSettings.Save();
                        }
                    }
                }

                if(!isSceneGUI)
                {
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                    {
                        EditorGUILayout.LabelField(contentDefaultMaterial, largeLabelWidth);
                        GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

                        {
                            EditorGUI.BeginChangeCheck();

                            {
                                defaultMaterial =
                                    EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;
                            }

                            if(EditorGUI.EndChangeCheck() && defaultMaterial)
                            {
                                CSGSettings.DefaultMaterial = defaultMaterial;
                                CSGSettings.Save();
                            }
                        }

                        GUILayout.Space(2);
                        GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                        {
                            GUILayout.Space(5);
                            defaultMaterial = CSGEditorGUIUtility.MaterialImage(defaultMaterial, false);
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }

            EditorGUI.EndDisabledGroup();
        }

        public static void OnInspectorGUI()
        {
            m_LastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
            EditModeClip tool = EditModeManager.ActiveTool as EditModeClip;

            m_DoCommit = false; // unity bug workaround
            m_DoCancel = false; // unity bug workaround

            CsgguiStyleUtility.InitStyles();
            InitLocalStyles();
            OnGUIContents(false, tool);

            if(tool != null)
            {
                if(m_DoCommit)
                {
                    tool.Commit(); // unity bug workaround
                }

                if(m_DoCancel)
                {
                    tool.Cancel(); // unity bug workaround
                }
            }
        }
    }
}

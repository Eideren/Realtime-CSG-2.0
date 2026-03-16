using InternalRealtimeCSG;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
    internal sealed partial class EditModeGenerateGUI
    {
        private static int SceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

        private static Rect lastGuiRect;

        public static Rect GetLastSceneGUIRect(EditModeGenerate tool)
        {
            return lastGuiRect;
        }

        public static bool OnSceneGUI(Rect windowRect, EditModeGenerate tool)
        {
            tool.CurrentGenerator.StartGUI();
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

                        GUILayout.BeginVertical(ContentTitleLabel, windowStyle, GUILayout.Width(275));

                        {
                            OnGUIContents(tool, true, 0);
                        }

                        GUILayout.EndVertical();
                        Rect currentArea = GUILayoutUtility.GetLastRect();
                        lastGuiRect = currentArea;

                        Rect buttonArea = currentArea;
                        buttonArea.x      += buttonArea.width - 17;
                        buttonArea.y      += 2;
                        buttonArea.height =  13;
                        buttonArea.width  =  13;

                        if(GUI.Button(buttonArea, GUIContent.none, "SearchCancelButton"))
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
            tool.CurrentGenerator.FinishGUI();

            return true;
        }

        private static void ChooseOperation(EditModeGenerate tool, bool isSceneGUI)
        {
            IGenerator generator = tool.CurrentGenerator;
            bool       enabled   = generator.HaveBrushes;
            EditorGUI.BeginDisabledGroup(!enabled);

            {
                if(isSceneGUI)
                {
                    GUILayout.BeginVertical(GUI.skin.box, Width100);
                }
                else
                {
                    GUILayout.BeginVertical(GUIStyle.none);
                }

                {
                    bool             mixedValues = !enabled;
                    CSGOperationType operation   = generator.CurrentCSGOperationType;
                    EditorGUI.BeginChangeCheck();
                    operation = CSG_EditorGUIUtility.ChooseOperation(operation, mixedValues);

                    if(EditorGUI.EndChangeCheck())
                    {
                        generator.CurrentCSGOperationType = operation;
                    }
                }

                GUILayout.EndVertical();
            }

            EditorGUI.EndDisabledGroup();
        }

        private static void CommitOrCancel(bool isSceneGUI, EditModeGenerate tool)
        {
            Material defaultMaterial = CSGSettings.DefaultMaterial;
            GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

            {
                if(isSceneGUI)
                {
                    GUILayout.Space(5);
                    GUILayout.BeginVertical(GUILayout.MinWidth(10));

                    {
                        EditorGUI.BeginChangeCheck();

                        {
                            defaultMaterial = CSG_EditorGUIUtility.MaterialImage(defaultMaterial);
                        }

                        if(EditorGUI.EndChangeCheck() && defaultMaterial)
                        {
                            CSGSettings.DefaultMaterial = defaultMaterial;
                            tool.CurrentGenerator.OnDefaultMaterialModified();
                            CSGSettings.Save();
                        }
                    }

                    GUILayout.EndVertical();
                    GUILayout.Space(4);
                    GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);
                }

                {
                    IGenerator generator = tool.CurrentGenerator;
                    EditorGUI.BeginDisabledGroup(!generator.CanCommit);

                    {
                        if(GUILayout.Button(CancelContent))
                        {
                            generator.DoCancel();
                        }

                        TooltipUtility.SetToolTip(CancelTooltip);

                        if(GUILayout.Button(CreateContent))
                        {
                            generator.DoCommit();
                        }

                        TooltipUtility.SetToolTip(CreateTooltip);
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
                    defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;
                }

                if(EditorGUI.EndChangeCheck() && defaultMaterial)
                {
                    CSGSettings.DefaultMaterial = defaultMaterial;
                    tool.CurrentGenerator.OnDefaultMaterialModified();
                    CSGSettings.Save();
                }
            }
        }

        private static Vector2 scrollPos;
        private static Rect[] shapeModeRects;

        private static void OnGUIContents(EditModeGenerate tool, bool isSceneGUI, float height)
        {
            if(!isSceneGUI)
            {
                Rect     shapeModeBounds;
                CSG_Skin csg_skin = CSG_GUIStyleUtility.Skin;

                tool.BuilderMode = (ShapeMode)CSG_EditorGUIUtility.ToolbarWrapped(
                    (int)tool.BuilderMode, ref shapeModeRects, out shapeModeBounds, csg_skin.shapeModeNames
                  , CSG_GUIStyleUtility.shapeModeTooltips, height, EditorGUIUtility.currentViewWidth - 4
                );

                GUILayout.Space(shapeModeBounds.height);

                EditModeCommonGUI.StartToolGUI();

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                {
                    ChooseOperation(tool, isSceneGUI);
                    tool.CurrentGenerator.OnShowGUI(isSceneGUI);
                    CommitOrCancel(isSceneGUI, tool);
                    EditorGUILayout.Space();
                    Material defaultMaterial = CSGSettings.DefaultMaterial;
                    GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

                    {
                        EditorGUILayout.LabelField(ContentDefaultMaterial, largeLabelWidth);
                        GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);

                        {
                            EditorGUI.BeginChangeCheck();

                            {
                                defaultMaterial =
                                    EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;
                            }

                            if(EditorGUI.EndChangeCheck() && defaultMaterial)
                            {
                                CSGSettings.DefaultMaterial = defaultMaterial;
                                tool.CurrentGenerator.OnDefaultMaterialModified();
                                CSGSettings.Save();
                            }
                        }

                        GUILayout.Space(2);
                        GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

                        {
                            GUILayout.Space(5);
                            EditorGUI.BeginChangeCheck();

                            {
                                defaultMaterial = CSG_EditorGUIUtility.MaterialImage(defaultMaterial, false);
                            }

                            if(EditorGUI.EndChangeCheck() && defaultMaterial)
                            {
                                CSGSettings.DefaultMaterial = defaultMaterial;
                                tool.CurrentGenerator.OnDefaultMaterialModified();
                                CSGSettings.Save();
                            }
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }

                    GUILayout.EndHorizontal();
                    /*
                    // Unity won't let us do this
                    GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
                    OnGUIContentsMaterialInspector(first_material, multiple_materials);
                    GUILayout.EndVertical();
                    */
                }

                EditorGUILayout.EndScrollView();
            }

            if(isSceneGUI)
            {
                GUILayout.BeginHorizontal(GUILayout.MinHeight(100));

                {
                    GUILayout.BeginVertical(GUILayout.Width(100));

                    {
                        GUILayout.FlexibleSpace();

                        Rect rect = GUILayoutUtility.GetLastRect();

                        CSG_Skin csg_skin = CSG_GUIStyleUtility.Skin;
                        Rect     shapeModeBounds;

                        tool.BuilderMode = (ShapeMode)CSG_EditorGUIUtility.ToolbarWrapped(
                            (int)tool.BuilderMode, ref shapeModeRects, out shapeModeBounds, csg_skin.shapeModeNames
                          , CSG_GUIStyleUtility.shapeModeTooltips, rect.y, 100
                        );

                        GUILayout.Space(shapeModeBounds.height);

                        ChooseOperation(tool, isSceneGUI);
                    }

                    GUILayout.EndVertical();
                    GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);

                    {
                        tool.CurrentGenerator.OnShowGUI(isSceneGUI);
                        GUILayout.FlexibleSpace();
                        CommitOrCancel(isSceneGUI, tool);
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.EndHorizontal();
            }
        }

        public static void OnInspectorGUI(EditModeGenerate tool, EditorWindow window, float height)
        {
            lastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
            tool.CurrentGenerator.StartGUI();
            CSG_GUIStyleUtility.InitStyles();
            InitLocalStyles();
            OnGUIContents(tool, false, height);
            tool.CurrentGenerator.FinishGUI();
        }
    }
}

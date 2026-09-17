using InternalRealtimeCSG;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Foundation;

namespace RealtimeCSG
{
    internal sealed partial class EditModeGenerateGUI
    {
        private static readonly int sceneViewMeshOverlayHash = "SceneViewMeshOverlay".GetHashCode();

        private static Rect m_LastGuiRect;

        public static Rect GetLastSceneGUIRect()
        {
            return m_LastGuiRect;
        }

        public static bool OnSceneGUI(EditModeGenerate tool)
        {
            tool.CurrentGenerator.StartGUI();
            CsgguiStyleUtility.InitStyles();
            InitLocalStyles();
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.FlexibleSpace();

            CsgguiStyleUtility.ResetGUIState();
            GUIStyle windowStyle = GUI.skin.window;

            GUILayout.BeginVertical(m_ContentTitleLabel, windowStyle, GUILayout.Width(275));

            {
                OnGUIContents(tool, true, 0);
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
            tool.CurrentGenerator.FinishGUI();

            return true;
        }

        private static void ChooseOperation(EditModeGenerate tool, bool isSceneGUI)
        {
            IGenerator generator = tool.CurrentGenerator;
            bool       enabled   = generator.HaveBrushes;
            EditorGUI.BeginDisabledGroup(!enabled);

            if(isSceneGUI)
            {
                GUILayout.BeginVertical(GUI.skin.box, width100);
            }
            else
            {
                GUILayout.BeginVertical(GUIStyle.none);
            }

            bool             mixedValues = !enabled;
            CSGOperationType operation   = generator.CurrentCSGOperationType;
            EditorGUI.BeginChangeCheck();
            operation = CSGEditorGUIUtility.ChooseOperation(operation, mixedValues);

            if(EditorGUI.EndChangeCheck())
            {
                generator.CurrentCSGOperationType = operation;
            }

            GUILayout.EndVertical();

            EditorGUI.EndDisabledGroup();
        }

        private static void CommitOrCancel(bool isSceneGUI, EditModeGenerate tool)
        {
            Material defaultMaterial = CSGSettings.SelectedMaterial;
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            if(isSceneGUI)
            {
                GUILayout.Space(5);
                GUILayout.BeginVertical(GUILayout.MinWidth(10));

                EditorGUI.BeginChangeCheck();

                defaultMaterial = CSGEditorGUIUtility.MaterialImage(defaultMaterial);

                if(EditorGUI.EndChangeCheck() && defaultMaterial)
                {
                    CSGSettings.DefaultMaterial = defaultMaterial;
                    tool.CurrentGenerator.OnDefaultMaterialModified();
                    CSGSettings.Save();
                }

                GUILayout.EndVertical();
                GUILayout.Space(4);
                GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);
            }

            IGenerator generator = tool.CurrentGenerator;
            EditorGUI.BeginDisabledGroup(!generator.CanCommit);

            if(GUILayout.Button(cancelContent))
            {
                generator.DoCancel();
            }

            TooltipUtility.SetToolTip(cancelTooltip);

            if(GUILayout.Button(createContent))
            {
                generator.DoCommit();
            }

            TooltipUtility.SetToolTip(createTooltip);

            EditorGUI.EndDisabledGroup();

            if(isSceneGUI)
            {
                GUILayout.EndVertical();
            }

            GUILayout.EndHorizontal();

            if (isSceneGUI)
            {
                GUILayout.Space(2);
                EditorGUI.BeginChangeCheck();

                defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;

                if(EditorGUI.EndChangeCheck() && defaultMaterial)
                {
                    CSGSettings.DefaultMaterial = defaultMaterial;
                    tool.CurrentGenerator.OnDefaultMaterialModified();
                    CSGSettings.Save();
                }
            }

            EditorPrefs.SetBool("GenerateUsingSurfaceMaterial", GUILayout.Toggle(EditorPrefs.GetBool("GenerateUsingSurfaceMaterial", false), "Match Surface", EditorStyles.miniButton));
        }

        private static Vector2 m_ScrollPos;
        private static Rect[] m_ShapeModeRects;

        private static void OnGUIContents(EditModeGenerate tool, bool isSceneGUI, float height)
        {
            if(!isSceneGUI)
            {
                CSGSkin csgSkin = CsgguiStyleUtility.Skin;

                tool.BuilderMode = (ShapeMode)CSGEditorGUIUtility.ToolbarWrapped((int)tool.BuilderMode, ref m_ShapeModeRects, out Rect shapeModeBounds, csgSkin.shapeModeNames, CsgguiStyleUtility.shapeModeTooltips, height, EditorGUIUtility.currentViewWidth - 4);

                GUILayout.Space(shapeModeBounds.height);

                EditModeCommonGUI.StartToolGUI();

                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

                ChooseOperation(tool, isSceneGUI);
                tool.CurrentGenerator.OnShowGUI(isSceneGUI);
                CommitOrCancel(isSceneGUI, tool);
                EditorGUILayout.Space();
                Material defaultMaterial = CSGSettings.DefaultMaterial;
                GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                EditorGUILayout.LabelField(contentDefaultMaterial, largeLabelWidth);
                GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

                EditorGUI.BeginChangeCheck();

                defaultMaterial = EditorGUILayout.ObjectField(defaultMaterial, typeof(Material), true) as Material;

                if(EditorGUI.EndChangeCheck() && defaultMaterial)
                {
                    CSGSettings.DefaultMaterial = defaultMaterial;
                    tool.CurrentGenerator.OnDefaultMaterialModified();
                    CSGSettings.Save();
                }

                GUILayout.Space(2);
                GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                GUILayout.Space(5);
                EditorGUI.BeginChangeCheck();

                defaultMaterial = CSGEditorGUIUtility.MaterialImage(defaultMaterial, false);

                if(EditorGUI.EndChangeCheck() && defaultMaterial)
                {
                    CSGSettings.DefaultMaterial = defaultMaterial;
                    tool.CurrentGenerator.OnDefaultMaterialModified();
                    CSGSettings.Save();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.BeginHorizontal(GUILayout.MinHeight(100));

                GUILayout.BeginVertical(GUILayout.Width(100));

                GUILayout.FlexibleSpace();

                Rect rect = GUILayoutUtility.GetLastRect();

                CSGSkin csgSkin = CsgguiStyleUtility.Skin;

                tool.BuilderMode = (ShapeMode)CSGEditorGUIUtility.ToolbarWrapped((int)tool.BuilderMode, ref m_ShapeModeRects, out Rect shapeModeBounds, csgSkin.shapeModeNames, CsgguiStyleUtility.shapeModeTooltips, rect.y, 100);

                GUILayout.Space(shapeModeBounds.height);

                ChooseOperation(tool, isSceneGUI);

                GUILayout.EndVertical();
                GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

                tool.CurrentGenerator.OnShowGUI(isSceneGUI);
                GUILayout.FlexibleSpace();
                CommitOrCancel(isSceneGUI, tool);

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }
        }

        public static void OnInspectorGUI(EditModeGenerate tool, EditorWindow window, float height)
        {
            m_LastGuiRect = Rect.MinMaxRect(-1, -1, -1, -1);
            tool.CurrentGenerator.StartGUI();
            CsgguiStyleUtility.InitStyles();
            InitLocalStyles();
            OnGUIContents(tool, false, height);
            tool.CurrentGenerator.FinishGUI();
        }
    }
}

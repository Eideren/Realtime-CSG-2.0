using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
    internal class SurfaceVisibilityPopup : PopupWindowContent
    {
        private class Flags
        {
            public readonly GUIContent mixedValueContent = new GUIContent("\u2014|Mixed Values");
            public readonly GUIContent nothingContent = new GUIContent("Nothing");
            public readonly GUIContent onlyVisibleContent = new GUIContent("Show Only Visible");
            public readonly GUIContent allHelperContent = new GUIContent("Show All Helper Surfaces");
            public readonly GUIContent mixedContent = new GUIContent("Mixed ...");
            public readonly GUIContent[] helperSurfaceFlagContent;
            public readonly int[] helperSurfaceFlagValues;
            public readonly int allSurfaces;

            public Flags()
            {
                helperSurfaceFlagContent = Enum.GetNames(typeof(HelperSurfaceFlags))
                   .Select(x => new GUIContent(ObjectNames.NicifyVariableName(x)))
                   .ToArray();

                Array crappyArray = Enum.GetValues(typeof(HelperSurfaceFlags));
                helperSurfaceFlagValues = new int[crappyArray.Length];

                for(int i = 0; i < crappyArray.Length; i++)
                {
                    helperSurfaceFlagValues[i] = (int)crappyArray.GetValue(i);
                }

                allSurfaces = 0;

                for(int i = 0; i < helperSurfaceFlagValues.Length; i++)
                {
                    allSurfaces |= helperSurfaceFlagValues[i];
                }
            }
        }

        private static Flags m_Flags;

        private readonly Camera _camera;

        private const float FRAME_WIDTH = 1f;
        private const float LINE_HEIGHT = 20f;

        private SurfaceVisibilityPopup(Camera camera)
        {
            _camera = camera;
        }

        public override Vector2 GetWindowSize()
        {
            if(m_Flags == null)
            {
                m_Flags = new Flags();
            }

            float   windowHeight = 2f * FRAME_WIDTH + LINE_HEIGHT * (m_Flags.helperSurfaceFlagContent.Length + 2) + 4;
            Vector2 windowSize   = new Vector2(210, windowHeight);

            return windowSize;
        }

        public override void OnGUI(Rect rect)
        {
            if(!_camera)
            {
                return;
            }

            if(m_Flags == null)
            {
                m_Flags = new Flags();
            }

            // We do not use the layout event
            if(Event.current.type == EventType.Layout)
            {
                return;
            }

            // Content
            Draw(rect);

            // Use mouse move so we get hover state correctly in the menu item rows
            if(Event.current.type == EventType.MouseMove)
            {
                Event.current.Use();
            }

            // Escape closes the window
            if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }
        }

        private void Draw(Rect rect)
        {
            const int allVisibleSurfaces = (int)HelperSurfaceFlags.ShowVisibleSurfaces;

            int allHelperSurfaces = m_Flags.allSurfaces & ~allVisibleSurfaces;

            Rect drawPos = new Rect(FRAME_WIDTH, FRAME_WIDTH, rect.width - 2 * FRAME_WIDTH, LINE_HEIGHT);

            int helperSurfaces = (int)CSGSettings.VisibleHelperSurfaces;
            EditorGUI.BeginChangeCheck();

            GUI.Toggle(
                drawPos, helperSurfaces == allVisibleSurfaces, m_Flags.onlyVisibleContent
              , CsgguiStyleUtility.Skin.menuItem
            );

            if(EditorGUI.EndChangeCheck())
            {
                CSGSettings.VisibleHelperSurfaces = (HelperSurfaceFlags)allVisibleSurfaces;
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
                CSGSettings.Save();
                CSGEditorGUIUtility.RepaintAll();
            }

            drawPos.y += LINE_HEIGHT;

            EditorGUI.BeginChangeCheck();

            GUI.Toggle(
                drawPos, helperSurfaces == allHelperSurfaces, m_Flags.allHelperContent
              , CsgguiStyleUtility.Skin.menuItem
            );

            if(EditorGUI.EndChangeCheck())
            {
                CSGSettings.VisibleHelperSurfaces = (HelperSurfaceFlags)allHelperSurfaces;
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
                CSGSettings.Save();
                CSGEditorGUIUtility.RepaintAll();
            }

            drawPos.y += LINE_HEIGHT;
            drawPos.y += 4;

            for(int i = 0; i < m_Flags.helperSurfaceFlagContent.Length; i++)
            {
                int flag = m_Flags.helperSurfaceFlagValues[i];
                DrawListElement(drawPos, m_Flags.helperSurfaceFlagContent[i], helperSurfaces, flag);
                drawPos.y += LINE_HEIGHT;
            }
        }

        private void DrawListElement(Rect rect, GUIContent toggleName, int helperSurfaces, int flag)
        {
            EditorGUI.BeginChangeCheck();
            bool result = GUI.Toggle(rect, (helperSurfaces & flag) != 0, toggleName, CsgguiStyleUtility.Skin.menuItem);

            if(EditorGUI.EndChangeCheck())
            {
                if(result)
                {
                    CSGSettings.VisibleHelperSurfaces = (HelperSurfaceFlags)(helperSurfaces | flag);
                }
                else
                {
                    CSGSettings.VisibleHelperSurfaces = (HelperSurfaceFlags)(helperSurfaces & ~flag);
                }

                MeshInstanceManager.UpdateHelperSurfaceVisibility();
                CSGSettings.Save();
                CSGEditorGUIUtility.RepaintAll();
            }
        }

        public static void Button(SceneView sceneView, Rect currentRect)
        {
            if(m_Flags == null)
            {
                m_Flags = new Flags();
            }

            int        helperSurfaces = (int)CSGSettings.VisibleHelperSurfaces;
            GUIStyle   style          = EditorStyles.toolbarDropDown;
            GUIContent buttonContent  = m_Flags.mixedValueContent;

            if(!EditorGUI.showMixedValue)
            {
                const int allVisibleSurfaces = (int)HelperSurfaceFlags.ShowVisibleSurfaces;

                int allHelperSurfaces = m_Flags.allSurfaces & ~allVisibleSurfaces;

                if(helperSurfaces == 0)
                {
                    buttonContent = m_Flags.nothingContent;
                    style         = CsgguiStyleUtility.Skin.redToolbarDropDown;
                }
                else if(helperSurfaces == allVisibleSurfaces)
                {
                    buttonContent = m_Flags.onlyVisibleContent;
                }
                else if(helperSurfaces == allHelperSurfaces)
                {
                    buttonContent = m_Flags.allHelperContent;
                }
                else
                {
                    int count = 0;

                    for(int i = 0; i < m_Flags.helperSurfaceFlagContent.Length; i++)
                    {
                        int flag = m_Flags.helperSurfaceFlagValues[i];

                        if((helperSurfaces & flag) != 0)
                        {
                            count++;

                            if(count > 1)
                            {
                                break;
                            }
                        }
                    }

                    buttonContent = m_Flags.mixedContent;
                }
            }

            if(GUI.Button(currentRect, buttonContent, style))
            {
                PopupWindow.Show(currentRect, new SurfaceVisibilityPopup(sceneView.camera));
            }
        }
    }
}

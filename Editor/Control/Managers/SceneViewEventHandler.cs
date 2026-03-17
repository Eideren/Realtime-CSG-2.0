using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;

namespace RealtimeCSG
{
    internal sealed class SceneViewEventHandler
    {
        private static bool m_MousePressed;
        private static int m_PrevFocusControl;

        internal static void OnScene(SceneView sceneView)
        {
            CSGSettings.RegisterSceneView(sceneView);

            if(!CSGSettings.EnableRealtimeCSG)
            {
                return;
            }

            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            UpdateLoop.UpdateOnSceneChange();

            if(!CSGSettings.EnableRealtimeCSG)
            {
                ColorSettings.IsInitialized = false;
            }
            else if(!ColorSettings.IsInitialized)
            {
                if(Event.current.type == EventType.Repaint)
                {
                    ColorSettings.Update();
                }
            }

            if(!UpdateLoop.IsActive())
            {
                UpdateLoop.ResetUpdateRoutine();
            }

            if(Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                m_MousePressed = true;
            }
            else if(Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseMove)
            {
                m_MousePressed = false;
            }

            SceneDragToolManager.OnHandleDragAndDrop(sceneView);
            RectangleSelectionManager.Update(sceneView);
            EditModeManager.InitSceneGUI();

            if(Event.current.type == EventType.Repaint)
            {
                MeshInstanceManager.UpdateHelperSurfaces();
            }

            if(Event.current.type == EventType.Repaint)
            {
                SceneToolRenderer.OnPaint(sceneView);
            }
            else
            {
                SceneViewBottomBarGUI.ShowGUI(sceneView);
                SceneViewInfoGUI.DrawInfoGUI(sceneView);
            }

            EditModeManager.OnSceneGUI(sceneView);

            TooltipUtility.InitToolTip(sceneView);

            if(Event.current.type == EventType.Repaint)
            {
                SceneViewBottomBarGUI.ShowGUI(sceneView, false);
                SceneViewInfoGUI.DrawInfoGUI(sceneView);
            }

            if(!m_MousePressed)
            {
                Handles.BeginGUI();
                TooltipUtility.DrawToolTip(false);
                Handles.EndGUI();
            }

            if(Event.current.type == EventType.Layout)
            {
                int currentFocusControl = CSGHandles.FocusControl;

                if(m_PrevFocusControl != currentFocusControl)
                {
                    m_PrevFocusControl = currentFocusControl;
                    HandleUtility.Repaint();
                }
            }
        }
    }
}

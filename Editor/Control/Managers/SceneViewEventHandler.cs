using UnityEngine;
using UnityEditor;
using InternalRealtimeCSG;
using RealtimeCSG.Helpers;

namespace RealtimeCSG
{
    internal sealed class SceneViewEventHandler
    {
        private static bool mousePressed;

        private static int prevFocusControl;

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
                ColorSettings.isInitialized = false;
            }
            else if(!ColorSettings.isInitialized)
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
                mousePressed = true;
            }
            else if(Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseMove)
            {
                mousePressed = false;
            }

            SceneDragToolManager.OnHandleDragAndDrop(sceneView);
            RectangleSelectionManager.Update(sceneView);
            EditModeManager.InitSceneGUI(sceneView);

            if(Event.current.type == EventType.Repaint)
            {
                MeshInstanceManager.UpdateHelperSurfaces();
            }

            if(Event.current.type == EventType.Repaint)
            {
                SceneToolRenderer.OnPaint(sceneView);
            }
            else
                //if (fallbackGUI)
            {
                SceneViewBottomBarGUI.ShowGUI(sceneView);
                SceneViewInfoGUI.DrawInfoGUI(sceneView);
            }

            EditModeManager.OnSceneGUI(sceneView);

            //if (fallbackGUI)
            {
                TooltipUtility.InitToolTip(sceneView);

                if(Event.current.type == EventType.Repaint)
                {
                    SceneViewBottomBarGUI.ShowGUI(sceneView, false);
                    SceneViewInfoGUI.DrawInfoGUI(sceneView);
                }

                if(!mousePressed)
                {
                    Handles.BeginGUI();
                    TooltipUtility.DrawToolTip(false);
                    Handles.EndGUI();
                }
            }

            if(Event.current.type == EventType.Layout)
            {
                int currentFocusControl = CSGHandles.FocusControl;

                if(prevFocusControl != currentFocusControl)
                {
                    prevFocusControl = currentFocusControl;
                    HandleUtility.Repaint();
                }
            }
        }
    }
}

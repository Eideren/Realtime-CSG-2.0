using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using InternalRealtimeCSG;

//This is a tempory fix to make text fields editable
namespace RealtimeCSG
{
    [InitializeOnLoad]
    public static class BottomBarUITKWrapper
    {
        static BottomBarUITKWrapper()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static IMGUIContainer container;

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (container == null)
            {
                container = new IMGUIContainer(() =>
                {
                    SceneViewBottomBarGUI.ShowGUI(sceneView);
                });

                container.name = "RCSG-BottomBar";
            }

            container.pickingMode = PickingMode.Position;

            container.style.position = Position.Absolute;
            container.style.bottom = 0;
            container.style.width = Length.Percent(100);
            container.style.height = CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 4;

            container.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 1.0f);

            var root = sceneView.rootVisualElement;

            IMGUIContainer foundContainer = root.Q<IMGUIContainer>("RCSG-BottomBar");

            if (CSGSettings.EnableRealtimeCSG == true && foundContainer == null && SceneDragToolManager.IsDraggingObjectInScene == false)
            {
                root.Add(container);
            }
            else if ((CSGSettings.EnableRealtimeCSG == false || SceneDragToolManager.IsDraggingObjectInScene == true) && foundContainer != null)
            {
                root.Remove(foundContainer);
            }
        }
    }

    [InitializeOnLoad]
    public static class ToolWindowUITKWrapper
    {
        static ToolWindowUITKWrapper()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static string ActiveToolName
        {
            get
            {
                if (EditModeManager.ActiveTool != null)
                {
                    return EditModeManager.ActiveTool.GetType().Name;
                }

                return "";
            }
        }

        static bool HasLightmapButton
        {
            get
            {
                if (MeshInstanceManager.DoesAnyModelUseLightmapUVs() && EditModeCommonGUI.NeedLightmapUVUpdate(InternalCSGModelManager.Models) && (CSGSettings.UpdateLightmapUVButtonMode == CSGSettings.UpdateLightmapUVButtonState.InCSGToolWidow || CSGSettings.UpdateLightmapUVButtonMode == CSGSettings.UpdateLightmapUVButtonState.Both))
                {
                    return true;
                }

                return false;
            }
        }

        static Vector2 ToolWindowSize
        {
            get
            {
                Vector2 returnSize = new Vector2();

                if(EditModeManager.ActiveTool == null)
                {
                    return new Vector2(0, 0);
                }

                switch (EditModeManager.ActiveTool.GetType().Name)
                {
                    case "EditModePlace":
                        returnSize = new Vector2(280, 136);

                        if(HasLightmapButton)
                        {
                            returnSize.y += 31;
                        }

                        break;

                    case "EditModeGenerate":
                        returnSize = new Vector2(294, 202);
                        break;

                    case "EditModeMeshEdit":
                        returnSize = new Vector2(222, 272);

                        if (HasLightmapButton)
                        {
                            returnSize.y += 31;
                        }

                        break;

                    case "EditModeClip":
                        returnSize = new Vector2(152, 195);

                        if (HasLightmapButton)
                        {
                            returnSize.x += 68;
                            returnSize.y += 31;
                        }

                        break;

                    case "EditModeSurface":
                        returnSize = new Vector2(232, EditModeSurfaceGUI.SURFACE_EDIT_WINDOW_HEIGHT);
                        if (HasLightmapButton)
                        {
                            returnSize.y += 20;
                        }
                        break;
                }

                return returnSize;
            }
        }

        static IMGUIContainer container;

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (container == null)
            {
                container = new IMGUIContainer(() =>
                {
                    Handles.BeginGUI();

                    //For some reason the Surface Mode Window needs extra padding
                    Vector2 SurfaceModePadding = Vector2.zero;
                    if (ActiveToolName == "EditModeSurface")
                    {
                        SurfaceModePadding = new Vector2(2, -10);
                    }

                    //-2 and +10 adjusts for the GUILayout.Spaces
                    //The GUILayout.Spaces are used to prevent clipping of the Active Tool Window
                    Rect viewRect = new Rect(-2 + SurfaceModePadding.x, ToolWindowSize.y - 1220 + 10 + SurfaceModePadding.y, sceneView.rootVisualElement.contentRect.width, sceneView.rootVisualElement.contentRect.height - (CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 4));

                    GUILayout.BeginArea(viewRect);

                    GUILayout.BeginHorizontal();

                    GUILayout.Space(2);

                    GUILayout.BeginVertical();

                    if (EditModeManager.ActiveTool != null)
                    {
                        EditModeManager.ActiveTool.OnSceneGUI(viewRect);
                    }

                    GUILayout.Space(10);

                    GUILayout.EndVertical();

                    GUILayout.EndHorizontal();

                    GUILayout.EndArea();

                    Handles.EndGUI();
                });

                container.name = "RCSG-ToolWindow";
            }

            container.pickingMode = PickingMode.Position;

            container.style.position = Position.Absolute;

            container.style.left = 5;
            container.style.bottom = CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 5;

            container.style.width = ToolWindowSize.x;
            container.style.height = ToolWindowSize.y;

            var root = sceneView.rootVisualElement;

            IMGUIContainer foundContainer = root.Q<IMGUIContainer>("RCSG-ToolWindow");

            bool ForceHide = false;

            if(ActiveToolName == "EditModePlace" && (EditModeManager.FilteredSelection.nodeTargets == null || EditModeManager.FilteredSelection.nodeTargets.Length == 0))
            {
                ForceHide = true;
            }

            if (CSGSettings.EnableRealtimeCSG == true && foundContainer == null && EditModeManager.ActiveTool != null && ForceHide == false)
            {
                root.Add(container);
            }
            else if ((CSGSettings.EnableRealtimeCSG == false || EditModeManager.ActiveTool == null || SceneDragToolManager.IsDraggingObjectInScene == true|| ForceHide == true) && foundContainer != null)
            {
                root.Remove(foundContainer);
            }
        }
    }
}

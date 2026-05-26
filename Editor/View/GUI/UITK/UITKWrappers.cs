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

        static IMGUIContainer bottomBarContainer;

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (bottomBarContainer == null)
            {
                bottomBarContainer = new IMGUIContainer(() =>
                {
                    SceneViewBottomBarGUI.ShowGUI(sceneView);
                });

                bottomBarContainer.name = "RCSG-BottomBar";
            }

            bottomBarContainer.pickingMode = PickingMode.Position;

            bottomBarContainer.style.position = Position.Absolute;

            bottomBarContainer.style.bottom = 0;
            bottomBarContainer.style.width = Length.Percent(100);
            bottomBarContainer.style.height = CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 4;

            bottomBarContainer.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f, 1.0f);

            var root = sceneView.rootVisualElement;

            IMGUIContainer foundContainer = root.Q<IMGUIContainer>("RCSG-BottomBar");

            if (CSGSettings.EnableRealtimeCSG == true && foundContainer == null && SceneDragToolManager.IsDraggingObjectInScene == false)
            {
                root.Add(bottomBarContainer);
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
                        
                        switch(((EditModeGenerate)EditModeManager.ActiveTool).BuilderMode)
                        {
                            case ShapeMode.FreeDraw:
                                returnSize = new Vector2(275, 184);
                                break;

                            case ShapeMode.Box:
                                returnSize = new Vector2(275, 180);
                                break;

                            case ShapeMode.Sphere:
                                returnSize = new Vector2(294, 202);
                                break;

                            case ShapeMode.Cylinder:
                                returnSize = new Vector2(315, 226);
                                break;

                            case ShapeMode.LinearStairs:
                                returnSize = new Vector2(300, 283);
                                break;
                        }

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

        static IMGUIContainer activeToolContainer;

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (activeToolContainer == null)
            {
                activeToolContainer = new IMGUIContainer(() =>
                {
                    Handles.BeginGUI();

                    //For some reason the Surface Mode Window needs extra padding
                    Vector2 SurfaceModePadding = Vector2.zero;
                    if (ActiveToolName == "EditModeSurface")
                    {
                        SurfaceModePadding = new Vector2(2, -10);
                    }

                    //-2 and +31 adjusts for alignment with container and GUILayout.Spaces
                    //The GUILayout.Spaces are used to prevent clipping of the Active Tool Window
                    Rect viewRect = new Rect(-2 + SurfaceModePadding.x, ToolWindowSize.y - sceneView.rootVisualElement.contentRect.height + 31 + SurfaceModePadding.y, sceneView.rootVisualElement.contentRect.width, sceneView.rootVisualElement.contentRect.height - (CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 4));

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

                activeToolContainer.name = "RCSG-ToolWindow";
            }

            activeToolContainer.pickingMode = PickingMode.Position;

            activeToolContainer.style.position = Position.Absolute;

            activeToolContainer.style.left = 5;
            activeToolContainer.style.bottom = CsgguiStyleUtility.BOTTOM_TOOL_BAR_HEIGHT + 10;

            activeToolContainer.style.width = ToolWindowSize.x;
            activeToolContainer.style.height = ToolWindowSize.y;

            //activeToolContainer.style.backgroundColor = new Color(1, 0, 0, 1);

            var root = sceneView.rootVisualElement;

            IMGUIContainer foundContainer = root.Q<IMGUIContainer>("RCSG-ToolWindow");

            bool ForceHide = false;

            if(ActiveToolName == "EditModePlace" && (EditModeManager.FilteredSelection.nodeTargets == null || EditModeManager.FilteredSelection.nodeTargets.Length == 0))
            {
                ForceHide = true;
            }

            if (CSGSettings.EnableRealtimeCSG == true && foundContainer == null && EditModeManager.ActiveTool != null && ForceHide == false)
            {
                root.Add(activeToolContainer);
            }
            else if ((CSGSettings.EnableRealtimeCSG == false || EditModeManager.ActiveTool == null || SceneDragToolManager.IsDraggingObjectInScene == true|| ForceHide == true) && foundContainer != null)
            {
                root.Remove(foundContainer);
            }
        }
    }
}

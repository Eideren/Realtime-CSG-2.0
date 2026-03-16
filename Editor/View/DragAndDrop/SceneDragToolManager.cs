using UnityEngine;
using UnityEditor;

namespace RealtimeCSG
{
    internal sealed class SceneDragToolManager
    {
        public static bool IsDraggingObjectInScene { get; internal set; }

        private static SceneDragToolMeshDragOnSurface meshDragTool = new SceneDragToolMeshDragOnSurface();
        private static SceneDragToolBrushDragOnSurface brushDragTool = new SceneDragToolBrushDragOnSurface();
        private static SceneDragToolMaterials materialDragTool = new SceneDragToolMaterials();

        private static ISceneDragTool currentDragTool = null;
        private static bool currentDragToolActive = false;
        private static Transform currentTransformInInspector = null;
        private static bool draggingInScene = false;

        internal static void UpdateDragAndDrop()
        {
            // TODO: never use drag & drop code when dropping into inspector
            //			instead:
            //			find 'new' components, check if they're part of a prefab, 
            //			check if that prefab has a copy flag, and replace it with a copy

            if(currentTransformInInspector)
            {
                if(currentDragTool != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                    if(currentDragToolActive)
                    {
                        currentDragTool.Reset();
                    }

                    currentDragTool             = null;
                    currentTransformInInspector = null;
                    draggingInScene             = false;
                }

                currentTransformInInspector = null;
            }
        }

        internal static void OnPaint(Camera camera)
        {
            if(currentDragTool != null)
            {
                currentDragTool.OnPaint(camera);
            }
        }

        private static void ValidateDrop(SceneView sceneView, Transform transformInInspector)
        {
            if(currentDragTool != null)
            {
                currentDragTool.Reset();
            }

            currentDragTool             = null;
            currentDragToolActive       = false;
            currentTransformInInspector = transformInInspector;

            if(materialDragTool.ValidateDrop(sceneView))
            {
                currentDragTool = materialDragTool;
            }
            else if(brushDragTool.ValidateDrop(sceneView))
            {
                currentDragTool = brushDragTool;
            }
            else if(meshDragTool.ValidateDrop(sceneView))
            {
                currentDragTool = meshDragTool;
            }
        }

        internal static void OnHandleDragAndDrop(
            SceneView sceneView, Transform transformInInspector = null, Rect? selectionRect = null
        )
        {
            switch(Event.current.type)
            {
                case EventType.DragUpdated:
                {
                    InternalCSGModelManager.IgnoreMaterials = true;

                    if(!draggingInScene)
                    {
                        ValidateDrop(sceneView, transformInInspector);
                    }

                    if(currentDragTool != null)
                    {
                        if(!currentDragTool.ValidateDropPoint(sceneView))
                        {
                            if(currentDragTool != null && currentDragToolActive)
                            {
                                currentDragTool.DragExited(sceneView);
                            }

                            currentDragToolActive = false;
                            Event.current.Use();
                        }
                        else
                        {
                            currentDragToolActive   = true;
                            IsDraggingObjectInScene = true;
                            DragAndDrop.visualMode  = DragAndDropVisualMode.Generic;

                            if(sceneView)
                            {
                                if(currentDragTool.DragUpdated(sceneView))
                                {
                                    HandleUtility.Repaint();
                                }
                            }
                            else
                            {
                                if(currentDragTool.DragUpdated(sceneView, transformInInspector, selectionRect.Value))
                                {
                                    CSG_EditorGUIUtility.RepaintAll();
                                }
                            }

                            Event.current.Use();
                            draggingInScene = true;
                        }
                    }

                    break;
                }
                case EventType.DragPerform:
                {
                    InternalCSGModelManager.IgnoreMaterials = true;

                    if(currentDragTool != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                        if(currentDragToolActive)
                        {
                            currentDragTool.DragPerform(sceneView);
                            currentDragTool.Reset();
                            Event.current.Use();
                            GUIUtility.ExitGUI();
                        }

                        currentDragTool             = null;
                        currentTransformInInspector = null;
                        draggingInScene             = false;
                    }

                    break;
                }
                case EventType.DragExited:
                    //case EventType.MouseMove:
                {
                    InternalCSGModelManager.IgnoreMaterials = true;

                    if(currentDragTool != null)
                    {
                        currentDragTool.DragExited(sceneView);
                        Event.current.Use();
                        IsDraggingObjectInScene     = false;
                        currentDragTool             = null;
                        currentTransformInInspector = null;
                        draggingInScene             = false;
                        CSG_EditorGUIUtility.RepaintAll();
                        GUIUtility.ExitGUI();
                    }

                    break;
                }
                case EventType.MouseUp:
                case EventType.MouseMove:
                case EventType.MouseDrag:
                {
                    InternalCSGModelManager.IgnoreMaterials = false;

                    break;
                }
            }
        }
    }
}

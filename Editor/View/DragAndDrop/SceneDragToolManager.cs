using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class SceneDragToolManager
    {
        public static bool IsDraggingObjectInScene { get; internal set; }

        private static readonly SceneDragToolMeshDragOnSurface meshDragTool = new SceneDragToolMeshDragOnSurface();
        private static readonly SceneDragToolBrushDragOnSurface brushDragTool = new SceneDragToolBrushDragOnSurface();
        private static readonly SceneDragToolMaterials materialDragTool = new SceneDragToolMaterials();

        private static ISceneDragTool m_CurrentDragTool;
        private static bool m_CurrentDragToolActive;
        private static Transform m_CurrentTransformInInspector;
        private static bool m_DraggingInScene;

        internal static void UpdateDragAndDrop()
        {
            // TODO: never use drag & drop code when dropping into inspector
            //      instead:
            //      find 'new' components, check if they're part of a prefab,
            //      check if that prefab has a copy flag, and replace it with a copy

            if(m_CurrentTransformInInspector)
            {
                if(m_CurrentDragTool != null)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                    if(m_CurrentDragToolActive)
                    {
                        m_CurrentDragTool.Reset();
                    }

                    m_CurrentDragTool             = null;
                    m_CurrentTransformInInspector = null;
                    m_DraggingInScene             = false;
                }

                m_CurrentTransformInInspector = null;
            }
        }

        internal static void OnPaint(Camera camera)
        {
            if(m_CurrentDragTool != null)
            {
                m_CurrentDragTool.OnPaint(camera);
            }
        }

        private static void ValidateDrop(SceneView sceneView, Transform transformInInspector)
        {
            if(m_CurrentDragTool != null)
            {
                m_CurrentDragTool.Reset();
            }

            m_CurrentDragTool             = null;
            m_CurrentDragToolActive       = false;
            m_CurrentTransformInInspector = transformInInspector;

            if(materialDragTool.ValidateDrop(sceneView))
            {
                m_CurrentDragTool = materialDragTool;
            }
            else if(brushDragTool.ValidateDrop(sceneView))
            {
                m_CurrentDragTool = brushDragTool;
            }
            else if(meshDragTool.ValidateDrop(sceneView))
            {
                m_CurrentDragTool = meshDragTool;
            }
        }

        internal static void OnHandleDragAndDrop(SceneView sceneView, Transform transformInInspector = null, Rect? selectionRect = null)
        {
            switch(Event.current.type)
            {
                case EventType.DragUpdated:
                {
                    InternalCSGModelManager.IgnoreMaterials = true;

                    if(!m_DraggingInScene)
                    {
                        ValidateDrop(sceneView, transformInInspector);
                    }

                    if(m_CurrentDragTool != null)
                    {
                        if(!m_CurrentDragTool.ValidateDropPoint(sceneView))
                        {
                            if(m_CurrentDragTool != null && m_CurrentDragToolActive)
                            {
                                m_CurrentDragTool.DragExited(sceneView);
                            }

                            m_CurrentDragToolActive = false;
                            Event.current.Use();
                        }
                        else
                        {
                            m_CurrentDragToolActive = true;
                            IsDraggingObjectInScene = true;
                            DragAndDrop.visualMode  = DragAndDropVisualMode.Generic;

                            if(sceneView)
                            {
                                if(m_CurrentDragTool.DragUpdated(sceneView))
                                {
                                    HandleUtility.Repaint();
                                }
                            }
                            else
                            {
                                if(selectionRect != null && m_CurrentDragTool.DragUpdated(sceneView, transformInInspector, selectionRect.Value))
                                {
                                    CSGEditorGUIUtility.RepaintAll();
                                }
                            }

                            Event.current.Use();
                            m_DraggingInScene = true;
                        }
                    }

                    break;
                }
                case EventType.DragPerform:
                {
                    InternalCSGModelManager.IgnoreMaterials = true;

                    if(m_CurrentDragTool != null)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                        if(m_CurrentDragToolActive)
                        {
                            m_CurrentDragTool.DragPerform(sceneView);
                            m_CurrentDragTool.Reset();
                            Event.current.Use();
                            GUIUtility.ExitGUI();
                        }

                        m_CurrentDragTool             = null;
                        m_CurrentTransformInInspector = null;
                        m_DraggingInScene             = false;
                    }

                    break;
                }
                case EventType.DragExited:
                {
                    InternalCSGModelManager.IgnoreMaterials = true;

                    if(m_CurrentDragTool != null)
                    {
                        m_CurrentDragTool.DragExited(sceneView);
                        Event.current.Use();
                        IsDraggingObjectInScene       = false;
                        m_CurrentDragTool             = null;
                        m_CurrentTransformInInspector = null;
                        m_DraggingInScene             = false;
                        CSGEditorGUIUtility.RepaintAll();
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

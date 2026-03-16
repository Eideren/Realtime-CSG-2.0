using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal static class SceneToolRenderer
    {
        private static int meshGeneration = -1;

        private static LineMeshManager lineMeshManager = new LineMeshManager();

        internal static void Cleanup()
        {
            meshGeneration = -1;
            lineMeshManager.Destroy();
        }

        private static bool forceOutlineUpdate = false;

        internal static void SetOutlineDirty()
        {
            forceOutlineUpdate = false;
        }

        internal static void OnPaint(SceneView sceneView)
        {
            if(!sceneView)
            {
                return;
            }

            Camera camera = sceneView.camera;
            SceneDragToolManager.OnPaint(camera);

            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            if(CSGSettings.GridVisible)
            {
                CSGGrid.RenderGrid(camera);
            }

            if(CSGSettings.IsWireframeShown(sceneView))
            {
                if(forceOutlineUpdate || meshGeneration != InternalCSGModelManager.MeshGeneration)
                {
                    forceOutlineUpdate = false;
                    meshGeneration     = InternalCSGModelManager.MeshGeneration;
                    lineMeshManager.Begin();

                    for(int i = 0; i < InternalCSGModelManager.Brushes.Count; i++)
                    {
                        CSGBrush brush = InternalCSGModelManager.Brushes[i];

                        if(!brush)
                        {
                            continue;
                        }

                        if(!brush.outlineColor.HasValue)
                        {
                            brush.outlineColor = ColorSettings.GetBrushOutlineColor(brush);
                        }

                        Matrix4x4 brush_transformation = brush.compareTransformation.localToWorldMatrix;

                        CSGRenderer.DrawSimpleOutlines(
                            lineMeshManager, brush.brushNodeID, brush_transformation, brush.outlineColor.Value
                        );
                    }

                    lineMeshManager.End();
                }

                MaterialUtility.LineDashMultiplier      = 1.0f;
                MaterialUtility.LineThicknessMultiplier = 1.0f;
                MaterialUtility.LineAlphaMultiplier     = 1.0f;
                lineMeshManager.Render(MaterialUtility.NoZTestGenericLine);
            }
        }
    }
}

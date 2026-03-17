using RealtimeCSG.Components;
using RealtimeCSG.Legacy;
using UnityEngine;

namespace RealtimeCSG
{
    internal class BrushOutlineRenderer
    {
        private readonly LineMeshManager _outlinesManager = new LineMeshManager();
        private readonly LineMeshManager _edgeColorsManager = new LineMeshManager();
        private readonly PolygonMeshManager _polygonManager = new PolygonMeshManager();

        public void Destroy()
        {
            _outlinesManager.Destroy();
            _edgeColorsManager.Destroy();
            _polygonManager.Destroy();
        }

        public void Update(
            Camera camera, CSGBrush[] brushes, ControlMesh[] controlMeshes, ControlMeshState[] meshStates
        )
        {
            if(brushes.Length == 0)
            {
                _outlinesManager.Clear();
                _edgeColorsManager.Clear();
                _polygonManager.Clear();

                return;
            }

            _outlinesManager.Begin();
            _edgeColorsManager.Begin();
            _polygonManager.Begin();

            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush brush = brushes[t];

                if(!brush)
                {
                    continue;
                }

                ControlMeshState meshState = meshStates[t];

                if(meshState.worldPoints.Length == 0 &&
                   meshState.edges.Length == 0)
                {
                    continue;
                }

                if(!meshState.UpdateColors(camera, controlMeshes[t]))
                {
                    continue;
                }

                _outlinesManager.DrawLines(meshState.worldPoints, meshState.edges, ColorSettings.MeshEdgeOutline);
                _edgeColorsManager.DrawLines(meshState.worldPoints, meshState.edges, meshState.edgeColors);

                for(int p = 0; p < meshState.polygonPointIndices.Length; p++)
                {
                    if(meshState.polygonColors[p].a < 1.0f / 255.0f)
                    {
                        continue;
                    }

                    Color color         = meshState.polygonColors[p];
                    int[] polygonPoints = meshState.polygonPointIndices[p];
                    _polygonManager.DrawPolygon(meshState.worldPoints, polygonPoints, color);
                }
            }

            _polygonManager.End();
            _edgeColorsManager.End();
            _outlinesManager.End();
        }

        public void RenderOutlines()
        {
            Material zTestGenericLineMaterial   = MaterialUtility.ZTestGenericLine;
            Material noZTestGenericLineMaterial = MaterialUtility.NoZTestGenericLine;
            Material coloredPolygonMaterial     = MaterialUtility.ColoredPolygonMaterial;

            _polygonManager.Render(coloredPolygonMaterial);

            MaterialUtility.LineAlphaMultiplier     = 0.75f;
            MaterialUtility.LineDashMultiplier      = 4.0f;
            MaterialUtility.LineThicknessMultiplier = GUIConstants.THICK_LINE_SCALE * 2.0f;
            _outlinesManager.Render(noZTestGenericLineMaterial);

            MaterialUtility.LineDashMultiplier      = 0.0f;
            MaterialUtility.LineThicknessMultiplier = GUIConstants.THICK_LINE_SCALE * 2.0f;
            _outlinesManager.Render(zTestGenericLineMaterial);

            MaterialUtility.LineDashMultiplier      = 4.0f;
            MaterialUtility.LineThicknessMultiplier = GUIConstants.THICK_LINE_SCALE;
            _edgeColorsManager.Render(noZTestGenericLineMaterial);

            MaterialUtility.LineDashMultiplier      = 0.0f;
            MaterialUtility.LineThicknessMultiplier = GUIConstants.THICK_LINE_SCALE;
            _edgeColorsManager.Render(zTestGenericLineMaterial);

            MaterialUtility.LineAlphaMultiplier = 1.0f;
        }
    }
}

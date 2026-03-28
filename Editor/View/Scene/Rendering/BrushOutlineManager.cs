using System;
using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal class BrushOutlineManager
    {
        private static readonly Dictionary<int, GeometryWireframe> outlineCache =
            new Dictionary<int, GeometryWireframe>();

#region ClearOutlines

        public static void ClearOutlines()
        {
            outlineCache.Clear();
        }

#endregion

#region ForceUpdateOutlines

        public static void ForceUpdateOutlines(int brushNodeId)
        {
            ulong externalOutlineGeneration = InternalCSGModelManager.External.getBrushOutlineGeneration(brushNodeId);

            if(!InternalCSGModelManager.External.getBrushOutline(brushNodeId, out GeometryWireframe outline))
            {
                return;
            }

            outline.outlineGeneration = externalOutlineGeneration;
            outlineCache[brushNodeId] = outline;
        }

#endregion

#region GetBrushOutline

        public static GeometryWireframe GetBrushOutline(int brushNodeId)
        {
            if(brushNodeId == CSGNode.INVALID_NODE_ID)
            {
                return null;
            }

            ulong externalOutlineGeneration = InternalCSGModelManager.External.getBrushOutlineGeneration(brushNodeId);

            if(!outlineCache.TryGetValue(brushNodeId, out GeometryWireframe outline))
            {
                externalOutlineGeneration -= 1;
            }

            if(outline != null && externalOutlineGeneration == outline.outlineGeneration)
            {
                return outline;
            }

            if(!InternalCSGModelManager.External.getBrushOutline(brushNodeId, out outline))
            {
                return null;
            }

            outline = new GeometryWireframe();

            outline.outlineGeneration = externalOutlineGeneration;
            outlineCache[brushNodeId] = outline;

            return outline;
        }

        public static GeometryWireframe[] GetBrushOutlines(int[] brushNodeIDs)
        {
            GeometryWireframe[] wireframes = new GeometryWireframe[brushNodeIDs.Length];

            for(int i = 0; i < brushNodeIDs.Length; i++)
            {
                int brushNodeId = brushNodeIDs[i];

                if(brushNodeId == CSGNode.INVALID_NODE_ID)
                {
                    wireframes[i] = null;

                    continue;
                }

                ulong externalOutlineGeneration = InternalCSGModelManager.External.getBrushOutlineGeneration(brushNodeId);

                if(!outlineCache.TryGetValue(brushNodeId, out GeometryWireframe outline))
                {
                    externalOutlineGeneration -= 1;
                }

                if(outline == null || externalOutlineGeneration != outline.outlineGeneration)
                {
                    if(!InternalCSGModelManager.External.getBrushOutline(brushNodeId, out outline))
                    {
                        outline = null;
                    }
                    else
                    {
                        outline.outlineGeneration = externalOutlineGeneration;
                        outlineCache[brushNodeId] = outline;
                    }
                }

                wireframes[i] = outline;
            }

            return wireframes;
        }

#endregion

#region GetSurfaceOutline

        public static GeometryWireframe[] GetSurfaceOutlines(SelectedBrushSurface[] selectedSurfaces)
        {
            if(selectedSurfaces == null || selectedSurfaces.Length == 0)
            {
                return Array.Empty<GeometryWireframe>();
            }

            GeometryWireframe[] wireframes = new GeometryWireframe[selectedSurfaces.Length];

            for(int i = 0; i < selectedSurfaces.Length; i++)
            {
                CSGBrush brush = selectedSurfaces[i].brush;

                if(!brush || brush.brushNodeID == CSGNode.INVALID_NODE_ID)
                {
                    wireframes[i] = null;

                    continue;
                }

                if(!InternalCSGModelManager.External.getSurfaceOutline(brush.brushNodeID, selectedSurfaces[i].surfaceIndex, out GeometryWireframe outline))
                {
                    outline = null;
                }

                wireframes[i] = outline;
            }

            return wireframes;
        }

#endregion
    }
}

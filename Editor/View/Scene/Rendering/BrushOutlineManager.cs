using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal class BrushOutlineManager
    {
        private static readonly Dictionary<int, GeometryWireframe> OutlineCache =
            new Dictionary<int, GeometryWireframe>();

#region ClearOutlines

        public static void ClearOutlines()
        {
            OutlineCache.Clear();
        }

#endregion

#region ForceUpdateOutlines

        public static void ForceUpdateOutlines(int brushNodeId)
        {
            ulong externalOutlineGeneration = InternalCSGModelManager.External.getBrushOutlineGeneration(brushNodeId);
            GeometryWireframe outline = new GeometryWireframe();

            if(!InternalCSGModelManager.External.getBrushOutline(brushNodeId, out outline))
            {
                return;
            }

            outline.outlineGeneration = externalOutlineGeneration;
            OutlineCache[brushNodeId] = outline;
        }

#endregion

#region GetBrushOutline

        public static GeometryWireframe GetBrushOutline(int brushNodeId)
        {
            if(brushNodeId == CSGNode.InvalidNodeID)
            {
                return null;
            }

            ulong externalOutlineGeneration = InternalCSGModelManager.External.getBrushOutlineGeneration(brushNodeId);

            GeometryWireframe outline;

            if(!OutlineCache.TryGetValue(brushNodeId, out outline))
            {
                externalOutlineGeneration = externalOutlineGeneration - 1;
            }

            if(outline != null &&
               externalOutlineGeneration == outline.outlineGeneration)
            {
                return outline;
            }

            outline = new GeometryWireframe();

            if(!InternalCSGModelManager.External.getBrushOutline(brushNodeId, out outline))
            {
                return null;
            }

            outline.outlineGeneration = externalOutlineGeneration;
            OutlineCache[brushNodeId] = outline;

            return outline;
        }

        public static GeometryWireframe[] GetBrushOutlines(int[] brushNodeIDs)
        {
            GeometryWireframe[] wireframes = new GeometryWireframe[brushNodeIDs.Length];

            for(int i = 0; i < brushNodeIDs.Length; i++)
            {
                int brushNodeId = brushNodeIDs[i];

                if(brushNodeId == CSGNode.InvalidNodeID)
                {
                    wireframes[i] = null;

                    continue;
                }

                ulong externalOutlineGeneration =
                    InternalCSGModelManager.External.getBrushOutlineGeneration(brushNodeId);

                GeometryWireframe outline;

                if(!OutlineCache.TryGetValue(brushNodeId, out outline))
                {
                    externalOutlineGeneration = externalOutlineGeneration - 1;
                }

                if(outline == null ||
                   externalOutlineGeneration != outline.outlineGeneration)
                {
                    if(!InternalCSGModelManager.External.getBrushOutline(brushNodeId, out outline))
                    {
                        outline = null;
                    }
                    else
                    {
                        outline.outlineGeneration = externalOutlineGeneration;
                        OutlineCache[brushNodeId] = outline;
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
                return new GeometryWireframe[0];
            }

            GeometryWireframe[] wireframes = new GeometryWireframe[selectedSurfaces.Length];

            for(int i = 0; i < selectedSurfaces.Length; i++)
            {
                CSGBrush brush = selectedSurfaces[i].brush;

                if(!brush || brush.brushNodeID == CSGNode.InvalidNodeID)
                {
                    wireframes[i] = null;

                    continue;
                }

                int brushNodeId  = brush.brushNodeID;
                int surfaceIndex = selectedSurfaces[i].surfaceIndex;

                GeometryWireframe outline;

                if(!InternalCSGModelManager.External.getSurfaceOutline(
                       brushNodeId,
                       surfaceIndex,
                       out outline
                   ))
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

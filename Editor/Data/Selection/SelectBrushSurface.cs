using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal sealed class SelectedBrushSurface
    {
        public SelectedBrushSurface(CSGBrush brush, int surfaceIndex, CSGPlane surfacePlane)
        {
            this.brush        = brush;
            this.surfaceIndex = surfaceIndex;
            this.surfacePlane = surfacePlane;
        }

        public SelectedBrushSurface(CSGBrush brush, int surfaceIndex, CSGPlane? surfacePlane = null)
        {
            this.brush        = brush;
            this.surfaceIndex = surfaceIndex;
            this.surfacePlane = surfacePlane;
        }

        public CSGBrush brush;
        public readonly int surfaceIndex;
        public CSGPlane? surfacePlane;
    }
}

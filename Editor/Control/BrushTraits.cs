using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal static class BrushTraits
    {
        public static bool IsSurfaceUnselectable(CSGBrush brush, int surfaceIndex, bool isTrigger, bool ignoreSurfaceFlags = false)
        {
            if(!brush)
            {
                return true;
            }

            Shape shape = brush.shape;

            if(shape == null)
            {
                return true;
            }

            Surface[] surfaces = shape.surfaces;

            if(surfaces == null)
            {
                return true;
            }

            if(surfaceIndex < 0 || surfaceIndex >= surfaces.Length)
            {
                return true;
            }

            int           texGenIndex = surfaces[surfaceIndex].TexGenIndex;
            TexGenFlags[] texGenFlags = shape.texGenFlags;

            if(texGenFlags == null || texGenIndex < 0 || texGenIndex >= texGenFlags.Length)
            {
                return true;
            }

            if((texGenFlags[texGenIndex] & TexGenFlags.NoRender) == TexGenFlags.NoRender)
            {
                return false;
            }

            if(ignoreSurfaceFlags)
            {
                bool isNotRenderable = (texGenFlags[texGenIndex] & TexGenFlags.NoRender) == TexGenFlags.NoRender;

                if(!isNotRenderable)
                {
                    return false;
                }

                if(CSGSettings.ShowHiddenSurfaces)
                {
                    return false;
                }

                bool isCollidable = (texGenFlags[texGenIndex] & TexGenFlags.NoCollision) != TexGenFlags.NoCollision;

                if(isCollidable)
                {
                    if(isTrigger)
                    {
                        if(CSGSettings.ShowTriggerSurfaces)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if(CSGSettings.ShowColliderSurfaces)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            return false;
        }
    }
}

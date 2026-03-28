using System;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    // this allows us to determine if our brush has any of it's surfaces changed
#if UNITY_EDITOR
    [Serializable]
    public sealed class CompareShape
    {
        public Surface[] prevSurfaces = Array.Empty<Surface>();
        public TexGen[] prevTexGens = Array.Empty<TexGen>();
        public TexGenFlags[] prevTexGenFlags = Array.Empty<TexGenFlags>();

        public void EnsureInitialized(Shape shape)
        {
            if(prevTexGens == null || prevTexGens.Length != shape.texGens.Length)
            {
                prevTexGens     = new TexGen[shape.texGens.Length];
                prevTexGenFlags = new TexGenFlags[shape.texGens.Length];
            }

            Array.Copy(shape.texGens, prevTexGens, shape.texGens.Length);
            Array.Copy(shape.texGenFlags, prevTexGenFlags, shape.texGenFlags.Length);

            if(prevSurfaces == null || prevSurfaces.Length != shape.surfaces.Length)
            {
                prevSurfaces = new Surface[shape.surfaces.Length];
            }

            Array.Copy(shape.surfaces, prevSurfaces, shape.surfaces.Length);
        }

        public void Reset()
        {
            prevSurfaces    = Array.Empty<Surface>();
            prevTexGens     = Array.Empty<TexGen>();
            prevTexGenFlags = Array.Empty<TexGenFlags>();
        }
    }
#endif
}

using RealtimeCSG.Foundation;
using UnityEngine;

namespace RealtimeCSG.Legacy
{
    /// <summary>Specifies if a matrix, used to calculate uv coordinates with, is in plane or world space</summary>
    /// <remarks><note>This code is legacy and will be removed eventually.</note></remarks>
    public enum TextureMatrixSpace
    {
        /// <summary>Texture matrix is in world space</summary>
        WorldSpace

       ,

        /// <summary>Texture matrix is in plane space</summary>
        PlaneSpace
    }

    /// <summary>Legacy factory class to create <see cref="ControlMesh"/>es and <see cref="Shape"/>s with</summary>
    /// <remarks><note>This class is legacy and will be replaced by a <see cref="RealtimeCSG.Foundation.BrushMesh"/> based version eventually.</note></remarks>
    partial class BrushFactory
    {
        /// <summary>Converts a <see cref="RealtimeCSG.Legacy.Surface"/>/<see cref="RealtimeCSG.Legacy.TexGen"/>/<see cref="RealtimeCSG.Legacy.TexGenFlags"/> combination into a <see cref="RealtimeCSG.Foundation.SurfaceDescription"/>.</summary>
        /// <param name="surface">A legacy <see cref="RealtimeCSG.Legacy.Surface"/> that describes how the texture space is orientated relative to the brush.</param>
        /// <param name="texGen">A legacy <see cref="RealtimeCSG.Legacy.TexGen"/> that describes how the texture coordinates are calculated in the <see cref="RealtimeCSG.Foundation.SurfaceDescription"/>.</param>
        /// <param name="texGenFlags">A legacy <see cref="RealtimeCSG.Legacy.TexGenFlags"/> enum that describes if the <see cref="RealtimeCSG.Foundation.SurfaceDescription"/> texture generation will be in world-space or brush-space.</param>
        /// <returns>A new <see cref="RealtimeCSG.Foundation.SurfaceDescription"/></returns>
        public static SurfaceDescription CreateSurfaceDescription(Surface surface, TexGen texGen, TexGenFlags texGenFlags)
        {
            Matrix4x4 localToTextureSpace = SurfaceUtility.GetLocalToTextureSpaceMatrix(texGen, surface);

            SurfaceDescription description;
            description.smoothingGroup = texGen.SmoothingGroup;

            description.surfaceFlags = (texGenFlags & TexGenFlags.WorldSpaceTexture) == 0 ? SurfaceFlags.None : SurfaceFlags.TextureIsInWorldSpace;

            description.UV0.U = localToTextureSpace.GetRow(0);
            description.UV0.V = localToTextureSpace.GetRow(1);

            return description;
        }

        /// <summary>Converts a <see cref="RealtimeCSG.Legacy.TexGen"/>/<see cref="RealtimeCSG.Legacy.TexGenFlags"/> pair into a <see cref="RealtimeCSG.Foundation.SurfaceLayers"/>.</summary>
        /// <param name="texGen">A legacy <see cref="RealtimeCSG.Legacy.TexGen"/> that describes what layer parameters will be used for this surface.</param>
        /// <param name="texGenFlags">A legacy <see cref="RealtimeCSG.Legacy.TexGenFlags"/> that describes what layers will be used for this surface</param>
        /// <returns>A new <see cref="RealtimeCSG.Foundation.SurfaceLayers"/></returns>
        public static SurfaceLayers CreateSurfaceLayer(TexGen texGen, TexGenFlags texGenFlags)
        {
            SurfaceLayers layers;

            bool renderable     = (texGenFlags & TexGenFlags.NoRender) != TexGenFlags.NoRender;
            bool receiveShadows = (texGenFlags & TexGenFlags.NoReceiveShadows) != TexGenFlags.NoReceiveShadows;
            bool castShadows    = (texGenFlags & TexGenFlags.NoCastShadows) != TexGenFlags.NoCastShadows;
            bool collidable     = (texGenFlags & TexGenFlags.NoCollision) != TexGenFlags.NoCollision;

            layers.layerUsage = (renderable ? LayerUsageFlags.Renderable : LayerUsageFlags.None) |
                (receiveShadows ? LayerUsageFlags.ReceiveShadows : LayerUsageFlags.None) |
                (castShadows ? LayerUsageFlags.CastShadows : LayerUsageFlags.None) |
                (collidable ? LayerUsageFlags.Collidable : LayerUsageFlags.None);

            layers.layerParameter1 = texGen.RenderMaterial ? texGen.RenderMaterial.GetInstanceID() : 0;
            layers.layerParameter2 = texGen.PhysicsMaterial ? texGen.PhysicsMaterial.GetInstanceID() : 0;
            layers.layerParameter3 = 0;

            return layers;
        }

        /// <summary>
        /// Creates a cube <see cref="RealtimeCSG.Foundation.BrushMesh"/> with <paramref name="size"/> and optional <paramref name="material"/>
        /// </summary>
        /// <param name="size">The size of the cube</param>
        /// <param name="material">The [UnityEngine.Material](https://docs.unity3d.com/ScriptReference/Material.html) that will be set to all surfaces of the cube (optional)</param>
        /// <returns>A <see cref="RealtimeCSG.Foundation.BrushMesh"/> on success, null on failure</returns>
        public static BrushMesh CreateCube(Vector3 size, Material material = null)
        {
            Vector3 halfSize = size * 0.5f;

            return !CreateCubeControlMesh(out ControlMesh controlMesh, out Shape shape, halfSize, -halfSize, material) ? null : GenerateFromControlMesh(controlMesh, shape);
        }
    }
}

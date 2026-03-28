using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
    /// <summary>Describes what surface parameters, and optionally which surface parameter, to query for in a model, to create a mesh.</summary>
    /// <remarks>See the [Create Unity Meshes](~/documentation/createUnityMesh.md) article for more information.
    /// <note>This struct is mirrored on the native side and cannot be modified.</note></remarks>
    /// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers"/>
    /// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon"/>
    /// <seealso cref="RealtimeCSG.Foundation.LayerUsageFlags"/>
    /// <seealso cref="RealtimeCSG.Foundation.LayerParameterIndex"/>
    /// <seealso cref="RealtimeCSG.Foundation.CSGTree.GetMeshDescriptions" />
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MeshQuery
    {
        private const int BIT_SHIFT = 24;
        private const uint BIT_MASK = (uint)127 << BIT_SHIFT;

        /// <summary>Constructs a <see cref="RealtimeCSG.Foundation.MeshQuery"/> to use to specify which surfaces should be combined into meshes, and should they be subdivided by a particular layer parameter index.</summary>
        /// <param name="query">Which layer combination would we like to look for and generate a mesh with.</param>
        /// <param name="mask">What layers do we ignore, and what layers do we include in our comparison. When this value is <see cref="RealtimeCSG.Foundation.LayerUsageFlags.None"/>, <paramref name="mask"/> is set to be equal to <paramref name="query"/>.</param>
        /// <param name="parameterIndex">Which parameter index we use to, for example, differentiate between different [UnityEngine.Material](https://docs.unity3d.com/ScriptReference/Material.html)s</param>
        /// <param name="vertexChannels">Which vertex channels need to be used for the meshes we'd like to generate.</param>
        /// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers" />
        public MeshQuery(LayerUsageFlags query, LayerUsageFlags mask = LayerUsageFlags.None, LayerParameterIndex parameterIndex = LayerParameterIndex.None, VertexChannelFlags vertexChannels = VertexChannelFlags.Position)
        {
            if(mask == LayerUsageFlags.None)
            {
                mask = query;
            }

            layers          = ((uint)query & ~BIT_MASK) | ((uint)parameterIndex << BIT_SHIFT);
            maskAndChannels = ((uint)mask & ~BIT_MASK) | ((uint)vertexChannels << BIT_SHIFT);
        }

        [SerializeField]
        private uint layers; // 24 bit layer-usage / 8 bit parameter-index

        [SerializeField]
        private uint maskAndChannels; // 24 bit layer-mask  / 8 bit vertex-channels

        /// <value>Which layer combination would we like to look for and generate a mesh with</value>
        /// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers" />
        /// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon" />
        public LayerUsageFlags LayerQuery
        {
            get => (LayerUsageFlags)(layers & ~BIT_MASK);
            set => layers = ((uint)value & ~BIT_MASK) | (layers & BIT_MASK);
        }

        /// <value>What layers do we ignore, and what layers do we include in our comparison</value>
        /// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers" />
        /// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon" />
        public LayerUsageFlags LayerQueryMask
        {
            get => (LayerUsageFlags)(maskAndChannels & ~BIT_MASK);
            set => maskAndChannels = ((uint)value & ~BIT_MASK) | (maskAndChannels & BIT_MASK);
        }

        /// <value>Which parameter index we use to, for example, differentiate between different [UnityEngine.Material](https://docs.unity3d.com/ScriptReference/Material.html)s.</value>
        /// <seealso cref="RealtimeCSG.Foundation.LayerParameterIndex" />
        /// <seealso cref="RealtimeCSG.Foundation.SurfaceLayers" />
        /// <seealso cref="RealtimeCSG.Foundation.BrushMesh.Polygon" />
        public LayerParameterIndex LayerParameterIndex
        {
            get => (LayerParameterIndex)((layers & BIT_MASK) >> BIT_SHIFT);
            set => layers = (layers & ~BIT_MASK) | ((uint)value << BIT_SHIFT);
        }

        /// <value>Which vertex channels need to be used for the meshes we'd like to generate</value>
        /// <seealso cref="RealtimeCSG.Foundation.CSGTree.GetMeshDescriptions" />
        public VertexChannelFlags UsedVertexChannels
        {
            get => (VertexChannelFlags)((maskAndChannels & BIT_MASK) >> BIT_SHIFT);
            set => maskAndChannels = (maskAndChannels & ~BIT_MASK) | ((uint)value << BIT_SHIFT);
        }

        public override string ToString()
        {
            return $"({layers}, {maskAndChannels})";
        }

#region Comparison

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator ==(MeshQuery left, MeshQuery right)
        {
            return left.layers == right.layers && left.maskAndChannels == right.maskAndChannels;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool operator !=(MeshQuery left, MeshQuery right)
        {
            return left.layers != right.layers || left.maskAndChannels != right.maskAndChannels;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            if(!(obj is MeshQuery other))
            {
                return false;
            }

            return layers == other.layers && maskAndChannels == other.maskAndChannels;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Equals(MeshQuery other)
        {
            return layers == other.layers && maskAndChannels == other.maskAndChannels;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool Equals(MeshQuery left, MeshQuery right)
        {
            return left.layers == right.layers && left.maskAndChannels == right.maskAndChannels;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            int hashCode = -1385006369;
            hashCode *= -1521134295;
            hashCode =  hashCode * -1521134295 + (int)layers;
            hashCode =  hashCode * -1521134295 + (int)maskAndChannels;

            return hashCode;
        }

#endregion
    }
}

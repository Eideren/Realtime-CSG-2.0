using System;
using InternalRealtimeCSG;
using UnityEngine;

namespace RealtimeCSG.Legacy
{
    /// <summary>A <see cref="RealtimeCSG.Legacy.HalfEdge"/> of a <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
    /// <remarks><note>This code will be replaced by pure <see cref="RealtimeCSG.Foundation.BrushMesh"/> code eventually.</note>
    /// <note>This class can be safely serialized.</note></remarks>
    /// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
    /// <seealso cref="RealtimeCSG.Legacy.Polygon"/>
    /// <seealso cref="RealtimeCSG.Legacy.Shape"/>
    [Serializable]
    public struct HalfEdge
    {
        /// <summary>Creates a HalfEdge for a <see cref="RealtimeCSG.Legacy.ControlMesh"/></summary>
        /// <param name="polygonIndex">The index to the <see cref="RealtimeCSG.Legacy.Polygon"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</param>
        /// <param name="twinIndex">The index to the twin edge <see cref="RealtimeCSG.Legacy.HalfEdge"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</param>
        /// <param name="vertexIndex">The index to a vertex in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</param>
        public HalfEdge(short polygonIndex, int twinIndex, short vertexIndex, bool hardEdge = true)
        {
            this.twinIndex    = twinIndex;
            this.polygonIndex = polygonIndex;
            this.hardEdge     = hardEdge;
            this.vertexIndex  = vertexIndex;
        }

        /// <value>Index to the twin edge <see cref="RealtimeCSG.Legacy.HalfEdge"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</value>
        public int twinIndex;

        /// <value>Index to the <see cref="RealtimeCSG.Legacy.Polygon"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</value>
        public short polygonIndex;

        /// <value>Is this edge considered an internal or external (hard) edge</value>
        public bool hardEdge;

        /// <value>Index to a vertex in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.HalfEdge"/> belongs to.</value>
        public short vertexIndex;
    }

    /// <summary>A <see cref="RealtimeCSG.Legacy.Polygon"/> of a <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
    /// <remarks><note>This code will be replaced by pure <see cref="RealtimeCSG.Foundation.BrushMesh"/> code eventually.</note>
    /// <note>This class can be safely serialized.</note></remarks>
    /// <seealso cref="RealtimeCSG.Legacy.ControlMesh"/>
    /// <seealso cref="RealtimeCSG.Legacy.HalfEdge"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGen"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGenFlags"/>
    /// <seealso cref="RealtimeCSG.Legacy.Shape"/>
    [Serializable]
    public sealed class Polygon
    {
        /// <summary>Creates a Polygon for a <see cref="RealtimeCSG.Legacy.ControlMesh"/></summary>
        /// <param name="edges">A list of indices to <see cref="RealtimeCSG.Legacy.HalfEdge"/>s in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.Polygon"/> belongs to, that form this <see cref="RealtimeCSG.Legacy.Polygon"/>.</param>
        /// <param name="texGenIndex">An index to the <see cref="RealtimeCSG.Legacy.TexGen"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> that this <see cref="RealtimeCSG.Legacy.Polygon"/> uses.</param>
        public Polygon(int[] edges, int texGenIndex)
        {
            edgeIndices      = edges;
            this.texGenIndex = texGenIndex;
        }

        /// <value>indices to <see cref="RealtimeCSG.Legacy.HalfEdge"/>s in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> this <see cref="RealtimeCSG.Legacy.Polygon"/> belongs to.</value>
        public int[] edgeIndices;

        /// <value>Index to the <see cref="RealtimeCSG.Legacy.TexGen"/> in the <see cref="RealtimeCSG.Legacy.ControlMesh"/> that this <see cref="RealtimeCSG.Legacy.Polygon"/> uses.</value>
        public int texGenIndex;
    }

    /// <summary>Defines the shape of a convex brush that can be used in CSG operations.</summary>
    /// <remarks><note>This code will be replaced by pure <see cref="RealtimeCSG.Foundation.BrushMesh"/> code eventually.</note>
    /// <note>This class can be safely serialized.</note></remarks>
    /// <seealso cref="RealtimeCSG.Legacy.Polygon"/>
    /// <seealso cref="RealtimeCSG.Legacy.HalfEdge"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGen"/>
    /// <seealso cref="RealtimeCSG.Legacy.TexGenFlags"/>
    /// <seealso cref="RealtimeCSG.Legacy.Shape"/>
    [Serializable]
    public sealed class ControlMesh
    {
        /// <value>Vertices that are used by this <see cref="RealtimeCSG.Legacy.ControlMesh"/></value>
        public Vector3[] vertices;

        /// <value><see cref="RealtimeCSG.Legacy.HalfEdge"/>s that are used by this <see cref="RealtimeCSG.Legacy.ControlMesh"/></value>
        public HalfEdge[] edges;

        /// <value><see cref="RealtimeCSG.Legacy.Polygon"/>s that are used by this <see cref="RealtimeCSG.Legacy.ControlMesh"/></value>
        public Polygon[] polygons;

        /// <value><b>true</b> if valid, <b>false</b> if not.</value>
        public bool Valid
        {
            get => valid;
            set => valid = value;
        }

        internal bool valid;

        /// <value>Every time this ControlMesh changes, this value can be increased to easily detect modifications over time.</value>
        public int generation;

        /// <summary>Create an uninitialized <see cref="RealtimeCSG.Legacy.ControlMesh"/></summary>
        public ControlMesh()
        {
        }

        /// <summary>Create new <see cref="RealtimeCSG.Legacy.ControlMesh"/> by copying an existing <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
        /// <param name="other">The <see cref="RealtimeCSG.Legacy.ControlMesh"/> to copy from.</param>
        public ControlMesh(ControlMesh other)
        {
            CopyFrom(other);
        }

        /// <summary>Increase the <see cref="generation"/> of this <see cref="RealtimeCSG.Legacy.ControlMesh"/> so that we can detect that its been changed.</summary>
        public void SetDirty()
        {
            generation++;
        }

        /// <summary>Clear the contents of this <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
        public void Reset()
        {
            vertices   = null;
            edges      = null;
            polygons   = null;
            valid      = false;
            generation = 0;
        }

        /// <summary>Copy the contents of another <see cref="RealtimeCSG.Legacy.ControlMesh"/> to this <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
        /// <param name="other">The <see cref="RealtimeCSG.Legacy.ControlMesh"/> to copy from.</param>
        public void CopyFrom(ControlMesh other)
        {
            if(other == null)
            {
                Reset();

                return;
            }

            if(other.vertices != null)
            {
                if(vertices == null || vertices.Length != other.vertices.Length)
                {
                    vertices = new Vector3[other.vertices.Length];
                }

                Array.Copy(other.vertices, vertices, other.vertices.Length);
            }
            else
            {
                vertices = null;
            }

            if(other.edges != null)
            {
                if(edges == null || edges.Length != other.edges.Length)
                {
                    edges = new HalfEdge[other.edges.Length];
                }

                Array.Copy(other.edges, edges, other.edges.Length);
            }
            else
            {
                edges = null;
            }

            if(other.polygons != null)
            {
                if(polygons == null || polygons.Length != other.polygons.Length)
                {
                    polygons = new Polygon[other.polygons.Length];
                }

                for(int p = 0; p < other.polygons.Length; p++)
                {
                    if(other.polygons[p].edgeIndices == null || other.polygons[p].edgeIndices.Length == 0)
                    {
                        continue;
                    }

                    int[] newEdges = new int[other.polygons[p].edgeIndices.Length];
                    Array.Copy(other.polygons[p].edgeIndices, newEdges, other.polygons[p].edgeIndices.Length);
                    polygons[p] = new Polygon(newEdges, other.polygons[p].texGenIndex);
                }
            }
            else
            {
                polygons = null;
            }

            valid      = other.valid;
            generation = other.generation;
        }

        /// <summary>Return a copy of this <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</summary>
        /// <returns>The copy of this <see cref="RealtimeCSG.Legacy.ControlMesh"/>.</returns>
        public ControlMesh Clone()
        {
            return new ControlMesh(this);
        }

        /// <summary>For an index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/>, return the vertex associated with it.</summary>
        /// <param name="halfEdgeIndex">The index to the <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A vertex that belongs to a <see cref="RealtimeCSG.Legacy.HalfEdge"/>. If the <paramref name="halfEdgeIndex"/> was invalid it returns a zero vertex.</returns>
        public Vector3 GetVertex(int halfEdgeIndex)
        {
            if(halfEdgeIndex < 0 || halfEdgeIndex >= edges.Length)
            {
                return MathConstants.zeroVector3;
            }

            short vertexIndex = edges[halfEdgeIndex].vertexIndex;

            if(vertexIndex < 0 || vertexIndex >= vertices.Length)
            {
                return MathConstants.zeroVector3;
            }

            return vertices[vertexIndex];
        }

        /// <summary>Returns the vertices for the given array of indices to <see cref="RealtimeCSG.Legacy.HalfEdge"/>s.</summary>
        /// <param name="halfEdgeIndices">Indices to <see cref="RealtimeCSG.Legacy.HalfEdge"/>s.</param>
        /// <returns>An array of vertices</returns>
        public Vector3[] GetVertices(int[] halfEdgeIndices)
        {
            Vector3[] verts = new Vector3[halfEdgeIndices.Length];

            for(int i = 0; i < halfEdgeIndices.Length; i++)
            {
                int halfEdgeIndex = halfEdgeIndices[i];

                if(halfEdgeIndex < 0 || halfEdgeIndex >= edges.Length)
                {
                    verts[i] = MathConstants.zeroVector3;

                    continue;
                }

                short vertexIndex = edges[halfEdgeIndex].vertexIndex;

                if(vertexIndex < 0 || vertexIndex >= vertices.Length)
                {
                    verts[i] = MathConstants.zeroVector3;

                    continue;
                }

                verts[i] = vertices[vertexIndex];
            }

            return verts;
        }

        /// <summary>Get the vertex that is associated with the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
        /// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
        /// <returns>A vertex</returns>
        public Vector3 GetVertex(ref HalfEdge halfEdge)
        {
            return vertices[halfEdge.vertexIndex];
        }

        /// <summary>Get the vertex index that is associated with the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A index to a vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public short GetVertexIndex(int halfEdgeIndex)
        {
            return edges[halfEdgeIndex].vertexIndex;
        }

        /// <summary>Get the vertex index that is associated with the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
        /// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
        /// <returns>A index to a vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public short GetVertexIndex(ref HalfEdge halfEdge)
        {
            return halfEdge.vertexIndex;
        }

        /// <summary>Get the vertex of the twin of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
        /// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
        /// <returns>A vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public Vector3 GetTwinEdgeVertex(ref HalfEdge halfEdge)
        {
            return vertices[edges[halfEdge.twinIndex].vertexIndex];
        }

        /// <summary>Get the vertex of the twin of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public Vector3 GetTwinEdgeVertex(int halfEdgeIndex)
        {
            return vertices[edges[edges[halfEdgeIndex].twinIndex].vertexIndex];
        }

        /// <summary>Get the index to the vertex of the twin of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
        /// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
        /// <returns>A index to a vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public short GetTwinEdgeVertexIndex(ref HalfEdge halfEdge)
        {
            return edges[halfEdge.twinIndex].vertexIndex;
        }

        /// <summary>Get the index to the vertex of the twin of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A index to a vertex in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public short GetTwinEdgeVertexIndex(int halfEdgeIndex)
        {
            return edges[edges[halfEdgeIndex].twinIndex].vertexIndex;
        }

        /// <summary>Get the index to twin <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/></summary>
        /// <param name="halfEdge">The <see cref="RealtimeCSG.Legacy.HalfEdge"/> to get a vertex for</param>
        /// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public int GetTwinEdgeIndex(ref HalfEdge halfEdge)
        {
            return halfEdge.twinIndex;
        }

        /// <summary>Get the index to twin <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public int GetTwinEdgeIndex(int halfEdgeIndex)
        {
            return edges[halfEdgeIndex].twinIndex;
        }

        /// <summary>Get the index to the polygon of the twin <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A index to a <see cref="RealtimeCSG.Legacy.Polygon"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public short GetTwinEdgePolygonIndex(int halfEdgeIndex)
        {
            return edges[edges[halfEdgeIndex].twinIndex].polygonIndex;
        }

        /// <summary>Get the index to the polygon of the given <see cref="RealtimeCSG.Legacy.HalfEdge"/> that is found using <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A index to a <see cref="RealtimeCSG.Legacy.Polygon"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public short GetEdgePolygonIndex(int halfEdgeIndex)
        {
            return edges[halfEdgeIndex].polygonIndex;
        }

        /// <summary>Get the next index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> around the vertex associated with the <see cref="RealtimeCSG.Legacy.HalfEdge"/> found with the given <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public int GetNextEdgeIndexAroundVertex(int halfEdgeIndex)
        {
            return GetTwinEdgeIndex(GetNextEdgeIndex(halfEdgeIndex));
        }

        /// <summary>Get the previous index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the <see cref="RealtimeCSG.Legacy.Polygon"/> associated with the <see cref="RealtimeCSG.Legacy.HalfEdge"/> found with the given <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public int GetPrevEdgeIndex(int halfEdgeIndex)
        {
            HalfEdge edge         = edges[halfEdgeIndex];
            short    polygonIndex = edge.polygonIndex;

            if(polygonIndex < 0 || polygonIndex >= polygons.Length)
            {
                return -1;
            }

            int[] edgeIndices = polygons[polygonIndex].edgeIndices;

            for(int i = 1; i < edgeIndices.Length; i++)
            {
                if(edgeIndices[i] == halfEdgeIndex)
                {
                    return edgeIndices[i - 1];
                }
            }

            return edgeIndices[edgeIndices.Length - 1];
        }

        /// <summary>Get the next index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> of the <see cref="RealtimeCSG.Legacy.Polygon"/> associated with the <see cref="RealtimeCSG.Legacy.HalfEdge"/> found with the given <paramref name="halfEdgeIndex"/></summary>
        /// <param name="halfEdgeIndex">Index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/></param>
        /// <returns>A index to a <see cref="RealtimeCSG.Legacy.HalfEdge"/> in this <see cref="RealtimeCSG.Legacy.ControlMesh"/></returns>
        public int GetNextEdgeIndex(int halfEdgeIndex)
        {
            HalfEdge edge         = edges[halfEdgeIndex];
            short    polygonIndex = edge.polygonIndex;

            if(polygonIndex < 0 || polygonIndex >= polygons.Length)
            {
                return -1;
            }

            int[] edgeIndices = polygons[polygonIndex].edgeIndices;

            for(int i = 0; i < edgeIndices.Length - 1; i++)
            {
                if(edgeIndices[i] == halfEdgeIndex)
                {
                    return edgeIndices[i + 1];
                }
            }

            return edgeIndices[0];
        }
    }
}

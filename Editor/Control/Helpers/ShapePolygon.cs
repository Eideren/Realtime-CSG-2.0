// * * * * * * * * * * * * * * * * * * * * * *
//  Description:
// 
// * * * * * * * * * * * * * * * * * * * * * *

using System;
using RealtimeCSG.Legacy;
using UnityEngine;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class ShapePolygon
    {
        // NOTE: an edge is defined as [vertex n, vertex (n+1)%vertices.length]
        public Vector3[] vertices;
        public TexGen[] edgeTexgens;

        public ShapePolygon()
        {
        }

        public ShapePolygon(Vector3[] vertices)
        {
            this.vertices = vertices;
        }
    }
}

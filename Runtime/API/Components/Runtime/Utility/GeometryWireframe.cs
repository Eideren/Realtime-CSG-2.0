using System;
using UnityEngine;

namespace RealtimeCSG
{
#if UNITY_EDITOR
    [Serializable]
    public sealed class GeometryWireframe
    {
        public Vector3[] vertices;
        public int[] visibleOuterLines;
        public int[] visibleInnerLines;
        public int[] visibleTriangles;
        public int[] invisibleOuterLines;
        public int[] invisibleInnerLines;
        public int[] invalidLines;
        public ulong outlineGeneration;
    }
#endif
}

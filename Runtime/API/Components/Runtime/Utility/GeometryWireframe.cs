using System;
using UnityEngine;

namespace RealtimeCSG
{
#if UNITY_EDITOR
    [Serializable]
    public sealed class GeometryWireframe
    {
        public Vector3[] vertices = null;
        public int[] visibleOuterLines = null;
        public int[] visibleInnerLines = null;
        public int[] visibleTriangles = null;
        public int[] invisibleOuterLines = null;
        public int[] invisibleInnerLines = null;
        public int[] invalidLines = null;
        public ulong outlineGeneration = 0;
    }
#endif
}

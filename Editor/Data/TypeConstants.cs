using System;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    public static class TypeConstants
    {
        public static readonly Type csgModelType = typeof(CSGModel);
        public static readonly Type csgOperationType = typeof(CSGOperation);
        public static readonly Type csgBrushType = typeof(CSGBrush);
        public static readonly Type csgNodeType = typeof(CSGNode);

        public static readonly Type generatedMeshInstanceType = typeof(GeneratedMeshInstance);

        public static readonly Type rigidbodyType = typeof(Rigidbody);
        public static readonly Type meshRendererType = typeof(MeshRenderer);
        public static readonly Type meshFilterType = typeof(MeshFilter);
        public static readonly Type meshColliderType = typeof(MeshCollider);
    }
}

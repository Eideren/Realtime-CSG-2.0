using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
    partial struct CSGTreeBrush
    {
        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateBrush(int userID, out int generatedNodeID);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern CSGTreeBrushFlags GetBrushFlags(int brushNodeID);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetBrushFlags(int brushNodeID, CSGTreeBrushFlags flags);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBrushOperationType(int brushNodeID);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetBrushOperationType(int brushNodeID, CSGOperationType operation);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBrushMeshID(int brushNodeID);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetBrushMeshID(int brushNodeID, int brushMeshID);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetNodeLocalTransformation(int brushNodeID, [Out] out Matrix4x4 brushToTreeSpace);

        private static bool SetBrushMesh(int brushNodeID, BrushMeshInstance brushMesh)
        {
            return SetBrushMeshID(brushNodeID, brushMesh.brushMeshID);
        }

        private static BrushMeshInstance GetBrushMesh(int brushNodeID)
        {
            return new BrushMeshInstance { brushMeshID = GetBrushMeshID(brushNodeID) };
        }

        private static Matrix4x4 GetNodeLocalTransformation(int brushNodeID)
        {
            return GetNodeLocalTransformation(brushNodeID, out Matrix4x4 result) ? result : Matrix4x4.identity;
        }

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetNodeLocalTransformation(int brushNodeID, [In] ref Matrix4x4 brushToTreeSpace);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetBrushBounds(int brushNodeID, ref AABB bounds);

        private static Bounds GetBrushBounds(int brushNodeID)
        {
            AABB aabb = new AABB();

            return GetBrushBounds(brushNodeID, ref aabb) ? new Bounds(aabb.Center, aabb.Size) : new Bounds();
        }
    }
}

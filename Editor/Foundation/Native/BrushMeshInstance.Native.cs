using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
    partial struct BrushMeshInstance
    {
        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CreateBrushMesh(int userID, int vertexCount, [In] IntPtr vertices, int halfEdgeCount, [In] IntPtr halfEdges, int polygonCount, [In] IntPtr polygons);

        private static int CreateBrushMesh(int userID, int vertexCount, Vector3[] vertices, int halfEdgeCount, BrushMesh.HalfEdge[] halfEdges, int polygonCount, BrushMesh.Polygon[] polygons)
        {
            GCHandle verticesHandle  = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            GCHandle halfEdgesHandle = GCHandle.Alloc(halfEdges, GCHandleType.Pinned);
            GCHandle polygonsHandle  = GCHandle.Alloc(polygons, GCHandleType.Pinned);

            int brushMeshID = CreateBrushMesh(userID, vertexCount, verticesHandle.AddrOfPinnedObject(), halfEdgeCount, halfEdgesHandle.AddrOfPinnedObject(), polygonCount, polygonsHandle.AddrOfPinnedObject());

            polygonsHandle.Free();
            halfEdgesHandle.Free();
            verticesHandle.Free();

            return brushMeshID;
        }

        private static int CreateBrushMesh(int userID, BrushMesh brushMesh)
        {
            if(brushMesh == null || brushMesh.vertices == null || brushMesh.halfEdges == null || brushMesh.polygons == null)
            {
                return 0;
            }

            int edgeCount = brushMesh.halfEdges.Length;

            if(edgeCount < 12)
            {
                return 0;
            }

            int polygonCount = brushMesh.polygons.Length;

            if(polygonCount <= 4)
            {
                return 0;
            }

            int vertexCount = brushMesh.vertices.Length;

            int result = CreateBrushMesh(userID, vertexCount, brushMesh.vertices, edgeCount, brushMesh.halfEdges, polygonCount, brushMesh.polygons);

            if(result <= 0)
            {
                result = 0;
            }

            return result;
        }

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBrushMeshUserID(int brushMeshIndex);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool UpdateBrushMesh(int brushMeshIndex, int vertexCount, [In] IntPtr vertices, int halfEdgeCount, [In] IntPtr halfEdges, int polygonCount, [In] IntPtr polygons);

        private static bool UpdateBrushMesh(int brushMeshIndex, int vertexCount, Vector3[] vertices, int halfEdgeCount, BrushMesh.HalfEdge[] halfEdges, int polygonCount, BrushMesh.Polygon[] polygons)
        {
            if(vertices == null || halfEdges == null || polygons == null)
            {
                return false;
            }

            if(vertexCount < 0 || halfEdgeCount < 0 || polygonCount < 0)
            {
                return false;
            }

            GCHandle verticesHandle  = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            GCHandle halfEdgesHandle = GCHandle.Alloc(halfEdges, GCHandleType.Pinned);
            GCHandle polygonsHandle  = GCHandle.Alloc(polygons, GCHandleType.Pinned);

            bool result = UpdateBrushMesh(brushMeshIndex, vertexCount, verticesHandle.AddrOfPinnedObject(), halfEdgeCount, halfEdgesHandle.AddrOfPinnedObject(), polygonCount, polygonsHandle.AddrOfPinnedObject());

            polygonsHandle.Free();
            halfEdgesHandle.Free();
            verticesHandle.Free();

            return result;
        }

        private static bool UpdateBrushMesh(int brushMeshIndex, BrushMesh brushMesh)
        {
            if(brushMeshIndex == 0 || brushMesh.vertices == null || brushMesh.halfEdges == null || brushMesh.polygons == null)
            {
                return false;
            }

            int edgeCount = brushMesh.halfEdges.Length;

            if(edgeCount < 12)
            {
                return false;
            }

            int polygonCount = brushMesh.polygons.Length;

            if(polygonCount < 5)
            {
                return false;
            }

            int vertexCount = brushMesh.vertices.Length;

            bool result = UpdateBrushMesh(brushMeshIndex, vertexCount, brushMesh.vertices, edgeCount, brushMesh.halfEdges, polygonCount, brushMesh.polygons);

            return result;
        }

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DestroyBrushMesh(int brushMeshIndex);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool IsBrushMeshIDValid(int brushMeshIndex);
    }
}

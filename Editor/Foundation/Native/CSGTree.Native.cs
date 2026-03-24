using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
    partial struct CSGTree
    {
        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateTree(int userID, out int generatedTreeNodeID);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetNumberOfBrushesInTree(int nodeID);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DoesTreeContainBrush(int nodeID, int brushID);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateMeshDescriptions(int treeNodeID, int meshTypeCount, [In] IntPtr meshTypes, VertexChannelFlags vertexChannelMask, [Out] out int meshDescriptionCount);

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetMeshDescriptions(int treeNodeID, int meshDescriptionCount, [Out] IntPtr meshDescriptions);

        private static GeneratedMeshDescription[] GetMeshDescriptions(int treeNodeID, MeshQuery[] meshTypes, VertexChannelFlags vertexChannelMask)
        {
            if(meshTypes == null)
            {
                throw new ArgumentNullException(nameof(meshTypes));
            }

            if(meshTypes.Length == 0)
            {
                return null;
            }

            GCHandle nativeMeshTypeHandle = GCHandle.Alloc(meshTypes, GCHandleType.Pinned);
            IntPtr   nativeMeshTypePtr    = nativeMeshTypeHandle.AddrOfPinnedObject();

            bool result = GenerateMeshDescriptions(treeNodeID, meshTypes.Length, nativeMeshTypePtr, vertexChannelMask, out int meshDescriptionCount);

            nativeMeshTypeHandle.Free();

            if(!result || meshDescriptionCount == 0)
            {
                return null;
            }

            GeneratedMeshDescription[] meshDescriptions       = new GeneratedMeshDescription[meshDescriptionCount];
            GCHandle                   meshDescriptionsHandle = GCHandle.Alloc(meshDescriptions, GCHandleType.Pinned);
            IntPtr                     meshDescriptionsPtr    = meshDescriptionsHandle.AddrOfPinnedObject();

            result = GetMeshDescriptions(treeNodeID, meshDescriptionCount, meshDescriptionsPtr);

            meshDescriptionsHandle.Free();

            if(!result || meshDescriptions[0].vertexCount <= 0 || meshDescriptions[0].indexCount <= 0)
            {
                return null;
            }

            return meshDescriptions;
        }

        [DllImport(CSGManager.NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetGeneratedMesh(int treeNodeID, int meshIndex, int subMeshIndex, int indexCount, [Out] IntPtr indices, int vertexCount, [Out] IntPtr positions, [Out] IntPtr tangents, [Out] IntPtr normals, [Out] IntPtr uvs, out Vector3 boundsCenter, out Vector3 boundsSize);

        private static GeneratedMeshContents GetGeneratedMesh(int treeNodeID, GeneratedMeshDescription meshDescription)
        {
            if(meshDescription.vertexCount <= 0 || meshDescription.indexCount <= 0)
            {
                Debug.LogWarning($"{nameof(CSGTree)} called with a {nameof(GeneratedMeshDescription)} that isn't valid");

                return null;
            }

            GeneratedMeshContents generatedMesh      = new GeneratedMeshContents();
            VertexChannelFlags    usedVertexChannels = meshDescription.meshQuery.UsedVertexChannels;

            int vertexCount  = meshDescription.vertexCount;
            int indexCount   = meshDescription.indexCount;
            int meshIndex    = meshDescription.meshQueryIndex;
            int subMeshIndex = meshDescription.subMeshQueryIndex;

            generatedMesh.description = meshDescription;

            // create our arrays on the managed side with the correct size
            generatedMesh.tangents = (usedVertexChannels & VertexChannelFlags.Tangent) != 0 ? new Vector4[vertexCount] : null;

            generatedMesh.normals = (usedVertexChannels & VertexChannelFlags.Normal) != 0 ? new Vector3[vertexCount] : null;

            generatedMesh.uv0       = (usedVertexChannels & VertexChannelFlags.UV0) != 0 ? new Vector2[vertexCount] : null;
            generatedMesh.positions = new Vector3[vertexCount];
            generatedMesh.indices   = new int[indexCount];

            GCHandle indicesHandle  = GCHandle.Alloc(generatedMesh.indices, GCHandleType.Pinned);
            GCHandle positionHandle = GCHandle.Alloc(generatedMesh.positions, GCHandleType.Pinned);
            GCHandle tangentHandle  = new GCHandle();
            GCHandle normalHandle   = new GCHandle();
            GCHandle uv0Handle      = new GCHandle();

            IntPtr indicesPtr  = indicesHandle.AddrOfPinnedObject();
            IntPtr positionPtr = positionHandle.AddrOfPinnedObject();
            IntPtr tangentPtr  = IntPtr.Zero;
            IntPtr normalPtr   = IntPtr.Zero;
            IntPtr uv0Ptr      = IntPtr.Zero;

            if(generatedMesh.tangents != null)
            {
                tangentHandle = GCHandle.Alloc(generatedMesh.tangents, GCHandleType.Pinned);
                tangentPtr    = tangentHandle.AddrOfPinnedObject();
            }

            if(generatedMesh.normals != null)
            {
                normalHandle = GCHandle.Alloc(generatedMesh.normals, GCHandleType.Pinned);
                normalPtr    = normalHandle.AddrOfPinnedObject();
            }

            if(generatedMesh.uv0 != null)
            {
                uv0Handle = GCHandle.Alloc(generatedMesh.uv0, GCHandleType.Pinned);
                uv0Ptr    = uv0Handle.AddrOfPinnedObject();
            }

            bool result = GetGeneratedMesh(treeNodeID, meshIndex, subMeshIndex, indexCount, indicesPtr, vertexCount, positionPtr, tangentPtr, normalPtr, uv0Ptr, out Vector3 boundsCenter, out Vector3 boundsSize);

            if(generatedMesh.uv0 != null)
            {
                uv0Handle.Free();
            }

            if(generatedMesh.normals != null)
            {
                normalHandle.Free();
            }

            if(generatedMesh.tangents != null)
            {
                tangentHandle.Free();
            }

            positionHandle.Free();
            indicesHandle.Free();

            if(!result)
            {
                return null;
            }

            generatedMesh.bounds = new Bounds(boundsCenter, boundsSize);

            return generatedMesh;
        }
    }
}

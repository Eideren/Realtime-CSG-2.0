using System;
using System.Runtime.InteropServices;

namespace RealtimeCSG.Foundation
{
    sealed partial class CSGManager
    {
        internal const string NATIVE_PLUGIN_NAME = "RealtimeCSG[" + VersionManager.PLUGIN_DLL_VERSION + "]";

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool HasBeenCompiledInDebugMode();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ClearDirty(int nodeID);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ClearAllNodes();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool UpdateAllTreeMeshes();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern void RebuildAll();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetNodeCount();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBrushCount();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBranchCount();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetTreeCount();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBrushMeshCount();

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetAllTreeNodes(int nodeCount, IntPtr allNodeIDs);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetAllTrees(int nodeCount, IntPtr allNodeIDs);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetAllBrushMeshInstances(int nodeCount, IntPtr allNodeIDs);

        [DllImport(NATIVE_PLUGIN_NAME, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DestroyNodes(int nodeCount, IntPtr nodeIDs);

        private static bool DestroyNodes(int nodeCount, CSGTreeNode[] nodeIDs)
        {
            if(nodeIDs == null)
            {
                return false;
            }

            GCHandle nodeIDsHandle = GCHandle.Alloc(nodeIDs, GCHandleType.Pinned);
            IntPtr   nodeIDsPtr    = nodeIDsHandle.AddrOfPinnedObject();
            bool     result        = DestroyNodes(nodeCount, nodeIDsPtr);
            nodeIDsHandle.Free();

            return result;
        }

        private static CSGTreeNode[] GetAllTreeNodes()
        {
            int           nodeCount      = GetNodeCount();
            CSGTreeNode[] allTreeNodeIDs = new CSGTreeNode[nodeCount];

            if(nodeCount == 0)
            {
                return allTreeNodeIDs;
            }

            GCHandle allNodeIDsHandle = GCHandle.Alloc(allTreeNodeIDs, GCHandleType.Pinned);
            IntPtr   allNodeIDsPtr    = allNodeIDsHandle.AddrOfPinnedObject();
            GetAllTreeNodes(nodeCount, allNodeIDsPtr);
            allNodeIDsHandle.Free();

            return allTreeNodeIDs;
        }

        private static CSGTree[] GetAllTrees()
        {
            int       nodeCount = GetTreeCount();
            CSGTree[] allTrees  = new CSGTree[nodeCount];

            if(nodeCount == 0)
            {
                return allTrees;
            }

            GCHandle allTreeIDsHandle = GCHandle.Alloc(allTrees, GCHandleType.Pinned);
            IntPtr   allTreeIDsPtr    = allTreeIDsHandle.AddrOfPinnedObject();
            GetAllTrees(nodeCount, allTreeIDsPtr);
            allTreeIDsHandle.Free();

            return allTrees;
        }

        private static BrushMeshInstance[] GetAllBrushMeshInstances()
        {
            int                 nodeCount    = GetBrushMeshCount();
            BrushMeshInstance[] allInstances = new BrushMeshInstance[nodeCount];

            if(nodeCount == 0)
            {
                return allInstances;
            }

            GCHandle allInstancesIDsHandle = GCHandle.Alloc(allInstances, GCHandleType.Pinned);
            IntPtr   allInstancesIDsPtr    = allInstancesIDsHandle.AddrOfPinnedObject();
            GetAllBrushMeshInstances(nodeCount, allInstancesIDsPtr);
            allInstancesIDsHandle.Free();

            return allInstances;
        }
    }
}

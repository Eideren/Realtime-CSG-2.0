using System.Runtime.InteropServices;

namespace RealtimeCSG.Foundation
{
    partial struct CSGTreeBranch
    {
        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GenerateBranch(int userID, out int generatedOperationNodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetBranchOperationType(int branchNodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetBranchOperationType(int branchNodeID, CSGOperationType operation);
    }
}

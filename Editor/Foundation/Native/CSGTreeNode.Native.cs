using InternalRealtimeCSG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
    partial struct CSGTreeNode
    {
        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool ClearDirty(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool IsNodeDirty(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool SetDirty(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern CSGNodeType GetTypeOfNode(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool IsNodeIDValid(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetUserIDOfNode(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetParentOfNode(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetTreeOfNode(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool SetChildNodes(int nodeID, int childCount, IntPtr childrenNodeIDs);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetChildNodeCount(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool GetChildNodes(int nodeID, int childCount, IntPtr children, int arrayIndex);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RemoveChildNode(int nodeID, int childNodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool AddChildNode(int nodeID, int childNodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool ClearChildNodes(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int GetChildNodeAtIndex(int nodeID, int index);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool DestroyNode(int nodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DestroyNodes(int nodeCount, IntPtr nodeIDs);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RemoveChildNodeAt(int nodeID, int index);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool InsertChildNode(int nodeID, int index, int childNodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int IndexOfChildNode(int nodeID, int childNodeID);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool RemoveChildNodeRange(int nodeID, int index, int count);

        [DllImport(CSGManager.NativePluginName, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool InsertChildNodeRange(int nodeID, int index, int count, IntPtr nodes);

        internal static bool SetChildNodes(int nodeID, CSGTreeNode[] children)
        {
            if(children == null)
            {
                return false;
            }

            GCHandle childrenHandle = GCHandle.Alloc(children, GCHandleType.Pinned);
            IntPtr   childrenPtr    = childrenHandle.AddrOfPinnedObject();
            bool     result         = SetChildNodes(nodeID, children.Length, childrenPtr);
            childrenHandle.Free();

            return result;
        }

        internal static CSGTreeNode[] GetChildNodes(int nodeID)
        {
            int           childCount = GetChildNodeCount(nodeID);
            CSGTreeNode[] children   = new CSGTreeNode[childCount];

            if(childCount == 0)
            {
                return children;
            }

            GCHandle childrenHandle = GCHandle.Alloc(children, GCHandleType.Pinned);
            IntPtr   childrenPtr    = childrenHandle.AddrOfPinnedObject();
            GetChildNodes(nodeID, childCount, childrenPtr, 0);
            childrenHandle.Free();

            return children;
        }

        internal static int CopyTo(int nodeID, CSGTreeNode[] children, int arrayIndex)
        {
            if(children == null)
            {
                throw new ArgumentNullException("children");
            }

            int childCount = GetChildNodeCount(nodeID);

            if(childCount <= 0)
            {
                return 0;
            }

            if(children.Length + arrayIndex < childCount)
            {
                throw new ArgumentException(
                    string.Format(
                        "The array does not have enough elements, its length is {0} and needs at least {1}"
                      , children.Length, childCount
                    ), "children"
                );
            }

            return CopyToUnsafe(nodeID, childCount, children, arrayIndex);
        }

        internal static int CopyToUnsafe(int nodeID, int childCount, CSGTreeNode[] children, int arrayIndex)
        {
            GCHandle childrenHandle = GCHandle.Alloc(children, GCHandleType.Pinned);
            IntPtr   childrenPtr    = childrenHandle.AddrOfPinnedObject();
            GetChildNodes(nodeID, childCount, childrenPtr, arrayIndex);
            childrenHandle.Free();

            return childCount;
        }

        internal static bool InsertChildNodeRange(int nodeID, int index, CSGTreeNode[] children)
        {
            if(children == null)
            {
                return false;
            }

            GCHandle childrenHandle = GCHandle.Alloc(children, GCHandleType.Pinned);
            IntPtr   childrenPtr    = childrenHandle.AddrOfPinnedObject();
            bool     result         = InsertChildNodeRange(nodeID, index, children.Length, childrenPtr);
            childrenHandle.Free();

            return result;
        }
    }
}

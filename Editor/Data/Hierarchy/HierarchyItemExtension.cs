using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal static class HierarchyItemExtension
    {
        internal static bool FindSiblingIndex(this HierarchyItem self, int siblingIndex, int searchTransformID, out int index)
        {
            if(self.childNodes == null || self.childNodes.Length == 0)
            {
                index = 0;

                return false;
            }

            int last             = self.childNodes.Length - 1;
            int currentLoopCount = HierarchyItem.CurrentLoopCount;

            if(self.childNodes[last].lastLoopCount != currentLoopCount)
            {
                if(self.childNodes[last].transform != null && self.childNodes[last].transform)
                {
                    self.childNodes[last].cachedTransformSiblingIndex =
                        self.childNodes[last].transform.GetSiblingIndex();
                }
                else
                {
                    self.childNodes[last].cachedTransformSiblingIndex = -1;
                }

                self.childNodes[last].lastLoopCount = currentLoopCount;
            }

            if(self.childNodes[last].cachedTransformSiblingIndex < siblingIndex)
            {
                index = self.childNodes.Length;

                return false;
            }

            // continue searching while [imin,imax] is not empty
            int imin = 0;
            int imax = last;

            while(imin <= imax)
            {
                // calculate the midpoint for roughly equal partition
                int imid = (imin + imax) / 2;

                if(self.childNodes[imid].lastLoopCount != currentLoopCount)
                {
                    if(self.childNodes[imid].transform != null && self.childNodes[imid].transform)
                    {
                        self.childNodes[imid].cachedTransformSiblingIndex = self.childNodes[imid].transform.GetSiblingIndex();
                    }
                    else
                    {
                        self.childNodes[imid].cachedTransformSiblingIndex = -1;
                    }

                    self.childNodes[imid].lastLoopCount = currentLoopCount;
                }

                int midKey2 = self.childNodes[imid].cachedTransformSiblingIndex;

                // determine which subarray to search
                if(midKey2 < siblingIndex)
                {
                    // change min index to search upper subarray
                    imin = imid + 1;
                }
                else
                {
                    if(midKey2 == siblingIndex)
                    {
                        // key found at index imid

                        index = imid;

                        return searchTransformID == self.childNodes[imid].transformID;
                    }

                    if(imid > 0)
                    {
                        if(self.childNodes[imid - 1].lastLoopCount != currentLoopCount)
                        {
                            if(self.childNodes[imid - 1].transform != null && self.childNodes[imid - 1].transform)
                            {
                                self.childNodes[imid - 1].cachedTransformSiblingIndex = self.childNodes[imid - 1].transform.GetSiblingIndex();
                            }
                            else
                            {
                                self.childNodes[imid - 1].cachedTransformSiblingIndex = -1;
                            }

                            self.childNodes[imid - 1].lastLoopCount = currentLoopCount;
                        }

                        int midKey1 = self.childNodes[imid - 1].cachedTransformSiblingIndex;

                        if(midKey1 < siblingIndex)
                        {
                            // key found at index imid
                            index = imid;

                            return searchTransformID == self.childNodes[imid].transformID;
                        }
                    }

                    // change max index to search lower subarray
                    imax = imid - 1;
                }
            }

            index = 0;

            return false;
        }

        internal static bool FindSiblingIndex(this HierarchyItem self, HierarchyItem item, out int index)
        {
            if(self.childNodes == null || self.childNodes.Length == 0)
            {
                index = 0;

                return false;
            }

            for(int i = 0; i < self.childNodes.Length; i++)
            {
                if(item != self.childNodes[i])
                {
                    continue;
                }

                index = i;

                return true;
            }

            index = 0;

            return false;
        }

        internal static bool AddChildItem(this HierarchyItem self, HierarchyItem item)
        {
            // The transform is already in the array?
            if(self.FindSiblingIndex(item, out int index))
            {
                return false;
            }

            int currentLoopCount = HierarchyItem.CurrentLoopCount;

            if(item.lastLoopCount != currentLoopCount)
            {
                item.cachedTransformSiblingIndex = item.transform.GetSiblingIndex();
                item.lastLoopCount               = currentLoopCount;
            }

            if(self.FindSiblingIndex(item.cachedTransformSiblingIndex, item.transformID, out index))
            {
                return false;
            }

            // make sure item is added in the correct position within the array
            UnityEditor.ArrayUtility.Insert(ref self.childNodes, index, item);
            item.siblingIndex = index;

            if(self is ParentNodeData parentData)
            {
                parentData.childrenModified = true;
            }

            Debug.Assert(self.childNodes[index] == item);

            return true;
        }

        internal static bool RemoveChildItem(this HierarchyItem self, HierarchyItem item)
        {
            // The transform is not in the array?
            if(!self.FindSiblingIndex(item, out int index))
            {
                return false;
            }

            // make sure item is removed from the array
            UnityEditor.ArrayUtility.RemoveAt(ref self.childNodes, index);

            return true;
        }

        public static void Init(this HierarchyItem self, CSGNode node, int nodeID)
        {
            self.transform   = node.transform;
            self.transformID = node.transform.GetIntegerID();
            self.nodeID      = nodeID;
        }
    }
}

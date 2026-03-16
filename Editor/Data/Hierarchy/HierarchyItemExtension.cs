using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal static class HierarchyItemExtension
    {
        internal static bool FindSiblingIndex(
            this HierarchyItem self, Transform searchTransform, int siblingIndex, int searchTransformID, out int index
        )
        {
            if(self.ChildNodes == null ||
               self.ChildNodes.Length == 0)
            {
                index = 0;

                return false;
            }

            int checkIndex       = siblingIndex;
            int last             = self.ChildNodes.Length - 1;
            int currentLoopCount = HierarchyItem.CurrentLoopCount;

            if(self.ChildNodes[last].LastLoopCount != currentLoopCount)
            {
                if(self.ChildNodes[last].Transform != null && self.ChildNodes[last].Transform)
                {
                    self.ChildNodes[last].CachedTransformSiblingIndex =
                        self.ChildNodes[last].Transform.GetSiblingIndex();
                }
                else
                {
                    self.ChildNodes[last].CachedTransformSiblingIndex = -1;
                }

                self.ChildNodes[last].LastLoopCount = currentLoopCount;
            }

            if(self.ChildNodes[last].CachedTransformSiblingIndex < checkIndex)
            {
                index = self.ChildNodes.Length;

                return false;
            }

            // continue searching while [imin,imax] is not empty
            int imin = 0;
            int imax = last;

            while(imin <= imax)
            {
                // calculate the midpoint for roughly equal partition
                int imid = (imin + imax) / 2;

                if(self.ChildNodes[imid].LastLoopCount != currentLoopCount)
                {
                    if(self.ChildNodes[imid].Transform != null && self.ChildNodes[imid].Transform)
                    {
                        self.ChildNodes[imid].CachedTransformSiblingIndex =
                            self.ChildNodes[imid].Transform.GetSiblingIndex();
                    }
                    else
                    {
                        self.ChildNodes[imid].CachedTransformSiblingIndex = -1;
                    }

                    self.ChildNodes[imid].LastLoopCount = currentLoopCount;
                }

                int midKey2 = self.ChildNodes[imid].CachedTransformSiblingIndex;

                // determine which subarray to search
                if(midKey2 < checkIndex)
                {
                    // change min index to search upper subarray
                    imin = imid + 1;
                }
                else
                {
                    if(midKey2 == checkIndex)
                    {
                        // key found at index imid

                        index = imid;

                        return searchTransformID == self.ChildNodes[imid].TransformID;
                    }

                    if(imid > 0)
                    {
                        if(self.ChildNodes[imid - 1].LastLoopCount != currentLoopCount)
                        {
                            if(self.ChildNodes[imid - 1].Transform != null && self.ChildNodes[imid - 1].Transform)
                            {
                                self.ChildNodes[imid - 1].CachedTransformSiblingIndex =
                                    self.ChildNodes[imid - 1].Transform.GetSiblingIndex();
                            }
                            else
                            {
                                self.ChildNodes[imid - 1].CachedTransformSiblingIndex = -1;
                            }

                            self.ChildNodes[imid - 1].LastLoopCount = currentLoopCount;
                        }

                        int midKey1 = self.ChildNodes[imid - 1].CachedTransformSiblingIndex;

                        if(midKey1 < checkIndex)
                        {
                            // key found at index imid
                            index = imid;

                            return searchTransformID == self.ChildNodes[imid].TransformID;
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
            if(self.ChildNodes == null ||
               self.ChildNodes.Length == 0)
            {
                index = 0;

                return false;
            }

            for(int i = 0; i < self.ChildNodes.Length; i++)
            {
                if(item != self.ChildNodes[i])
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
            int index;

            if(self.FindSiblingIndex(item, out index))
                // The transform is already in the array?
            {
                return false;
            }

            int currentLoopCount = HierarchyItem.CurrentLoopCount;

            if(item.LastLoopCount != currentLoopCount)
            {
                item.CachedTransformSiblingIndex = item.Transform.GetSiblingIndex();
                item.LastLoopCount               = currentLoopCount;
            }

            if(self.FindSiblingIndex(item.Transform, item.CachedTransformSiblingIndex, item.TransformID, out index))
            {
                return false;
            }

            // make sure item is added in the correct position within the array
            UnityEditor.ArrayUtility.Insert(ref self.ChildNodes, index, item);
            item.SiblingIndex = index;

            /*
            bool childrenModified = false;
            for (int i = index + 1; i < ChildNodes.Length; i++)
            {
                if (ChildNodes[i].SiblingIndex != i)
                {
                    ChildNodes[i].SiblingIndex = i;
                    childrenModified = true;
                }
            }
            if (childrenModified)*/
            {
                ParentNodeData parentData = self as ParentNodeData;

                if(parentData != null)
                {
                    parentData.ChildrenModified = true;
                }
            }

            Debug.Assert(self.ChildNodes[index] == item);

            return true;
        }

        internal static bool RemoveChildItem(this HierarchyItem self, HierarchyItem item)
        {
            int index;

            if(!self.FindSiblingIndex(item, out index))
                // The transform is not in the array?
            {
                return false;
            }

            // make sure item is removed from the array
            UnityEditor.ArrayUtility.RemoveAt(ref self.ChildNodes, index);

            //item.siblingIndex = -1;
            return true;
        }

        public static IEnumerable<HierarchyItem> IterateChildrenDeep(this HierarchyItem self)
        {
            for(int i = 0; i < self.ChildNodes.Length; i++)
            {
                HierarchyItem childNode = self.ChildNodes[i];

                yield return childNode;

                if(childNode.ChildNodes.Length == 0)
                {
                    continue;
                }

                foreach(HierarchyItem item in childNode.IterateChildrenDeep())
                {
                    yield return item;
                }
            }
        }

        public static void Init(this HierarchyItem self, CSGNode node, int nodeID)
        {
            self.Transform   = node.transform;
            self.TransformID = node.transform.GetInstanceID();
            self.NodeID      = nodeID;
        }
    }
}

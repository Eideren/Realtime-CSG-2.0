using System.Collections.Generic;
using UnityEngine;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal static class ParentNodeDataExtensions
    {
        // returns true on success
        internal static bool RemoveNode(HierarchyItem node, ParentNodeData top)
        {
            if(node.Parent == null)
            {
                return false;
            }

            // on each level remove self from parent. 
            // if it doesn't have any childNodes left, remove itself from it's parent
            HierarchyItem iterator = node;

            do
            {
                HierarchyItem parent = iterator.Parent;

                if(parent == null)
                {
                    break;
                }

                parent.RemoveChildItem(iterator);

                if(parent == top)
                {
                    break;
                }

                iterator = parent;
            }
            while(iterator.ChildNodes.Length == 0);

            node.Parent          = null;
            top.ChildrenModified = true;

            return true;
        }

        // returns true when modified
        internal static bool UpdateNodePosition(HierarchyItem node, ParentNodeData top)
        {
            // on each level, compare sibling position to nodes before and after it.
            // if it's different, remove self and find new position in array.
            // continue to next parent.
            HierarchyItem iteratorParent = node.Parent;

            if(iteratorParent == null)
            {
                AddNode(node, top);
                top.ChildrenModified = true;

                return true;
            }

            int currentLoopCount = HierarchyItem.CurrentLoopCount;

            HierarchyItem iterator = node;

            while(iteratorParent != null)
            {
                int iteratorTransformID = iterator.TransformID;

                if(iteratorTransformID == 0)
                {
                    Debug.LogWarning("iterator_transform == null");
                }
                else if(iterator.LastLoopCount != currentLoopCount)
                {
                    Transform iteratorTransform = iterator.Transform;

                    int iteratorParentTransformID =
                        iteratorTransform.parent == null ? 0 : iteratorTransform.parent.GetInstanceID();

                    // Compare the unity parent transform to the stored parent transform
                    if(iteratorParent.TransformID != iteratorParentTransformID)
                    {
                        int defaultCSGInstanceID = 0;

                        CSGModel defaultCSGModel =
                            InternalCSGModelManager.GetDefaultCSGModelForObject(iteratorTransform);

                        if(defaultCSGModel != null &&
                           defaultCSGModel.transform != null)
                        {
                            defaultCSGInstanceID = defaultCSGModel.transform.GetInstanceID();
                        }

                        if(defaultCSGInstanceID == 0 || defaultCSGInstanceID != iteratorParent.TransformID)
                        {
                            RemoveNode(node, top);
                            AddNode(node, top);
                            top.ChildrenModified = true;

                            return true;
                        }
                    }

                    if(iterator.LastLoopCount != currentLoopCount)
                    {
                        iterator.CachedTransformSiblingIndex = iteratorTransform.GetSiblingIndex();
                        iterator.LastLoopCount               = currentLoopCount;
                    }

                    // Does the child even exist in the parent transform?
                    int iteratorChildIndex;

                    if(!iteratorParent.FindSiblingIndex(
                           iteratorTransform, iterator.CachedTransformSiblingIndex, iteratorTransformID
                         , out iteratorChildIndex
                       ))
                    {
                        RemoveNode(node, top);
                        AddNode(node, top);
                        top.ChildrenModified = true;

                        return true;
                    }

                    // See if the position of the child has changed ..
                    if(iteratorChildIndex != iterator.SiblingIndex)
                    {
                        iterator.SiblingIndex = iteratorChildIndex;
                        RemoveNode(node, top);       //
                        AddNode(node, top);          //
                        top.ChildrenModified = true; //

                        return true;
                    }

                    // Compare the child index to the one before and after it ..
                    HierarchyItem[] iteratorParentChildNodes = iteratorParent.ChildNodes;
                    int             iteratorSiblingIndex     = iteratorTransform.GetSiblingIndex();

                    if(iteratorChildIndex > 0)
                    {
                        Transform prevTransform = iteratorParentChildNodes[iteratorChildIndex - 1].Transform;

                        if(!prevTransform)
                        {
                            return false;
                        }

                        int iteratorPrevSiblingIndex = prevTransform.GetSiblingIndex();

                        if(iteratorPrevSiblingIndex >= iteratorSiblingIndex)
                        {
                            RemoveNode(node, top);
                            AddNode(node, top);
                            top.ChildrenModified = true;

                            return true;
                        }
                    }
                    else if(iteratorChildIndex < iteratorParentChildNodes.Length - 1)
                    {
                        Transform nextTransform = iteratorParentChildNodes[iteratorChildIndex + 1].Transform;

                        if(!nextTransform)
                        {
                            return false;
                        }

                        int iteratorNextSiblingIndex = nextTransform.GetSiblingIndex();

                        if(iteratorNextSiblingIndex <= iteratorSiblingIndex)
                        {
                            RemoveNode(node, top);
                            AddNode(node, top);
                            top.ChildrenModified = true;

                            return true;
                        }
                    }
                }

                iterator       = iteratorParent;
                iteratorParent = iterator.Parent;
            }

            return false;
        }

        // returns true on success
        internal static bool AddNode(HierarchyItem node, ParentNodeData top)
        {
            if(node.Parent != null)
            {
                return false;
            }

            if(!top.Transform)
            {
                return false;
            }

            List<Transform> ancestors = new List<Transform>();

            Transform leafTransform = node.Transform;

            if(leafTransform == null)
            {
                return false;
            }

            Transform iterator = leafTransform.parent;

            while(iterator != null &&
                  iterator != top.Transform)
            {
                ancestors.Add(iterator);
                iterator = iterator.parent;
            }

            CSGModel defaultModel = InternalCSGModelManager.GetDefaultCSGModelForObject(iterator);

            if(!defaultModel)
            {
                return false;
            }

            Transform defaultModelTransform = defaultModel.transform;

            if(iterator == null || top.Transform == null ||
               top.Transform == defaultModelTransform)
            {
                iterator = defaultModelTransform;
            }

            if(iterator == null)
            {
                node.Reset();
                top.Reset();

                return false;
            }
#if !UNITY_2018_3_OR_NEWER
			// TODO: figure out how to check this, yet still support prefab isolation mode
			if (iterator != top.Transform)
			{
				return false;
			}
#endif

            //var currentLoopCount = CurrentLoopCount;

            HierarchyItem lastParent    = top;
            int           ancestorDepth = ancestors.Count - 1;

            while(ancestorDepth >= 0)
            {
                Transform ancestor = ancestors[ancestorDepth];
                int       childIndex;

                if(!lastParent.FindSiblingIndex(
                       ancestor, ancestor.GetSiblingIndex(), ancestor.GetInstanceID(), out childIndex
                   ))
                {
                    break;
                }

                lastParent = lastParent.ChildNodes[childIndex];
                ancestorDepth--;
            }

            while(ancestorDepth >= 0)
            {
                HierarchyItem newAncestor = new HierarchyItem();
                newAncestor.Transform   = ancestors[ancestorDepth];
                newAncestor.TransformID = newAncestor.Transform.GetInstanceID();
                newAncestor.Parent      = lastParent;

                if(!lastParent.AddChildItem(newAncestor))
                {
                    return false;
                }

                lastParent = newAncestor;
                ancestorDepth--;
            }

            node.Parent          = lastParent;
            top.ChildrenModified = true;

            return lastParent.AddChildItem(node);
        }

        public static bool AddNode(this ParentNodeData parentNodeData, HierarchyItem hierarchyItem)
        {
            return AddNode(hierarchyItem, parentNodeData);
        }

        public static bool RemoveNode(this ParentNodeData parentNodeData, HierarchyItem hierarchyItem)
        {
            return RemoveNode(hierarchyItem, parentNodeData);
        }
    }
}

using System;
using RealtimeCSG.Components;
using UnityEngine;
using UnityEngine.Serialization;

namespace RealtimeCSG
{
#if UNITY_EDITOR
    [Serializable]
    public class HierarchyItem
    {
        public bool transformInitialized;

        public Transform transform;
        public int transformID;
        public HierarchyItem parent;

        public int prevSiblingIndex = -1;
        public int siblingIndex = -1;

        public int nodeID = CSGNode.INVALID_NODE_ID;
        public HierarchyItem[] childNodes = Array.Empty<HierarchyItem>();
        public int lastLoopCount = -1;

        [FormerlySerializedAs("CachedTransformSiblingIndex")]
        public int cachedTransformSiblingIndex;

        public static int CurrentLoopCount { get; set; }

        public virtual void Reset()
        {
            transform        = null;
            transformID      = 0;
            parent           = null;
            prevSiblingIndex = -1;
            siblingIndex     = -1;
            nodeID           = CSGNode.INVALID_NODE_ID;
            childNodes       = Array.Empty<HierarchyItem>();

            lastLoopCount               = -1;
            cachedTransformSiblingIndex = 0;
        }
    }
#endif
}

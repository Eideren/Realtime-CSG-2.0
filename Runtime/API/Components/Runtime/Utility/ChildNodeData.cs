using System;
using RealtimeCSG.Components;
using UnityEngine;

namespace RealtimeCSG
{
#if UNITY_EDITOR
    [Serializable]
    public sealed class ChildNodeData
    {
        // this allows us to help detect when the operation has been modified in the hierarchy
        public CSGOperation parent;
        public CSGModel model;
        public Transform modelTransform;
        public ParentNodeData ownerParentData; // link to parents' parentData

        public int ParentNodeID
        {
            get => parent != null ? parent.operationNodeID : CSGNode.INVALID_NODE_ID;
        }

        public int ModelNodeID
        {
            get => model != null ? model.modelNodeID : CSGNode.INVALID_NODE_ID;
        }

        public void Reset()
        {
            parent          = null;
            model           = null;
            ownerParentData = null;
            modelTransform  = null;
        }
    }
#endif
}

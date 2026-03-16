using System;
using UnityEngine;

namespace RealtimeCSG
{
#if UNITY_EDITOR
    [Serializable]
    public sealed class ChildNodeData
    {
        // this allows us to help detect when the operation has been modified in the hierarchy
        public Components.CSGOperation Parent = null;
        public Components.CSGModel Model = null;
        public Transform ModelTransform = null;
        public ParentNodeData OwnerParentData = null; // link to parents' parentData

        public int ParentNodeID
        {
            get => Parent != null ? Parent.operationNodeID : Components.CSGNode.InvalidNodeID;
        }

        public int ModelNodeID
        {
            get => Model != null ? Model.modelNodeID : Components.CSGNode.InvalidNodeID;
        }

        public void Reset()
        {
            Parent          = null;
            Model           = null;
            OwnerParentData = null;
            ModelTransform  = null;
        }
    }
#endif
}

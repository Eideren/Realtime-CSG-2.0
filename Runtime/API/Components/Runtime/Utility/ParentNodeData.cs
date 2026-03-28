using System;

namespace RealtimeCSG
{
#if UNITY_EDITOR
    [Serializable]
    public sealed class ParentNodeData : HierarchyItem
    {
        public bool childrenModified = true;

        public override void Reset()
        {
            base.Reset();
            childrenModified = true;
        }
    }
#endif
}

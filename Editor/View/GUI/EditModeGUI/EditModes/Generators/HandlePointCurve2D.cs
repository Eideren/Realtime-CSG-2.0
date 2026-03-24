// * * * * * * * * * * * * * * * * * * * * * *
//  Description:
// 
// * * * * * * * * * * * * * * * * * * * * * *

using System;

namespace RealtimeCSG
{
    [Serializable]
    internal struct HandlePointCurve2D
    {
        [NonSerialized]
        public int id;

        public SelectState state;
        public bool tangent;
    }
}

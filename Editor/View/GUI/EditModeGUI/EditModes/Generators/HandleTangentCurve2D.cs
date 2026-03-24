// * * * * * * * * * * * * * * * * * * * * * *
//  Description:
// 
// * * * * * * * * * * * * * * * * * * * * * *

using System;
using InternalRealtimeCSG;

namespace RealtimeCSG
{
    [Serializable]
    internal struct HandleTangentCurve2D
    {
        [NonSerialized]
        public int id;

        public SelectState state;
        public HandleConstraints constraint;
    }
}

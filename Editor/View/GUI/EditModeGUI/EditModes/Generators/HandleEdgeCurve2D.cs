// * * * * * * * * * * * * * * * * * * * * * *
//  Description:
// 
// * * * * * * * * * * * * * * * * * * * * * *

using System;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    [Serializable]
    internal struct HandleEdgeCurve2D
    {
        [NonSerialized]
        public int id;

        public SelectState state;
        public TexGen texgen;
    }
}

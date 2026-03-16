using UnityEngine;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal interface IShapeSettings
    {
        void CalculatePlane(ref CSGPlane plane);
        Vector3 GetCenter(CSGPlane plane);
        AABB CalculateBounds(Quaternion rotation, Vector3 gridTangent, Vector3 gridBinormal);
    }
}

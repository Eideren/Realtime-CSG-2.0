using UnityEngine;

namespace InternalRealtimeCSG
{
    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class LegacyGeneratedMeshContainer : MonoBehaviour
    {
        private void Awake()
        {
            DestroyImmediate(gameObject);
        }
    }
}

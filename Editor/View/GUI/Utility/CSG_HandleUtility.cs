using UnityEditor;
using UnityEngine;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal static class CSG_HandleUtility
    {
        public static float GetHandleSize(Vector3 position)
        {
            return HandleUtility.GetHandleSize(position) / 20.0f;
            //Mathf.Max(0.01f, );
        }

        public static CSGPlane GetNearPlane(Camera camera)
        {
            Transform cameraTransform = camera.transform;
            Vector3   normal          = cameraTransform.forward;
            Vector3   pos             = cameraTransform.position + (camera.nearClipPlane + 0.01f) * normal;

            return new CSGPlane(normal, pos);
        }
    }
}

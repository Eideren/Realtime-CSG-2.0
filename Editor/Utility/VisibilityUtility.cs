using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    public sealed class CSGVisibilityUtility
    {
        public static void SetGameObjectVisibility(GameObject gameObject, bool visible)
        {
            if(SceneVisibilityManager.instance.IsHidden(gameObject) == visible)
            {
                if(visible)
                {
                    SceneVisibilityManager.instance.Show(gameObject, true);
                }
                else
                {
                    SceneVisibilityManager.instance.Hide(gameObject, true);
                }
            }
        }
    }
}

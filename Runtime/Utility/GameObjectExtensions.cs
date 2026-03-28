using UnityEngine;

public static class GameObjectExtensions
{
    private const string UNABLE_TO_DELETE = "<unable to delete>";

    public static void Destroy(Object obj)
    {
        if(!obj)
        {
            return;
        }

        Object.DestroyImmediate(obj);
    }

    public static void Destroy(GameObject gameObject)
    {
        if(!gameObject)
        {
            return;
        }

        // Cannot destroy gameObjects when certain hideflags are set
        if(!TryDestroy(gameObject))
        {
            // Work-around for nested prefab instance issues ..
            if(gameObject.activeSelf && gameObject.name != UNABLE_TO_DELETE)
            {
                gameObject.hideFlags = HideFlags.DontSaveInBuild;
                gameObject.SetActive(false);
                SanitizeGameObject(gameObject);
                gameObject.name = UNABLE_TO_DELETE;
            }
        }
    }

    public static void SanitizeGameObject(GameObject gameObject)
    {
        {
            MonoBehaviour[] childComponents = gameObject.GetComponentsInChildren<MonoBehaviour>();

            foreach(MonoBehaviour component in childComponents)
            {
                try
                {
                    Object.DestroyImmediate(component);
                }
                catch
                {
                }
            }
        }

        {
            Component[] childComponents = gameObject.GetComponentsInChildren<Component>();

            foreach(Component component in childComponents)
            {
                if(!(component is Transform))
                {
                    try
                    {
                        Object.DestroyImmediate(component);
                    }
                    catch
                    {
                    }
                }
            }
        }

        {
            Transform transform = gameObject.transform;

            foreach(Transform childTransform in transform)
            {
                Destroy(childTransform.gameObject);
            }
        }
    }

    // Sometimes we're not allowed to destroy objects, so we try to destroy it and return false if we failed
    public static bool TryDestroy(GameObject gameObject)
    {
        if(!gameObject)
        {
            return false;
        }

        HideFlags prevHideFlags = gameObject.hideFlags;

        // Cannot destroy gameObjects when certain hideflags are set
        gameObject.hideFlags = HideFlags.None;

        try
        {
            Object.DestroyImmediate(gameObject);

            return true;
        }
        catch
        {
            gameObject.hideFlags = prevHideFlags;

            return false;
        }
    }
}

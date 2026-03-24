using RealtimeCSG.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    public sealed class CSGPrefabUtility
    {
        public static Object GetCorrespondingObjectFromSource(Object source)
        {
            return PrefabUtility.GetCorrespondingObjectFromSource(source);
        }

        public static bool IsPrefab(CSGModel model)
        {
            if(!model)
            {
                return false;
            }

            GameObject parent = GetPrefabAsset(model.gameObject);

            return parent;
        }

        public static bool AreInPrefabMode()
        {
            StageHandle mainStage          = StageUtility.GetMainStageHandle();
            StageHandle currentStageHandle = StageUtility.GetCurrentStageHandle();

            return mainStage != currentStageHandle;
        }

        public static bool IsEditedInPrefabMode(CSGModel model)
        {
            if(!model)
            {
                return false;
            }

            if(!AreInPrefabMode())
            {
                return false;
            }

            StageHandle currentStageHandle = StageUtility.GetCurrentStageHandle();

            return currentStageHandle.Contains(model.gameObject);
        }

        public static CSGNode[] GetNodesInPrefabMode()
        {
            StageHandle mainStage          = StageUtility.GetMainStageHandle();
            StageHandle currentStageHandle = StageUtility.GetCurrentStageHandle();

            return mainStage != currentStageHandle ? currentStageHandle.FindComponentsOfType<CSGNode>() : null;
        }

        public static bool IsPrefabAssetOrInstance(GameObject gameObject)
        {
            PrefabAssetType      prefabAssetType    = PrefabUtility.GetPrefabAssetType(gameObject);
            PrefabInstanceStatus prefabInstanceType = PrefabUtility.GetPrefabInstanceStatus(gameObject);

            return prefabAssetType != PrefabAssetType.NotAPrefab || prefabInstanceType != PrefabInstanceStatus.NotAPrefab;
        }

        public static bool IsPrefabAsset(GameObject gameObject)
        {
            PrefabInstanceStatus prefabInstanceType = PrefabUtility.GetPrefabInstanceStatus(gameObject);

            if(prefabInstanceType != PrefabInstanceStatus.NotAPrefab)
            {
                return false;
            }

            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(gameObject);

            return prefabType != PrefabAssetType.NotAPrefab && prefabType != PrefabAssetType.Model;
        }

        public static bool IsPrefabInstance(GameObject gameObject)
        {
            if(!PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                return false;
            }

            PrefabInstanceStatus prefabType = PrefabUtility.GetPrefabInstanceStatus(gameObject);

            return prefabType != PrefabInstanceStatus.NotAPrefab;
        }

        public static GameObject GetPrefabAsset(GameObject gameObject)
        {
            if(AreInPrefabMode())
            {
                PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                if(prefabStage.IsPartOfPrefabContents(gameObject))
                {
                    return (GameObject)AssetDatabase.LoadMainAssetAtPath(prefabStage.assetPath);
                }
            }

            if(!IsPrefabInstance(gameObject))
            {
                return null;
            }

            return (GameObject)AssetDatabase.LoadMainAssetAtPath(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject));
        }

        public static GameObject GetOutermostPrefabInstanceRoot(GameObject gameObject)
        {
            return !IsPrefabInstance(gameObject) ? null : PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
        }

        public static GameObject Instantiate(GameObject originalGameObject, bool copy = false)
        {
            PrefabInstanceStatus prefabInstanceType = PrefabUtility.GetPrefabInstanceStatus(originalGameObject);

            if(prefabInstanceType != PrefabInstanceStatus.NotAPrefab && !copy)
            {
                Object     corrObj = GetCorrespondingObjectFromSource(originalGameObject);
                Object     obj     = PrefabUtility.InstantiatePrefab(corrObj);
                GameObject result  = obj as GameObject;

                return result;
            }

            PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(originalGameObject);

            if(prefabAssetType != PrefabAssetType.NotAPrefab && !copy)
            {
                Object     obj    = PrefabUtility.InstantiatePrefab(originalGameObject);
                GameObject result = obj as GameObject;

                return result;
            }

            GameObject inst = Object.Instantiate(originalGameObject);

            return inst;
        }

        internal static bool IsPartOfAsset(Mesh sharedMesh)
        {
            return !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(sharedMesh));
        }
    }
}

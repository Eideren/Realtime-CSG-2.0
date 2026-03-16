using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    internal partial class InternalCSGModelManager
    {
        private class CSGSceneState
        {
            public GameObject defaultModelObject;
            public CSGModel defaultModel;
        }

        internal const string DEFAULT_MODEL_NAME = "[default-CSGModel]";
        internal const string DEFAULT_PREFAB_MODEL_NAME = "[prefab-CSGModel]";
        private static readonly Dictionary<Scene, CSGSceneState> sceneStates = new Dictionary<Scene, CSGSceneState>();

#region DefaultCSGModel

        internal static CSGModel GetDefaultCSGModelForObject(Transform transform)
        {
            Scene currentScene;

            if(!transform || !transform.gameObject)
            {
                currentScene = SceneManager.GetActiveScene();
            }
            else
            {
                currentScene = transform.gameObject.scene;
            }

            return GetDefaultCSGModel(currentScene);
        }

        internal static CSGModel GetDefaultCSGModelForObject(MonoBehaviour component)
        {
            Scene currentScene;

            if(!component || !component.gameObject)
            {
                currentScene = SceneManager.GetActiveScene();
            }
            else
            {
                currentScene = component.gameObject.scene;
            }

            return GetDefaultCSGModel(currentScene);
        }

        internal static CSGModel GetDefaultCSGModel(Scene scene)
        {
            if(!scene.IsValid() ||
               !scene.isLoaded)
            {
                return null;
            }

            // make sure we're properly initialized (just in case)
            if(!sceneStates.ContainsKey(scene))
            {
                sceneStates[scene] = new CSGSceneState();
            }

            if(!sceneStates[scene].defaultModel)
            {
                InitializeDefaultCSGModel(scene, sceneStates[scene]);
            }

            return sceneStates[scene].defaultModel;
        }

#endregion

#region InitializeDefaultCSGModel

        private static void InitializeDefaultCSGModel(
            Scene currentScene, CSGSceneState sceneState, bool createIfNoExists = true
        )
        {
            bool      inPrefabMode        = false;
            Transform prefabRootTransform = null;

            PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();

            if(currentPrefabStage != null)
            {
                GameObject prefabRoot = currentPrefabStage.prefabContentsRoot;
                prefabRootTransform = prefabRoot.transform;
                inPrefabMode        = prefabRoot.scene == currentScene;
            }

            string defaultModelName = inPrefabMode ? DEFAULT_PREFAB_MODEL_NAME : DEFAULT_MODEL_NAME;

            // make sure we have a default CSG model
            sceneState.defaultModelObject =
                SceneQueryUtility.GetUniqueHiddenGameObjectInSceneWithName(currentScene, defaultModelName);

            if(createIfNoExists)
            {
                if(!sceneState.defaultModelObject)
                {
                    sceneState.defaultModelObject = new GameObject(defaultModelName);
                    sceneState.defaultModel       = CreateCSGModel(sceneState.defaultModelObject);
                }
                else
                {
                    sceneState.defaultModel = sceneState.defaultModelObject.GetComponent<CSGModel>();

                    if(!sceneState.defaultModel)
                    {
                        sceneState.defaultModel = CreateCSGModel(sceneState.defaultModelObject);
                    }
                }

                // make sure it's transformation has sensible values
                Transform transform = sceneState.defaultModel.transform;

                if(inPrefabMode)
                {
                    transform.SetParent(prefabRootTransform, false);
                }
                else
                {
                    transform.parent = null;
                }

                transform.localPosition = MathConstants.zeroVector3;
                transform.localScale    = MathConstants.oneVector3;
                transform.localRotation = Quaternion.identity;

                const StaticEditorFlags defaultFlags = StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.NavigationStatic |
                    StaticEditorFlags.OccludeeStatic |
                    StaticEditorFlags.OffMeshLinkGeneration |
                    StaticEditorFlags.ReflectionProbeStatic;

                GameObjectUtility.SetStaticEditorFlags(sceneState.defaultModel.gameObject, defaultFlags);

                RegisterModel(sceneState.defaultModel);
            }

            // do not give default CSG model any extra abilities other than rendering
            //  since an invisible object doesn't work well with navmesh/colliders etc.
            // force the users to use a CSG-model, but gracefully let it still be
            //  visible for objects outside of it.
            if(inPrefabMode)
            {
                sceneState.defaultModelObject.hideFlags = MeshInstanceManager.COMPONENT_HIDE_FLAGS | HideFlags.DontSave;
            }
            else
            {
                sceneState.defaultModelObject.hideFlags = MeshInstanceManager.COMPONENT_HIDE_FLAGS;
            }
        }

#endregion
    }
}

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Components;

namespace InternalRealtimeCSG
{
    internal sealed partial class MeshInstanceManager
    {
        [UnityEditor.Callbacks.PostProcessScene(0)]
        internal static void OnBuild()
        {
            // apparently only way to determine current scene while post processing a scene
            Transform randomObject = Object.FindAnyObjectByType<Transform>();

            if(!randomObject)
            {
                return;
            }

            Scene currentScene = randomObject.gameObject.scene;

            List<GeneratedMeshes> foundMeshContainers =
                SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshes>(currentScene);

            foreach(GeneratedMeshes meshContainer in foundMeshContainers)
            {
                CSGModel model = meshContainer.owner;

                if(!model)
                {
                    Object.DestroyImmediate(meshContainer.gameObject);

                    continue;
                }

                if(model.NeedAutoUpdateRigidBody)
                {
                    AutoUpdateRigidBody(meshContainer);
                }

                model.gameObject.hideFlags         = HideFlags.None;
                meshContainer.transform.hideFlags  = HideFlags.None;
                meshContainer.gameObject.hideFlags = HideFlags.None;
                meshContainer.hideFlags            = HideFlags.None;

                GeneratedMeshInstance[] instances = meshContainer.GetComponentsInChildren<GeneratedMeshInstance>(true);

                foreach(GeneratedMeshInstance instance in instances)
                {
                    if(!instance)
                    {
                        continue;
                    }

                    instance.gameObject.hideFlags = HideFlags.None; // HideFlags.NotEditable;
                    instance.gameObject.SetActive(true);

                    //Refresh(instance, model, postProcessScene: true);
#if SHOW_GENERATED_MESHES
                    UpdateName(instance);
#endif

                    // TODO: make sure meshes are no longer marked as dynamic!

                    if(!HasRuntimeMesh(instance))
                    {
                        Object.DestroyImmediate(instance.gameObject);

                        continue;
                    }

                    RenderSurfaceType surfaceType = GetSurfaceType(instance.MeshDescription, model.Settings);

                    if(surfaceType == RenderSurfaceType.ShadowOnly)
                    {
                        MeshRenderer meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();

                        if(meshRenderer)
                        {
                            meshRenderer.sharedMaterial = MaterialUtility.DefaultMaterial;
                            meshRenderer.enabled        = true;
                        }

                        RemoveIfEmpty(instance.gameObject);

                        continue;
                    }

                    if(surfaceType == RenderSurfaceType.Normal)
                    {
                        MeshRenderer meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();

                        if(meshRenderer)
                        {
                            meshRenderer.enabled = true;
                        }
                    }

                    if(surfaceType == RenderSurfaceType.Collider ||
                       surfaceType == RenderSurfaceType.Trigger)
                    {
                        MeshRenderer meshRenderer = instance.gameObject.GetComponent<MeshRenderer>();

                        if(meshRenderer)
                        {
                            Object.DestroyImmediate(meshRenderer);
                        }

                        MeshFilter meshFilter = instance.gameObject.GetComponent<MeshFilter>();

                        if(meshFilter)
                        {
                            Object.DestroyImmediate(meshFilter);
                        }

                        if(surfaceType == RenderSurfaceType.Trigger)
                        {
                            MeshCollider oldMeshCollider = instance.gameObject.GetComponent<MeshCollider>();

                            if(oldMeshCollider)
                            {
                                MeshCollider newMeshCollider = model.gameObject.AddComponent<MeshCollider>();
                                EditorUtility.CopySerialized(oldMeshCollider, newMeshCollider);
                                Object.DestroyImmediate(oldMeshCollider);
                            }
                        }

                        RemoveIfEmpty(instance.gameObject);

                        continue;
                    }

                    RemoveIfEmpty(instance.gameObject);
                }

                if(!meshContainer)
                {
                    continue;
                }

                GeneratedMeshInstance[] children = meshContainer.GetComponentsInChildren<GeneratedMeshInstance>();

                foreach(GeneratedMeshInstance child in children)
                {
                    child.hideFlags            = HideFlags.None;
                    child.gameObject.hideFlags = HideFlags.None;
                    child.transform.hideFlags  = HideFlags.None;

                    if(child.SharedMesh && !UsesLightmapUVs(model))
                    {
                        MeshUtility.Optimize(child.SharedMesh);
#if UNITY_EDITOR
                        MeshCollider currentMeshCollider = child.gameObject.GetComponent<MeshCollider>();

                        if(currentMeshCollider && currentMeshCollider.enabled)
                        {
                            currentMeshCollider.enabled = false;
                            currentMeshCollider.enabled = true;
                        }
#endif
                    }
                }

                if(CSGProjectSettings.Instance.SaveMeshesInSceneFiles
                || !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    Object.DestroyImmediate(meshContainer);
                }
                else
                {
                    meshContainer.hideFlags |= HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                }
            }

            List<GeneratedMeshInstance> meshInstances =
                SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshInstance>(currentScene);

            foreach(GeneratedMeshInstance meshInstance in meshInstances)
            {
                if(meshInstance)
                {
                    if(CSGProjectSettings.Instance.SaveMeshesInSceneFiles
                    || !EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        Object.DestroyImmediate(meshInstance);
                    }
                    else
                    {
                        meshInstance.hideFlags |= HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }
                }
            }

            HashSet<CSGNode> csgnodes =
                new HashSet<CSGNode>(SceneQueryUtility.GetAllComponentsInScene<CSGNode>(currentScene));

            List<GameObject> removableGameObjects = new List<GameObject>();

            foreach(CSGNode csgnode in csgnodes)
            {
                if(!csgnode)
                {
                    continue;
                }

                GameObject gameObject = csgnode.gameObject;
                CSGModel   model      = csgnode as CSGModel;

                if(
                    model && model.name == InternalCSGModelManager.DefaultModelName &&
                    (model.transform.childCount == 0 ||
                        (model.transform.childCount == 1 &&
                            model.transform.GetChild(0).name == MeshContainerName &&
                            model.transform.GetChild(0).childCount == 0)
                    )
                )
                {
                    Object.DestroyImmediate(gameObject);

                    continue;
                }
                else if(model)
                {
                    gameObject.tag = "Untagged";
                    AssignLayerToChildren(gameObject);
                }
                else if(gameObject.CompareTag("Untagged"))
                {
                    removableGameObjects.Add(gameObject);
                }

                if(csgnode)
                {
                    if(CSGProjectSettings.Instance.SaveMeshesInSceneFiles
                    || !EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        Object.DestroyImmediate(csgnode);
                    }
                    else
                    {
                        csgnode.hideFlags |= HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    }
                }
            }

            HashSet<Transform> removableTransforms = new HashSet<Transform>();

            for(int i = 0; i < removableGameObjects.Count; i++)
            {
                GameObject gameObject = removableGameObjects[i];
                Transform  transform  = gameObject.transform;

                if(removableTransforms.Contains(transform))
                {
                    continue;
                }

                if(CSGProjectSettings.Instance.SaveMeshesInSceneFiles
                || !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    RemoveWithChildrenIfPossible(transform, removableTransforms);
                }
            }
        }

        private static bool RemoveWithChildrenIfPossible(Transform transform, HashSet<Transform> removableTransforms)
        {
            for(int i = 0; i < transform.childCount; i++)
            {
                Transform childTransform = transform.GetChild(i);

                //if (!childTransform.CompareTag(CSGContstants.kRemoveNodeTag))
                //    return false;
                if(childTransform.GetComponent<CSGNode>() == null)
                {
                    transform.gameObject.tag = "Untagged";

                    return false;
                }

                if(!RemoveWithChildrenIfPossible(childTransform, removableTransforms))
                {
                    transform.gameObject.tag = "Untagged";

                    return false;
                }
            }

            Object.DestroyImmediate(transform.gameObject);

            return true;
        }

        internal static void DestroyAllMeshInstances()
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            for(int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);

                if(!scene.isLoaded)
                {
                    continue;
                }

                List<CSGModel> sceneModels = SceneQueryUtility.GetAllComponentsInScene<CSGModel>(scene);

                for(int i = 0; i < sceneModels.Count; i++)
                {
                    if(!ModelTraits.IsModelEditable(sceneModels[i]))
                    {
                        continue;
                    }

                    Transform   selfTransform = sceneModels[i].transform;
                    Transform[] transforms    = selfTransform.GetComponentsInChildren<Transform>();

                    foreach(Transform generateMeshesTransform in transforms)
                    {
                        if(!generateMeshesTransform || generateMeshesTransform.parent != selfTransform)
                        {
                            continue;
                        }

                        if(generateMeshesTransform.name != MeshContainerName)
                        {
                            continue;
                        }

                        Transform childTransforms = generateMeshesTransform;

                        foreach(Transform childTransform in childTransforms)
                        {
                            GameObjectExtensions.Destroy(childTransform.gameObject);
                        }
                    }
                }
            }
        }
    }
}

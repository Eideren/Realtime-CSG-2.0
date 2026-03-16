//#define SHOW_GENERATED_MESHES

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Foundation;
using RealtimeCSG.Components;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
#if UNITY_2018_3_OR_NEWER

#endif

namespace InternalRealtimeCSG
{
    internal sealed partial class MeshInstanceManager
    {
#if SHOW_GENERATED_MESHES
		public const HideFlags ComponentHideFlags = HideFlags.DontSaveInBuild;
#else
        public const HideFlags ComponentHideFlags = HideFlags.None
            //| HideFlags.NotEditable       // when this is put into a prefab (when making a prefab containing a model for instance) this will make it impossible to delete
            //| HideFlags.HideInInspector   // apparently, when set, can cause issues with selection in sceneview
          | HideFlags.HideInHierarchy
          | HideFlags.DontSaveInBuild;
#endif

        internal const string MeshContainerName = "[generated-meshes]";
        private const string RenderMeshInstanceName = "[generated-render-mesh]";
        private const string ColliderMeshInstanceName = "[generated-collider-mesh]";
        private const string HelperMeshInstanceName = "[generated-helper-mesh]";

        public static void Shutdown()
        {
        }

        public static void OnDestroyed(GeneratedMeshes container)
        {
        }

        public static void Destroy(GeneratedMeshes generatedMeshes)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(!generatedMeshes)
            {
                return;
            }

            if(generatedMeshes.owner)
            {
                generatedMeshes.owner.generatedMeshes = null;
            }

            generatedMeshes.owner = null;
            GameObjectExtensions.Destroy(generatedMeshes.gameObject);

            // Undo.DestroyObjectImmediate // NOTE: why was this used before?
            // can't use Undo variant here because it'll mark scenes as dirty on load ..
        }

        public static void OnCreated(GeneratedMeshes container)
        {
            ValidateGeneratedMeshesDelayed(container);
        }

        public static void OnEnable(GeneratedMeshes container)
        {
            if(container)
            {
                container.gameObject.SetActive(true);
            }
        }

        public static void OnDisable(GeneratedMeshes container)
        {
            if(container)
            {
                container.gameObject.SetActive(false);
            }
        }

        public static void OnCreated(GeneratedMeshInstance meshInstance)
        {
            Transform       parent    = meshInstance.transform.parent;
            GeneratedMeshes container = null;

            if(parent)
            {
                container = parent.GetComponent<GeneratedMeshes>();
            }

            if(!container)
            {
                GameObjectExtensions.Destroy(meshInstance.gameObject);

                return;
            }

            //EnsureValidHelperMaterials(container.owner, meshInstance);

            Initialize(container, meshInstance);

            MeshInstanceKey key = meshInstance.GenerateKey();

            if(container.GetMeshInstance(key) != meshInstance)
            {
                if(meshInstance && meshInstance.gameObject)
                {
                    GameObjectExtensions.Destroy(meshInstance.gameObject);
                }
            }
        }

        private static void Initialize(GeneratedMeshes container, GeneratedMeshInstance meshInstance)
        {
            // Retain layer
            meshInstance.gameObject.layer = container.gameObject.layer;

            container.AddMeshInstance(meshInstance);
        }

        private static void Initialize(GeneratedMeshes container, HelperSurfaceDescription helperSurface)
        {
            container.AddHelperSurface(helperSurface);
        }

        private static readonly HashSet<GeneratedMeshes> validateGeneratedMeshes = new HashSet<GeneratedMeshes>();
        private static readonly HashSet<CSGModel> validateModelWithChildren = new HashSet<CSGModel>();
        private static readonly HashSet<CSGModel> validateModelWithoutChildren = new HashSet<CSGModel>();

        public static void Update()
        {
            CSGModel[] models = CSGModelManager.GetAllModels();

            for(int i = 0; i < models.Length; i++)
            {
                CSGModel model = models[i];

                if(ModelTraits.IsModelEditable(model))
                {
                    ValidateModelDelayed(model);
                }
            }

            ignoreValidateModelDelayed = true;

            try
            {
                if(validateModelWithChildren.Count > 0)
                {
                    foreach(CSGModel model in validateModelWithChildren)
                    {
                        ValidateModelNow(model, true);
                    }

                    validateModelWithChildren.Clear();
                }

                if(validateModelWithoutChildren.Count > 0)
                {
                    foreach(CSGModel model in validateModelWithoutChildren)
                    {
                        ValidateModelNow(model, false);
                    }

                    validateModelWithoutChildren.Clear();
                }
            }
            finally
            {
                ignoreValidateModelDelayed = false;
            }

            for(int i = 0; i < models.Length; i++)
            {
                CSGModel model = models[i];

                if(!ModelTraits.IsModelEditable(model))
                {
                    continue;
                }

                GeneratedMeshes meshContainer = model.generatedMeshes;

                if(!meshContainer)
                {
                    continue;
                }

                if(!meshContainer.HasMeshInstances)
                {
                    ValidateGeneratedMeshesDelayed(meshContainer);

                    continue;
                }
            }

            if(validateGeneratedMeshes.Count > 0)
            {
                foreach(GeneratedMeshes generatedMeshes in validateGeneratedMeshes)
                {
                    CSGModel prevModel = generatedMeshes ? generatedMeshes.owner : null;

                    if(ValidateGeneratedMeshesNow(generatedMeshes))
                    {
                        if(prevModel)
                        {
                            prevModel.forceUpdate = true;
                        }
                    }
                }

                validateGeneratedMeshes.Clear();
            }

            UpdateTransforms();
        }

        public static void ValidateGeneratedMeshesDelayed(GeneratedMeshes meshContainer)
        {
            validateGeneratedMeshes.Add(meshContainer);
        }

        private static bool ignoreValidateModelDelayed = false;

        internal static void ValidateModelDelayed(CSGModel model, bool checkChildren = false)
        {
            if(ignoreValidateModelDelayed)
            {
                return;
            }

            if(checkChildren)
            {
                validateModelWithChildren.Add(model);
                validateModelWithoutChildren.Remove(model);
            }
            else
            {
                if(!validateModelWithChildren.Contains(model))
                {
                    validateModelWithoutChildren.Add(model);
                }
            }
        }

        private static void UpdateGeneratedMeshesFlags(CSGModel model, GeneratedMeshes generatedMeshes)
        {
            if(!generatedMeshes)
            {
                return;
            }

            if(generatedMeshes.enabled == false)
            {
                generatedMeshes.enabled = true;
            }

            GameObject generatedMeshesGameObject = generatedMeshes.gameObject;
            bool       activated                 = model.enabled && model.gameObject.activeSelf;

            if(generatedMeshesGameObject.activeSelf != activated)
            {
                generatedMeshesGameObject.SetActive(activated);
            }
        }

        private static GeneratedMeshes EnsureOneValidGeneratedMeshesComponent(CSGModel model)
        {
            if(!model.isActiveAndEnabled)
            {
                return null;
            }

            // Find all the GeneratedMeshes inside this model
            GeneratedMeshes[] foundGeneratedMeshes = model.GetComponentsInChildren<GeneratedMeshes>(true);

            // If we have CSGModel components inside this CSGModel component, we ignore all the GeneratedMeshes inside those ..
            for(int i = foundGeneratedMeshes.Length - 1; i >= 0; i--)
            {
                CSGModel[] models      = foundGeneratedMeshes[i].GetComponentsInParent<CSGModel>(true);
                CSGModel   parentModel = models.Length == 0 ? null : models[0];

                if(parentModel != model)
                {
                    // TODO: should just swap with last element + keep track of our own count in array and use that below
                    ArrayUtility.RemoveAt(ref foundGeneratedMeshes, i);
                }
            }

            if(foundGeneratedMeshes.Length > 1)
            {
                GeneratedMeshes prevGeneratedMeshes = model.generatedMeshes;
                GeneratedMeshes newGeneratedMeshes  = null;

                // Check if we have more than one GeneratedMeshes component, this can happen, for instance,
                //	due to prefab merging issues or if the user duplicates the gameObject

                for(int i = foundGeneratedMeshes.Length - 1; i >= 0; i--)
                {
                    GeneratedMeshes generatedMeshesComponent = foundGeneratedMeshes[i];

                    if(!generatedMeshesComponent)
                    {
                        continue;
                    }

                    GameObject generatedMeshesGameObject = generatedMeshesComponent.gameObject;

                    if(!newGeneratedMeshes ||
                       // Prefer to keep the GeneratedMesh that is currently used by the Model
                       generatedMeshesComponent == prevGeneratedMeshes)
                    {
                        // if the we already found a GeneratedMesh, and it's valid, try to see if we can destroy it
                        if(newGeneratedMeshes)
                        {
                            GameObject newGeneratedMeshesGameObject = newGeneratedMeshes.gameObject;

                            if(GameObjectExtensions.TryDestroy(newGeneratedMeshesGameObject))
                            {
                                newGeneratedMeshes = generatedMeshesComponent;

                                continue;
                            }
                            // Fall through, we need to destroy this component after all
                        }
                        else
                        {
                            newGeneratedMeshes = generatedMeshesComponent;

                            continue;
                        }
                    }

                    // Try to destroy the GeneratedMeshes gameObject
                    if(!GameObjectExtensions.TryDestroy(generatedMeshesGameObject))
                    {
                        // If the we already found a GeneratedMesh, and it's valid, try to see if we can destroy that instead
                        GameObject newGeneratedMeshesGameObject = newGeneratedMeshes.gameObject;

                        if(!GameObjectExtensions.TryDestroy(newGeneratedMeshesGameObject))
                        {
                            // Fall back to disabling the component instead
                            GameObjectExtensions.Destroy(newGeneratedMeshesGameObject);
                        }

                        prevGeneratedMeshes = null;
                        newGeneratedMeshes  = generatedMeshesComponent;
                    }
                }

                model.generatedMeshes = newGeneratedMeshes;

                if(model.generatedMeshes)
                {
                    model.generatedMeshes.owner = model;
                    UpdateGeneratedMeshesFlags(model, model.generatedMeshes);

                    if(ValidateGeneratedMeshesNow(model.generatedMeshes, true) && model)
                    {
                        model.forceUpdate = true;
                    }

                    return model.generatedMeshes;
                }

                // Fall through, create a new GeneratedMeshes component
            }
            else if(foundGeneratedMeshes.Length == 1)
            {
                model.generatedMeshes       = foundGeneratedMeshes[0];
                model.generatedMeshes.owner = model;
                UpdateGeneratedMeshesFlags(model, model.generatedMeshes);

                if(ValidateGeneratedMeshesNow(model.generatedMeshes, true) && model)
                {
                    model.forceUpdate = true;
                }

                return model.generatedMeshes;
            }

            // Create it if it doesn't exist
            GameObject generatedMeshesObject = new GameObject(MeshContainerName);
            generatedMeshesObject.SetActive(false);

            GeneratedMeshes generatedMeshes = generatedMeshesObject.AddComponent<GeneratedMeshes>();
            generatedMeshes.owner = model;

            Transform generatedMeshesTransform = generatedMeshesObject.transform;
            generatedMeshesTransform.SetParent(model.transform, false);

            // Retain layer
            generatedMeshesObject.layer = model.gameObject.layer;

            generatedMeshesObject.SetActive(true);

            UpdateGeneratedMeshesVisibility(generatedMeshes, model.ShowGeneratedMeshes);
            UpdateGeneratedMeshesFlags(model, generatedMeshes);
            model.generatedMeshes       = generatedMeshes;
            model.generatedMeshes.owner = model;

            return generatedMeshes;
        }

        internal static bool ValidateModelNow(CSGModel model, bool checkChildren = false)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return true;
            }

            if(!checkChildren &&
               model.generatedMeshes &&
               model.generatedMeshes.owner == model)
            {
                return true;
            }

            if(!ModelTraits.IsModelEditable(model) ||
               !model.isActiveAndEnabled)
            {
                return false;
            }

            EnsureOneValidGeneratedMeshesComponent(model);

            //model.forceUpdate = true; // TODO: this causes issues where models are constantly force updated
            return true;
        }

        private static bool ShouldRenderHelperSurface(RenderSurfaceType renderSurfaceType)
        {
            switch(renderSurfaceType)
            {
                case RenderSurfaceType.Hidden:   return CSGSettings.ShowHiddenSurfaces;
                case RenderSurfaceType.Culled:   return CSGSettings.ShowCulledSurfaces;
                case RenderSurfaceType.Collider: return CSGSettings.ShowColliderSurfaces;
                case RenderSurfaceType.Trigger:  return CSGSettings.ShowTriggerSurfaces;
                case RenderSurfaceType.ShadowOnly:
                case RenderSurfaceType.CastShadows: return CSGSettings.ShowCastShadowsSurfaces;
                case RenderSurfaceType.ReceiveShadows: return CSGSettings.ShowReceiveShadowsSurfaces;
            }

            return false;
        }

        /*
        private static RenderSurfaceType EnsureValidHelperMaterials(CSGModel model, GeneratedMeshInstance meshInstance)
        {
            var surfaceType = !meshInstance.RenderMaterial ? meshInstance.RenderSurfaceType :
                                GetRenderSurfaceType(model, meshInstance);
            if (surfaceType != RenderSurfaceType.Normal)
                meshInstance.RenderMaterial = MaterialUtility.GetSurfaceMaterial(surfaceType);
            return surfaceType;
        }
        */

        private static bool ValidMeshInstance(GeneratedMeshInstance meshInstance)
        {
            return meshInstance.IsValid();
        }

        public static bool HasVisibleMeshRenderer(GeneratedMeshInstance meshInstance)
        {
            if(!meshInstance)
            {
                return false;
            }

            return meshInstance.RenderSurfaceType == RenderSurfaceType.Normal;
        }

        public static bool HasRuntimeMesh(GeneratedMeshInstance meshInstance)
        {
            if(!meshInstance)
            {
                return false;
            }

            return meshInstance.RenderSurfaceType != RenderSurfaceType.Culled &&
                meshInstance.RenderSurfaceType != RenderSurfaceType.ReceiveShadows &&
                meshInstance.RenderSurfaceType != RenderSurfaceType.CastShadows &&
                meshInstance.RenderSurfaceType != RenderSurfaceType.Hidden;
        }

        public static void UpdateHelperSurfaces()
        {
            CSGModel[] models = InternalCSGModelManager.Models;

            for(int i = 0; i < models.Length; i++)
            {
                CSGModel model = models[i];

                if(!ModelTraits.IsModelEditable(model))
                {
                    continue;
                }

                if(!model.generatedMeshes)
                {
                    continue;
                }

                GeneratedMeshes container = model.generatedMeshes;

                if(container.owner != model ||
                   !container.gameObject)
                {
                    continue;
                }

                if(!container.HasHelperSurfaces)
                {
                    if(!container.HasMeshInstances)
                    {
                        ValidateGeneratedMeshesDelayed(container);
                    }

                    continue;
                }

                foreach(GeneratedMeshInstance meshInstance in container.MeshInstances)
                {
                    if(!meshInstance)
                    {
                        GameObjectExtensions.TryDestroy(meshInstance.gameObject);

                        return;
                    }
                }

                foreach(HelperSurfaceDescription helperSurface in container.HelperSurfaces)
                {
                    RenderSurfaceType renderSurfaceType = helperSurface.RenderSurfaceType;

                    if(!helperSurface.SharedMesh ||
                       helperSurface.SharedMesh.vertexCount == 0 ||
                       !ShouldRenderHelperSurface(renderSurfaceType))
                    {
                        if(helperSurface.GameObject)
                        {
                            helperSurface.GameObject.SetActive(false);
                        }

                        continue;
                    }

                    if(!helperSurface.GameObject || !helperSurface.MeshFilter || !helperSurface.MeshRenderer ||
                       helperSurface.MeshFilter.sharedMesh != helperSurface.SharedMesh)
                    {
                        UpdateHelperSurfaceGameObject(container, helperSurface);
                    }

                    if(!helperSurface.HasGeneratedNormals)
                    {
                        helperSurface.SharedMesh.RecalculateNormals();
                        helperSurface.HasGeneratedNormals = true;
                    }

                    helperSurface.GameObject.SetActive(true);
                    /*

                    if (!showWireframe)
                    {
                        // "DrawMeshNow" so that it renders properly in all shading modes
                        if (material.SetPass(0))
                            Graphics.DrawMeshNow(meshInstance.SharedMesh,
                                                    matrix);
                    } else
                    {
                        Graphics.DrawMesh(meshInstance.SharedMesh,
                                          matrix,
                                          material,
                                          layer: 0,
                                          camera: camera,
                                          submeshIndex: 0,
                                          properties: null,
                                          castShadows: false,
                                          receiveShadows: false);
                    }*/
                }
            }
        }

        public static Object[] FindRenderers(CSGModel[] models)
        {
            List<Object> renderers = new List<Object>();

            foreach(CSGModel model in models)
            {
                if(!model || !model.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if(!model.generatedMeshes)
                {
                    continue;
                }

                foreach(MeshRenderer renderer in model.generatedMeshes.GetComponentsInChildren<MeshRenderer>())
                {
                    if(!renderer)
                    {
                        continue;
                    }

                    RenderSurfaceType type = MaterialUtility.GetMaterialSurfaceType(renderer.sharedMaterial);

                    if(type == RenderSurfaceType.Normal)
                    {
                        continue;
                    }

                    renderers.Add(renderer);
                }
            }

            return renderers.ToArray();
        }

        internal static void ResetScene(Scene scene)
        {
            List<GeneratedMeshInstance> meshInstances =
                SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshInstance>(scene);

            for(int m = 0; m < meshInstances.Count; m++)
            {
                if(!meshInstances[m])
                {
                    continue;
                }

                Transform meshInstanceTransform = meshInstances[m].transform;

                Transform modelTransform = meshInstanceTransform
                    ? meshInstanceTransform.parent ? meshInstanceTransform.parent.parent : null
                    : null;

                CSGModel model = modelTransform ? modelTransform.GetComponent<CSGModel>() : null;

                if(model && !ModelTraits.IsModelEditable(model))
                {
                    continue;
                }

                model.generatedMeshes      = null;
                meshInstances[m].hideFlags = HideFlags.None;
                GameObject gameObject = meshInstances[m].gameObject;
                GameObjectExtensions.SanitizeGameObject(gameObject);
                GameObjectExtensions.TryDestroy(gameObject);
            }
        }

        internal static void Reset()
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

                ResetScene(scene);
            }

#if UNITY_2018_4_OR_NEWER
            if(CSGPrefabUtility.AreInPrefabMode())
            {
                PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                ResetScene(currentPrefabStage.scene);
            }
#endif
        }

        public static RenderSurfaceType GetSurfaceType(
            GeneratedMeshDescription meshDescription, ModelSettingsFlags modelSettings
        )
        {
            if(meshDescription.meshQuery.LayerQuery == LayerUsageFlags.Culled)
            {
                return RenderSurfaceType.Culled;
            }

            switch(meshDescription.meshQuery.LayerParameterIndex)
            {
                case LayerParameterIndex.LayerParameter1:
                {
                    switch(meshDescription.meshQuery.LayerQuery)
                    {
                        case LayerUsageFlags.RenderReceiveCastShadows:
                        case LayerUsageFlags.RenderReceiveShadows:
                        case LayerUsageFlags.RenderCastShadows:
                        case LayerUsageFlags.Renderable: return RenderSurfaceType.Normal;
                        case LayerUsageFlags.ReceiveShadows: return RenderSurfaceType.Hidden;
                        case LayerUsageFlags.CastShadows | LayerUsageFlags.ReceiveShadows:
                        case LayerUsageFlags.CastShadows: return RenderSurfaceType.ShadowOnly;
                        default:
                            return RenderSurfaceType.Culled;
                    }
                }
                case LayerParameterIndex.LayerParameter2:
                {
                    if((modelSettings & ModelSettingsFlags.IsTrigger) != 0)
                    {
                        return RenderSurfaceType.Trigger;
                    }
                    else
                    {
                        return RenderSurfaceType.Collider;
                    }
                }
                case LayerParameterIndex.None:
                {
                    switch(meshDescription.meshQuery.LayerQuery)
                    {
                        case LayerUsageFlags.None:           return RenderSurfaceType.Hidden;
                        case LayerUsageFlags.CastShadows:    return RenderSurfaceType.CastShadows;
                        case LayerUsageFlags.ReceiveShadows: return RenderSurfaceType.ReceiveShadows;
                        case LayerUsageFlags.Collidable:
                        {
                            if((modelSettings & ModelSettingsFlags.IsTrigger) != 0)
                            {
                                return RenderSurfaceType.Trigger;
                            }
                            else
                            {
                                return RenderSurfaceType.Collider;
                            }
                        }
                        default:
                        case LayerUsageFlags.Culled:
                            return RenderSurfaceType.Culled;
                    }
                }
            }

            return RenderSurfaceType.Normal;
        }

        private static GeneratedMeshInstance SafeGetGeneratedMeshInstance(GameObject meshInstanceGameObject)
        {
            GeneratedMeshInstance instance;

            if(meshInstanceGameObject.TryGetComponent(out instance))
            {
                return instance;
            }

            return meshInstanceGameObject.AddComponent<GeneratedMeshInstance>();
        }

        private static void RemoveComponent<T>(GameObject gameObject) where T : Component
        {
            T component;

            if(!gameObject.TryGetComponent(out component))
            {
                return;
            }

            Object.DestroyImmediate(component);
        }

        private static void RemoveMeshInstanceComponents(
            GameObject gameObject, GeneratedMeshDescription meshDescription
        )
        {
            LayerParameterIndex parameterIndex = meshDescription.meshQuery.LayerParameterIndex;

            if(parameterIndex != LayerParameterIndex.PhysicsMaterial)
            {
                RemoveComponent<MeshCollider>(gameObject);
            }

            if(parameterIndex != LayerParameterIndex.RenderMaterial)
            {
                RemoveComponent<MeshFilter>(gameObject);
                RemoveComponent<MeshRenderer>(gameObject);
            }
        }

        private static void GetGameObjectAndGeneratedMeshInstance(
            List<GameObject> unusedInstances, GeneratedMeshDescription meshDescription
          , out GameObject meshInstanceGameObject, out GeneratedMeshInstance meshInstance
        )
        {
            if(unusedInstances == null ||
               unusedInstances.Count == 0)
            {
                meshInstanceGameObject = new GameObject();
                meshInstance           = meshInstanceGameObject.AddComponent<GeneratedMeshInstance>();

                return;
            }

            for(int i = 0; i < unusedInstances.Count; i++)
            {
                GeneratedMeshInstance instance;

                if(!unusedInstances[i].TryGetComponent(out instance))
                {
                    continue;
                }

                if(meshDescription.meshQuery.LayerParameterIndex
                == instance.MeshDescription.meshQuery.LayerParameterIndex)
                {
                    meshInstanceGameObject           = unusedInstances[i];
                    meshInstanceGameObject.hideFlags = HideFlags.None;
                    unusedInstances.RemoveAt(i);
                    meshInstance = SafeGetGeneratedMeshInstance(meshInstanceGameObject);

                    return;
                }
            }

            meshInstanceGameObject           = unusedInstances[unusedInstances.Count - 1];
            meshInstanceGameObject.hideFlags = HideFlags.None;
            RemoveMeshInstanceComponents(meshInstanceGameObject, meshDescription);
            meshInstance = SafeGetGeneratedMeshInstance(meshInstanceGameObject);
            unusedInstances.RemoveAt(unusedInstances.Count - 1);
        }

        public static GeneratedMeshInstance CreateMeshInstance(
            GeneratedMeshes generatedMeshes, GeneratedMeshDescription meshDescription, ModelSettingsFlags modelSettings
          , RenderSurfaceType renderSurfaceType, List<GameObject> unusedInstances
        )
        {
            if(!generatedMeshes || !generatedMeshes.owner)
            {
                return null;
            }

            Transform             generatedMeshesTransform = generatedMeshes.transform;
            GameObject            meshInstanceGameObject   = null;
            GeneratedMeshInstance meshInstance             = null;

            GetGameObjectAndGeneratedMeshInstance(
                unusedInstances, meshDescription, out meshInstanceGameObject, out meshInstance
            );

            meshInstance.Reset();
            meshInstanceGameObject.SetActive(false);
            meshInstanceGameObject.transform.SetParent(generatedMeshesTransform, false);
            meshInstanceGameObject.transform.localPosition = MathConstants.zeroVector3;
            meshInstanceGameObject.transform.localRotation = MathConstants.identityQuaternion;
            meshInstanceGameObject.transform.localScale    = MathConstants.oneVector3;

            StaticEditorFlags containerStaticFlags =
                GameObjectUtility.GetStaticEditorFlags(generatedMeshes.owner.gameObject);

            GameObjectUtility.SetStaticEditorFlags(meshInstanceGameObject, containerStaticFlags);

            Material        renderMaterial  = null;
            PhysicsMaterial physicsMaterial = null;

            if(meshDescription.surfaceParameter != 0)
            {
                Object obj = EditorUtility.EntityIdToObject(meshDescription.surfaceParameter);

                if(obj)
                {
                    switch(meshDescription.meshQuery.LayerParameterIndex)
                    {
                        case LayerParameterIndex.RenderMaterial:
                        {
                            renderMaterial = obj as Material;

                            break;
                        }
                        case LayerParameterIndex.PhysicsMaterial:
                        {
                            physicsMaterial = obj as PhysicsMaterial;

                            break;
                        }
                    }
                }
            }

            meshInstance.MeshDescription = meshDescription;

            // Our mesh has not been initialized yet, so make sure we reflect that fact
            meshInstance.MeshDescription.geometryHashValue = 0;
            meshInstance.MeshDescription.surfaceHashValue  = 0;
            meshInstance.MeshDescription.vertexCount       = 0;
            meshInstance.MeshDescription.indexCount        = 0;

            meshInstance.RenderMaterial    = renderMaterial;
            meshInstance.PhysicsMaterial   = physicsMaterial;
            meshInstance.RenderSurfaceType = renderSurfaceType;

            meshInstanceGameObject.SetActive(true);

            Initialize(generatedMeshes, meshInstance);

            return meshInstance;
        }

        public static void UpdateHelperSurfaceGameObject(
            GeneratedMeshes container, HelperSurfaceDescription helperSurfaceDescription
        )
        {
            if(!helperSurfaceDescription.GameObject ||
               !helperSurfaceDescription.MeshFilter ||
               !helperSurfaceDescription.MeshRenderer)
            {
                if(helperSurfaceDescription.GameObject)
                {
                    GameObjectExtensions.Destroy(helperSurfaceDescription.GameObject);
                }

                helperSurfaceDescription.GameObject = new GameObject(HelperMeshInstanceName);
                helperSurfaceDescription.GameObject.SetActive(false);
                helperSurfaceDescription.GameObject.transform.SetParent(container.transform, true);
                helperSurfaceDescription.GameObject.transform.localPosition = Vector3.zero;
                helperSurfaceDescription.GameObject.transform.localRotation = Quaternion.identity;
                helperSurfaceDescription.GameObject.transform.localScale = Vector3.one;
                helperSurfaceDescription.GameObject.hideFlags = HideFlags.DontSave;
                helperSurfaceDescription.MeshFilter = helperSurfaceDescription.GameObject.AddComponent<MeshFilter>();

                helperSurfaceDescription.MeshRenderer =
                    helperSurfaceDescription.GameObject.AddComponent<MeshRenderer>();
            }

            if(helperSurfaceDescription.MeshFilter.sharedMesh != helperSurfaceDescription.SharedMesh)
            {
                helperSurfaceDescription.MeshFilter.sharedMesh = helperSurfaceDescription.SharedMesh;
            }

            Material sharedMaterial = MaterialUtility.GetSurfaceMaterial(helperSurfaceDescription.RenderSurfaceType);

            if(helperSurfaceDescription.MeshRenderer.sharedMaterial != sharedMaterial)
            {
                helperSurfaceDescription.MeshRenderer.sharedMaterial = sharedMaterial;
            }
        }

        public static HelperSurfaceDescription CreateHelperSurfaceDescription(
            GeneratedMeshes container, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription
          , RenderSurfaceType renderSurfaceType
        )
        {
            HelperSurfaceDescription instance = new HelperSurfaceDescription
            {
                RenderSurfaceType = renderSurfaceType
              , MeshDescription   = meshDescription
            };

            // Our mesh has not been initialized yet, so make sure we reflect that fact
            instance.MeshDescription.geometryHashValue = 0;
            instance.MeshDescription.surfaceHashValue  = 0;
            instance.MeshDescription.vertexCount       = 0;
            instance.MeshDescription.indexCount        = 0;

            Initialize(container, instance);

            return instance;
        }

        internal static void ClearOrCreateMesh(
            string baseName, bool editorOnly, ref bool hasGeneratedNormals, ref Mesh sharedMesh
        )
        {
            bool recreateMeshes = CSGPrefabUtility.IsPartOfAsset(sharedMesh);

            hasGeneratedNormals = false;

            if(!recreateMeshes && sharedMesh)
            {
                if(!CSGProjectSettings.Instance.SaveMeshesInSceneFiles)
                {
                    sharedMesh.hideFlags |= HideFlags.DontSaveInEditor;
                }

                sharedMesh.Clear(true);

                return;
            }

            sharedMesh      = new Mesh();
            sharedMesh.name = $"<{baseName} generated {sharedMesh.GetInstanceID()}>";
            sharedMesh.MarkDynamic();

            if(editorOnly)
            {
                sharedMesh.hideFlags = HideFlags.DontSaveInBuild;
            }

            if(!CSGProjectSettings.Instance.SaveMeshesInSceneFiles)
            {
                sharedMesh.hideFlags |= HideFlags.DontSaveInEditor;
            }
        }

        public static bool UsesLightmapUVs(CSGModel model)
        {
            StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);
#if UNITY_2019_2_OR_NEWER
            if((staticFlags & StaticEditorFlags.ContributeGI) != StaticEditorFlags.ContributeGI)
            {
                return false;
            }
#else
            if ((staticFlags & StaticEditorFlags.LightmapStatic) != StaticEditorFlags.LightmapStatic)
                return false;
#endif
            return true;
        }

        public static bool NeedToGenerateLightmapUVsForModel(CSGModel model)
        {
            if(!ModelTraits.IsModelEditable(model))
            {
                return false;
            }

            if(!model.generatedMeshes)
            {
                return false;
            }

            GeneratedMeshes container = model.generatedMeshes;

            if(!container || container.owner != model)
            {
                return false;
            }

            if(!container.HasMeshInstances)
            {
                return false;
            }

            if(!UsesLightmapUVs(model))
            {
                return false;
            }

            foreach(GeneratedMeshInstance instance in container.MeshInstances)
            {
                if(!instance)
                {
                    continue;
                }

                if(NeedToGenerateLightmapUVsForInstance(instance))
                {
                    return true;
                }
            }

            return false;
        }

        public static void GenerateLightmapUVsForModel(CSGModel model)
        {
            if(!ModelTraits.IsModelEditable(model))
            {
                return;
            }

            if(!model.generatedMeshes)
            {
                return;
            }

            GeneratedMeshes container = model.generatedMeshes;

            if(!container || !container.owner)
            {
                return;
            }

            if(!container.HasMeshInstances)
            {
                return;
            }

            UnwrapParam uvGenerationSettings = new UnwrapParam
            {
                angleError = Mathf.Clamp(model.angleError, CSGModel.MinAngleError, CSGModel.MaxAngleError)
              , areaError  = Mathf.Clamp(model.areaError, CSGModel.MinAreaError, CSGModel.MaxAreaError)
              , hardAngle  = model.hardAngle
              , packMargin = model.packMargin / 1024.0f
            };

            foreach(GeneratedMeshInstance instance in container.MeshInstances)
            {
                if(!instance)
                {
                    continue;
                }

                if(!instance.SharedMesh)
                {
                    instance.FindMissingSharedMesh();

                    if(!instance.SharedMesh)
                    {
                        continue;
                    }
                }

                GenerateLightmapUVsForInstance(instance, model, uvGenerationSettings);
            }
        }

        private static void GenerateLightmapUVsForInstance(
            GeneratedMeshInstance instance, CSGModel model, UnwrapParam param
        )
        {
            MeshRenderer meshRendererComponent = instance.CachedMeshRenderer;

            if(!meshRendererComponent)
            {
                GameObject gameObject = instance.gameObject;
                meshRendererComponent         = gameObject.GetComponent<MeshRenderer>();
                instance.CachedMeshRendererSO = null;
            }

            if(!meshRendererComponent)
            {
                return;
            }

            meshRendererComponent.realtimeLightmapIndex = -1;
            meshRendererComponent.lightmapIndex         = -1;

            Vector3[] oldVertices = instance.SharedMesh.vertices;

            if(oldVertices.Length == 0)
            {
                return;
            }

            Mesh tempMesh = instance.SharedMesh.Clone();
            instance.SharedMesh = tempMesh;

            Object pingObject = model;

            if(model.ShowGeneratedMeshes)
            {
                pingObject = instance;
            }

            Debug.Log(
                "Generating lightmap UVs (by Unity) for the mesh " + instance.name + " of the Model named \""
              + model.name + "\"\n", pingObject
            );
            //var optimizeTime = EditorApplication.timeSinceStartup;
            //MeshUtility.Optimize(instance.SharedMesh);
            //optimizeTime = EditorApplication.timeSinceStartup - optimizeTime;

            double lightmapGenerationTime = EditorApplication.timeSinceStartup;
            MeshUtility.Optimize(instance.SharedMesh);
            Unwrapping.GenerateSecondaryUVSet(instance.SharedMesh, param);
            lightmapGenerationTime = EditorApplication.timeSinceStartup - lightmapGenerationTime;

            Debug.Log( //"\tMesh optimizing in " + (optimizeTime * 1000) + " ms\n"+
                "\tUV generation in " + lightmapGenerationTime * 1000 + " ms\n", model
            );

            EditorSceneManager.MarkSceneDirty(instance.gameObject.scene);
            instance.LightingHashValue = instance.MeshDescription.geometryHashValue;
            instance.HasUV2            = true;
        }

        private static bool NeedToGenerateLightmapUVsForInstance(GeneratedMeshInstance instance)
        {
            return !instance.HasUV2 && instance.RenderSurfaceType == RenderSurfaceType.Normal;
        }

        private static bool AreBoundsEmpty(GeneratedMeshInstance instance)
        {
            return
                Mathf.Abs(instance.SharedMesh.bounds.size.x) <= MathConstants.EqualityEpsilon ||
                Mathf.Abs(instance.SharedMesh.bounds.size.y) <= MathConstants.EqualityEpsilon ||
                Mathf.Abs(instance.SharedMesh.bounds.size.z) <= MathConstants.EqualityEpsilon;
        }

        private static bool NeedCollider(RenderSurfaceType surfaceType)
        {
            return surfaceType == RenderSurfaceType.Collider || surfaceType == RenderSurfaceType.Trigger;
        }

        private static bool NeedMeshRenderer(RenderSurfaceType renderSurfaceType)
        {
            return renderSurfaceType == RenderSurfaceType.Normal || renderSurfaceType == RenderSurfaceType.ShadowOnly;
        }

        private static StaticEditorFlags FilterStaticEditorFlags(
            StaticEditorFlags modelStaticFlags, RenderSurfaceType renderSurfaceType
        )
        {
            if(!NeedMeshRenderer(renderSurfaceType))
            {
                return modelStaticFlags;
            }

            StaticEditorFlags meshStaticFlags = modelStaticFlags;

            bool walkable = renderSurfaceType != RenderSurfaceType.Hidden &&
                renderSurfaceType != RenderSurfaceType.ShadowOnly &&
                renderSurfaceType != RenderSurfaceType.Culled &&
                renderSurfaceType != RenderSurfaceType.Trigger;

            if(!walkable)
            {
                meshStaticFlags = meshStaticFlags & ~StaticEditorFlags.NavigationStatic;
            }

            // This fixes a bug in 2018.3 where it tries to generate lightmaps for ShadowOnly surfaces ..
            // .. but then rage quits because it doesn't have any normals
#if UNITY_2019_2_OR_NEWER
            if(renderSurfaceType == RenderSurfaceType.ShadowOnly)
            {
                meshStaticFlags = meshStaticFlags
                  & ~(StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            }
#else
            if (renderSurfaceType == RenderSurfaceType.ShadowOnly)
                meshStaticFlags =
 meshStaticFlags & ~(StaticEditorFlags.LightmapStatic | StaticEditorFlags.ReflectionProbeStatic);
#endif

            return meshStaticFlags & modelStaticFlags;
        }

        private static string MaterialToString(Material mat)
        {
            if(ReferenceEquals(mat, null))
            {
                return "null";
            }

            if(!mat)
            {
                return "invalid";
            }

            return mat.name + " " + mat.GetInstanceID().ToString();
        }

        public static void ClearUVs(CSGModel model)
        {
            if(!model.generatedMeshes)
            {
                return;
            }

            GeneratedMeshes container = model.generatedMeshes;

            if(!container || !container.owner)
            {
                return;
            }

            foreach(GeneratedMeshInstance instance in container.MeshInstances)
            {
                if(!instance)
                {
                    continue;
                }

                Refresh(instance, model, onlyFastRefreshes: false, skipAssetDatabaseUpdate: true);
                ClearUVs(instance);
            }
        }

        public static void ClearUVs(GeneratedMeshInstance instance)
        {
            MeshRenderer meshRendererComponent = instance.CachedMeshRenderer;

            if(meshRendererComponent)
            {
                meshRendererComponent.realtimeLightmapIndex = -1;
                meshRendererComponent.lightmapIndex         = -1;
            }

            instance.LightingHashValue = 0;
            instance.HasUV2            = false;
        }

        public static void Refresh(CSGModel model, bool postProcessScene = false, bool onlyFastRefreshes = true)
        {
            if(!ModelTraits.IsModelEditable(model))
            {
                return;
            }

            GeneratedMeshes generatedMeshes = model.generatedMeshes;

            if(!generatedMeshes || generatedMeshes.owner != model)
            {
                return;
            }

            foreach(GeneratedMeshInstance instance in generatedMeshes.MeshInstances)
            {
                if(!instance)
                {
                    continue;
                }

                Refresh(instance, model, postProcessScene, onlyFastRefreshes, true);
            }
        }

        //		internal static double updateMeshColliderMeshTime = 0.0;
        public static void Refresh(
            GeneratedMeshInstance instance, CSGModel owner, bool postProcessScene = false, bool onlyFastRefreshes = true
          , bool skipAssetDatabaseUpdate = true
        )
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(!instance)
            {
                return;
            }

            if(postProcessScene)
            {
                onlyFastRefreshes = false;
            }

            if(!instance.SharedMesh)
            {
                return;
            }

            // Update the flags
            RenderSurfaceType oldRenderSurfaceType = instance.RenderSurfaceType;
            instance.RenderSurfaceType = GetSurfaceType(instance.MeshDescription, owner.Settings);
            instance.Dirty             = instance.Dirty || oldRenderSurfaceType != instance.RenderSurfaceType;

            // Update the transform, if incorrect
            GameObject gameObject = instance.gameObject;

            if(gameObject.transform.localPosition != MathConstants.zeroVector3)
            {
                gameObject.transform.localPosition = MathConstants.zeroVector3;
            }

            if(gameObject.transform.localRotation != MathConstants.identityQuaternion)
            {
                gameObject.transform.localRotation = MathConstants.identityQuaternion;
            }

            if(gameObject.transform.localScale != MathConstants.oneVector3)
            {
                gameObject.transform.localScale = MathConstants.oneVector3;
            }

#if SHOW_GENERATED_MESHES
			var meshInstanceFlags = HideFlags.None;
			var transformFlags = HideFlags.None;
			var gameObjectFlags = HideFlags.None;
#else
            HideFlags meshInstanceFlags = HideFlags.DontSaveInBuild; // | HideFlags.NotEditable;
            HideFlags transformFlags    = HideFlags.HideInInspector; // | HideFlags.NotEditable;
            HideFlags gameObjectFlags   = HideFlags.None;
#endif

            if(gameObject.transform.hideFlags != transformFlags)
            {
                gameObject.transform.hideFlags = transformFlags;
            }

            if(gameObject.hideFlags != gameObjectFlags)
            {
                gameObject.hideFlags = gameObjectFlags;
            }

            if(instance.hideFlags != meshInstanceFlags)
            {
                instance.hideFlags = meshInstanceFlags;
            }

            bool showVisibleSurfaces = instance.RenderSurfaceType != RenderSurfaceType.Normal ||
                (CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;

            CSGVisibilityUtility.SetGameObjectVisibility(gameObject, showVisibleSurfaces);

            if(!instance.enabled)
            {
                instance.enabled = true;
            }

            // Update navigation on mesh
            StaticEditorFlags oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);

            StaticEditorFlags newStaticFlags = FilterStaticEditorFlags(
                GameObjectUtility.GetStaticEditorFlags(owner.gameObject), instance.RenderSurfaceType
            );

            if(newStaticFlags != oldStaticFlags)
            {
                GameObjectUtility.SetStaticEditorFlags(gameObject, newStaticFlags);
            }

            MeshFilter   meshFilterComponent   = instance.CachedMeshFilter;
            MeshRenderer meshRendererComponent = instance.CachedMeshRenderer;

            bool needMeshRenderer = NeedMeshRenderer(instance.RenderSurfaceType);

            if(needMeshRenderer)
            {
                if(!meshRendererComponent)
                {
                    meshRendererComponent         = gameObject.GetComponent<MeshRenderer>();
                    instance.CachedMeshRendererSO = null;
                }

                if(!meshFilterComponent)
                {
                    meshFilterComponent = gameObject.GetComponent<MeshFilter>();

                    if(!meshFilterComponent)
                    {
                        meshFilterComponent           = gameObject.AddComponent<MeshFilter>();
                        instance.CachedMeshRendererSO = null;
                        instance.Dirty                = true;
                    }
                }

//				var ownerReceiveShadows = owner.ReceiveShadows;
//				var shadowCastingMode	= owner.ShadowCastingModeFlags;
                bool ownerReceiveShadows = true;

                ShadowCastingMode shadowCastingMode = owner.IsTwoSidedShadows
                    ? UnityEngine.Rendering.ShadowCastingMode.TwoSided
                    : UnityEngine.Rendering.ShadowCastingMode.On;

                if(instance.RenderSurfaceType == RenderSurfaceType.ShadowOnly)
                {
                    shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }

                switch(instance.MeshDescription.meshQuery.LayerQuery)
                {
                    case LayerUsageFlags.RenderReceiveCastShadows:
                    {
                        break;
                    }
                    case LayerUsageFlags.RenderReceiveShadows:
                    {
                        shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        break;
                    }
                    case LayerUsageFlags.RenderCastShadows:
                    {
                        ownerReceiveShadows = false;

                        break;
                    }
                    case LayerUsageFlags.Renderable:
                    {
                        shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.Off;
                        ownerReceiveShadows = false;

                        break;
                    }
                    case LayerUsageFlags.CastShadows:
                    {
                        shadowCastingMode   = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                        ownerReceiveShadows = false;

                        break;
                    }
                }

                Material requiredMaterial = instance.RenderMaterial;

                if(shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                    // Note: need non-transparent material here
                {
                    requiredMaterial = MaterialUtility.DefaultMaterial;
                }

                if(!requiredMaterial)
                {
                    requiredMaterial = MaterialUtility.MissingMaterial;
                }

                if(!meshRendererComponent)
                {
                    meshRendererComponent                 = gameObject.AddComponent<MeshRenderer>();
                    meshRendererComponent.sharedMaterial  = requiredMaterial;
                    meshRendererComponent.gameObject.name = RenderMeshInstanceName;
                    instance.CachedMeshRendererSO         = null;
                    instance.Dirty                        = true;
                    // we don't actually want the unity style of rendering a wireframe
                    // for our meshes, so we turn it off
                    //*
                    EditorUtility.SetSelectedRenderState(meshRendererComponent, EditorSelectedRenderState.Hidden);
                    //*/
                }

                if((meshFilterComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
                {
                    meshFilterComponent.hideFlags |= HideFlags.HideInHierarchy;
                }

                if((meshRendererComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
                {
                    meshRendererComponent.hideFlags |= HideFlags.HideInHierarchy;
                }

                if(instance.RenderSurfaceType != RenderSurfaceType.ShadowOnly)
                {
                    if(instance.HasUV2 &&
                       instance.LightingHashValue != instance.MeshDescription.geometryHashValue
                    && meshRendererComponent)
                    {
                        instance.ResetUVTime = Time.realtimeSinceStartup;

                        if(instance.HasUV2)
                        {
                            ClearUVs(instance);
                        }
                    }

                    if(owner.AutoRebuildUVs || postProcessScene)
                    {
                        if((float.IsPositiveInfinity(instance.ResetUVTime)
                            || Time.realtimeSinceStartup - instance.ResetUVTime > 2.0f) &&
                           NeedToGenerateLightmapUVsForModel(owner))
                        {
                            GenerateLightmapUVsForModel(owner);
                        }
                    }
                }

                if(!postProcessScene &&
                   meshFilterComponent.sharedMesh != instance.SharedMesh)
                {
                    meshFilterComponent.sharedMesh = instance.SharedMesh;
                }

                if(meshRendererComponent &&
                   meshRendererComponent.shadowCastingMode != shadowCastingMode)
                {
                    meshRendererComponent.shadowCastingMode = shadowCastingMode;
                    instance.Dirty                          = true;
                }

                if(meshRendererComponent &&
                   meshRendererComponent.receiveShadows != ownerReceiveShadows)
                {
                    meshRendererComponent.receiveShadows = ownerReceiveShadows;
                    instance.Dirty                       = true;
                }

                //*
                if(!onlyFastRefreshes)
                {
                    SerializedObject meshRendererComponentSO = instance.CachedMeshRendererSO as SerializedObject;

                    if(meshRendererComponentSO == null)
                    {
                        if(meshRendererComponent)
                        {
                            instance.CachedMeshRendererSO =
                                meshRendererComponentSO = new SerializedObject(meshRendererComponent);
                        }
                    }
                    else if(!meshRendererComponent)
                    {
                        instance.CachedMeshRendererSO =
                            meshRendererComponentSO = null;
                    }

                    if(meshRendererComponentSO != null)
                    {
                        bool SOModified = false;
                        meshRendererComponentSO.Update();

                        SerializedProperty scaleInLightmapProperty =
                            meshRendererComponentSO.FindProperty("m_ScaleInLightmap");

                        float scaleInLightmap = owner.scaleInLightmap;

                        if(scaleInLightmapProperty != null &&
                           scaleInLightmapProperty.floatValue != scaleInLightmap)
                        {
                            scaleInLightmapProperty.floatValue = scaleInLightmap;
                            SOModified                         = true;
                        }

                        SerializedProperty autoUVMaxDistanceProperty =
                            meshRendererComponentSO.FindProperty("m_AutoUVMaxDistance");

                        float autoUVMaxDistance = owner.autoUVMaxDistance;

                        if(autoUVMaxDistanceProperty != null &&
                           autoUVMaxDistanceProperty.floatValue != autoUVMaxDistance)
                        {
                            autoUVMaxDistanceProperty.floatValue = autoUVMaxDistance;
                            SOModified                           = true;
                        }

                        SerializedProperty autoUVMaxAngleProperty =
                            meshRendererComponentSO.FindProperty("m_AutoUVMaxAngle");

                        float autoUVMaxAngle = owner.autoUVMaxAngle;

                        if(autoUVMaxAngleProperty != null &&
                           autoUVMaxAngleProperty.floatValue != autoUVMaxAngle)
                        {
                            autoUVMaxAngleProperty.floatValue = autoUVMaxAngle;
                            SOModified                        = true;
                        }

                        SerializedProperty ignoreNormalsProperty =
                            meshRendererComponentSO.FindProperty("m_IgnoreNormalsForChartDetection");

                        bool ignoreNormals = owner.IgnoreNormals;

                        if(ignoreNormalsProperty != null &&
                           ignoreNormalsProperty.boolValue != ignoreNormals)
                        {
                            ignoreNormalsProperty.boolValue = ignoreNormals;
                            SOModified                      = true;
                        }

                        SerializedProperty minimumChartSizeProperty =
                            meshRendererComponentSO.FindProperty("m_MinimumChartSize");

                        int minimumChartSize = owner.minimumChartSize;

                        if(minimumChartSizeProperty != null &&
                           minimumChartSizeProperty.intValue != minimumChartSize)
                        {
                            minimumChartSizeProperty.intValue = minimumChartSize;
                            SOModified                        = true;
                        }

                        SerializedProperty preserveUVsProperty = meshRendererComponentSO.FindProperty("m_PreserveUVs");
                        bool               preserveUVs         = owner.PreserveUVs;

                        if(preserveUVsProperty != null &&
                           preserveUVsProperty.boolValue != preserveUVs)
                        {
                            preserveUVsProperty.boolValue = preserveUVs;
                            SOModified                    = true;
                        }

#if UNITY_2017_2_OR_NEWER
                        SerializedProperty stitchLightmapSeamsProperty =
                            meshRendererComponentSO.FindProperty("m_StitchLightmapSeams");

                        bool stitchLightmapSeams = owner.StitchLightmapSeams;

                        if(stitchLightmapSeamsProperty != null
                        && // Note that some alpha/beta versions of 2017.2 had a different name
                           stitchLightmapSeamsProperty.boolValue != stitchLightmapSeams)
                        {
                            stitchLightmapSeamsProperty.boolValue = stitchLightmapSeams;
                            SOModified                            = true;
                        }
#endif

                        if(SOModified)
                        {
                            meshRendererComponentSO.ApplyModifiedProperties();
                        }
                    }
                }
                //*/

#if UNITY_2019_2_OR_NEWER
                ReceiveGI receiveGI = owner.ReceiveGI;

                if(meshRendererComponent &&
                   meshRendererComponent.receiveGI != receiveGI)
                {
                    meshRendererComponent.receiveGI = receiveGI;
                    instance.Dirty                  = true;
                }
#endif

                if(meshRendererComponent &&
                   meshRendererComponent.sharedMaterial != requiredMaterial)
                {
                    meshRendererComponent.sharedMaterial = requiredMaterial;
                    instance.Dirty                       = true;
                }
            }
            else
            {
                if(meshFilterComponent)
                {
                    meshFilterComponent.hideFlags = HideFlags.None;
                    Object.DestroyImmediate(meshFilterComponent);
                    instance.Dirty = true;
                }

                if(meshRendererComponent)
                {
                    meshRendererComponent.hideFlags = HideFlags.None;
                    Object.DestroyImmediate(meshRendererComponent);
                    instance.Dirty = true;
                }

                instance.LightingHashValue    = instance.MeshDescription.geometryHashValue;
                meshFilterComponent           = null;
                meshRendererComponent         = null;
                instance.CachedMeshRendererSO = null;
            }

            instance.CachedMeshFilter   = meshFilterComponent;
            instance.CachedMeshRenderer = meshRendererComponent;

            // TODO:	navmesh specific mesh
            // TODO:	occludee/reflection probe static

            MeshCollider meshColliderComponent = instance.CachedMeshCollider;
            bool         needMeshCollider      = !AreBoundsEmpty(instance) && NeedCollider(instance.RenderSurfaceType);

            if(needMeshCollider)
            {
                if(!meshColliderComponent)
                {
                    meshColliderComponent = gameObject.GetComponent<MeshCollider>();
                }

                if(meshColliderComponent && !meshColliderComponent.enabled)
                {
                    meshColliderComponent.enabled = true;
                }

                if(!meshColliderComponent)
                {
                    meshColliderComponent                 = gameObject.AddComponent<MeshCollider>();
                    meshColliderComponent.gameObject.name = ColliderMeshInstanceName;
                    instance.Dirty                        = true;
                }

                // stops it from rendering wireframe in scene
                if((meshColliderComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
                {
                    meshColliderComponent.hideFlags |= HideFlags.HideInHierarchy;
                }

                PhysicsMaterial currentPhysicsMaterial = instance.PhysicsMaterial ?? owner.DefaultPhysicsMaterial;

                if(meshColliderComponent.sharedMaterial != currentPhysicsMaterial)
                {
                    meshColliderComponent.sharedMaterial = currentPhysicsMaterial;
                    instance.Dirty                       = true;
                }

                bool setToConvex = owner.SetColliderConvex;

                if(meshColliderComponent.convex != setToConvex)
                {
                    meshColliderComponent.convex = setToConvex;
                    instance.Dirty               = true;
                }

#if UNITY_2017_3_OR_NEWER
                MeshColliderCookingOptions cookingOptions = owner.MeshColliderCookingOptions;

                if(meshColliderComponent.cookingOptions != cookingOptions)
                {
                    meshColliderComponent.cookingOptions = cookingOptions;
                    instance.Dirty                       = true;
                }
#endif

                if(instance.RenderSurfaceType == RenderSurfaceType.Trigger ||
                   owner.IsTrigger)
                {
                    if(!meshColliderComponent.isTrigger)
                    {
                        meshColliderComponent.isTrigger = true;
                        instance.Dirty                  = true;
                    }
                }
                else
                {
                    if(meshColliderComponent.isTrigger)
                    {
                        meshColliderComponent.isTrigger = false;
                        instance.Dirty                  = true;
                    }
                }

                if(meshColliderComponent.sharedMesh != instance.SharedMesh)
                {
                    meshColliderComponent.sharedMesh = instance.SharedMesh;
                }

                // .. for some reason this fixes mesh-colliders not being found with ray-casts in the editor?
#if UNITY_EDITOR
                if(instance.Dirty)
                {
                    meshColliderComponent.enabled = false;
                    meshColliderComponent.enabled = true;
                }
#endif
            }
            else
            {
                if(meshColliderComponent)
                {
                    meshColliderComponent.hideFlags = HideFlags.None;
                    Object.DestroyImmediate(meshColliderComponent);
                    instance.Dirty = true;
                }

                meshColliderComponent = null;
            }

            instance.CachedMeshCollider = meshColliderComponent;

            if(!postProcessScene)
            {
#if SHOW_GENERATED_MESHES
				if (instance.Dirty)
					UpdateName(instance);
#else
                /*
                if (needMeshRenderer)
                {
                    if (instance.name != RenderMeshInstanceName)
                        instance.name = RenderMeshInstanceName;
                } else
                if (needMeshCollider)
                {
                    if (instance.name != ColliderMeshInstanceName)
                        instance.name = ColliderMeshInstanceName;
                }
                */
#endif
                instance.Dirty = false;
            }
        }

#if SHOW_GENERATED_MESHES
		private static void UpdateName(GeneratedMeshInstance instance)
		{
			var renderMaterial = instance.RenderMaterial;
			var parentObject = instance.gameObject;

			var builder = new System.Text.StringBuilder();
			builder.Append(instance.RenderSurfaceType);
			builder.Append(' ');
			builder.Append(instance.GetInstanceID());

			if (instance.PhysicsMaterial)
			{
				var physicmaterialName = ((!instance.PhysicsMaterial) ? "default" : instance.PhysicsMaterial.name);
				if (builder.Length > 0) builder.Append(' ');
				builder.AppendFormat(" Physics [{0}]", physicmaterialName);
			}
			if (renderMaterial)
			{
				builder.AppendFormat(" Material [{0} {1}]", renderMaterial.name, renderMaterial.GetInstanceID());
			}

			//builder.AppendFormat(" Key {0}", instance.GenerateKey().GetHashCode());

			var objectName = builder.ToString();
			if (parentObject.name != objectName) parentObject.name = objectName;
			if (instance.SharedMesh &&
				instance.SharedMesh.name != objectName)
				instance.SharedMesh.name = objectName;
		}
#endif

        private static int RefreshModelCounter = 0;

        public static void UpdateHelperSurfaceVisibility(bool force = false)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            //			updateMeshColliderMeshTime = 0.0;
            CSGModel[] models                   = InternalCSGModelManager.Models;
            int        currentRefreshModelCount = 0;

            for(int i = 0; i < models.Length; i++)
            {
                CSGModel model = models[i];

                if(!ModelTraits.IsModelEditable(model))
                {
                    continue;
                }

                GeneratedMeshes generatedMeshes = model.generatedMeshes;

                if(!generatedMeshes || generatedMeshes.owner != model)
                {
                    continue;
                }

                if(force ||
                   RefreshModelCounter == currentRefreshModelCount)
                {
                    UpdateContainerFlags(generatedMeshes);

                    foreach(GeneratedMeshInstance instance in generatedMeshes.MeshInstances)
                    {
                        if(!instance)
                        {
                            ValidateGeneratedMeshesDelayed(generatedMeshes);

                            continue;
                        }

                        Refresh(
                            instance, generatedMeshes.owner, onlyFastRefreshes: !force, skipAssetDatabaseUpdate: false
                        );
                    }
                }

                currentRefreshModelCount++;
            }

            if(RefreshModelCounter < currentRefreshModelCount)
            {
                RefreshModelCounter++;
            }
            else
            {
                RefreshModelCounter = 0;
            }

            UpdateHelperSurfaces();
        }

        private static void AssignLayerToChildren(GameObject gameObject)
        {
            if(!gameObject)
            {
                return;
            }

            int layer = gameObject.layer;

            foreach(Transform transform in gameObject.GetComponentsInChildren<Transform>(true))
            {
                if(transform.GetComponent<CSGNode>())
                {
                    transform.gameObject.layer = layer;
                }
            }
        }

        public static void UpdateGeneratedMeshesVisibility(CSGModel model)
        {
            if(!model.generatedMeshes)
            {
                return;
            }

            UpdateGeneratedMeshesVisibility(model.generatedMeshes, model.ShowGeneratedMeshes);
        }

        public static void UpdateGeneratedMeshesVisibility(GeneratedMeshes container, bool visible)
        {
            if(!ModelTraits.IsModelEditable(container.owner) ||
               ModelTraits.IsDefaultModel(container.owner))
            {
                return;
            }

            GameObject containerGameObject = container.gameObject;

            HideFlags gameObjectFlags;
            HideFlags transformFlags;
#if SHOW_GENERATED_MESHES
			gameObjectFlags = HideFlags.None;
#else
            if(visible)
            {
                gameObjectFlags = HideFlags.HideInInspector;
            }
            else
            {
                gameObjectFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            }
#endif
            transformFlags = gameObjectFlags | HideFlags.NotEditable;

            if(containerGameObject.hideFlags != gameObjectFlags)
            {
                containerGameObject.hideFlags = gameObjectFlags;
            }

            if(container.transform.hideFlags != transformFlags)
            {
                container.transform.hideFlags = transformFlags;
                container.hideFlags           = transformFlags | ComponentHideFlags;
            }
        }

        private static void AutoUpdateRigidBody(GeneratedMeshes container)
        {
            CSGModel   model      = container.owner;
            GameObject gameObject = model.gameObject;

            if(ModelTraits.NeedsRigidBody(model))
            {
                Rigidbody rigidBody = container.CachedRigidBody;

                if(!rigidBody)
                {
                    rigidBody = model.GetComponent<Rigidbody>();
                }

                if(!rigidBody)
                {
                    rigidBody = gameObject.AddComponent<Rigidbody>();
                }

                if(rigidBody.hideFlags != HideFlags.None)
                {
                    rigidBody.hideFlags = HideFlags.None;
                }

                RigidbodyConstraints constraints;
                bool                 isKinematic;
                bool                 useGravity;

                if(ModelTraits.NeedsStaticRigidBody(model))
                {
                    isKinematic = true;
                    useGravity  = false;
                    constraints = RigidbodyConstraints.FreezeAll;
                }
                else
                {
                    isKinematic = false;
                    useGravity  = true;
                    constraints = RigidbodyConstraints.None;
                }

                if(rigidBody.isKinematic != isKinematic)
                {
                    rigidBody.isKinematic = isKinematic;
                }

                if(rigidBody.useGravity != useGravity)
                {
                    rigidBody.useGravity = useGravity;
                }

                if(rigidBody.constraints != constraints)
                {
                    rigidBody.constraints = constraints;
                }

                container.CachedRigidBody = rigidBody;
            }
            else
            {
                Rigidbody rigidBody = container.CachedRigidBody;

                if(!rigidBody)
                {
                    rigidBody = model.GetComponent<Rigidbody>();
                }

                if(rigidBody)
                {
                    rigidBody.hideFlags = HideFlags.None;
                    Object.DestroyImmediate(rigidBody);
                }

                container.CachedRigidBody = null;
            }
        }

        public static void RemoveIfEmpty(GameObject gameObject)
        {
            Component[] allComponents = gameObject.GetComponents<Component>();

            for(int i = 0; i < allComponents.Length; i++)
            {
                if(allComponents[i] is Transform)
                {
                    continue;
                }

                if(allComponents[i] is GeneratedMeshInstance)
                {
                    continue;
                }

                return;
            }

            GameObjectExtensions.Destroy(gameObject);
        }

        private static readonly List<GeneratedMeshInstance> s_foundMeshInstances = new List<GeneratedMeshInstance>();

        public static bool ValidateGeneratedMeshesNow(GeneratedMeshes generatedMeshes, bool skipSiblingCheck = false)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return false;
            }

            if(!generatedMeshes)
            {
                return true;
            }

            if(generatedMeshes.owner && generatedMeshes.gameObject)
            {
                if(!skipSiblingCheck)
                {
                    ValidateModelNow(generatedMeshes.owner, true);

                    if(!generatedMeshes)
                    {
                        return true;
                    }
                }
            }
            else
            {
                GameObjectExtensions.Destroy(generatedMeshes.gameObject);

                return true;
            }

            GameObject generatedMeshesGameObject = generatedMeshes.gameObject;
            Transform  generatedMeshesTransform  = generatedMeshesGameObject.transform;

            s_foundMeshInstances.Clear();

            for(int i = 0; i < generatedMeshesTransform.childCount; i++)
            {
                Transform             meshInstanceTransform = generatedMeshesTransform.GetChild(i);
                GeneratedMeshInstance meshInstance = meshInstanceTransform.GetComponent<GeneratedMeshInstance>();

                if(!meshInstance)
                {
                    if(meshInstanceTransform.gameObject &&
                       meshInstanceTransform.hideFlags != HideFlags.DontSave)
                    {
                        GameObjectExtensions.Destroy(meshInstanceTransform.gameObject);
                    }

                    continue;
                }

                MeshInstanceKey key = meshInstance.GenerateKey();

                if(!generatedMeshes.HasMeshInstance(key))
                {
                    GameObjectExtensions.Destroy(meshInstanceTransform.gameObject);

                    continue;
                }

                /*
                if (meshInstance.RenderSurfaceType == RenderSurfaceType.Normal && !meshInstance.RenderMaterial)
                {
                    GameObjectExtensions.Destroy(meshInstanceTransform.gameObject);
                    continue;
                }
                */
                if(!ValidMeshInstance(meshInstance))
                {
                    GameObjectExtensions.Destroy(meshInstanceTransform.gameObject);

                    continue;
                }

                s_foundMeshInstances.Add(meshInstance);
            }

            generatedMeshes.SetMeshInstances(s_foundMeshInstances);

            if(string.IsNullOrEmpty(generatedMeshesGameObject.name))
            {
                HideFlags flags = generatedMeshesGameObject.hideFlags;

                if(generatedMeshesGameObject.hideFlags != HideFlags.None)
                {
                    generatedMeshesGameObject.hideFlags = HideFlags.None;
                }

                generatedMeshesGameObject.name = MeshContainerName;

                if(generatedMeshesGameObject.hideFlags != flags)
                {
                    generatedMeshesGameObject.hideFlags = flags;
                }
            }

            if(generatedMeshes.owner)
            {
                UpdateGeneratedMeshesVisibility(generatedMeshes, generatedMeshes.owner.ShowGeneratedMeshes);
            }

            if(generatedMeshes.owner)
            {
                Transform modelTransform = generatedMeshes.owner.transform;

                if(generatedMeshesTransform.parent != modelTransform)
                {
                    generatedMeshesTransform.parent.SetParent(modelTransform, true);
                }
            }

            return false;
        }

        public static GeneratedMeshInstance[] GetAllModelMeshInstances(GeneratedMeshes container)
        {
            if(!container.HasMeshInstances)
            {
                return null;
            }

            return container.MeshInstances;
        }

        public static GeneratedMeshInstance GetMeshInstance(
            GeneratedMeshes container, GeneratedMeshDescription meshDescription, ModelSettingsFlags modelSettings
          , RenderSurfaceType renderSurfaceType
        )
        {
            MeshInstanceKey       key      = MeshInstanceKey.GenerateKey(meshDescription);
            GeneratedMeshInstance instance = container.GetMeshInstance(key);

            if(instance && instance.SharedMesh)
            {
                return instance;
            }

            return null;
        }

        public static HelperSurfaceDescription GetHelperSurfaceDescription(
            GeneratedMeshes container, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription
          , RenderSurfaceType renderSurfaceType
        )
        {
            MeshInstanceKey          key      = MeshInstanceKey.GenerateKey(meshDescription);
            HelperSurfaceDescription instance = container.GetHelperSurface(key);

            if(instance != null)
            {
                instance.RenderSurfaceType = renderSurfaceType;

                return instance;
            }

            return CreateHelperSurfaceDescription(container, modelSettings, meshDescription, renderSurfaceType);
        }

        public static bool IsObjectGenerated(Object obj)
        {
            if(!obj)
            {
                return false;
            }

            GameObject gameObject = obj as GameObject;

            if(Equals(gameObject, null))
            {
                return false;
            }

            if(gameObject.name == MeshContainerName)
            {
                return true;
            }

            Transform parent = gameObject.transform.parent;

            if(Equals(parent, null))
            {
                return false;
            }

            return parent.name == MeshContainerName;
        }

#region UpdateTransform

        public static void UpdateTransforms()
        {
            CSGModel[] models = InternalCSGModelManager.Models;

            for(int i = 0; i < models.Length; i++)
            {
                CSGModel model = models[i];

                if(!ModelTraits.IsModelEditable(model))
                {
                    continue;
                }

                UpdateTransform(model.generatedMeshes);
            }
        }

        private static void UpdateTransform(GeneratedMeshes container)
        {
            if(!container || !container.owner)
            {
                return;
            }

            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            // TODO: make sure outlines are updated when models move

            Transform containerTransform = container.transform;

            if(containerTransform.localPosition != MathConstants.zeroVector3 ||
               containerTransform.localRotation != MathConstants.identityQuaternion ||
               containerTransform.localScale != MathConstants.oneVector3)
            {
                containerTransform.localPosition = MathConstants.zeroVector3;
                containerTransform.localRotation = MathConstants.identityQuaternion;
                containerTransform.localScale    = MathConstants.oneVector3;
                SceneToolRenderer.SetOutlineDirty();
            }
        }

#endregion

#region UpdateContainerComponents

        private static readonly List<GameObject> __notfoundGameObjects = new List<GameObject>();

        public static List<GameObject> FindUnusedMeshInstances(
            GeneratedMeshes container,
            HashSet<GeneratedMeshInstance> foundInstances
        )
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return null;
            }

            if(!container || !container.owner)
            {
                return null;
            }

            __notfoundGameObjects.Clear();

            if(container.HasMeshInstances)
            {
                GeneratedMeshInstance[] instances = container.GetComponentsInChildren<GeneratedMeshInstance>(true);

                if(foundInstances == null)
                {
                    for(int i = 0; i < instances.Length; i++)
                    {
                        __notfoundGameObjects.Add(instances[i].gameObject);
                    }
                }
                else
                {
                    for(int i = 0; i < instances.Length; i++)
                    {
                        GeneratedMeshInstance instance = instances[i];

                        if(!foundInstances.Contains(instance))
                        {
                            __notfoundGameObjects.Add(instance.gameObject);

                            continue;
                        }
                    }
                }
            }

            Transform generatedMeshesTransform = container.transform;

            for(int i = 0; i < generatedMeshesTransform.childCount; i++)
            {
                Transform  childTransform  = generatedMeshesTransform.GetChild(i);
                GameObject childGameObject = childTransform.gameObject;

                if(childGameObject.activeSelf)
                {
                    continue;
                }

                __notfoundGameObjects.Add(childGameObject);
            }

            return __notfoundGameObjects;
        }

        private static readonly List<GeneratedMeshInstance> __notfoundInstances = new List<GeneratedMeshInstance>();
        private static MeshInstanceKey[] __removeMeshInstances = new MeshInstanceKey[0];

        public static void UpdateContainerComponents(
            GeneratedMeshes container,
            HashSet<GeneratedMeshInstance> foundInstances,
            HashSet<HelperSurfaceDescription> foundHelperSurfaces
        )
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(!container || !container.owner)
            {
                return;
            }

            if(!container.HasMeshInstances)
            {
                CSGModel prevModel = container ? container.owner : null;

                if(ValidateGeneratedMeshesNow(container) && prevModel)
                {
                    prevModel.forceUpdate = true;
                }
            }

            HashSet<Mesh> oldMeshes = new HashSet<Mesh>();

            foreach(HelperSurfaceDescription helperSurface in container.HelperSurfaces)
            {
                if(helperSurface.SharedMesh)
                {
                    oldMeshes.Add(helperSurface.SharedMesh);
                }
            }

            foreach(GeneratedMeshInstance meshInstance in container.MeshInstances)
            {
                if(meshInstance.SharedMesh)
                {
                    oldMeshes.Add(meshInstance.SharedMesh);
                }
            }

            HashSet<Mesh> newMeshes = new HashSet<Mesh>();

            foreach(HelperSurfaceDescription helperSurface in foundHelperSurfaces)
            {
                if(!helperSurface.SharedMesh)
                {
                    continue;
                }

                newMeshes.Add(helperSurface.SharedMesh);
            }

            foreach(GeneratedMeshInstance meshInstance in foundInstances)
            {
                if(!meshInstance.SharedMesh)
                {
                    continue;
                }

                newMeshes.Add(meshInstance.SharedMesh);
            }

            container.SetHelperSurfaces(foundHelperSurfaces);
            container.SetMeshInstances(foundInstances);

            __notfoundInstances.Clear();
            GeneratedMeshInstance[] instances = container.GetComponentsInChildren<GeneratedMeshInstance>(true);

            if(foundInstances == null)
            {
                __notfoundInstances.AddRange(instances);
            }
            else
            {
                for(int i = 0; i < instances.Length; i++)
                {
                    GeneratedMeshInstance instance = instances[i];

                    if(!foundInstances.Contains(instance))
                    {
                        __notfoundInstances.Add(instance);

                        continue;
                    }

                    container.AddMeshInstance(instance);
                }
            }

            for(int i = 0; i < __notfoundInstances.Count; i++)
            {
                GeneratedMeshInstance meshInstance = __notfoundInstances[i];

                if(meshInstance && meshInstance.gameObject)
                {
                    //var key = meshInstance.GenerateKey();
                    //var keyObj = EditorUtility.InstanceIDToObject(key.SurfaceParameter);
                    GameObjectExtensions.Destroy(meshInstance.gameObject);
                }

                if(__removeMeshInstances.Length < container.MeshInstances.Length)
                {
                    __removeMeshInstances = new MeshInstanceKey[container.MeshInstances.Length];
                }

                int removeMeshInstancesCount = 0;

                foreach(GeneratedMeshInstance item in container.MeshInstances)
                {
                    if(!item ||
                       item == meshInstance)
                    {
                        __removeMeshInstances[removeMeshInstancesCount] = item.GenerateKey();
                        removeMeshInstancesCount++;
                    }
                }

                if(removeMeshInstancesCount > 0)
                {
                    container.RemoveMeshInstances(__removeMeshInstances, removeMeshInstancesCount);
                }
            }

            if(!container.owner)
            {
                return;
            }

            UpdateTransform(container);
        }

#endregion

#region UpdateContainerFlags

        private static void UpdateContainerFlags(GeneratedMeshes generatedMeshes)
        {
            if(!generatedMeshes)
            {
                return;
            }

            if(generatedMeshes.owner)
            {
                if(CSGPrefabUtility.IsPrefabAsset(generatedMeshes.gameObject) ||
                   CSGPrefabUtility.IsPrefabAsset(generatedMeshes.owner.gameObject))
                {
                    return;
                }

                if(!CSGPrefabUtility.IsPrefabInstance(generatedMeshes.gameObject) &&
                   !CSGPrefabUtility.IsPrefabInstance(generatedMeshes.owner.gameObject))
                {
                    Transform ownerTransform = generatedMeshes.owner.transform;

                    if(generatedMeshes.transform.parent != ownerTransform)
                    {
                        generatedMeshes.transform.SetParent(ownerTransform, false);
                    }

                    if(!generatedMeshes)
                    {
                        return;
                    }
                }
            }

            //var isTrigger			= container.owner.IsTrigger;
            //var collidable		= container.owner.HaveCollider || isTrigger;
            GameObject        ownerGameObject     = generatedMeshes.owner.gameObject;
            StaticEditorFlags ownerStaticFlags    = GameObjectUtility.GetStaticEditorFlags(ownerGameObject);
            StaticEditorFlags previousStaticFlags = GameObjectUtility.GetStaticEditorFlags(generatedMeshes.gameObject);

            int containerLayer = ownerGameObject.layer;

            bool showVisibleSurfaces =
                (CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;

            if(ownerStaticFlags != previousStaticFlags
            || !ownerGameObject.CompareTag(generatedMeshes.gameObject.tag)
            || containerLayer != generatedMeshes.gameObject.layer)
            {
                string containerTag = ownerGameObject.tag;

                foreach(GeneratedMeshInstance meshInstance in generatedMeshes.MeshInstances)
                {
                    if(!meshInstance)
                    {
                        continue;
                    }

                    if(meshInstance.RenderSurfaceType == RenderSurfaceType.Normal)
                    {
                        CSGVisibilityUtility.SetGameObjectVisibility(meshInstance.gameObject, showVisibleSurfaces);
                    }

                    StaticEditorFlags oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(meshInstance.gameObject);

                    StaticEditorFlags newStaticFlags = FilterStaticEditorFlags(
                        oldStaticFlags, meshInstance.RenderSurfaceType
                    );

                    foreach(Transform transform in meshInstance.GetComponentsInChildren<Transform>(true))
                    {
                        GameObject gameObject = transform.gameObject;

                        if(oldStaticFlags != newStaticFlags)
                        {
                            GameObjectUtility.SetStaticEditorFlags(gameObject, newStaticFlags);
                        }

                        if(!gameObject.CompareTag(containerTag))
                        {
                            gameObject.tag = containerTag;
                        }

                        if(gameObject.layer != containerLayer)
                        {
                            gameObject.layer = containerLayer;
                        }
                    }
                }
            }

            if(generatedMeshes.owner.NeedAutoUpdateRigidBody)
            {
                AutoUpdateRigidBody(generatedMeshes);
            }
        }

#endregion
    }
}

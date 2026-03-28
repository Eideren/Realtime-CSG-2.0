//#define SHOW_GENERATED_MESHES

using System;
using System.Collections.Generic;
using RealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace InternalRealtimeCSG
{
    internal static partial class MeshInstanceManager
    {
        public const HideFlags COMPONENT_HIDE_FLAGS = HideFlags.None | HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;

        internal const string MESH_CONTAINER_NAME = "[generated-meshes]";
        private const string RENDER_MESH_INSTANCE_NAME = "[generated-render-mesh]";
        private const string COLLIDER_MESH_INSTANCE_NAME = "[generated-collider-mesh]";
        private const string HELPER_MESH_INSTANCE_NAME = "[generated-helper-mesh]";

        public static void Shutdown()
        {
        }

        public static void OnDestroyed()
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

            m_IgnoreValidateModelDelayed = true;

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
                        ValidateModelNow(model);
                    }

                    validateModelWithoutChildren.Clear();
                }
            }
            finally
            {
                m_IgnoreValidateModelDelayed = false;
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

        private static bool m_IgnoreValidateModelDelayed;

        internal static void ValidateModelDelayed(CSGModel model, bool checkChildren = false)
        {
            if(m_IgnoreValidateModelDelayed)
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

            if(!generatedMeshes.enabled)
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
                // due to prefab merging issues or if the user duplicates the gameObject

                for(int i = foundGeneratedMeshes.Length - 1; i >= 0; i--)
                {
                    GeneratedMeshes generatedMeshesComponent = foundGeneratedMeshes[i];

                    if(!generatedMeshesComponent)
                    {
                        continue;
                    }

                    GameObject generatedMeshesGameObject = generatedMeshesComponent.gameObject;

                    // Prefer to keep the GeneratedMesh that is currently used by the Model
                    if(!newGeneratedMeshes || generatedMeshesComponent == prevGeneratedMeshes)
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
            GameObject generatedMeshesObject = new GameObject(MESH_CONTAINER_NAME);
            generatedMeshesObject.SetActive(false);

            GeneratedMeshes generatedMeshes = generatedMeshesObject.AddComponent<GeneratedMeshes>();
            generatedMeshes.owner = model;

            Transform generatedMeshesTransform = generatedMeshesObject.transform;
            generatedMeshesTransform.SetParent(model.transform, false);

            // Retain layer
            generatedMeshesObject.layer = model.gameObject.layer;

            generatedMeshesObject.SetActive(true);

            UpdateGeneratedMeshesVisibility(generatedMeshes, model.showGeneratedMeshes);
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

            if(!checkChildren && model.generatedMeshes && model.generatedMeshes.owner == model)
            {
                return true;
            }

            if(!ModelTraits.IsModelEditable(model) || !model.isActiveAndEnabled)
            {
                return false;
            }

            EnsureOneValidGeneratedMeshesComponent(model);

            return true;
        }

        private static bool ShouldRenderHelperSurface(RenderSurfaceType renderSurfaceType)
        {
            return renderSurfaceType switch
            {
                RenderSurfaceType.Hidden                                      => CSGSettings.ShowHiddenSurfaces
              , RenderSurfaceType.Culled                                      => CSGSettings.ShowCulledSurfaces
              , RenderSurfaceType.Collider                                    => CSGSettings.ShowColliderSurfaces
              , RenderSurfaceType.Trigger                                     => CSGSettings.ShowTriggerSurfaces
              , RenderSurfaceType.ShadowOnly or RenderSurfaceType.CastShadows => CSGSettings.ShowCastShadowsSurfaces
              , RenderSurfaceType.ReceiveShadows                              => CSGSettings.ShowReceiveShadowsSurfaces
              , _                                                             => false
            };
        }

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

            return meshInstance.renderSurfaceType == RenderSurfaceType.Normal;
        }

        public static bool HasRuntimeMesh(GeneratedMeshInstance meshInstance)
        {
            if(!meshInstance)
            {
                return false;
            }

            return meshInstance.renderSurfaceType != RenderSurfaceType.Culled && meshInstance.renderSurfaceType != RenderSurfaceType.ReceiveShadows && meshInstance.renderSurfaceType != RenderSurfaceType.CastShadows && meshInstance.renderSurfaceType != RenderSurfaceType.Hidden;
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

                if(container.owner != model || !container.gameObject)
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
                    RenderSurfaceType renderSurfaceType = helperSurface.renderSurfaceType;

                    if(!helperSurface.sharedMesh || helperSurface.sharedMesh.vertexCount == 0 || !ShouldRenderHelperSurface(renderSurfaceType))
                    {
                        if(helperSurface.gameObject)
                        {
                            helperSurface.gameObject.SetActive(false);
                        }

                        continue;
                    }

                    if(!helperSurface.gameObject || !helperSurface.meshFilter || !helperSurface.meshRenderer || helperSurface.meshFilter.sharedMesh != helperSurface.sharedMesh)
                    {
                        UpdateHelperSurfaceGameObject(container, helperSurface);
                    }

                    if(!helperSurface.hasGeneratedNormals)
                    {
                        helperSurface.sharedMesh.RecalculateNormals();
                        helperSurface.hasGeneratedNormals = true;
                    }

                    helperSurface.gameObject.SetActive(true);
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
            List<GeneratedMeshInstance> meshInstances = SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshInstance>(scene);

            for(int m = 0; m < meshInstances.Count; m++)
            {
                if(!meshInstances[m])
                {
                    continue;
                }

                Transform meshInstanceTransform = meshInstances[m].transform;
                Transform modelTransform        = meshInstanceTransform ? meshInstanceTransform.parent ? meshInstanceTransform.parent.parent : null : null;
                CSGModel  model                 = modelTransform ? modelTransform.GetComponent<CSGModel>() : null;

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

            if(CSGPrefabUtility.AreInPrefabMode())
            {
                PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                ResetScene(currentPrefabStage.scene);
            }
        }

        public static RenderSurfaceType GetSurfaceType(GeneratedMeshDescription meshDescription, ModelSettingsFlags modelSettings)
        {
            if(meshDescription.meshQuery.LayerQuery == LayerUsageFlags.Culled)
            {
                return RenderSurfaceType.Culled;
            }

            switch(meshDescription.meshQuery.LayerParameterIndex)
            {
                case LayerParameterIndex.LayerParameter1:
                {
                    return meshDescription.meshQuery.LayerQuery switch
                    {
                        LayerUsageFlags.RenderReceiveCastShadows                     => RenderSurfaceType.Normal
                      , LayerUsageFlags.RenderReceiveShadows                         => RenderSurfaceType.Normal
                      , LayerUsageFlags.RenderCastShadows                            => RenderSurfaceType.Normal
                      , LayerUsageFlags.Renderable                                   => RenderSurfaceType.Normal
                      , LayerUsageFlags.ReceiveShadows                               => RenderSurfaceType.Hidden
                      , LayerUsageFlags.CastShadows | LayerUsageFlags.ReceiveShadows => RenderSurfaceType.ShadowOnly
                      , LayerUsageFlags.CastShadows                                  => RenderSurfaceType.ShadowOnly
                      , _                                                            => RenderSurfaceType.Culled
                    };
                }
                case LayerParameterIndex.LayerParameter2:
                {
                    return (modelSettings & ModelSettingsFlags.IsTrigger) != 0 ? RenderSurfaceType.Trigger : RenderSurfaceType.Collider;
                }
                case LayerParameterIndex.None:
                {
                    switch(meshDescription.meshQuery.LayerQuery)
                    {
                        case LayerUsageFlags.None:           return RenderSurfaceType.Hidden;
                        case LayerUsageFlags.CastShadows:    return RenderSurfaceType.CastShadows;
                        case LayerUsageFlags.ReceiveShadows: return RenderSurfaceType.ReceiveShadows;
                        case LayerUsageFlags.Collidable:     return (modelSettings & ModelSettingsFlags.IsTrigger) != 0 ? RenderSurfaceType.Trigger : RenderSurfaceType.Collider;
                        default:
                        case LayerUsageFlags.Culled: return RenderSurfaceType.Culled;
                    }
                }
            }

            return RenderSurfaceType.Normal;
        }

        private static GeneratedMeshInstance SafeGetGeneratedMeshInstance(GameObject meshInstanceGameObject)
        {
            return meshInstanceGameObject.TryGetComponent(out GeneratedMeshInstance instance) ? instance : meshInstanceGameObject.AddComponent<GeneratedMeshInstance>();
        }

        private static void RemoveComponent<T>(GameObject gameObject) where T : Component
        {
            if(!gameObject.TryGetComponent(out T component))
            {
                return;
            }

            Object.DestroyImmediate(component);
        }

        private static void RemoveMeshInstanceComponents(GameObject gameObject, GeneratedMeshDescription meshDescription)
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

        private static void GetGameObjectAndGeneratedMeshInstance(List<GameObject> unusedInstances, GeneratedMeshDescription meshDescription, out GameObject meshInstanceGameObject, out GeneratedMeshInstance meshInstance)
        {
            if(unusedInstances == null || unusedInstances.Count == 0)
            {
                meshInstanceGameObject = new GameObject();
                meshInstance           = meshInstanceGameObject.AddComponent<GeneratedMeshInstance>();

                return;
            }

            for(int i = 0; i < unusedInstances.Count; i++)
            {
                if(!unusedInstances[i].TryGetComponent(out GeneratedMeshInstance instance))
                {
                    continue;
                }

                if(meshDescription.meshQuery.LayerParameterIndex == instance.meshDescription.meshQuery.LayerParameterIndex)
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

        public static GeneratedMeshInstance CreateMeshInstance(GeneratedMeshes generatedMeshes, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType, List<GameObject> unusedInstances)
        {
            if(!generatedMeshes || !generatedMeshes.owner)
            {
                return null;
            }

            Transform generatedMeshesTransform = generatedMeshes.transform;

            GetGameObjectAndGeneratedMeshInstance(unusedInstances, meshDescription, out GameObject meshInstanceGameObject, out GeneratedMeshInstance meshInstance);

            meshInstance.Reset();
            meshInstanceGameObject.SetActive(false);
            meshInstanceGameObject.transform.SetParent(generatedMeshesTransform, false);
            meshInstanceGameObject.transform.localPosition = MathConstants.zeroVector3;
            meshInstanceGameObject.transform.localRotation = MathConstants.identityQuaternion;
            meshInstanceGameObject.transform.localScale    = MathConstants.oneVector3;

            StaticEditorFlags containerStaticFlags = GameObjectUtility.GetStaticEditorFlags(generatedMeshes.owner.gameObject);

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

            meshInstance.meshDescription = meshDescription;

            // Our mesh has not been initialized yet, so make sure we reflect that fact
            meshInstance.meshDescription.geometryHashValue = 0;
            meshInstance.meshDescription.surfaceHashValue  = 0;
            meshInstance.meshDescription.vertexCount       = 0;
            meshInstance.meshDescription.indexCount        = 0;

            meshInstance.renderMaterial    = renderMaterial;
            meshInstance.physicsMaterial   = physicsMaterial;
            meshInstance.renderSurfaceType = renderSurfaceType;

            meshInstanceGameObject.SetActive(true);

            Initialize(generatedMeshes, meshInstance);

            return meshInstance;
        }

        public static void UpdateHelperSurfaceGameObject(GeneratedMeshes container, HelperSurfaceDescription helperSurfaceDescription)
        {
            if(!helperSurfaceDescription.gameObject || !helperSurfaceDescription.meshFilter || !helperSurfaceDescription.meshRenderer)
            {
                if(helperSurfaceDescription.gameObject)
                {
                    GameObjectExtensions.Destroy(helperSurfaceDescription.gameObject);
                }

                helperSurfaceDescription.gameObject = new GameObject(HELPER_MESH_INSTANCE_NAME);
                helperSurfaceDescription.gameObject.SetActive(false);
                helperSurfaceDescription.gameObject.transform.SetParent(container.transform, true);
                helperSurfaceDescription.gameObject.transform.localPosition = Vector3.zero;
                helperSurfaceDescription.gameObject.transform.localRotation = Quaternion.identity;
                helperSurfaceDescription.gameObject.transform.localScale    = Vector3.one;
                helperSurfaceDescription.gameObject.hideFlags               = HideFlags.DontSave;
                helperSurfaceDescription.meshFilter                         = helperSurfaceDescription.gameObject.AddComponent<MeshFilter>();
                helperSurfaceDescription.meshRenderer                       = helperSurfaceDescription.gameObject.AddComponent<MeshRenderer>();
            }

            if(helperSurfaceDescription.meshFilter.sharedMesh != helperSurfaceDescription.sharedMesh)
            {
                helperSurfaceDescription.meshFilter.sharedMesh = helperSurfaceDescription.sharedMesh;
            }

            Material sharedMaterial = MaterialUtility.GetSurfaceMaterial(helperSurfaceDescription.renderSurfaceType);

            if(helperSurfaceDescription.meshRenderer.sharedMaterial != sharedMaterial)
            {
                helperSurfaceDescription.meshRenderer.sharedMaterial = sharedMaterial;
            }
        }

        public static HelperSurfaceDescription CreateHelperSurfaceDescription(GeneratedMeshes container, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType)
        {
            HelperSurfaceDescription instance = new HelperSurfaceDescription
            {
                renderSurfaceType = renderSurfaceType
              , meshDescription   = meshDescription
            };

            // Our mesh has not been initialized yet, so make sure we reflect that fact
            instance.meshDescription.geometryHashValue = 0;
            instance.meshDescription.surfaceHashValue  = 0;
            instance.meshDescription.vertexCount       = 0;
            instance.meshDescription.indexCount        = 0;

            Initialize(container, instance);

            return instance;
        }

        internal static void ClearOrCreateMesh(string baseName, bool editorOnly, ref bool hasGeneratedNormals, ref Mesh sharedMesh)
        {
            bool recreateMeshes = CSGPrefabUtility.IsPartOfAsset(sharedMesh);

            hasGeneratedNormals = false;

            if(!recreateMeshes && sharedMesh)
            {
                if(!CSGProjectSettings.Instance.saveMeshesInSceneFiles)
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

            if(!CSGProjectSettings.Instance.saveMeshesInSceneFiles)
            {
                sharedMesh.hideFlags |= HideFlags.DontSaveInEditor;
            }
        }

        public static bool UsesLightmapUVs(CSGModel model)
        {
            StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);

            return (staticFlags & StaticEditorFlags.ContributeGI) == StaticEditorFlags.ContributeGI;
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
                angleError = Mathf.Clamp(model.angleError, CSGModel.MIN_ANGLE_ERROR, CSGModel.MAX_ANGLE_ERROR)
              , areaError  = Mathf.Clamp(model.areaError, CSGModel.MIN_AREA_ERROR, CSGModel.MAX_AREA_ERROR)
              , hardAngle  = model.hardAngle
              , packMargin = model.packMargin / 1024.0f
            };

            foreach(GeneratedMeshInstance instance in container.MeshInstances)
            {
                if(!instance)
                {
                    continue;
                }

                if(!instance.sharedMesh)
                {
                    instance.FindMissingSharedMesh();

                    if(!instance.sharedMesh)
                    {
                        continue;
                    }
                }

                GenerateLightmapUVsForInstance(instance, model, uvGenerationSettings);
            }
        }

        private static void GenerateLightmapUVsForInstance(GeneratedMeshInstance instance, CSGModel model, UnwrapParam param)
        {
            MeshRenderer meshRendererComponent = instance.cachedMeshRenderer;

            if(!meshRendererComponent)
            {
                GameObject gameObject = instance.gameObject;
                meshRendererComponent         = gameObject.GetComponent<MeshRenderer>();
                instance.cachedMeshRendererSO = null;
            }

            if(!meshRendererComponent)
            {
                return;
            }

            meshRendererComponent.realtimeLightmapIndex = -1;
            meshRendererComponent.lightmapIndex         = -1;

            Vector3[] oldVertices = instance.sharedMesh.vertices;

            if(oldVertices.Length == 0)
            {
                return;
            }

            Mesh tempMesh = instance.sharedMesh.Clone();
            instance.sharedMesh = tempMesh;

            Object pingObject = model;

            if(model.showGeneratedMeshes)
            {
                pingObject = instance;
            }

            // TODO: make logging optional
            Debug.Log($"Generating lightmap UVs (by Unity) for the mesh {instance.name} of the Model named \"{model.name}\"\n", pingObject);

            double lightmapGenerationTime = EditorApplication.timeSinceStartup;
            MeshUtility.Optimize(instance.sharedMesh);
            Unwrapping.GenerateSecondaryUVSet(instance.sharedMesh, param);
            lightmapGenerationTime = EditorApplication.timeSinceStartup - lightmapGenerationTime;

            Debug.Log($"\tUV generation in {lightmapGenerationTime * 1000} ms\n", model);

            EditorSceneManager.MarkSceneDirty(instance.gameObject.scene);
            instance.lightingHashValue = instance.meshDescription.geometryHashValue;
            instance.hasUV2            = true;
        }

        private static bool NeedToGenerateLightmapUVsForInstance(GeneratedMeshInstance instance)
        {
            return !instance.hasUV2 && instance.renderSurfaceType == RenderSurfaceType.Normal;
        }

        private static bool AreBoundsEmpty(GeneratedMeshInstance instance)
        {
            return
                Mathf.Abs(instance.sharedMesh.bounds.size.x) <= MathConstants.EQUALITY_EPSILON
             || Mathf.Abs(instance.sharedMesh.bounds.size.y) <= MathConstants.EQUALITY_EPSILON
             || Mathf.Abs(instance.sharedMesh.bounds.size.z) <= MathConstants.EQUALITY_EPSILON;
        }

        private static bool NeedCollider(RenderSurfaceType surfaceType)
        {
            return surfaceType == RenderSurfaceType.Collider || surfaceType == RenderSurfaceType.Trigger;
        }

        private static bool NeedMeshRenderer(RenderSurfaceType renderSurfaceType)
        {
            return renderSurfaceType == RenderSurfaceType.Normal || renderSurfaceType == RenderSurfaceType.ShadowOnly;
        }

        private static StaticEditorFlags FilterStaticEditorFlags(StaticEditorFlags modelStaticFlags, RenderSurfaceType renderSurfaceType)
        {
            if(!NeedMeshRenderer(renderSurfaceType))
            {
                return modelStaticFlags;
            }

            StaticEditorFlags meshStaticFlags = modelStaticFlags;

            bool walkable = renderSurfaceType !=
                RenderSurfaceType.Hidden &&
                renderSurfaceType != RenderSurfaceType.ShadowOnly &&
                renderSurfaceType != RenderSurfaceType.Culled &&
                renderSurfaceType != RenderSurfaceType.Trigger;

            if(!walkable) // TODO: fix navigation flags
            {
                meshStaticFlags &= ~StaticEditorFlags.NavigationStatic;
            }

            // This fixes a bug in 2018.3 where it tries to generate lightmaps for ShadowOnly surfaces ..
            // .. but then rage quits because it doesn't have any normals
            if(renderSurfaceType == RenderSurfaceType.ShadowOnly)
            {
                meshStaticFlags &= ~(StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
            }

            return meshStaticFlags & modelStaticFlags;
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

                Refresh(instance, model, onlyFastRefreshes: false);
                ClearUVs(instance);
            }
        }

        public static void ClearUVs(GeneratedMeshInstance instance)
        {
            MeshRenderer meshRendererComponent = instance.cachedMeshRenderer;

            if(meshRendererComponent)
            {
                meshRendererComponent.realtimeLightmapIndex = -1;
                meshRendererComponent.lightmapIndex         = -1;
            }

            instance.lightingHashValue = 0;
            instance.hasUV2            = false;
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

                Refresh(instance, model, postProcessScene, onlyFastRefreshes);
            }
        }

        public static void Refresh(GeneratedMeshInstance instance, CSGModel owner, bool postProcessScene = false, bool onlyFastRefreshes = true)
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

            if(!instance.sharedMesh)
            {
                return;
            }

            // Update the flags
            RenderSurfaceType oldRenderSurfaceType = instance.renderSurfaceType;
            instance.renderSurfaceType = GetSurfaceType(instance.meshDescription, owner.settings);
            instance.dirty             = instance.dirty || oldRenderSurfaceType != instance.renderSurfaceType;

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

            const HideFlags meshInstanceFlags = HideFlags.DontSaveInBuild;
            const HideFlags transformFlags    = HideFlags.HideInInspector;
            const HideFlags gameObjectFlags   = HideFlags.None;

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

            bool showVisibleSurfaces = instance.renderSurfaceType != RenderSurfaceType.Normal || (CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;

            CSGVisibilityUtility.SetGameObjectVisibility(gameObject, showVisibleSurfaces);

            if(!instance.enabled)
            {
                instance.enabled = true;
            }

            // Update navigation on mesh
            StaticEditorFlags oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            StaticEditorFlags newStaticFlags = FilterStaticEditorFlags(GameObjectUtility.GetStaticEditorFlags(owner.gameObject), instance.renderSurfaceType);

            if(newStaticFlags != oldStaticFlags)
            {
                GameObjectUtility.SetStaticEditorFlags(gameObject, newStaticFlags);
            }

            MeshFilter   meshFilterComponent   = instance.cachedMeshFilter;
            MeshRenderer meshRendererComponent = instance.cachedMeshRenderer;

            bool needMeshRenderer = NeedMeshRenderer(instance.renderSurfaceType);

            if(needMeshRenderer)
            {
                if(!meshRendererComponent)
                {
                    meshRendererComponent         = gameObject.GetComponent<MeshRenderer>();
                    instance.cachedMeshRendererSO = null;
                }

                if(!meshFilterComponent)
                {
                    meshFilterComponent = gameObject.GetComponent<MeshFilter>();

                    if(!meshFilterComponent)
                    {
                        meshFilterComponent           = gameObject.AddComponent<MeshFilter>();
                        instance.cachedMeshRendererSO = null;
                        instance.dirty                = true;
                    }
                }

                bool ownerReceiveShadows = true;

                ShadowCastingMode shadowCastingMode = owner.IsTwoSidedShadows ? ShadowCastingMode.TwoSided : ShadowCastingMode.On;

                if(instance.renderSurfaceType == RenderSurfaceType.ShadowOnly)
                {
                    shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }

                switch(instance.meshDescription.meshQuery.LayerQuery)
                {
                    case LayerUsageFlags.RenderReceiveCastShadows:
                    {
                        break;
                    }
                    case LayerUsageFlags.RenderReceiveShadows:
                    {
                        shadowCastingMode = ShadowCastingMode.Off;

                        break;
                    }
                    case LayerUsageFlags.RenderCastShadows:
                    {
                        ownerReceiveShadows = false;

                        break;
                    }
                    case LayerUsageFlags.Renderable:
                    {
                        shadowCastingMode   = ShadowCastingMode.Off;
                        ownerReceiveShadows = false;

                        break;
                    }
                    case LayerUsageFlags.CastShadows:
                    {
                        shadowCastingMode   = ShadowCastingMode.ShadowsOnly;
                        ownerReceiveShadows = false;

                        break;
                    }
                }

                Material requiredMaterial = instance.renderMaterial;

                if(shadowCastingMode == ShadowCastingMode.ShadowsOnly)
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
                    meshRendererComponent.gameObject.name = RENDER_MESH_INSTANCE_NAME;
                    instance.cachedMeshRendererSO         = null;
                    instance.dirty                        = true;
                    // we don't actually want the unity style of rendering a wireframe
                    // for our meshes, so we turn it off
                    EditorUtility.SetSelectedRenderState(meshRendererComponent, EditorSelectedRenderState.Hidden);
                }

                if((meshFilterComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
                {
                    meshFilterComponent.hideFlags |= HideFlags.HideInHierarchy;
                }

                if((meshRendererComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
                {
                    meshRendererComponent.hideFlags |= HideFlags.HideInHierarchy;
                }

                if(instance.renderSurfaceType != RenderSurfaceType.ShadowOnly)
                {
                    if(instance.hasUV2 && instance.lightingHashValue != instance.meshDescription.geometryHashValue && meshRendererComponent)
                    {
                        instance.resetUVTime = Time.realtimeSinceStartup;

                        if(instance.hasUV2)
                        {
                            ClearUVs(instance);
                        }
                    }

                    if(owner.AutoRebuildUVs || postProcessScene)
                    {
                        if((float.IsPositiveInfinity(instance.resetUVTime) || Time.realtimeSinceStartup - instance.resetUVTime > 2.0f) && NeedToGenerateLightmapUVsForModel(owner))
                        {
                            GenerateLightmapUVsForModel(owner);
                        }
                    }
                }

                if(!postProcessScene &&
                   meshFilterComponent.sharedMesh != instance.sharedMesh)
                {
                    meshFilterComponent.sharedMesh = instance.sharedMesh;
                }

                if(meshRendererComponent &&
                   meshRendererComponent.shadowCastingMode != shadowCastingMode)
                {
                    meshRendererComponent.shadowCastingMode = shadowCastingMode;
                    instance.dirty                          = true;
                }

                if(meshRendererComponent &&
                   meshRendererComponent.receiveShadows != ownerReceiveShadows)
                {
                    meshRendererComponent.receiveShadows = ownerReceiveShadows;
                    instance.dirty                       = true;
                }

                //*
                if(!onlyFastRefreshes)
                {
                    SerializedObject meshRendererComponentSO = instance.cachedMeshRendererSO as SerializedObject;

                    if(meshRendererComponentSO == null)
                    {
                        if(meshRendererComponent)
                        {
                            instance.cachedMeshRendererSO = meshRendererComponentSO = new SerializedObject(meshRendererComponent);
                        }
                    }
                    else if(!meshRendererComponent)
                    {
                        instance.cachedMeshRendererSO = meshRendererComponentSO = null;
                    }

                    if(meshRendererComponentSO != null)
                    {
                        bool soModified = false;
                        meshRendererComponentSO.Update();

                        SerializedProperty scaleInLightmapProperty = meshRendererComponentSO.FindProperty("m_ScaleInLightmap");

                        float scaleInLightmap = owner.scaleInLightmap;

                        if(scaleInLightmapProperty != null && scaleInLightmapProperty.floatValue != scaleInLightmap)
                        {
                            scaleInLightmapProperty.floatValue = scaleInLightmap;
                            soModified                         = true;
                        }

                        SerializedProperty autoUVMaxDistanceProperty = meshRendererComponentSO.FindProperty("m_AutoUVMaxDistance");

                        float autoUVMaxDistance = owner.autoUVMaxDistance;

                        if(autoUVMaxDistanceProperty != null && autoUVMaxDistanceProperty.floatValue != autoUVMaxDistance)
                        {
                            autoUVMaxDistanceProperty.floatValue = autoUVMaxDistance;
                            soModified                           = true;
                        }

                        SerializedProperty autoUVMaxAngleProperty = meshRendererComponentSO.FindProperty("m_AutoUVMaxAngle");

                        float autoUVMaxAngle = owner.autoUVMaxAngle;

                        if(autoUVMaxAngleProperty != null && autoUVMaxAngleProperty.floatValue != autoUVMaxAngle)
                        {
                            autoUVMaxAngleProperty.floatValue = autoUVMaxAngle;
                            soModified                        = true;
                        }

                        SerializedProperty ignoreNormalsProperty = meshRendererComponentSO.FindProperty("m_IgnoreNormalsForChartDetection");

                        bool ignoreNormals = owner.IgnoreNormals;

                        if(ignoreNormalsProperty != null && ignoreNormalsProperty.boolValue != ignoreNormals)
                        {
                            ignoreNormalsProperty.boolValue = ignoreNormals;
                            soModified                      = true;
                        }

                        SerializedProperty minimumChartSizeProperty = meshRendererComponentSO.FindProperty("m_MinimumChartSize");

                        int minimumChartSize = owner.minimumChartSize;

                        if(minimumChartSizeProperty != null && minimumChartSizeProperty.intValue != minimumChartSize)
                        {
                            minimumChartSizeProperty.intValue = minimumChartSize;
                            soModified                        = true;
                        }

                        SerializedProperty preserveUVsProperty = meshRendererComponentSO.FindProperty("m_PreserveUVs");
                        bool               preserveUVs         = owner.PreserveUVs;

                        if(preserveUVsProperty != null && preserveUVsProperty.boolValue != preserveUVs)
                        {
                            preserveUVsProperty.boolValue = preserveUVs;
                            soModified                    = true;
                        }

                        SerializedProperty stitchLightmapSeamsProperty = meshRendererComponentSO.FindProperty("m_StitchLightmapSeams");

                        bool stitchLightmapSeams = owner.StitchLightmapSeams;

                        // Note that some alpha/beta versions of 2017.2 had a different name
                        if(stitchLightmapSeamsProperty != null && stitchLightmapSeamsProperty.boolValue != stitchLightmapSeams)
                        {
                            stitchLightmapSeamsProperty.boolValue = stitchLightmapSeams;
                            soModified                            = true;
                        }

                        if(soModified)
                        {
                            meshRendererComponentSO.ApplyModifiedProperties();
                        }
                    }
                }

                ReceiveGI receiveGI = owner.receiveGI;

                if(meshRendererComponent && meshRendererComponent.receiveGI != receiveGI)
                {
                    meshRendererComponent.receiveGI = receiveGI;
                    instance.dirty                  = true;
                }

                if(meshRendererComponent && meshRendererComponent.sharedMaterial != requiredMaterial)
                {
                    meshRendererComponent.sharedMaterial = requiredMaterial;
                    instance.dirty                       = true;
                }
            }
            else
            {
                if(meshFilterComponent)
                {
                    meshFilterComponent.hideFlags = HideFlags.None;
                    Object.DestroyImmediate(meshFilterComponent);
                    instance.dirty = true;
                }

                if(meshRendererComponent)
                {
                    meshRendererComponent.hideFlags = HideFlags.None;
                    Object.DestroyImmediate(meshRendererComponent);
                    instance.dirty = true;
                }

                instance.lightingHashValue    = instance.meshDescription.geometryHashValue;
                meshFilterComponent           = null;
                meshRendererComponent         = null;
                instance.cachedMeshRendererSO = null;
            }

            instance.cachedMeshFilter   = meshFilterComponent;
            instance.cachedMeshRenderer = meshRendererComponent;

            // TODO: navmesh specific mesh
            // TODO: occludee/reflection probe static

            MeshCollider meshColliderComponent = instance.cachedMeshCollider;
            bool         needMeshCollider      = !AreBoundsEmpty(instance) && NeedCollider(instance.renderSurfaceType);

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
                    meshColliderComponent.gameObject.name = COLLIDER_MESH_INSTANCE_NAME;
                    instance.dirty                        = true;
                }

                // stops it from rendering wireframe in scene
                if((meshColliderComponent.hideFlags & HideFlags.HideInHierarchy) == 0)
                {
                    meshColliderComponent.hideFlags |= HideFlags.HideInHierarchy;
                }

                PhysicsMaterial currentPhysicsMaterial = instance.physicsMaterial ?? owner.defaultPhysicsMaterial;

                if(meshColliderComponent.sharedMaterial != currentPhysicsMaterial)
                {
                    meshColliderComponent.sharedMaterial = currentPhysicsMaterial;
                    instance.dirty                       = true;
                }

                bool setToConvex = owner.SetColliderConvex;

                if(meshColliderComponent.convex != setToConvex)
                {
                    meshColliderComponent.convex = setToConvex;
                    instance.dirty               = true;
                }

                MeshColliderCookingOptions cookingOptions = owner.meshColliderCookingOptions;

                if(meshColliderComponent.cookingOptions != cookingOptions)
                {
                    meshColliderComponent.cookingOptions = cookingOptions;
                    instance.dirty                       = true;
                }

                if(instance.renderSurfaceType == RenderSurfaceType.Trigger || owner.IsTrigger)
                {
                    if(!meshColliderComponent.isTrigger)
                    {
                        meshColliderComponent.isTrigger = true;
                        instance.dirty                  = true;
                    }
                }
                else
                {
                    if(meshColliderComponent.isTrigger)
                    {
                        meshColliderComponent.isTrigger = false;
                        instance.dirty                  = true;
                    }
                }

                if(meshColliderComponent.sharedMesh != instance.sharedMesh)
                {
                    meshColliderComponent.sharedMesh = instance.sharedMesh;
                }

                // .. for some reason this fixes mesh-colliders not being found with ray-casts in the editor?
                if(instance.dirty)
                {
                    meshColliderComponent.enabled = false;
                    meshColliderComponent.enabled = true;
                }
            }
            else
            {
                if(meshColliderComponent)
                {
                    meshColliderComponent.hideFlags = HideFlags.None;
                    Object.DestroyImmediate(meshColliderComponent);
                    instance.dirty = true;
                }

                meshColliderComponent = null;
            }

            instance.cachedMeshCollider = meshColliderComponent;

            if(!postProcessScene)
            {
                instance.dirty = false;
            }
        }

        private static int m_RefreshModelCounter;

        public static void UpdateHelperSurfaceVisibility(bool force = false)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

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

                if(force || m_RefreshModelCounter == currentRefreshModelCount)
                {
                    UpdateContainerFlags(generatedMeshes);

                    foreach(GeneratedMeshInstance instance in generatedMeshes.MeshInstances)
                    {
                        if(!instance)
                        {
                            ValidateGeneratedMeshesDelayed(generatedMeshes);

                            continue;
                        }

                        Refresh(instance, generatedMeshes.owner, onlyFastRefreshes: !force);
                    }
                }

                currentRefreshModelCount++;
            }

            if(m_RefreshModelCounter < currentRefreshModelCount)
            {
                m_RefreshModelCounter++;
            }
            else
            {
                m_RefreshModelCounter = 0;
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

            UpdateGeneratedMeshesVisibility(model.generatedMeshes, model.showGeneratedMeshes);
        }

        public static void UpdateGeneratedMeshesVisibility(GeneratedMeshes container, bool visible)
        {
            if(!ModelTraits.IsModelEditable(container.owner) || ModelTraits.IsDefaultModel(container.owner))
            {
                return;
            }

            GameObject containerGameObject = container.gameObject;
            HideFlags  gameObjectFlags;

            if(visible)
            {
                gameObjectFlags = HideFlags.HideInInspector;
            }
            else
            {
                gameObjectFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            }

            HideFlags transformFlags = gameObjectFlags | HideFlags.NotEditable;

            if(containerGameObject.hideFlags != gameObjectFlags)
            {
                containerGameObject.hideFlags = gameObjectFlags;
            }

            if(container.transform.hideFlags != transformFlags)
            {
                container.transform.hideFlags = transformFlags;
                container.hideFlags           = transformFlags | COMPONENT_HIDE_FLAGS;
            }
        }

        private static void AutoUpdateRigidBody(GeneratedMeshes container)
        {
            CSGModel   model      = container.owner;
            GameObject gameObject = model.gameObject;

            if(ModelTraits.NeedsRigidBody(model))
            {
                Rigidbody rigidBody = container.cachedRigidBody;

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

                container.cachedRigidBody = rigidBody;
            }
            else
            {
                Rigidbody rigidBody = container.cachedRigidBody;

                if(!rigidBody)
                {
                    rigidBody = model.GetComponent<Rigidbody>();
                }

                if(rigidBody)
                {
                    rigidBody.hideFlags = HideFlags.None;
                    Object.DestroyImmediate(rigidBody);
                }

                container.cachedRigidBody = null;
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

        private static readonly List<GeneratedMeshInstance> foundMeshInstances = new List<GeneratedMeshInstance>();

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

            foundMeshInstances.Clear();

            for(int i = 0; i < generatedMeshesTransform.childCount; i++)
            {
                Transform             meshInstanceTransform = generatedMeshesTransform.GetChild(i);
                GeneratedMeshInstance meshInstance          = meshInstanceTransform.GetComponent<GeneratedMeshInstance>();

                if(!meshInstance)
                {
                    if(meshInstanceTransform.gameObject && meshInstanceTransform.hideFlags != HideFlags.DontSave)
                    {
                        GameObjectExtensions.Destroy(meshInstanceTransform.gameObject);
                    }

                    continue;
                }

                MeshInstanceKey key = meshInstance.GenerateKey();

                if(!generatedMeshes.HasMeshInstance(key) || !ValidMeshInstance(meshInstance))
                {
                    GameObjectExtensions.Destroy(meshInstanceTransform.gameObject);

                    continue;
                }

                foundMeshInstances.Add(meshInstance);
            }

            generatedMeshes.SetMeshInstances(foundMeshInstances);

            if(string.IsNullOrEmpty(generatedMeshesGameObject.name))
            {
                HideFlags flags = generatedMeshesGameObject.hideFlags;

                if(generatedMeshesGameObject.hideFlags != HideFlags.None)
                {
                    generatedMeshesGameObject.hideFlags = HideFlags.None;
                }

                generatedMeshesGameObject.name = MESH_CONTAINER_NAME;

                if(generatedMeshesGameObject.hideFlags != flags)
                {
                    generatedMeshesGameObject.hideFlags = flags;
                }
            }

            if(generatedMeshes.owner)
            {
                UpdateGeneratedMeshesVisibility(generatedMeshes, generatedMeshes.owner.showGeneratedMeshes);
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
            return !container.HasMeshInstances ? null : container.MeshInstances;
        }

        public static GeneratedMeshInstance GetMeshInstance(GeneratedMeshes container, GeneratedMeshDescription meshDescription)
        {
            MeshInstanceKey       key      = MeshInstanceKey.GenerateKey(meshDescription);
            GeneratedMeshInstance instance = container.GetMeshInstance(key);

            if(instance && instance.sharedMesh)
            {
                return instance;
            }

            return null;
        }

        public static HelperSurfaceDescription GetHelperSurfaceDescription(GeneratedMeshes container, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType)
        {
            MeshInstanceKey          key      = MeshInstanceKey.GenerateKey(meshDescription);
            HelperSurfaceDescription instance = container.GetHelperSurface(key);

            if(instance != null)
            {
                instance.renderSurfaceType = renderSurfaceType;

                return instance;
            }

            return CreateHelperSurfaceDescription(container, meshDescription, renderSurfaceType);
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

            if(containerTransform.localPosition != MathConstants.zeroVector3 || containerTransform.localRotation != MathConstants.identityQuaternion || containerTransform.localScale != MathConstants.oneVector3)
            {
                containerTransform.localPosition = MathConstants.zeroVector3;
                containerTransform.localRotation = MathConstants.identityQuaternion;
                containerTransform.localScale    = MathConstants.oneVector3;

                SceneToolRenderer.SetOutlineDirty();
            }
        }

#endregion

#region UpdateContainerComponents

        private static readonly List<GameObject> notfoundGameObjects = new List<GameObject>();

        public static List<GameObject> FindUnusedMeshInstances(GeneratedMeshes container, HashSet<GeneratedMeshInstance> foundInstances)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return null;
            }

            if(!container || !container.owner)
            {
                return null;
            }

            notfoundGameObjects.Clear();

            if(container.HasMeshInstances)
            {
                GeneratedMeshInstance[] instances = container.GetComponentsInChildren<GeneratedMeshInstance>(true);

                if(foundInstances == null)
                {
                    for(int i = 0; i < instances.Length; i++)
                    {
                        notfoundGameObjects.Add(instances[i].gameObject);
                    }
                }
                else
                {
                    for(int i = 0; i < instances.Length; i++)
                    {
                        GeneratedMeshInstance instance = instances[i];

                        if(!foundInstances.Contains(instance))
                        {
                            notfoundGameObjects.Add(instance.gameObject);
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

                notfoundGameObjects.Add(childGameObject);
            }

            return notfoundGameObjects;
        }

        private static readonly List<GeneratedMeshInstance> notfoundInstances = new List<GeneratedMeshInstance>();
        private static MeshInstanceKey[] m_RemoveMeshInstances = Array.Empty<MeshInstanceKey>();

        public static void UpdateContainerComponents(GeneratedMeshes container, HashSet<GeneratedMeshInstance> foundInstances, HashSet<HelperSurfaceDescription> foundHelperSurfaces)
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

            container.SetHelperSurfaces(foundHelperSurfaces);
            container.SetMeshInstances(foundInstances);

            notfoundInstances.Clear();
            GeneratedMeshInstance[] instances = container.GetComponentsInChildren<GeneratedMeshInstance>(true);

            if(foundInstances == null)
            {
                notfoundInstances.AddRange(instances);
            }
            else
            {
                for(int i = 0; i < instances.Length; i++)
                {
                    GeneratedMeshInstance instance = instances[i];

                    if(!foundInstances.Contains(instance))
                    {
                        notfoundInstances.Add(instance);

                        continue;
                    }

                    container.AddMeshInstance(instance);
                }
            }

            for(int i = 0; i < notfoundInstances.Count; i++)
            {
                GeneratedMeshInstance meshInstance = notfoundInstances[i];

                if(meshInstance && meshInstance.gameObject)
                {
                    GameObjectExtensions.Destroy(meshInstance.gameObject);
                }

                if(m_RemoveMeshInstances.Length < container.MeshInstances.Length)
                {
                    m_RemoveMeshInstances = new MeshInstanceKey[container.MeshInstances.Length];
                }

                int removeMeshInstancesCount = 0;

                foreach(GeneratedMeshInstance item in container.MeshInstances)
                {
                    if(!item || item == meshInstance)
                    {
                        m_RemoveMeshInstances[removeMeshInstancesCount] = item.GenerateKey();
                        removeMeshInstancesCount++;
                    }
                }

                if(removeMeshInstancesCount > 0)
                {
                    container.RemoveMeshInstances(m_RemoveMeshInstances, removeMeshInstancesCount);
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
                if(CSGPrefabUtility.IsPrefabAsset(generatedMeshes.gameObject) || CSGPrefabUtility.IsPrefabAsset(generatedMeshes.owner.gameObject))
                {
                    return;
                }

                if(!CSGPrefabUtility.IsPrefabInstance(generatedMeshes.gameObject) && !CSGPrefabUtility.IsPrefabInstance(generatedMeshes.owner.gameObject))
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

            GameObject        ownerGameObject     = generatedMeshes.owner.gameObject;
            StaticEditorFlags ownerStaticFlags    = GameObjectUtility.GetStaticEditorFlags(ownerGameObject);
            StaticEditorFlags previousStaticFlags = GameObjectUtility.GetStaticEditorFlags(generatedMeshes.gameObject);

            int containerLayer = ownerGameObject.layer;

            bool showVisibleSurfaces = (CSGSettings.VisibleHelperSurfaces & HelperSurfaceFlags.ShowVisibleSurfaces) != 0;

            if(ownerStaticFlags != previousStaticFlags || !ownerGameObject.CompareTag(generatedMeshes.gameObject.tag) || containerLayer != generatedMeshes.gameObject.layer)
            {
                string containerTag = ownerGameObject.tag;

                foreach(GeneratedMeshInstance meshInstance in generatedMeshes.MeshInstances)
                {
                    if(!meshInstance)
                    {
                        continue;
                    }

                    if(meshInstance.renderSurfaceType == RenderSurfaceType.Normal)
                    {
                        CSGVisibilityUtility.SetGameObjectVisibility(meshInstance.gameObject, showVisibleSurfaces);
                    }

                    StaticEditorFlags oldStaticFlags = GameObjectUtility.GetStaticEditorFlags(meshInstance.gameObject);

                    StaticEditorFlags newStaticFlags = FilterStaticEditorFlags(oldStaticFlags, meshInstance.renderSurfaceType);

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

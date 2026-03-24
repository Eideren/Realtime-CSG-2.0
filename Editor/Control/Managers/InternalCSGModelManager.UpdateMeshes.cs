using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal partial class InternalCSGModelManager
    {
#region ClearMeshInstances

        public static void ClearMeshInstances()
        {
            MeshInstanceManager.Reset();
            MeshInstanceManager.DestroyAllMeshInstances();
        }

#endregion

#region GetMeshTypesForModel

        // TODO: simplify this. do we really need an array of these to do this?

        private static readonly MeshQuery[] renderAndColliderMeshTypes =
        {
            // MeshRenderers
            // Note: shadow-only surface could have masks, potentially could have shader that generates them based on existing vertex information
            new MeshQuery(
                LayerUsageFlags.CastShadows
              , LayerUsageFlags.RenderCastShadows
              , LayerParameterIndex.RenderMaterial
              , VertexChannelFlags.All
            )
          , new MeshQuery(
                LayerUsageFlags.Renderable
              , LayerUsageFlags.RenderReceiveCastShadows
              , LayerParameterIndex.RenderMaterial
              , VertexChannelFlags.All
            )
          , new MeshQuery(
                LayerUsageFlags.RenderCastShadows
              , LayerUsageFlags.RenderReceiveCastShadows
              , LayerParameterIndex.RenderMaterial
              , VertexChannelFlags.All
            )
          , new MeshQuery(
                LayerUsageFlags.RenderReceiveShadows
              , LayerUsageFlags.RenderReceiveCastShadows
              , LayerParameterIndex.RenderMaterial
              , VertexChannelFlags.All
            )
          , new MeshQuery(
                LayerUsageFlags.RenderReceiveCastShadows
              , LayerUsageFlags.RenderReceiveCastShadows
              , LayerParameterIndex.RenderMaterial
              , VertexChannelFlags.All
            )

            // MeshColliders
          , new MeshQuery(
                LayerUsageFlags.Collidable
              , parameterIndex: LayerParameterIndex.PhysicsMaterial
            )

            // Helper surfaces (editor only)
          , new MeshQuery(LayerUsageFlags.None, LayerUsageFlags.Renderable)
            // hidden surfaces
          , new MeshQuery(LayerUsageFlags.CastShadows), new MeshQuery(LayerUsageFlags.ReceiveShadows), new MeshQuery(LayerUsageFlags.Culled)
        };

        private static readonly MeshQuery[] renderOnlyTypes =
        {
            // MeshRenderers
            // Note: shadow-only surface could have masks, potentially could have shader that generates them based on existing vertex information
            new MeshQuery(LayerUsageFlags.CastShadows, LayerUsageFlags.RenderCastShadows, LayerParameterIndex.RenderMaterial, VertexChannelFlags.All), new MeshQuery(LayerUsageFlags.Renderable, LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial, VertexChannelFlags.All), new MeshQuery(LayerUsageFlags.RenderCastShadows, LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial, VertexChannelFlags.All), new MeshQuery(LayerUsageFlags.RenderReceiveShadows, LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial, VertexChannelFlags.All), new MeshQuery(LayerUsageFlags.RenderReceiveCastShadows, LayerUsageFlags.RenderReceiveCastShadows, LayerParameterIndex.RenderMaterial, VertexChannelFlags.All)

            // Helper surfaces (editor only)
          , new MeshQuery(LayerUsageFlags.None, LayerUsageFlags.Renderable), // hidden surfaces
            new MeshQuery(LayerUsageFlags.CastShadows)
          , new MeshQuery(LayerUsageFlags.ReceiveShadows), new MeshQuery(LayerUsageFlags.Culled)
        };

        private static readonly MeshQuery[] colliderMeshTypes =
        {
            // MeshColliders
            new MeshQuery(LayerUsageFlags.Collidable, parameterIndex: LayerParameterIndex.PhysicsMaterial),

            // Helper surfaces (editor only)
            new MeshQuery(LayerUsageFlags.None, LayerUsageFlags.Renderable)
          , // hidden surfaces
            new MeshQuery(LayerUsageFlags.Culled)
        };

        private static readonly MeshQuery[] emptyMeshTypes =
        {
            // Helper surfaces (editor only)
            new MeshQuery(LayerUsageFlags.None, LayerUsageFlags.Renderable), // hidden surfaces
            new MeshQuery(LayerUsageFlags.Culled)
        };

        public static MeshQuery[] GetMeshTypesForModel(CSGModel model)
        {
            MeshQuery[] query;

            if(!model.HaveCollider)
            {
                query = !model.IsRenderable ? emptyMeshTypes : renderOnlyTypes;
            }
            else
            {
                query = !model.IsRenderable ? colliderMeshTypes : renderAndColliderMeshTypes;
            }

            return query;
        }

#endregion

#region UpdateModelSettings

        public static bool UpdateModelSettings()
        {
            bool forceHierarchyUpdate = false;

            for(int i = 0; i < Models.Length; i++)
            {
                CSGModel model = Models[i];

                if(!ModelTraits.IsModelEditable(model))
                {
                    continue;
                }

                bool modelModified = false;
                bool invertedWorld = model.InvertedWorld;

                if(invertedWorld)
                {
                    if(model.infiniteBrush == null)
                    {
                        CSGBrush   infiniteBrush = null;
                        CSGBrush[] childNodes    = model.GetComponentsInChildren<CSGBrush>();

                        for(int c = 0; c < childNodes.Length; c++)
                        {
                            if(childNodes[c].Flags == BrushFlags.InfiniteBrush)
                            {
                                if(infiniteBrush != null)
                                {
                                    Object.DestroyImmediate(childNodes[c]);
                                }
                                else
                                {
                                    infiniteBrush = childNodes[c];
                                }
                            }
                        }

                        if(infiniteBrush == null)
                        {
                            GameObject gameObject = new GameObject("*hidden infinite brush*");
                            gameObject.hideFlags = MeshInstanceManager.COMPONENT_HIDE_FLAGS;
                            gameObject.transform.SetParent(model.transform, false);
                            infiniteBrush = gameObject.AddComponent<CSGBrush>();
                        }

                        model.infiniteBrush       = infiniteBrush;
                        model.infiniteBrush.Flags = BrushFlags.InfiniteBrush;

                        modelModified = true;
                    }

                    if(model.infiniteBrush.transform.GetSiblingIndex() != 0)
                    {
                        model.infiniteBrush.transform.SetSiblingIndex(0);
                        modelModified = true;
                    }
                }
                else
                {
                    if(model.infiniteBrush)
                    {
                        if(model.infiniteBrush.gameObject)
                        {
                            Object.DestroyImmediate(model.infiniteBrush.gameObject);
                        }

                        model.infiniteBrush = null;
                        modelModified       = true;
                    }
                }

                if(modelModified)
                {
                    CSGBrush[] childBrushes = model.GetComponentsInChildren<CSGBrush>();

                    for(int j = 0; j < childBrushes.Length; j++)
                    {
                        if(!childBrushes[j] ||
                           childBrushes[j].ControlMesh == null)
                        {
                            continue;
                        }

                        childBrushes[j].ControlMesh.Generation++;
                    }

                    forceHierarchyUpdate = true;
                }
            }

            return forceHierarchyUpdate;
        }

#endregion

#region RefreshMeshes

        public static void RefreshMeshes()
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(SkipCheckForChanges)
            {
                return;
            }

            lock(lockObj)
            {
                UpdateModelSettings();
                UpdateMeshes();
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
            }
        }

#endregion

#region UpdateRemoteMeshes

        public static void UpdateRemoteMeshes()
        {
            if(External != null && External.updateAllModelMeshes != null)
            {
                External.updateAllModelMeshes();
            }
        }

#endregion

#region UpdateMeshes

#region DoForcedMeshUpdate

        private static bool m_ForcedUpdateRequired;

        public static void DoForcedMeshUpdate()
        {
            m_ForcedUpdateRequired = true;
        }

#endregion

        internal static int MeshGeneration;

#region GetModelMesh

        internal static double GetMeshInstanceTime;
        internal static double GetModelMeshesTime;
        internal static double UpdateMeshTime;

        private const int MAX_VERTEX_COUNT = 65000;

        private static bool UpdateMesh(CSGModel model, string baseName, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType, ref bool outputHasGeneratedNormals, ref Mesh sharedMesh, bool editorOnly = false)
        {
            // create our arrays on the C# side with the correct size
            double startGetModelMeshesTime = EditorApplication.timeSinceStartup;

            GeneratedMeshContents generatedMesh = External.getModelMesh(model.modelNodeID, meshDescription);

            if(generatedMesh == null)
            {
                return false;
            }

            GetModelMeshesTime += EditorApplication.timeSinceStartup - startGetModelMeshesTime;

            UpdateMesh(baseName, generatedMesh, meshDescription, renderSurfaceType, ref outputHasGeneratedNormals, ref sharedMesh, editorOnly);

            if(!CSGProjectSettings.Instance.saveMeshesInSceneFiles)
            {
                // If is prefab scene, allow save
                if(string.IsNullOrEmpty(model.gameObject.scene.path) && sharedMesh != null)
                {
                    sharedMesh.hideFlags &= ~HideFlags.DontSaveInEditor;
                }
            }

            return true;
        }

        public static bool UpdateMesh(string baseName, GeneratedMeshContents generatedMesh, GeneratedMeshDescription inputMeshDescription, RenderSurfaceType renderSurfaceType, ref bool outputHasGeneratedNormals, ref Mesh sharedMesh, bool editorOnly)
        {
            double startUpdateMeshTime = EditorApplication.timeSinceStartup;

            MeshInstanceManager.ClearOrCreateMesh(baseName, editorOnly, ref outputHasGeneratedNormals, ref sharedMesh);

            // finally, we start filling our (sub)meshes using the C# arrays
            sharedMesh.vertices = generatedMesh.positions;

            if(generatedMesh.normals != null)
            {
                sharedMesh.normals = generatedMesh.normals;
            }

            if(generatedMesh.tangents != null)
            {
                sharedMesh.tangents = generatedMesh.tangents;
            }

            if(generatedMesh.uv0 != null)
            {
                sharedMesh.uv = generatedMesh.uv0;
            }

            // fill the mesh with the given indices
            sharedMesh.SetTriangles(generatedMesh.indices, 0, true);

            UpdateMeshTime += EditorApplication.timeSinceStartup - startUpdateMeshTime;

            if(renderSurfaceType != RenderSurfaceType.Normal)
            {
                outputHasGeneratedNormals = (inputMeshDescription.meshQuery.UsedVertexChannels & VertexChannelFlags.Normal) != 0;
            }

            return true;
        }

        private static bool ValidateMesh(GeneratedMeshDescription meshDescription)
        {
            int indexCount  = meshDescription.indexCount;
            int vertexCount = meshDescription.vertexCount;

            if(vertexCount <= 0 || vertexCount > MAX_VERTEX_COUNT || indexCount <= 0)
            {
                if(vertexCount > 0 && indexCount > 0)
                {
                    Debug.LogError($"Mesh has too many vertices (vertexCount > {MAX_VERTEX_COUNT})");
                }

                return false;
            }

            return true;
        }

        private static bool TryGetMeshInstance(GeneratedMeshes meshContainer, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType, out GeneratedMeshInstance meshInstance)
        {
            double startGetMeshInstanceTime = EditorApplication.timeSinceStartup;

            meshInstance = MeshInstanceManager.GetMeshInstance(meshContainer, meshDescription);

            GetMeshInstanceTime += EditorApplication.timeSinceStartup - startGetMeshInstanceTime;

            if(meshInstance && meshDescription == meshInstance.MeshDescription && meshInstance.SharedMesh && meshInstance.IsValid())
            {
                return true;
            }

            meshInstance = null;

            return true;
        }

        private static GeneratedMeshInstance GenerateMeshInstance(GeneratedMeshes meshContainer, CSGModel model, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType, List<GameObject> unusedInstances)
        {
            GeneratedMeshInstance meshInstance = MeshInstanceManager.CreateMeshInstance(meshContainer, meshDescription, renderSurfaceType, unusedInstances);

            string baseName = renderSurfaceType switch
            {
                RenderSurfaceType.Collider => "Collider"
              , RenderSurfaceType.Normal   => "Renderable"
              , _                          => "Unknown"
            };

            meshInstance.MeshDescription = meshDescription;

            return !UpdateMesh(model, baseName, meshInstance.MeshDescription, meshInstance.RenderSurfaceType, ref meshInstance.HasGeneratedNormals, ref meshInstance.SharedMesh) ? null : meshInstance;
        }

        private static readonly string[] renderSurfaceMeshNames = { "Renderable helper surface", "Hidden helper surface", "Culled helper surface", "ShadowOnly helper surface", "Collider helper surface", "Trigger helper surface", "CastShadows helper surface", "ReceiveShadows helper surface" };

        private static bool TryGetHelperSurfaceDescription(GeneratedMeshes meshContainer, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType, out HelperSurfaceDescription helperSurfaceDescription)
        {
            double startGetMeshInstanceTime = EditorApplication.timeSinceStartup;

            helperSurfaceDescription = MeshInstanceManager.GetHelperSurfaceDescription(meshContainer, meshDescription, renderSurfaceType);

            GetMeshInstanceTime += EditorApplication.timeSinceStartup - startGetMeshInstanceTime;

            if(helperSurfaceDescription != null && meshDescription == helperSurfaceDescription.MeshDescription && helperSurfaceDescription.SharedMesh && helperSurfaceDescription.IsValid())
            {
                return true;
            }

            helperSurfaceDescription = null;

            return true;
        }

        private static HelperSurfaceDescription GenerateHelperSurfaceDescription(GeneratedMeshes meshContainer, CSGModel model, ModelSettingsFlags modelSettings, GeneratedMeshDescription meshDescription, RenderSurfaceType renderSurfaceType)
        {
            HelperSurfaceDescription helperSurfaceDescription = MeshInstanceManager.CreateHelperSurfaceDescription(meshContainer, meshDescription, renderSurfaceType);

            helperSurfaceDescription.MeshDescription = meshDescription;

            return !UpdateMesh(model, renderSurfaceMeshNames[(int)renderSurfaceType], helperSurfaceDescription.MeshDescription, helperSurfaceDescription.RenderSurfaceType, ref helperSurfaceDescription.HasGeneratedNormals, ref helperSurfaceDescription.SharedMesh, true) ? null : helperSurfaceDescription;
        }

#endregion

#region RemoveForcedUpdates

        public static void RemoveForcedUpdates()
        {
            int modelCount = Models.Length;

            if(modelCount == 0)
            {
                return;
            }

            for(int i = 0; i < modelCount; i++)
            {
                CSGModel model = Models[i];
                model.forceUpdate = false;
            }
        }

#endregion

        private static readonly HashSet<HelperSurfaceDescription> foundHelperSurfaces = new HashSet<HelperSurfaceDescription>();

        private static readonly HashSet<GeneratedMeshInstance> foundGeneratedMeshInstance = new HashSet<GeneratedMeshInstance>();

        private static readonly HashSet<int> unfoundMeshInstances = new HashSet<int>();
        private static GeneratedMeshDescription[] m_MeshDescriptions = Array.Empty<GeneratedMeshDescription>();

        private static bool m_InUpdateMeshes;

        public static bool UpdateMeshes(StringBuilder text = null, bool forceUpdate = false)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return false;
            }

            if(m_InUpdateMeshes)
            {
                return false;
            }

            MeshInstanceManager.Update();

            double unityMeshUpdates       = 0.0;
            double getMeshDescriptionTime = 0.0;

            GetMeshInstanceTime = 0.0;
            GetModelMeshesTime  = 0.0;
            UpdateMeshTime      = 0.0;

            m_InUpdateMeshes = true;

            try
            {
                if(External == null)
                {
                    return false;
                }

                if(m_ForcedUpdateRequired)
                {
                    forceUpdate            = true;
                    m_ForcedUpdateRequired = false;
                }

                int modelCount = Models.Length;

                if(modelCount == 0)
                {
                    return false;
                }

                for(int i = 0; i < modelCount; i++)
                {
                    CSGModel model = Models[i];

                    if(!forceUpdate &&
                       !model.forceUpdate)
                    {
                        continue;
                    }

                    if(!ModelTraits.IsModelEditable(model))
                    {
                        continue;
                    }

                    External.setDirty(model.modelNodeID);
                    model.forceUpdate = false;
                }

                // update the model meshes
                if(!External.updateAllModelMeshes())
                {
                    return false; // nothing to do
                }

                MeshGeneration++;
                bool haveUpdates = forceUpdate;

                for(int i = 0; i < modelCount; i++)
                {
                    CSGModel model = Models[i];

                    if(!new CSGTreeNode { nodeID = model.modelNodeID }.Dirty)
                    {
                        continue;
                    }

                    GeneratedMeshes meshContainer = model.generatedMeshes;

                    if(!meshContainer)
                    {
                        continue;
                    }

                    EnsureInitialized(model);

                    double startGetMeshDescriptionTime = EditorApplication.timeSinceStartup;
                    bool   needToUpdateMeshes          = External.getMeshDescriptions(model, ref m_MeshDescriptions);

                    getMeshDescriptionTime += EditorApplication.timeSinceStartup - startGetMeshDescriptionTime;

                    if(!ModelTraits.IsModelEditable(model))
                    {
                        continue;
                    }

                    if(!needToUpdateMeshes)
                    {
                        continue;
                    }

                    foundHelperSurfaces.Clear();
                    foundGeneratedMeshInstance.Clear();
                    unfoundMeshInstances.Clear();

                    double startUnityMeshUpdates = EditorApplication.timeSinceStartup;

                    for(int meshIndex = 0; meshIndex < m_MeshDescriptions.Length; meshIndex++)
                    {
                        if(!ValidateMesh(m_MeshDescriptions[meshIndex]))
                        {
                            continue;
                        }

                        haveUpdates = true;

                        RenderSurfaceType renderSurfaceType = MeshInstanceManager.GetSurfaceType(m_MeshDescriptions[meshIndex], model.Settings);

                        if(renderSurfaceType == RenderSurfaceType.Normal || renderSurfaceType == RenderSurfaceType.ShadowOnly || renderSurfaceType == RenderSurfaceType.Collider || renderSurfaceType == RenderSurfaceType.Trigger)
                        {
                            if(TryGetMeshInstance(meshContainer, model.Settings, m_MeshDescriptions[meshIndex], renderSurfaceType, out GeneratedMeshInstance meshInstance))
                            {
                                if(meshInstance == null)
                                {
                                    unfoundMeshInstances.Add(meshIndex);

                                    continue;
                                }

                                foundGeneratedMeshInstance.Add(meshInstance);
                            }
                        }

                        if(renderSurfaceType != RenderSurfaceType.Normal)
                        {
                            if(TryGetHelperSurfaceDescription(meshContainer, model.Settings, m_MeshDescriptions[meshIndex], renderSurfaceType, out HelperSurfaceDescription helperSurface))
                            {
                                if(helperSurface != null)
                                {
                                    foundHelperSurfaces.Add(helperSurface);
                                }
                                else
                                {
                                    unfoundMeshInstances.Add(meshIndex);
                                }
                            }
                        }
                    }

                    List<GameObject> unusedInstances = MeshInstanceManager.FindUnusedMeshInstances(meshContainer, foundGeneratedMeshInstance);

                    foreach(int meshIndex in unfoundMeshInstances)
                    {
                        haveUpdates = true;

                        RenderSurfaceType renderSurfaceType = MeshInstanceManager.GetSurfaceType(m_MeshDescriptions[meshIndex], model.Settings);

                        if(renderSurfaceType == RenderSurfaceType.Normal || renderSurfaceType == RenderSurfaceType.ShadowOnly || renderSurfaceType == RenderSurfaceType.Collider || renderSurfaceType == RenderSurfaceType.Trigger)
                        {
                            GeneratedMeshInstance meshInstance = GenerateMeshInstance(meshContainer, model, model.Settings, m_MeshDescriptions[meshIndex], renderSurfaceType, unusedInstances);

                            if(meshInstance != null)
                            {
                                foundGeneratedMeshInstance.Add(meshInstance);
                            }
                        }

                        if(renderSurfaceType != RenderSurfaceType.Normal)
                        {
                            HelperSurfaceDescription helperSurface = GenerateHelperSurfaceDescription(meshContainer, model, model.Settings, m_MeshDescriptions[meshIndex], renderSurfaceType);

                            foundHelperSurfaces.Add(helperSurface);
                        }
                    }

                    MeshInstanceManager.UpdateContainerComponents(meshContainer, foundGeneratedMeshInstance, foundHelperSurfaces);

                    unityMeshUpdates += EditorApplication.timeSinceStartup - startUnityMeshUpdates;
                }

                if(haveUpdates)
                {
                    MeshInstanceManager.UpdateHelperSurfaceVisibility(true);
                }

                if(text != null) // TODO: make logging optional
                {
                    text.AppendFormat(CultureInfo.InvariantCulture, "All mesh generation {0:F} ms + retrieving {1:F} ms + Unity mesh updates {2:F} ms + overhead {3:F} ms. ", getMeshDescriptionTime * 1000, GetModelMeshesTime * 1000, UpdateMeshTime * 1000, (unityMeshUpdates - (GetModelMeshesTime + UpdateMeshTime)) * 1000);
                }

                return true;
            }
            finally
            {
                m_InUpdateMeshes = false;
            }
        }

#endregion

#region Destroy meshes

        /// <summary>
        /// Used to destroy any helper surface meshes in the current scene. Useful because they don't get destroyed otherwise, and stay in the Scene file.
        /// </summary>
        [MenuItem("Edit/Realtime-CSG/Destroy All Helper Surface Meshes In Scene")]
        public static void DestroyAllHelperSurfaceCSGMeshes()
        {
            List<Mesh> allMeshesInScene = new List<Mesh>();

            for(int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);

                if(!s.isLoaded)
                {
                    continue;
                }

                foreach(GameObject go in s.GetRootGameObjects())
                {
                    foreach(MeshFilter filter in go.GetComponentsInChildren<MeshFilter>(true))
                    {
                        allMeshesInScene.Add(filter.sharedMesh);
                    }
                }
            }

            // regex matches all possible names for the helper surfaces that we can generate
            Regex nameMatch = new Regex($"^{Regex.Escape("<")}(?:{string.Join("|", renderSurfaceMeshNames.Select(Regex.Escape))}) generated -?[0-9]+{Regex.Escape(">")}$", RegexOptions.Compiled);

            foreach(Mesh mesh in allMeshesInScene)
            {
                if(nameMatch.IsMatch(mesh.name))
                {
                    Undo.DestroyObjectImmediate(mesh);
                }
            }
        }

#endregion Destroy meshes
    }
}

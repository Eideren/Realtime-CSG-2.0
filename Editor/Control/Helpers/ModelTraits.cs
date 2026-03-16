using System.Collections.Generic;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal static class ModelTraits
    {
        private static bool IsObjectPartOfAsset(string assetPath, Object obj)
        {
            if(!obj)
            {
                return false;
            }

            string objPath = AssetDatabase.GetAssetPath(obj);

            return assetPath == objPath;
        }

        private static bool CanAddObjectToAsset(string assetPath, Object obj)
        {
            if(!obj ||
               string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string objPath = AssetDatabase.GetAssetPath(obj);

            if(assetPath == objPath)
            {
                return false;
            }

            return string.IsNullOrEmpty(objPath) || assetPath == objPath;
        }

        private static bool CanRemoveObjectFromAsset(
            string assetPath, Object obj, bool ignoreWhenPartOfOtherAsset = false
        )
        {
            if(!obj)
            {
                return false;
            }

            string objPath = AssetDatabase.GetAssetPath(obj);

            if(string.IsNullOrEmpty(objPath))
            {
                if(!ignoreWhenPartOfOtherAsset)
                {
                    Debug.LogError("Cannot remove object from prefab because it is not owned by any asset");
                }

                return false;
            }

            if(objPath != assetPath)
            {
                if(!ignoreWhenPartOfOtherAsset)
                {
                    Debug.LogError("Trying to remove asset that is owned by another prefab");
                }

                return false;
            }

            return true;
        }

        private static readonly HashSet<Mesh> oldMeshes = new HashSet<Mesh>();
        private static readonly HashSet<Mesh> newMeshes = new HashSet<Mesh>();

        public static void OnPrefabSaving(GameObject obj)
        {
            GeneratedMeshes[] foundGeneratedMeshes = obj.GetComponentsInChildren<GeneratedMeshes>();

            if(foundGeneratedMeshes.Length == 0)
            {
                return;
            }

            CSGModel defaultModel = InternalCSGModelManager.GetDefaultCSGModelForObject(obj.transform);
            newMeshes.Clear();

            foreach(GeneratedMeshes generatedMeshesInstance in foundGeneratedMeshes)
            {
                if(generatedMeshesInstance.owner == defaultModel)
                {
                    continue;
                }

                foreach(GeneratedMeshInstance generatedMeshInstance in generatedMeshesInstance
                           .GetComponentsInChildren<GeneratedMeshInstance>())
                {
                    if(!generatedMeshInstance) // possible when it's deleted in a prefab
                    {
                        continue;
                    }

                    if(generatedMeshInstance.SharedMesh)
                    {
                        newMeshes.Add(generatedMeshInstance.SharedMesh);
                    }
                }

                foreach(HelperSurfaceDescription helperSurface in generatedMeshesInstance.HelperSurfaces)
                {
                    newMeshes.Add(helperSurface.SharedMesh);
                }
            }

            GameObject asset     = CSGPrefabUtility.GetPrefabAsset(obj);
            string     assetPath = AssetDatabase.GetAssetPath(asset);

            Object[] assetObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach(Object assetObject in assetObjects)
            {
                Mesh mesh = assetObject as Mesh;

                if(!mesh)
                {
                    continue;
                }

                oldMeshes.Add(mesh);
            }

            if(newMeshes.Count == 0 && oldMeshes.Count == 0)
            {
                return;
            }

            // We might be modifying a prefab, in which case we need to store meshes inside it that belong to it
            AssetDatabase.StartAssetEditing();

            try
            {
                foreach(Mesh oldMesh in oldMeshes)
                {
                    if(newMeshes.Contains(oldMesh))
                    {
                        continue;
                    }

                    if(CanRemoveObjectFromAsset(assetPath, oldMesh, true))
                    {
                        AssetDatabase.RemoveObjectFromAsset(oldMesh);
                    }
                }

                foreach(Mesh newMesh in newMeshes)
                {
                    if(oldMeshes.Contains(newMesh))
                    {
                        if(IsObjectPartOfAsset(assetPath, newMesh))
                        {
                            continue;
                        }
                    }

                    if(CanAddObjectToAsset(assetPath, newMesh))
                    {
                        AssetDatabase.AddObjectToAsset(newMesh, asset);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        public static bool IsDefaultModel(CSGModel model)
        {
            return (model.hideFlags & MeshInstanceManager.COMPONENT_HIDE_FLAGS) == MeshInstanceManager.COMPONENT_HIDE_FLAGS
              ||
                (model.hideFlags & HideFlags.DontSave) == HideFlags.DontSave ||
                (model.hideFlags & HideFlags.DontSaveInEditor) == HideFlags.DontSaveInEditor;
        }

        public static bool IsModelEditable(CSGModel model)
        {
            if(!model)
            {
                return false;
            }

            // Nested prefabs do not play nice with editing in scene, so it's best to edit them in prefab mode themselves
            // We only allow editing of nested prefabs when in prefab mode
            return CSGPrefabUtility.AreInPrefabMode()
                ? CSGPrefabUtility.IsEditedInPrefabMode(model)
                : model.isActiveAndEnabled;
        }

        public static bool IsModelSelectable(CSGModel model)
        {
            if(!model || !model.isActiveAndEnabled)
            {
                return false;
            }

            return ((1 << model.gameObject.layer) & Tools.visibleLayers) != 0;
        }

        public static bool WillModelRender(CSGModel model)
        {
            // Is our model valid ...?
            if(!IsModelEditable(model))
            {
                return false;
            }

            if(CSGPrefabUtility.AreInPrefabMode())
            {
                if(!CSGPrefabUtility.IsEditedInPrefabMode(model))
                {
                    return false;
                }
            }

            // Does our model have a meshRenderer?
            if(model.IsRenderable)
            {
                // Otherwise, it is always rendering (with the exception of manually hidden surfaces)
                return true;
            }

            // Is it a trigger and are we showing triggers?
            if(model.IsTrigger && CSGSettings.ShowTriggerSurfaces)
            {
                return true;
            }

            // Check if it's a collider and are we showing colliders?
            if(model.HaveCollider && CSGSettings.ShowColliderSurfaces)
            {
                return true;
            }

            // Otherwise see if we're showing surfaces culled by the CSG process ...
            return CSGSettings.ShowCulledSurfaces;
        }

        public static RenderSurfaceType GetModelSurfaceType(CSGModel model)
        {
            if(model.IsRenderable) // if it's renderable then it's already being rendered using a MeshRenderer
            {
                // except if it's a shadow only surface, it has a MeshRenderer, but is not usually visible ...
                return RenderSurfaceType.Normal;
            }

            if(model.IsTrigger)
            {
                return RenderSurfaceType.Trigger;
            }

            return model.HaveCollider ? RenderSurfaceType.Collider : RenderSurfaceType.Culled;
        }

        public static bool NeedsRigidBody(CSGModel model)
        {
            if(!IsModelEditable(model))
            {
                return false;
            }

            bool              collidable       = model.HaveCollider;
            bool              isTrigger        = collidable && model.IsTrigger;
            StaticEditorFlags ownerStaticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);

            bool batchingstatic = (ownerStaticFlags & StaticEditorFlags.BatchingStatic)
             == StaticEditorFlags.BatchingStatic;

            return (batchingstatic || collidable) && !isTrigger;
        }

        public static bool NeedsStaticRigidBody(CSGModel model)
        {
            if(!IsModelEditable(model))
            {
                return false;
            }

            StaticEditorFlags ownerStaticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);

            bool batchingstatic = (ownerStaticFlags & StaticEditorFlags.BatchingStatic)
             == StaticEditorFlags.BatchingStatic;

            return batchingstatic;
        }
    }
}

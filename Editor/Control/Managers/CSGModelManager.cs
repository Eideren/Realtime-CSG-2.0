using System;
using System.Collections.Generic;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    public static class CSGModelManager
    {
        public static bool AllowInEditorPlayMode = false;

        public static bool IsInPlayMode
        {
            get => !AllowInEditorPlayMode && (UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode);
        }

        public static void ForceRebuild()
        {
            if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            InternalCSGModelManager.ForceRebuildAll();
            InternalCSGModelManager.OnHierarchyModified();
            InternalCSGModelManager.OnHierarchyModified();
            NativeMethodBindings.RebuildAll();
            InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
        }

        public static void BuildLightmapUvs(bool force = false)
        {
            for(int m = 0; m < InternalCSGModelManager.Models.Length; m++)
            {
                if(!InternalCSGModelManager.Models[m])
                {
                    continue;
                }

                if(!force && !MeshInstanceManager.NeedToGenerateLightmapUVsForModel(InternalCSGModelManager.Models[m]))
                {
                    continue;
                }

                MeshInstanceManager.GenerateLightmapUVsForModel(InternalCSGModelManager.Models[m]);
            }
        }

        public static void EnsureBuildFinished()
        {
            InternalCSGModelManager.CheckForChanges(true);
        }

        public static GameObject[] GetModelMeshes(CSGModel model)
        {
            GeneratedMeshes         meshContainer = model.generatedMeshes;
            GeneratedMeshInstance[] meshInstances = MeshInstanceManager.GetAllModelMeshInstances(meshContainer);

            if(meshInstances == null)
            {
                return Array.Empty<GameObject>();
            }

            List<GameObject> gameObjects = new List<GameObject>();

            for(int i = 0; i < meshInstances.Length; i++)
            {
                if(!meshInstances[i] ||
                   meshInstances[i].renderSurfaceType != RenderSurfaceType.Normal)
                {
                    continue;
                }

                gameObjects.Add(meshInstances[i].gameObject);
            }

            return gameObjects.ToArray();
        }

        public static CSGModel[] GetAllModels()
        {
            return InternalCSGModelManager.Models;
        }
    }
}

using InternalRealtimeCSG;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    internal partial class InternalCSGModelManager
    {
        private static bool m_IsInitialized;
        private static readonly object lockObj = new object();

        internal static NativeMethods External;

#region Clear

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            BrushOutlineManager.ClearOutlines();
            ClearRegistration();
            ClearCaches();
            m_IsHierarchyModified = true;
        }

#endregion

#region InitOnNewScene

        public static void InitOnNewScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if(External == null || External.resetCSG == null)
            {
                NativeMethodBindings.RegisterUnityMethods();
                NativeMethodBindings.RegisterExternalMethods();
            }

            if(External == null)
            {
                return;
            }

            if(RegisterAllComponents())
            {
                External.resetCSG();
            }
        }

#endregion

#region Shutdown

        public static void Shutdown()
        {
            sceneStates.Clear();

            ClearRegistration();
            ClearCaches();

            m_IsInitialized = false;
        }

#endregion

#region UndoRedoPerformed

        internal static bool IgnoreMaterials = false;

        public static void UndoRedoPerformed()
        {
            BrushOutlineManager.ClearOutlines();

            CheckForChanges(true);

            if(!IgnoreMaterials)
            {
                foreach(CSGBrush brush in Brushes)
                {
                    if(brush.shape != null)
                    {
                        ShapeUtility.CheckMaterials(brush.shape);
                    }
                }

                foreach(CSGBrush brush in Brushes)
                {
                    CheckSurfaceModifications(brush, true);
                }
            }
        }

#endregion

#region CheckForChanges

        public static bool SkipCheckForChanges = false;

        public static void CheckForChanges(bool forceHierarchyUpdate = false)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(!forceHierarchyUpdate && SkipCheckForChanges)
            {
                return;
            }

            lock(lockObj)
            {
                if(!m_IsInitialized)
                {
                    RegisterAllComponents();
                    forceHierarchyUpdate = true;
                    m_IsInitialized      = true;

                    OnHierarchyModified();
                    UpdateRemoteMeshes();
                }

                forceHierarchyUpdate = UpdateModelSettings() || forceHierarchyUpdate;

                // unfortunately this is the only reliable way I could find
                // to determine when a transform is modified, either in
                // the inspector or the scene.
                CheckTransformChanged(forceHierarchyUpdate);

                if(m_IsHierarchyModified || forceHierarchyUpdate)
                {
                    OnHierarchyModified();
                    OnHierarchyModified();
                    m_IsHierarchyModified = false;
                }

                UpdateMeshes();
                MeshInstanceManager.UpdateHelperSurfaceVisibility();
            }
        }

#endregion

#region ForceRebuildAll

        public static void ForceRebuildAll()
        {
            Clear();

            if(External == null || External.resetCSG == null)
            {
                NativeMethodBindings.RegisterUnityMethods();
                NativeMethodBindings.RegisterExternalMethods();
            }

            ClearMeshInstances();

            if(RegisterAllComponents())
            {
                External.resetCSG();
            }
        }

#endregion

#if UNITY_EDITOR

        [InitializeOnEnterPlayMode]
        public static void OnEnterPlayMode()
        {
            // If saving meshes to scene files, we don't need to dynamically rebuild on scene changes
            if(CSGProjectSettings.Instance.saveMeshesInSceneFiles)
            {
                return;
            }

            if(!EnsureExternalMethodsPopulated())
            {
                return;
            }

            EditorApplication.playModeStateChanged += OnPlayModeChange;
            SceneManager.sceneLoaded               += SceneLoaded;
        }

        private static bool EnsureExternalMethodsPopulated()
        {
            if(External == null || External.resetCSG == null)
            {
                NativeMethodBindings.RegisterUnityMethods();
                NativeMethodBindings.RegisterExternalMethods();
            }

            if(External == null)
            {
                Debug.LogError("RealtimeCSG: Cannot rebuild meshes for some reason. External modules not loaded. Please save meshes into the Scene.");

                return false;
            }

            return true;
        }

        private static void RebuildMeshes()
        {
            if(!EnsureExternalMethodsPopulated())
            {
                return;
            }

            CSGModelManager.AllowInEditorPlayMode = true;
            Shutdown();
            DoForcedMeshUpdate();
            CheckForChanges();
            CSGModelManager.AllowInEditorPlayMode = false;
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RebuildMeshes();
        }

        private static void OnPlayModeChange(PlayModeStateChange playMode)
        {
            if(playMode == PlayModeStateChange.EnteredEditMode)
            {
                SceneManager.sceneLoaded               -= SceneLoaded;
                EditorApplication.playModeStateChanged -= OnPlayModeChange;

                RebuildMeshes();
            }
        }
#endif
    }
}

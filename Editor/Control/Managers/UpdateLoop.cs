using System;
using InternalRealtimeCSG;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    [InitializeOnLoad]
    internal sealed class UpdateLoop
    {
        [MenuItem("Edit/Realtime-CSG/Turn Realtime-CSG on or off %F3", false, 30)]
        private static void ToggleRealtimeCSG()
        {
            CSGSettings.SetRealtimeCSGEnabled(!CSGSettings.EnableRealtimeCSG);
        }

        public static bool IsActive()
        {
            return m_Editor != null && m_Editor._initialized;
        }

        private static UpdateLoop m_Editor;

        static UpdateLoop()
        {
            m_Editor = new UpdateLoop();
            m_Editor.Initialize();
        }

        private bool _initialized;
        private bool _hadFirstUpdate;

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            CSGKeysPreferenceWindow.ReadKeys();

            _initialized = true;

            CSGSceneManagerRedirector.Interface = new CSGSceneManagerInstance();

            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;

            EditorApplication.update -= OnFirstUpdate;
            EditorApplication.update += OnFirstUpdate;

            EditorApplication.hierarchyChanged -= OnHierarchyWindowChanged;
            EditorApplication.hierarchyChanged += OnHierarchyWindowChanged;

            PrefabStage.prefabSaving += OnPrefabSaving;

#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= HierarchyWindowItemGUI.OnHierarchyWindowItemByEntityIdOnGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HierarchyWindowItemGUI.OnHierarchyWindowItemByEntityIdOnGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemGUI.OnHierarchyWindowItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemGUI.OnHierarchyWindowItemOnGUI;
#endif

#if UNITY_6000_5_OR_NEWER
            Unity.Hierarchy.Editor.HierarchyWindow.BindView -= HierarchyWindowItemGUI.OnBindView;
            Unity.Hierarchy.Editor.HierarchyWindow.BindView += HierarchyWindowItemGUI.OnBindView;

            Unity.Hierarchy.Editor.HierarchyWindow.BindViewItem -= HierarchyWindowItemGUI.OnBindViewItem;
            Unity.Hierarchy.Editor.HierarchyWindow.BindViewItem += HierarchyWindowItemGUI.OnBindViewItem;
#endif

            UnityCompilerDefineManager.UpdateUnityDefines();

            InternalCSGModelManagerSystem.LazyInitialize();
        }

        private void OnPrefabSaving(GameObject obj)
        {
            ModelTraits.OnPrefabSaving(obj);
        }

        private void Shutdown(bool finalizing = false)
        {
            if(m_Editor != this)
            {
                return;
            }

            m_Editor                            = null;
            CSGSceneManagerRedirector.Interface = null;

            if(!_initialized)
            {
                return;
            }

            EditorApplication.update                   -= OnFirstUpdate;
            EditorApplication.hierarchyChanged         -= OnHierarchyWindowChanged;
#if UNITY_6000_4_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= HierarchyWindowItemGUI.OnHierarchyWindowItemByEntityIdOnGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemGUI.OnHierarchyWindowItemOnGUI;
#endif

            SceneView.duringSceneGui -= SceneViewEventHandler.OnScene;
            Undo.undoRedoPerformed   -= UndoRedoPerformed;

            _initialized = false;

            // make sure the C++ side of things knows to clear the method pointers
            // so that we don't accidentally use them while closing unity
            NativeMethodBindings.ClearUnityMethods();
            NativeMethodBindings.ClearExternalMethods();

            if(!finalizing)
            {
                SceneToolRenderer.Cleanup();
            }

            InternalCSGModelManagerSystem.Cleanup();
        }

        private static Scene m_CurrentScene;

        internal static void UpdateOnSceneChange()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();

            if(m_CurrentScene != activeScene)
            {
                if(m_Editor == null)
                {
                    ResetUpdateRoutine();
                }

                m_Editor.OnSceneUnloaded();
                m_CurrentScene = activeScene;
                InternalCSGModelManager.InitOnNewScene();
            }
        }

        private void OnSceneUnloaded()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if(_initialized)
            {
                Shutdown();
            }

            MeshInstanceManager.Shutdown();
            InternalCSGModelManager.Shutdown();

            m_Editor = new UpdateLoop();
            m_Editor.Initialize();
        }

        private void OnHierarchyWindowChanged()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            SceneDragToolManager.UpdateDragAndDrop();
            InternalCSGModelManager.UpdateHierarchy();
        }

        private void UndoRedoPerformed()
        {
            InternalCSGModelManager.UndoRedoPerformed();
        }

        // Delegate for generic updates
        private void OnFirstUpdate()
        {
            _hadFirstUpdate          =  true;
            EditorApplication.update -= OnFirstUpdate;
            CSGSettings.Reload();

            // register unity methods in the c++ code so that some unity functions
            // (such as debug.log) can be called from within the c++ code.
            NativeMethodBindings.RegisterUnityMethods();

            // register dll methods so we can use them
            NativeMethodBindings.RegisterExternalMethods();

            RunOnce();
            //CreateSceneChangeDetector();
        }

        private void RunOnce()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // when you start playing the game in the editor, it'll call
                // RunOnce before playing the game, but not after.
                // so we need to wait until the game has stopped, after which we'll
                // run first update again.
                EditorApplication.update -= OnWaitUntillStoppedPlaying;
                EditorApplication.update += OnWaitUntillStoppedPlaying;

                return;
            }

            SceneView.duringSceneGui -= SceneViewEventHandler.OnScene;
            SceneView.duringSceneGui += SceneViewEventHandler.OnScene;
            Undo.undoRedoPerformed   -= UndoRedoPerformed;
            Undo.undoRedoPerformed   += UndoRedoPerformed;

            // we use a co-routine for updates because EditorApplication.update
            // works at a ridiculous rate and the co-routine is only fired in the
            // editor when something has happened.
            ResetUpdateRoutine();
        }

        private void OnWaitUntillStoppedPlaying()
        {
            if(!EditorApplication.isPlaying)
            {
                EditorApplication.update -= OnWaitUntillStoppedPlaying;

                EditorApplication.update -= OnFirstUpdate;
                EditorApplication.update += OnFirstUpdate;
            }
        }

        private static void RunEditorUpdate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            UpdateOnSceneChange();

            if (CSGSettings.SyncronizeUnityGrid)
            {
                CSGSettings.SyncronizeGrid();
            }

            try
            {
                if(!ColorSettings.IsInitialized)
                {
                    ColorSettings.Update();
                }

                InternalCSGModelManagerSystem.Tick();
                InternalCSGModelManager.CheckForChanges();
                TooltipUtility.CleanCache();
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void ResetUpdateRoutine()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if(m_Editor != null && !m_Editor._initialized)
            {
                m_Editor = null;
            }

            if(m_Editor == null)
            {
                m_Editor = new UpdateLoop();
                m_Editor.Initialize();
            }

            EditorApplication.update                    -= RunEditorUpdate;
            EditorApplication.update                    += RunEditorUpdate;
            InternalCSGModelManager.SkipCheckForChanges =  false;
        }

        private static void OnSelectionChanged()
        {
            EditModeManager.UpdateSelection();
        }
    }
}

using System;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Foundation;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace RealtimeCSG.Components
{
    [Flags, Serializable]
    public enum ModelSettingsFlags
    {
        DoNotRender = 16
      , NoCollider = 32
      , IsTrigger = 64
      , InvertedWorld = 128
      , SetColliderConvex = 256
      , AutoUpdateRigidBody = 512
      , PreserveUVs = 1024
      , AutoRebuildUVs = 2048
      , StitchLightmapSeams = 4096
      , IgnoreNormals = 8192
      , TwoSidedShadows = 16384
    }

    /// <summary>Holds a CSG tree and generates meshes for that CSG tree</summary>
    /// <remarks>The CSG tree that defines a model is defined by its child [UnityEngine.GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html)s.</remarks>
#if UNITY_EDITOR
    [AddComponentMenu("CSG/Model"), DisallowMultipleComponent, ExecuteInEditMode]
#endif
    public sealed class CSGModel : CSGNode
    {
        public const float CURRENT_VERSION = 1.1f;

        public static ModelSettingsFlags DefaultSettings =
            (ModelSettingsFlags)ShadowCastingMode.On | ModelSettingsFlags.PreserveUVs
          | ModelSettingsFlags.AutoUpdateRigidBody;

        /// <value>The version number of this instance of a <see cref="CSGModel" /></value>
        [HideInInspector] public float version = CURRENT_VERSION;

        public bool IsRenderable
        {
            get => (settings & ModelSettingsFlags.DoNotRender) == 0;
        }

        public bool IsTwoSidedShadows
        {
            get => (settings & ModelSettingsFlags.TwoSidedShadows) != 0;
        }

        public bool HaveCollider
        {
            get => (settings & ModelSettingsFlags.NoCollider) == 0;
        }

        public bool IsTrigger
        {
            get => (settings & ModelSettingsFlags.IsTrigger) != 0;
        }

        public bool InvertedWorld
        {
            get => (settings & ModelSettingsFlags.InvertedWorld) != 0;
        }

        public bool SetColliderConvex
        {
            get => (settings & ModelSettingsFlags.SetColliderConvex) != 0;
        }

        public bool NeedAutoUpdateRigidBody
        {
            get => (settings & ModelSettingsFlags.AutoUpdateRigidBody) == 0;
        }

        public bool PreserveUVs
        {
            get => (settings & ModelSettingsFlags.PreserveUVs) != 0;
        }

        public bool StitchLightmapSeams
        {
            get => (settings & ModelSettingsFlags.StitchLightmapSeams) != 0;
        }

        public bool AutoRebuildUVs
        {
            get => (settings & ModelSettingsFlags.AutoRebuildUVs) != 0;
        }

        public bool IgnoreNormals
        {
            get => (settings & ModelSettingsFlags.IgnoreNormals) != 0;
        }

#if UNITY_EDITOR
        public bool IsRegistered
        {
            get => modelNodeID != INVALID_NODE_ID;
        }
#endif

#region Cached values

#if UNITY_EDITOR
        [NonSerialized] public int modelNodeID = INVALID_NODE_ID;
        [NonSerialized] public readonly ParentNodeData parentData = new ParentNodeData();
        [NonSerialized] public GeneratedMeshes generatedMeshes;
        [NonSerialized] public Transform cachedTransform = null;
        [HideInInspector, SerializeField] public CSGBrush infiniteBrush;
        [NonSerialized] public bool forceUpdate;

        [NonSerialized]
        public bool isActive; // this allows us to detect if we're enabled/disabled

        public void ClearCache()
        {
            parentData.Reset();
            generatedMeshes = null;
            forceUpdate     = false;
            isActive        = false;
        }
#endif

#endregion

#region Settings

        [EnumAsFlags] public ModelSettingsFlags settings = DefaultSettings;
        [EnumAsFlags] public VertexChannelFlags vertexChannels = VertexChannelFlags.All;
        [EnumAsFlags] public ReceiveGI receiveGI = ReceiveGI.Lightmaps;

        [EnumAsFlags] public MeshColliderCookingOptions meshColliderCookingOptions =
            MeshColliderCookingOptions.CookForFasterSimulation |
            MeshColliderCookingOptions.EnableMeshCleaning |
            MeshColliderCookingOptions.WeldColocatedVertices;

#endregion

#region Editor Settings

#if UNITY_EDITOR

        public bool showGeneratedMeshes;
        public PhysicsMaterial defaultPhysicsMaterial;

#endif

#region Lightmap settings

        public const float MIN_ANGLE_ERROR = 0.001f;
        public const float MIN_AREA_ERROR = 0.001f;
        public const float MAX_ANGLE_ERROR = 1.000f;
        public const float MAX_AREA_ERROR = 1.000f;

        // Note: contents of UnityEditor.UnwrapParam, which is not marked serializable :(
        public float angleError = 1.00f;
        public float areaError = 1.00f;
        public float hardAngle = 60.0f;
        public float packMargin = 20.0f;

        public float scaleInLightmap = 1.0f;
        public float autoUVMaxDistance = 0.5f;
        public float autoUVMaxAngle = 89.0f;
        public int minimumChartSize = 4;

#endregion

#endregion

#region Events

#if UNITY_EDITOR
        // register ourselves with our scene manager
        internal void Awake()
        {
            // cannot change visibility since this might have an effect on exporter
            hideFlags |= HideFlags.DontSaveInBuild;
            ComponentUpgrader.UpgradeWhenNecessary(this);

            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnCreated(this);
            }
        }

        internal void OnEnable()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnEnabled(this);
            }
        }

        internal void OnDisable()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnDisabled(this);
            }
        }

        internal void OnDestroy()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnDestroyed(this);
            }
        }

        internal void OnTransformChildrenChanged()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnTransformChildrenChanged(this);
            }
        }

        // called when any value of this brush has been modified from within the inspector / or recompile
        // on recompile causes our data to be forgotten, yet awake isn't called
        internal void OnValidate()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnValidate(this);
            }
        }

        internal void Update()
        {
            if(CSGSceneManagerRedirector.Interface != null && !IsRegistered)
            {
                CSGSceneManagerRedirector.Interface.OnUpdate(this);
            }

            //Ensure children match model's scene visibility
            if (UnityEditor.SceneVisibilityManager.instance.IsHidden(gameObject))
            {
                if (UnityEditor.SceneVisibilityManager.instance.AreAllDescendantsHidden(gameObject) == false)
                {
                    UnityEditor.SceneVisibilityManager.instance.Hide(gameObject, true);
                }
            }
            else
            {
                if (UnityEditor.SceneVisibilityManager.instance.AreAllDescendantsVisible(gameObject) == false)
                {
                    UnityEditor.SceneVisibilityManager.instance.Show(gameObject, true);
                }
            }

            //Ensure children match model's scene pickability
            if (UnityEditor.SceneVisibilityManager.instance.IsPickingDisabled(gameObject))
            {
                if (UnityEditor.SceneVisibilityManager.instance.IsPickingDisabledOnAllDescendants(gameObject) == false)
                {
                    UnityEditor.SceneVisibilityManager.instance.DisablePicking(gameObject, true);
                }
            }
            else
            {
                if (UnityEditor.SceneVisibilityManager.instance.IsPickingEnabledOnAllDescendants(gameObject) == false)
                {
                    UnityEditor.SceneVisibilityManager.instance.EnablePicking(gameObject, true);
                }
            }
        }
#endif

#endregion
    }
}

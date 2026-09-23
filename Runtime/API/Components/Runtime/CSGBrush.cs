using System;
using UnityEngine;
using RealtimeCSG.Legacy;
using InternalRealtimeCSG;
using UnityEngine.Serialization;
using CSGOperationType = RealtimeCSG.Foundation.CSGOperationType;

namespace RealtimeCSG.Components
{
    [Serializable]
    public enum BrushFlags
    {
        None = 0
      , InfiniteBrush = 1 // used to create inverted world
    }

    /// <summary>Holds a CSG tree brush</summary>
#if UNITY_EDITOR
    [AddComponentMenu("CSG/Brush"), ExecuteInEditMode]
#endif
    public sealed class CSGBrush : CSGNode
    {
        public const float CURRENT_VERSION = 2.1f;

        /// <value>The version number of this instance of a <see cref="CSGBrush" /></value>
        [FormerlySerializedAs("Version")] public float version = CURRENT_VERSION;

#if UNITY_EDITOR
        public bool IsRegistered
        {
            get => brushNodeID != INVALID_NODE_ID;
        }
#endif

#region Settings

#if UNITY_EDITOR

        /// <value>The CSG operation to perform with this brush</value>
        [FormerlySerializedAs("OperationType")] public CSGOperationType operationType = CSGOperationType.Additive;

        [FormerlySerializedAs("Flags")] public BrushFlags flags = BrushFlags.None;

        /// <value>The <see cref="shape"/> that defines the shape by this brush together with its <see cref="controlMesh"/>.</value>
        /// <remarks><note>This will be replaced by <see cref="RealtimeCSG.Foundation.BrushMesh"/> eventually</note></remarks>
        [FormerlySerializedAs("Shape")] public Shape shape;

        /// <value>The <see cref="controlMesh"/> that defines the shape by this brush together with its <see cref="shape"/>.</value>
        /// <remarks><note>This will be replaced by <see cref="RealtimeCSG.Foundation.BrushMesh"/> eventually</note></remarks>
        [FormerlySerializedAs("ControlMesh")] public ControlMesh controlMesh;

#endif

#endregion

#region Cached values

#if UNITY_EDITOR
        [NonSerialized] public int brushNodeID = INVALID_NODE_ID;
        [NonSerialized] public Color? outlineColor;
        [NonSerialized] public readonly ChildNodeData childData = new ChildNodeData();
        [NonSerialized] public readonly HierarchyItem hierarchyItem = new HierarchyItem();
        [NonSerialized] public GeometryWireframe outline;
        [NonSerialized] public CSGOperationType prevOperation = CSGOperationType.Additive;
        [NonSerialized] public int controlMeshGeneration;

        // this allows us to determine if our brush has changed it's transformation
        [NonSerialized]
        public readonly CompareTransformation compareTransformation = new CompareTransformation();

        // this allows us to determine if our brush has any of it's surfaces changed
        [NonSerialized] public readonly CompareShape compareShape = new CompareShape();

        public void ClearCache()
        {
            childData.Reset();
            hierarchyItem.Reset();
            compareShape.Reset();
            compareTransformation.Reset();
            outline               = null;
            prevOperation         = CSGOperationType.Additive;
            controlMeshGeneration = 0;
        }
#endif

#endregion

#region Events

#if UNITY_EDITOR
        // register ourselves with our scene manager
        private void Awake()
        {
            // cannot change visibility since this might have an effect on exporter
            hideFlags   |= HideFlags.DontSaveInBuild;
            brushNodeID =  INVALID_NODE_ID;
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

        // unregister ourselves from our scene manager
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

        // detect if this node has been moved within the hierarchy
        internal void OnTransformParentChanged()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnTransformParentChanged(this);
            }
        }

        // called when any value of this brush has been modified from within the inspector
        internal void OnValidate()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnValidate(this);
            }
        }
#endif

#endregion

#if UNITY_EDITOR
        public void EnsureInitialized()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.EnsureInitialized(this);
            }
        }
#endif
    }
}

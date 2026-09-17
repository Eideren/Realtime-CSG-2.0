using System;
using InternalRealtimeCSG;
using UnityEngine;
using CSGOperationType = RealtimeCSG.Foundation.CSGOperationType;

namespace RealtimeCSG.Components
{
    /// <summary>Holds a CSG tree branch</summary>
    /// <remarks>The CSG branch that defines a CSGOperation is defined by its child [UnityEngine.GameObject](https://docs.unity3d.com/ScriptReference/GameObject.html)s.</remarks>
#if UNITY_EDITOR
    [AddComponentMenu("CSG/Operation"), ExecuteInEditMode, DisallowMultipleComponent]
#endif
    public sealed class CSGOperation : CSGNode
    {
        public const float CURRENT_VERSION = 1.1f;

        /// <value>The version number of this instance of a <see cref="CSGOperation" /></value>
        [HideInInspector] public float version = CURRENT_VERSION;

#if UNITY_EDITOR
        public bool IsRegistered
        {
            get => operationNodeID != INVALID_NODE_ID;
        }
#endif

#region Settings

#if UNITY_EDITOR

        /// <value>The CSG operation to perform with this CSG branch</value>
        public CSGOperationType operationType = CSGOperationType.Additive;

        [HideInInspector, SerializeField] internal bool passThrough;
        public bool PassThrough
        {
            get => passThrough;
            set
            {
                if(passThrough == value)
                {
                    return;
                }

                OnDisable();
                passThrough = value;
                OnEnable();

                if(CSGSceneManagerRedirector.Interface != null)
                {
                    CSGSceneManagerRedirector.Interface.OnPassthroughChanged(this);
                }
            }
        }
#endif

#endregion

#region Editor Settings

#if UNITY_EDITOR
        public bool handleAsOne;
#endif

#endregion

#region Cached values

#if UNITY_EDITOR
        [NonSerialized] public int operationNodeID = INVALID_NODE_ID;
        [NonSerialized] public readonly ParentNodeData parentData = new ParentNodeData();
        [NonSerialized] public readonly ChildNodeData childData = new ChildNodeData();

        // this allows us to detect if our operation has been modified
        [NonSerialized] public CSGOperationType prevOperation = (CSGOperationType)0xff;

        public void ClearCache()
        {
            parentData.Reset();
            childData.Reset();
            prevOperation = (CSGOperationType)0xff;
        }
#endif

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
            if(CSGSceneManagerRedirector.Interface != null && !passThrough)
            {
                CSGSceneManagerRedirector.Interface.OnEnabled(this);
            }
        }

        // unregister ourselves from our scene manager
        internal void OnDisable()
        {
            if(CSGSceneManagerRedirector.Interface != null && !passThrough)
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

            UnityEditor.EditorGUIUtility.SetIconForObject(gameObject, null);
        }

        // detect if this node has been moved within the hierarchy
        internal void OnTransformParentChanged()
        {
            if(CSGSceneManagerRedirector.Interface != null && !passThrough)
            {
                CSGSceneManagerRedirector.Interface.OnTransformParentChanged(this);
            }
        }

        // called when any value of this brush has been modified from within the inspector / or recompile
        internal void OnValidate()
        {
            if(CSGSceneManagerRedirector.Interface != null && !passThrough)
            {
                CSGSceneManagerRedirector.Interface.OnValidate(this);
            }
        }
#endif

#endregion

#if UNITY_EDITOR
        public void EnsureInitialized()
        {
            if(CSGSceneManagerRedirector.Interface != null && !passThrough)
            {
                CSGSceneManagerRedirector.Interface.EnsureInitialized(this);
            }
        }
#endif
    }
}

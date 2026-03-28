using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using MeshQuery = RealtimeCSG.Foundation.MeshQuery;
using GeneratedMeshDescription = RealtimeCSG.Foundation.GeneratedMeshDescription;

namespace InternalRealtimeCSG
{
#if UNITY_EDITOR
    public struct MeshInstanceKey : IEqualityComparer<MeshInstanceKey>, IEquatable<MeshInstanceKey>
    {
        public static MeshInstanceKey GenerateKey(GeneratedMeshDescription meshDescription)
        {
            return new MeshInstanceKey(meshDescription.meshQuery, meshDescription.surfaceParameter, meshDescription.subMeshQueryIndex);
        }

        private MeshInstanceKey(MeshQuery meshType, int surfaceParameter, int subMeshIndex)
        {
            this.subMeshIndex     = subMeshIndex;
            this.meshType         = meshType;
            this.surfaceParameter = surfaceParameter;
        }

        public readonly int subMeshIndex;
        public readonly int surfaceParameter;
        public readonly MeshQuery meshType;

        public override string ToString()
        {
            return $"({subMeshIndex} {surfaceParameter} {meshType})";
        }

#region Comparison

        public override int GetHashCode()
        {
            int hash1 = subMeshIndex.GetHashCode();
            int hash2 = surfaceParameter.GetHashCode();
            int hash3 = meshType.GetHashCode();
            int hash  = hash1;
            hash *= 389 + hash2;
            hash *= 397 + hash3;

            return hash + (hash1 ^ hash2 ^ hash3) + hash1 + hash2 + hash3 + hash1 * hash2 * hash3;
        }

        public int GetHashCode(MeshInstanceKey obj)
        {
            return obj.GetHashCode();
        }

        public bool Equals(MeshInstanceKey other)
        {
            if(object.Equals(this, other))
            {
                return true;
            }

            return subMeshIndex == other.subMeshIndex && surfaceParameter == other.surfaceParameter && meshType == other.meshType;
        }

        public override bool Equals(object obj)
        {
            if(object.Equals(this, obj))
            {
                return true;
            }

            if(!(obj is MeshInstanceKey other))
            {
                return false;
            }

            return subMeshIndex == other.subMeshIndex && surfaceParameter == other.surfaceParameter && meshType == other.meshType;
        }

        public bool Equals(MeshInstanceKey left, MeshInstanceKey right)
        {
            if(object.Equals(left, right))
            {
                return true;
            }

            return left.subMeshIndex == right.subMeshIndex && left.surfaceParameter == right.surfaceParameter && left.meshType == right.meshType;
        }

        public static bool operator ==(MeshInstanceKey left, MeshInstanceKey right)
        {
            if(object.Equals(left, right))
            {
                return true;
            }

            return left.subMeshIndex == right.subMeshIndex && left.surfaceParameter == right.surfaceParameter && left.meshType == right.meshType;
        }

        public static bool operator !=(MeshInstanceKey left, MeshInstanceKey right)
        {
            if(object.Equals(left, right))
            {
                return false;
            }

            return left.subMeshIndex != right.subMeshIndex || left.surfaceParameter != right.surfaceParameter || left.meshType != right.meshType;
        }

#endregion
    }
#endif

    [DisallowMultipleComponent, ExecuteInEditMode]
    public sealed class GeneratedMeshInstance : MonoBehaviour
    {
        [HideInInspector] public float version = 1.00f;
#if UNITY_EDITOR
        public Mesh sharedMesh;
        public Material renderMaterial;
        public PhysicsMaterial physicsMaterial;
        public RenderSurfaceType renderSurfaceType = (RenderSurfaceType)999;
        public GeneratedMeshDescription meshDescription;

        [HideInInspector] public bool hasGeneratedNormals;
        [HideInInspector] public bool hasUV2;
        [HideInInspector] public long lightingHashValue;

        [NonSerialized] public float resetUVTime = float.PositiveInfinity;
        [NonSerialized] public bool dirty = true;
        [NonSerialized] public MeshCollider cachedMeshCollider;
        [NonSerialized] public MeshFilter cachedMeshFilter;
        [NonSerialized] public MeshRenderer cachedMeshRenderer;
        [NonSerialized] public object cachedMeshRendererSO;

        public void Reset()
        {
            renderMaterial    = null;
            physicsMaterial   = null;
            renderSurfaceType = (RenderSurfaceType)999;

            hasGeneratedNormals = false;
            hasUV2              = false;
            resetUVTime         = float.PositiveInfinity;
            lightingHashValue   = 0;

            dirty = true;

            cachedMeshCollider   = null;
            cachedMeshFilter     = null;
            cachedMeshRenderer   = null;
            cachedMeshRendererSO = null;
        }

        public MeshInstanceKey GenerateKey()
        {
            return MeshInstanceKey.GenerateKey(meshDescription);
        }

        public bool IsValid()
        {
            if((!physicsMaterial || physicsMaterial.GetInstanceID() != 0) && (!renderMaterial || renderMaterial.GetInstanceID() != 0))
            {
                if(sharedMesh)
                {
                    if(sharedMesh.vertexCount < 0)
                    {
                        return false;
                    }
                }
                else if(!ReferenceEquals(sharedMesh, null) && sharedMesh.GetInstanceID() != 0)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private static readonly List<Vector2> sUVList = new List<Vector2>();

        internal void Awake()
        {
            // cannot change visibility since this might have an effect on exporter
            gameObject.hideFlags = HideFlags.DontSaveInBuild;
            hideFlags            = HideFlags.DontSaveInBuild;

            // Unity bug workaround
            cachedMeshCollider   = null;
            cachedMeshFilter     = null;
            cachedMeshRenderer   = null;
            cachedMeshRendererSO = null;

            // InstanceIDs are not properly remembered across domain reloads,
            // this causes issues on, for instance, first startup of Unity.
            // So we need to refresh the instanceIDs
            if(renderSurfaceType == RenderSurfaceType.Collider || renderSurfaceType == RenderSurfaceType.Trigger)
            {
                if(!ReferenceEquals(physicsMaterial, null))
                {
                    if(physicsMaterial)
                    {
                        meshDescription.surfaceParameter = physicsMaterial.GetInstanceID();
                    }
                }
            }
            else
            {
                if(!ReferenceEquals(renderMaterial, null))
                {
                    if(renderMaterial)
                    {
                        meshDescription.surfaceParameter = renderMaterial.GetInstanceID();
                    }
                }
                else if(!ReferenceEquals(physicsMaterial, null))
                {
                    if(physicsMaterial)
                    {
                        meshDescription.surfaceParameter = physicsMaterial.GetInstanceID();
                    }
                }
            }
        }

        internal void OnEnable()
        {
            // Workaround for when Unity gets confused with some prefab instance merging and moves
            //    the GeneratedMeshInstances out of its GeneratedMeshes container
            if(transform.parent && !transform.parent.GetComponent<GeneratedMeshes>())
            {
                gameObject.hideFlags = HideFlags.None;

                try
                {
                    DestroyImmediate(gameObject);
                }
                catch
                {
                    // Work-around for nested prefab instance issues ..
                    if(gameObject.activeSelf)
                    {
                        gameObject.SetActive(false);

                        MonoBehaviour[] childBehaviours = gameObject.GetComponentsInChildren<MonoBehaviour>();

                        foreach(MonoBehaviour component in childBehaviours)
                        {
                            DestroyImmediate(component);
                        }

                        Component[] childComponents = gameObject.GetComponentsInChildren<Component>();

                        foreach(Component component in childComponents)
                        {
                            if(!(component is Transform))
                            {
                                DestroyImmediate(component);
                            }
                        }

                        gameObject.name = "removed";
                    }
                }
            }

            if(sharedMesh)
            {
                sharedMesh.GetUVs(1, sUVList);
                hasUV2 = sUVList != null && sUVList.Count == sharedMesh.vertexCount;
            }
            else
            {
                hasUV2 = false;
            }
        }

        public void FindMissingSharedMesh()
        {
            if(TryGetComponent(out MeshCollider meshCollider))
            {
                sharedMesh = meshCollider.sharedMesh;

                return;
            }

            if(TryGetComponent(out MeshFilter meshFilter))
            {
                sharedMesh = meshFilter.sharedMesh;
            }
        }
#else
        void Awake()
        {
            this.hideFlags = HideFlags.DontSaveInBuild;
        }
#endif
    }
}

using System;
using UnityEngine;
using System.Collections.Generic;
using RealtimeCSG.Components;
using System.Linq;
using UnityEngine.Serialization;
using RealtimeCSG.Foundation;

namespace InternalRealtimeCSG
{
    [Serializable]
    public enum RenderSurfaceType
    {
        Normal
      , Hidden     // manually hidden by user
      , Culled     // removed by CSG process
      , ShadowOnly // surface that casts shadows
      , Collider
      , Trigger
      , CastShadows    // surface that casts shadows
      , ReceiveShadows // surface that receive shadows
    }

#if UNITY_EDITOR
    [Serializable]
    public sealed class HelperSurfaceDescription
    {
        public Mesh sharedMesh;
        public RenderSurfaceType renderSurfaceType = (RenderSurfaceType)999;
        public bool hasGeneratedNormals;
        public GeneratedMeshDescription meshDescription;
        public GameObject gameObject;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;

        public MeshInstanceKey GenerateKey()
        {
            return MeshInstanceKey.GenerateKey(meshDescription);
        }

        public bool IsValid()
        {
            if(sharedMesh)
            {
                if(sharedMesh.vertexCount < 0)
                {
                    return false;
                }
            }
            else if(sharedMesh.GetIntegerID() != 0)
            {
                return false;
            }

            return true;
        }

        public void Destroy()
        {
            GameObjectExtensions.Destroy(gameObject);
            gameObject   = null;
            meshFilter   = null;
            meshRenderer = null;
        }
    }
#endif

    [DisallowMultipleComponent, ExecuteInEditMode, SelectionBase]
    public sealed class GeneratedMeshes : MonoBehaviour
    {
        [HideInInspector] public float version = 1.00f;
#if UNITY_EDITOR
        public CSGModel owner;

        [NonSerialized] public Rigidbody cachedRigidBody;

        [NonSerialized] private GeneratedMeshInstance[] _meshInstances;
        [SerializeField] private HelperSurfaceDescription[] helperSurfaces;

        private static readonly GeneratedMeshInstance[] emptyMeshInstances = Array.Empty<GeneratedMeshInstance>();
        private static readonly HelperSurfaceDescription[] emptyHelperSurfaces = Array.Empty<HelperSurfaceDescription>();

        public bool HasMeshInstances
        {
            get => _meshInstances != null && _meshInstances.Length > 0;
        }

        public bool HasHelperSurfaces
        {
            get => helperSurfaces != null && helperSurfaces.Length > 0;
        }

        public GeneratedMeshInstance[] MeshInstances
        {
            get => _meshInstances == null ? emptyMeshInstances : _meshInstances;
        }

        public HelperSurfaceDescription[] HelperSurfaces
        {
            get => helperSurfaces == null ? emptyHelperSurfaces : helperSurfaces;
        }

        public HelperSurfaceDescription GetHelperSurface(MeshInstanceKey key)
        {
            if(helperSurfaces == null)
            {
                return null;
            }

            for(int i = 0; i < helperSurfaces.Length; i++)
            {
                if(helperSurfaces[i].GenerateKey() == key)
                {
                    return helperSurfaces[i];
                }
            }

            return null;
        }

        public void AddHelperSurface(HelperSurfaceDescription instance)
        {
            if(instance == null)
            {
                return;
            }

            if(helperSurfaces == null)
            {
                return;
            }

            MeshInstanceKey key = instance.GenerateKey();

            for(int i = 0; i < helperSurfaces.Length; i++)
            {
                if(helperSurfaces[i].sharedMesh && helperSurfaces[i].GenerateKey() == key)
                {
                    helperSurfaces[i].Destroy();
                    helperSurfaces[i] = instance;

                    return;
                }
            }

            UnityEditor.ArrayUtility.Add(ref helperSurfaces, instance);
        }

        public GeneratedMeshInstance GetMeshInstance(MeshInstanceKey key)
        {
            if(_meshInstances == null)
            {
                return null;
            }

            for(int i = 0; i < _meshInstances.Length; i++)
            {
                MeshInstanceKey instanceKey = _meshInstances[i].GenerateKey();

                if(instanceKey == key)
                {
                    return _meshInstances[i];
                }
            }

            return null;
        }

        public void AddMeshInstance(GeneratedMeshInstance instance)
        {
            if(!instance)
            {
                return;
            }

            if(_meshInstances == null)
            {
                _meshInstances = new[] { instance };

                return;
            }

            MeshInstanceKey key = instance.GenerateKey();

            for(int i = 0; i < _meshInstances.Length; i++)
            {
                if(_meshInstances[i].GenerateKey() == key)
                {
                    _meshInstances[i] = instance;

                    return;
                }
            }

            UnityEditor.ArrayUtility.Add(ref _meshInstances, instance);
        }

        public void SetHelperSurfaces(HashSet<HelperSurfaceDescription> foundInstances)
        {
            if(helperSurfaces != null && foundInstances.Count == helperSurfaces.Length)
            {
                bool differenceFound = false;

                for(int i = 0; i < helperSurfaces.Length; i++)
                {
                    if(foundInstances.Contains(helperSurfaces[i]))
                    {
                        differenceFound = true;

                        break;
                    }
                }

                if(!differenceFound)
                {
                    return;
                }
            }

            if(helperSurfaces != null)
            {
                foreach(HelperSurfaceDescription helperSurface in helperSurfaces)
                {
                    helperSurface.Destroy();
                }
            }

            if(foundInstances.Count == 0)
            {
                helperSurfaces = null;

                return;
            }

            if(helperSurfaces == null || foundInstances.Count != helperSurfaces.Length)
            {
                helperSurfaces = new HelperSurfaceDescription[foundInstances.Count];
            }

            int idx = 0;

            foreach(HelperSurfaceDescription item in foundInstances)
            {
                helperSurfaces[idx] = item;
                idx++;
            }
        }

        public void SetMeshInstances(HashSet<GeneratedMeshInstance> foundInstances)
        {
            if(_meshInstances != null && foundInstances.Count == _meshInstances.Length)
            {
                bool differenceFound = false;

                for(int i = 0; i < _meshInstances.Length; i++)
                {
                    if(foundInstances.Contains(_meshInstances[i]))
                    {
                        differenceFound = true;

                        break;
                    }
                }

                if(!differenceFound)
                {
                    return;
                }
            }

            if(foundInstances.Count == 0)
            {
                _meshInstances = null;

                return;
            }

            if(_meshInstances == null || foundInstances.Count != _meshInstances.Length)
            {
                _meshInstances = new GeneratedMeshInstance[foundInstances.Count];
            }

            int idx = 0;

            foreach(GeneratedMeshInstance item in foundInstances)
            {
                _meshInstances[idx] = item;
                idx++;
            }
        }

        public void SetMeshInstances(List<GeneratedMeshInstance> foundInstances)
        {
            if(_meshInstances != null && foundInstances.Count == _meshInstances.Length)
            {
                bool differenceFound = false;

                for(int i = 0; i < _meshInstances.Length; i++)
                {
                    bool found = false;

                    for(int j = 0; j < foundInstances.Count; j++)
                    {
                        if(_meshInstances[i] == foundInstances[j])
                        {
                            found = true;

                            break;
                        }
                    }

                    if(!found)
                    {
                        differenceFound = true;

                        break;
                    }
                }

                if(!differenceFound)
                {
                    return;
                }
            }

            if(foundInstances.Count == 0)
            {
                _meshInstances = null;

                return;
            }

            if(_meshInstances == null || foundInstances.Count != _meshInstances.Length)
            {
                _meshInstances = new GeneratedMeshInstance[foundInstances.Count];
            }

            for(int i = 0; i < foundInstances.Count; i++)
            {
                _meshInstances[i] = foundInstances[i];
            }
        }

        public void RemoveMeshInstances(MeshInstanceKey[] removeKeys, int count)
        {
            if(count == _meshInstances.Length)
            {
                _meshInstances = null;

                return;
            }

            List<GeneratedMeshInstance> oldInstances = _meshInstances.ToList();

            for(int j = oldInstances.Count - 1; j >= 0; j--)
            {
                MeshInstanceKey key = oldInstances[j].GenerateKey();

                for(int i = 0; i < removeKeys.Length; i++)
                {
                    MeshInstanceKey removeKey = removeKeys[i];

                    if(removeKey != key)
                    {
                        continue;
                    }

                    oldInstances.RemoveAt(j);

                    break;
                }
            }

            _meshInstances = oldInstances.ToArray();
        }

        public bool HasMeshInstance(MeshInstanceKey key)
        {
            return GetMeshInstance(key) != null;
        }

        private void Awake()
        {
            // cannot change visibility since this might have an effect on exporter
            gameObject.hideFlags = HideFlags.None;
            hideFlags            = HideFlags.None;

            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnCreated(this);
            }
        }

        private void OnEnable()
        {
            _meshInstances = GetComponentsInChildren<GeneratedMeshInstance>();
        }

        private void OnDestroy()
        {
            if(CSGSceneManagerRedirector.Interface != null)
            {
                CSGSceneManagerRedirector.Interface.OnDestroyed(this);
            }
        }

        // Unity bug workaround
        private void Update()
        {
            // we need to kill a dangling "generated-meshes" when deleting prefab instance in scene
            if(owner)
            {
                return;
            }

            GameObjectExtensions.Destroy(gameObject);
        }
#else
        void Awake()
        {
            this.gameObject.tag = "EditorOnly";
        }
#endif
    }
}

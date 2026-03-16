//#define SHOW_GENERATED_MESHES

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor;
using RealtimeCSG;
using RealtimeCSG.Components;
using UnityEngine.SceneManagement;

namespace InternalRealtimeCSG
{
    internal sealed partial class MeshInstanceManager
    {
        public static void Export(CSGModel model, ExportType exportType, bool exportColliders)
        {
            string typeName;
            string extension;

            switch(exportType)
            {
                case ExportType.FBX:
                    typeName  = "FBX";
                    extension = @"fbx";

                    break;
                default:
                    //case RealtimeCSG.Components.ExportType.UnityMesh:
                    typeName   = "Mesh";
                    extension  = @"prefab";
                    exportType = ExportType.UnityMesh;

                    break;
            }

            string newPath = model.exportPath;

            if(exportType != ExportType.UnityMesh)
            {
                newPath = UnityFBXExporter.ExporterMenu.GetNewPath(
                    model.gameObject, typeName, extension, model.exportPath
                );

                if(string.IsNullOrEmpty(newPath))
                {
                    return;
                }
            }

            model.ShowGeneratedMeshes = false;
            CSGModel[] foundModels = model.GetComponentsInChildren<CSGModel>(true);

            for(int index = 0; index < foundModels.Length; index++)
            {
                if(!foundModels[index].ShowGeneratedMeshes)
                {
                    continue;
                }

                foundModels[index].ShowGeneratedMeshes = false;
                UpdateGeneratedMeshesVisibility(foundModels[index]);
            }

            GameObject tempExportObject;

            if(!string.IsNullOrEmpty(model.exportPath))
            {
                tempExportObject = new GameObject(System.IO.Path.GetFileNameWithoutExtension(model.exportPath));

                if(string.IsNullOrEmpty(tempExportObject.name))
                {
                    tempExportObject.name = model.name;
                }
            }
            else
            {
                tempExportObject = new GameObject(model.name);
            }

            tempExportObject.transform.position   = MathConstants.zeroVector3;
            tempExportObject.transform.rotation   = MathConstants.identityQuaternion;
            tempExportObject.transform.localScale = MathConstants.oneVector3;

            int                       colliderCounter      = 1;
            int                       shadowOnlyCounter    = 1;
            Dictionary<Material, int> materialMeshCounters = new Dictionary<Material, int>();

            Scene currentScene = model.gameObject.scene;

            List<GeneratedMeshes> foundMeshContainers =
                SceneQueryUtility.GetAllComponentsInScene<GeneratedMeshes>(currentScene);

            AABB bounds = new AABB();
            bounds.Reset();
            List<MeshFilter>   foundMeshFilters   = new List<MeshFilter>();
            List<MeshCollider> foundMeshColliders = new List<MeshCollider>();

            foreach(GeneratedMeshes meshContainer in foundMeshContainers)
            {
                CSGModel owner = meshContainer.owner;

                if(!owner || !ArrayUtility.Contains(foundModels, owner))
                {
                    continue;
                }

                if(!meshContainer || !meshContainer.HasMeshInstances)
                {
                    continue;
                }

                foreach(GeneratedMeshInstance instance in meshContainer.MeshInstances)
                {
                    if(!instance)
                    {
                        continue;
                    }

                    Refresh(instance, model, true, skipAssetDatabaseUpdate: true);

                    RenderSurfaceType surfaceType = GetSurfaceType(instance.MeshDescription, owner.Settings);

                    if(surfaceType != RenderSurfaceType.Normal &&
                       surfaceType != RenderSurfaceType.ShadowOnly &&
                       surfaceType != RenderSurfaceType.Collider)
                    {
                        continue;
                    }

                    int counter = 0;

                    if(instance.RenderMaterial)
                    {
                        if(!materialMeshCounters.TryGetValue(instance.RenderMaterial, out counter))
                        {
                            counter = 1;
                        }
                        else
                        {
                            counter++;
                        }
                    }

                    Mesh mesh = instance.SharedMesh;

                    if(!mesh.isReadable)
                    {
                        //bounds.Extend(mesh.bounds.min);
                        //bounds.Extend(mesh.bounds.max);
                    }
                    else
                    {
                        Vector3[] vertices = mesh.vertices;

                        for(int v = 0; v < vertices.Length; v++)
                        {
                            bounds.Extend(vertices[v]);
                        }
                    }

                    GameObject subObj = Object.Instantiate(
                        instance.gameObject, MathConstants.zeroVector3, MathConstants.identityQuaternion
                    ) as GameObject;

                    subObj.hideFlags            = HideFlags.None;
                    subObj.transform.position   = owner.transform.position;
                    subObj.transform.rotation   = owner.transform.rotation;
                    subObj.transform.localScale = owner.transform.localScale;
                    subObj.transform.SetParent(tempExportObject.transform, false);

                    GeneratedMeshInstance genMeshInstance = subObj.GetComponent<GeneratedMeshInstance>();

                    Object.DestroyImmediate(genMeshInstance);

                    if(surfaceType == RenderSurfaceType.Collider)
                    {
                        subObj.name = "no-material Mesh (" + colliderCounter + ") COLLIDER";
                        colliderCounter++;
                    }
                    else
                    {
                        if(surfaceType == RenderSurfaceType.ShadowOnly)
                        {
                            subObj.name = "shadow-only Mesh (" + shadowOnlyCounter + ")";
                            shadowOnlyCounter++;
                            MeshRenderer meshRenderer = subObj.GetComponent<MeshRenderer>();

                            if(meshRenderer)
                            {
                                meshRenderer.sharedMaterial = MaterialUtility.DefaultMaterial;
                            }
                        }
                        else
                        {
                            Material renderMaterial = instance.RenderMaterial;

                            if(!renderMaterial)
                            {
                                renderMaterial = MaterialUtility.DefaultMaterial;
                                subObj.name    = "missing-material Mesh (" + counter + ")";
                                counter++;
                            }
                            else
                            {
                                subObj.name = renderMaterial.name + " Mesh (" + counter + ")";
                            }

                            counter++;
                            materialMeshCounters[instance.RenderMaterial] = counter;
                        }

                        MeshFilter meshFilter = subObj.GetComponent<MeshFilter>();

                        if(meshFilter)
                        {
                            foundMeshFilters.Add(meshFilter);
                        }
                    }

                    MeshCollider meshCollider = subObj.GetComponent<MeshCollider>();

                    if(meshCollider)
                    {
                        foundMeshColliders.Add(meshCollider);
                    }
                }
            }

            Undo.IncrementCurrentGroup();
            int groupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Exported model");

            try
            {
                Vector3 position = model.transform.position;

                if(float.IsInfinity(position.x) || float.IsNaN(position.x))
                {
                    position.x = 0;
                }

                if(float.IsInfinity(position.y) || float.IsNaN(position.y))
                {
                    position.y = 0;
                }

                if(float.IsInfinity(position.z) || float.IsNaN(position.z))
                {
                    position.z = 0;
                }

                Vector3 center = bounds.Center;

                switch(model.originType)
                {
                    default:
                    case OriginType.ModelCenter: center = bounds.Center + position; break;
                    case OriginType.ModelPivot: center = position; break;
                    case OriginType.WorldSpace: center = Vector3.zero; break;
                }

                if(float.IsInfinity(center.x) || float.IsNaN(center.x))
                {
                    center.x = 0;
                }

                if(float.IsInfinity(center.y) || float.IsNaN(center.y))
                {
                    center.y = 0;
                }

                if(float.IsInfinity(center.z) || float.IsNaN(center.z))
                {
                    center.z = 0;
                }

                Dictionary<Mesh, Mesh> modifiedMeshes = new Dictionary<Mesh, Mesh>();

                foreach(MeshFilter meshFilter in foundMeshFilters)
                {
                    Mesh mesh = meshFilter.sharedMesh;

                    if(!mesh.isReadable)
                    {
                        continue;
                    }

                    Mesh newMesh;

                    if(!modifiedMeshes.TryGetValue(mesh, out newMesh))
                    {
                        newMesh = (Mesh)Object.Instantiate(mesh);
                        Vector3[] vertices = mesh.vertices;

                        for(int v = 0; v < vertices.Length; v++)
                        {
                            vertices[v] += position;
                            vertices[v] -= center;
                        }

                        newMesh.vertices = vertices;
                        newMesh.RecalculateBounds();
                        modifiedMeshes[mesh] = newMesh;
                    }

                    meshFilter.sharedMesh         = newMesh;
                    meshFilter.transform.position = Vector3.zero;
                }

                foreach(MeshCollider meshCollider in foundMeshColliders)
                {
                    Mesh mesh = meshCollider.sharedMesh;

                    if(!mesh.isReadable)
                    {
                        continue;
                    }

                    Mesh newMesh;

                    if(!modifiedMeshes.TryGetValue(mesh, out newMesh))
                    {
                        newMesh = (Mesh)Object.Instantiate(mesh);
                        Vector3[] vertices = mesh.vertices;

                        for(int v = 0; v < vertices.Length; v++)
                        {
                            vertices[v] += position;
                            vertices[v] -= center;
                        }

                        newMesh.vertices = vertices;
                        newMesh.RecalculateBounds();
                        modifiedMeshes[mesh] = newMesh;
                    }

                    meshCollider.sharedMesh         = newMesh;
                    meshCollider.transform.position = Vector3.zero;
                }

                GameObject prefabObj;
                GameObject modelGameObject;

                switch(exportType)
                {
                    case ExportType.FBX:
                    {
                        if(!UnityFBXExporter.FBXExporter.ExportGameObjToFBX(tempExportObject, newPath, exportColliders))
                        {
                            //InternalCSGModelManager.ClearMeshInstances();
                            EditorUtility.DisplayDialog("Warning", "Failed to export the FBX file.", "Ok");

                            return;
                        }

                        prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(newPath);

                        modelGameObject = CSGPrefabUtility.Instantiate(prefabObj);

                        foreach(MeshRenderer renderer in modelGameObject.GetComponentsInChildren<MeshRenderer>())
                        {
                            GameObject gameObject = renderer.gameObject;

                            if(gameObject.name.EndsWith("COLLIDER"))
                            {
                                MeshFilter   filter       = gameObject.GetComponent<MeshFilter>();
                                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                                meshCollider.sharedMesh = filter.sharedMesh;
                                Object.DestroyImmediate(renderer);
                                Object.DestroyImmediate(filter);
                            }
                        }

                        model.exportPath = newPath;

                        break;
                    }
                    default:
                        //case RealtimeCSG.Components.ExportType.UnityMesh:
                    {
                        prefabObj = tempExportObject; // AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath);
                        modelGameObject = tempExportObject;

                        foreach(MeshFilter meshFilter in tempExportObject.GetComponentsInChildren<MeshFilter>())
                        {
                            Mesh mesh = meshFilter.sharedMesh;
                            mesh.name       = tempExportObject.name;
                            meshFilter.mesh = mesh;
                        }

                        break;
                    }
                }

                model.exportPath = newPath;

                if(exportType == ExportType.FBX && prefabObj)
                {
                    foreach(MeshRenderer meshRenderer in prefabObj.GetComponentsInChildren<MeshRenderer>())
                    {
                        if(meshRenderer.sharedMaterials.Length != 1)
                        {
                            continue;
                        }

                        GameObject gameObject = meshRenderer.gameObject;
                        string[]   nameSplit  = gameObject.name.Split('|');

                        if(nameSplit.Length == 1)
                        {
                            continue;
                        }

                        int instanceId;

                        if(!int.TryParse(nameSplit[1], out instanceId))
                        {
                            continue;
                        }

                        Material realMaterial = EditorUtility.EntityIdToObject(instanceId) as Material;

                        if(!realMaterial)
                        {
                            continue;
                        }

                        meshRenderer.sharedMaterial = realMaterial;
                        gameObject.name             = nameSplit[0];
                    }
                }

                StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(model.gameObject);
                int               modelLayer  = model.gameObject.layer;

                foreach(Transform transform in modelGameObject.GetComponentsInChildren<Transform>())
                {
                    GameObject gameObject = transform.gameObject;
                    GameObjectUtility.SetStaticEditorFlags(gameObject, staticFlags);
                    gameObject.layer = modelLayer;
                }

                modelGameObject.transform.SetParent(model.transform, true);
                modelGameObject.transform.SetSiblingIndex(0);
                modelGameObject.tag = model.gameObject.tag;

                modelGameObject.transform.localPosition = center - position;

                Undo.RegisterCreatedObjectUndo(modelGameObject, "Instantiated model");

                CSGModelExported exported = model.gameObject.AddComponent<CSGModelExported>();
                exported.containedModel         = null;
                exported.containedExportedModel = modelGameObject;
                exported.disarm                 = true;
                Undo.RegisterCreatedObjectUndo(exported, "Created CSGModelExported");
                Undo.RegisterCompleteObjectUndo(exported, "Created CSGModelExported");

                CSGBrush[]        foundBrushes    = model.GetComponentsInChildren<CSGBrush>(true);
                CSGOperation[]    foundOperations = model.GetComponentsInChildren<CSGOperation>(true);
                GeneratedMeshes[] foundContainers = model.GetComponentsInChildren<GeneratedMeshes>(true);

                HashSet<MonoBehaviour> foundBehaviours = new HashSet<MonoBehaviour>();

                foreach(CSGBrush foundBrush in foundBrushes)
                {
                    foundBehaviours.Add(foundBrush);
                }

                foreach(CSGOperation foundOperation in foundOperations)
                {
                    foundBehaviours.Add(foundOperation);
                }

                foreach(CSGModel foundModel in foundModels)
                {
                    foundBehaviours.Add(foundModel);
                }

                foreach(GeneratedMeshes foundContainer in foundContainers)
                {
                    foundBehaviours.Add(foundContainer);
                }

                exported.hiddenComponents = new HiddenComponentData[foundBehaviours.Count];
                int index = 0;

                foreach(MonoBehaviour foundBehaviour in foundBehaviours)
                {
                    Undo.RegisterCompleteObjectUndo(foundBehaviour, "Hide component");
                    exported.hiddenComponents[index] = new HiddenComponentData { behaviour = foundBehaviour };
                    index++;
                }

                for(int i = 0; i < exported.hiddenComponents.Length; i++)
                {
                    exported.hiddenComponents[i].hideFlags = exported.hiddenComponents[i].behaviour.hideFlags;
                    exported.hiddenComponents[i].enabled   = exported.hiddenComponents[i].behaviour.enabled;
                }

                for(int i = 0; i < exported.hiddenComponents.Length; i++)
                {
                    exported.hiddenComponents[i].behaviour.hideFlags =
                        exported.hiddenComponents[i].behaviour.hideFlags | ComponentHideFlags;

                    exported.hiddenComponents[i].behaviour.enabled = false;
                }

                EditorSceneManager.MarkSceneDirty(currentScene);
                Undo.CollapseUndoOperations(groupIndex);
                groupIndex      = 0;
                exported.disarm = false;
            }
            finally
            {
                switch(exportType)
                {
                    case ExportType.FBX:
                    {
                        Object.DestroyImmediate(tempExportObject);

                        break;
                    }
                }

                if(groupIndex != 0)
                {
                    Undo.CollapseUndoOperations(groupIndex);
                }
            }
        }

        public static void ReverseExport(CSGModelExported exported)
        {
            if(exported.hiddenComponents != null)
            {
                for(int i = exported.hiddenComponents.Length - 1; i >= 0; i--)
                {
                    if(!exported.hiddenComponents[i].behaviour)
                    {
                        continue;
                    }

                    Undo.RegisterCompleteObjectUndo(exported.hiddenComponents[i].behaviour, "Show hidden component");
                    exported.hiddenComponents[i].behaviour.enabled   = exported.hiddenComponents[i].enabled;
                    exported.hiddenComponents[i].behaviour.hideFlags = exported.hiddenComponents[i].hideFlags;
                }
            }

            if(exported.containedExportedModel)
            {
                Undo.DestroyObjectImmediate(exported.containedExportedModel);
            }
        }
    }
}

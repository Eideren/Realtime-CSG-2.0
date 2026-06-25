using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    internal enum SelectionType
    {
        Replace
      , Additive
      , Subtractive
      , Toggle
    }

    internal static class SelectionUtility
    {
        private static CSGModel m_LastUsedModelInstance;

        internal static CSGModel LastUsedModel
        {
            get
            {
                CSGModel returnModel = null;

                if(m_LastUsedModelInstance != null)
                {
                    HideFlags flags = m_LastUsedModelInstance.gameObject.hideFlags;

                    if((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) == 0)
                    {
                        returnModel = m_LastUsedModelInstance;
                    }
                }

                if(returnModel != null && returnModel.gameObject.activeInHierarchy)
                {
                    return returnModel;
                }

                foreach(CSGModel model in InternalCSGModelManager.Models)
                {
                    HideFlags flags = model.gameObject.hideFlags;

                    if((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) == 0 && model.gameObject.activeInHierarchy)
                    {
                        // don't want new stuff to be added to a prefab instance
                        if(CSGPrefabUtility.IsPrefabAssetOrInstance(model.gameObject))
                        {
                            continue;
                        }

                        return model;
                    }
                }

                return null;
            }
            set
            {
                if(!value)
                {
                    return;
                }

                HideFlags flags = value.gameObject.hideFlags;

                if((flags & (HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontSaveInBuild)) != 0 && value.gameObject.activeInHierarchy)
                {
                    return;
                }

                // don't want new stuff to be added to a prefab instance
                if(CSGPrefabUtility.IsPrefabAssetOrInstance(value.gameObject))
                {
                    return;
                }

                m_LastUsedModelInstance = value;
            }
        }

        public static Transform FindParentToAssignTo(LegacyBrushIntersection intersection)
        {
            if(intersection.brush == null || CSGPrefabUtility.IsPrefabAsset(intersection.brush.gameObject))
            {
                CSGModel lastModel = LastUsedModel;

                if(lastModel == null || CSGPrefabUtility.IsPrefabAsset(lastModel.gameObject))
                {
                    return null;
                }

                return lastModel.transform;
            }

            Transform hoverParent = intersection.brush.transform.parent;
            Transform iterator    = hoverParent;

            while(iterator != null)
            {
                CSGNode node = iterator.GetComponent<CSGNode>();

                if(node != null)
                {
                    hoverParent = node.transform;
                }

                iterator = iterator.transform.parent;
            }

            if(!hoverParent)
            {
                return null;
            }

            return CSGPrefabUtility.GetCorrespondingObjectFromSource(hoverParent.gameObject) != null ? null : hoverParent;
        }

        public static Quaternion FindDragOrientation(SceneView sceneView, Vector3 normal, PrefabSourceAlignment sourceSurfaceAlignment, PrefabDestinationAlignment destinationSurfaceAlignment)
        {
            Quaternion srcRotation;

            switch(sourceSurfaceAlignment)
            {
                default:
                case PrefabSourceAlignment.AlignedFront:
                    srcRotation = Quaternion.LookRotation(MathConstants.forwardVector3); break;
                case PrefabSourceAlignment.AlignedBack:
                    srcRotation = Quaternion.LookRotation(MathConstants.backVector3); break;
                case PrefabSourceAlignment.AlignedLeft:
                    srcRotation = Quaternion.LookRotation(MathConstants.rightVector3); break;
                case PrefabSourceAlignment.AlignedRight:
                    srcRotation = Quaternion.LookRotation(MathConstants.leftVector3); break;
                case PrefabSourceAlignment.AlignedTop:
                    srcRotation = Quaternion.LookRotation(MathConstants.upVector3, MathConstants.forwardVector3); break;
                case PrefabSourceAlignment.AlignedBottom:
                    srcRotation = Quaternion.LookRotation(MathConstants.downVector3, MathConstants.backVector3); break;
            }

            switch(destinationSurfaceAlignment)
            {
                default:
                case PrefabDestinationAlignment.AlignToSurface:
                {
                    Vector3 tangent = MathConstants.upVector3; // assume up is up in the world
                    float   absX    = Mathf.Abs(normal.x);
                    float   absY    = Mathf.Abs(normal.y);
                    float   absZ    = Mathf.Abs(normal.z);

                    // if our surface is a floor / ceiling then assume up is the axis
                    // aligned vector that is most aligned with the camera's up vector
                    if(absX <= absY && absX <= absZ && absY > absZ)
                    {
                        tangent = GeometryUtility.SnapToClosestAxis(sceneView.camera ? sceneView.camera.transform.up : MathConstants.upVector3);
                    }

                    return Quaternion.LookRotation(normal, tangent) * srcRotation;
                }
                case PrefabDestinationAlignment.AlignSurfaceUp:
                {
                    normal.y = 0;
                    normal.Normalize();

                    if(normal.sqrMagnitude == 0)
                    {
                        normal = GeometryUtility.SnapToClosestAxis(sceneView.camera ? sceneView.camera.transform.forward : MathConstants.forwardVector3);
                    }

                    Vector3 tangent = MathConstants.upVector3; // assume up is up in the world
                    float   absX    = Mathf.Abs(normal.x);
                    float   absY    = Mathf.Abs(normal.y);
                    float   absZ    = Mathf.Abs(normal.z);

                    // if our surface is a floor / ceiling then assume up is the axis
                    // aligned vector that is most aligned with the camera's up vector
                    if(absX <= absY && absX <= absZ && absY > absZ)
                    {
                        tangent = GeometryUtility.SnapToClosestAxis(sceneView.camera ? sceneView.camera.transform.up : MathConstants.upVector3);
                    }

                    return Quaternion.LookRotation(normal, tangent) * srcRotation;
                }
                case PrefabDestinationAlignment.Default:
                {
                    return Quaternion.identity;
                }
            }
        }

        private static bool m_ShiftPressed;
        private static bool m_ActionKeyPressed;
        private static EventModifiers m_CurrentModifiers = EventModifiers.None;

        public static EventModifiers CurrentModifiers
        {
            get => m_CurrentModifiers;
            internal set => m_CurrentModifiers = value;
        }

        public static void HandleEvents()
        {
            switch(Event.current.type)
            {
                case EventType.MouseDown:
                case EventType.MouseUp:
                case EventType.MouseDrag:
                case EventType.MouseMove:
                case EventType.KeyDown:
                case EventType.KeyUp:
                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:
                case EventType.Repaint:
                {
                    m_ShiftPressed     = Event.current.shift;
                    m_ActionKeyPressed = EditorGUI.actionKey;

                    m_CurrentModifiers = Event.current.modifiers & (EventModifiers.Alt | EventModifiers.Control
                      | EventModifiers.Shift | EventModifiers.Command);

                    break;
                }
            }
        }

        public static void HideObjectsRemoteOnly(List<GameObject> gameObjects)
        {
            if(gameObjects == null || gameObjects.Count == 0)
            {
                return;
            }

            for(int i = gameObjects.Count - 1; i >= 0; i--)
            {
                if(gameObjects[i])
                {
                    gameObjects[i].SetActive(false);
                }
            }

            InternalCSGModelManager.CheckTransformChanged();
            InternalCSGModelManager.OnHierarchyModified();
            InternalCSGModelManager.UpdateRemoteMeshes();
        }

        public static void ShowObjectsAndUpdate(List<GameObject> gameObjects)
        {
            if(gameObjects == null || gameObjects.Count == 0)
            {
                return;
            }

            for(int i = gameObjects.Count - 1; i >= 0; i--)
            {
                if(gameObjects[i])
                {
                    gameObjects[i].SetActive(true);
                }
            }

            InternalCSGModelManager.CheckTransformChanged();
            InternalCSGModelManager.OnHierarchyModified();
            InternalCSGModelManager.UpdateMeshes(forceUpdate: true);
            MeshInstanceManager.UpdateHelperSurfaceVisibility();
        }

        public static bool IsSnappingToggled
        {
            get => m_ActionKeyPressed;
        }

        public static SelectionType GetEventSelectionType()
        {
            if(m_ShiftPressed && m_ActionKeyPressed)
            {
                return SelectionType.Subtractive;
            }

            if(m_ActionKeyPressed)
            {
                return SelectionType.Toggle;
            }

            return m_ShiftPressed ? SelectionType.Additive : SelectionType.Replace;
        }

        public static void DeselectAll()
        {
            if(EditModeManager.DeselectAll())
            {
                return;
            }

            DeselectAllBrushes();
        }

        public static void DeselectAllBrushes()
        {
            Selection.activeObject = null;
        }

        public static void ToggleSelectedObjectVisibility()
        {
            Undo.IncrementCurrentGroup();
            int undoGroupIndex = Undo.GetCurrentGroup();

            GameObject[] selected = Selection.gameObjects.ToArray();
            Undo.RecordObjects(selected, "Toggle Object Visibility");
            bool haveVisibleSelection = false;

            for(int i = 0; i < selected.Length; i++)
            {
                haveVisibleSelection = selected[i].activeInHierarchy || haveVisibleSelection;
            }

            if(haveVisibleSelection)
            {
                for(int i = 0; i < selected.Length; i++)
                {
                    selected[i].SetActive(false);
                }
            }
            else
            {
                for(int i = 0; i < selected.Length; i++)
                {
                    selected[i].SetActive(true);
                }
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
        }

        public static void HideSelectedObjects()
        {
            Undo.IncrementCurrentGroup();
            int undoGroupIndex = Undo.GetCurrentGroup();

            GameObject[] selected = Selection.gameObjects.ToArray();
            Undo.RecordObjects(selected, "Hiding Objects");

            for(int i = 0; i < selected.Length; i++)
            {
                selected[i].SetActive(false);
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
        }

        public static void UnHideAll()
        {
            Undo.IncrementCurrentGroup();
            int undoGroupIndex = Undo.GetCurrentGroup();

            for(int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene        activeScene     = SceneManager.GetSceneAt(sceneIndex);
                GameObject[] rootGameObjects = activeScene.GetRootGameObjects();

                for(int i = 0; i < rootGameObjects.Length; i++)
                {
                    Transform[] children = rootGameObjects[i].GetComponentsInChildren<Transform>(true);

                    for(int c = 0; c < children.Length; c++)
                    {
                        Transform  transform  = children[c];
                        GameObject gameObject = transform.gameObject;

                        if(gameObject.activeInHierarchy || gameObject.hideFlags != HideFlags.None)
                        {
                            continue;
                        }

                        Undo.RecordObject(gameObject, "Un-hiding Object");
                        gameObject.SetActive(true);
                    }
                }
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
        }

        public static void HideUnselectedObjects()
        {
            Undo.IncrementCurrentGroup();
            int undoGroupIndex = Undo.GetCurrentGroup();

            List<GameObject> selected    = Selection.gameObjects.ToList();
            HashSet<int>     selectedIDs = new HashSet<int>();
            CSGModel[]       models      = InternalCSGModelManager.Models;

            for(int i = 0; i < models.Length; i++)
            {
                CSGModel model = models[i];

                if(!ModelTraits.IsModelSelectable(model))
                {
                    continue;
                }

                if(!model.generatedMeshes)
                {
                    continue;
                }

                Transform[] meshContainerChildren = model.generatedMeshes.GetComponentsInChildren<Transform>();

                foreach(Transform child in meshContainerChildren)
                {
                    selected.Add(child.gameObject);
                }
            }

            // we keep adding parents, and their parents until we hit the root-objects
            for(int i = 0; i < selected.Count; i++)
            {
                selectedIDs.Add(selected[i].GetIntegerID());
                Transform transform = selected[i].transform;
                Transform parent    = transform.parent;

                if(parent == null)
                {
                    continue;
                }

                selected.Add(parent.gameObject);
            }

            for(int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene        activeScene     = SceneManager.GetSceneAt(sceneIndex);
                GameObject[] rootGameObjects = activeScene.GetRootGameObjects();

                for(int i = 0; i < rootGameObjects.Length; i++)
                {
                    Transform[] children = rootGameObjects[i].GetComponentsInChildren<Transform>();

                    for(int c = 0; c < children.Length; c++)
                    {
                        Transform  transform  = children[c];
                        GameObject gameObject = transform.gameObject;

                        if(!gameObject.activeInHierarchy || gameObject.hideFlags != HideFlags.None)
                        {
                            continue;
                        }

                        if(selectedIDs.Contains(gameObject.GetIntegerID()))
                        {
                            continue;
                        }

                        Undo.RecordObject(gameObject, "Hiding Object");
                        gameObject.SetActive(false);
                    }
                }
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
        }

#region DoSelectionClick

        public static void DoSelectionClick(SceneView sceneView, bool ignoreInvisibleSurfaces = true)
        {
            Camera camera = sceneView.camera;

            SceneQueryUtility.FindClickWorldIntersection(camera, Event.current.mousePosition, out GameObject gameobject, ignoreInvisibleSurfaces);

            gameobject = SceneQueryUtility.FindSelectionBase(gameobject);

            List<EntityId> selectedObjectsOnClick = new List<EntityId>(Selection.entityIds);
            bool addedSelection = false;

            if(EditorGUI.actionKey)
            {
                if(gameobject != null)
                {
                    EntityId instanceID = gameobject.GetEntityId();
                    
                    if (selectedObjectsOnClick.Contains(instanceID))
                    {
                        selectedObjectsOnClick.Remove(instanceID);
                    }
                    else
                    {
                        selectedObjectsOnClick.Add(instanceID);
                        addedSelection = true;
                    }

                    if(selectedObjectsOnClick.Count == 0)
                    {
                        Selection.activeTransform = null;
                    }
                    else
                    {
                        Selection.entityIds = selectedObjectsOnClick.ToArray();
                    }
                }
            }
            else if(Event.current.shift)
            {
                if(gameobject != null)
                {
                    EntityId instanceID = gameobject.GetEntityId();
                    selectedObjectsOnClick.Add(instanceID);

                    Selection.entityIds = selectedObjectsOnClick.ToArray();
                    addedSelection        = true;
                }
            }
            else if(Event.current.alt)
            {
                if(gameobject != null)
                {
                    EntityId instanceID = gameobject.GetEntityId();
                    selectedObjectsOnClick.Remove(instanceID);

                    Selection.entityIds = selectedObjectsOnClick.ToArray();

                    return;
                }
            }
            else
            {
                Selection.activeGameObject = gameobject;
                addedSelection             = true;
            }

            if(!addedSelection)
            {
                foreach(Object item in Selection.GetFiltered(typeof(CSGBrush), SelectionMode.Deep))
                {
                    CSGBrush brush = item as CSGBrush;

                    if(brush.childData == null || !ModelTraits.IsModelEditable(brush.childData.model))
                    {
                        continue;
                    }

                    LastUsedModel = brush.childData.model;

                    break;
                }
            }
            else if(gameobject != null)
            {
                CSGBrush brush = gameobject.GetComponent<CSGBrush>();

                if(brush != null)
                {
                    if(brush.childData == null || !ModelTraits.IsModelEditable(brush.childData.model))
                    {
                        return;
                    }

                    LastUsedModel = brush.childData.model;
                }
            }
        }

#endregion
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal sealed class OperationsUtility
    {
        private static readonly Type csgOperationType = typeof(CSGOperation);
        private static readonly Type csgBrushType = typeof(CSGBrush);

        public static void SetPassThroughOnSelected()
        {
            bool modified = false;

            foreach(GameObject gameObject in Selection.gameObjects)
            {
                Component obj = gameObject.GetComponent(csgOperationType);

                if(!obj)
                {
                    continue;
                }

                CSGOperation operation = obj as CSGOperation;

                if(operation.PassThrough)
                {
                    continue;
                }

                modified = true;
                Undo.RecordObject(operation, "Modifying csg operation of operation component");
                operation.PassThrough = true;
            }

            if(!modified)
            {
                return;
            }

            InternalCSGModelManager.CheckForChanges();
            EditorApplication.RepaintHierarchyWindow();
        }

        public static void ModifyOperationsOnSelected(Foundation.CSGOperationType operationType)
        {
            bool modified = false;

            foreach(GameObject gameObject in Selection.gameObjects)
            {
                Component obj = gameObject.GetComponent(csgBrushType);

                if(obj)
                {
                    CSGBrush brush = obj as CSGBrush;

                    if(brush.OperationType != operationType)
                    {
                        modified = true;
                        Undo.RecordObject(brush, "Modifying csg operation of brush component");
                        brush.OperationType = operationType;
                    }
                }

                obj = gameObject.GetComponent(csgOperationType);

                if(obj)
                {
                    CSGOperation operation = obj as CSGOperation;

                    if(operation.OperationType == operationType)
                    {
                        continue;
                    }

                    modified = true;
                    Undo.RecordObject(operation, "Modifying csg operation of operation component");
                    operation.PassThrough   = false;
                    operation.OperationType = operationType;
                }
            }

            if(modified)
            {
                InternalCSGModelManager.CheckForChanges();
                EditorApplication.RepaintHierarchyWindow();
            }
        }

        public static GameObject CreateGameObject(Transform parent, string name, bool worldPositionStays)
        {
            GameObject gameObject = name == null ? new GameObject() : new GameObject(name);

            if(parent != null && parent)
            {
                gameObject.transform.SetParent(parent, worldPositionStays);

                if(name != null)
                {
                    gameObject.name = GameObjectUtility.GetUniqueNameForSibling(parent, name);
                }
            }

            return gameObject;
        }

        private static Transform GetTransformForMenu(MenuCommand command)
        {
            Transform parent = null;

            if(command != null && command.context != null)
            {
                GameObject parentGameObject = command.context as GameObject;

                if(parentGameObject)
                {
                    parent = parentGameObject.transform;
                }
            }

            return parent;
        }

        [MenuItem("GameObject/Realtime-CSG/Model", false, 30)]
        public static CSGModel CreateModelInstanceInScene(MenuCommand command)
        {
            return CreateModelInstanceInScene(GetTransformForMenu(command));
        }

        public static CSGModel CreateModelInstanceInScene(Transform parent)
        {
            GameObject gameObject = new GameObject("Model");
            gameObject.name = GameObjectUtility.GetUniqueNameForSibling(null, "Model");
            CSGModel model = InternalCSGModelManager.CreateCSGModel(gameObject);

            Selection.activeGameObject     = gameObject;
            SelectionUtility.LastUsedModel = model;

            if(parent)
            {
                gameObject.transform.SetParent(parent, false);
            }

            Undo.RegisterCreatedObjectUndo(gameObject, "Created model");
            InternalCSGModelManager.CheckForChanges();

            return model;
        }

        public static CSGModel CreateModelInstanceInScene(bool selectModel)
        {
            GameObject gameObject = new GameObject("Model");
            gameObject.name = GameObjectUtility.GetUniqueNameForSibling(null, "Model");
            CSGModel model = InternalCSGModelManager.CreateCSGModel(gameObject);

            if(selectModel)
            {
                Selection.activeGameObject = gameObject;
            }

            SelectionUtility.LastUsedModel = model;
            Undo.RegisterCreatedObjectUndo(gameObject, "Created model");
            InternalCSGModelManager.CheckForChanges();

            return model;
        }

        public static GameObject CreateOperation(Transform parent, string name, bool worldPositionStays)
        {
            GameObject gameObject = CreateGameObject(parent, name, worldPositionStays);
            gameObject.AddComponent<CSGOperation>();
            Undo.RegisterCreatedObjectUndo(gameObject, "Created operation");
            InternalCSGModelManager.CheckForChanges();

            return gameObject;
        }

        [MenuItem("GameObject/Realtime-CSG/Operation", false, 31)]
        public static CSGOperation CreateOperationInstanceInScene(MenuCommand command)
        {
            Transform parent = GetTransformForMenu(command);

            Transform lastUsedModelTransform = !SelectionUtility.LastUsedModel ? null : SelectionUtility.LastUsedModel.transform;

            if(lastUsedModelTransform == null && !parent)
            {
                lastUsedModelTransform = CreateModelInstanceInScene(parent).transform;
                parent                 = lastUsedModelTransform;
            }
            else if(!parent)
            {
                parent = lastUsedModelTransform;
            }

            string name = GameObjectUtility.GetUniqueNameForSibling(parent, "Operation");

            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, true);
            CSGOperation operation = gameObject.AddComponent<CSGOperation>();

            Selection.activeGameObject = gameObject;
            Undo.RegisterCreatedObjectUndo(gameObject, "Created operation");
            InternalCSGModelManager.CheckForChanges();
            InternalCSGModelManager.UpdateMeshes();

            return operation;
        }

        [MenuItem("GameObject/Realtime-CSG/Brush", false, 31)]
        public static CSGBrush CreateBrushInstanceInScene(MenuCommand command)
        {
            Transform parent = GetTransformForMenu(command);

            Transform lastUsedModelTransform = !SelectionUtility.LastUsedModel ? null : SelectionUtility.LastUsedModel.transform;

            if(lastUsedModelTransform == null && !parent)
            {
                lastUsedModelTransform = CreateModelInstanceInScene(parent).transform;
                parent                 = lastUsedModelTransform;
            }
            else if(!parent)
            {
                parent = lastUsedModelTransform;
            }

            string     name       = GameObjectUtility.GetUniqueNameForSibling(parent, "Brush");
            GameObject gameObject = new GameObject(name);
            CSGBrush   brush      = gameObject.AddComponent<CSGBrush>();

            gameObject.transform.SetParent(parent, true);
            gameObject.transform.position = new Vector3(0.5f, 0.5f, 0.5f); // this aligns it's vertices to the grid
            BrushFactory.CreateCubeControlMesh(out brush.ControlMesh, out brush.Shape, Vector3.one);

            Selection.activeGameObject = gameObject;
            Undo.RegisterCreatedObjectUndo(gameObject, "Created brush");
            InternalCSGModelManager.CheckForChanges();
            InternalCSGModelManager.UpdateMeshes();

            return brush;
        }

        [MenuItem("GameObject/Group selection %G", false, 32)]
        public static void GroupSelectionInOperation()
        {
            if(Selection.activeObject == null)
            {
                return;
            }

            List<Transform> childTransforms = new List<Transform>(Selection.transforms);
            Selection.activeObject = null;

            if(childTransforms.Count == 0)
            {
                return;
            }

            for(int i = childTransforms.Count - 1; i >= 0; i--)
            {
                Transform iterator = childTransforms[i].parent;
                bool      found    = false;

                while(iterator != null)
                {
                    if(childTransforms.Contains(iterator))
                    {
                        found = true;

                        break;
                    }

                    iterator = iterator.parent;
                }

                if(found)
                {
                    childTransforms.RemoveAt(i);
                }
            }

            SortedList<int, Transform> sortedChildTransform = new SortedList<int, Transform>();

            for(int i = 0; i < childTransforms.Count; i++)
            {
                sortedChildTransform.Add(childTransforms[i].GetSiblingIndex(), childTransforms[i]);
            }

            childTransforms.Clear();
            childTransforms.AddRange(sortedChildTransform.Values);

            List<Transform> parentTransforms = new List<Transform>(childTransforms.Count);
            List<Transform> groupTransforms  = new List<Transform>(childTransforms.Count);
            List<int>       parentIndices    = new List<int>(childTransforms.Count);

            for(int i = 0; i < childTransforms.Count; i++)
            {
                int index = parentTransforms.IndexOf(childTransforms[i].parent);

                if(index == -1)
                {
                    Transform parent = childTransforms[i].parent;
                    parentTransforms.Add(parent);
                    index = parentTransforms.Count - 1;
                }

                parentIndices.Add(index);
            }

            Undo.IncrementCurrentGroup();
            int undoGroupIndex = Undo.GetCurrentGroup();

            foreach(Transform parentTransform in parentTransforms)
            {
                string       name      = GameObjectUtility.GetUniqueNameForSibling(parentTransform, "Operation");
                GameObject   group     = CreateGameObject(parentTransform, name, false);
                CSGOperation operation = group.AddComponent<CSGOperation>();
                operation.PassThrough = true;
                groupTransforms.Add(group.transform);
                Undo.RegisterCreatedObjectUndo(group, "Created operation");
            }

            Transform prevGroup = null;

            for(int i = 0; i < childTransforms.Count; i++)
            {
                Transform group = groupTransforms[parentIndices[i]];
                int       index = childTransforms[i].GetSiblingIndex();
                Undo.SetTransformParent(childTransforms[i], group, "Moved gameObject under operation");
                Undo.RecordObject(childTransforms[i], "Set sibling index of operation");

                if(prevGroup != group)
                {
                    group.SetSiblingIndex(index);
                }

                prevGroup = group;
            }

            Undo.CollapseUndoOperations(undoGroupIndex);
        }
    }
}

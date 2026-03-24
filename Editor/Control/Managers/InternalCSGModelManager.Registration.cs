using System;
using System.Collections.Generic;
using System.Linq;
using InternalRealtimeCSG;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using RealtimeCSG.Legacy;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal partial class InternalCSGModelManager
    {
        internal static CSGModel[] Models = Array.Empty<CSGModel>();

        private static readonly HashSet<CSGModel> modelLookup = new HashSet<CSGModel>();
        private static readonly HashSet<CSGModel> registerModels = new HashSet<CSGModel>();
        private static readonly HashSet<CSGModel> enableModels = new HashSet<CSGModel>();
        private static readonly HashSet<CSGModel> disableModels = new HashSet<CSGModel>();
        private static readonly HashSet<int> removedModels = new HashSet<int>();

        internal static List<CSGOperation> Operations = new List<CSGOperation>();

        private static readonly HashSet<CSGOperation> registerOperations = new HashSet<CSGOperation>();
        private static readonly HashSet<CSGOperation> validateOperations = new HashSet<CSGOperation>();
        private static readonly HashSet<CSGOperation> unregisterOperations = new HashSet<CSGOperation>();
        private static readonly HashSet<CSGOperation> operationTransformChanged = new HashSet<CSGOperation>();
        private static readonly HashSet<int> removedOperations = new HashSet<int>();

        internal static List<CSGBrush> Brushes = new List<CSGBrush>();

        private static readonly HashSet<CSGBrush> registerBrushes = new HashSet<CSGBrush>();
        private static readonly HashSet<CSGBrush> validateBrushes = new HashSet<CSGBrush>();
        private static readonly HashSet<CSGBrush> unregisterBrushes = new HashSet<CSGBrush>();
        private static readonly HashSet<CSGBrush> brushTransformChanged = new HashSet<CSGBrush>();
        private static readonly HashSet<int> removedBrushes = new HashSet<int>();

#region ClearRegistration

        internal static void ClearRegistration()
        {
            removedBrushes.Clear();
            removedOperations.Clear();
            removedModels.Clear();

            registerModels.Clear();
            enableModels.Clear();
            disableModels.Clear();

            registerOperations.Clear();
            validateOperations.Clear();
            unregisterOperations.Clear();

            registerBrushes.Clear();
            validateBrushes.Clear();
            unregisterBrushes.Clear();

            modelLookup.Clear();

            operationTransformChanged.Clear();
            brushTransformChanged.Clear();

            Models     = Array.Empty<CSGModel>();
            Operations = new List<CSGOperation>();
            Brushes    = new List<CSGBrush>();
        }

#endregion

#region RegisterAllComponents

        private static bool RegisterAllComponents()
        {
            if(External == null)
            {
                return false;
            }

            Clear();

            if(CSGModelManager.IsInPlayMode)
            {
                return false;
            }

            List<CSGNode> allNodes = new List<CSGNode>();

            if(CSGPrefabUtility.AreInPrefabMode())
            {
                allNodes.AddRange(CSGPrefabUtility.GetNodesInPrefabMode());
            }

            for(int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);

                if(!scene.isLoaded)
                {
                    continue;
                }

                List<CSGNode> sceneNodes = SceneQueryUtility.GetAllComponentsInScene<CSGNode>(scene);
                allNodes.AddRange(sceneNodes);
            }

            for(int i = allNodes.Count - 1; i >= 0; i--)
            {
                Reset(allNodes[i]);
            }

            for(int i = 0; i < allNodes.Count; i++)
            {
                AddNodeRegistration(allNodes[i]);
            }

            return true;
        }

#endregion

#region Reset

        public static void Reset(CSGNode node)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            CSGBrush brush = node as CSGBrush;

            if(brush)
            {
                Reset(brush);

                return;
            }

            CSGOperation operation = node as CSGOperation;

            if(operation && !operation.PassThrough)
            {
                Reset(operation);

                return;
            }

            CSGModel model = node as CSGModel;

            if(model)
            {
                Reset(model);
            }
        }

#endregion

#region AddNodeRegistration

        private static void AddNodeRegistration(CSGNode node)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            CSGBrush brush = node as CSGBrush;

            if(brush)
            {
                registerBrushes.Add(brush);

                return;
            }

            CSGOperation operation = node as CSGOperation;

            if(operation && !operation.PassThrough)
            {
                registerOperations.Add(operation);

                return;
            }

            CSGModel model = node as CSGModel;

            if(model)
            {
                registerModels.Add(model);
            }
        }

#endregion

#region CreateCSGModel

        internal static CSGModel CreateCSGModel(GameObject gameObject)
        {
            CSGModel model = gameObject.AddComponent<CSGModel>();

            const StaticEditorFlags defaultFlags =
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.NavigationStatic | // TODO: fix navigation flags
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.OffMeshLinkGeneration |
                StaticEditorFlags.ReflectionProbeStatic;

            GameObjectUtility.SetStaticEditorFlags(gameObject, defaultFlags);

            return model;
        }

#endregion

#region Brush events

        public static void OnCreated(CSGBrush component)
        {
            registerBrushes.Add(component);
            validateBrushes.Remove(component);
            unregisterBrushes.Remove(component);
            m_IsHierarchyModified = true;
        }

        public static void OnEnabled(CSGBrush component)
        {
            registerBrushes.Add(component);
            validateBrushes.Remove(component);
            unregisterBrushes.Remove(component);
            m_IsHierarchyModified = true;
        }

        public static void OnValidate(CSGBrush component)
        {
            if(!unregisterBrushes.Contains(component))
            {
                validateBrushes.Add(component);
            }
        }

        public static void OnTransformParentChanged(CSGBrush component)
        {
            OnBrushTransformChanged(component);
        }

        public static void OnDisabled(CSGBrush component)
        {
            registerBrushes.Remove(component);
            validateBrushes.Remove(component);
            unregisterBrushes.Add(component);
            m_IsHierarchyModified = true;
        }

        public static void OnDestroyed(CSGBrush component)
        {
            registerBrushes.Remove(component);
            validateBrushes.Remove(component);
            unregisterBrushes.Remove(component);
            UnregisterBrush(component);
            m_IsHierarchyModified = true;
        }

        public static void EnsureInitialized(CSGBrush component)
        {
            if(!component)
            {
                if(component.IsRegistered)
                {
                    OnDestroyed(component);
                }

                return;
            }

            bool dirty = false;

            bool statement1 = component.hierarchyItem.TransformID == 0;
            bool statement3 = component.brushNodeID != CSGNode.InvalidNodeID;
            bool statement4 = statement1 && statement3;

            if(statement4)
            {
                dirty                                        = component.hierarchyItem.TransformInitialized;
                component.hierarchyItem.Transform            = component.transform;
                component.hierarchyItem.TransformID          = component.transform.GetInstanceID();
                component.hierarchyItem.NodeID               = component.brushNodeID;
                component.hierarchyItem.TransformInitialized = true;
            }

            // make sure that the surface array is not empty,
            //  otherwise nothing can get rendered
            if(component.Shape == null || component.Shape.Surfaces == null || component.Shape.Surfaces.Length == 0 || component.Shape.TexGens == null || component.Shape.TexGens.Length == 0)
            {
                dirty = true;
                BrushFactory.CreateCubeControlMesh(out component.ControlMesh, out component.Shape, Vector3.one);
            }

            if(component.ControlMesh == null)
            {
                component.ControlMesh = ControlMeshUtility.EnsureValidControlMesh(component);
            }

            dirty = ShapeUtility.EnsureInitialized(component.Shape) || dirty;

            if(dirty)
            {
                EditorUtility.SetDirty(component);
            }
        }

        public static void Reset(CSGBrush component)
        {
            if(component.IsRegistered)
            {
                if(component.ChildData != null)
                {
                    component.ChildData.Reset();
                }

                if(component.hierarchyItem != null)
                {
                    component.hierarchyItem.Reset();
                }
            }

            component.brushNodeID = CSGNode.InvalidNodeID;
        }

#endregion

#region Operation events

        public static void OnCreated(CSGOperation component)
        {
            registerOperations.Add(component);
            validateOperations.Remove(component);
            unregisterOperations.Remove(component);
            m_IsHierarchyModified = true;
        }

        public static void OnEnabled(CSGOperation component)
        {
            registerOperations.Add(component);
            validateOperations.Remove(component);
            unregisterOperations.Remove(component);
            m_IsHierarchyModified = true;
        }

        public static void OnValidate(CSGOperation component)
        {
            if(!unregisterOperations.Contains(component))
            {
                validateOperations.Add(component);
            }
        }

        public static void OnTransformParentChanged(CSGOperation component)
        {
            OnOperationTransformChanged(component);
        }

        public static void OnPassthroughChanged(CSGOperation component)
        {
            if(!component.PassThrough)
            {
                return;
            }

            CSGOperation parent = component.ChildData.Parent;

            if(parent)
            {
                parent.ClearCache();
            }
            else
            {
                CSGModel model = component.ChildData.Model;

                if(model)
                {
                    model.ClearCache();
                }
            }
        }

        public static void OnDisabled(CSGOperation component)
        {
            registerOperations.Remove(component);
            validateOperations.Remove(component);
            unregisterOperations.Add(component);
            m_IsHierarchyModified = true;
        }

        public static void OnDestroyed(CSGOperation component)
        {
            registerOperations.Remove(component);
            validateOperations.Remove(component);
            unregisterOperations.Remove(component);
            UnregisterOperation(component);
            m_IsHierarchyModified = true;
        }

        public static void EnsureInitialized(CSGOperation component)
        {
            if(!component)
            {
                if(component.IsRegistered)
                {
                    OnDestroyed(component);
                }

                return;
            }

            if(component.ParentData.TransformID == 0 && component.operationNodeID != CSGNode.InvalidNodeID)
            {
                component.ParentData.Transform   = component.transform;
                component.ParentData.TransformID = component.transform.GetInstanceID();
                component.ParentData.NodeID      = component.operationNodeID;
            }
        }

        public static void Reset(CSGOperation component)
        {
            if(component.IsRegistered)
            {
                if(component.ParentData != null)
                {
                    component.ParentData.Reset();
                }

                if(component.ChildData != null)
                {
                    component.ChildData.Reset();
                }
            }

            component.operationNodeID = CSGNode.InvalidNodeID;
        }

#endregion

#region Model events

        public static void OnCreated(CSGModel component)
        {
            registerModels.Add(component);
            enableModels.Remove(component);
            disableModels.Remove(component);
            m_IsHierarchyModified = true;
        }

        public static void OnEnabled(CSGModel component)
        {
            enableModels.Add(component);
            disableModels.Remove(component);
            m_IsHierarchyModified = true;
        }

        public static void OnValidate(CSGModel component)
        {
            registerModels.Add(component);
            enableModels.Remove(component);
            disableModels.Remove(component);
        }

        public static void OnUpdate(CSGModel component)
        {
            RegisterModel(component);

            registerModels.Add(component);
            enableModels.Remove(component);
            disableModels.Remove(component);
        }

        public static void OnTransformChildrenChanged(CSGModel component)
        {
            if(!component)
            {
                return;
            }

            MeshInstanceManager.ValidateModelDelayed(component, true);
        }

        public static void OnDisabled(CSGModel component)
        {
            enableModels.Remove(component);
            disableModels.Add(component);
            m_IsHierarchyModified = true;
        }

        public static void OnDestroyed(CSGModel component)
        {
            if(!component)
            {
                return;
            }

            registerModels.Remove(component);
            enableModels.Remove(component);
            disableModels.Remove(component);

            UnregisterModel(component);
            MeshInstanceManager.Destroy(component.generatedMeshes);
            component.generatedMeshes = null;
            m_IsHierarchyModified     = true;
        }

        public static void EnsureInitialized(CSGModel component)
        {
            if(!component)
            {
                if(component.IsRegistered)
                {
                    OnDestroyed(component);
                }

                return;
            }

            if(component.parentData.TransformID == 0 &&
               component.modelNodeID != CSGNode.InvalidNodeID)
            {
                component.parentData.Transform   = component.transform;
                component.parentData.TransformID = component.transform.GetInstanceID();
                component.parentData.NodeID      = component.modelNodeID;
            }
        }

        public static void Reset(CSGModel component)
        {
            if(component.IsRegistered)
            {
                if(component.parentData != null)
                {
                    component.parentData.Reset();
                }
            }

            component.modelNodeID = CSGNode.InvalidNodeID;
        }

#endregion

#region RegisterChild

        private static void RegisterChild(ChildNodeData childData)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            // make sure our model has actually been initialized
            if(childData.Model && !childData.Model.IsRegistered)
            {
                RegisterModel(childData.Model);
            }

            // make sure our parent (if any) has actually been initialized
            if(childData.Parent && !childData.Parent.IsRegistered)
            {
                registerOperations.Remove(childData.Parent);
                unregisterOperations.Remove(childData.Parent);
                RegisterOperation(childData.Parent);
            }
        }

#endregion

#region RegisterBrush

        private static void RegisterBrush(CSGBrush brush)
        {
            if(CSGModelManager.IsInPlayMode || External == null || !brush || !brush.isActiveAndEnabled || brush.IsRegistered)
            {
                return;
            }

            if(!External.generateBrush(brush.GetInstanceID(), out brush.brushNodeID))
            {
                Debug.LogError("Failed to generate ID for brush", brush);

                return;
            }

            Transform brushTransform = brush.transform;
            InitializeHierarchy(brush.ChildData, brushTransform);
            InitializeHierarchy(brush, brushTransform);

            brush.prevOperation = brush.OperationType;
            EnsureInitialized(brush);

            brush.controlMeshGeneration = brush.ControlMesh.Generation;
            brush.compareShape.EnsureInitialized(brush.Shape);

            RegisterChild(brush.ChildData);

            int brushNodeID = brush.brushNodeID;

            if(brush.ChildData.Model == null)
            {
                if(brush.brushNodeID != CSGNode.InvalidNodeID)
                {
                    External.destroyNode(brush.brushNodeID);
                    brush.brushNodeID = CSGNode.InvalidNodeID;
                }

                Debug.LogError("Brush has no registered model?");

                return;
            }

            ParentNodeData parentData = GetParentData(brush.ChildData);

            if(parentData == null)
            {
                return;
            }

            EnsureInitialized(brush);

            brush.ChildData.OwnerParentData = parentData;
            brush.ChildData.ModelTransform  = !brush.ChildData.Model ? null : brush.ChildData.Model.transform;

            parentData.AddNode(brush.hierarchyItem);

            if(brush.Shape == null || brush.Shape.Surfaces == null || brush.Shape.Surfaces.Length < 4)
            {
                Debug.LogError($"The brush ({brush.name}) is infinitely thin and is invalid", brush);
                brush.ControlMesh.Valid = false;
            }
            else
            {
                Matrix4x4 brushToModelSpace = brush.compareTransformation.brushToModelSpaceMatrix;
                External.setBrushToModelSpace(brushNodeID, brushToModelSpace);
                External.setBrushOperationType(brushNodeID, brush.OperationType);

                if((brush.Flags & BrushFlags.InfiniteBrush) != BrushFlags.InfiniteBrush)
                {
                    External.setBrushFlags(brushNodeID, CSGTreeBrushFlags.Default);
                    SetBrushMesh(brush);
                }
                else
                {
                    SetBrushMesh(brush);
                    External.setBrushFlags(brushNodeID, CSGTreeBrushFlags.Infinite);
                }
            }

            Brushes.Add(brush);
            m_IsHierarchyModified = true;
        }

#endregion

#region RegisterOperation

        private static void RegisterOperation(CSGOperation op)
        {
            if(CSGModelManager.IsInPlayMode || External == null || !op || !op.isActiveAndEnabled || op.PassThrough || op.IsRegistered)
            {
                return;
            }

            if(!External.generateOperation(op.GetInstanceID(), out op.operationNodeID))
            {
                Debug.LogError("Failed to generate ID for operation");

                return;
            }

            EnsureInitialized(op);

            InitializeHierarchy(op.ChildData, op.transform);
            op.PrevOperation   = op.OperationType;
            op.PrevPassThrough = op.PassThrough;

            UpdateChildrenParent(op, op.transform);

            RegisterChild(op.ChildData);

            if(!op.ChildData.Model)
            {
                External.destroyNode(op.operationNodeID);
                op.operationNodeID   = CSGNode.InvalidNodeID;
                op.ParentData.NodeID = CSGNode.InvalidNodeID;
                Debug.LogError("Operation has no registered model?");

                return;
            }

            ParentNodeData parentData = GetParentData(op.ChildData);

            if(parentData == null)
            {
                Debug.LogError("!GetParentData");

                return;
            }

            op.ChildData.OwnerParentData = parentData;
            op.ChildData.ModelTransform  = !op.ChildData.Model ? null : op.ChildData.Model.transform;

            parentData.AddNode(op.ParentData);

            int opID = op.operationNodeID;

            External.setOperationOperationType(opID, op.OperationType);
            Operations.Add(op);
            op.ParentData.NodeID  = opID;
            m_IsHierarchyModified = true;
        }

#endregion

#region RegisterModel

        private static void RegisterModel(CSGModel model)
        {
            if(CSGModelManager.IsInPlayMode || External == null || !model || !model.isActiveAndEnabled || model.IsRegistered)
            {
                return;
            }

            if(model.IsRegistered)
            {
                if(modelLookup.Contains(model))
                {
                    return;
                }

                Debug.LogWarning($"Model named {model.name} marked as registered, but wasn't actually registered", model);
            }

            if(!model.DefaultPhysicsMaterial)
            {
                model.DefaultPhysicsMaterial = MaterialUtility.DefaultPhysicsMaterial;
            }

            if(!External.generateModel(model.GetInstanceID(), out model.modelNodeID))
            {
                Debug.LogError($"Failed to generate ID for model named {model.name}");

                return;
            }

            EnsureInitialized(model);

            model.isActive = ModelTraits.IsModelEditable(model);

            External.setModelEnabled(model.modelNodeID, model.isActive);

            ArrayUtility.Add(ref Models, model);
            modelLookup.Add(model);
            UpdateChildrenModel(model, model.transform);
            m_IsHierarchyModified = true;
        }

#endregion

#region SetBrushMesh

        // set the polygons using the control-mesh if we have one
        private static bool SetBrushMesh(CSGBrush brush)
        {
            if(!brush)
            {
                return false;
            }

            Shape       shape       = brush.Shape;
            ControlMesh controlMesh = brush.ControlMesh;

            BrushMesh brushMesh = BrushFactory.GenerateFromControlMesh(controlMesh, shape, brush.ChildData.Model.DefaultPhysicsMaterial);

            if(brushMesh == null)
            {
                return false;
            }

            int brushMeshID = External.getBrushMeshID(brush.brushNodeID);

            if(brushMeshID != CSGNode.InvalidNodeID)
            {
                return External.updateBrushMesh(brushMeshID, brushMesh);
            }

            brushMeshID = External.createBrushMesh(brush.GetInstanceID(), brushMesh);

            return External.setBrushMeshID(brush.brushNodeID, brushMeshID);
        }

#endregion

#region SetBrushSurfaces

        // set the polygons using the control-mesh if we have one
        public static void SetBrushMeshSurfaces(CSGBrush brush)
        {
            if(External == null)
            {
                return;
            }

            SetBrushMesh(brush);
        }

#endregion

#region UnregisterBrush

        private static void UnregisterBrush(CSGBrush brush)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(External == null)
            {
                return;
            }

            if(!brush || !brush.IsRegistered)
            {
                return;
            }

            int brushNodeID = brush.brushNodeID;

            if(brush.ChildData != null && brush.ChildData.Model && brush.ChildData.Model.IsRegistered)
            {
                ParentNodeData parentData = GetParentData(brush.ChildData);

                if(parentData != null)
                {
                    parentData.RemoveNode(brush.hierarchyItem);
                }
            }

            Brushes.Remove(brush);
            removedBrushes.Add(brushNodeID);

            // did we remove component, or entire game-object?
            if(brush && brush.gameObject)
            {
                Transform transform = brush.transform;

                if(brush.gameObject.activeInHierarchy)
                {
                    FindParentOperation(transform, out CSGOperation parent);
                    UpdateChildrenParent(parent, transform);
                }
            }

            brush.brushNodeID = CSGNode.InvalidNodeID;
            brush.ClearCache();
        }

#endregion

#region UnregisterOperation

        private static void UnregisterOperation(CSGOperation op)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(External == null)
            {
                return;
            }

            if(!op.IsRegistered)
            {
                return;
            }

            int opID = op.operationNodeID;

            if(op.ChildData.Model && op.ChildData.Model.IsRegistered)
            {
                ParentNodeData parentData = GetParentData(op.ChildData);

                if(parentData != null && op.ParentData != null)
                {
                    parentData.RemoveNode(op.ParentData);
                }
            }

            Operations.Remove(op);
            removedOperations.Add(opID);

            // did we remove component, or entire game-object?
            if(op && op.gameObject)
            {
                Transform transform = op.transform;

                if(op.gameObject.activeInHierarchy)
                {
                    FindParentOperation(transform, out CSGOperation parent);
                    UpdateChildrenParent(parent, transform);
                }
            }

            op.operationNodeID = CSGNode.InvalidNodeID;

            op.ClearCache();
        }

#endregion

#region UnregisterModel

        private static void UnregisterModel(CSGModel model)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(!model.IsRegistered)
            {
                if(!modelLookup.Contains(model))
                {
                    return;
                }

                Debug.LogWarning("model marked as unregistered, but was registered");
            }

            // did we remove component, or entire game-object?
            if(model && model.gameObject)
            {
                Transform transform = model.transform;

                if(model.gameObject.activeInHierarchy)
                {
                    // NOTE: returns default model when it can't find parent model
                    FindParentModel(transform, out CSGModel parentModel);

                    // make sure it's children are set to another model
                    UpdateChildrenModel(parentModel, transform);
                }
            }

            removedModels.Add(model.modelNodeID);

            ArrayUtility.Remove(ref Models, model);
            modelLookup.Remove(model);

            model.modelNodeID = CSGNode.InvalidNodeID;

            MeshGeneration++;
        }

#endregion

#region EnableModel

        private static void EnableModel(CSGModel model)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(External == null)
            {
                return;
            }

            if(!model.IsRegistered)
            {
                return;
            }

            bool enabled = ModelTraits.IsModelEditable(model);

            if(enabled == model.isActive)
            {
                return;
            }

            model.isActive = enabled;

            External.setModelEnabled(model.modelNodeID, enabled);
            MeshInstanceManager.OnEnable(model.generatedMeshes);
        }

#endregion

#region DisableModel

        private static void DisableModel(CSGModel model)
        {
            if(CSGModelManager.IsInPlayMode)
            {
                return;
            }

            if(External == null)
            {
                return;
            }

            if(!model.IsRegistered)
            {
                return;
            }

            External.setModelEnabled(model.modelNodeID, false);

            model.isActive = false;
            MeshInstanceManager.OnDisable(model.generatedMeshes);
        }

#endregion

#region ValidateOperation

        public static void ValidateOperation(CSGOperation op)
        {
            if(External == null)
            {
                return;
            }

            // make sure that this is not a prefab that's not actually in the scene ...
            if(!op || !op.gameObject.activeInHierarchy)
            {
                return;
            }

            EnsureInitialized(op);

            if(!op.IsRegistered)
            {
                registerOperations.Remove(op);
                unregisterOperations.Remove(op);
                RegisterOperation(op);

                // make sure it's registered, otherwise ignore it
                if(!op.IsRegistered)
                {
                    return;
                }
            }

            if(op.OperationType != op.PrevOperation)
            {
                op.PrevOperation = op.OperationType;

                External.setOperationOperationType(op.operationNodeID, op.OperationType);
            }
        }

#endregion

#region CheckSurfaces

        public static void CheckSurfaceModifications(CSGBrush[] brushes)
        {
            for(int t = 0; t < brushes.Length; t++)
            {
                CSGBrush brush = brushes[t];
                CheckSurfaceModifications(brush, true);
            }
        }

        public static void CheckSurfaceModifications(CSGBrush brush, bool surfacesModified = false)
        {
            if(External == null || !brush)
            {
                return;
            }

            if(brush.Shape == null || brush.Shape.Surfaces == null || brush.Shape.Surfaces.Length < 4)
            {
                if(brush.ControlMesh != null)
                {
                    brush.ControlMesh.Valid = false;
                }

                return;
            }

            Shape        shape        = brush.Shape;
            CompareShape compareShape = brush.compareShape;

            if(compareShape.prevSurfaces == null || compareShape.prevTexGens == null)
            {
                Debug.LogWarning("storedShape.prevSubShapes == null || storedShape.prevTexGens == null");

                return;
            }

            bool updateTexGens = compareShape.prevTexGens == null || compareShape.prevTexGens.Length != shape.TexGens.Length || surfacesModified;

            if(!updateTexGens)
            {
                for(int i = 0; i < shape.TexGens.Length; i++)
                {
                    if(shape.TexGens[i].RotationAngle == compareShape.prevTexGens[i].RotationAngle &&
                       shape.TexGens[i].Scale == compareShape.prevTexGens[i].Scale &&
                       shape.TexGens[i].Translation == compareShape.prevTexGens[i].Translation &&
                       shape.TexGens[i].SmoothingGroup == compareShape.prevTexGens[i].SmoothingGroup &&
                       shape.TexGenFlags[i] == compareShape.prevTexGenFlags[i])
                    {
                        continue;
                    }

                    updateTexGens = true;

                    break;
                }

                if(!updateTexGens)
                {
                    for(int i = 0; i < shape.TexGens.Length; i++)
                    {
                        if(shape.TexGens[i].RenderMaterial == compareShape.prevTexGens[i].RenderMaterial)
                        {
                            continue;
                        }

                        updateTexGens = true;

                        break;
                    }
                }
            }

            if(compareShape.prevSurfaces.Length != shape.Surfaces.Length)
            {
                surfacesModified = true;
            }
            else if(!surfacesModified)
            {
                if(shape == null || shape.Surfaces == null || shape.Surfaces.Length < 4)
                {
                    Debug.LogError("Shape is infinitely thin and is invalid");
                    brush.ControlMesh.Valid = false;
                }
                else
                {
                    for(int surfaceIndex = 0; surfaceIndex < shape.Surfaces.Length; surfaceIndex++)
                    {
                        if(shape.Surfaces[surfaceIndex].Plane.normal
                        != compareShape.prevSurfaces[surfaceIndex].Plane.normal ||
                           shape.Surfaces[surfaceIndex].Plane.d != compareShape.prevSurfaces[surfaceIndex].Plane.d ||
                           shape.Surfaces[surfaceIndex].Tangent != compareShape.prevSurfaces[surfaceIndex].Tangent)
                        {
                            surfacesModified = true;

                            break;
                        }

                        if(shape.Surfaces[surfaceIndex].TexGenIndex != compareShape.prevSurfaces[surfaceIndex].TexGenIndex)
                        {
                            updateTexGens = true;
                        }
                    }
                }
            }

            if(updateTexGens || surfacesModified)
            {
                if(shape == null || shape.Surfaces == null || shape.Surfaces.Length < 4)
                {
                    Debug.LogError("Shape is infinitely thin and is invalid");
                    brush.ControlMesh.Valid = false;
                }
                else
                {
                    SetBrushMeshSurfaces(brush);
                    surfacesModified = false;

                    if(compareShape.prevSurfaces == null || compareShape.prevSurfaces.Length != shape.Surfaces.Length)
                    {
                        compareShape.prevSurfaces = new Surface[shape.Surfaces.Length];
                    }

                    if(shape.Surfaces.Length > 0)
                    {
                        Array.Copy(shape.Surfaces, compareShape.prevSurfaces, shape.Surfaces.Length);
                    }
                }
            }

            if(surfacesModified)
            {
                SetBrushMesh(brush);
            }
        }

#endregion

#region ValidateBrush

        public static void ValidateBrush(CSGBrush brush, bool surfacesModified = false)
        {
            if(External == null)
            {
                return;
            }

            // make sure that this is not a prefab that's not actually in the scene ...
            if(!brush || !brush.gameObject.activeInHierarchy)
            {
                return;
            }

            if(!brush.IsRegistered)
            {
                registerBrushes.Remove(brush);
                unregisterBrushes.Remove(brush);
                RegisterBrush(brush);

                // make sure it's registered, otherwise ignore it
                if(!brush.IsRegistered)
                {
                    return;
                }
            }

            EnsureInitialized(brush);

            bool wasRegistered = brush.IsRegistered;

            if(brush.OperationType != brush.prevOperation)
            {
                brush.prevOperation = brush.OperationType;

                External.setBrushOperationType(brush.brushNodeID, brush.OperationType);
            }

            // check if surfaces changed
            CheckSurfaceModifications(brush, surfacesModified);

            if(wasRegistered && brush.controlMeshGeneration != brush.ControlMesh.Generation)
            {
                brush.controlMeshGeneration = brush.ControlMesh.Generation;
                ControlMeshUtility.RebuildShape(brush);
            }
        }

#endregion

        private static readonly CSGBrush[] emptyBrushArray = Array.Empty<CSGBrush>();
        private static readonly CSGOperation[] emptyOperationArray = Array.Empty<CSGOperation>();
        private static readonly CSGModel[] emptyModelArray = Array.Empty<CSGModel>();

        internal static void UpdateRegistration()
        {
            for(int i = 0; i < Brushes.Count; i++)
            {
                CSGBrush brush = Brushes[i];

                if(!brush && brush.IsRegistered)
                {
                    OnDestroyed(brush);
                }
            }

            for(int i = 0; i < Operations.Count; i++)
            {
                CSGOperation operation = Operations[i];

                if(!operation && operation.IsRegistered)
                {
                    OnDestroyed(operation);
                }
            }

            for(int i = 0; i < Models.Length; i++)
            {
                CSGModel model = Models[i];

                if(!model && model.IsRegistered)
                {
                    OnDestroyed(model);
                }
            }

            // unregister old components
            if(unregisterBrushes.Count > 0 || unregisterOperations.Count > 0 || disableModels.Count > 0)
            {
                CSGBrush[]     unregisterBrushesList    = unregisterBrushes.Count == 0 ? emptyBrushArray : unregisterBrushes.ToArray();
                CSGOperation[] unregisterOperationsList = unregisterOperations.Count == 0 ? emptyOperationArray : unregisterOperations.ToArray();
                CSGModel[]     disableModelsList        = disableModels.Count == 0 ? emptyModelArray : disableModels.ToArray();

                unregisterBrushes.Clear();
                unregisterOperations.Clear();
                disableModels.Clear();

                if(unregisterBrushesList.Length > 0 || unregisterOperationsList.Length > 0 || disableModelsList.Length > 0)
                {
                    m_IsHierarchyModified = true;
                }

                for(int i = 0; i < unregisterBrushesList.Length; i++)
                {
                    CSGBrush component = unregisterBrushesList[i];

                    if(component)
                    {
                        UnregisterBrush(component);
                    }
                }

                for(int i = 0; i < unregisterOperationsList.Length; i++)
                {
                    CSGOperation component = unregisterOperationsList[i];

                    if(component)
                    {
                        UnregisterOperation(component);
                    }
                }

                for(int i = 0; i < disableModelsList.Length; i++)
                {
                    CSGModel component = disableModelsList[i];

                    if(component)
                    {
                        DisableModel(component);
                    }
                }
            }

            // register new components
            if(registerModels.Count > 0 || enableModels.Count > 0 || registerOperations.Count > 0
            || registerBrushes.Count > 0)
            {
                CSGModel[]     registerModelsList     = registerModels.Count == 0 ? emptyModelArray : registerModels.ToArray();
                CSGModel[]     enableModelsList       = enableModels.Count == 0 ? emptyModelArray : enableModels.ToArray();
                CSGOperation[] registerOperationsList = registerOperations.Count == 0 ? emptyOperationArray : registerOperations.ToArray();
                CSGBrush[]     registerBrushesList    = registerBrushes.Count == 0 ? emptyBrushArray : registerBrushes.ToArray();

                registerModels.Clear();
                enableModels.Clear();
                registerOperations.Clear();
                registerBrushes.Clear();

                if(registerBrushesList.Length > 0 || registerModelsList.Length > 0 || enableModelsList.Length > 0 || registerOperationsList.Length > 0)
                {
                    m_IsHierarchyModified = true;
                }

                HashSet<GameObject> prefabInstances = new HashSet<GameObject>();
                HashSet<GameObject> foundPrefabs    = new HashSet<GameObject>();

                for(int i = registerModelsList.Length - 1; i >= 0; i--)
                {
                    CSGModel component = registerModelsList[i];

                    if(!component)
                    {
                        continue;
                    }

                    GameObject parent = CSGPrefabUtility.GetOutermostPrefabInstanceRoot(component.gameObject);

                    if(!parent)
                    {
                        continue;
                    }

                    if(prefabInstances.Contains(parent))
                    {
                        ArrayUtility.RemoveAt(ref registerModelsList, i);

                        continue;
                    }

                    if(foundPrefabs.Contains(parent))
                    {
                        continue;
                    }

                    Component parentNodeObj = parent.GetComponent(TypeConstants.csgNodeType);
                    foundPrefabs.Add(parent);

                    if(!parentNodeObj)
                    {
                        continue;
                    }

                    CSGNode parentNode = parentNodeObj as CSGNode;

                    if(parentNode.PrefabBehaviour != PrefabInstantiateBehaviour.Copy)
                    {
                        continue;
                    }

                    prefabInstances.Add(parent);
                    ArrayUtility.RemoveAt(ref registerModelsList, i);
                }

                for(int i = registerOperationsList.Length - 1; i >= 0; i--)
                {
                    CSGOperation component = registerOperationsList[i];

                    if(!component)
                    {
                        continue;
                    }

                    GameObject parent = CSGPrefabUtility.GetOutermostPrefabInstanceRoot(component.gameObject);

                    if(!parent)
                    {
                        continue;
                    }

                    if(prefabInstances.Contains(parent))
                    {
                        ArrayUtility.RemoveAt(ref registerOperationsList, i);

                        continue;
                    }

                    if(foundPrefabs.Contains(parent))
                    {
                        continue;
                    }

                    Component parentNodeObj = parent.GetComponent(TypeConstants.csgNodeType);
                    foundPrefabs.Add(parent);

                    if(!parentNodeObj)
                    {
                        continue;
                    }

                    CSGNode parentNode = parentNodeObj as CSGNode;

                    if(parentNode.PrefabBehaviour != PrefabInstantiateBehaviour.Copy)
                    {
                        continue;
                    }

                    prefabInstances.Add(parent);
                    ArrayUtility.RemoveAt(ref registerOperationsList, i);
                }

                for(int i = registerBrushesList.Length - 1; i >= 0; i--)
                {
                    CSGBrush component = registerBrushesList[i];

                    if(!component)
                    {
                        continue;
                    }

                    GameObject parent = CSGPrefabUtility.GetOutermostPrefabInstanceRoot(component.gameObject);

                    if(!parent)
                    {
                        continue;
                    }

                    if(prefabInstances.Contains(parent))
                    {
                        ArrayUtility.RemoveAt(ref registerBrushesList, i);

                        continue;
                    }

                    if(foundPrefabs.Contains(parent))
                    {
                        continue;
                    }

                    Component parentNodeObj = parent.GetComponent(TypeConstants.csgNodeType);
                    foundPrefabs.Add(parent);

                    if(!parentNodeObj)
                    {
                        continue;
                    }

                    CSGNode parentNode = parentNodeObj as CSGNode;

                    if(parentNode.PrefabBehaviour != PrefabInstantiateBehaviour.Copy)
                    {
                        continue;
                    }

                    prefabInstances.Add(parent);
                    ArrayUtility.RemoveAt(ref registerBrushesList, i);
                }

                // we've found new prefabs that should have been copied instead of prefab-instances (this can happen when you drag it into the hierarchy)
                if(prefabInstances.Count > 0)
                {
                    foreach(GameObject instance in prefabInstances)
                    {
                        Undo.ClearUndo(instance);
                    }

                    Undo.IncrementCurrentGroup();
                    int      currentGroup     = Undo.GetCurrentGroup();
                    bool     selectionChanged = false;
                    Object[] selection        = Selection.objects;

                    foreach(GameObject instance in prefabInstances)
                    {
                        Transform instanceTransform = instance.transform;
                        Transform instanceParent    = instanceTransform.parent;
                        int       instanceIndex     = instanceTransform.GetSiblingIndex();

                        GameObject newInstance = instanceParent ? Object.Instantiate(instance, instanceParent) : Object.Instantiate(instance);

                        if(ArrayUtility.Contains(selection, instance))
                        {
                            ArrayUtility.Remove(ref selection, instance);
                            ArrayUtility.Add(ref selection, newInstance);
                            selectionChanged = true;
                        }

                        Undo.RegisterCreatedObjectUndo(newInstance, "Copied prefab");
                        Undo.RecordObject(newInstance.transform, "Copied prefab");
                        newInstance.name = instance.name;
                        Object.DestroyImmediate(instance);
                        newInstance.transform.SetSiblingIndex(instanceIndex);
                    }

                    if(selectionChanged)
                    {
                        Selection.objects = selection;
                    }

                    Undo.CollapseUndoOperations(currentGroup);
                }

                for(int i = 0; i < registerModelsList.Length; i++)
                {
                    CSGModel component = registerModelsList[i];

                    if(!component)
                    {
                        continue;
                    }

                    RegisterModel(component);

                    if(ModelTraits.IsModelEditable(component))
                    {
                        SelectionUtility.LastUsedModel = component;
                    }
                }

                for(int i = 0; i < enableModelsList.Length; i++)
                {
                    CSGModel component = enableModelsList[i];

                    if(!component)
                    {
                        continue;
                    }

                    EnableModel(component);
                }

                for(int i = 0; i < registerOperationsList.Length; i++)
                {
                    CSGOperation component = registerOperationsList[i];

                    if(!component)
                    {
                        continue;
                    }

                    RegisterOperation(component);
                }

                // TODO: register all brushes in list at the same time, optimize for lots of brushes at same time
                for(int i = 0; i < registerBrushesList.Length; i++)
                {
                    CSGBrush component = registerBrushesList[i];

                    if(!component)
                    {
                        continue;
                    }

                    RegisterBrush(component);
                }
            }

            // validate components
            if(validateBrushes.Count > 0 || validateOperations.Count > 0)
            {
                CSGBrush[]     validateBrushesList    = validateBrushes.Count == 0 ? emptyBrushArray : validateBrushes.ToArray();
                CSGOperation[] validateOperationsList = validateOperations.Count == 0 ? emptyOperationArray : validateOperations.ToArray();

                validateBrushes.Clear();
                validateOperations.Clear();

                for(int i = 0; i < validateBrushesList.Length; i++)
                {
                    CSGBrush component = validateBrushesList[i];
                    ValidateBrush(component);
                }

                for(int i = 0; i < validateOperationsList.Length; i++)
                {
                    CSGOperation component = validateOperationsList[i];
                    ValidateOperation(component);
                }
            }
        }
    }
}

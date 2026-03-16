using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEditor;
using RealtimeCSG.Legacy;
using RealtimeCSG.Components;
using InternalRealtimeCSG;
using RealtimeCSG.Foundation;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal partial class InternalCSGModelManager
    {
        internal static CSGModel[] Models = new CSGModel[0];

        private static readonly HashSet<CSGModel> ModelLookup = new HashSet<CSGModel>();
        private static readonly HashSet<CSGModel> RegisterModels = new HashSet<CSGModel>();
        private static readonly HashSet<CSGModel> EnableModels = new HashSet<CSGModel>();
        private static readonly HashSet<CSGModel> DisableModels = new HashSet<CSGModel>();
        private static readonly HashSet<int> RemovedModels = new HashSet<int>();

        internal static List<CSGOperation> Operations = new List<CSGOperation>();

        private static readonly HashSet<CSGOperation> RegisterOperations = new HashSet<CSGOperation>();
        private static readonly HashSet<CSGOperation> ValidateOperations = new HashSet<CSGOperation>();
        private static readonly HashSet<CSGOperation> UnregisterOperations = new HashSet<CSGOperation>();
        private static readonly HashSet<CSGOperation> OperationTransformChanged = new HashSet<CSGOperation>();
        private static readonly HashSet<int> RemovedOperations = new HashSet<int>();

        internal static List<CSGBrush> Brushes = new List<CSGBrush>();

        private static readonly HashSet<CSGBrush> RegisterBrushes = new HashSet<CSGBrush>();
        private static readonly HashSet<CSGBrush> ValidateBrushes = new HashSet<CSGBrush>();
        private static readonly HashSet<CSGBrush> UnregisterBrushes = new HashSet<CSGBrush>();
        private static readonly HashSet<CSGBrush> BrushTransformChanged = new HashSet<CSGBrush>();
        private static readonly HashSet<int> RemovedBrushes = new HashSet<int>();

#region ClearRegistration

        internal static void ClearRegistration()
        {
            RemovedBrushes.Clear();
            RemovedOperations.Clear();
            RemovedModels.Clear();

            RegisterModels.Clear();
            EnableModels.Clear();
            DisableModels.Clear();

            RegisterOperations.Clear();
            ValidateOperations.Clear();
            UnregisterOperations.Clear();

            RegisterBrushes.Clear();
            ValidateBrushes.Clear();
            UnregisterBrushes.Clear();

            ModelLookup.Clear();

            OperationTransformChanged.Clear();
            BrushTransformChanged.Clear();

            Models     = new CSGModel[0];
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

                return;
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
                RegisterBrushes.Add(brush);

                return;
            }

            CSGOperation operation = node as CSGOperation;

            if(operation && !operation.PassThrough)
            {
                RegisterOperations.Add(operation);

                return;
            }

            CSGModel model = node as CSGModel;

            if(model)
            {
                RegisterModels.Add(model);

                return;
            }
        }

#endregion

#region FindBrushByID

        public static CSGBrush FindBrushByNodeID(int brushNodeID)
        {
            for(int i = 0; i < Brushes.Count; i++)
            {
                if(Brushes[i].brushNodeID == brushNodeID)
                {
                    return Brushes[i];
                }
            }

            return null;
        }

#endregion

#region CreateCSGModel

        internal static CSGModel CreateCSGModel(GameObject gameObject)
        {
            CSGModel model = gameObject.AddComponent<CSGModel>();

            StaticEditorFlags defaultFlags = //StaticEditorFlags.LightmapStatic |
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.NavigationStatic |
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
            RegisterBrushes.Add(component);
            ValidateBrushes.Remove(component);
            UnregisterBrushes.Remove(component);
            _isHierarchyModified = true;
        }

        public static void OnEnabled(CSGBrush component)
        {
            RegisterBrushes.Add(component);
            ValidateBrushes.Remove(component);
            UnregisterBrushes.Remove(component);
            _isHierarchyModified = true;
        }

        public static void OnValidate(CSGBrush component)
        {
            if(!UnregisterBrushes.Contains(component))
            {
                ValidateBrushes.Add(component);
            }
        }

        public static void OnTransformParentChanged(CSGBrush component)
        {
            OnBrushTransformChanged(component);
        }

        public static void OnDisabled(CSGBrush component)
        {
            RegisterBrushes.Remove(component);
            ValidateBrushes.Remove(component);
            UnregisterBrushes.Add(component);
            _isHierarchyModified = true;
        }

        public static void OnDestroyed(CSGBrush component)
        {
            RegisterBrushes.Remove(component);
            ValidateBrushes.Remove(component);
            UnregisterBrushes.Remove(component);
            UnregisterBrush(component);
            _isHierarchyModified = true;
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

            bool statement1 = !ReferenceEquals(component, null) &&
                component.hierarchyItem.TransformID == 0;

            bool statement3 = component.brushNodeID != CSGNode.InvalidNodeID;
            bool statement4 = statement1 && statement3;

            if(statement4)
            {
                dirty                                        = component.hierarchyItem.TransformInitialized || dirty;
                component.hierarchyItem.Transform            = component.transform;
                component.hierarchyItem.TransformID          = component.transform.GetInstanceID();
                component.hierarchyItem.NodeID               = component.brushNodeID;
                component.hierarchyItem.TransformInitialized = true;
            }

            // make sure that the surface array is not empty, 
            //  otherwise nothing can get rendered
            if(component.Shape == null ||
               component.Shape.Surfaces == null ||
               component.Shape.Surfaces.Length == 0 ||
               component.Shape.TexGens == null ||
               component.Shape.TexGens.Length == 0)
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
            RegisterOperations.Add(component);
            ValidateOperations.Remove(component);
            UnregisterOperations.Remove(component);
            //CSGModelManager.RegisterOperation(component);
            _isHierarchyModified = true;
        }

        public static void OnEnabled(CSGOperation component)
        {
            RegisterOperations.Add(component);
            ValidateOperations.Remove(component);
            UnregisterOperations.Remove(component);
            //CSGModelManager.RegisterOperation(component);
            _isHierarchyModified = true;
        }

        public static void OnValidate(CSGOperation component)
        {
            if(!UnregisterOperations.Contains(component))
            {
                ValidateOperations.Add(component);
            }
            //CSGModelManager.ValidateOperation(component);
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
            RegisterOperations.Remove(component);
            ValidateOperations.Remove(component);
            UnregisterOperations.Add(component);
            //CSGModelManager.UnregisterOperation(component);
            _isHierarchyModified = true;
        }

        public static void OnDestroyed(CSGOperation component)
        {
            RegisterOperations.Remove(component);
            ValidateOperations.Remove(component);
            UnregisterOperations.Remove(component);
            UnregisterOperation(component);
            _isHierarchyModified = true;
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

            if(component.ParentData.TransformID == 0 &&
               component.operationNodeID != CSGNode.InvalidNodeID)
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
            RegisterModels.Add(component);
            EnableModels.Remove(component);
            DisableModels.Remove(component);
            _isHierarchyModified = true;
        }

        public static void OnEnabled(CSGModel component)
        {
            EnableModels.Add(component);
            DisableModels.Remove(component);
            _isHierarchyModified = true;
        }

        public static void OnValidate(CSGModel component)
        {
            RegisterModels.Add(component);
            EnableModels.Remove(component);
            DisableModels.Remove(component);
            //CSGModelManager.RegisterModel(component);
        }

        public static void OnUpdate(CSGModel component)
        {
            RegisterModel(component);

            RegisterModels.Add(component);
            EnableModels.Remove(component);
            DisableModels.Remove(component);
            //CSGModelManager.RegisterModel(component);
            //MeshInstanceManager.UpdateTransform(model_cache.meshContainer);
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
            EnableModels.Remove(component);
            DisableModels.Add(component);
            //CSGModelManager.DisableModel(component);
            _isHierarchyModified = true;
        }

        public static void OnDestroyed(CSGModel component)
        {
            if(!component)
            {
                return;
            }

            RegisterModels.Remove(component);
            EnableModels.Remove(component);
            DisableModels.Remove(component);

            UnregisterModel(component);
            MeshInstanceManager.Destroy(component.generatedMeshes);
            component.generatedMeshes = null;
            _isHierarchyModified      = true;
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
            if(childData.Model &&
               !childData.Model.IsRegistered)
            {
                RegisterModel(childData.Model);
            }

            // make sure our parent (if any) has actually been initialized
            if(childData.Parent &&
               !childData.Parent.IsRegistered)
            {
                RegisterOperations.Remove(childData.Parent);
                UnregisterOperations.Remove(childData.Parent);
                RegisterOperation(childData.Parent);
            }
        }

#endregion

#region RegisterBrush

        private static void RegisterBrush(CSGBrush brush)
        {
            if(CSGModelManager.IsInPlayMode ||
               External == null ||
               !brush ||
               !brush.isActiveAndEnabled ||
               brush.IsRegistered)
            {
                return;
            }

            if(!External.GenerateBrush(brush.GetInstanceID(), out brush.brushNodeID))
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
            //var modelNodeID	= brushCache.childData.modelNodeID;
            //var parentNodeID	= brushCache.childData.parentNodeID;

            if(brush.ChildData.Model == null)
            {
                if(brush.brushNodeID != CSGNode.InvalidNodeID)
                {
                    External.DestroyNode(brush.brushNodeID);
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
                Debug.LogError("The brush (" + brush.name + ") is infinitely thin and is invalid", brush);
                brush.ControlMesh.Valid = false;
            }
            else
            {
                Matrix4x4 brushToModelSpace = brush.compareTransformation.brushToModelSpaceMatrix;
                External.SetBrushToModelSpace(brushNodeID, brushToModelSpace);
                External.SetBrushOperationType(brushNodeID, brush.OperationType);

                if((brush.Flags & BrushFlags.InfiniteBrush) != BrushFlags.InfiniteBrush)
                {
                    External.SetBrushFlags(brushNodeID, CSGTreeBrushFlags.Default);
                    SetBrushMesh(brush);
                }
                else
                {
                    SetBrushMesh(brush);
                    External.SetBrushFlags(brushNodeID, CSGTreeBrushFlags.Infinite);
                }
            }

            Brushes.Add(brush);
            _isHierarchyModified = true;
        }

#endregion

#region RegisterOperation

        private static void RegisterOperation(CSGOperation op)
        {
            if(CSGModelManager.IsInPlayMode ||
               External == null ||
               !op ||
               !op.isActiveAndEnabled ||
               op.PassThrough ||
               op.IsRegistered)
            {
                return;
            }

            if(!External.GenerateOperation(op.GetInstanceID(), out op.operationNodeID))
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
                External.DestroyNode(op.operationNodeID);
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
            //var modelNodeID	= operationCache.ChildData.modelNodeID;
            //var parentNodeID	= operationCache.ChildData.parentNodeID;

            //External.SetOperationHierarchy(opID, modelNodeID, parentNodeID);
            External.SetOperationOperationType(opID, op.OperationType);
            Operations.Add(op);
            op.ParentData.NodeID = opID;
            _isHierarchyModified = true;
        }

#endregion

#region RegisterModel

        private static void RegisterModel(CSGModel model)
        {
            if(CSGModelManager.IsInPlayMode ||
               External == null ||
               !model ||
               !model.isActiveAndEnabled ||
               model.IsRegistered)
            {
                return;
            }

            if(model.IsRegistered)
            {
                if(ModelLookup.Contains(model))
                {
                    return;
                }

                Debug.LogWarning(
                    "Model named " + model.name + " marked as registered, but wasn't actually registered", model
                );
            }

            if(!model.DefaultPhysicsMaterial)
            {
                model.DefaultPhysicsMaterial = MaterialUtility.DefaultPhysicsMaterial;
            }

            if(!External.GenerateModel(model.GetInstanceID(), out model.modelNodeID))
            {
                Debug.LogError("Failed to generate ID for model named " + model.name);

                return;
            }

            EnsureInitialized(model);

            model.isActive = ModelTraits.IsModelEditable(model);

            //External.SetModelMeshTypes(model.modelNodeID, GetMeshTypesForModel(model));
            External.SetModelEnabled(model.modelNodeID, model.isActive);

            ArrayUtility.Add(ref Models, model);
            ModelLookup.Add(model);
            UpdateChildrenModel(model, model.transform);
            _isHierarchyModified = true;

            //MeshInstanceManager.ValidateModelDelayed(model);
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

            BrushMesh brushMesh = BrushFactory.GenerateFromControlMesh(
                controlMesh, shape, brush.ChildData.Model.DefaultPhysicsMaterial
            );

            if(brushMesh == null)
            {
                return false;
            }

            int brushMeshID = External.GetBrushMeshID(brush.brushNodeID);

            if(brushMeshID != CSGNode.InvalidNodeID)
            {
                return External.UpdateBrushMesh(brushMeshID, brushMesh);
            }

            brushMeshID = External.CreateBrushMesh(brush.GetInstanceID(), brushMesh);

            return External.SetBrushMeshID(brush.brushNodeID, brushMeshID);
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

            //	int brushNodeID = External.GetBrushMeshID(brush.brushNodeID);
            //	if (brushNodeID == CSGNode.InvalidNodeID)
            {
                SetBrushMesh(brush);
                //	} else
                //	{
                //		External.UpdateBrushMeshSurfaces(brushNodeID, brush.Shape);
            }
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

            if(brush.ChildData != null &&
               brush.ChildData.Model &&
               brush.ChildData.Model.IsRegistered)
            {
                ParentNodeData parent_data = GetParentData(brush.ChildData);

                if(parent_data != null)
                {
                    parent_data.RemoveNode(brush.hierarchyItem);
                }
            }

            Brushes.Remove(brush);
            RemovedBrushes.Add(brushNodeID);

            // did we remove component, or entire game-object?
            if(brush &&
               brush.gameObject)
            {
                Transform transform = brush.transform;

                if(brush.gameObject.activeInHierarchy)
                {
                    CSGOperation parent;
                    FindParentOperation(transform, out parent);
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

            if(op.ChildData.Model &&
               op.ChildData.Model.IsRegistered)
            {
                ParentNodeData parentData = GetParentData(op.ChildData);

                if(parentData != null && op.ParentData != null)
                {
                    parentData.RemoveNode(op.ParentData);
                }
            }

            Operations.Remove(op);
            RemovedOperations.Add(opID);

            // did we remove component, or entire game-object?
            if(op &&
               op.gameObject)
            {
                Transform transform = op.transform;

                if(op.gameObject.activeInHierarchy)
                {
                    CSGOperation parent;
                    FindParentOperation(transform, out parent);
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
                if(!ModelLookup.Contains(model))
                {
                    return;
                }

                Debug.LogWarning("model marked as unregistered, but was registered");
            }

            // did we remove component, or entire game-object?
            if(model &&
               model.gameObject)
            {
                Transform transform = model.transform;

                if(model.gameObject.activeInHierarchy)
                {
                    // NOTE: returns default model when it can't find parent model
                    CSGModel parentModel;
                    FindParentModel(transform, out parentModel);

                    // make sure it's children are set to another model
                    UpdateChildrenModel(parentModel, transform);
                }
            }

            RemovedModels.Add(model.modelNodeID);

            //External.RemoveModel(model.ID); // delayed
            ArrayUtility.Remove(ref Models, model);
            ModelLookup.Remove(model);

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

            External.SetModelEnabled(model.modelNodeID, enabled);
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

            External.SetModelEnabled(model.modelNodeID, false);

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
                RegisterOperations.Remove(op);
                UnregisterOperations.Remove(op);
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

                External.SetOperationOperationType(
                    op.operationNodeID,
                    op.OperationType
                );
            }
        }

#endregion

#region CheckSurfaces

        public static void CheckSurfaceModifications(CSGBrush[] brushes, bool surfacesModified = false)
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
                //Debug.LogError("Shape is infinitely thin and is invalid");
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

            bool updateTexGens = compareShape.prevTexGens == null
             || compareShape.prevTexGens.Length != shape.TexGens.Length ||
                surfacesModified;

            if(!updateTexGens)
            {
                for(int i = 0; i < shape.TexGens.Length; i++)
                {
                    if( //shape.TexGens[i].Color			== compareShape.prevTexGens[i].Color &&
                       shape.TexGens[i].RotationAngle == compareShape.prevTexGens[i].RotationAngle &&
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

                        if(shape.Surfaces[surfaceIndex].TexGenIndex
                        != compareShape.prevSurfaces[surfaceIndex].TexGenIndex)
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

                    if(compareShape.prevSurfaces == null ||
                       compareShape.prevSurfaces.Length != shape.Surfaces.Length)
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
                RegisterBrushes.Remove(brush);
                UnregisterBrushes.Remove(brush);
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

                External.SetBrushOperationType(brush.brushNodeID, brush.OperationType);
            }

            // check if surfaces changed
            CheckSurfaceModifications(brush, surfacesModified);

            if(wasRegistered &&
               brush.controlMeshGeneration != brush.ControlMesh.Generation)
            {
                brush.controlMeshGeneration = brush.ControlMesh.Generation;
                ControlMeshUtility.RebuildShape(brush);
            }
        }

#endregion

        private static readonly CSGBrush[] EmptyBrushArray = new CSGBrush[0];
        private static readonly CSGOperation[] EmptyOperationArray = new CSGOperation[0];
        private static readonly CSGModel[] EmptyModelArray = new CSGModel[0];

        internal static double registerTime = 0.0;
        internal static double validateTime = 0.0;

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
            if(UnregisterBrushes.Count > 0 || UnregisterOperations.Count > 0 || DisableModels.Count > 0)
            {
                CSGBrush[] unregisterBrushesList =
                    UnregisterBrushes.Count == 0 ? EmptyBrushArray : UnregisterBrushes.ToArray();

                CSGOperation[] unregisterOperationsList = UnregisterOperations.Count == 0
                    ? EmptyOperationArray
                    : UnregisterOperations.ToArray();

                CSGModel[] disableModelsList = DisableModels.Count == 0 ? EmptyModelArray : DisableModels.ToArray();
                UnregisterBrushes.Clear();
                UnregisterOperations.Clear();
                DisableModels.Clear();

                if(unregisterBrushesList.Length > 0 ||
                   unregisterOperationsList.Length > 0 ||
                   disableModelsList.Length > 0)
                {
                    _isHierarchyModified = true;
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

            double startTime = EditorApplication.timeSinceStartup;

            // register new components
            if(RegisterModels.Count > 0 || EnableModels.Count > 0 || RegisterOperations.Count > 0
            || RegisterBrushes.Count > 0)
            {
                CSGModel[] registerModelsList = RegisterModels.Count == 0 ? EmptyModelArray : RegisterModels.ToArray();
                CSGModel[] enableModelsList   = EnableModels.Count == 0 ? EmptyModelArray : EnableModels.ToArray();

                CSGOperation[] registerOperationsList =
                    RegisterOperations.Count == 0 ? EmptyOperationArray : RegisterOperations.ToArray();

                CSGBrush[] registerBrushesList =
                    RegisterBrushes.Count == 0 ? EmptyBrushArray : RegisterBrushes.ToArray();

                RegisterModels.Clear();
                EnableModels.Clear();
                RegisterOperations.Clear();
                RegisterBrushes.Clear();

                if(registerBrushesList.Length > 0 ||
                   registerModelsList.Length > 0 ||
                   enableModelsList.Length > 0 ||
                   registerOperationsList.Length > 0)
                {
                    _isHierarchyModified = true;
                }

                HashSet<GameObject> prefabInstances = new HashSet<GameObject>();

                {
                    HashSet<GameObject> foundPrefabs = new HashSet<GameObject>();

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

                        Component parentNodeObj = parent.GetComponent(TypeConstants.CSGNodeType);
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

                        Component parentNodeObj = parent.GetComponent(TypeConstants.CSGNodeType);
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

                        Component parentNodeObj = parent.GetComponent(TypeConstants.CSGNodeType);
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

                        GameObject newInstance;

                        if(instanceParent)
                        {
                            newInstance = (GameObject)UnityEngine.Object.Instantiate(instance, instanceParent);
                        }
                        else
                        {
                            newInstance = GameObject.Instantiate(instance);
                        }

                        if(ArrayUtility.Contains(selection, instance))
                        {
                            ArrayUtility.Remove(ref selection, instance);
                            ArrayUtility.Add(ref selection, newInstance);
                            selectionChanged = true;
                        }

                        Undo.RegisterCreatedObjectUndo(newInstance, "Copied prefab");
                        Undo.RecordObject(newInstance.transform, "Copied prefab");
                        newInstance.name = instance.name;
                        UnityEngine.Object.DestroyImmediate(instance);
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
                /*
                for (var i = 0; i < registerModelsList.Length; i++)
                {
                    var component = registerModelsList[i];
                    if (!component)
                        continue;

                    MeshInstanceManager.ValidateModelDelayed(component);
                }
                */
            }

            registerTime = EditorApplication.timeSinceStartup - startTime;

            startTime = EditorApplication.timeSinceStartup;

            // validate components
            if(ValidateBrushes.Count > 0 || ValidateOperations.Count > 0)
            {
                CSGBrush[] validateBrushesList =
                    ValidateBrushes.Count == 0 ? EmptyBrushArray : ValidateBrushes.ToArray();

                CSGOperation[] validateOperationsList =
                    ValidateOperations.Count == 0 ? EmptyOperationArray : ValidateOperations.ToArray();

                ValidateBrushes.Clear();
                ValidateOperations.Clear();

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

            validateTime = EditorApplication.timeSinceStartup - startTime;
        }
    }
}

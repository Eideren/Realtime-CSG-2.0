using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class FilteredSelection // : ScriptableObject
    {
        [SerializeField] public Transform[] OtherTargets = new Transform[0];
        [SerializeField] public CSGNode[] NodeTargets = new CSGNode[0];
        [SerializeField] public CSGBrush[] BrushTargets = new CSGBrush[0];
        [SerializeField] public CSGOperation[] OperationTargets = new CSGOperation[0];
        [SerializeField] public CSGModel[] ModelTargets = new CSGModel[0];

        [SerializeField] public CSGNode[] RemovedNodes = new CSGNode[0];
        [SerializeField] public Transform[] RemovedOthers = new Transform[0];

        [SerializeField] public CSGNode[] AddedNodes = new CSGNode[0];
        [SerializeField] public Transform[] AddedOthers = new Transform[0];

        public HashSet<CSGBrush> GetAllContainedBrushes()
        {
            HashSet<CSGBrush> brushes = new HashSet<CSGBrush>();

            if(BrushTargets != null &&
               BrushTargets.Length > 0)
            {
                CSGBrush[] brush_targets = BrushTargets;

                for(int i = 0; i < brush_targets.Length; i++)
                {
                    CSGBrush brush = brush_targets[i];

                    if((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
                    {
                        continue;
                    }

                    brush.EnsureInitialized();

                    if(brush.ControlMesh == null ||
                       brush.Shape == null)
                    {
                        continue;
                    }

                    brushes.Add(brush);
                }
            }

            if(OperationTargets != null &&
               OperationTargets.Length > 0)
            {
                CSGOperation[] operation_targets = OperationTargets;

                for(int i = 0; i < operation_targets.Length; i++)
                {
                    CSGOperation operation         = operation_targets[i];
                    CSGBrush[]   operation_brushes = operation.GetComponentsInChildren<CSGBrush>();

                    for(int j = 0; j < operation_brushes.Length; j++)
                    {
                        CSGBrush brush = operation_brushes[j];

                        if((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
                        {
                            continue;
                        }

                        brush.EnsureInitialized();

                        if(brush.ControlMesh == null ||
                           brush.Shape == null)
                        {
                            continue;
                        }

                        brushes.Add(brush);
                    }
                }
            }

            if(ModelTargets != null &&
               ModelTargets.Length > 0)
            {
                CSGModel[] model_targets = ModelTargets;

                for(int i = 0; i < model_targets.Length; i++)
                {
                    CSGModel   model         = model_targets[i];
                    CSGBrush[] model_brushes = model.GetComponentsInChildren<CSGBrush>();

                    for(int j = 0; j < model_brushes.Length; j++)
                    {
                        CSGBrush brush = model_brushes[j];

                        if((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
                        {
                            continue;
                        }

                        brush.EnsureInitialized();

                        if(brush.ControlMesh == null ||
                           brush.Shape == null)
                        {
                            continue;
                        }

                        brushes.Add(brush);
                    }
                }
            }

            return brushes;
        }

        public List<Transform> GetAllTopTransforms()
        {
            List<Transform> transforms = new List<Transform>();

            if(BrushTargets != null &&
               BrushTargets.Length > 0)
            {
                foreach(CSGBrush brush in BrushTargets)
                {
                    transforms.Add(brush.transform);
                }
            }

            if(OperationTargets != null &&
               OperationTargets.Length > 0)
            {
                foreach(CSGOperation operation in OperationTargets)
                {
                    transforms.Add(operation.transform);
                }
            }

            if(ModelTargets != null &&
               ModelTargets.Length > 0)
            {
                foreach(CSGModel model in ModelTargets)
                {
                    transforms.Add(model.transform);
                }
            }

            if(OtherTargets != null &&
               OtherTargets.Length > 0)
            {
                transforms.AddRange(OtherTargets);
            }

            for(int i = transforms.Count - 1; i >= 0; i--)
            {
                bool      found    = false;
                Transform iterator = transforms[i].parent;

                while(iterator != null)
                {
                    if(transforms.Contains(iterator))
                    {
                        found = true;

                        break;
                    }

                    iterator = iterator.parent;
                }

                if(found)
                {
                    transforms.RemoveAt(i);
                }
            }

            return transforms;
        }

        private bool Validate()
        {
            bool modified = false;

            if(OtherTargets != null &&
               OtherTargets.Length > 0)
            {
                for(int i = OtherTargets.Length - 1; i >= 0; i--)
                {
                    if(!OtherTargets[i])
                    {
                        ArrayUtility.RemoveAt(ref OtherTargets, i);
                        modified = true;
                    }
                }

                if(OtherTargets.Length == 0)
                {
                    OtherTargets = new Transform[0];
                }
            }

            if(NodeTargets != null &&
               NodeTargets.Length > 0)
            {
                for(int i = NodeTargets.Length - 1; i >= 0; i--)
                {
                    if(!NodeTargets[i])
                    {
                        ArrayUtility.RemoveAt(ref NodeTargets, i);
                        modified = true;
                    }
                }

                if(NodeTargets.Length == 0)
                {
                    NodeTargets      = new CSGNode[0];
                    ModelTargets     = new CSGModel[0];
                    BrushTargets     = new CSGBrush[0];
                    OperationTargets = new CSGOperation[0];
                }
                else
                {
                    if(BrushTargets != null &&
                       BrushTargets.Length > 0)
                    {
                        for(int i = BrushTargets.Length - 1; i >= 0; i--)
                        {
                            if(!BrushTargets[i])
                            {
                                ArrayUtility.RemoveAt(ref BrushTargets, i);
                                modified = true;
                            }
                        }

                        if(BrushTargets.Length == 0)
                        {
                            BrushTargets = new CSGBrush[0];
                        }
                    }

                    if(OperationTargets != null &&
                       OperationTargets.Length > 0)
                    {
                        for(int i = OperationTargets.Length - 1; i >= 0; i--)
                        {
                            if(!OperationTargets[i])
                            {
                                ArrayUtility.RemoveAt(ref OperationTargets, i);
                                modified = true;
                            }
                        }

                        if(OperationTargets.Length == 0)
                        {
                            OperationTargets = new CSGOperation[0];
                        }
                    }

                    if(ModelTargets != null &&
                       ModelTargets.Length > 0)
                    {
                        for(int i = ModelTargets.Length - 1; i >= 0; i--)
                        {
                            if(!ModelTargets[i])
                            {
                                ArrayUtility.RemoveAt(ref ModelTargets, i);
                                modified = true;
                            }
                        }

                        if(ModelTargets.Length == 0)
                        {
                            ModelTargets = new CSGModel[0];
                        }
                    }
                }
            }

            return modified;
        }

        public bool UpdateSelection(HashSet<CSGNode> newTargetNodes, HashSet<Transform> newTargetOthers)
        {
            if(newTargetNodes == null)
            {
                newTargetNodes = new HashSet<CSGNode>();
            }

            if(newTargetOthers == null)
            {
                newTargetOthers = new HashSet<Transform>();
            }

            RemovedNodes  = null;
            RemovedOthers = null;

            AddedNodes  = null;
            AddedOthers = null;

            List<CSGNode> foundRemovedNodes = new List<CSGNode>();

            if(NodeTargets != null &&
               NodeTargets.Length > 0)
            {
                if(newTargetNodes.Count == 0)
                {
                    foundRemovedNodes.AddRange(NodeTargets);
                }
                else
                {
                    for(int i = 0; i < NodeTargets.Length; i++)
                    {
                        if(!NodeTargets[i] ||
                           !newTargetNodes.Contains(NodeTargets[i]))
                        {
                            foundRemovedNodes.Add(NodeTargets[i]);
                        }
                    }
                }
            }

            List<Transform> foundRemovedOthers = new List<Transform>();

            if(OtherTargets != null &&
               OtherTargets.Length > 0)
            {
                if(newTargetOthers.Count == 0)
                {
                    foundRemovedOthers.AddRange(OtherTargets);
                }
                else
                {
                    for(int i = 0; i < OtherTargets.Length; i++)
                    {
                        if(!OtherTargets[i] ||
                           !newTargetOthers.Contains(OtherTargets[i]))
                        {
                            foundRemovedOthers.Add(OtherTargets[i]);
                        }
                    }
                }
            }

            int originalTargetNodeCount  = NodeTargets == null ? 0 : NodeTargets.Length;
            int originalTargetOtherCount = OtherTargets == null ? 0 : OtherTargets.Length;

            // If our counts are the same and nothing is removed (and nothing could've been added), nothing has changed.
            if(newTargetNodes.Count == originalTargetNodeCount &&
               newTargetOthers.Count == originalTargetOtherCount &&
               foundRemovedNodes.Count == 0 &&
               foundRemovedOthers.Count == 0)
            {
                return false;
            }

            Validate();

            foreach(CSGNode node in foundRemovedNodes)
            {
                ArrayUtility.Remove(ref NodeTargets, node);

                CSGBrush brush = node as CSGBrush;

                if(brush != null)
                {
                    ArrayUtility.Remove(ref BrushTargets, brush);

                    continue;
                }

                CSGOperation operation = node as CSGOperation;

                if(node is CSGOperation)
                {
                    ArrayUtility.Remove(ref OperationTargets, operation);

                    continue;
                }

                CSGModel model = node as CSGModel;

                if(node is CSGModel)
                {
                    ArrayUtility.Remove(ref ModelTargets, model);

                    continue;
                }
            }

            foreach(Transform other in foundRemovedOthers)
            {
                ArrayUtility.Remove(ref OtherTargets, other);
            }

            List<CSGNode> foundAddedNodes = new List<CSGNode>();

            foreach(CSGNode node in newTargetNodes)
            {
                if(NodeTargets != null &&
                   ArrayUtility.Contains(NodeTargets, node))
                {
                    continue;
                }

                if(NodeTargets == null)
                {
                    NodeTargets = new CSGNode[] { node };
                }
                else
                {
                    ArrayUtility.Add(ref NodeTargets, node);
                }

                foundAddedNodes.Add(node);

                if(CSGPrefabUtility.IsPrefabAsset(node.gameObject))
                {
                    continue;
                }

                CSGBrush brush = node as CSGBrush;

                if(brush != null)
                {
                    if(BrushTargets == null)
                    {
                        BrushTargets = new CSGBrush[] { brush };
                    }
                    else
                    {
                        ArrayUtility.Add(ref BrushTargets, brush);
                    }

                    continue;
                }

                CSGOperation operation = node as CSGOperation;

                if(node is CSGOperation)
                {
                    if(OperationTargets == null)
                    {
                        OperationTargets = new CSGOperation[] { operation };
                    }
                    else
                    {
                        ArrayUtility.Add(ref OperationTargets, operation);
                    }

                    continue;
                }

                CSGModel model = node as CSGModel;

                if(node is CSGModel)
                {
                    if(ModelTargets == null)
                    {
                        ModelTargets = new CSGModel[] { model };
                    }
                    else
                    {
                        ArrayUtility.Add(ref ModelTargets, model);
                    }

                    continue;
                }
            }

            List<Transform> foundAddedOthers = new List<Transform>();

            foreach(Transform other in newTargetOthers)
            {
                if(OtherTargets != null &&
                   ArrayUtility.Contains(OtherTargets, other))
                {
                    continue;
                }

                if(OtherTargets == null)
                {
                    OtherTargets = new Transform[] { other };
                }
                else
                {
                    ArrayUtility.Add(ref OtherTargets, other);
                }

                foundAddedOthers.Add(other);
            }

            for(int i = foundAddedNodes.Count - 1; i >= 0; i--)
            {
                CSGNode  node  = foundAddedNodes[i];
                CSGBrush brush = node as CSGBrush;

                if(brush != null)
                {
                    if(brush.ChildData == null ||
                       brush.ChildData.Model == null)
                    {
                        continue;
                    }

                    CSGModel childModel = brush.ChildData.Model;

                    if(ModelTraits.IsModelEditable(childModel))
                    {
                        SelectionUtility.LastUsedModel = childModel;
                    }

                    break;
                }

                CSGOperation operation = node as CSGOperation;

                if(operation != null)
                {
                    if(operation.ChildData == null ||
                       operation.ChildData.Model == null)
                    {
                        continue;
                    }

                    SelectionUtility.LastUsedModel = operation.ChildData.Model;

                    break;
                }

                CSGModel model = node as CSGModel;

                if(ModelTraits.IsModelEditable(model))
                {
                    SelectionUtility.LastUsedModel = model;

                    break;
                }
            }

            RemovedNodes  = foundRemovedNodes.ToArray();
            RemovedOthers = foundRemovedOthers.ToArray();

            AddedNodes  = foundAddedNodes.ToArray();
            AddedOthers = foundAddedOthers.ToArray();

            return foundAddedNodes.Count > 0 || foundRemovedNodes.Count > 0 || foundAddedOthers.Count > 0
             || foundRemovedOthers.Count > 0;
        }
    }
}

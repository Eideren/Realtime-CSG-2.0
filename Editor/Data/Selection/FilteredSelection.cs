using System;
using System.Collections.Generic;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace RealtimeCSG
{
    [Serializable]
    internal sealed class FilteredSelection
    {
        [SerializeField] public Transform[] otherTargets = Array.Empty<Transform>();
        [SerializeField] public CSGNode[] nodeTargets = Array.Empty<CSGNode>();
        [SerializeField] public CSGBrush[] brushTargets = Array.Empty<CSGBrush>();
        [SerializeField] public CSGOperation[] operationTargets = Array.Empty<CSGOperation>();
        [SerializeField] public CSGModel[] modelTargets = Array.Empty<CSGModel>();
        [SerializeField] public CSGNode[] removedNodes = Array.Empty<CSGNode>();
        [SerializeField] public Transform[] removedOthers = Array.Empty<Transform>();
        [SerializeField] public CSGNode[] addedNodes = Array.Empty<CSGNode>();
        [SerializeField] public Transform[] addedOthers = Array.Empty<Transform>();

        public HashSet<CSGBrush> GetAllContainedBrushes()
        {
            HashSet<CSGBrush> brushes = new HashSet<CSGBrush>();

            if(brushTargets != null && brushTargets.Length > 0)
            {
                CSGBrush[] targets = brushTargets;

                for(int i = 0; i < targets.Length; i++)
                {
                    CSGBrush brush = targets[i];

                    if((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
                    {
                        continue;
                    }

                    brush.EnsureInitialized();

                    if(brush.ControlMesh == null || brush.Shape == null)
                    {
                        continue;
                    }

                    brushes.Add(brush);
                }
            }

            if(operationTargets != null && operationTargets.Length > 0)
            {
                CSGOperation[] targets = operationTargets;

                for(int i = 0; i < targets.Length; i++)
                {
                    CSGOperation operation        = targets[i];
                    CSGBrush[]   operationBrushes = operation.GetComponentsInChildren<CSGBrush>();

                    for(int j = 0; j < operationBrushes.Length; j++)
                    {
                        CSGBrush brush = operationBrushes[j];

                        if((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
                        {
                            continue;
                        }

                        brush.EnsureInitialized();

                        if(brush.ControlMesh == null || brush.Shape == null)
                        {
                            continue;
                        }

                        brushes.Add(brush);
                    }
                }
            }

            if(modelTargets != null && modelTargets.Length > 0)
            {
                CSGModel[] targets = modelTargets;

                for(int i = 0; i < targets.Length; i++)
                {
                    CSGModel   model        = targets[i];
                    CSGBrush[] modelBrushes = model.GetComponentsInChildren<CSGBrush>();

                    for(int j = 0; j < modelBrushes.Length; j++)
                    {
                        CSGBrush brush = modelBrushes[j];

                        if((brush.Flags & BrushFlags.InfiniteBrush) == BrushFlags.InfiniteBrush)
                        {
                            continue;
                        }

                        brush.EnsureInitialized();

                        if(brush.ControlMesh == null || brush.Shape == null)
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

            if(brushTargets != null && brushTargets.Length > 0)
            {
                foreach(CSGBrush brush in brushTargets)
                {
                    transforms.Add(brush.transform);
                }
            }

            if(operationTargets != null && operationTargets.Length > 0)
            {
                foreach(CSGOperation operation in operationTargets)
                {
                    transforms.Add(operation.transform);
                }
            }

            if(modelTargets != null && modelTargets.Length > 0)
            {
                foreach(CSGModel model in modelTargets)
                {
                    transforms.Add(model.transform);
                }
            }

            if(otherTargets != null && otherTargets.Length > 0)
            {
                transforms.AddRange(otherTargets);
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

            if(otherTargets != null && otherTargets.Length > 0)
            {
                for(int i = otherTargets.Length - 1; i >= 0; i--)
                {
                    if(!otherTargets[i])
                    {
                        ArrayUtility.RemoveAt(ref otherTargets, i);
                        modified = true;
                    }
                }

                if(otherTargets.Length == 0)
                {
                    otherTargets = Array.Empty<Transform>();
                }
            }

            if(nodeTargets != null && nodeTargets.Length > 0)
            {
                for(int i = nodeTargets.Length - 1; i >= 0; i--)
                {
                    if(!nodeTargets[i])
                    {
                        ArrayUtility.RemoveAt(ref nodeTargets, i);
                        modified = true;
                    }
                }

                if(nodeTargets.Length == 0)
                {
                    nodeTargets      = Array.Empty<CSGNode>();
                    modelTargets     = Array.Empty<CSGModel>();
                    brushTargets     = Array.Empty<CSGBrush>();
                    operationTargets = Array.Empty<CSGOperation>();
                }
                else
                {
                    if(brushTargets != null && brushTargets.Length > 0)
                    {
                        for(int i = brushTargets.Length - 1; i >= 0; i--)
                        {
                            if(!brushTargets[i])
                            {
                                ArrayUtility.RemoveAt(ref brushTargets, i);
                                modified = true;
                            }
                        }

                        if(brushTargets.Length == 0)
                        {
                            brushTargets = Array.Empty<CSGBrush>();
                        }
                    }

                    if(operationTargets != null && operationTargets.Length > 0)
                    {
                        for(int i = operationTargets.Length - 1; i >= 0; i--)
                        {
                            if(!operationTargets[i])
                            {
                                ArrayUtility.RemoveAt(ref operationTargets, i);
                                modified = true;
                            }
                        }

                        if(operationTargets.Length == 0)
                        {
                            operationTargets = Array.Empty<CSGOperation>();
                        }
                    }

                    if(modelTargets != null && modelTargets.Length > 0)
                    {
                        for(int i = modelTargets.Length - 1; i >= 0; i--)
                        {
                            if(!modelTargets[i])
                            {
                                ArrayUtility.RemoveAt(ref modelTargets, i);
                                modified = true;
                            }
                        }

                        if(modelTargets.Length == 0)
                        {
                            modelTargets = Array.Empty<CSGModel>();
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

            removedNodes  = null;
            removedOthers = null;

            addedNodes  = null;
            addedOthers = null;

            List<CSGNode> foundRemovedNodes = new List<CSGNode>();

            if(nodeTargets != null && nodeTargets.Length > 0)
            {
                if(newTargetNodes.Count == 0)
                {
                    foundRemovedNodes.AddRange(nodeTargets);
                }
                else
                {
                    for(int i = 0; i < nodeTargets.Length; i++)
                    {
                        if(!nodeTargets[i] || !newTargetNodes.Contains(nodeTargets[i]))
                        {
                            foundRemovedNodes.Add(nodeTargets[i]);
                        }
                    }
                }
            }

            List<Transform> foundRemovedOthers = new List<Transform>();

            if(otherTargets != null && otherTargets.Length > 0)
            {
                if(newTargetOthers.Count == 0)
                {
                    foundRemovedOthers.AddRange(otherTargets);
                }
                else
                {
                    for(int i = 0; i < otherTargets.Length; i++)
                    {
                        if(!otherTargets[i] || !newTargetOthers.Contains(otherTargets[i]))
                        {
                            foundRemovedOthers.Add(otherTargets[i]);
                        }
                    }
                }
            }

            int originalTargetNodeCount  = nodeTargets == null ? 0 : nodeTargets.Length;
            int originalTargetOtherCount = otherTargets == null ? 0 : otherTargets.Length;

            // If our counts are the same and nothing is removed (and nothing could've been added), nothing has changed.
            if(newTargetNodes.Count == originalTargetNodeCount && newTargetOthers.Count == originalTargetOtherCount && foundRemovedNodes.Count == 0 && foundRemovedOthers.Count == 0)
            {
                return false;
            }

            Validate();

            foreach(CSGNode node in foundRemovedNodes)
            {
                ArrayUtility.Remove(ref nodeTargets, node);

                CSGBrush brush = node as CSGBrush;

                if(brush != null)
                {
                    ArrayUtility.Remove(ref brushTargets, brush);

                    continue;
                }

                CSGOperation operation = node as CSGOperation;

                if(node is CSGOperation)
                {
                    ArrayUtility.Remove(ref operationTargets, operation);

                    continue;
                }

                CSGModel model = node as CSGModel;

                if(node is CSGModel)
                {
                    ArrayUtility.Remove(ref modelTargets, model);
                }
            }

            foreach(Transform other in foundRemovedOthers)
            {
                ArrayUtility.Remove(ref otherTargets, other);
            }

            List<CSGNode> foundAddedNodes = new List<CSGNode>();

            foreach(CSGNode node in newTargetNodes)
            {
                if(nodeTargets != null &&
                   ArrayUtility.Contains(nodeTargets, node))
                {
                    continue;
                }

                if(nodeTargets == null)
                {
                    nodeTargets = new[] { node };
                }
                else
                {
                    ArrayUtility.Add(ref nodeTargets, node);
                }

                foundAddedNodes.Add(node);

                if(CSGPrefabUtility.IsPrefabAsset(node.gameObject))
                {
                    continue;
                }

                CSGBrush brush = node as CSGBrush;

                if(brush != null)
                {
                    if(brushTargets == null)
                    {
                        brushTargets = new[] { brush };
                    }
                    else
                    {
                        ArrayUtility.Add(ref brushTargets, brush);
                    }

                    continue;
                }

                CSGOperation operation = node as CSGOperation;

                if(node is CSGOperation)
                {
                    if(operationTargets == null)
                    {
                        operationTargets = new[] { operation };
                    }
                    else
                    {
                        ArrayUtility.Add(ref operationTargets, operation);
                    }

                    continue;
                }

                CSGModel model = node as CSGModel;

                if(node is CSGModel)
                {
                    if(modelTargets == null)
                    {
                        modelTargets = new[] { model };
                    }
                    else
                    {
                        ArrayUtility.Add(ref modelTargets, model);
                    }
                }
            }

            List<Transform> foundAddedOthers = new List<Transform>();

            foreach(Transform other in newTargetOthers)
            {
                if(otherTargets != null && ArrayUtility.Contains(otherTargets, other))
                {
                    continue;
                }

                if(otherTargets == null)
                {
                    otherTargets = new[] { other };
                }
                else
                {
                    ArrayUtility.Add(ref otherTargets, other);
                }

                foundAddedOthers.Add(other);
            }

            for(int i = foundAddedNodes.Count - 1; i >= 0; i--)
            {
                CSGNode  node  = foundAddedNodes[i];
                CSGBrush brush = node as CSGBrush;

                if(brush != null)
                {
                    if(brush.ChildData == null || brush.ChildData.Model == null)
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
                    if(operation.ChildData == null || operation.ChildData.Model == null)
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

            removedNodes  = foundRemovedNodes.ToArray();
            removedOthers = foundRemovedOthers.ToArray();

            addedNodes  = foundAddedNodes.ToArray();
            addedOthers = foundAddedOthers.ToArray();

            return foundAddedNodes.Count > 0 || foundRemovedNodes.Count > 0 || foundAddedOthers.Count > 0 || foundRemovedOthers.Count > 0;
        }
    }
}

#if TEST_ENABLED
using System.Collections.Generic;
using System.Linq;
using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class CSGHierarchyView : EditorWindow
    {
        private CSGHierarchyView()
        {
            windows.Add(this);
        }

        private void Awake()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            Repaint();
        }

        private void OnDestroy()
        {
            windows.Remove(this);
        }

        private Dictionary<int, bool> _openNodes = new Dictionary<int, bool>();
        private static readonly List<CSGHierarchyView> windows = new List<CSGHierarchyView>();

        [MenuItem("Window/CSG Hierarchy")]
        private static void Create()
        {
            m_Window = (CSGHierarchyView)GetWindow(typeof(CSGHierarchyView), false, "CSG Hierarchy");
            m_Window.autoRepaintOnSceneChange = true;
        }

        private static CSGHierarchyView m_Window;

        private class Styles
        {
            public GUIStyle emptyItem;
            public GUIStyle emptySelected;
            public GUIStyle foldOut;
            public GUIStyle foldOutSelected;
        }

        private static Styles m_Styles;

        private static void UpdateStyles()
        {
            m_Styles = new Styles();
            m_Styles.emptyItem = new GUIStyle(EditorStyles.foldout);

            m_Styles.emptyItem.active.background = null;
            m_Styles.emptyItem.hover.background = null;
            m_Styles.emptyItem.normal.background = null;
            m_Styles.emptyItem.focused.background = null;

            m_Styles.emptyItem.onActive.background = null;
            m_Styles.emptyItem.onHover.background = null;
            m_Styles.emptyItem.onNormal.background = null;
            m_Styles.emptyItem.onFocused.background = null;

            m_Styles.emptySelected = new GUIStyle(m_Styles.emptyItem);
            m_Styles.emptySelected.normal = m_Styles.emptySelected.active;
            m_Styles.emptySelected.onNormal = m_Styles.emptySelected.onActive;

            m_Styles.foldOut = new GUIStyle(EditorStyles.foldout);
            m_Styles.foldOut.active = m_Styles.foldOut.normal;
            m_Styles.foldOut.onActive = m_Styles.foldOut.onNormal;

            m_Styles.foldOutSelected = new GUIStyle(EditorStyles.foldout);
            m_Styles.foldOutSelected.normal = m_Styles.foldOutSelected.active;
            m_Styles.foldOutSelected.onNormal = m_Styles.foldOutSelected.onActive;

            m_Styles.emptyItem.active = m_Styles.emptyItem.normal;
            m_Styles.emptyItem.onActive = m_Styles.emptyItem.onNormal;
        }

        private const int SCROLL_WIDTH = 20;
        private const int ITEM_INDENT = 20;
        private const int ICON_WIDTH = 20;
        private const int PADDING = 2;
        private static Vector2 m_ScrollPos;

        private sealed class StackItem
        {
            public StackItem(CSGTreeNode[] children, float xpos = 0)
            {
                this.children = children;
                index = 0;
                count = this.children.Length;
                this.xpos = xpos;
            }

            public int index;
            public readonly int count;
            public readonly float xpos;
            public readonly CSGTreeNode[] children;
        }

        private static readonly List<StackItem> itemStack = new List<StackItem>();

        // TODO: cache item heights
        private static int GetVisibleItems(CSGTreeNode[] hierarchyItems, ref Dictionary<int, bool> openNodes)
        {
            if(hierarchyItems == null)
            {
                return 0;
            }

            int totalCount = hierarchyItems.Length;
            itemStack.Add(new StackItem(hierarchyItems));

            ContinueOnNextStackItem:

            if(itemStack.Count == 0)
            {
                return totalCount;
            }

            StackItem     currentStackItem = itemStack[itemStack.Count - 1];
            CSGTreeNode[] children = currentStackItem.children;

            while(currentStackItem.index < currentStackItem.count)
            {
                int i = currentStackItem.index;
                currentStackItem.index++;

                int nodeID = children[i].NodeID;

                if(!openNodes.TryGetValue(nodeID, out bool isOpen))
                {
                    isOpen = true;
                    openNodes[nodeID] = true;
                }

                if(isOpen)
                {
                    int childCount = children[i].Count;

                    if(childCount > 0)
                    {
                        totalCount += childCount;
                        itemStack.Add(new StackItem(children[i].ChildrenToArray()));

                        goto ContinueOnNextStackItem;
                    }
                }
            }

            itemStack.RemoveAt(itemStack.Count - 1);

            goto ContinueOnNextStackItem;
        }

        private static void AddFoldOuts(
            ref Rect itemRect, ref Rect visibleArea, CSGTreeNode[] hierarchyItems, HashSet<int> selectedInstanceIDs
          , ref Dictionary<int, bool> openNodes
        )
        {
            if(hierarchyItems == null || hierarchyItems.Length == 0)
            {
                return;
            }

            Color defaultColor = GUI.color;

            AddFoldOuts(
                ref itemRect, ref visibleArea, hierarchyItems, selectedInstanceIDs, defaultColor, ref openNodes
            );

            GUI.color = defaultColor;
        }

        private static string NameForCoreNode(CSGTreeNode coreNode)
        {
            int    userID = coreNode.UserID;
            int    nodeID = coreNode.NodeID;
            Object obj = userID != 0 ? EditorUtility.InstanceIDToObject(userID) : null;

            if(obj == null)
            {
                return $"<unknown> [{(nodeID - 1)}:{userID}]";
            }

            return $"{obj.name} [{(nodeID - 1)}:{userID}]";
        }

        private static void AddFoldOuts(
            ref Rect itemRect, ref Rect visibleArea, CSGTreeNode[] hierarchyItems, HashSet<int> selectedInstanceIDs
          , Color defaultColor, ref Dictionary<int, bool> openNodes
        )
        {
            if(hierarchyItems == null)
            {
                return;
            }

            itemStack.Add(new StackItem(hierarchyItems, itemRect.x));

            ContinueOnNextStackItem:

            if(itemStack.Count == 0)
            {
                return;
            }

            float kItemHeight = EditorGUIUtility.singleLineHeight;

            StackItem     currentStackItem = itemStack[itemStack.Count - 1];
            CSGTreeNode[] children = currentStackItem.children;
            itemRect.x = currentStackItem.xpos;

            while(currentStackItem.index < currentStackItem.count)
            {
                int i = currentStackItem.index;
                currentStackItem.index++;

                if(itemRect.y > visibleArea.yMax)
                {
                    return;
                }

                int nodeID = children[i].NodeID;
                int userID = children[i].UserID;
                int childCount = children[i].Count;

                if(itemRect.y > visibleArea.yMin)
                {
                    string name = NameForCoreNode(children[i]);
                    bool   selected = selectedInstanceIDs.Contains(userID);

                    GUIStyle style = (childCount > 0)
                        ? (selected ? m_Styles.foldOutSelected : m_Styles.foldOut)
                        : (selected ? m_Styles.emptySelected : m_Styles.emptyItem);

                    EditorGUI.BeginChangeCheck();

                    bool isOpen;

                    if(!openNodes.TryGetValue(nodeID, out isOpen))
                    {
                        openNodes[nodeID] = false;
                    }

                    openNodes[nodeID] = EditorGUI.Foldout(itemRect, isOpen, name, true, style);

                    if(EditorGUI.EndChangeCheck())
                    {
                        Object obj = EditorUtility.InstanceIDToObject(userID);

                        if(!(obj is GameObject))
                        {
                            MonoBehaviour mono = (obj as MonoBehaviour);

                            if(mono)
                            {
                                userID = mono.gameObject.GetIntegerID();
                            }
                        }

                        Selection.instanceIDs = new[] { userID };
                    }
                }

                itemRect.y += kItemHeight;

                if(openNodes[nodeID])
                {
                    if(childCount > 0)
                    {
                        itemStack.Add(new StackItem(children[i].ChildrenToArray(), itemRect.x + ITEM_INDENT));

                        goto ContinueOnNextStackItem;
                    }
                }
            }

            itemStack.RemoveAt(itemStack.Count - 1);

            goto ContinueOnNextStackItem;
        }

        private void OnGUI()
        {
            if(m_Styles == null)
            {
                UpdateStyles();
            }

            HashSet<int> selectedInstanceIDs = new HashSet<int>();

            foreach(int instanceID in Selection.instanceIDs)
            {
                Object     obj = EditorUtility.InstanceIDToObject(instanceID);
                GameObject go = obj as GameObject;

                if(go != null)
                {
                    foreach(CSGNode no in go.GetComponents<CSGNode>())
                    {
                        int id = no.GetIntegerID();
                        selectedInstanceIDs.Add(id);
                    }
                }
            }

            float kItemHeight = EditorGUIUtility.singleLineHeight;

            CSGTreeNode[]     allNodes = CSGManager.AllTreeNodes;
            List<CSGTreeNode> allRootNodeList = new List<CSGTreeNode>();

            for(int i = 0; i < allNodes.Length; i++)
            {
                if(allNodes[i].Type != CSGNodeType.Tree &&
                   (allNodes[i].Tree.Valid || allNodes[i].Parent.Valid))
                {
                    continue;
                }

                allRootNodeList.Add(allNodes[i]);
            }

            CSGTreeNode[] allRootNodes = allRootNodeList.ToArray();

            int totalCount = GetVisibleItems(allRootNodes, ref _openNodes);

            Rect itemArea = position;
            itemArea.x = 0;
            itemArea.y = 0;
            itemArea.height -= 200;

            Rect totalRect = position;
            totalRect.x = 0;
            totalRect.y = 0;
            totalRect.width = position.width - SCROLL_WIDTH;
            totalRect.height = (totalCount * kItemHeight) + (2 * PADDING);

            Rect itemRect = position;
            itemRect.x = 0;
            itemRect.y = PADDING;
            itemRect.height = kItemHeight;

            m_ScrollPos = GUI.BeginScrollView(itemArea, m_ScrollPos, totalRect);

            {
                Rect visibleArea = itemArea;
                visibleArea.x += m_ScrollPos.x;
                visibleArea.y += m_ScrollPos.y;

                AddFoldOuts(ref itemRect, ref visibleArea, allRootNodes, selectedInstanceIDs, ref _openNodes);
            }

            GUI.EndScrollView();

            if(selectedInstanceIDs.Count == 1)
            {
                CSGNode obj = EditorUtility.InstanceIDToObject(selectedInstanceIDs.First()) as CSGNode;

                if(obj)
                {
                    CSGBrush     brush = obj as CSGBrush;
                    CSGOperation operation = obj as CSGOperation;
                    CSGModel     model = obj as CSGModel;
                    int          nodeID = CSGNode.InvalidNodeID;

                    if(brush)
                    {
                        nodeID = brush.brushNodeID;
                    }

                    if(operation)
                    {
                        nodeID = operation.operationNodeID;
                    }

                    if(model)
                    {
                        nodeID = model.modelNodeID;
                    }

                    if(nodeID != CSGNode.InvalidNodeID)
                    {
                        Rect labelArea = itemArea;
                        labelArea.x = 0;
                        labelArea.y = labelArea.height;
                        labelArea.height = kItemHeight;
                        CSGTreeNode node = new CSGTreeNode { nodeID = nodeID };
                        GUI.Label(labelArea, $"NodeID: {nodeID}");
                        labelArea.y += kItemHeight;
                        GUI.Label(labelArea, $"UserID: {node.UserID}");
                        labelArea.y += kItemHeight;
                        GUI.Label(labelArea, $"Valid: {node.Valid}");
                        labelArea.y += kItemHeight;
                        GUI.Label(labelArea, $"NodeType: {node.Type}");
                        labelArea.y += kItemHeight;
                        GUI.Label(labelArea, $"ChildCount: {node.Count}");
                        labelArea.y += kItemHeight;
                        CSGNodeType nodeType = node.Type;

                        if(nodeType != CSGNodeType.Tree)
                        {
                            GUI.Label(labelArea, $"Parent: {node.Parent.NodeID} valid {node.Parent.Valid}");

                            labelArea.y += kItemHeight;

                            GUI.Label(labelArea, $"Model: {node.Tree.NodeID} valid {node.Tree.Valid}");

                            labelArea.y += kItemHeight;

                            if(nodeType == CSGNodeType.Brush)
                            {
                                CSGOperationType op = ((CSGTreeBrush)node).Operation;
                                GUI.Label(labelArea, $"Operation: {op}");
                                labelArea.y += kItemHeight;
                            }
                            else if(nodeType == CSGNodeType.Branch)
                            {
                                CSGOperationType op = ((CSGTreeBranch)node).Operation;
                                GUI.Label(labelArea, $"Operation: {op}");
                                labelArea.y += kItemHeight;
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif

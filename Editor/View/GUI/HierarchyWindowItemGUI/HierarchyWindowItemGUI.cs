using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RealtimeCSG
{
    internal sealed class HierarchyWindowItemGUI
    {
        private static Texture2D ModelIcon
        {
            get
            {
                if (m_ModelIcon == null)
                {
                    m_ModelIcon = (Texture2D)EditorGUIUtility.IconContent("prematcube@2x").image;
                }

                return m_ModelIcon;
            }
        }

        private static Texture2D m_ModelIcon = null;

#if !UNITY_6000_4_OR_NEWER
        internal static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) == null)
            {
                return;
            }

            OnHierarchyWindowItemByEntityIdOnGUI(EditorUtility.InstanceIDToObject(instanceID).GetEntityId(), selectionRect);
        }
#endif

        //Legacy Hierarchy
        internal static void OnHierarchyWindowItemByEntityIdOnGUI(EntityId entityID, Rect selectionRect)
        {
            GameObject o = EditorUtility.EntityIdToObject(entityID) as GameObject;

            if (selectionRect.Contains(Event.current.mousePosition))
            {
                Transform t = o == null ? null : o.transform;
                SceneDragToolManager.OnHandleDragAndDrop(null, t, selectionRect);
            }

            if (o == null)
            {
                return;
            }

            CsgguiStyleUtility.InitStyles();

            CSGNode node = o.GetComponent<CSGNode>();

            if (node == null || !node.enabled || (node.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
            {
                return;
            }

            CSGOperationType operationType;

            CSGBrush brush = node as CSGBrush;

            if (brush != null)
            {
                operationType = brush.operationType;
                CSGSkin skin = CsgguiStyleUtility.Skin;

                GUI.Label(selectionRect, skin.hierarchyOperations[(int)operationType], CsgguiStyleUtility.RightAlignedLabel);

                return;
            }

            CSGOperation operation = node as CSGOperation;

            if (operation != null)
            {
                CSGSkin skin = CsgguiStyleUtility.Skin;

                if (!operation.PassThrough)
                {
                    operationType = operation.operationType;
                    int operationTypeIndex = (int)operationType;

                    if (operationTypeIndex >= 0 && operationTypeIndex < skin.hierarchyOperations.Length)
                    {
                        GUI.Label(selectionRect, skin.hierarchyOperations[operationTypeIndex], CsgguiStyleUtility.RightAlignedLabel);
                    }
                }
                else
                {
                    GUI.Label(selectionRect, skin.hierarchyPassThrough, CsgguiStyleUtility.RightAlignedLabel);
                }
            }

            CSGModel model = node as CSGModel;

            if (model != null)
            {
                GUI.Label(selectionRect, ModelIcon, CsgguiStyleUtility.RightAlignedLabel);
            }
        }

#if UNITY_6000_5_OR_NEWER
        //New Hierarchy
        internal static void OnBindViewItem(Unity.Hierarchy.Editor.HierarchyWindow window, Unity.Hierarchy.HierarchyView view, Unity.Hierarchy.HierarchyViewItem item)
        {
            if (item.Handler is not Unity.Hierarchy.Editor.HierarchyGameObjectHandler handler)
            {
                return;
            }

            GameObject hierarchyGameObject = handler.GetGameObject(item.Node);
            if (hierarchyGameObject == null)
            {
                return;
            }

            CSGNode node = hierarchyGameObject.GetComponent<CSGNode>();

            if (node != null)
            {
                StyleBackground newStyleBackground = new StyleBackground();

                if (node is CSGModel _model)
                {
                    newStyleBackground.value = Background.FromTexture2D(ModelIcon);
                }
                else if(node is CSGBrush _brush)
                {
                    newStyleBackground.value = Background.FromTexture2D((Texture2D)CsgguiStyleUtility.Skin.hierarchyOperations[(int)_brush.operationType]?.image);
                }
                else if(node is CSGOperation _operation)
                {
                    Texture2D OperationIcon = null;

                    if (!_operation.PassThrough)
                    {
                        OperationIcon = (Texture2D)CsgguiStyleUtility.Skin.hierarchyOperations[(int)_operation.operationType]?.image;
                    }
                    else
                    {
                        OperationIcon = (Texture2D)CsgguiStyleUtility.Skin.hierarchyPassThrough?.image;
                    }

                    newStyleBackground.value = Background.FromTexture2D(OperationIcon);
                }

                item.Icon.style.backgroundImage = newStyleBackground;
            }
        }
#endif
    }
}
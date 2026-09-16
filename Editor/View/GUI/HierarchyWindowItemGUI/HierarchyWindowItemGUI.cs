using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class HierarchyWindowItemGUI
    {
        private static Texture2D ModelIcon
        {
            get
            {
                if(m_ModelIcon == null)
                {
                    m_ModelIcon = Resources.Load<Texture2D>("GUI/icon_model");
                }

                return m_ModelIcon;
            }
        }

        private static Texture2D m_ModelIcon = null;

        internal static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            OnHierarchyWindowItemByEntityIdOnGUI(EntityIdUtility.GetEntityId(instanceID), selectionRect);
        }

        internal static void OnHierarchyWindowItemByEntityIdOnGUI(EntityId entityID, Rect selectionRect)
        {
            GameObject o = EditorUtility.EntityIdToObject(entityID) as GameObject;

            if(selectionRect.Contains(Event.current.mousePosition))
            {
                Transform t = o == null ? null : o.transform;
                SceneDragToolManager.OnHandleDragAndDrop(null, t, selectionRect);
            }

            if(o == null)
            {
                return;
            }

            CsgguiStyleUtility.InitStyles();

            CSGNode node = o.GetComponent<CSGNode>();

            if(node == null || !node.enabled || (node.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
            {
                return;
            }

            CSGOperationType operationType;

            CSGBrush brush = node as CSGBrush;

            if(brush != null)
            {
                operationType = brush.operationType;
                CSGSkin skin = CsgguiStyleUtility.Skin;

                GUI.Label(selectionRect, skin.hierarchyOperations[(int)operationType], CsgguiStyleUtility.RightAlignedLabel);

                return;
            }

            CSGOperation operation = node as CSGOperation;

            if(operation != null)
            {
                CSGSkin skin = CsgguiStyleUtility.Skin;

                if(!operation.PassThrough)
                {
                    operationType = operation.operationType;
                    int operationTypeIndex = (int)operationType;

                    if(operationTypeIndex >= 0 && operationTypeIndex < skin.hierarchyOperations.Length)
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
    }
}

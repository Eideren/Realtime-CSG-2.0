using RealtimeCSG.Components;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class HierarchyWindowItemGUI
    {
        internal static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            GameObject o = EditorUtility.EntityIdToObject(instanceID) as GameObject;

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

            if(node == null ||
               !node.enabled || (node.hideFlags & (HideFlags.HideInHierarchy | HideFlags.HideInInspector)) != 0)
            {
                return;
            }

            CSGOperationType operationType;

            CSGBrush brush = node as CSGBrush;

            if(brush != null)
            {
                operationType = brush.OperationType;
                CSGSkin skin = CsgguiStyleUtility.Skin;

                GUI.Label(
                    selectionRect, skin.hierarchyOperations[(int)operationType], CsgguiStyleUtility.RightAlignedLabel
                );

                return;
            }

            CSGOperation operation = node as CSGOperation;

            if(operation != null)
            {
                CSGSkin skin = CsgguiStyleUtility.Skin;

                if(!operation.PassThrough)
                {
                    operationType = operation.OperationType;
                    int operationTypeIndex = (int)operationType;

                    if(operationTypeIndex >= 0 && operationTypeIndex < skin.hierarchyOperations.Length)
                    {
                        GUI.Label(
                            selectionRect, skin.hierarchyOperations[operationTypeIndex]
                          , CsgguiStyleUtility.RightAlignedLabel
                        );
                    }
                }
                else
                {
                    GUI.Label(selectionRect, skin.hierarchyPassThrough, CsgguiStyleUtility.RightAlignedLabel);
                }
            }
        }
    }
}

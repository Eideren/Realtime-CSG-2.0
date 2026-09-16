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

        //Only runs in legacy hierarchy
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

        //Disable all icons before the scene view renders to hide the gizmo icons
        internal static void BeforeSceneGUI(SceneView sceneView)
        {
            for(int i = 0; i < InternalCSGModelManager.Models.Length; i++)
            {
                EditorGUIUtility.SetIconForObject(InternalCSGModelManager.Models[i].gameObject, null);
            }

            for (int i = 0; i < InternalCSGModelManager.Brushes.Count; i++)
            {
                EditorGUIUtility.SetIconForObject(InternalCSGModelManager.Brushes[i].gameObject, null);
            }

            for (int i = 0; i < InternalCSGModelManager.Operations.Count; i++)
            {
                EditorGUIUtility.SetIconForObject(InternalCSGModelManager.Operations[i].gameObject, null);
            }
        }

        //Set all icons again so they show up in the hierarchy
        internal static void DuringSceneGUI(SceneView sceneView)
        {
            for (int i = 0; i < InternalCSGModelManager.Models.Length; i++)
            {
                EditorGUIUtility.SetIconForObject(InternalCSGModelManager.Models[i].gameObject, ModelIcon);
            }

            for (int i = 0; i < InternalCSGModelManager.Brushes.Count; i++)
            {
                EditorGUIUtility.SetIconForObject(InternalCSGModelManager.Brushes[i].gameObject, (Texture2D)CsgguiStyleUtility.Skin.hierarchyOperations[(int)InternalCSGModelManager.Brushes[i].operationType].image);
            }

            for (int i = 0; i < InternalCSGModelManager.Operations.Count; i++)
            {
                Texture2D OperationIcon = null;

                CSGOperation operation = InternalCSGModelManager.Operations[i];

                if (!operation.PassThrough)
                {
                    OperationIcon = (Texture2D)CsgguiStyleUtility.Skin.hierarchyOperations[(int)operation.operationType].image;
                }
                else
                {
                    OperationIcon = (Texture2D)CsgguiStyleUtility.Skin.hierarchyPassThrough.image;
                }

                EditorGUIUtility.SetIconForObject(operation, OperationIcon);
            }
        }
    }
}

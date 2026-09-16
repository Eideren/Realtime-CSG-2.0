using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RealtimeCSG.Components.CSGNode), true), CanEditMultipleObjects]
public class EditModeToolWindowEditor : Editor
{
    public bool HasFrameBounds()
    {
        return RealtimeCSG.BoundsUtilities.HasFrameBounds(RealtimeCSG.EditModeManager.FilteredSelection);
    }

    public Bounds OnGetFrameBounds()
    {
        return RealtimeCSG.BoundsUtilities.OnGetFrameBounds(RealtimeCSG.EditModeManager.FilteredSelection);
    }

    public override void OnInspectorGUI()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Selection.activeObject = null;

            return;
        }

        SetIcons(targets);

        RealtimeCSG.EditModeSelectionGUI.OnInspectorGUI(this, targets);
    }

    public static Texture2D ModelIcon
    {
        get
        {
            if (m_ModelIcon == null)
            {
                m_ModelIcon = Resources.Load<Texture2D>("GUI/icon_model");
            }

            return m_ModelIcon;
        }
    }

    private static Texture2D m_ModelIcon = null;
    void SetIcons(Object[] targets)
    {
        foreach (Object _target in targets)
        {
            if (_target is RealtimeCSG.Components.CSGBrush)
            {
                RealtimeCSG.Components.CSGBrush brush = (RealtimeCSG.Components.CSGBrush)_target;

                EditorGUIUtility.SetIconForObject(brush.gameObject, (Texture2D)RealtimeCSG.CsgguiStyleUtility.Skin.hierarchyOperations[(int)brush.operationType].image);
            }
            else if (_target is RealtimeCSG.Components.CSGOperation)
            {
                RealtimeCSG.Components.CSGOperation operation = (RealtimeCSG.Components.CSGOperation)_target;

                Texture2D OperationIcon = null;

                if (!operation.PassThrough)
                {
                    OperationIcon = (Texture2D)RealtimeCSG.CsgguiStyleUtility.Skin.hierarchyOperations[(int)operation.operationType].image;
                }
                else
                {
                    OperationIcon = (Texture2D)RealtimeCSG.CsgguiStyleUtility.Skin.hierarchyPassThrough.image;
                }

                EditorGUIUtility.SetIconForObject(operation.gameObject, OperationIcon);
            }
            else if (_target is RealtimeCSG.Components.CSGModel)
            {
                RealtimeCSG.Components.CSGModel model = (RealtimeCSG.Components.CSGModel)_target;

                EditorGUIUtility.SetIconForObject(model.gameObject, ModelIcon);
            }
        }
    }
}

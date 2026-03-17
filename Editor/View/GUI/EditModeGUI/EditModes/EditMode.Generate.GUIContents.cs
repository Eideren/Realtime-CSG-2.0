using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditModeGenerateGUI
    {
        private static GUIContent m_ContentTitleLabel;

        private static readonly GUIContent contentDefaultMaterial = new GUIContent("Default");
        private static readonly GUIContent createContent = new GUIContent("Create");
        private static readonly GUIContent cancelContent = new GUIContent("Cancel");

        private static readonly ToolTip createTooltip = new ToolTip(
            "Create your brush", "Create the brush from the current box shape.", Keys.performActionKey
        );

        private static readonly ToolTip cancelTooltip = new ToolTip(
            "Cancel brush creation", "Do not generate the brush.", Keys.cancelActionKey
        );

        private static readonly GUILayoutOption largeLabelWidth = GUILayout.Width(80);
        private static readonly GUILayoutOption width100 = GUILayout.Width(100);

        private static void InitLocalStyles()
        {
            if(m_ContentTitleLabel != null)
            {
                return;
            }

            m_ContentTitleLabel = new GUIContent(CsgguiStyleUtility.BrushEditModeNames[(int)ToolEditMode.Generate]);
        }
    }
}

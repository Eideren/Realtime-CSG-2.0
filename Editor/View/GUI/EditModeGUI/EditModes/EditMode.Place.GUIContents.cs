using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditmodePlaceGUI
    {
        private static GUIContent m_ContentTitleLabel;

        private static readonly GUIContent rotateByOffsetContent = new GUIContent("Rotate");
        private static readonly ToolTip rotateByOffsetTooltip = new ToolTip("Rotate by given offset", "Click to rotate the selected objects by the given offset.");
        private static readonly GUIContent cloneRotateByOffsetContent = new GUIContent("Clone + Rotate");
        private static readonly ToolTip cloneRotateByOffsetTooltip = new ToolTip("Clone + rotate", "Click to rotate a copy of the selected objects by the given offset.");
        private static readonly GUIContent moveByOffsetContent = new GUIContent("Move");
        private static readonly ToolTip moveByOffsetTooltip = new ToolTip("Move by given offset", "Click to move the selected objects by the given offset.");
        private static readonly GUIContent cloneMoveByOffsetContent = new GUIContent("Clone + Move");
        private static readonly ToolTip cloneMoveByOffsetTooltip = new ToolTip("Clone + Move", "Click to move a copy of the selected objects by the given offset.");
        private static readonly GUIContent recenterPivotContent = new GUIContent("Recenter pivot");

        private static readonly ToolTip recenterPivotTooltip = new ToolTip(
            "Recenter pivot",
            "Click this to place the center of rotation\n" +
            "(the pivot) to the center of the selection.\n\n" +
            "This is disabled when you have no selection\n" +
            "or when Unity's pivot mode (top left corner)\n" +
            "is set to 'Center'.",
            Keys.centerPivot
        );

        private static readonly ToolTip pivotVectorTooltip = new ToolTip(
            "Set pivot point",
            "Here you can manually set the current center\n" +
            "of rotation (the pivot).\n\n" +
            "This is disabled when you have no selection\n" +
            "or when Unity's pivot mode (top left corner)\n" +
            "is set to 'Center'."
        );

        private static readonly GUIContent pivotCenterContent = new GUIContent("Pivot Center");
        private static readonly GUIContent rotationCenterContent = new GUIContent("Rotation Center");
        private static readonly GUIContent moveOffsetContent = new GUIContent("Move Offset");

        private static readonly GUILayoutOption[] maxWidth150 = { GUILayout.Width(80) };

        private static void InitLocalStyles()
        {
            if(m_ContentTitleLabel != null)
            {
                return;
            }

            m_ContentTitleLabel = new GUIContent(CsgguiStyleUtility.BrushEditModeNames[(int)ToolEditMode.Place]);
        }
    }
}

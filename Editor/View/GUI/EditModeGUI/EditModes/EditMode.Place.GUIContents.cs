using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditmodePlaceGUI
    {
        private static GUIContent ContentTitleLabel;

        private static readonly GUIContent RotateByOffsetContent = new GUIContent("Rotate");

        private static readonly ToolTip RotateByOffsetTooltip = new ToolTip(
            "Rotate by given offset",
            "Click to rotate the selected objects by the given offset."
        );

        private static readonly GUIContent CloneRotateByOffsetContent = new GUIContent("Clone + Rotate");

        private static readonly ToolTip CloneRotateByOffsetTooltip = new ToolTip(
            "Clone + rotate",
            "Click to rotate a copy of the selected objects by the given offset."
        );

        private static readonly GUIContent MoveByOffsetContent = new GUIContent("Move");

        private static readonly ToolTip MoveByOffsetTooltip = new ToolTip(
            "Move by given offset",
            "Click to move the selected objects by the given offset."
        );

        private static readonly GUIContent CloneMoveByOffsetContent = new GUIContent("Clone + Move");

        private static readonly ToolTip CloneMoveByOffsetTooltip = new ToolTip(
            "Clone + Move",
            "Click to move a copy of the selected objects by the given offset."
        );

        private static readonly GUIContent RecenterPivotContent = new GUIContent("Recenter pivot");

        private static readonly ToolTip RecenterPivotTooltip = new ToolTip(
            "Recenter pivot",
            "Click this to place the center of rotation\n" +
            "(the pivot) to the center of the selection.\n\n" +
            "This is disabled when you have no selection\n" +
            "or when Unity's pivot mode (top left corner)\n" +
            "is set to 'Center'.",
            Keys.centerPivot
        );

        private static readonly ToolTip PivotVectorTooltip = new ToolTip(
            "Set pivot point",
            "Here you can manually set the current center\n" +
            "of rotation (the pivot).\n\n" +
            "This is disabled when you have no selection\n" +
            "or when Unity's pivot mode (top left corner)\n" +
            "is set to 'Center'."
        );

        private static readonly GUIContent PivotCenterContent = new GUIContent("Pivot Center");
        private static readonly GUIContent RotationCenterContent = new GUIContent("Rotation Center");
        private static readonly GUIContent MoveOffsetContent = new GUIContent("Move Offset");

        private static readonly GUILayoutOption[] MaxWidth150 = new GUILayoutOption[] { GUILayout.Width(80) };

        private static void InitLocalStyles()
        {
            if(ContentTitleLabel != null)
            {
                return;
            }

            ContentTitleLabel = new GUIContent(CSG_GUIStyleUtility.brushEditModeNames[(int)ToolEditMode.Place]);
        }
    }
}

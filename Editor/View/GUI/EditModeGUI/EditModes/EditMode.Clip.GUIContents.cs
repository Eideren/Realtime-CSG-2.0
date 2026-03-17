using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditModeClipGUI
    {
        private static GUIContent m_ContentClipLabel;

        private static readonly ClipMode[] clipModeValues =
        {
            ClipMode.RemovePositive, ClipMode.RemoveNegative, ClipMode.Split
        };

        private static readonly GUIContent contentDefaultMaterial = new GUIContent("Default");
        private static readonly GUIContent contentCommit = new GUIContent("Commit");
        private static readonly GUIContent contentCancel = new GUIContent("Cancel");

        private static readonly ToolTip commitTooltip = new ToolTip(
            "Commit your changes"
          , "Split the selected brush(es) with the current clipping plane. This makes your changes final."
          , Keys.performActionKey
        );

        private static readonly ToolTip cancelTooltip = new ToolTip(
            "Cancel your changes", "Do not clip your selected brushes and return them to their original state."
          , Keys.cancelActionKey
        );

        private static readonly GUILayoutOption largeLabelWidth = GUILayout.Width(80);
        private static readonly GUILayoutOption[] materialSceneWidth = { GUILayout.Width(140) };
    }
}

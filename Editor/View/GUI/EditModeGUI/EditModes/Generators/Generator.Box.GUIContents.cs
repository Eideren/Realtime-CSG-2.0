using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorBoxGUI
    {
        private static readonly GUIContent lengthContent = new GUIContent("Length (X)");
        private static readonly ToolTip lengthTooltip = new ToolTip("Length (X)", "Set the length of the box, this is in the X direction.");
        private static readonly GUIContent heightContent = new GUIContent("Height (Y)");
        private static readonly ToolTip heightTooltip = new ToolTip("Height (Y)", "Set the height of the box, this is in the Y direction.");
        private static readonly GUIContent widthContent = new GUIContent("Width (Z)");
        private static readonly ToolTip widthTooltip = new ToolTip("Width (Z)", "Set the width of the box, this is in the Z direction.");

        private static readonly GUILayoutOption width25 = GUILayout.Width(25);
        private static readonly GUILayoutOption width65 = GUILayout.Width(65);
    }
}

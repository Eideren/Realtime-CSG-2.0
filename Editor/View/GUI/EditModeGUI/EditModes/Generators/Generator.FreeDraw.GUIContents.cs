using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorFreeDrawGUI
    {
        private static readonly GUIContent heightContent = new GUIContent("Height");
        private static readonly ToolTip heightTooltip = new ToolTip("Height", "Set the height of the shape.");
        private static readonly GUIContent curveSidesContent = new GUIContent("Splits");

        private static readonly ToolTip curveSidesTooltip = new ToolTip(
            "Curve Splits", "How many sides should each edge that's turned into a curve have."
        );

        private static readonly GUIContent edgeTypeContent = new GUIContent("Edge");

        private static readonly ToolTip edgeTypeTooltip = new ToolTip(
            "Edge Curve Type", "Set the current selected edges to be Straight, Broken Curve or an Aligned Curve."
        );

        private static readonly GUIContent splitSelectedContent = new GUIContent("Insert point");

        private static readonly ToolTip splitSelectedTooltip = new ToolTip(
            "Insert point", "Insert a new point in the middle of each selected edge.", Keys.insertPoint
        );

        private static readonly GUILayoutOption width20 = GUILayout.Width(25);
        private static readonly GUILayoutOption width65 = GUILayout.Width(65);
    }
}

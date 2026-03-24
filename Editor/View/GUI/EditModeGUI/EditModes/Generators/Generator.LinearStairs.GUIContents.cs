using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorLinearStairsGUI
    {
        private static readonly GUIContent stepDepthContent = new GUIContent("Step Depth");
        private static readonly ToolTip stepDepthTooltip = new ToolTip("Step Depth", "Set how deep the steps of the stairs are");
        private static readonly GUIContent stepHeightContent = new GUIContent("Step Height");
        private static readonly ToolTip stepHeightTooltip = new ToolTip("Step Height", "Set how high the steps of the stairs are");
        private static readonly GUIContent stairsWidthContent = new GUIContent("Stairs Width");
        private static readonly ToolTip stairsWidthTooltip = new ToolTip("Stairs Width", "Set how wide the entire staircase is");
        private static readonly GUIContent stairsHeightContent = new GUIContent("Stairs Height");
        private static readonly ToolTip stairsHeightTooltip = new ToolTip("Stairs Height", "Set how high the entire staircase is");
        private static readonly GUIContent stairsDepthContent = new GUIContent("Stairs Depth");
        private static readonly ToolTip stairsDepthTooltip = new ToolTip("Stairs Depth", "Set how deep the entire staircase is");
        private static readonly GUIContent totalStepsContent = new GUIContent("Total Steps");
        private static readonly ToolTip totalStepsTooltip = new ToolTip("Total Steps", "Set the total number of steps in this staircase");
        private static readonly GUIContent extraDepthContent = new GUIContent("Extra Depth");
        private static readonly ToolTip extraDepthTooltip = new ToolTip("Extra Depth", "Add an additional space before the steps start at the top");
        private static readonly GUIContent extraHeightContent = new GUIContent("Extra Height");
        private static readonly ToolTip extraHeightTooltip = new ToolTip("Extra Height", "Add additional height to the step at the bottom of the staircase");
        private static readonly GUIContent stairsBottomContent = new GUIContent("Bottom");
        private static readonly ToolTip stairsBottomTooltip = new ToolTip("Bottom", "Sets how the bottom of the stair steps should look");

        private static readonly GUILayoutOption width25 = GUILayout.Width(25);
        private static readonly GUILayoutOption width65 = GUILayout.Width(65);
        private static readonly GUILayoutOption width80 = GUILayout.Width(80);
    }
}

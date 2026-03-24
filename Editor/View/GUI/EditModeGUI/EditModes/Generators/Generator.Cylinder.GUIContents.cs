using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorCylinderGUI
    {
        private static readonly GUIContent smoothShadingContent = new GUIContent("Smooth shading");

        private static readonly ToolTip smoothShadingTooltip = new ToolTip(
            "Smooth shading"
          , "Toggle if you want the sides of the cylinder have smooth lighting or have a faceted look."
        );

        private static readonly GUIContent radialCapsContent = new GUIContent("Radial caps");

        private static readonly ToolTip radialCapsTooltip = new ToolTip(
            "Radial caps"
          , "Toggle if you want the top and bottom of the cylinder be a single polygon, or have a triangle per side."
        );

        private static readonly GUIContent alignToSideContent = new GUIContent("Start mid side");

        private static readonly ToolTip alignToSideTooltip = new ToolTip(
            "Start in the middle of a side"
          , "Toggle if you want the cylinder to begin in the center of a side, or at a point."
        );

        private static readonly GUIContent fitShapeContent = new GUIContent("Fit shape");

        private static readonly ToolTip fitShapeTooltip = new ToolTip(
            "Fit shape"
          , "Toggle if you want the cylinder to be fitted to the square that encapsulates the full circle that's defined by its radius. This makes the shapes more predictable when they have only a few sides, but it may change its shape slightly."
        );

        private static readonly GUIContent offsetContent = new GUIContent("Angle");
        private static readonly ToolTip offsetTooltip = new ToolTip("Offset angle", "Set the offset angle at which the cylinder starts.");
        private static readonly GUIContent sidesContent = new GUIContent("Sides");
        private static readonly ToolTip sidesTooltip = new ToolTip("Number of sides", "Set the number of sides the cylinder has when generated.");
        private static readonly GUIContent heightContent = new GUIContent("Height");
        private static readonly ToolTip heightTooltip = new ToolTip("Height", "Set the height of the cylinder.");
        private static readonly GUIContent radiusContent = new GUIContent("Radius");
        private static readonly ToolTip radiusTooltip = new ToolTip("Radius", "Set the radius of the cylinder. The radius is half of the width of a cylinder.");

        private static readonly GUILayoutOption width25 = GUILayout.Width(25);
        private static readonly GUILayoutOption width65 = GUILayout.Width(65);
        private static readonly GUILayoutOption width80 = GUILayout.Width(80);
        private static readonly GUILayoutOption width110 = GUILayout.Width(110);
    }
}

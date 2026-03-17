using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class SceneViewBottomBarGUI
    {
        private static readonly ToolTip showGridTooltip = new ToolTip(
            "Show the grid"
          , "Click this to toggle between showing the grid or hiding it.\nWhen hidden you can still snap against it."
          , Keys.toggleShowGridKey
        );

        private static readonly ToolTip gridSnapModeTooltip = new ToolTip(
            "Grid Snapping mode"
          , "Select grid to snap to the grid, relative snapping to snap along a distance from your starting position, or no snapping."
          , Keys.toggleSnappingKey
        );

        private static readonly ToolTip relativeSnapModeTooltip = new ToolTip(
            "Relative Snapping mode"
          , "Select grid to snap to the grid, relative snapping to snap along a distance from your starting position, or no snapping."
          , Keys.toggleSnappingKey
        );

        private static readonly ToolTip noSnappingModeTooltip = new ToolTip(
            "No Snapping mode"
          , "Select grid to snap to the grid, relative snapping to snap along a distance from your starting position, or no snapping."
          , Keys.toggleSnappingKey
        );

        private static readonly ToolTip showWireframeTooltip = new ToolTip(
            "Toggle wireframe", "Click this to switch between showing the\nwireframe of your scene or the regular view."
        );

        private static readonly ToolTip rebuildTooltip = new ToolTip(
            "Rebuild your CSG meshes", "Click this to rebuild your CSG meshes\nin case something didn't go quite right."
        );

        private static readonly ToolTip helperSurfacesTooltip = new ToolTip(
            "Helper surfaces", "Select what kind of helper surfaces you want to display in this sceneview."
        );

        private static readonly GUIContent xLabel = new GUIContent("X");
        private static readonly GUIContent yLabel = new GUIContent("Y");
        private static readonly GUIContent zLabel = new GUIContent("Z");

        private static readonly ToolTip xTooltipOff = new ToolTip(
            "Lock X axis", "Click to disable movement on the X axis"
        );

        private static readonly ToolTip yTooltipOff = new ToolTip(
            "Lock Y axis", "Click to disable movement on the Y axis"
        );

        private static readonly ToolTip zTooltipOff = new ToolTip(
            "Lock Z axis", "Click to disable movement on the Z axis"
        );

        private static readonly ToolTip xTooltipOn = new ToolTip(
            "Unlock X axis", "Click to enable movement on the X axis"
        );

        private static readonly ToolTip yTooltipOn = new ToolTip(
            "Unlock Y axis", "Click to enable movement on the Y axis"
        );

        private static readonly ToolTip zTooltipOn = new ToolTip(
            "Unlock Z axis", "Click to enable movement on the Z axis"
        );

        private static readonly GUIContent positionLargeLabel = new GUIContent("position");
        private static readonly GUIContent positionSmallLabel = new GUIContent("pos");
        private static readonly GUIContent positionPlusLabel = new GUIContent("+");
        private static readonly GUIContent positionMinusLabel = new GUIContent("-");

        private static readonly ToolTip positionTooltip = new ToolTip(
            "Grid size"
          , "Here you can set the size of the grid. Click this\nto switch between setting the grid size for X Y Z\nseparately, or for all of them uniformly."
        );

        private static readonly ToolTip positionPlusTooltip = new ToolTip(
            "Double grid size", "Multiply the grid size by 2", Keys.doubleGridSizeKey
        );

        private static readonly ToolTip positionMinnusTooltip = new ToolTip(
            "Half grid size", "Divide the grid size by 2", Keys.halfGridSizeKey
        );

        private static readonly GUIContent scaleLargeLabel = new GUIContent("scale");
        private static readonly GUIContent scaleSmallLabel = new GUIContent("scl");
        private static readonly GUIContent scalePlusLabel = new GUIContent("+");
        private static readonly GUIContent scaleMinusLabel = new GUIContent("-");
        private static readonly GUIContent scaleUnitLabel = new GUIContent("%");

        private static readonly ToolTip scaleTooltip = new ToolTip(
            "Scale snapping", "Here you can set scale snapping."
        );

        private static readonly ToolTip scalePlusTooltip = new ToolTip(
            "Increase scale snapping", "Multiply the scale snapping by 10"
        );

        private static readonly ToolTip scaleMinnusTooltip = new ToolTip(
            "Decrease scale snapping", "Divide the scale snapping by 10"
        );

        private static readonly GUIContent angleLargeLabel = new GUIContent("angle");
        private static readonly GUIContent angleSmallLabel = new GUIContent("ang");
        private static readonly GUIContent anglePlusLabel = new GUIContent("+");
        private static readonly GUIContent angleMinusLabel = new GUIContent("-");
        private static readonly GUIContent angleUnitLabel = new GUIContent("°");

        private static readonly ToolTip angleTooltip = new ToolTip(
            "Angle snapping", "Here you can set rotational snapping."
        );

        private static readonly ToolTip anglePlusTooltip = new ToolTip(
            "Double angle snapping", "Multiply the rotational snapping by 2"
        );

        private static readonly ToolTip angleMinnusTooltip = new ToolTip(
            "Half angle snapping", "Divide the rotational snapping by 2"
        );

        private static GUIStyle m_MiniTextStyle;
        private static GUIStyle m_TextInputStyle;

        private static bool m_LocalStyles;

        private static void InitStyles()
        {
            if(m_LocalStyles)
            {
                return;
            }

            m_MiniTextStyle               = new GUIStyle(EditorStyles.miniLabel);
            m_MiniTextStyle.contentOffset = new Vector2(0, -1);
            m_TextInputStyle              = new GUIStyle(EditorStyles.miniTextField);
            m_TextInputStyle.padding.top--;
            m_TextInputStyle.margin.top += 2;
            m_LocalStyles               =  true;
        }
    }
}

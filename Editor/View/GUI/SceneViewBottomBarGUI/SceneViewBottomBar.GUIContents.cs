using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class SceneViewBottomBarGUI
    {
        private static ToolTip showGridTooltip = new ToolTip(
            "Show the grid"
          , "Click this to toggle between showing the grid or hiding it.\nWhen hidden you can still snap against it."
          , Keys.toggleShowGridKey
        );

//		static ToolTip snapToGridTooltip	    = new ToolTip("Grid snapping", "Click this if you want to turn grid snapping on or off.", Keys.ToggleSnappingKey);
        private static ToolTip gridSnapModeTooltip = new ToolTip(
            "Grid Snapping mode"
          , "Select grid to snap to the grid, relative snapping to snap along a distance from your starting position, or no snapping."
          , Keys.toggleSnappingKey
        );

        private static ToolTip relativeSnapModeTooltip = new ToolTip(
            "Relative Snapping mode"
          , "Select grid to snap to the grid, relative snapping to snap along a distance from your starting position, or no snapping."
          , Keys.toggleSnappingKey
        );

        private static ToolTip noSnappingModeTooltip = new ToolTip(
            "No Snapping mode"
          , "Select grid to snap to the grid, relative snapping to snap along a distance from your starting position, or no snapping."
          , Keys.toggleSnappingKey
        );

        private static ToolTip showWireframeTooltip = new ToolTip(
            "Toggle wireframe", "Click this to switch between showing the\nwireframe of your scene or the regular view."
        );

        private static ToolTip rebuildTooltip = new ToolTip(
            "Rebuild your CSG meshes", "Click this to rebuild your CSG meshes\nin case something didn't go quite right."
        );

        private static ToolTip helperSurfacesTooltip = new ToolTip(
            "Helper surfaces", "Select what kind of helper surfaces you want to display in this sceneview."
        );

        private static GUIContent xLabel = new GUIContent("X");
        private static GUIContent yLabel = new GUIContent("Y");
        private static GUIContent zLabel = new GUIContent("Z");

        private static ToolTip xTooltipOff = new ToolTip("Lock X axis", "Click to disable movement on the X axis");
        private static ToolTip yTooltipOff = new ToolTip("Lock Y axis", "Click to disable movement on the Y axis");
        private static ToolTip zTooltipOff = new ToolTip("Lock Z axis", "Click to disable movement on the Z axis");
        private static ToolTip xTooltipOn = new ToolTip("Unlock X axis", "Click to enable movement on the X axis");
        private static ToolTip yTooltipOn = new ToolTip("Unlock Y axis", "Click to enable movement on the Y axis");
        private static ToolTip zTooltipOn = new ToolTip("Unlock Z axis", "Click to enable movement on the Z axis");

        private static GUIContent positionLargeLabel = new GUIContent("position");
        private static GUIContent positionSmallLabel = new GUIContent("pos");
        private static GUIContent positionPlusLabel = new GUIContent("+");
        private static GUIContent positionMinusLabel = new GUIContent("-");

        private static ToolTip positionTooltip = new ToolTip(
            "Grid size"
          , "Here you can set the size of the grid. Click this\nto switch between setting the grid size for X Y Z\nseparately, or for all of them uniformly."
        );

        private static ToolTip positionPlusTooltip = new ToolTip(
            "Double grid size", "Multiply the grid size by 2", Keys.doubleGridSizeKey
        );

        private static ToolTip positionMinnusTooltip = new ToolTip(
            "Half grid size", "Divide the grid size by 2", Keys.halfGridSizeKey
        );

        private static GUIContent scaleLargeLabel = new GUIContent("scale");
        private static GUIContent scaleSmallLabel = new GUIContent("scl");
        private static GUIContent scalePlusLabel = new GUIContent("+");
        private static GUIContent scaleMinusLabel = new GUIContent("-");
        private static GUIContent scaleUnitLabel = new GUIContent("%");

        private static ToolTip scaleTooltip = new ToolTip("Scale snapping", "Here you can set scale snapping.");

        private static ToolTip scalePlusTooltip = new ToolTip(
            "Increase scale snapping", "Multiply the scale snapping by 10"
        );

        private static ToolTip scaleMinnusTooltip = new ToolTip(
            "Decrease scale snapping", "Divide the scale snapping by 10"
        );

        private static GUIContent angleLargeLabel = new GUIContent("angle");
        private static GUIContent angleSmallLabel = new GUIContent("ang");
        private static GUIContent anglePlusLabel = new GUIContent("+");
        private static GUIContent angleMinusLabel = new GUIContent("-");
        private static GUIContent angleUnitLabel = new GUIContent("°");

        private static ToolTip angleTooltip = new ToolTip("Angle snapping", "Here you can set rotational snapping.");

        private static ToolTip anglePlusTooltip = new ToolTip(
            "Double angle snapping", "Multiply the rotational snapping by 2"
        );

        private static ToolTip angleMinnusTooltip = new ToolTip(
            "Half angle snapping", "Divide the rotational snapping by 2"
        );

//		static GUILayoutOption EnumMaxWidth		= GUILayout.MaxWidth(165);
//		static GUILayoutOption EnumMinWidth		= GUILayout.MinWidth(20);
//		static GUILayoutOption MinSnapWidth		= GUILayout.MinWidth(30);
//		static GUILayoutOption MaxSnapWidth		= GUILayout.MaxWidth(70);

        private static GUIStyle miniTextStyle;
        private static GUIStyle textInputStyle;

        private static bool localStyles = false;

        private static void InitStyles()
        {
            if(localStyles)
            {
                return;
            }

            miniTextStyle               = new GUIStyle(EditorStyles.miniLabel);
            miniTextStyle.contentOffset = new Vector2(0, -1);
            textInputStyle              = new GUIStyle(EditorStyles.miniTextField);
            textInputStyle.padding.top--;
            textInputStyle.margin.top += 2;
            localStyles               =  true;
        }
    }
}

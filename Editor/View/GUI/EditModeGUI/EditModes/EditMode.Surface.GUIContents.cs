using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditModeSurfaceGUI
    {
        private const int MATERIAL_SIZE = 100;
        private const int WINDOW_WIDTH = MATERIAL_SIZE;
        private const int JUSTIFY_BUTTON_SIZE = 22;
        private const int LABEL_WIDTH_CONST = 56;
        private const int BUTTON_WIDTH = 42;

        private static GUIContent m_ContentSurfacesLabel;

        private static readonly GUIContent contentJustifyLabel = new GUIContent("UV Justify");
        private static readonly GUIContent contentJustifyUpLeft = new GUIContent("↖");
        private static readonly GUIContent contentJustifyUp = new GUIContent("↑");
        private static readonly GUIContent contentJustifyUpRight = new GUIContent("↗");
        private static readonly GUIContent contentJustifyLeft = new GUIContent("←");
        private static readonly GUIContent contentJustifyCenter = new GUIContent("C");
        private static readonly GUIContent contentJustifyRight = new GUIContent("→");
        private static readonly GUIContent contentJustifyDownLeft = new GUIContent("↙");
        private static readonly GUIContent contentJustifyDown = new GUIContent("↓");
        private static readonly GUIContent contentJustifyDownRight = new GUIContent("↘");

        private static readonly ToolTip toolTipJustifyUpLeft = new ToolTip(
            "Top Left Align", "Align the texture to the top (smallest U) left (smallest V) corner of the surface"
        );

        private static readonly ToolTip toolTipJustifyUp = new ToolTip(
            "Top Align", "Align the texture to the top (smallest U) side of the surface"
        );

        private static readonly ToolTip toolTipJustifyUpRight = new ToolTip(
            "Top Right Align", "Align the texture to the top (smallest U) right (largest V) corner of the surface"
        );

        private static readonly ToolTip toolTipJustifyLeft = new ToolTip(
            "Left Align", "Align the texture to the left (smallest V) side of the surface"
        );

        private static readonly ToolTip toolTipJustifyCenter = new ToolTip(
            "Center Align", "Align the texture to the center of the surface"
        );

        private static readonly ToolTip toolTipJustifyRight = new ToolTip(
            "Right Align", "Align the texture to the right (largest V) side of the surface"
        );

        private static readonly ToolTip toolTipJustifyDownLeft = new ToolTip(
            "Bottom Left Align", "Align the texture to the bottom (largest U) left (smallest U) corner of the surface"
        );

        private static readonly ToolTip toolTipJustifyDown = new ToolTip(
            "Bottom Align", "Align the texture to the bottom (largest U) side of the surface"
        );

        private static readonly ToolTip toolTipJustifyDownRight = new ToolTip(
            "Bottom Right Align", "Align the texture to the bottom (largest U) right (largest U) corner of the surface"
        );

        private static readonly GUIContent contentMaterial = new GUIContent("Material");

        private static readonly GUIContent contentLockTexture = new GUIContent("Lock texture to object");

        private static readonly ToolTip toolTipLockTexture = new ToolTip(
            "Lock texture to object", "When not set, the texture will stay\n" +
            "in the same world position when the\n" +
            "brush is moved in the world. When \n" +
            "set it'll move with the brush."
        );

        private static readonly GUIContent contentSelectAllSurfaces = new GUIContent("Select all brush surfaces");

        private static readonly ToolTip tooltipSelectAllSurfaces = new ToolTip(
            "Select all brush surfaces", "Select all the surfaces of all the\n" +
            "brushes that are currently selected."
        );

        private static readonly GUIContent contentReset = new GUIContent("Reset");
        private static readonly GUIContent contentResetXY = new GUIContent("UV");
        private static readonly GUIContent contentResetX = new GUIContent("U");
        private static readonly GUIContent contentResetY = new GUIContent("V");

        private static readonly ToolTip toolTipResetXY = new ToolTip(
            "Reset U & V coordinates", "Reset the texture coordinates in the U and V direction"
        );

        private static readonly ToolTip toolTipResetX = new ToolTip(
            "Reset U coordinates", "Reset the texture coordinates in the U direction"
        );

        private static readonly ToolTip toolTipResetY = new ToolTip(
            "Reset V coordinates", "Reset the texture coordinates in the V direction"
        );

        private static readonly GUIContent contentFit = new GUIContent("Fit");
        private static readonly GUIContent contentFitXY = new GUIContent("UV");
        private static readonly GUIContent contentFitX = new GUIContent("U");
        private static readonly GUIContent contentFitY = new GUIContent("V");

        private static readonly ToolTip toolTipFitXY = new ToolTip(
            "Fit U & V coordinates", "Fit the texture coordinates in both the U and V direction"
        );

        private static readonly ToolTip toolTipFitX = new ToolTip(
            "Fit U coordinates", "Fit the texture coordinates in the U direction"
        );

        private static readonly ToolTip toolTipFitY = new ToolTip(
            "Fit V coordinates", "Fit the texture coordinates in the V direction"
        );

        private static readonly GUIContent contentFlip = new GUIContent("Reverse");
        private static readonly GUIContent contentFlipXY = new GUIContent("UV");
        private static readonly GUIContent contentFlipX = new GUIContent("U");
        private static readonly GUIContent contentFlipY = new GUIContent("V");

        private static readonly ToolTip toolTipFlipXY = new ToolTip(
            "Flip U & V coordinates", "Reverse the texture coordinates in both the U and V direction"
        );

        private static readonly ToolTip toolTipFlipX = new ToolTip(
            "Flip U coordinates", "Reverse the texture coordinates in the U direction"
        );

        private static readonly ToolTip toolTipFlipY = new ToolTip(
            "Flip V coordinates", "Reverse the texture coordinates in the V direction"
        );

        private static readonly GUIContent contentUVScale = new GUIContent("UV Scale");
        private static readonly GUIContent contentScale = new GUIContent("Scale");

        private static readonly ToolTip toolTipScaleUV = new ToolTip(
            "UV Scale", "Sets the U and V scale of the texture on the surface of the brush."
        );

        private static readonly GUIContent contentDoubleScale = new GUIContent("x2");
        private static readonly GUIContent contentHalfScale = new GUIContent("/2");

        private static readonly ToolTip toolTipDoubleScale = new ToolTip(
            "Double the scale"
          , "Make the texturing bigger by doubling the scale. This will decrease the tiling (if any)."
        );

        private static readonly ToolTip toolTipHalfScale = new ToolTip(
            "Half the scale", "Make the texturing smaller by halving the scale. This will increase the tiling (if any)."
        );

        private static readonly GUIContent contentUSymbol = new GUIContent("U");
        private static readonly GUIContent contentVSymbol = new GUIContent("V");
        private static readonly GUIContent contentOffset = new GUIContent("UV Offset");

        private static readonly ToolTip toolTipOffsetUV = new ToolTip(
            "UV Offset", "Sets the U and V position of the texture on the surface of the brush."
        );

        private static readonly GUIContent contentRotate = new GUIContent("Rotation");

        private static readonly ToolTip toolTipRotation = new ToolTip(
            "Rotation", "Sets the rotation the texture on the surface of the brush."
        );

        private static readonly GUIContent contentAngleSymbol = new GUIContent("°");
        private static readonly GUIContent contentRotate90Negative = new GUIContent("-90°");
        private static readonly GUIContent contentRotate90Positive = new GUIContent("+90°");

        private static readonly ToolTip toolTipRotate90Negative = new ToolTip(
            "Rotate texture -90°", "Subtracts 90 from the rotation of the texture"
        );

        private static readonly ToolTip toolTipRotate90Positive = new ToolTip(
            "Rotate texture +90°", "Adds 90 to the rotation of the texture"
        );

        private static readonly GUIContent contentUnSmoothSurfaces = new GUIContent("Un-smooth");
        private static readonly GUIContent contentSmoothSurfaces = new GUIContent("Smooth");

        private static readonly ToolTip toolTipUnSmoothSurfaces = new ToolTip(
            "Un-smooth", "Remove all selected smoothed surfaces from smoothing groups"
        );

        private static readonly ToolTip toolTipSmoothSurfaces = new ToolTip(
            "Smooth", "Put all selected surfaces in same smoothing group"
        );

        private static readonly GUILayoutOption minFloatFieldWidth = GUILayout.MinWidth(20);

        public static readonly GUILayoutOption smallLabelWidth = GUILayout.Width(45);
        public static readonly GUILayoutOption largeLabelWidth = GUILayout.Width(80);
        private static readonly GUILayoutOption unitWidth = GUILayout.Width(14);

        private static readonly GUILayoutOption justifyButtonWidth = GUILayout.Width(JUSTIFY_BUTTON_SIZE);
        private static readonly GUILayoutOption justifyButtonHeight = GUILayout.Height(JUSTIFY_BUTTON_SIZE);

        private static readonly GUILayoutOption[] justifyButtonLayout =
            new GUILayoutOption[] { justifyButtonWidth, justifyButtonHeight };

        private static readonly GUILayoutOption materialWidth = GUILayout.Width(MATERIAL_SIZE);
        private static readonly GUILayoutOption materialHeight = GUILayout.Height(MATERIAL_SIZE);

        private static void InitLocalStyles()
        {
            if(m_ContentSurfacesLabel != null)
            {
                return;
            }

            m_ContentSurfacesLabel = new GUIContent(CsgguiStyleUtility.BrushEditModeNames[(int)ToolEditMode.Surfaces]);
        }
    }
}

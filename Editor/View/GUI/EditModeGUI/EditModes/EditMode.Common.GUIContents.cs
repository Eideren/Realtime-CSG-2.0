using UnityEngine;
using RealtimeCSG;

namespace InternalRealtimeCSG
{
    internal sealed partial class EditModeCommonGUI
    {
        private static readonly GUIContent updateLightmapUVContent = new GUIContent("Update lightmap UVs");

        private static readonly GUIContent contentShadows = new GUIContent("Shadows");
        private static readonly GUIContent contentCastShadowsSurfaces = new GUIContent("Cast");
        private static readonly GUIContent contentReceiveShadowsSurfaces = new GUIContent("Receive");

        private static readonly GUIContent contentVisibleSurfaces = new GUIContent("Visible");
        private static readonly GUIContent contentCollisionSurfaces = new GUIContent("Collision");

        private static readonly ToolTip toolTipCastShadowsSurfaces = new ToolTip(
            "Cast Shadows"
          , "Toggle shadow casting for all selected surfaces. When cast is not toggled, these surfaces will not cast a shadow."
        );

        private static readonly ToolTip toolTipReceiveShadowsSurfaces = new ToolTip(
            "Receive Shadows"
          , "Toggle shadow receiving for all selected surfaces. When receive is not toggled, these surfaces will not receive shadows (when visible). Note: only works with forward rendering"
        );

        private static readonly ToolTip toolTipVisibleSurfaces = new ToolTip(
            "Visible"
          , "Toggle visibility for all selected surfaces. When visible is not toggled, it won't be part of the rendered mesh."
        );

        private static readonly ToolTip toolTipCollisionSurfaces = new ToolTip(
            "Collision"
          , "Toggle collision on/off for all selected surfaces. When collision is not toggled, it won't be part of the collision mesh."
        );
    }
}

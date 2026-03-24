using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditModeSelectionGUI
    {
        private static readonly GUIContent contentTitleLabel = new GUIContent("Realtime CSG");

        private static readonly GUIContent handleAsOneLabel = new GUIContent("Handle as one object", "Select this operation when a child is selected");

        private static readonly GUIContent prefabLabelContent = new GUIContent("Drag & drop behaviour");
        private static readonly GUIContent raySnappingLabelContent = new GUIContent("Ray snapping behaviour");

        private static readonly GUIContent prefabInstantiateBehaviourContent = new GUIContent("Instantiation");
        private static readonly GUIContent destinationAlignmentContent = new GUIContent("Placement");

        private static readonly GUIContent sourceAlignmentContent = new GUIContent("Front");
        private static readonly GUIContent surfacesContent = new GUIContent("Surfaces");

        private static readonly ToolTip raySnappingBehaviourTooltip = new ToolTip(
            "Ray snapping behaviour"
          , "Here you can set how your object will align to the surface underneath it when dragging it while ray-snapping (holding Shift)"
        );

        private static readonly ToolTip prefabInstantiateBehaviourTooltip = new ToolTip(
            "Instantiation"
          , "Here you can set if your prefab is copied into the scene without any link to the original prefab, or if it'll be an instance of your prefab (default unity behaviour)."
        );

        private static readonly ToolTip destinationAlignmentTooltip = new ToolTip(
            "Placement", "Here you can set how your object will be rotated if it's dragged over a brush surface.\n\n" +
            "Align to Surface:\nRotate it so that it's aligned with the surface you drag over.\n\n" +
            "Always face up:\nAlign it with the surface but always make it face 'up'.\n\n" +
            "None:\ndo not rotate it along the surface."
        );

        private static readonly ToolTip sourceAlignmentTooltip = new ToolTip(
            "Front"
          , "Define here what direction the original object was created and what is considered it's 'front', this helps align it properly during placement."
        );

        private static readonly GUIContent versionLabel = new GUIContent(Foundation.CSGManager.VersionName);
        private static readonly GUIContent disabledLabelContent = new GUIContent("Realtime CSG is disabled, press control-F3 to enable");
        private static readonly GUIContent enableRealtimeCSGContent = new GUIContent("Enable Realtime-CSG");
    }
}

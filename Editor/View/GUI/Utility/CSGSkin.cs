using UnityEngine;

namespace RealtimeCSG
{
    internal sealed class CSGSkin
    {
        public readonly GUIContent[] hierarchyOperations = new GUIContent[CsgguiStyleUtility.OPERATION_TYPE_COUNT];
        public GUIContent hierarchyPassThrough;

        public readonly GUIContent[] operationNames = new GUIContent[CsgguiStyleUtility.OPERATION_TYPE_COUNT];
        public readonly GUIContent[] operationNamesOn = new GUIContent[CsgguiStyleUtility.OPERATION_TYPE_COUNT];

        public readonly GUIContent[] shapeModeNames = new GUIContent[CsgguiStyleUtility.SHAPE_MODE_COUNT];
        public readonly GUIContent[] shapeModeNamesOn = new GUIContent[CsgguiStyleUtility.SHAPE_MODE_COUNT];

        public GUIContent passThrough;
        public GUIContent passThroughOn;

        public GUIContent wireframe;
        public GUIContent wireframeOn;

        public readonly GUIContent[] clipNames = new GUIContent[CsgguiStyleUtility.CLIP_TYPE_COUNT];
        public readonly GUIContent[] clipNamesOn = new GUIContent[CsgguiStyleUtility.CLIP_TYPE_COUNT];

        public GUIContent rebuildIcon;

        public GUIContent gridIcon;
        public GUIContent gridIconOn;

        public GUIContent gridSnapIcon;
        public GUIContent gridSnapIconOn;
        public GUIContent relSnapIcon;
        public GUIContent relSnapIconOn;
        public GUIContent noSnapIcon;
        public GUIContent noSnapIconOn;
        public GUIStyle messageStyle;
        public GUIStyle messageWarningStyle;

        public Color lockedBackgroundColor;
        public GUIStyle xToolbarButton;
        public GUIStyle yToolbarButton;
        public GUIStyle zToolbarButton;

        public GUIStyle redToolbarDropDown;

        public GUIStyle menuItem;
    };
}

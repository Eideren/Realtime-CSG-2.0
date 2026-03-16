using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal class TooltipContentState
    {
        public Rect tooltipArea;
        public ToolTip prevToolTip;
        public ToolTip currentToolTip;

        public GUIContent currentToolTipTitle;
        public GUIContent currentToolTipContents;
        public GUIContent currentToolTipKeyCodes;

        public Rect currentToolTipTitleArea = new Rect();
        public Rect currentToolTipContentsArea = new Rect();
        public Rect currentToolTipKeyCodesArea = new Rect();

        public Rect currentToolTipArea;

        //public Vector2    rectOffset;
        public float tooltipTimer = 0;
        public bool drawnThisFrame = false;

        public Rect maxArea;

        public EditorWindow editorWindow;
        public Editor editor;
    }
}

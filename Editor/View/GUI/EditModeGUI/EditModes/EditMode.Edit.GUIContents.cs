using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class EditModeMeshModeGUI
    {
        private static GUIContent m_ContentMeshLabel;

        private static readonly GUIContent contentDefaultMaterial = new GUIContent("Default");

        private static readonly GUILayoutOption labelWidth = GUILayout.Width(30);
        private static readonly GUILayoutOption largeLabelWidth = GUILayout.Width(80);
        private static readonly GUILayoutOption[] inSceneWidth = { GUILayout.Width(150) };

        private static void InitLocalStyles()
        {
            if(m_ContentMeshLabel != null)
            {
                return;
            }

            m_ContentMeshLabel = new GUIContent(CsgguiStyleUtility.BrushEditModeNames[(int)ToolEditMode.Edit]);
        }

        private static readonly GUIContent contentFlip = new GUIContent("Flip");
        private static readonly GUIContent contentFlipX = new GUIContent("X");
        private static readonly ToolTip tooltipFlipX = new ToolTip("Flip X", "Flip the selection in the x direction", Keys.flipSelectionX);
        private static readonly GUIContent contentFlipY = new GUIContent("Y");
        private static readonly ToolTip tooltipFlipY = new ToolTip("Flip Y", "Flip the selection in the y direction", Keys.flipSelectionY);
        private static readonly GUIContent contentFlipZ = new GUIContent("Z");
        private static readonly ToolTip tooltipFlipZ = new ToolTip("Flip Z", "Flip the selection in the z direction", Keys.flipSelectionZ);
        private static readonly GUIContent contentSnapToGrid = new GUIContent("Snap to grid");
        private static readonly ToolTip tooltipSnapToGrid = new ToolTip(contentSnapToGrid.text, "Snap the selection to the closest grid lines", Keys.snapToGridKey);
        private static readonly GUIContent contentSelection = new GUIContent("Selection");
        private static readonly GUIContent contentIgnoreHidden = new GUIContent("Ignore Hidden Surfaces");

        private static readonly ToolTip tooltipIgnoreHidden = new ToolTip(
            "Ignore Hidden Surfaces", "When set, selecting surfaces will\n" +
            "only select the front most surfaces."
        );

        private static readonly GUIContent contentVertex = new GUIContent("Vertex");
        private static readonly ToolTip tooltipVertex = new ToolTip("Vertex", "When set you can select vertices.");
        private static readonly GUIContent contentEdge = new GUIContent("Edge");
        private static readonly ToolTip tooltipEdge = new ToolTip("Edge", "When set you can select edges.");
        private static readonly GUIContent contentSurface = new GUIContent("Surface");
        private static readonly ToolTip tooltipSurface = new ToolTip("Surface", "When set you can select surfaces.");

        private static readonly GUIContent contentAutoCommitExtrusion = new GUIContent("Auto commit extrusion");

        private static readonly ToolTip tooltipAutoCommitExtrusion = new ToolTip(
            "Auto commit extrusion", "When set, when extruding a surface,\n" +
            "using shift drag on a polygon,\n" +
            "editing will automatically commit\n" +
            "the brush when releasing the mouse\n" +
            "button."
        );
    }
}

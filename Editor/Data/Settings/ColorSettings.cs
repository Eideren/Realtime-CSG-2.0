using System.Globalization;
using RealtimeCSG.Components;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal static class ColorSettings
    {
        private static readonly float state0 = Mathf.Pow(1.00f, 2.2f);
        private static readonly float state1 = Mathf.Pow(0.90f, 2.2f);
        private static readonly float state2 = Mathf.Pow(0.60f, 2.2f);
        private static readonly float state3 = Mathf.Pow(0.70f, 2.2f);

        private static readonly Color[] pointInnerStateColor =
        {
            new Color(0.80f, state3, 0.00f), // None
            new Color(1.00f, state2, 0.00f)  // Hovering
          , new Color(1.0f, state1, 0.00f)   // Selected
          , new Color(state0, state0, 0.00f) // Selected | Hovering
        };

        public static Color[] PointInnerStateColor
        {
            get => pointInnerStateColor;
        }

        private static readonly Color[] alignedCurveStateColor =
        {
            new Color(0.80f, state3, 0.00f), // None
            new Color(1.00f, state2, 0.00f)  // Hovering
          , new Color(1.0f, state1, 0.00f)   // Selected
          , new Color(state0, state0, 0.00f) // Selected | Hovering
        };

        public static Color[] AlignedCurveStateColor
        {
            get => alignedCurveStateColor;
        }

        private static readonly Color[] brokenCurveStateColor =
        {
            new Color(0.80f, state3, 0.00f), // None
            new Color(1.00f, state2, 0.00f)  // Hovering
          , new Color(1.0f, state1, 0.00f)   // Selected
          , new Color(state0, state0, 0.00f) // Selected | Hovering
        };

        public static Color[] BrokenCurveStateColor
        {
            get => brokenCurveStateColor;
        }

        private static readonly Color[] polygonInnerStateColor =
        {
            new Color(state3, 0.80f, state3), // None
            new Color(state1, 1.00f, state1)  // Hovering
          , new Color(state1, state1, 0.0f)   // Selected
          , new Color(state0, state0, 0.0f)   // Selected | Hovering
        };

        public static Color[] PolygonInnerStateColor
        {
            get => polygonInnerStateColor;
        }

        public static readonly Color[] surfaceInnerStateColor =
        {
            new Color(state3, state3, 0.80f), // None
            new Color(state2, state2, 1.00f)  // Hovering
          , new Color(state1, state1, 1.00f)  // Selected
          , new Color(state0, state0, 1.00f)  // Selected | Hovering
        };

        public static Color[] SurfaceInnerStateColor
        {
            get => surfaceInnerStateColor;
        }

        public static readonly Color[] surfaceOuterStateColor =
        {
            new Color(state3, state3, 0.80f), // None
            new Color(state2, state2, 1.00f)  // Hovering// Hovering
          , new Color(state1, state1, 1.00f)  // Selected
          , new Color(state0, state0, 1.00f)  // Selected | Hovering
        };

        public static Color[] SurfaceOuterStateColor
        {
            get => surfaceOuterStateColor;
        }

        public static readonly Color[] surfaceTriangleStateColor =
        {
            new Color(state3, state3, 0.80f, 0.125f), // None
            new Color(state2, state2, 1.00f, 0.125f)  // Hovering
          , new Color(state1, state1, 1.00f, 0.125f)  // Selected
          , new Color(state0, state0, 1.00f, 0.125f)  // Selected | Hovering
        };

        public static Color[] SurfaceTriangleStateColor
        {
            get => surfaceTriangleStateColor;
        }

        private static readonly Color[] invalidInnerStateColor =
        {
            new Color(state3, 0.0f, 0.0f), // None
            new Color(state2, 0.0f, 0.0f)  // Hovering
          , new Color(state1, 0.0f, 0.0f)  // Selected
          , new Color(state0, 0.0f, 0.0f)  // Selected | Hovering
        };

        public static Color[] InvalidInnerStateColor
        {
            get => invalidInnerStateColor;
        }

        public static Color GridColorW = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);

        private const float LARGE = 0.95f;
        private const float SMALL = 0.35f;

        public static Color GridColor1W = new Color(
            LARGE * GridColorW.r, LARGE * GridColorW.g, LARGE * GridColorW.b, 1.00f * GridColorW.a
        );

        public static Color GridColor2W = new Color(
            SMALL * GridColorW.r, SMALL * GridColorW.g, SMALL * GridColorW.b, 0.65f * GridColorW.a
        );

        public static readonly Color outerInvalidColor = Color.black;
        public static readonly Color innerInvalidColor = Color.red;

        private static Color m_SelectedOutlines = new Color(1.0f, 0.4f, 0, 1.0f);

        public static Color SelectedOutlines
        {
            get => m_SelectedOutlines;
        }

        private static readonly Color hoverOutlines = new Color(1.0f, 1.0f, 0, 1.0f);

        public static Color HoverOutlines
        {
            get => hoverOutlines;
        }

        private static Color m_WireframeOutline = new Color(0.0f, 0.0f, 0.0f, 64.0f / 255.0f);

        public static Color WireframeOutline
        {
            get => m_WireframeOutline;
        }

        private static Color m_MeshEdgeOutline = new Color(0.0f, 0.0f, 0.0f, 1.0f);

        public static Color MeshEdgeOutline
        {
            get => m_MeshEdgeOutline;
        }

        private static Color m_BoundsOutlines = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        public static Color BoundsOutlines
        {
            get => m_BoundsOutlines;
        }

        private static Color m_BoundsEdgeHover = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        public static Color BoundsEdgeHover
        {
            get => m_BoundsEdgeHover;
        }

        private static Color m_ShapeDrawingFill = new Color(0.0f, 0.8f, 1, 1.00f);

        public static Color ShapeDrawingFill
        {
            get => m_ShapeDrawingFill;
        }

        private static Color m_RotateCirclePieOutline = new Color(0.0f, 0.2f, 1, 1.00f);

        public static Color RotateCirclePieOutline
        {
            get => m_RotateCirclePieOutline;
        }

        private static Color m_RotateCirclePieFill = new Color(0.0f, 0.2f, 1, 0.1f);

        public static Color RotateCirclePieFill
        {
            get => m_RotateCirclePieFill;
        }

        private static Color m_RotateCircleOutline = new Color(0.0f, 0.8f, 1, 1.00f);

        public static Color RotateCircleOutline
        {
            get => m_RotateCircleOutline;
        }

        private static Color m_RotateCircleHatches = new Color(1.0f, 1.0f, 1, 1.00f);

        public static Color RotateCircleHatches
        {
            get => m_RotateCircleHatches;
        }

        private static Color m_ClipPlaneOutline = new Color(0.0f, 0.2f, 1, 1.00f);

        public static Color ClipPlaneOutline
        {
            get => m_ClipPlaneOutline;
        }

        private static Color m_ClipPlaneFill = new Color(0.0f, 0.2f, 1, 0.1f);

        public static Color ClipPlaneFill
        {
            get => m_ClipPlaneFill;
        }

        private static Color m_ClipLeftOverOutlines = new Color(0.0f, 0.2f, 1, 1.0f);

        public static Color ClipLeftOverOutlines
        {
            get => m_ClipLeftOverOutlines;
        }

        private static readonly Color shadowColor = new Color(0.0f, 0.0f, 0.0f, 0.1f);

        public static Color ShadowColor
        {
            get => shadowColor;
        }

        private static readonly Color shadowOutlineColor = new Color(0.0f, 0.0f, 0.0f, 0.5f);

        public static Color ShadowOutlineColor
        {
            get => shadowOutlineColor;
        }

        private static Color m_SnappedEdges = new Color(
            125.0f / 255.0f, 176.0f / 255.0f, 250.0f / 255.0f, 95.0f / 255.0f
        );

        public static Color SnappedEdges
        {
            get => m_SnappedEdges;
        }

        private static Color m_GuideLine = new Color(125.0f / 255.0f, 176.0f / 255.0f, 250.0f / 255.0f, 95.0f / 255.0f);

        public static Color GuideLine
        {
            get => m_GuideLine;
        }

        private static Color m_Background = new Color(0.278431f, 0.278431f, 0.278431f, 0);

        public static Color Background
        {
            get => m_Background;
        }

        private static Color m_MinOutlines2D = new Color(0.3f, 0.00f, 0.55f, 1.0f);
        private static Color m_MaxOutlines2D = new Color(0.0f, 0.80f, 0.60f, 1.0f);
        private static Color[] m_Outlines2D;

        public static Color GetBrushOutlineColor(CSGBrush brush)
        {
            if(m_Outlines2D == null)
            {
                Update();
            }

            int instanceID = Mathf.Abs(brush.GetInstanceID());

            return m_Outlines2D[instanceID % m_Outlines2D.Length];
        }

        private static bool ParseColor(string name, ref string prevname, Color defaultColor, ref Color resultColor)
        {
            string prefString = EditorPrefs.GetString(name);

            if(prevname == prefString)
            {
                return false;
            }

            prevname = prefString;
            string[] split = prefString.Split(';');

            if(split.Length != 5)
            {
                resultColor = defaultColor;

                return false;
            }

            split[1] = split[1].Replace(',', '.');
            split[2] = split[2].Replace(',', '.');
            split[3] = split[3].Replace(',', '.');
            split[4] = split[4].Replace(',', '.');

            bool success = float.TryParse(
                split[1], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float r
            );

            success &= float.TryParse(
                split[2], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float g
            );

            success &= float.TryParse(
                split[3], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float b
            );

            success &= float.TryParse(
                split[4], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float a
            );

            if(success)
            {
                Color newColor = new Color(r, g, b, a);

                if(newColor == resultColor)
                {
                    return false;
                }

                resultColor = newColor;

                return true;
            }

            resultColor = defaultColor;

            return false;
        }

        private static readonly Color defaultGrid = new Color(.5f, .5f, .5f, .4f);
        private static readonly Color defaultGuideline = new Color(.5f, .5f, .5f, .2f);
        private static readonly Color defaultCenterAxis = new Color(.8f, .8f, .8f, .93f);
        private static readonly Color defaultSelectedAxis = new Color(246f / 255f, 242f / 255f, 50f / 255f, .89f);
        private static readonly Color defaultSelectionOutline = new Color(255f / 255f, 102f / 255f, 0f / 255f, 0.0f);
        private static readonly Color defaultBackground = new Color(0.278431f, 0.278431f, 0.278431f, 0);
        private static readonly Color defaultWireframe = new Color(0.0f, 0.0f, 0.0f, 0.5f);
        private static readonly Color defaultWireframeOverlay = new Color(0.0f, 0.0f, 0.0f, 0.25f);

        private static readonly Color defaultWireframeActive = new Color(
            125.0f / 255.0f, 176.0f / 255.0f, 250.0f / 255.0f, 95.0f / 255.0f
        );

        private static readonly Color defaultWireframeSelected = new Color(
            94.0f / 255.0f, 119.0f / 255.0f, 155.0f / 255.0f, 0.25f
        );

        private static readonly Color defaultXAxis = new Color(154f / 255, 243f / 255, 72f / 255, .93f);
        private static readonly Color defaultYAxis = new Color(154f / 255, 243f / 255, 72f / 255, .93f);
        private static readonly Color defaultZAxis = new Color(154f / 255, 243f / 255, 72f / 255, .93f);

        private static Color CopyHueSaturation(Color src1, Color src2)
        {
            Color.RGBToHSV(src1, out float _, out float s1, out float _);
            Color.RGBToHSV(src2, out float h2, out float s2, out float v2);

            Color dstColor = Color.HSVToRGB(h2, (s1 + s2) * 0.5f, v2);
            dstColor.a = src1.a;

            return dstColor;
        }

        private static Color CopyValue(Color src1, Color src2)
        {
            Color.RGBToHSV(src1, out float _, out float _, out float v1);
            Color.RGBToHSV(src2, out float h2, out float s2, out float _);

            Color dstColor = Color.HSVToRGB(h2, s2, v1);
            dstColor.a = src2.a;

            return dstColor;
        }

        private static Color InvertHue(Color src)
        {
            Color.RGBToHSV(src, out float h1, out float s1, out float v1);

            Color dstColor = Color.HSVToRGB(1 - h1, s1, v1);
            dstColor.a = src.a;

            return dstColor;
        }

        private static string m_TextBackground;

        private static string m_TextGuideline;
        private static string m_TextGrid;

        private static string m_TextCenterAxis;
        private static string m_TextSelectedAxis;
        private static string m_TextSelectionOutline;

        private static string m_TextWireframe;
        private static string m_TextWireframeOverlay;
        private static string m_TextWireframeActive;
        private static string m_TextWireframeSelected;

        private static string m_TextXAxis;
        private static string m_TextYAxis;
        private static string m_TextZAxis;

        private static Color m_ParsedBackground = Color.black;

        private static Color m_ParsedGuideline = Color.black;
        private static Color m_ParsedGrid = Color.black;

        private static Color m_ParsedCenterAxis = Color.black;
        private static Color m_ParsedSelectedAxis = Color.black;
        private static Color m_ParsedSelectionOutline = Color.black;

        private static Color m_ParsedWireframe = Color.black;
        private static Color m_ParsedWireframeOverlay = Color.black;
        private static Color m_ParsedWireframeActive = Color.black;
        private static Color m_ParsedWireframeSelected = Color.black;

        private static Color m_ParsedXAxis = Color.black;
        private static Color m_ParsedYAxis = Color.black;
        private static Color m_ParsedZAxis = Color.black;

        private static Shader m_SceneViewShader;

        public static Shader GetWireframeShader()
        {
            if(!m_SceneViewShader)
            {
                Update();
            }

            return m_SceneViewShader;
        }

        public static Color Brighten(Color color, float brightScale)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);

            v *= brightScale;

            Color outColor = Color.HSVToRGB(h, s, v);
            outColor.a = color.a;

            return outColor;
        }

        public static bool IsInitialized;

        public static void Update()
        {
            IsInitialized = true;

            if(!m_SceneViewShader)
            {
                m_SceneViewShader = Shader.Find("Hidden/CSG/internal/Background");
            }

            bool modified = false;

            modified = ParseColor("Scene/Background", ref m_TextBackground, defaultBackground, ref m_ParsedBackground)
             || modified;

            modified = ParseColor("Scene/Guide Line", ref m_TextGuideline, defaultGuideline, ref m_ParsedGuideline)
             || modified;

            modified = ParseColor("Scene/Grid", ref m_TextGrid, defaultGrid, ref m_ParsedGrid) || modified;

            modified = ParseColor("Scene/Center Axis", ref m_TextCenterAxis, defaultCenterAxis, ref m_ParsedCenterAxis)
             || modified;

            modified = ParseColor(
                "Scene/Selected Axis", ref m_TextSelectedAxis, defaultSelectedAxis, ref m_ParsedSelectedAxis
            ) || modified;

            modified = ParseColor(
                "Scene/Selection Outline", ref m_TextSelectionOutline, defaultSelectionOutline
              , ref m_ParsedSelectionOutline
            ) || modified;

            modified = ParseColor("Scene/Wireframe", ref m_TextWireframe, defaultWireframe, ref m_ParsedWireframe)
             || modified;

            modified = ParseColor(
                "Scene/Wireframe Overlay", ref m_TextWireframeOverlay, defaultWireframeOverlay
              , ref m_ParsedWireframeOverlay
            ) || modified;

            modified = ParseColor(
                "Scene/Wireframe Active", ref m_TextWireframeActive, defaultWireframeActive, ref m_ParsedWireframeActive
            ) || modified;

            modified = ParseColor(
                "Scene/Wireframe Selected", ref m_TextWireframeSelected, defaultWireframeSelected
              , ref m_ParsedWireframeSelected
            ) || modified;

            modified = ParseColor("Scene/X Axis", ref m_TextXAxis, defaultXAxis, ref m_ParsedXAxis) || modified;
            modified = ParseColor("Scene/Z Axis", ref m_TextZAxis, defaultZAxis, ref m_ParsedZAxis) || modified;
            modified = ParseColor("Scene/Y Axis", ref m_TextYAxis, defaultYAxis, ref m_ParsedYAxis) || modified;

            if(!modified)
            {
                return;
            }

            m_Background   = m_ParsedBackground;
            m_Background.a = 1.0f;

            m_WireframeOutline = m_ParsedWireframeActive;

            m_ClipPlaneOutline     =  m_ParsedWireframe;
            m_ClipPlaneFill        =  m_ParsedWireframeOverlay;
            m_ClipPlaneFill.a      *= 0.5f;
            m_ClipLeftOverOutlines =  m_ParsedWireframe;

            m_RotateCirclePieOutline = m_ParsedWireframe;
            m_RotateCirclePieFill    = m_ParsedWireframeOverlay;
            m_RotateCircleOutline    = m_ParsedWireframeActive;
            m_RotateCircleOutline.a  = Mathf.Clamp01(m_RotateCircleOutline.a * 2.0f);
            m_RotateCircleHatches    = m_ParsedCenterAxis;
            m_RotateCircleHatches.a  = Mathf.Clamp01(m_RotateCircleHatches.a * 2.0f);

            m_ShapeDrawingFill = m_ParsedWireframeOverlay;

            Color preSelectionColor = new Color(201f / 255, 200f / 255f, 144f / 255f, 0.89f);

            m_SelectedOutlines   = m_ParsedSelectionOutline;
            m_SelectedOutlines.a = Mathf.Max(0.5f, Mathf.Clamp01(m_SelectedOutlines.a * 3.0f));
            m_BoundsOutlines     = m_ParsedWireframeActive;
            m_BoundsOutlines.a   = Mathf.Clamp01(m_BoundsOutlines.a * 2.5f);
            m_BoundsEdgeHover    = m_ParsedSelectedAxis;
            m_BoundsEdgeHover.a  = Mathf.Clamp01(m_BoundsEdgeHover.a * 2.5f);
            m_SnappedEdges       = m_ParsedCenterAxis;
            m_SnappedEdges.a     = Mathf.Clamp01(m_SnappedEdges.a * 2.5f);

            m_MeshEdgeOutline   = m_ParsedBackground;
            m_MeshEdgeOutline.a = Mathf.Max(0.9f, m_MeshEdgeOutline.a);

            m_GuideLine = m_ParsedGuideline;

            pointInnerStateColor[0] = m_ParsedWireframeSelected; // None
            pointInnerStateColor[1] = preSelectionColor;         // Hovering
            pointInnerStateColor[2] = m_SelectedOutlines;        // Selected
            pointInnerStateColor[3] = m_BoundsEdgeHover;         // Hovering + Selected

            alignedCurveStateColor[0] = pointInnerStateColor[0];
            alignedCurveStateColor[1] = pointInnerStateColor[1];
            alignedCurveStateColor[2] = pointInnerStateColor[2];
            alignedCurveStateColor[3] = pointInnerStateColor[3];

            brokenCurveStateColor[0] = pointInnerStateColor[0];
            brokenCurveStateColor[1] = pointInnerStateColor[1];
            brokenCurveStateColor[2] = pointInnerStateColor[2];
            brokenCurveStateColor[3] = pointInnerStateColor[3];

            for(int i = 0; i < 4; i++)
            {
                alignedCurveStateColor[i].a = 1.0f;
                brokenCurveStateColor[i].a  = 1.0f;
                pointInnerStateColor[i].a   = 1.0f;

                invalidInnerStateColor[i] = CopyHueSaturation(pointInnerStateColor[i], innerInvalidColor);
                alignedCurveStateColor[i] = CopyHueSaturation(pointInnerStateColor[i], m_ParsedYAxis);
                brokenCurveStateColor[i]  = CopyHueSaturation(pointInnerStateColor[i], m_ParsedXAxis);
            }

            surfaceTriangleStateColor[0] =  m_ParsedWireframeOverlay;
            m_ParsedWireframeOverlay.a   *= 0.5f;
            surfaceTriangleStateColor[1] =  m_ParsedWireframeOverlay;
            m_ParsedWireframeOverlay.a   *= 0.25f;
            surfaceTriangleStateColor[2] =  m_ParsedWireframeOverlay;
            m_ParsedWireframeOverlay.a   *= 0.35f;
            surfaceTriangleStateColor[3] =  m_ParsedWireframeOverlay;
            m_ParsedWireframeOverlay.a   *= 0.25f;

            surfaceInnerStateColor[0]   = pointInnerStateColor[0] * 0.75f;
            surfaceInnerStateColor[1]   = pointInnerStateColor[1] * 0.75f;
            surfaceInnerStateColor[2]   = pointInnerStateColor[2] * 0.75f;
            surfaceInnerStateColor[3]   = pointInnerStateColor[3] * 0.75f;
            surfaceInnerStateColor[0].a = 0.5f;
            surfaceInnerStateColor[1].a = 0.5f;
            surfaceOuterStateColor[2].a = 1.0f;
            surfaceOuterStateColor[3].a = 1.0f;

            surfaceOuterStateColor[0]   = pointInnerStateColor[0];
            surfaceOuterStateColor[1]   = pointInnerStateColor[1];
            surfaceOuterStateColor[2]   = pointInnerStateColor[2];
            surfaceOuterStateColor[3]   = pointInnerStateColor[3];
            surfaceOuterStateColor[0].a = 0.5f;
            surfaceOuterStateColor[1].a = 0.5f;
            surfaceOuterStateColor[2].a = 1.0f;
            surfaceOuterStateColor[3].a = 1.0f;

            polygonInnerStateColor[0] = CopyHueSaturation(pointInnerStateColor[0], m_ParsedYAxis);
            polygonInnerStateColor[1] = CopyHueSaturation(pointInnerStateColor[1], m_ParsedYAxis);
            polygonInnerStateColor[2] = CopyHueSaturation(pointInnerStateColor[2], m_ParsedYAxis);
            polygonInnerStateColor[3] = CopyHueSaturation(pointInnerStateColor[3], m_ParsedYAxis);

            for(int i = 0; i < 4; i++)
            {
                polygonInnerStateColor[i].a = Mathf.Clamp01(polygonInnerStateColor[i].a * 2.5f);
            }

            Color invertedColor1 = InvertHue(m_ParsedWireframeActive);
            Color invertedColor2 = InvertHue(m_ParsedCenterAxis);

            m_MinOutlines2D = CopyValue(pointInnerStateColor[0], invertedColor1);
            m_MaxOutlines2D = CopyValue(pointInnerStateColor[0], invertedColor2);

            m_Outlines2D = new Color[13];
            Random.State cache = Random.state; //cache current state to avoid throwing off global rng.

            try
            {
                Random.InitState(0);

                for(int i = 0; i < m_Outlines2D.Length; i++)
                {
                    Color color = new Color(
                        Mathf.Lerp(m_MinOutlines2D.r, m_MaxOutlines2D.r, Random.value),
                        Mathf.Lerp(m_MinOutlines2D.g, m_MaxOutlines2D.g, Random.value),
                        Mathf.Lerp(m_MinOutlines2D.b, m_MaxOutlines2D.b, Random.value),
                        1.0f
                    );

                    Color.RGBToHSV(color, out float h, out float s, out float v);

                    s *= 0.5f;
                    v *= 1.1f;

                    m_Outlines2D[i] = Color.HSVToRGB(h, s, v);
                }
            }
            finally
            {
                Random.state = cache;
            }

            GridColorW = m_ParsedCenterAxis;

            m_ParsedGrid.a = Mathf.Max(0.85f, m_ParsedGrid.a);

            GridColor1W = m_ParsedGrid;
            GridColor2W = m_ParsedGuideline;
        }
    }
}

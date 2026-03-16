using System.Globalization;
using UnityEditor;
using UnityEngine;
using RealtimeCSG.Components;

namespace RealtimeCSG
{
    internal static class ColorSettings
    {
        private static readonly float state0 = (float)Mathf.Pow(1.00f, 2.2f);
        private static readonly float state1 = (float)Mathf.Pow(0.90f, 2.2f);
        private static readonly float state2 = (float)Mathf.Pow(0.60f, 2.2f);
        private static readonly float state3 = (float)Mathf.Pow(0.70f, 2.2f);

        private static Color[] pointInnerStateColor =
        {
            new Color(0.80f, state3, 0.00f), // None
            new Color(1.00f, state2, 0.00f)
          , // Hovering
            new Color(1.0f, state1, 0.00f)
,                                            // Selected
            new Color(state0, state0, 0.00f) // Selected | Hovering
        };

        public static Color[] PointInnerStateColor
        {
            get => pointInnerStateColor;
        }

        private static Color[] alignedCurveStateColor =
        {
            new Color(0.80f, state3, 0.00f), // None
            new Color(1.00f, state2, 0.00f)
          , // Hovering
            new Color(1.0f, state1, 0.00f)
,                                            // Selected
            new Color(state0, state0, 0.00f) // Selected | Hovering
        };

        public static Color[] AlignedCurveStateColor
        {
            get => alignedCurveStateColor;
        }

        private static Color[] brokenCurveStateColor =
        {
            new Color(0.80f, state3, 0.00f), // None
            new Color(1.00f, state2, 0.00f)
          , // Hovering
            new Color(1.0f, state1, 0.00f)
,                                            // Selected
            new Color(state0, state0, 0.00f) // Selected | Hovering
        };

        public static Color[] BrokenCurveStateColor
        {
            get => brokenCurveStateColor;
        }

        private static Color[] polygonInnerStateColor =
        {
            new Color(state3, 0.80f, state3), // None
            new Color(state1, 1.00f, state1)
          , // Hovering
            new Color(state1, state1, 0.0f)
,                                           // Selected
            new Color(state0, state0, 0.0f) // Selected | Hovering
        };

        public static Color[] PolygonInnerStateColor
        {
            get => polygonInnerStateColor;
        }

        public static Color[] surfaceInnerStateColor =
        {
            new Color(state3, state3, 0.80f), // None
            new Color(state2, state2, 1.00f)
          , // Hovering
            new Color(state1, state1, 1.00f)
,                                            // Selected
            new Color(state0, state0, 1.00f) // Selected | Hovering
        };

        public static Color[] SurfaceInnerStateColor
        {
            get => surfaceInnerStateColor;
        }

        public static Color[] surfaceOuterStateColor =
        {
            new Color(state3, state3, 0.80f), // None
            new Color(state2, state2, 1.00f)
          , // Hovering
            new Color(state1, state1, 1.00f)
,                                            // Selected
            new Color(state0, state0, 1.00f) // Selected | Hovering
        };

        public static Color[] SurfaceOuterStateColor
        {
            get => surfaceOuterStateColor;
        }

        public static Color[] surfaceTriangleStateColor =
        {
            new Color(state3, state3, 0.80f, 0.125f), // None
            new Color(state2, state2, 1.00f, 0.125f)
          , // Hovering
            new Color(state1, state1, 1.00f, 0.125f)
,                                                    // Selected
            new Color(state0, state0, 1.00f, 0.125f) // Selected | Hovering
        };

        public static Color[] SurfaceTriangleStateColor
        {
            get => surfaceTriangleStateColor;
        }

        private static Color[] invalidInnerStateColor =
        {
            new Color(state3, 0.0f, 0.0f), // None
            new Color(state2, 0.0f, 0.0f)
          , // Hovering
            new Color(state1, 0.0f, 0.0f)
,                                         // Selected
            new Color(state0, 0.0f, 0.0f) // Selected | Hovering
        };

        public static Color[] InvalidInnerStateColor
        {
            get => invalidInnerStateColor;
        }

//		public static readonly Color backgroundColor = new Color(19.0f / 255.0f, 22.0f / 255.0f, 28.0f / 255.0f, 1.0f);

        public static Color gridColorW = new Color(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);

        private const float large = 0.95f;
        private const float small = 0.35f;

        public static Color gridColor1W = new Color(
            large * gridColorW.r, large * gridColorW.g, large * gridColorW.b, 1.00f * gridColorW.a
        );

        public static Color gridColor2W = new Color(
            small * gridColorW.r, small * gridColorW.g, small * gridColorW.b, 0.65f * gridColorW.a
        );

        public static readonly Color outerInvalidColor = Color.black;
        public static readonly Color innerInvalidColor = Color.red;

        private static Color selectedOutlines = new Color(1.0f, 0.4f, 0, 1.0f);

        public static Color SelectedOutlines
        {
            get => selectedOutlines;
        }

        private static Color hoverOutlines = new Color(1.0f, 1.0f, 0, 1.0f);

        public static Color HoverOutlines
        {
            get => hoverOutlines;
        }

        private static Color wireframeOutline = new Color(0.0f, 0.0f, 0.0f, 64.0f / 255.0f);

        public static Color WireframeOutline
        {
            get => wireframeOutline;
        }

        private static Color meshEdgeOutline = new Color(0.0f, 0.0f, 0.0f, 1.0f);

        public static Color MeshEdgeOutline
        {
            get => meshEdgeOutline;
        }

        private static Color boundsOutlines = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        public static Color BoundsOutlines
        {
            get => boundsOutlines;
        }

        private static Color boundsEdgeHover = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        public static Color BoundsEdgeHover
        {
            get => boundsEdgeHover;
        }

        private static Color shapeDrawingFill = new Color(0.0f, 0.8f, 1, 1.00f);

        public static Color ShapeDrawingFill
        {
            get => shapeDrawingFill;
        }

        private static Color rotateCirclePieOutline = new Color(0.0f, 0.2f, 1, 1.00f);

        public static Color RotateCirclePieOutline
        {
            get => rotateCirclePieOutline;
        }

        private static Color rotateCirclePieFill = new Color(0.0f, 0.2f, 1, 0.1f);

        public static Color RotateCirclePieFill
        {
            get => rotateCirclePieFill;
        }

        private static Color rotateCircleOutline = new Color(0.0f, 0.8f, 1, 1.00f);

        public static Color RotateCircleOutline
        {
            get => rotateCircleOutline;
        }

        private static Color rotateCircleHatches = new Color(1.0f, 1.0f, 1, 1.00f);

        public static Color RotateCircleHatches
        {
            get => rotateCircleHatches;
        }

        private static Color clipPlaneOutline = new Color(0.0f, 0.2f, 1, 1.00f);

        public static Color ClipPlaneOutline
        {
            get => clipPlaneOutline;
        }

        private static Color clipPlaneFill = new Color(0.0f, 0.2f, 1, 0.1f);

        public static Color ClipPlaneFill
        {
            get => clipPlaneFill;
        }

        private static Color clipLeftOverOutlines = new Color(0.0f, 0.2f, 1, 1.0f);

        public static Color ClipLeftOverOutlines
        {
            get => clipLeftOverOutlines;
        }

        private static Color shadowColor = new Color(0.0f, 0.0f, 0.0f, 0.1f);

        public static Color ShadowColor
        {
            get => shadowColor;
        }

        private static Color shadowOutlineColor = new Color(0.0f, 0.0f, 0.0f, 0.5f);

        public static Color ShadowOutlineColor
        {
            get => shadowOutlineColor;
        }

        private static Color snappedEdges = new Color(
            125.0f / 255.0f, 176.0f / 255.0f, 250.0f / 255.0f, 95.0f / 255.0f
        );

        public static Color SnappedEdges
        {
            get => snappedEdges;
        }

        private static Color guideLine = new Color(125.0f / 255.0f, 176.0f / 255.0f, 250.0f / 255.0f, 95.0f / 255.0f);

        public static Color GuideLine
        {
            get => guideLine;
        }

        private static Color background = new Color(0.278431f, 0.278431f, 0.278431f, 0);

        public static Color Background
        {
            get => background;
        }

        public static Color XAxis = Color.black;
        public static Color YAxis = Color.black;
        public static Color ZAxis = Color.black;

        private static Color minOutlines2D = new Color(0.3f, 0.00f, 0.55f, 1.0f);
        private static Color maxOutlines2D = new Color(0.0f, 0.80f, 0.60f, 1.0f);
        private static Color[] outlines2D;

        public static Color GetBrushOutlineColor(CSGBrush brush)
        {
            if(outlines2D == null)
            {
                Update();
            }

            int instanceID = Mathf.Abs(brush.GetInstanceID());

            return outlines2D[instanceID % outlines2D.Length];
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

            bool  success;
            float r, g, b, a;
            success =  float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out r);
            success &= float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out g);
            success &= float.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out b);
            success &= float.TryParse(split[4], NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out a);

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
            float h1, s1, v1;
            Color.RGBToHSV(src1, out h1, out s1, out v1);

            float h2, s2, v2;
            Color.RGBToHSV(src2, out h2, out s2, out v2);

            Color dstColor = Color.HSVToRGB(h2, (s1 + s2) * 0.5f, v2);
            dstColor.a = src1.a; // (src1.a + src2.a) * 0.5f;

            return dstColor;
        }

        private static Color CopyValue(Color src1, Color src2)
        {
            float h1, s1, v1;
            Color.RGBToHSV(src1, out h1, out s1, out v1);

            float h2, s2, v2;
            Color.RGBToHSV(src2, out h2, out s2, out v2);

            Color dstColor = Color.HSVToRGB(h2, s2, v1);
            dstColor.a = src2.a; // (src1.a + src2.a) * 0.5f;

            return dstColor;
        }

        private static Color InvertHue(Color src)
        {
            float h1, s1, v1;
            Color.RGBToHSV(src, out h1, out s1, out v1);

            Color dstColor = Color.HSVToRGB(1 - h1, s1, v1);
            dstColor.a = src.a;

            return dstColor;
        }

        private static string textBackground;

        private static string textGuideline;
        private static string textGrid;

        private static string textCenterAxis;
        private static string textSelectedAxis;
        private static string textSelectionOutline;

        private static string textWireframe;
        private static string textWireframeOverlay;
        private static string textWireframeActive;
        private static string textWireframeSelected;

        private static string textXAxis;
        private static string textYAxis;
        private static string textZAxis;

        private static Color parsedBackground = Color.black;

        private static Color parsedGuideline = Color.black;
        private static Color parsedGrid = Color.black;

        private static Color parsedCenterAxis = Color.black;
        private static Color parsedSelectedAxis = Color.black;
        private static Color parsedSelectionOutline = Color.black;

        private static Color parsedWireframe = Color.black;
        private static Color parsedWireframeOverlay = Color.black;
        private static Color parsedWireframeActive = Color.black;
        private static Color parsedWireframeSelected = Color.black;

        private static Color parsedXAxis = Color.black;
        private static Color parsedYAxis = Color.black;
        private static Color parsedZAxis = Color.black;

        private static Shader sceneViewShader;

        public static Shader GetWireframeShader()
        {
            if(!sceneViewShader)
            {
                Update();
            }

            return sceneViewShader;
        }

        public static Color Brighten(Color color, float brightScale)
        {
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);

            v *= brightScale;

            Color outColor = Color.HSVToRGB(h, s, v);
            outColor.a = color.a;

            return outColor;
        }

        public static bool isInitialized = false;

        public static void Update()
        {
            isInitialized = true;

            if(!sceneViewShader)
            {
                sceneViewShader = Shader.Find("Hidden/CSG/internal/Background");
            }

            bool modified = false;

            modified = ParseColor("Scene/Background", ref textBackground, defaultBackground, ref parsedBackground)
             || modified;

            modified = ParseColor("Scene/Guide Line", ref textGuideline, defaultGuideline, ref parsedGuideline)
             || modified;

            modified = ParseColor("Scene/Grid", ref textGrid, defaultGrid, ref parsedGrid) || modified;

            modified = ParseColor("Scene/Center Axis", ref textCenterAxis, defaultCenterAxis, ref parsedCenterAxis)
             || modified;

            modified = ParseColor(
                "Scene/Selected Axis", ref textSelectedAxis, defaultSelectedAxis, ref parsedSelectedAxis
            ) || modified;

            modified = ParseColor(
                "Scene/Selection Outline", ref textSelectionOutline, defaultSelectionOutline, ref parsedSelectionOutline
            ) || modified;

            modified = ParseColor("Scene/Wireframe", ref textWireframe, defaultWireframe, ref parsedWireframe)
             || modified;

            modified = ParseColor(
                "Scene/Wireframe Overlay", ref textWireframeOverlay, defaultWireframeOverlay, ref parsedWireframeOverlay
            ) || modified;

            modified = ParseColor(
                "Scene/Wireframe Active", ref textWireframeActive, defaultWireframeActive, ref parsedWireframeActive
            ) || modified;

            modified = ParseColor(
                "Scene/Wireframe Selected", ref textWireframeSelected, defaultWireframeSelected
              , ref parsedWireframeSelected
            ) || modified;

            modified = ParseColor("Scene/X Axis", ref textXAxis, defaultXAxis, ref parsedXAxis) || modified;
            modified = ParseColor("Scene/Z Axis", ref textZAxis, defaultZAxis, ref parsedZAxis) || modified;
            modified = ParseColor("Scene/Y Axis", ref textYAxis, defaultYAxis, ref parsedYAxis) || modified;

            if(!modified)
            {
                return;
            }

            background   = parsedBackground;
            background.a = 1.0f;

            wireframeOutline = parsedWireframeActive;

            clipPlaneOutline     =  parsedWireframe;
            clipPlaneFill        =  parsedWireframeOverlay;
            clipPlaneFill.a      *= 0.5f;
            clipLeftOverOutlines =  parsedWireframe;

            rotateCirclePieOutline = parsedWireframe;
            rotateCirclePieFill    = parsedWireframeOverlay;
            rotateCircleOutline    = parsedWireframeActive;
            rotateCircleOutline.a  = Mathf.Clamp01(rotateCircleOutline.a * 2.0f);
            rotateCircleHatches    = parsedCenterAxis;
            rotateCircleHatches.a  = Mathf.Clamp01(rotateCircleHatches.a * 2.0f);

            shapeDrawingFill = parsedWireframeOverlay;

            Color preSelectionColor = new Color(
                201f / 255, 200f / 255f, 144f / 255f, 0.89f
            ); // Handles.preselectionColor;

            selectedOutlines   = parsedSelectionOutline;
            selectedOutlines.a = Mathf.Max(0.5f, Mathf.Clamp01(selectedOutlines.a * 3.0f));
            boundsOutlines     = parsedWireframeActive;
            boundsOutlines.a   = Mathf.Clamp01(boundsOutlines.a * 2.5f);
            boundsEdgeHover    = parsedSelectedAxis;
            boundsEdgeHover.a  = Mathf.Clamp01(boundsEdgeHover.a * 2.5f);
            snappedEdges       = parsedCenterAxis;
            snappedEdges.a     = Mathf.Clamp01(snappedEdges.a * 2.5f);

            meshEdgeOutline   = parsedBackground;
            meshEdgeOutline.a = Mathf.Max(0.9f, meshEdgeOutline.a);

            guideLine = parsedGuideline;

            XAxis = parsedXAxis;
            YAxis = parsedYAxis;
            ZAxis = parsedZAxis;

            pointInnerStateColor[0] = parsedWireframeSelected; // None
            pointInnerStateColor[1] = preSelectionColor;       // Hovering
            pointInnerStateColor[2] = selectedOutlines;        // Selected
            pointInnerStateColor[3] = boundsEdgeHover;         // Hovering + Selected

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
                alignedCurveStateColor[i] = CopyHueSaturation(pointInnerStateColor[i], parsedYAxis);
                brokenCurveStateColor[i]  = CopyHueSaturation(pointInnerStateColor[i], parsedXAxis);
            }

            surfaceTriangleStateColor[0] =  parsedWireframeOverlay;
            parsedWireframeOverlay.a     *= 0.5f;
            surfaceTriangleStateColor[1] =  parsedWireframeOverlay;
            parsedWireframeOverlay.a     *= 0.25f;
            surfaceTriangleStateColor[2] =  parsedWireframeOverlay;
            parsedWireframeOverlay.a     *= 0.35f;
            surfaceTriangleStateColor[3] =  parsedWireframeOverlay;
            parsedWireframeOverlay.a     *= 0.25f;

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

            polygonInnerStateColor[0] = CopyHueSaturation(pointInnerStateColor[0], parsedYAxis);
            polygonInnerStateColor[1] = CopyHueSaturation(pointInnerStateColor[1], parsedYAxis);
            polygonInnerStateColor[2] = CopyHueSaturation(pointInnerStateColor[2], parsedYAxis);
            polygonInnerStateColor[3] = CopyHueSaturation(pointInnerStateColor[3], parsedYAxis);

            for(int i = 0; i < 4; i++)
            {
                polygonInnerStateColor[i].a = Mathf.Clamp01(polygonInnerStateColor[i].a * 2.5f);
            }

            Color invertedColor1 = InvertHue(parsedWireframeActive);
            Color invertedColor2 = InvertHue(parsedCenterAxis);

            minOutlines2D = CopyValue(pointInnerStateColor[0], invertedColor1);
            maxOutlines2D = CopyValue(pointInnerStateColor[0], invertedColor2);

            outlines2D = new Color[13];
            Random.State cache = Random.state; //cache current state to avoid throwing off global rng.

            try
            {
                Random.InitState(0);

                for(int i = 0; i < outlines2D.Length; i++)
                {
                    Color color = new Color(
                        Mathf.Lerp(minOutlines2D.r, maxOutlines2D.r, Random.value),
                        Mathf.Lerp(minOutlines2D.g, maxOutlines2D.g, Random.value),
                        Mathf.Lerp(minOutlines2D.b, maxOutlines2D.b, Random.value),
                        1.0f
                    );

                    float h, s, v;
                    Color.RGBToHSV(color, out h, out s, out v);

                    s *= 0.5f;
                    v *= 1.1f;

                    outlines2D[i] = Color.HSVToRGB(h, s, v);
                }
            }
            finally
            {
                Random.state = cache;
            }

            gridColorW = parsedCenterAxis;

            parsedGrid.a = Mathf.Max(0.85f, parsedGrid.a);

            gridColor1W = parsedGrid;
            gridColor2W = parsedGuideline;
        }
    }
}

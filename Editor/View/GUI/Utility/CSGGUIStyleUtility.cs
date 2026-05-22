// * * * * * * * * * * * * * * * * * * * * * *
//  Description:
//
// * * * * * * * * * * * * * * * * * * * * * *

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal static class CsgguiStyleUtility
    {
        internal const int OPERATION_TYPE_COUNT = 3;
        internal const int SHAPE_MODE_COUNT = (int)ShapeMode.Last + 1;
        internal const int CLIP_TYPE_COUNT = 3;

        private static bool m_StylesInitialized;

        public static string[] BrushEditModeNames;
        public static GUIContent[] BrushEditModeContent;
        public static ToolTip[] BrushEditModeTooltips;
        public static ToolEditMode[] BrushEditModeValues;

        public const float BOTTOM_TOOL_BAR_HEIGHT = 17;
        public static GUIStyle BottomToolBarStyle;

        internal const float SINGLE_LINE_HEIGHT = 16;
        public static GUIStyle EmptyMaterialStyle;
        public static GUIStyle UnselectedIconLabelStyle;
        public static GUIStyle SelectedIconLabelStyle;

        public static GUIStyle SelectionRectStyle;
        public static GUIStyle RedTextArea;
        public static GUIStyle RedTextLabel;
        public static GUIStyle RedButton;
        public static GUIStyle WrapLabel;

        public static GUIStyle VersionLabelStyle;

        public static GUIStyle ToolTipTitleStyle;
        public static GUIStyle ToolTipContentsStyle;
        public static GUIStyle ToolTipKeycodesStyle;

        public static GUIStyle SceneTextLabel;

        public static GUIStyle RightAlignedLabel;

        public static GUIStyle UnpaddedWindow;

        public static readonly GUILayoutOption[] contentEmpty = Array.Empty<GUILayoutOption>();

        public static readonly ToolTip popOutTooltip = new ToolTip(
            "Pop out tool window",
            "Click this to turn this into a floating tool window.\n" +
            "Close that tool window to get the in scene tool window back"
          , new KeyEvent(KeyCode.F2, EventModifiers.Control)
        );

        private const string CSG_ADDITION_TOOLTIP = "Addition";
        private const string CSG_SUBTRACTION_TOOLTIP = "Subtraction";
        private const string CSG_INTERSECTION_TOOLTIP = "Intersection";

        private static readonly GUIContent[] proInActiveOperationTypes = { IconContent("icon_pro_csg_addition_small"), IconContent("icon_pro_csg_subtraction_small"), IconContent("icon_pro_csg_intersection_small") };

        private static readonly GUIContent[] proActiveOperationTypes = { IconContent("icon_pro_csg_addition_small_on"), IconContent("icon_pro_csg_subtraction_small_on"), IconContent("icon_pro_csg_intersection_small_on") };

        private static readonly GUIContent[] personalInActiveOperationTypes = { IconContent("icon_pers_csg_addition_small"), IconContent("icon_pers_csg_subtraction_small"), IconContent("icon_pers_csg_intersection_small") };

        private static readonly GUIContent[] personalActiveOperationTypes = { IconContent("icon_pers_csg_addition_small_on"), IconContent("icon_pers_csg_subtraction_small_on"), IconContent("icon_pers_csg_intersection_small_on") };

        private static readonly string[] operationText = { " Addition", " Subtraction", " Intersection" };

        public static readonly ToolTip[] operationTooltip =
        {
            new ToolTip("Additive CSG Operation", "Set the selection to be additive", Keys.makeSelectedAdditiveKey), new ToolTip("Subtractive CSG operation", "Set the selection to be subtractive", Keys.makeSelectedSubtractiveKey), new ToolTip(
                "Intersecting CSG operation", "Set the selection to be an intersection operation"
              , Keys.makeSelectedIntersectingKey
            )
        };

        private const string CSG_PASSTHROUGH_TOOLTIP = "PassThrough|No CSG operation";

        private static readonly GUIContent proPassThrough = IconContent("icon_pro_pass_through");
        private static readonly GUIContent proPassThroughOn = IconContent("icon_pro_pass_through_on");
        private static readonly GUIContent personalPassThrough = IconContent("icon_pers_pass_through");
        private static readonly GUIContent personalPassThroughOn = IconContent("icon_pers_pass_through_on");

        private static readonly string passThroughText = " Pass through";

        public static readonly ToolTip passThroughTooltip = new ToolTip(
            "Perform no CSG operation"
          , "No operation is performed. Child nodes act as if there is no operation above it. This is useful to group different kinds of nodes with."
          , Keys.makeSelectedPassThroughKey
        );

        private static readonly GUIContent proWireframe = IconContent("icon_pro_wireframe");
        private static readonly GUIContent proWireframeOn = IconContent("icon_pro_wireframe_on");
        private static readonly GUIContent personalWireframe = IconContent("icon_pers_wireframe");
        private static readonly GUIContent personalWireframeOn = IconContent("icon_pers_wireframe_on");

        private static readonly string wireframeTooltip = "Show/Hide brush wireframe";

        private static readonly GUIContent[] proInActiveShapeModes = { GUIContent.none, GUIContent.none, GUIContent.none, GUIContent.none, GUIContent.none };
        private static readonly GUIContent[] proActiveShapeModes = { GUIContent.none, GUIContent.none, GUIContent.none, GUIContent.none, GUIContent.none };
        private static readonly GUIContent[] personalInActiveShapeModes = { GUIContent.none, GUIContent.none, GUIContent.none, GUIContent.none, GUIContent.none };
        private static readonly GUIContent[] personalActiveShapeModes = { GUIContent.none, GUIContent.none, GUIContent.none, GUIContent.none, GUIContent.none };

        private static readonly string[] shapeModeText = { "Free-draw", "Box", "Sphere", "Cylinder", "Linear Stairs" };

        public static readonly ToolTip[] shapeModeTooltips =
        {
            new ToolTip(
                "Free-draw brush"
              , "Use this to draw a 2D shape and extrude it,\noptionally with curves by double clicking on edges."
              , Keys.freeBuilderMode
            )
          , new ToolTip("Create Box brush", "Use this to create boxes", Keys.boxBuilderMode), new ToolTip("Create (Hemi)Sphere brush", "Use this to create (hemi)spheres", Keys.sphereBuilderMode), new ToolTip("Create Cylinder brush", "Use this to create cylinders", Keys.cylinderBuilderMode), new ToolTip("Create Linear Stairs", "Use this to create linear stairs", Keys.linearStairsBuilderMode)
        };

        private static readonly GUIContent[] proInActiveClipTypes = { IconContent("icon_pro_remove_front"), IconContent("icon_pro_remove_behind"), IconContent("icon_pro_split") };
        private static readonly GUIContent[] proActiveClipTypes = { IconContent("icon_pro_remove_front_on"), IconContent("icon_pro_remove_behind_on"), IconContent("icon_pro_split_on") };
        private static readonly GUIContent[] personalInActiveClipTypes = { IconContent("icon_pers_remove_front"), IconContent("icon_pers_remove_behind"), IconContent("icon_pers_split") };
        private static readonly GUIContent[] personalActiveClipTypes = { IconContent("icon_pers_remove_front_on"), IconContent("icon_pers_remove_behind_on"), IconContent("icon_pers_split_on") };

        private static readonly string[] clipText = { " Remove in front", " Remove behind", " Split" };

        public static readonly ToolTip[] clipTooltips =
        {
            new ToolTip(
                "Remove in front"
              , "Remove the area in front of the created clipping plane from the selected brushes"
            )
          , new ToolTip("Remove behind", "Remove the area behind the created clipping plane from the selected brushes"), new ToolTip("Split", "Split the selected brushes with the created splitting plane")
        };

        private static readonly GUIContent proRebuildIcon = IconContent("icon_pro_rebuild");
        private static readonly GUIContent proGridIcon = IconContent("icon_pro_grid");
        private static readonly GUIContent proGridIconOn = IconContent("icon_pro_grid_on");
        private static readonly GUIContent proGridSnapIcon = IconContent("icon_pro_gridsnap");
        private static readonly GUIContent proGridSnapIconOn = IconContent("icon_pro_gridsnap_on");
        private static readonly GUIContent proRelSnapIcon = IconContent("icon_pro_relsnap");
        private static readonly GUIContent proRelSnapIconOn = IconContent("icon_pro_relsnap_on");
        private static readonly GUIContent proNoSnapIcon = IconContent("icon_pro_nosnap");
        private static readonly GUIContent proNoSnapIconOn = IconContent("icon_pro_nosnap_on");

        private static readonly GUIContent personalRebuildIcon = IconContent("icon_pers_rebuild");
        private static readonly GUIContent personalGridIcon = IconContent("icon_pers_grid");
        private static readonly GUIContent personalGridIconOn = IconContent("icon_pers_grid_on");
        private static readonly GUIContent personalGridSnapIcon = IconContent("icon_pers_gridsnap");
        private static readonly GUIContent personalGridSnapIconOn = IconContent("icon_pers_gridsnap_on");
        private static readonly GUIContent personalRelSnapIcon = IconContent("icon_pers_relsnap");
        private static readonly GUIContent personalRelSnapIconOn = IconContent("icon_pers_relsnap_on");
        private static readonly GUIContent personalNoSnapIcon = IconContent("icon_pers_nosnap");
        private static readonly GUIContent personalNoSnapIconOn = IconContent("icon_pers_nosnap_on");

        private static readonly CSGSkin proSkin = new CSGSkin();
        private static readonly CSGSkin personalSkin = new CSGSkin();

        public static CSGSkin Skin
        {
            get => EditorGUIUtility.isProSkin ? proSkin : personalSkin;
        }

        public static void InitStyles()
        {
            if(m_StylesInitialized)
            {
                return;
            }

            GUISkin oldSkin = GUI.skin;
            m_StylesInitialized = true;
            SetDefaultGUISkin();

            Texture2D whiteTexture = MaterialUtility.CreateSolidColorTexture(8, 8, Color.white);

            SceneTextLabel          = new GUIStyle(GUI.skin.textArea);
            SceneTextLabel.richText = true;

            SceneTextLabel.onActive.background  = whiteTexture;
            SceneTextLabel.onFocused.background = whiteTexture;
            SceneTextLabel.onHover.background   = whiteTexture;
            SceneTextLabel.onNormal.background  = whiteTexture;

            SceneTextLabel.onActive.textColor  = Color.black;
            SceneTextLabel.onFocused.textColor = Color.black;
            SceneTextLabel.onHover.textColor   = Color.black;
            SceneTextLabel.onNormal.textColor  = Color.black;

            GUIStyle toolTipStyle = new GUIStyle(GUI.skin.textArea);
            toolTipStyle.richText      =  true;
            toolTipStyle.wordWrap      =  true;
            toolTipStyle.stretchHeight =  true;
            toolTipStyle.padding.left  += 4;
            toolTipStyle.padding.right += 4;
            toolTipStyle.clipping      =  TextClipping.Overflow;

            ToolTipTitleStyle                =  new GUIStyle(toolTipStyle);
            ToolTipTitleStyle.padding.top    += 4;
            ToolTipTitleStyle.padding.bottom += 2;
            ToolTipContentsStyle             =  new GUIStyle(toolTipStyle);
            ToolTipKeycodesStyle             =  new GUIStyle(toolTipStyle);
            ToolTipKeycodesStyle.padding.top += 2;

            RightAlignedLabel           = new GUIStyle();
            RightAlignedLabel.alignment = TextAnchor.MiddleRight;

            UnpaddedWindow                = new GUIStyle(GUI.skin.window);
            UnpaddedWindow.padding.left   = 2;
            UnpaddedWindow.padding.bottom = 2;

            EmptyMaterialStyle                   = new GUIStyle(GUIStyle.none);
            EmptyMaterialStyle.normal.background = MaterialUtility.CreateSolidColorTexture(2, 2, Color.black);

            SelectionRectStyle = GetStyle("selectionRect");

            GUIStyle redToolbarDropDown = GetStyle("toolbarDropDown");

            proSkin.redToolbarDropDown                    = new GUIStyle(redToolbarDropDown);
            proSkin.redToolbarDropDown.normal.textColor   = Color.Lerp(Color.red, redToolbarDropDown.normal.textColor, 0.5f);
            proSkin.redToolbarDropDown.onNormal.textColor = Color.Lerp(Color.red, redToolbarDropDown.onNormal.textColor, 0.125f);

            personalSkin.redToolbarDropDown                    = new GUIStyle(redToolbarDropDown);
            personalSkin.redToolbarDropDown.normal.textColor   = Color.Lerp(Color.red, redToolbarDropDown.normal.textColor, 0.5f);
            personalSkin.redToolbarDropDown.onNormal.textColor = Color.Lerp(Color.red, redToolbarDropDown.onNormal.textColor, 0.125f);

            proSkin.menuItem      = GetStyle("MenuItem");
            personalSkin.menuItem = GetStyle("MenuItem");

            proSkin.lockedBackgroundColor             = Color.Lerp(Color.white, Color.red, 0.5f);
            proSkin.xToolbarButton                    = new GUIStyle(EditorStyles.toolbarButton);
            proSkin.xToolbarButton.normal.textColor   = Color.Lerp(Handles.xAxisColor, Color.gray, 0.75f);
            proSkin.xToolbarButton.onNormal.textColor = Color.Lerp(Handles.xAxisColor, Color.white, 0.125f);
            proSkin.yToolbarButton                    = new GUIStyle(EditorStyles.toolbarButton);
            proSkin.yToolbarButton.normal.textColor   = Color.Lerp(Handles.yAxisColor, Color.gray, 0.75f);
            proSkin.yToolbarButton.onNormal.textColor = Color.Lerp(Handles.yAxisColor, Color.white, 0.125f);
            proSkin.zToolbarButton                    = new GUIStyle(EditorStyles.toolbarButton);
            proSkin.zToolbarButton.normal.textColor   = Color.Lerp(Handles.zAxisColor, Color.gray, 0.75f);
            proSkin.zToolbarButton.onNormal.textColor = Color.Lerp(Handles.zAxisColor, Color.white, 0.125f);

            personalSkin.lockedBackgroundColor             = Color.Lerp(Color.black, Color.red, 0.5f);
            personalSkin.xToolbarButton                    = new GUIStyle(EditorStyles.toolbarButton);
            personalSkin.xToolbarButton.normal.textColor   = Color.Lerp(Handles.xAxisColor, Color.white, 0.75f);
            personalSkin.xToolbarButton.onNormal.textColor = Color.Lerp(Handles.xAxisColor, Color.black, 0.25f);
            personalSkin.yToolbarButton                    = new GUIStyle(EditorStyles.toolbarButton);
            personalSkin.yToolbarButton.normal.textColor   = Color.Lerp(Handles.yAxisColor, Color.white, 0.75f);
            personalSkin.yToolbarButton.onNormal.textColor = Color.Lerp(Handles.yAxisColor, Color.black, 0.25f);
            personalSkin.zToolbarButton                    = new GUIStyle(EditorStyles.toolbarButton);
            personalSkin.zToolbarButton.normal.textColor   = Color.Lerp(Handles.zAxisColor, Color.white, 0.75f);
            personalSkin.zToolbarButton.onNormal.textColor = Color.Lerp(Handles.zAxisColor, Color.black, 0.25f);

            RedTextArea                  = new GUIStyle(GUI.skin.textArea);
            RedTextArea.normal.textColor = Color.red;

            RedTextLabel                  = new GUIStyle(GUI.skin.label);
            RedTextLabel.normal.textColor = Color.red;
            RedTextLabel.richText         = true;
            RedTextLabel.wordWrap         = true;

            RedButton                  = new GUIStyle(GUI.skin.button);
            RedButton.normal.textColor = Color.red;

            WrapLabel          = new GUIStyle(GUI.skin.label);
            WrapLabel.wordWrap = true;

            VersionLabelStyle           = new GUIStyle(EditorStyles.label);
            VersionLabelStyle.alignment = TextAnchor.MiddleRight;
            Color originalColor = VersionLabelStyle.normal.textColor;
            originalColor.a                    = 0.4f;
            VersionLabelStyle.normal.textColor = originalColor;

            BottomToolBarStyle = new GUIStyle(EditorStyles.toolbar);

            BrushEditModeContent = new[] { new GUIContent("Place"), new GUIContent("Generate"), new GUIContent("Edit"), new GUIContent("Clip"), new GUIContent("Surfaces") };

            BrushEditModeTooltips = new[]
            {
                new ToolTip(
                    "Place mode", "In this mode you can place, rotate and scale brushes"
                  , Keys.switchToObjectEditMode
                )
              , new ToolTip(
                    "Generate mode", "In this mode you can create brushes using several generators"
                  , Keys.switchToGenerateEditMode
                )
              , new ToolTip("Edit mode", "In this mode you can edit the shapes of brushes", Keys.switchToMeshEditMode), new ToolTip("Clip mode", "In this mode you can split or clip brushes", Keys.switchToClipEditMode), new ToolTip(
                    "Surfaces mode"
                  , "In this mode you can modify the texturing and everything else related to brush surfaces"
                  , Keys.switchToSurfaceEditMode
                )
            };

            Type enumType = typeof(ToolEditMode);

            BrushEditModeNames  = (from name in Enum.GetNames(enumType) select ObjectNames.NicifyVariableName(name)).ToArray();
            BrushEditModeValues = Enum.GetValues(enumType).Cast<ToolEditMode>().ToArray();

            for(int i = 0; i < BrushEditModeNames.Length; i++)
            {
                if(BrushEditModeContent[i].text != BrushEditModeNames[i])
                {
                    Debug.LogError("Fail!");
                }
            }

            CSGSkin pro      = proSkin;
            CSGSkin personal = personalSkin;

            for(int i = 0; i < CLIP_TYPE_COUNT; i++)
            {
                pro.clipNames[i]   = new GUIContent(proInActiveClipTypes[i]);
                pro.clipNamesOn[i] = new GUIContent(proActiveClipTypes[i]);

                pro.clipNames[i].text   = clipText[i];
                pro.clipNamesOn[i].text = clipText[i];

                personal.clipNames[i]   = new GUIContent(personalActiveClipTypes[i]);
                personal.clipNamesOn[i] = new GUIContent(personalInActiveClipTypes[i]);

                personal.clipNames[i].text   = clipText[i];
                personal.clipNamesOn[i].text = clipText[i];
            }

            pro.passThrough          = new GUIContent(proPassThrough);
            pro.passThroughOn        = new GUIContent(proPassThroughOn);
            pro.hierarchyPassThrough = new GUIContent(proPassThroughOn);
            pro.passThrough.text     = passThroughText;
            pro.passThroughOn.text   = passThroughText;

            personal.passThrough          = new GUIContent(personalPassThrough);
            personal.passThroughOn        = new GUIContent(personalPassThroughOn);
            personal.hierarchyPassThrough = new GUIContent(personalPassThroughOn);
            personal.passThrough.text     = passThroughText;
            personal.passThroughOn.text   = passThroughText;

            pro.wireframe           = new GUIContent(proWireframe);
            pro.wireframeOn         = new GUIContent(proWireframeOn);
            pro.wireframe.tooltip   = wireframeTooltip;
            pro.wireframeOn.tooltip = wireframeTooltip;

            personal.wireframe           = new GUIContent(personalWireframe);
            personal.wireframeOn         = new GUIContent(personalWireframeOn);
            personal.wireframe.tooltip   = wireframeTooltip;
            personal.wireframeOn.tooltip = wireframeTooltip;

            for(int i = 0; i < SHAPE_MODE_COUNT; i++)
            {
                pro.shapeModeNames[i]   = new GUIContent(proInActiveShapeModes[i]);
                pro.shapeModeNamesOn[i] = new GUIContent(proActiveShapeModes[i]);

                personal.shapeModeNames[i]   = new GUIContent(personalActiveShapeModes[i]);
                personal.shapeModeNamesOn[i] = new GUIContent(personalInActiveShapeModes[i]);

                pro.shapeModeNames[i].text   = shapeModeText[i];
                pro.shapeModeNamesOn[i].text = shapeModeText[i];

                personal.shapeModeNames[i].text   = shapeModeText[i];
                personal.shapeModeNamesOn[i].text = shapeModeText[i];
            }

            for(int i = 0; i < OPERATION_TYPE_COUNT; i++)
            {
                pro.operationNames[i]      = new GUIContent(proInActiveOperationTypes[i]);
                pro.operationNamesOn[i]    = new GUIContent(proActiveOperationTypes[i]);
                pro.hierarchyOperations[i] = new GUIContent(proActiveOperationTypes[i]);

                personal.operationNames[i]      = new GUIContent(personalActiveOperationTypes[i]);
                personal.operationNamesOn[i]    = new GUIContent(personalInActiveOperationTypes[i]);
                personal.hierarchyOperations[i] = new GUIContent(personalInActiveOperationTypes[i]);

                pro.operationNames[i].text   = operationText[i];
                pro.operationNamesOn[i].text = operationText[i];

                personal.operationNames[i].text   = operationText[i];
                personal.operationNamesOn[i].text = operationText[i];
            }

            pro.rebuildIcon    = proRebuildIcon;
            pro.gridIcon       = proGridIcon;
            pro.gridIconOn     = proGridIconOn;
            pro.gridSnapIcon   = proGridSnapIcon;
            pro.gridSnapIconOn = proGridSnapIconOn;
            pro.relSnapIcon    = proRelSnapIcon;
            pro.relSnapIconOn  = proRelSnapIconOn;
            pro.noSnapIcon     = proNoSnapIcon;
            pro.noSnapIconOn   = proNoSnapIconOn;

            personal.rebuildIcon    = personalRebuildIcon;
            personal.gridIcon       = personalGridIcon;
            personal.gridIconOn     = personalGridIconOn;
            personal.gridSnapIcon   = personalGridSnapIcon;
            personal.gridSnapIconOn = personalGridSnapIconOn;
            personal.relSnapIcon    = personalRelSnapIcon;
            personal.relSnapIconOn  = personalRelSnapIconOn;
            personal.noSnapIcon     = personalNoSnapIcon;
            personal.noSnapIconOn   = personalNoSnapIconOn;

            GUISkin skin2 = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
            personal.messageStyle        = new GUIStyle(skin2.textArea);
            personal.messageWarningStyle = new GUIStyle(personal.messageStyle);

            pro.messageStyle        = new GUIStyle(skin2.textArea);
            pro.messageWarningStyle = new GUIStyle(pro.messageStyle);

            UnselectedIconLabelStyle          = new GUIStyle(GUI.skin.label);
            UnselectedIconLabelStyle.richText = true;
            Color color = UnselectedIconLabelStyle.normal.textColor;

            color.r *= 232.0f / 255.0f;
            color.g *= 232.0f / 255.0f;
            color.b *= 232.0f / 255.0f;
            color.a =  153.0f / 255.0f;

            UnselectedIconLabelStyle.normal.textColor = color;

            SelectedIconLabelStyle          = new GUIStyle(GUI.skin.label);
            SelectedIconLabelStyle.richText = true;

            GUI.skin = oldSkin;
        }

        public static void ResetGUIState()
        {
            GUI.skin = null;
            Color white = Color.white;
            GUI.contentColor               = white;
            GUI.backgroundColor            = white;
            GUI.color                      = Color.white;
            GUI.enabled                    = true;
            GUI.changed                    = false;
            EditorGUI.indentLevel          = 0;
            EditorGUIUtility.fieldWidth    = 0f;
            EditorGUIUtility.labelWidth    = 0f;
            EditorGUIUtility.hierarchyMode = false;
            EditorGUIUtility.wideMode      = false;
            SetDefaultGUISkin();
        }

        public static void SetDefaultGUISkin()
        {
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorGUIUtility.isProSkin ? EditorSkin.Scene : EditorSkin.Inspector);
        }

        private static GUIContent IconContent(string name)
        {
            Generated.EmbeddedAssets.TryFindIcon(name, out Texture2D image);

            return new GUIContent(image);
        }

        public static GUIStyle GetStyle(string styleName)
        {
            GUIStyle s = GUI.skin.FindStyle(styleName);

            if(s == null)
            {
                GUISkin oldSkin = GUI.skin;
                SetDefaultGUISkin();
                s        = GUI.skin.FindStyle(styleName);
                GUI.skin = oldSkin;
            }

            return s;
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorCylinderGUI
    {
        private static bool SettingsToggle(bool value, GUIContent content, GUILayoutOption sceneWidth, bool isSceneGUI)
        {
            return isSceneGUI ? EditorGUILayout.ToggleLeft(content, value, sceneWidth) : EditorGUILayout.Toggle(content, value);
        }

        private static float FloatSettingsControl(float value, float minValue, float maxValue, GUIContent content)
        {
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);
            GUILayout.Label(content, width65);
            GUI.enabled = false;
            value       = EditorGUILayout.FloatField(Mathf.Clamp(value, minValue, maxValue));
            GUI.enabled = true;

            if(GUILayout.Button("-", "buttonleft", width25))
            {
                value--;
            }

            if(GUILayout.Button("+", "buttonright", width25))
            {
                value++;
            }

            GUILayout.EndHorizontal();

            return value;
        }

        private static int IntSettingsControl(int value, int minValue, int maxValue, GUIContent content)
        {
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);
            GUILayout.Label(content, width65);
            GUI.enabled = false;
            value       = EditorGUILayout.IntField(Mathf.Clamp(value, minValue, maxValue));
            GUI.enabled = true;

            if(GUILayout.Button("-", "buttonleft", width25))
            {
                value--;
            }

            if(GUILayout.Button("+", "buttonright", width25))
            {
                value++;
            }

            GUILayout.EndHorizontal();

            return value;
        }

        private static void CylinderSettingsGUI(GeneratorCylinder generator, bool isSceneGUI)
        {
            if(isSceneGUI)
            {
                GUILayout.BeginHorizontal(GUILayout.MinWidth(0));
                GUILayout.BeginVertical(width110);
            }

            generator.CircleSmoothShading = SettingsToggle(generator.CircleSmoothShading, smoothShadingContent, width110, isSceneGUI);

            TooltipUtility.SetToolTip(smoothShadingTooltip);

            generator.CircleDistanceToSide = SettingsToggle(generator.CircleDistanceToSide, alignToSideContent, width110, isSceneGUI);

            TooltipUtility.SetToolTip(alignToSideTooltip);

            if(isSceneGUI)
            {
                GUILayout.EndVertical();
                GUILayout.BeginVertical(width80);
            }

            generator.CircleSingleSurfaceEnds = !SettingsToggle(!generator.CircleSingleSurfaceEnds, radialCapsContent, width80, isSceneGUI);

            TooltipUtility.SetToolTip(radialCapsTooltip);
            generator.CircleRecenter = SettingsToggle(generator.CircleRecenter, fitShapeContent, width80, isSceneGUI);
            TooltipUtility.SetToolTip(fitShapeTooltip);

            if(isSceneGUI)
            {
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        private static void OnGUIContents(GeneratorCylinder generator, bool isSceneGUI)
        {
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            CylinderSettingsGUI(generator, isSceneGUI);

            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            DistanceUnit distanceUnit = CSGSettings.DistanceUnit;
            DistanceUnit nextUnit     = Units.CycleToNextUnit(distanceUnit);
            GUIContent   unitText     = Units.GetUnitGUIContent(distanceUnit);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(heightContent, width65);

            if(isSceneGUI)
            {
                TooltipUtility.SetToolTip(heightTooltip);
            }

            float height = generator.HaveHeight ? generator.Height : GeometryUtility.CleanLength(generator.DefaultHeight);

            EditorGUI.BeginChangeCheck();

            height = Units.DistanceUnitToUnity(
                distanceUnit, !isSceneGUI
                    ? EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, height))
                    : EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, height), width65)
            );

            if(EditorGUI.EndChangeCheck())
            {
                if(generator.HaveHeight)
                {
                    generator.Height = height;
                }
                else
                {
                    generator.DefaultHeight = height;
                }
            }

            if(GUILayout.Button(unitText, EditorStyles.miniLabel, width25))
            {
                distanceUnit             = nextUnit;
                CSGSettings.DistanceUnit = distanceUnit;
                CSGSettings.UpdateSnapSettings();
                CSGSettings.Save();
                CSGEditorGUIUtility.RepaintAll();
            }

            GUILayout.EndHorizontal();

            TooltipUtility.SetToolTip(heightTooltip);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUI.BeginDisabledGroup(!generator.CanCommit);

            GUILayout.Label(radiusContent, width65);

            if(isSceneGUI)
            {
                TooltipUtility.SetToolTip(radiusTooltip);
            }

            float radius = generator.RadiusA;
            EditorGUI.BeginChangeCheck();

            radius = Units.DistanceUnitToUnity(
                distanceUnit, !isSceneGUI
                    ? EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius))
                    : EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius), width65)
            );

            if(EditorGUI.EndChangeCheck())
            {
                generator.RadiusA = radius;
            }

            if(GUILayout.Button(unitText, EditorStyles.miniLabel, width25))
            {
                distanceUnit             = nextUnit;
                CSGSettings.DistanceUnit = distanceUnit;
                CSGSettings.UpdateSnapSettings();
                CSGSettings.Save();
                CSGEditorGUIUtility.RepaintAll();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            if(!isSceneGUI)
            {
                TooltipUtility.SetToolTip(radiusTooltip);
            }

            GUILayout.EndVertical();

            generator.CircleSides = IntSettingsControl(generator.CircleSides, 3, CSGSettings.MaxCircleSides, sidesContent);

            TooltipUtility.SetToolTip(sidesTooltip);

            generator.CircleOffset = FloatSettingsControl(generator.CircleOffset, 0, 360, offsetContent);

            TooltipUtility.SetToolTip(offsetTooltip);

            if(!isSceneGUI)
            {
                GUILayout.Space(5);

                CylinderSettingsGUI(generator, isSceneGUI);
            }
        }

        public static bool OnShowGUI(GeneratorCylinder generator, bool isSceneGUI)
        {
            CsgguiStyleUtility.InitStyles();
            OnGUIContents(generator, isSceneGUI);

            return true;
        }
    }
}

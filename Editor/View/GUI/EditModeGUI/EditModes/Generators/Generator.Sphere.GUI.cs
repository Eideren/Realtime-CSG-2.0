using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorSphereGUI
    {
        private static bool SettingsToggle(bool value, GUIContent content, bool isSceneGUI)
        {
            return isSceneGUI
                ? EditorGUILayout.ToggleLeft(content, value, width120)
                : EditorGUILayout.Toggle(content, value);
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

        private static void SphereSettingsGUI(GeneratorSphere generator, bool isSceneGUI)
        {
            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            {
                generator.SphereSmoothShading = SettingsToggle(
                    generator.SphereSmoothShading, smoothShadingContent, isSceneGUI
                );

                TooltipUtility.SetToolTip(smoothShadingTooltip);
                generator.IsHemiSphere = SettingsToggle(generator.IsHemiSphere, hemiSphereContent, isSceneGUI);
                TooltipUtility.SetToolTip(hemiSphereTooltip);
            }

            GUILayout.EndVertical();
        }

        private static void OnGUIContents(GeneratorSphere generator, bool isSceneGUI)
        {
            DistanceUnit distanceUnit = CSGSettings.DistanceUnit;
            DistanceUnit nextUnit     = Units.CycleToNextUnit(distanceUnit);
            GUIContent   unitText     = Units.GetUnitGUIContent(distanceUnit);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            SphereSettingsGUI(generator, isSceneGUI);

            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            {
                GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

                {
                    EditorGUI.BeginDisabledGroup(!generator.CanCommit);

                    {
                        GUILayout.Label(radiusContent, width65);

                        if(isSceneGUI)
                        {
                            TooltipUtility.SetToolTip(radiusTooltip);
                        }

                        float radius = generator.SphereRadius;
                        EditorGUI.BeginChangeCheck();

                        {
                            if(!isSceneGUI)
                            {
                                radius = Units.DistanceUnitToUnity(
                                    distanceUnit
                                  , EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, radius))
                                );
                            }
                            else
                            {
                                radius = Units.DistanceUnitToUnity(
                                    distanceUnit
                                  , EditorGUILayout.DoubleField(
                                        Units.UnityToDistanceUnit(distanceUnit, radius), width65
                                    )
                                );
                            }
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            generator.SphereRadius = radius;
                        }

                        if(GUILayout.Button(unitText, EditorStyles.miniLabel, width25))
                        {
                            distanceUnit             = nextUnit;
                            CSGSettings.DistanceUnit = distanceUnit;
                            CSGSettings.UpdateSnapSettings();
                            CSGSettings.Save();
                            CSGEditorGUIUtility.RepaintAll();
                        }
                    }

                    EditorGUI.EndDisabledGroup();
                }

                GUILayout.EndHorizontal();

                if(!isSceneGUI)
                {
                    TooltipUtility.SetToolTip(radiusTooltip);
                }

                generator.SphereSplits = IntSettingsControl(
                    generator.SphereSplits,
                    1,
                    CSGSettings.MaxSphereSplits,
                    splitsContent
                );

                TooltipUtility.SetToolTip(splitsTooltip);

                generator.SphereOffset = FloatSettingsControl(
                    generator.SphereOffset,
                    0,
                    360,
                    offsetContent
                );

                TooltipUtility.SetToolTip(offsetTooltip);
            }

            GUILayout.EndVertical();

            if(!isSceneGUI)
            {
                GUILayout.Space(5);

                SphereSettingsGUI(generator, isSceneGUI);
            }
        }

        public static bool OnShowGUI(GeneratorSphere generator, bool isSceneGUI)
        {
            CsgguiStyleUtility.InitStyles();
            OnGUIContents(generator, isSceneGUI);

            return true;
        }
    }
}

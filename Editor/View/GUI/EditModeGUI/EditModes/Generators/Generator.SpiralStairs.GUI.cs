using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorSpiralStairsGUI
    {
        private static float FloatUnitsSettings(float value, GUIContent content, ToolTip tooltip, bool isSceneGUI)
        {
            DistanceUnit distanceUnit = CSGSettings.DistanceUnit;
            GUIContent   unitText     = Units.GetUnitGUIContent(distanceUnit);

            float newValue;
            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(content, width80);

            if(isSceneGUI)
            {
                TooltipUtility.SetToolTip(tooltip);
            }

            newValue = Units.DistanceUnitToUnity(
                distanceUnit
              , !isSceneGUI
                    ? EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, value))
                    : EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, value), width65)
            );

            if(GUILayout.Button(unitText, EditorStyles.miniLabel, width25))
            {
                distanceUnit = Units.CycleToNextUnit(distanceUnit);

                CSGSettings.DistanceUnit = distanceUnit;
                CSGSettings.UpdateSnapSettings();
                CSGSettings.Save();
                CSGEditorGUIUtility.RepaintAll();
            }

            GUILayout.EndHorizontal();

            if(!isSceneGUI)
            {
                TooltipUtility.SetToolTip(tooltip);
            }

            return EditorGUI.EndChangeCheck() ? newValue : value;
        }

        private static int IntValueSettings(int value, GUIContent content, ToolTip tooltip, bool isSceneGUI)
        {
            int newValue;
            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(content, width80);

            if(isSceneGUI)
            {
                TooltipUtility.SetToolTip(tooltip);
            }

            newValue = !isSceneGUI ? EditorGUILayout.IntField(value) : EditorGUILayout.IntField(value, width65);

            GUILayout.EndHorizontal();

            if(!isSceneGUI)
            {
                TooltipUtility.SetToolTip(tooltip);
            }

            return EditorGUI.EndChangeCheck() ? newValue : value;
        }

        private static void OnGUIContents(GeneratorSpiralStairs generator, bool isSceneGUI)
        {
            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            EditorGUI.BeginChangeCheck();

            int totalSteps = Mathf.Max(IntValueSettings(generator.TotalSteps, totalStepsContent, totalStepsTooltip, isSceneGUI), 1);

            if(EditorGUI.EndChangeCheck())
            {
                generator.TotalSteps = totalSteps;
            }

            EditorGUI.BeginChangeCheck();

            float stepDepth = Mathf.Max(FloatUnitsSettings(generator.StepDepth, stepDepthContent, stepDepthTooltip, isSceneGUI), GeneratorSpiralStairsSettings.MIN_STEP_DEPTH);

            if(EditorGUI.EndChangeCheck())
            {
                generator.StepDepth = stepDepth;
            }

            EditorGUI.BeginChangeCheck();

            float stepHeight = Mathf.Max(FloatUnitsSettings(generator.StepHeight, stepHeightContent, stepHeightTooltip, isSceneGUI), GeneratorSpiralStairsSettings.MIN_STEP_HEIGHT);

            if(EditorGUI.EndChangeCheck())
            {
                generator.StepHeight = stepHeight;
            }

            GUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            float stairsWidth = Mathf.Max(FloatUnitsSettings(generator.StairsWidth, stairsWidthContent, stairsWidthTooltip, isSceneGUI), 0.01f);

            if(EditorGUI.EndChangeCheck())
            {
                generator.StairsWidth = stairsWidth;
            }

            EditorGUI.BeginChangeCheck();

            float stairsHeight = Mathf.Max(FloatUnitsSettings(generator.StairsHeight, stairsHeightContent, stairsHeightTooltip, isSceneGUI), 0.01f);

            if(EditorGUI.EndChangeCheck())
            {
                generator.StairsHeight = stairsHeight;
            }

            EditorGUI.BeginChangeCheck();

            float stairsDepth = Mathf.Max(FloatUnitsSettings(generator.StairsDepth, stairsDepthContent, stairsDepthTooltip, isSceneGUI), 0.01f);

            if(EditorGUI.EndChangeCheck())
            {
                generator.StairsDepth = stairsDepth;
            }

            GUILayout.Space(4);

            EditorGUI.BeginChangeCheck();

            float extraDepth = Mathf.Max(FloatUnitsSettings(generator.ExtraDepth, extraDepthContent, extraDepthTooltip, isSceneGUI), 0);

            if(EditorGUI.EndChangeCheck())
            {
                generator.ExtraDepth = extraDepth;
            }

            EditorGUI.BeginChangeCheck();

            float extraHeight = Mathf.Max(FloatUnitsSettings(generator.ExtraHeight, extraHeightContent, extraHeightTooltip, isSceneGUI), 0);

            if(EditorGUI.EndChangeCheck())
            {
                generator.ExtraHeight = extraHeight;
            }

            GUILayout.EndVertical();
        }

        public static bool OnShowGUI(GeneratorSpiralStairs generator, bool isSceneGUI)
        {
            OnGUIContents(generator, isSceneGUI);

            return true;
        }
    }
}

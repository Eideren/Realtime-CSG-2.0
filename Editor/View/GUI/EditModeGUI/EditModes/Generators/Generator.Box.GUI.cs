using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorBoxGUI
    {
        private static void BoxSettingsGUI(GeneratorBox generator)
        {
            DistanceUnit distanceUnit = CSGSettings.DistanceUnit;
            DistanceUnit nextUnit     = Units.CycleToNextUnit(distanceUnit);
            GUIContent   unitText     = Units.GetUnitGUIContent(distanceUnit);

            GUILayout.BeginVertical(CsgguiStyleUtility.contentEmpty);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(heightContent, width65);

            float height = generator.HaveHeight ? generator.Height : GeometryUtility.CleanLength(generator.DefaultHeight);

            EditorGUI.BeginChangeCheck();

            height = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, height)));

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

            EditorGUI.BeginDisabledGroup(!generator.CanCommit);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(lengthContent, width65);
            float width = generator.Width;
            EditorGUI.BeginChangeCheck();

            width = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, width)));

            if(EditorGUI.EndChangeCheck())
            {
                generator.Width = width;
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
            TooltipUtility.SetToolTip(lengthTooltip);
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            GUILayout.Label(widthContent, width65);
            float length = generator.Length;
            EditorGUI.BeginChangeCheck();

            length = Units.DistanceUnitToUnity(distanceUnit, EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, length)));

            if(EditorGUI.EndChangeCheck())
            {
                generator.Length = length;
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
            TooltipUtility.SetToolTip(widthTooltip);

            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();
        }

        private static void OnGUIContents(GeneratorBox generator, bool isSceneGUI)
        {
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            if(isSceneGUI)
            {
                BoxSettingsGUI(generator);
            }

            GUILayout.EndHorizontal();

            if(!isSceneGUI)
            {
                GUILayout.Space(5);

                BoxSettingsGUI(generator);
            }
        }

        public static bool OnShowGUI(GeneratorBox generator, bool isSceneGUI)
        {
            OnGUIContents(generator, isSceneGUI);

            return true;
        }
    }
}

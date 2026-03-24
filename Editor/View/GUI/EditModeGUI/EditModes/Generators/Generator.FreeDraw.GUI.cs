using InternalRealtimeCSG;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorFreeDrawGUI
    {
        private static void FreeDrawSettingsGUI(GeneratorFreeDraw generator)
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

            if(GUILayout.Button(unitText, EditorStyles.miniLabel, width20))
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

            GUILayout.Label(curveSidesContent, width65);
            uint subdivisions = generator.CurveSides;
            EditorGUI.BeginChangeCheck();

            subdivisions = (uint)Mathf.Clamp(EditorGUILayout.IntField((int)subdivisions), 0, 32);

            if(EditorGUI.EndChangeCheck() && generator.CurveSides != subdivisions)
            {
                generator.CurveSides = subdivisions;
            }

            GUILayout.EndHorizontal();
            TooltipUtility.SetToolTip(curveSidesTooltip);

            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            EditorGUI.BeginDisabledGroup(!generator.HaveSelectedEdgesOrVertices);

            HandleConstraints? tangentState = generator.SelectedTangentState;
            EditorGUI.BeginChangeCheck();

            GUILayout.Label(edgeTypeContent, width65);
            EditorGUI.showMixedValue = !tangentState.HasValue;

            tangentState = (HandleConstraints)EditorGUILayout.EnumPopup(tangentState.HasValue ? tangentState.Value : HandleConstraints.Straight);

            EditorGUI.showMixedValue = false;

            if(EditorGUI.EndChangeCheck())
            {
                generator.SelectedTangentState = tangentState;
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();
            TooltipUtility.SetToolTip(edgeTypeTooltip);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(!generator.HaveSelectedEdges);

            if(GUILayout.Button(splitSelectedContent))
            {
                generator.SplitSelectedEdge();
            }

            TooltipUtility.SetToolTip(splitSelectedTooltip);

            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();
        }

        private static void OnGUIContents(GeneratorFreeDraw generator, bool isSceneGUI)
        {
            GUILayout.BeginHorizontal(CsgguiStyleUtility.contentEmpty);

            if(isSceneGUI)
            {
                FreeDrawSettingsGUI(generator);
            }

            GUILayout.EndHorizontal();

            if(!isSceneGUI)
            {
                GUILayout.Space(5);

                FreeDrawSettingsGUI(generator);
            }
        }

        public static bool OnShowGUI(GeneratorFreeDraw generator, bool isSceneGUI)
        {
            CsgguiStyleUtility.InitStyles();
            OnGUIContents(generator, isSceneGUI);

            return true;
        }
    }
}

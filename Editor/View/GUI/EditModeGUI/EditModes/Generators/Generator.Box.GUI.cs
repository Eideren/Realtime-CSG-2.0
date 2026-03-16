using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RealtimeCSG
{
    internal sealed partial class GeneratorBoxGUI
    {
        private static void BoxSettingsGUI(GeneratorBox generator, bool isSceneGUI)
        {
            DistanceUnit distanceUnit = CSGSettings.DistanceUnit;
            DistanceUnit nextUnit     = Units.CycleToNextUnit(distanceUnit);
            GUIContent   unitText     = Units.GetUnitGUIContent(distanceUnit);

            GUILayout.BeginVertical(CSG_GUIStyleUtility.ContentEmpty);

            {
                {
                    GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

                    {
                        GUILayout.Label(HeightContent, Width65);

                        float height = generator.HaveHeight
                            ? generator.Height
                            : GeometryUtility.CleanLength(generator.DefaultHeight);

                        EditorGUI.BeginChangeCheck();

                        {
                            height = Units.DistanceUnitToUnity(
                                distanceUnit
                              , EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, height))
                            );
                        }

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

                        if(GUILayout.Button(unitText, EditorStyles.miniLabel, Width25))
                        {
                            distanceUnit             = nextUnit;
                            CSGSettings.DistanceUnit = distanceUnit;
                            CSGSettings.UpdateSnapSettings();
                            CSGSettings.Save();
                            CSG_EditorGUIUtility.RepaintAll();
                        }
                    }

                    GUILayout.EndHorizontal();
                    TooltipUtility.SetToolTip(HeightTooltip);
                }

                EditorGUI.BeginDisabledGroup(!generator.CanCommit);

                {
                    GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

                    {
                        GUILayout.Label(LengthContent, Width65);
                        float width = generator.Width;
                        EditorGUI.BeginChangeCheck();

                        {
                            width = Units.DistanceUnitToUnity(
                                distanceUnit
                              , EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, width))
                            );
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            generator.Width = width;
                        }

                        if(GUILayout.Button(unitText, EditorStyles.miniLabel, Width25))
                        {
                            distanceUnit             = nextUnit;
                            CSGSettings.DistanceUnit = distanceUnit;
                            CSGSettings.UpdateSnapSettings();
                            CSGSettings.Save();
                            CSG_EditorGUIUtility.RepaintAll();
                        }
                    }

                    GUILayout.EndHorizontal();
                    TooltipUtility.SetToolTip(LengthTooltip);
                    GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

                    {
                        GUILayout.Label(WidthContent, Width65);
                        float length = generator.Length;
                        EditorGUI.BeginChangeCheck();

                        {
                            length = Units.DistanceUnitToUnity(
                                distanceUnit
                              , EditorGUILayout.DoubleField(Units.UnityToDistanceUnit(distanceUnit, length))
                            );
                        }

                        if(EditorGUI.EndChangeCheck())
                        {
                            generator.Length = length;
                        }

                        if(GUILayout.Button(unitText, EditorStyles.miniLabel, Width25))
                        {
                            distanceUnit             = nextUnit;
                            CSGSettings.DistanceUnit = distanceUnit;
                            CSGSettings.UpdateSnapSettings();
                            CSGSettings.Save();
                            CSG_EditorGUIUtility.RepaintAll();
                        }
                    }

                    GUILayout.EndHorizontal();
                    TooltipUtility.SetToolTip(WidthTooltip);
                }

                EditorGUI.EndDisabledGroup();
            }

            GUILayout.EndVertical();
        }

        private static void OnGUIContents(GeneratorBox generator, bool isSceneGUI)
        {
            //GUILayout.BeginVertical(GUIStyleUtility.ContentEmpty);
            //{
            //bool enabled = generator.HaveBrushes;
            GUILayout.BeginHorizontal(CSG_GUIStyleUtility.ContentEmpty);

            { /*
                EditorGUI.BeginDisabledGroup(!enabled);
                {
                    if (isSceneGUI)
                        GUILayout.BeginVertical(GUI.skin.box, Width100);
                    else
                        GUILayout.BeginVertical(GUIStyle.none);
                    {
                        bool mixedValues = !enabled;
                        CSGOperationType operation = generator.CurrentCSGOperationType;
                        EditorGUI.BeginChangeCheck();
                        operation = CSG_EditorGUIUtility.ChooseOperation(operation, mixedValues);
                        if (EditorGUI.EndChangeCheck())
                        {
                            generator.CurrentCSGOperationType = operation;
                        }
                    }
                    GUILayout.EndVertical();
                }
                EditorGUI.EndDisabledGroup();
                */
                if(isSceneGUI)
                {
                    BoxSettingsGUI(generator, true);
                }
            }

            GUILayout.EndHorizontal();

            if(!isSceneGUI)
            {
                GUILayout.Space(5);

                BoxSettingsGUI(generator, false);

                //GUILayout.Space(10);
            } /*else
            {
                GUILayout.Space(10);
            }*/
            /*
            EditorGUI.BeginDisabledGroup(!generator.CanCommit);
            {
                GUILayout.BeginHorizontal(GUIStyleUtility.ContentEmpty);
                {
                    if (GUILayout.Button(CommitContent)) { generator.DoCommit(); }
                    TooltipUtility.SetToolTip(CommitTooltip);
                    if (GUILayout.Button(CancelContent)) { generator.DoCancel(); }
                    TooltipUtility.SetToolTip(CancelTooltip);
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.EndDisabledGroup();
            */
            //}
            //GUILayout.EndVertical();
        }

        public static bool OnShowGUI(GeneratorBox generator, bool isSceneGUI)
        {
            CSG_GUIStyleUtility.InitStyles();
            OnGUIContents(generator, isSceneGUI);

            return true;
        }
    }
}

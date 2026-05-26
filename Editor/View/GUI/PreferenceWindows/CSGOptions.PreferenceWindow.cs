using RealtimeCSG.Legacy;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal class CSGOptionsPreferenceWindow
    {
        public static void PreferenceWindow()
        {
            EditorGUI.BeginChangeCheck();

            CSGSettings.ShowTooltips       = EditorGUILayout.ToggleLeft("Show Tool-Tips", CSGSettings.ShowTooltips);
            CSGSettings.SnapNonCSGObjects  = EditorGUILayout.ToggleLeft("Snap Non-CSG Objects to the grid", CSGSettings.SnapNonCSGObjects);
            CSGSettings.AutoRigidbody      = EditorGUILayout.ToggleLeft("Disable auto add rigidbodies", CSGSettings.AutoRigidbody);
            CSGSettings.DefaultPreserveUVs = EditorGUILayout.ToggleLeft("Preserve UVs (Default)", CSGSettings.DefaultPreserveUVs);
            CSGSettings.UpdateLightmapUVButtonMode = (CSGSettings.UpdateLightmapUVButtonState)EditorGUILayout.EnumPopup("Lightmap UVs Button", CSGSettings.UpdateLightmapUVButtonMode);

            EditorGUILayout.Space();

            CSGSettings.MaxCircleSides  = EditorGUILayout.IntField("Max Circle Sides", CSGSettings.MaxCircleSides);
            CSGSettings.MaxSphereSplits = EditorGUILayout.IntField("Max Sphere Splits", CSGSettings.MaxSphereSplits);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Surfaces", EditorStyles.boldLabel);

            bool beforeToggleWorldSpaceTexture = (CSGSettings.DefaultTexGenFlags & TexGenFlags.WorldSpaceTexture) != TexGenFlags.WorldSpaceTexture;
            bool afterToggleWorldSpaceTexture  = EditorGUILayout.ToggleLeft("Lock Texture To Object (Default)", beforeToggleWorldSpaceTexture);

            if(afterToggleWorldSpaceTexture != beforeToggleWorldSpaceTexture)
            {
                if(afterToggleWorldSpaceTexture)
                {
                    CSGSettings.DefaultTexGenFlags &= ~TexGenFlags.WorldSpaceTexture;
                }
                else
                {
                    CSGSettings.DefaultTexGenFlags |= TexGenFlags.WorldSpaceTexture;
                }
            }

            CSGSettings.ShowSceneInfo = EditorGUILayout.ToggleLeft("Show Scene Info", CSGSettings.ShowSceneInfo);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presets", EditorStyles.boldLabel);

            for (int i = 0; i < System.Enum.GetValues(typeof(PresetCategory)).Length; i++)
            {
                if (CSGSettings.Presets == null)
                {
                    CSGSettings.Presets = new Dictionary<PresetCategory, List<Preset>>();
                }

                if (CSGSettings.Presets.ContainsKey((PresetCategory)i) == false)
                {
                    CSGSettings.Presets[(PresetCategory)i] = new List<Preset>();
                }

                string PresetName = ((PresetCategory)i).ToString().Replace("_", " ");

                EditorGUILayout.LabelField(PresetName, EditorStyles.label);

                for (int j = 0; j < CSGSettings.Presets[(PresetCategory)i].Count; j++)
                {
                    EditorGUILayout.BeginHorizontal();

                    float value = EditorGUILayout.FloatField($"#{j + 1}", CSGSettings.Presets[(PresetCategory)i][j].Value);

                    GUIContent presetLabel = new GUIContent("Label");
                    EditorGUILayout.LabelField(presetLabel, GUILayout.Width(GUI.skin.label.CalcSize(presetLabel).x));
                    string label = EditorGUILayout.TextField(CSGSettings.Presets[(PresetCategory)i][j].Label);

                    CSGSettings.Presets[(PresetCategory)i][j] = new Preset(label, value);

                    if (GUILayout.Button("X", GUILayout.Width(50)))
                    {
                        CSGSettings.Presets[(PresetCategory)i].RemoveAt(j);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button($"Add {PresetName} Preset"))
                {
                    CSGSettings.Presets[(PresetCategory)i].Add(new Preset("", 1.0f));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                CSGSettings.Save();
            }
        }

        public static void ProjectSettingsWindow()
        {
            EditorGUI.BeginChangeCheck();

            CSGProjectSettings.Instance.saveMeshesInSceneFiles = !EditorGUILayout.ToggleLeft("[Experimental] Don't Save Debug Meshes In Scene Files", !CSGProjectSettings.Instance.saveMeshesInSceneFiles);

            if(!CSGProjectSettings.Instance.saveMeshesInSceneFiles)
            {
                EditorGUILayout.HelpBox("Debug meshes will be rebuilt on editor & scene load. This is an experimental option, if debug meshes are not rebuilding properly, please disable it.", MessageType.Warning, true);
            }
            else
            {
                EditorGUILayout.HelpBox("If enabled, debug meshes will be automatically rebuilt on editor & scene load, dramatically saving disk space.", MessageType.Info, true);
            }

            CSGProjectSettings.Instance.snapEverythingTo0001Grid = EditorGUILayout.ToggleLeft("Snap All CSG Objects To 0.001 Grid", CSGProjectSettings.Instance.snapEverythingTo0001Grid);

            EditorGUILayout.HelpBox("If enabled, every CSG object will snap to a small grid regardless of snapping options. This ensures things line up when on a tiny scale, but can make it difficult to work at a small scale.", MessageType.Info, true);

            if(EditorGUI.EndChangeCheck())
            {
                CSGProjectSettings.Instance.Save();
            }
        }
    }

    // Register a SettingsProvider using IMGUI for the drawing framework:
    internal static class RealtimeCSGOptionsIMGUIRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateCSGPreferenceSettings()
        {
            SettingsProvider provider = new SettingsProvider("Preferences/RealtimeCSG", SettingsScope.User)
            {
                label = "Realtime CSG"
              , guiHandler = _ =>
                {
                    GUILayout.Label("Options", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    CSGOptionsPreferenceWindow.PreferenceWindow();
                    EditorGUILayout.Separator();
                    GUILayout.Label("Keyboard settings", EditorStyles.boldLabel);
                    GUILayout.Space(10);
                    CSGKeysPreferenceWindow.PreferenceWindow();
                }
              , keywords = new[] { "Tool-Tip", "ToolTip", "Tool-Tips", "ToolTips", "Snap", "Circle", "Sphere", "Lock", "Texture", "Surface" }
            };

            return provider;
        }

        [SettingsProvider]
        public static SettingsProvider CreateCSGProjectSettings()
        {
            SettingsProvider provider =
                new SettingsProvider("Project/RealtimeCSGProjectSettings", SettingsScope.Project)
                {
                    label = "Realtime CSG"
                  , guiHandler = _ =>
                    {
                        GUILayout.Label("Options", EditorStyles.boldLabel);
                        GUILayout.Space(10);
                        CSGOptionsPreferenceWindow.ProjectSettingsWindow();
                    }
                  , keywords = new[] { "Meshes" }
                };

            return provider;
        }
    }
}

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
// Author:             Daniel Cornelius (NukeAndBeans)
// Contact:            Twitter @nukeandbeans, Discord Nuke#3681
// License:
// Date/Time:          01-27-2022 @ 7:06 PM
//
// Description:
// General utility window used to generate Editor/Scripts/Data/Generated/Icons/EmbeddedAssets.cs
//
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *

//#define RCSG_GEN_ICONS_UTILITY
#if RCSG_GEN_ICONS_UTILITY
using System;
using System.Globalization;
using System.IO;
using System.Text;
using RealtimeCSG.Foundation;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Utilities
{
    public class Icon2EmbeddedAssetsUtility : EditorWindow
    {
        private static Icon2EmbeddedAssetsUtility m_Window;

        [MenuItem("Tools/Icon Generator Utility")]
        private static void Init()
        {
            m_Window              = GetWindow<Icon2EmbeddedAssetsUtility>(true);
            m_Window.titleContent = new GUIContent("Embedded Assets Utility");
            m_Window.minSize      = new Vector2(450, 160);
            m_Window.maxSize      = new Vector2(450, 600);
            m_Window.ShowUtility();
        }

        private const string SCRIPT_NAME = "EmbeddedAssets";
        private const string NAMESPACE = "RealtimeCSG.Generated";

        private GUIContent _convertContent;
        private Texture2D[] _foundT2Ds;
        private bool _assetsLoaded;
        private Vector2 _scrollPos;

        private void OnGUI()
        {
            if(_convertContent == null)
            {
                _convertContent = new GUIContent("Convert");
            }

            using(new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if(GUILayout.Button("Find Icons"))
                {
                    _foundT2Ds = null;
                    _foundT2Ds = new Texture2D[] {};
                    _foundT2Ds = Resources.LoadAll<Texture2D>($"{nameof(RealtimeCSG)}/Icons");
                }
            }

            if(_foundT2Ds != null)
            {
                using(new GUILayout.HorizontalScope("GameViewBackground"))
                {
                    using(GUILayout.ScrollViewScope sv = new GUILayout.ScrollViewScope(_scrollPos, false, true))
                    {
                        _scrollPos = sv.scrollPosition;

                        if(_foundT2Ds.Length > 1)
                        {
                            foreach(Texture2D t in _foundT2Ds)
                            {
                                using(new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Label(t.name);
                                    //GUILayout.FlexibleSpace();
                                    GUILayout.Label($"{t.width}x{t.height}", GUILayout.Width(60));
                                    GUILayout.Label($"{t.format}", GUILayout.Width(50));
                                }
                            }

                            _assetsLoaded = true;
                        }
                    }

                    GUILayout.Space(8);

                    using(new GUILayout.VerticalScope("HelpBox"))
                    {
                        GUILayout.FlexibleSpace();

                        if(!_assetsLoaded)
                        {
                            GUI.enabled = false;
                        }

                        if(GUILayout.Button(_convertContent))
                        {
                            AssetDatabase.StartAssetEditing();

                            string[] folders    = AssetDatabase.FindAssets("t:Script TexGenState");
                            string   rcsgFolder = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(folders[0]));
                            string   path       = $@"{rcsgFolder}\Generated\Icons\{SCRIPT_NAME}.cs";

                            Generator generator = Generator.Instance;

                            generator.Initialize(NAMESPACE);
                            generator.Append("public static bool TryFindIcon(string name, out Texture2D icon)");
                            generator.Append("{");
                            generator.indentLevel++;
                            generator.Append("return icons.TryGetValue(name, out icon);");
                            generator.indentLevel--;
                            generator.Append("}");
                            generator.Append("");

                            generator.ConstructIconDictionary("icons", ref _foundT2Ds);

                            foreach(Texture2D tex in _foundT2Ds)
                            {
                                string b64 = Convert.ToBase64String(tex.GetRawTextureData());
                                generator.ConstructIconProperty(tex.name, tex.width, tex.height, tex.format, b64);
                            }

                            File.WriteAllText($@"{Application.dataPath.Replace("/", "\\").Replace("Assets", "")}\{path}", generator.Complete());

                            AssetDatabase.ImportAsset(path);

                            AssetDatabase.StopAssetEditing();
                            AssetDatabase.SaveAssets();
                            AssetDatabase.Refresh();
                        }

                        GUI.enabled = true;
                    }
                }
            }
        }

        private class Generator
        {
            private static Generator m_Instance;

            public static Generator Instance
            {
                get
                {
                    if(m_Instance != null)
                    {
                        return m_Instance;
                    }

                    return m_Instance = new Generator();
                }
            }

            public int indentLevel = 0;
            public string header = string.Empty;

            private readonly StringBuilder _builder = new StringBuilder();

            public void Initialize(string nameSpace)
            {
                _builder.Clear();

                _builder.AppendLine("/*******************************************************");
                _builder.AppendLine("* !!!!!! GENERATED, DO NOT MANUALLY EDIT !!!!!!");
                _builder.AppendLine($"* Last updated on: {DateTime.Now:D} for Realtime-CSG {VersionManager.Version}");
                _builder.AppendLine("*");
                _builder.AppendLine("* Contains embedded versions of all the icons used by Realtime-CSG,");
                _builder.AppendLine("* which are automatically loaded as-needed.");
                _builder.AppendLine("********************************************************/");
                _builder.AppendLine();

                _builder.AppendLine("using System;");
                _builder.AppendLine("using System.Collections.Generic;");
                _builder.AppendLine("using UnityEditor;");
                _builder.AppendLine("using UnityEngine;");
                _builder.AppendLine();

                _builder.AppendLine($"namespace {nameSpace}");
                _builder.AppendLine("{");
                indentLevel = 1;
                Append("public static class EmbeddedAssets");
                Append("{");
                indentLevel = 2;

                Append("static EmbeddedAssets()");
                Append("{");
                indentLevel = 3;
                Append("LoadIcons();");
                indentLevel = 2;
                Append("}");
                Append("");
            }

            public string Complete()
            {
                indentLevel = 1;
                Append("}");
                indentLevel = 0;
                Append("}");

                return _builder.ToString();
            }

            public void ConstructIconProperty(string name, int texWidth, int texHeight, TextureFormat texFormat, string b64)
            {
                //_builder.Clear();
                string nameFormatted      = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(name).Replace("_", string.Empty);
                string backingField       = $"m_{nameFormatted}";
                string backingFieldLoaded = $"{backingField}Loaded";
                string b64ConstField      = $"{name.ToUpper()}";

                // base64 string constant
                Append($"private const string {b64ConstField} = \"{b64}\";");

                // backing field e.g. 'm_Name'
                Append($"private static Texture2D {backingField};");
                Append($"private static bool {backingFieldLoaded};");

                // actual property
                Append($"private static Texture2D {nameFormatted}");
                Append("{");
                indentLevel++;
                Append("get");
                Append("{");
                indentLevel++;
                // if the backing field isn't null, and it has been loaded...
                Append($"if({backingField} != null && {backingFieldLoaded})");
                Append("{");
                indentLevel++;
                Append($"return {backingField};");
                indentLevel--;
                Append("}");
                Append(string.Empty);
                // ... otherwise we load it into GPU memory, and set the loaded flag to true
                Append($"{backingFieldLoaded} = false;");
                Append($"{backingField} = new Texture2D({texWidth}, {texHeight}, {nameof(TextureFormat)}.{texFormat}, false, false);");
                Append($"{backingField}.LoadRawTextureData(Convert.FromBase64String({b64ConstField}));");
                Append($"{backingField}.Apply();");
                Append($"{backingFieldLoaded} = true;");
                Append(string.Empty);
                Append($"return {backingField};");
                indentLevel--;
                Append("}");
                indentLevel--;
                Append("}");
                int indent = indentLevel;
                indentLevel = 0;
                Append("");
                indentLevel = indent;
            }

            public void ConstructIconDictionary(string name, ref Texture2D[] icons)
            {
                if(icons == null || icons.Length < 1)
                {
                    throw new ArgumentException($"Array {nameof(icons)} cannot be null or 0 length!");
                }

                Append($"public static Dictionary<string, Texture2D> {name} {{ get; private set; }}");
                Append("public static void LoadIcons()");
                Append("{");
                indentLevel++;
                int idx = 0;

                Append($"{name} = new Dictionary<string, Texture2D>()");
                Append("{");
                indentLevel++;

                foreach (Texture2D icon in icons)
                {
                    string propName = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(icon.name).Replace("_", string.Empty);

                    Append(idx > 0 ? $",{{\"{icon.name}\", {propName}}}" : $" {{\"{icon.name}\", {propName}}}");
                    idx++;
                }

                indentLevel--;
                Append("};");
                indentLevel--;
                Append("}");
            }

            public void Append(string text, bool newline = true)
            {
                AppendIndented(text, indentLevel, newline);
            }

            private void AppendIndented(string text, int level, bool newLine = true)
            {
                StringBuilder sb = new StringBuilder();

                for(int i = 0; i < level; i++)
                {
                    sb.Append("    ");
                }

                sb.Append(text);

                if(newLine)
                {
                    _builder.AppendLine(sb.ToString());
                }
                else
                {
                    _builder.Append(sb.ToString());
                }
            }
        }
    }
}
#endif

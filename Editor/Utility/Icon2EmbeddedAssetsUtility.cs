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

#if RCSG_GEN_ICONS_UTILITY
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Utilities
{
    public class Icon2EmbeddedAssetsUtility : EditorWindow
    {
        private static Icon2EmbeddedAssetsUtility m_Window;

        [MenuItem( "Tools/Icon Generator Utility" )]
        private static void Init()
        {
            m_Window = GetWindow<Icon2EmbeddedAssetsUtility>(true);
            m_Window.titleContent = new GUIContent("Icon2EmbeddedAssets");
            m_Window.minSize = new Vector2(450, 160);
            m_Window.maxSize = new Vector2(450, 600);
            m_Window.ShowUtility();
        }

        private const string SCRIPT_NAME = "EmbeddedAssets";
        private const string NAMESPACE = "RealtimeCSG";

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
                                    GUILayout.FlexibleSpace();

                                    GUILayout.Label($"{t.width}x{t.height} \t {t.format}", GUILayout.Width(110));
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

                            string[] folders = AssetDatabase.FindAssets("t:Script TexGenState");
                            string   rcsgFolder = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(folders[0]));
                            string   path = $@"{rcsgFolder}\Generated\Icons\{SCRIPT_NAME}.cs";

                            File.WriteAllText($@"{Application.dataPath.Replace("/", "\\").Replace("Assets", "")}\{path}", ConstructScript(_foundT2Ds));

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

        private static string TexNameProperty(string s)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.Replace("_", " ")).Replace(" ", string.Empty);
        }

        private static void ScriptAppend(ref StringBuilder sb, string content, int depth)
        {
            int i = 0;

            while(i < depth)
            {
                sb.Append("\t");

                i++;
            }

            sb.Append($"{content}\n");
        }

        private string ConstructScript(Texture2D[] textures)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("/*******************************************************");
            sb.AppendLine("* !!!!!! GENERATED, DO NOT MANUALLY EDIT !!!!!!");
            sb.AppendLine($"* Last updated on: {DateTime.Now:D}");
            sb.AppendLine("*");
            sb.AppendLine("* Contains embedded versions of all the icons used by RealtimeCSG,");
            sb.AppendLine("* which are automatically loaded as-needed.");
            sb.AppendLine("********************************************************/");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using UnityEditor;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"namespace {NAMESPACE}");
            sb.AppendLine("{");
            ScriptAppend(ref sb, $"public static class {SCRIPT_NAME}", 1);
            ScriptAppend(ref sb, "{", 1);
            ScriptAppend(ref sb, "public static bool TryFindIcon( string name, out Texture2D icon )", 2);
            ScriptAppend(ref sb, "{", 2);
            ScriptAppend(ref sb, "return icons.TryGetValue( name, out icon );", 3);
            ScriptAppend(ref sb, "}", 2);
            ScriptAppend(ref sb, "public static Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>()", 2);
            ScriptAppend(ref sb, "{", 2);

            foreach(Texture2D t in textures)
            {
                ScriptAppend(ref sb, $"{{ \"{t.name}\", {TexNameProperty(t.name)} }},", 3);
            }

            ScriptAppend(ref sb, "};", 2);

            foreach(Texture2D t in textures)
            {
                if(t != null)
                {
                    string tname = TexNameProperty(t.name);
                    string tnameLower0 = tname.Replace(tname[0].ToString(), tname[0].ToString().ToLower());

                    ScriptAppend(ref sb, $"private const string {t.name} = @\"{Convert.ToBase64String(t.GetRawTextureData())}\";", 2);
                    ScriptAppend(ref sb, $"private static Texture2D {tnameLower0};", 2);
                    ScriptAppend(ref sb, $"private static Texture2D {tname}", 2);
                    ScriptAppend(ref sb, "{", 2);
                    ScriptAppend(ref sb, "get", 3);
                    ScriptAppend(ref sb, "{", 3);
                    ScriptAppend(ref sb, $"if( {tnameLower0} == null )", 4);
                    ScriptAppend(ref sb, "{", 4);
                    ScriptAppend(ref sb, $"{tnameLower0} = new Texture2D( {t.width},{t.height}, TextureFormat.RGBA32, false, PlayerSettings.colorSpace == ColorSpace.Linear  );", 5);
                    ScriptAppend(ref sb, $"{tnameLower0}.LoadRawTextureData( Convert.FromBase64String( {t.name} ) );", 5);
                    ScriptAppend(ref sb, $"{tnameLower0}.Apply();", 5);
                    sb.AppendLine();
                    ScriptAppend(ref sb, "}}", 4);
                    ScriptAppend(ref sb, $"return {tnameLower0};", 4);
                    ScriptAppend(ref sb, "}}", 3);
                    ScriptAppend(ref sb, "}}", 2);
                    sb.AppendLine();
                }
            }

            sb.AppendLine("\t}");
            sb.AppendLine("}");
            sb.AppendLine();

            return sb.ToString().Replace("\t", "    ");
        }
    }
}
#endif

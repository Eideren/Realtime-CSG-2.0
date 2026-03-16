using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using UnityEditor.Build;

namespace RealtimeCSG
{
    internal sealed class UnityCompilerDefineManager
    {
        private const string REAL_TIME_CSG_DEFINE = "RealtimeCSG";

        private static bool IsObsolete(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            ObsoleteAttribute[] attributes = (ObsoleteAttribute[])
                fi.GetCustomAttributes(typeof(ObsoleteAttribute), false);

            return attributes.Length > 0;
        }

        public static void UpdateUnityDefines()
        {
            List<string> requiredDefines = new List<string>();

            requiredDefines.Add(REAL_TIME_CSG_DEFINE);

            const string v                  = Foundation.Versioning.PluginVersion;
            int          index              = v.IndexOfAny(new[] { '_', '.' });
            string       releaseVersionPart = v.Remove(index);
            string       lowerPart          = v.Substring(index + 1);
            string       majorVersionPart   = lowerPart.Remove(1);
            string       minorVersionPart   = lowerPart.Substring(1);

            string release_version = REAL_TIME_CSG_DEFINE + "_" + releaseVersionPart;
            string major_version   = release_version + "_" + majorVersionPart;
            string minor_version   = major_version + "_" + minorVersionPart;

            requiredDefines.Add(release_version);
            requiredDefines.Add(major_version);
            requiredDefines.Add(minor_version);

            BuildTargetGroup[] targetGroups =
                Enum.GetValues(typeof(BuildTargetGroup)).Cast<BuildTargetGroup>().ToArray();

            foreach(BuildTargetGroup targetGroup in targetGroups)
            {
                if(IsObsolete(targetGroup))
                {
                    continue;
                }

                if(targetGroup == BuildTargetGroup.Unknown || targetGroup == (BuildTargetGroup)27)
                {
                    continue;
                }

                string symbolString =
                    PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));

                if(symbolString == null)
                {
                    continue;
                }

                bool     modified = false;
                string[] symbols  = symbolString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                for(int i = symbols.Length - 1; i >= 0; i--)
                {
                    symbols[i] = symbols[i].Trim();

                    if(symbols[i].Length == 0)
                    {
                        ArrayUtility.RemoveAt(ref symbols, i);

                        continue;
                    }

                    if(symbols[i].StartsWith(REAL_TIME_CSG_DEFINE))
                    {
                        bool keepSymbol = false;

                        for(int j = 0; j < requiredDefines.Count; j++)
                        {
                            if(symbols[i] == requiredDefines[j])
                            {
                                keepSymbol = true;

                                break;
                            }
                        }

                        if(keepSymbol)
                        {
                            continue;
                        }

                        modified = true;
                        ArrayUtility.RemoveAt(ref symbols, i);
                    }
                }

                for(int i = 0; i < requiredDefines.Count; i++)
                {
                    if(!symbols.Contains(requiredDefines[i]))
                    {
                        modified = true;
                        ArrayUtility.Add(ref symbols, requiredDefines[i]);
                    }
                }

                if(!modified)
                {
                    continue;
                }

                StringBuilder stringBuilder = new StringBuilder();

                for(int i = 0; i < symbols.Length; i++)
                {
                    if(stringBuilder.Length != 0)
                    {
                        stringBuilder.Append(';');
                    }

                    stringBuilder.Append(symbols[i]);
                }

                string newSymbolString = stringBuilder.ToString();

                if(newSymbolString != symbolString)
                {
                    PlayerSettings.SetScriptingDefineSymbols(
                        NamedBuildTarget.FromBuildTargetGroup(targetGroup), newSymbolString
                    );
                }
            }
        }
    }
}

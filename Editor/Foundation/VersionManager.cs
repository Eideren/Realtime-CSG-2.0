// * * * * * * * * * * * * * * * * * * * * * *
//  Description:
//
// * * * * * * * * * * * * * * * * * * * * * *

using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Foundation
{
    public static class VersionManager
    {
        public const string PLUGIN_DLL_VERSION = "1_559";
        private const string PACKAGE_NAME = "com.prenominal.realtimecsg";

        private static Version m_Version;
        private static bool m_FoundVersion;

        public static Version Version
        {
            get
            {
                if(m_FoundVersion)
                {
                    return m_Version;
                }

                string[] search = AssetDatabase.FindAssets("package");

                if(search.Length < 1)
                {
                    Debug.LogError("Could not find any package.json");
                }

                foreach(string eid in search)
                {
                    string path = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(eid));

                    //Make sure the path is to a package.json file, if we don't check for this the read operation will fail when the plugin is insalled with the package manager
                    if (path.Contains("package.json") == false)
                    {
                        continue;
                    }

                    string json = File.ReadAllText(path);

                    // TODO: find a cleaner way to do this. i tried to use the package manager Client class, but it was being very inconsitent.
                    if(json.Contains(PACKAGE_NAME))
                    {
                        JObject jObject = JObject.Parse(json);
                        JToken  obj     = jObject["version"];

                        if(obj != null)
                        {
                            string   versionStr = obj.Value<string>();
                            string[] numbers    = versionStr.Split('.', StringSplitOptions.RemoveEmptyEntries);

                            m_Version = new Version
                            {
                                Major    = Convert.ToByte(numbers[0])
                              , Minor    = Convert.ToByte(numbers[1])
                              , Revision = Convert.ToByte(numbers[2])
                            };
                        }

                        m_FoundVersion = true;

                        return m_Version;
                    }
                }

                return m_Version;
            }
        }

        static VersionManager()
        {
        }
    }
}

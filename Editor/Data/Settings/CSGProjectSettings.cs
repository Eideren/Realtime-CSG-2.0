using UnityEngine;
using UnityEditorInternal;
using System.IO;

namespace RealtimeCSG
{
    public class CSGProjectSettings : ScriptableObject
    {
        private const string ASSET_PATH = "ProjectSettings/CSGProjectSettings.asset";

        private static CSGProjectSettings m_Instance;

        public static CSGProjectSettings Instance
        {
            get => m_Instance == null ? CreateOrLoad() : m_Instance;
        }

        public bool saveMeshesInSceneFiles = true;
        public bool snapEverythingTo0001Grid = true;

        private CSGProjectSettings()
        {
            m_Instance = this;
        }

        private static CSGProjectSettings CreateOrLoad()
        {
            InternalEditorUtility.LoadSerializedFileAndForget(ASSET_PATH);

            if(m_Instance == null)
            {
                CSGProjectSettings created = CreateInstance<CSGProjectSettings>();
                created.hideFlags = HideFlags.HideAndDontSave;
            }

            System.Diagnostics.Debug.Assert(m_Instance != null);

            return m_Instance;
        }

        public void Save()
        {
            if(m_Instance == null)
            {
                Debug.Log("Cannot save ScriptableSingleton: no instance!");

                return;
            }

            string folderPath = Path.GetDirectoryName(ASSET_PATH);

            if(!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath!);
            }

            InternalEditorUtility.SaveToSerializedFileAndForget(new[] { m_Instance }, ASSET_PATH, true);
        }
    }
}

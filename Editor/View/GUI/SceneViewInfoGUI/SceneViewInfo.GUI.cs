using System.Text;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class SceneViewInfoGUI
    {
        private static StringBuilder m_StringBuilder = new StringBuilder(64);

        public static void DrawInfoGUI(SceneView sceneView)
        {
            InitStyles(sceneView);

            try
            {
                Handles.BeginGUI();

                if(!CSGSettings.ShowSceneInfo)
                {
                    return;
                }

                // -Nuke: internally there is a root model that all other models are grouped under, so we'll subtract 1 to keep an accurate count of user created brushes.
                int modelCount = Mathf.Clamp(InternalCSGModelManager.Models.Length - 1, 0, int.MaxValue);
                int brushCount = InternalCSGModelManager.Brushes.Count;

                m_StringBuilder = new StringBuilder(64);

                m_StringBuilder.Append("Models:\t")
                   .Append(modelCount)
                   .Append("\nBrushes:\t")
                   .Append(brushCount);

                GUILayout.BeginArea(m_InfoGUIRect, m_InfoGuibgStyle);

                GUILayout.FlexibleSpace();

                GUILayout.Label(m_StringBuilder.ToString(), m_InfoGUIStyle, GUILayout.Height(INFO_GUI_LABEL_HEIGHT));

                GUILayout.EndArea();
            }
            finally
            {
                Handles.EndGUI();
            }
        }
    }
}

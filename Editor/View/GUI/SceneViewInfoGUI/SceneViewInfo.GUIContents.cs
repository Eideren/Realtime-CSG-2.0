using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal sealed partial class SceneViewInfoGUI
    {
        private static readonly Color infoGUITextColor = new Color32(160, 160, 160, 255);

        private static Rect m_InfoGUIRect;
        private static GUIStyle m_InfoGUIStyle;
        private static GUIStyle m_InfoGuibgStyle;
        private const float INFO_GUI_LABEL_HEIGHT = 34;
        private static Texture2D m_InfoGuibgTex;

        private static Texture2D InfoGuibgTex
        {
            get
            {
                if(m_InfoGuibgTex == null)
                {
                    m_InfoGuibgTex = Resources.Load<Texture2D>("GUI/infobg_blk");
                }

                return m_InfoGuibgTex;
            }
        }

        private static void InitStyles(SceneView sceneView)
        {
            m_InfoGUIRect.x = sceneView.rootVisualElement.contentRect.width - 116;
            m_InfoGUIRect.y = sceneView.rootVisualElement.contentRect.height - 74;

            m_InfoGUIRect.width  = 110;
            m_InfoGUIRect.height = INFO_GUI_LABEL_HEIGHT;

            m_InfoGUIStyle = new GUIStyle("MiniLabel")
            {
                alignment = TextAnchor.UpperLeft
              , normal    = new GUIStyleState { textColor = infoGUITextColor }
              , margin    = new RectOffset(0, 0, 0, 0)
              , padding   = new RectOffset(0, 0, 0, 0)
              , fontSize  = 11
            };

            m_InfoGuibgStyle = new GUIStyle("box") { normal = new GUIStyleState { background = InfoGuibgTex } };
        }
    }
}

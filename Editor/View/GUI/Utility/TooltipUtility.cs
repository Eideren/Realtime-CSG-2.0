using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealtimeCSG
{
    internal static class TooltipUtility
    {
        private static TooltipContentState m_CurrentState;

        private static readonly Dictionary<Object, TooltipContentState> currentStateCache =
            new Dictionary<Object, TooltipContentState>();

        private const float TOOLTIP_WAIT_TIME = 0.125f;

        private static readonly List<Object> removeObjects = new List<Object>();

        public static void CleanCache()
        {
            if(currentStateCache.Count == 0)
            {
                return;
            }

            removeObjects.Clear();

            foreach(Object item in currentStateCache.Keys)
            {
                if(!item)
                {
                    removeObjects.Add(item);
                }
            }

            if(removeObjects.Count > 0)
            {
                for(int i = 0; i < removeObjects.Count; i++)
                {
                    currentStateCache.Remove(removeObjects[i]);
                }
            }
        }

        public static void InitToolTip(SceneView sceneView)
        {
            if(!currentStateCache.TryGetValue(sceneView, out TooltipContentState state))
            {
                state                        = new TooltipContentState();
                currentStateCache[sceneView] = state;
            }

            InitToolTip(state, sceneView);
        }

        public static void InitToolTip(EditorWindow editorWindow)
        {
            if(!currentStateCache.TryGetValue(editorWindow, out TooltipContentState state))
            {
                state                           = new TooltipContentState();
                currentStateCache[editorWindow] = state;
            }

            InitToolTip(state, editorWindow);
        }

        public static void InitToolTip(Editor editor)
        {
            if(!currentStateCache.TryGetValue(editor, out TooltipContentState state))
            {
                state                     = new TooltipContentState();
                currentStateCache[editor] = state;
            }

            InitToolTip(state, editor);
        }

        private static void InitToolTip(TooltipContentState state, EditorWindow editorWindow)
        {
            m_CurrentState = state;

            if(Event.current.type != EventType.Repaint)
            {
                return;
            }

            m_CurrentState.currentToolTip = null;
            m_CurrentState.drawnThisFrame = false;
            m_CurrentState.editorWindow   = editorWindow;
            m_CurrentState.editor         = null;

            m_CurrentState.maxArea = new Rect(0, 0, editorWindow.position.width, editorWindow.position.height);
        }

        private static void InitToolTip(TooltipContentState state, Editor editor)
        {
            m_CurrentState = state;

            if(Event.current.type != EventType.Repaint &&
               Event.current.type != EventType.Layout)
            {
                return;
            }

            m_CurrentState.currentToolTip = null;
            m_CurrentState.drawnThisFrame = false;
            m_CurrentState.editorWindow   = null;
            m_CurrentState.editor         = editor;

            m_CurrentState.maxArea = new Rect(0, 0, EditorGUIUtility.currentViewWidth, Screen.height);
        }

        private static void UpdateToolTip(bool getLastRect = false, bool goUp = false)
        {
            if(m_CurrentState == null)
            {
                return;
            }

            if(m_CurrentState.prevToolTip != m_CurrentState.currentToolTip)
            {
                m_CurrentState.prevToolTip            = m_CurrentState.currentToolTip;
                m_CurrentState.tooltipTimer           = Time.realtimeSinceStartup;
                m_CurrentState.currentToolTipTitle    = null;
                m_CurrentState.currentToolTipContents = null;
                m_CurrentState.currentToolTipKeyCodes = null;
            }

            if(m_CurrentState.currentToolTip == null)
            {
                return;
            }

            GUIStyle titleStyle    = CsgguiStyleUtility.ToolTipTitleStyle;
            GUIStyle contentsStyle = CsgguiStyleUtility.ToolTipContentsStyle;
            GUIStyle keycodesStyle = CsgguiStyleUtility.ToolTipKeycodesStyle;

            if(m_CurrentState.currentToolTipTitle == null)
            {
                string currentToolTipTitleString    = m_CurrentState.currentToolTip.TitleString();
                string currentToolTipContentsString = m_CurrentState.currentToolTip.ContentsString();
                string currentToolTipKeyCodesString = m_CurrentState.currentToolTip.KeyString();

                m_CurrentState.currentToolTipTitle    = new GUIContent(currentToolTipTitleString);
                m_CurrentState.currentToolTipContents = new GUIContent(currentToolTipContentsString);

                m_CurrentState.currentToolTipKeyCodes = string.IsNullOrEmpty(currentToolTipKeyCodesString)
                    ? null
                    : new GUIContent(currentToolTipKeyCodesString);

                Vector2 currentToolTipTitleSize    = titleStyle.CalcSize(m_CurrentState.currentToolTipTitle);
                Vector2 currentToolTipContentsSize = contentsStyle.CalcSize(m_CurrentState.currentToolTipContents);

                Vector2 currentToolTipKeyCodesSize = m_CurrentState.currentToolTipKeyCodes != null
                    ? keycodesStyle.CalcSize(m_CurrentState.currentToolTipKeyCodes)
                    : Vector2.zero;

                Rect currentArea = getLastRect ? GUILayoutUtility.GetLastRect() : m_CurrentState.maxArea;

                Vector2 size = currentToolTipTitleSize;

                size.x = Mathf.Max(
                    currentToolTipTitleSize.x, currentToolTipContentsSize.x, currentToolTipKeyCodesSize.x
                );

                size.y = currentToolTipTitleSize.y + currentToolTipContentsSize.y + currentToolTipKeyCodesSize.y;
                m_CurrentState.currentToolTipArea = GUIUtility.ScreenToGUIRect(m_CurrentState.tooltipArea);
                m_CurrentState.currentToolTipArea.width = size.x;

                if(m_CurrentState.currentToolTipArea.xMax > currentArea.xMax)
                {
                    m_CurrentState.currentToolTipArea.xMin -= m_CurrentState.currentToolTipArea.xMax - currentArea.xMax;
                }

                if(m_CurrentState.currentToolTipArea.xMin < 0)
                {
                    m_CurrentState.currentToolTipArea.xMin  = 0;
                    m_CurrentState.currentToolTipArea.width = currentArea.xMax;
                }

                float currentToolTipTitleHeight = titleStyle.CalcHeight(
                    m_CurrentState.currentToolTipTitle, m_CurrentState.currentToolTipArea.width
                );

                float currentToolTipContentsHeight = contentsStyle.CalcHeight(
                    m_CurrentState.currentToolTipContents, m_CurrentState.currentToolTipArea.width
                );

                float currentToolTipKeyCodesHeight = m_CurrentState.currentToolTipKeyCodes != null
                    ? keycodesStyle.CalcHeight(
                        m_CurrentState.currentToolTipKeyCodes, m_CurrentState.currentToolTipArea.width
                    )
                    : 0;

                m_CurrentState.currentToolTipArea.height = currentToolTipTitleHeight + currentToolTipContentsHeight
                  + currentToolTipKeyCodesHeight + 4;

                if(goUp)
                {
                    if(m_CurrentState.currentToolTipArea.y - m_CurrentState.tooltipArea.height < 0)
                    {
                        m_CurrentState.currentToolTipArea.y += m_CurrentState.tooltipArea.height;
                    }
                    else
                    {
                        m_CurrentState.currentToolTipArea.y -= m_CurrentState.currentToolTipArea.height;
                    }
                }
                else
                {
                    if(m_CurrentState.currentToolTipArea.yMax + m_CurrentState.tooltipArea.height < currentArea.yMax)
                    {
                        m_CurrentState.currentToolTipArea.y += m_CurrentState.tooltipArea.height;
                    }
                    else
                    {
                        m_CurrentState.currentToolTipArea.y -= m_CurrentState.currentToolTipArea.height;
                    }
                }

                if(m_CurrentState.currentToolTipArea.y < 0)
                {
                    m_CurrentState.currentToolTipArea.y = 0;
                }

                m_CurrentState.currentToolTipTitleArea.x     = m_CurrentState.currentToolTipArea.x;
                m_CurrentState.currentToolTipTitleArea.width = m_CurrentState.currentToolTipArea.width;

                m_CurrentState.currentToolTipContentsArea.x     = m_CurrentState.currentToolTipArea.x;
                m_CurrentState.currentToolTipContentsArea.width = m_CurrentState.currentToolTipArea.width;

                m_CurrentState.currentToolTipKeyCodesArea.x     = m_CurrentState.currentToolTipArea.x;
                m_CurrentState.currentToolTipKeyCodesArea.width = m_CurrentState.currentToolTipArea.width;

                m_CurrentState.currentToolTipTitleArea.height    = currentToolTipTitleHeight;
                m_CurrentState.currentToolTipContentsArea.height = currentToolTipContentsHeight;
                m_CurrentState.currentToolTipKeyCodesArea.height = currentToolTipKeyCodesHeight;

                m_CurrentState.currentToolTipTitleArea.y = m_CurrentState.currentToolTipArea.y;

                m_CurrentState.currentToolTipContentsArea.y = m_CurrentState.currentToolTipTitleArea.y
                  + m_CurrentState.currentToolTipTitleArea.height;

                m_CurrentState.currentToolTipKeyCodesArea.y = m_CurrentState.currentToolTipContentsArea.y
                  + m_CurrentState.currentToolTipContentsArea.height;

                if(m_CurrentState.currentToolTipKeyCodes != null)
                {
                    m_CurrentState.currentToolTipKeyCodesArea.height += 4;
                }
                else
                {
                    m_CurrentState.currentToolTipContentsArea.height += 4;
                }
            }
        }

        public static void DrawToolTip(bool getLastRect = true, bool goUp = false)
        {
            if(!CSGSettings.ShowTooltips || m_CurrentState == null)
            {
                return;
            }

            if(Event.current.type != EventType.Repaint)
            {
                if(m_CurrentState.prevToolTip != m_CurrentState.currentToolTip && m_CurrentState.editor != null)
                {
                    m_CurrentState.editor.Repaint();
                }

                return;
            }

            if(m_CurrentState.currentToolTip == null)
            {
                return;
            }

            UpdateToolTip(getLastRect, goUp);

            GUIStyle titleStyle    = CsgguiStyleUtility.ToolTipTitleStyle;
            GUIStyle contentsStyle = CsgguiStyleUtility.ToolTipContentsStyle;
            GUIStyle keycodesStyle = CsgguiStyleUtility.ToolTipKeycodesStyle;

            Color prevBackgroundColor = GUI.backgroundColor;
            Color prevColor           = GUI.color;
            int   prevDepth           = GUI.depth;
            GUI.backgroundColor = Color.black;
            GUI.color           = Color.white;

            EditorGUI.LabelField(
                m_CurrentState.currentToolTipTitleArea, m_CurrentState.currentToolTipTitle, titleStyle
            );

            EditorGUI.LabelField(
                m_CurrentState.currentToolTipContentsArea, m_CurrentState.currentToolTipContents, contentsStyle
            );

            if(m_CurrentState.currentToolTipKeyCodes != null)
            {
                EditorGUI.LabelField(
                    m_CurrentState.currentToolTipKeyCodesArea, m_CurrentState.currentToolTipKeyCodes, keycodesStyle
                );
            }

            GUI.backgroundColor = prevBackgroundColor;
            GUI.color           = prevColor;
            GUI.depth           = prevDepth;
        }

        public static void SetToolTip(ToolTip tooltip)
        {
            if(tooltip == null || m_CurrentState == null)
            {
                return;
            }

            SetToolTip(tooltip, GUILayoutUtility.GetLastRect());
        }

        public static Rect GUIToScreenRect(Rect rect)
        {
            Vector2 min = GUIUtility.GUIToScreenPoint(new Vector2(rect.xMin, rect.yMin));
            Vector2 max = GUIUtility.GUIToScreenPoint(new Vector2(rect.xMax, rect.yMax));

            return new Rect(min, max - min);
        }

        public static void SetToolTip(ToolTip tooltip, Rect currentArea)
        {
            if(tooltip == null || m_CurrentState == null)
            {
                return;
            }

            if(currentArea.Contains(Event.current.mousePosition) &&
               (EditorWindow.mouseOverWindow == m_CurrentState.editorWindow ||
                   m_CurrentState.editorWindow == null))
            {
                Rect newArea = GUIToScreenRect(currentArea);

                if(m_CurrentState.prevToolTip == tooltip &&
                   m_CurrentState.tooltipArea != newArea)
                {
                    m_CurrentState.prevToolTip = null;
                }

                m_CurrentState.tooltipArea    = newArea;
                m_CurrentState.currentToolTip = tooltip;
            }
        }
    }
}

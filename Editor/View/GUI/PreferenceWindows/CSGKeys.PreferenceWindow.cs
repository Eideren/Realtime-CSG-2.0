using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal class CSGKeysPreferenceWindow
    {
        internal class LocalStyles
        {
            public readonly GUIStyle sectionScrollView = "PreferencesSectionBox";
            public readonly GUIStyle settingsBoxTitle = "OL Title";
            public GUIStyle settingsBox = "OL Box";
            public GUIStyle errorLabel = "WordWrappedLabel";
            public GUIStyle sectionElement = "PreferencesSection";
            public GUIStyle evenRow = "CN EntryBackEven";

            public GUIStyle oddRow = "CN EntryBackOdd";

            //public GUIStyle selected = "ServerUpdateChangesetOn";
            public readonly GUIStyle keysElement = "PreferencesKeysElement";
            public GUIStyle warningIcon = "CN EntryWarn";
            public readonly GUIStyle sectionHeader = new GUIStyle(EditorStyles.largeLabel);
            public readonly GUIStyle cacheFolderLocation = new GUIStyle(GUI.skin.label);
            public readonly GUIStyle redTextBox;

            public LocalStyles()
            {
                sectionScrollView                 =  new GUIStyle(sectionScrollView);
                sectionScrollView.overflow.bottom += 1;

                sectionHeader.fontStyle   =  FontStyle.Bold;
                sectionHeader.fontSize    =  18;
                sectionHeader.margin.top  =  10;
                sectionHeader.margin.left += 1;

                sectionHeader.normal.textColor = !EditorGUIUtility.isProSkin
                    ? new Color(0.4f, 0.4f, 0.4f, 1.0f)
                    : new Color(0.7f, 0.7f, 0.7f, 1.0f);

                redTextBox                  = new GUIStyle(GUI.skin.textField);
                redTextBox.normal.textColor = Color.red;

                cacheFolderLocation.wordWrap = true;
            }
        }

        private static Vector2 m_KeyNamesScrollPos = Vector2.zero;
        private static Vector2 m_KeySettingsScrollPos = Vector2.zero;
        private static KeyPref m_SelectedKey;

        private static int m_SelectedKeyIndex = -1;

        private static LocalStyles m_Constants;

        public class KeyPref
        {
            public string name;
            public KeyEvent keyEvent;
        }

        private static KeyPref[] m_KeyArray;
        private static KeyCodeWithModifier m_NewKey;

        public static KeyPref[] GetKeys()
        {
            List<KeyPref> prefs  = new List<KeyPref>();
            Type          type   = typeof(Keys);
            FieldInfo[]   fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach(FieldInfo field in fields)
            {
                if(field.FieldType == typeof(KeyEvent))
                {
                    KeyPref pref = new KeyPref();

                    KeyDescription attribute =
                        (KeyDescription)field.GetCustomAttributes(typeof(KeyDescription), false).FirstOrDefault();

                    pref.name = attribute != null ? attribute.name : ObjectNames.NicifyVariableName(field.Name);

                    pref.keyEvent = (KeyEvent)field.GetValue(null);
                    prefs.Add(pref);
                }
            }

            return prefs.ToArray();
        }

        private static bool m_CheckBounds;
        private static readonly int keysControlHash = "KeysControlHash".GetHashCode();
        private static readonly int keyEventFieldHash = "KeyEventField".GetHashCode();
        private static int m_BKeyEventActive = -1;

        private static void DoKeyEventField(Rect position, int index, ref KeyCodeWithModifier key, GUIStyle style)
        {
            int   id  = GUIUtility.GetControlID(keyEventFieldHash + index, FocusType.Passive, position);
            Event evt = Event.current;

            switch(evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                {
                    // If the mouse is inside the button, we say that we're the hot control
                    if(position.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        evt.Use();

                        if(m_BKeyEventActive == id)
                            // cancel
                        {
                            m_BKeyEventActive = -1;
                        }
                        else
                        {
                            m_BKeyEventActive = id;
                        }
                    }

                    return;
                }
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = id;

                        // If we got the mousedown, the mouseup is ours as well
                        // (no matter if the click was in the button or not)
                        evt.Use();
                    }

                    return;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        evt.Use();
                    }

                    break;
                }
                case EventType.Repaint:
                {
                    if(m_BKeyEventActive == id)
                    {
                        style.Draw(position, m_DefaultKeyContent, id);
                    }
                    else
                    {
                        string str = KeyEvent.CodeToString(key.keyCode);
                        style.Draw(position, new GUIContent(str), id);
                    }

                    break;
                }
                case EventType.KeyDown:
                {
                    if(GUIUtility.hotControl == id && m_BKeyEventActive == id)
                    {
                        // ignore presses of just modifier keys
                        if(evt.character == '\0')
                        {
                            if((evt.alt &&
                                   (evt.keyCode == KeyCode.AltGr || evt.keyCode == KeyCode.LeftAlt
                                    || evt.keyCode == KeyCode.RightAlt)) ||
                               (evt.control && (evt.keyCode == KeyCode.LeftControl
                                || evt.keyCode == KeyCode.RightControl)) ||
                               (evt.command &&
                                   (evt.keyCode == KeyCode.LeftApple || evt.keyCode == KeyCode.RightApple
                                    || evt.keyCode == KeyCode.LeftWindows ||
                                       evt.keyCode == KeyCode.RightWindows)) ||
                               (evt.shift &&
                                   (evt.keyCode == KeyCode.LeftShift || evt.keyCode == KeyCode.RightShift
                                    || (int)evt.keyCode == 0)))
                            {
                                return;
                            }
                        }

                        m_BKeyEventActive = -1;
                        GUI.changed       = true;
                        key.keyCode       = evt.keyCode;

                        key.modifiers = (evt.command ? EventModifiers.Command : 0)
                          | (evt.alt ? EventModifiers.Alt : 0)
                          | (evt.shift ? EventModifiers.Shift : 0)
                          | (evt.control ? EventModifiers.Control : 0);

                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }

                    break;
                }
            }
        }

        private static readonly GUIContent m_DefaultKeyContent = new GUIContent("[Please press a key]");

        internal static void KeyEventField(Rect position, int index, ref KeyCodeWithModifier key, GUIStyle style)
        {
            DoKeyEventField(position, index, ref key, style);
        }

        internal static void KeyEventField(
            int index, ref KeyCodeWithModifier key, GUIStyle style, params GUILayoutOption[] options
        )
        {
            Rect r = GUILayoutUtility.GetRect(m_DefaultKeyContent, style, options);
            KeyEventField(r, index, ref key, style);
        }

        private static bool UniqueKey(ref KeyCodeWithModifier keyPref, int index)
        {
            for(int i = 0; i < m_KeyArray.Length; i++)
            {
                KeyCodeWithModifier[] allKeys = m_KeyArray[i].keyEvent.keys;

                for(int j = 0; j < allKeys.Length; j++)
                {
                    if(i == m_SelectedKeyIndex && j == index)
                    {
                        continue;
                    }

                    if(allKeys[j].keyCode == keyPref.keyCode &&
                       allKeys[j].modifiers == keyPref.modifiers)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool PreferenceKeyItem(ref KeyCodeWithModifier keyPref, int index = -1)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Key:");

            if(keyPref.keyCode != KeyCode.None && !UniqueKey(ref keyPref, index))
            {
                KeyEventField(index, ref keyPref, m_Constants.redTextBox);
            }
            else
            {
                KeyEventField(index, ref keyPref, GUI.skin.textField);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Modifiers:");
            GUILayout.BeginVertical();

            EventModifiers modifier = keyPref.modifiers;

            bool modifierCommand = (modifier & EventModifiers.Command) != 0;
            bool modifierAlt     = (modifier & EventModifiers.Alt) != 0;
            bool modifierShift   = (modifier & EventModifiers.Shift) != 0;
            bool modifierControl = (modifier & EventModifiers.Control) != 0;

            if(Application.platform == RuntimePlatform.OSXEditor)
            {
                modifierCommand = GUILayout.Toggle(modifierCommand, "Command");
            }

            modifierControl = GUILayout.Toggle(modifierControl, "Control");
            modifierShift   = GUILayout.Toggle(modifierShift, "Shift");
            modifierAlt     = GUILayout.Toggle(modifierAlt, "Alt");

            bool keyIsNoLongerValid = index != -1 &&
                (GUILayout.Button("Remove") ||
                    keyPref.keyCode == KeyCode.None);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if(GUI.changed && keyIsNoLongerValid)
            {
                keyPref.modifiers = (modifierCommand ? EventModifiers.Command : 0)
                  | (modifierAlt ? EventModifiers.Alt : 0)
                  | (modifierShift ? EventModifiers.Shift : 0)
                  | (modifierControl ? EventModifiers.Control : 0);
            }

            return keyIsNoLongerValid;
        }

        private static bool PreferenceKey(ref KeyCodeWithModifier[] events, int index)
        {
            KeyCodeWithModifier keyPref = events[index];

            if(PreferenceKeyItem(ref keyPref, index))
            {
                ArrayUtility.RemoveAt(ref events, index);

                return false;
            }

            if(GUI.changed)
            {
                events[index] = keyPref;
            }

            return true;
        }

        private static void RevertKeys()
        {
            if(m_KeyArray == null)
            {
                m_KeyArray = GetKeys();
            }

            for(int i = 0; i < m_KeyArray.Length; i++)
            {
                m_KeyArray[i].keyEvent.keys = m_KeyArray[i].keyEvent.defaultKeys;
            }
        }

        public static void ReadKeys()
        {
            if(m_KeyArray == null)
            {
                m_KeyArray = GetKeys();
            }

            for(int i = 0; i < m_KeyArray.Length; i++)
            {
                m_KeyArray[i].keyEvent.keys = m_KeyArray[i].keyEvent.defaultKeys;
            }

            for(int i = 0; i < m_KeyArray.Length; i++)
            {
                string keyName  = "KEY:" + m_KeyArray[i].name;
                string keyValue = EditorPrefs.GetString(keyName, null);

                if(string.IsNullOrEmpty(keyValue))
                {
                    m_KeyArray[i].keyEvent.keys = m_KeyArray[i].keyEvent.defaultKeys;

                    continue;
                }

                try
                {
                    string[] keyStrings = keyValue.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    m_KeyArray[i].keyEvent.keys = new KeyCodeWithModifier[keyStrings.Length];

                    for(int j = 0; j < keyStrings.Length; j++)
                    {
                        string[] keyString = keyStrings[j].Split(':');

                        if(keyString.Length == 0)
                        {
                            continue;
                        }

                        KeyCode        keyCode   = (KeyCode)Enum.Parse(typeof(KeyCode), keyString[0]);
                        EventModifiers modifiers = EventModifiers.None;

                        if(keyString.Length > 0 && int.TryParse(keyString[1], out int intModifier))
                        {
                            modifiers = (EventModifiers)intModifier;
                        }

                        m_KeyArray[i].keyEvent.keys[j].keyCode   = keyCode;
                        m_KeyArray[i].keyEvent.keys[j].modifiers = modifiers;
                    }
                }
                catch
                {
                    m_KeyArray[i].keyEvent.keys = m_KeyArray[i].keyEvent.defaultKeys;
                }
            }
        }

        private static void StoreKeys()
        {
            for(int i = 0; i < m_KeyArray.Length; i++)
            {
                string                keyName    = "KEY:" + m_KeyArray[i].name;
                StringBuilder         keyBuilder = new StringBuilder();
                KeyCodeWithModifier[] keys       = m_KeyArray[i].keyEvent.keys;

                for(int j = 0; j < keys.Length; j++)
                {
                    KeyCodeWithModifier key = keys[j];
                    keyBuilder.Append(key.keyCode.ToString());
                    keyBuilder.Append(':');
                    keyBuilder.Append((int)key.modifiers);
                    keyBuilder.Append('|');
                }

                EditorPrefs.SetString(keyName, keyBuilder.ToString());
            }
        }

        public static void PreferenceWindow()
        {
            if(m_Constants == null)
            {
                m_Constants = new LocalStyles();
            }

            if(m_KeyArray == null)
            {
                m_KeyArray = GetKeys();
                ReadKeys();
            }

            int id = GUIUtility.GetControlID(keysControlHash, FocusType.Keyboard);

            KeyPref prevKey          = null;
            KeyPref nextKey          = null;
            bool    foundSelectedKey = false;

            float width = Mathf.Min(Mathf.Max(170f, EditorGUIUtility.currentViewWidth - 600), 400);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.MaxWidth(width));
            GUILayout.Label("Actions", m_Constants.settingsBoxTitle, GUILayout.ExpandWidth(true));
            Rect selectedRect = default;
            m_KeyNamesScrollPos = GUILayout.BeginScrollView(m_KeyNamesScrollPos);

            {
                for(int i = 0; i < m_KeyArray.Length; i++)
                {
                    KeyPref keyPref = m_KeyArray[i];

                    if(!foundSelectedKey)
                    {
                        if(keyPref == m_SelectedKey)
                        {
                            foundSelectedKey = true;
                        }
                        else
                        {
                            prevKey = keyPref;
                        }
                    }
                    else
                    {
                        if(nextKey == null)
                        {
                            nextKey = keyPref;
                        }
                    }

                    EditorGUI.BeginChangeCheck();

                    if(GUILayout.Toggle(keyPref == m_SelectedKey, keyPref.name, m_Constants.keysElement))
                    {
                        if(m_SelectedKey != keyPref)
                        {
                            m_CheckBounds = true;
                        }

                        m_SelectedKeyIndex = i;
                        m_SelectedKey      = keyPref;
                        m_NewKey           = new KeyCodeWithModifier();

                        if(Event.current.type == EventType.Repaint)
                        {
                            selectedRect = GUILayoutUtility.GetLastRect();
                        }
                    }

                    if(EditorGUI.EndChangeCheck())
                    {
                        GUIUtility.keyboardControl = id;
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            if(Event.current.type == EventType.Repaint && m_CheckBounds)
            {
                m_CheckBounds = false;
                Rect scrollViewRect = GUILayoutUtility.GetLastRect();
                scrollViewRect.position =  Vector2.zero;
                scrollViewRect.position += m_KeyNamesScrollPos;
                scrollViewRect.yMax     -= 34;

                if(selectedRect.yMax > scrollViewRect.yMax)
                {
                    m_KeyNamesScrollPos.y = selectedRect.yMax - scrollViewRect.height;
                    HandleUtility.Repaint();
                }

                if(selectedRect.yMin < scrollViewRect.yMin)
                {
                    m_KeyNamesScrollPos.y = selectedRect.yMin;
                    HandleUtility.Repaint();
                }

                if(m_KeyNamesScrollPos.y < 0)
                {
                    m_KeyNamesScrollPos.y = 0;
                }
            }

            GUILayout.Space(10.0f);

            GUILayout.BeginVertical();
            m_KeySettingsScrollPos = GUILayout.BeginScrollView(m_KeySettingsScrollPos);

            if(m_SelectedKey != null)
            {
                GUI.changed = false;

                KeyCodeWithModifier[] allKeys = m_SelectedKey.keyEvent.keys;

                for(int i = 0; i < allKeys.Length; i++)
                {
                    PreferenceKey(ref allKeys, i);
                }

                PreferenceKeyItem(ref m_NewKey);

                if(m_NewKey.keyCode != KeyCode.None)
                {
                    ArrayUtility.Add(ref allKeys, m_NewKey);
                }

                m_SelectedKey.keyEvent.keys = allKeys;

                if(GUI.changed)
                {
                    StoreKeys();
                }
                else
                {
                    if(GUIUtility.keyboardControl == id && Event.current.type == EventType.KeyDown)
                    {
                        switch(Event.current.keyCode)
                        {
                            case KeyCode.UpArrow:
                                if(prevKey != null && prevKey != m_SelectedKey)
                                {
                                    m_SelectedKey = prevKey;
                                    m_CheckBounds = true;
                                }

                                Event.current.Use();

                                break;

                            case KeyCode.DownArrow:
                                if(nextKey != null && nextKey != m_SelectedKey)
                                {
                                    m_SelectedKey = nextKey;
                                    m_CheckBounds = true;
                                }

                                Event.current.Use();

                                break;
                        }
                    }
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.Space(10f);

            GUILayout.EndHorizontal();
            GUILayout.Space(5f);

            if(GUILayout.Button("Use Defaults", GUILayout.Width(120)))
            {
                RevertKeys();
                StoreKeys();
            }
        }
    }
}

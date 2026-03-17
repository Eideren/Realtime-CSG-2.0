using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
    public class CSGScaleSlider
    {
        private static float m_StartScale;
        private static float m_ScaleDrawLength = 1.0f;
        private static Vector3 m_StartScale3;
        private static float m_ValueDrag;
        private static Vector2 m_StartMousePosition;
        private static Vector2 m_CurrentMousePosition;

        private static float SnapValue(float val, float snap, bool snapping)
        {
            if(snapping && snap > 0)
            {
                return Mathf.Round(val / snap) * snap;
            }

            return val;
        }

        public static float DoAxis(
            int id, float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, bool snapping
          , float snap, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction
        )
        {
            Event evt = Event.current;

            switch(evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    HandleUtility.AddControl(id, HandleUtility.DistanceToLine(position, position + direction * size));

                    HandleUtility.AddControl(
                        id, HandleUtility.DistanceToCircle(position + direction * size, size * .2f)
                    );

                    break;
                }
                case EventType.MouseDown:
                {
                    if(CSGHandles.Disabled)
                    {
                        break;
                    }

                    if(((HandleUtility.nearestControl == id && evt.button == 0) ||
                           (GUIUtility.keyboardControl == id && evt.button == 2)) && GUIUtility.hotControl == 0)
                    {
                        if(initFunction != null)
                        {
                            initFunction();
                        }

                        GUIUtility.hotControl  = GUIUtility.keyboardControl = id;
                        m_CurrentMousePosition = m_StartMousePosition       = evt.mousePosition;
                        m_StartScale           = scale;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        m_CurrentMousePosition += evt.delta;

                        float dist = 1 + HandleUtility.CalcLineTranslation(
                            m_StartMousePosition, m_CurrentMousePosition, position, direction
                        ) / size;

                        dist        = SnapValue(dist, snap, snapping);
                        scale       = m_StartScale * dist;
                        GUI.changed = true;
                        evt.Use();
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();

                        if(shutdownFunction != null)
                        {
                            shutdownFunction();
                        }

                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }

                    break;
                }
                case EventType.Repaint:
                {
                    Color originalColor = Handles.color;

                    if(id == GUIUtility.keyboardControl)
                    {
                        Handles.color = Handles.selectedColor;
                    }
                    else if(CSGHandles.Disabled)
                    {
                        Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);
                    }

                    float s = size;

                    if(GUIUtility.hotControl == id)
                    {
                        s = size * scale / m_StartScale;
                    }

                    Handles.CubeHandleCap(
                        id, position + direction * s * m_ScaleDrawLength, rotation, size * .1f, EventType.Repaint
                    );

                    Handles.DrawLine(position, position + direction * (s * m_ScaleDrawLength - size * .05f));

                    Handles.color = originalColor;

                    break;
                }
            }

            return scale;
        }

        public static Vector3 DoCenter(
            int id, Vector3 value, Vector3 position, Quaternion rotation, float size, CSGHandles.CapFunction capFunction
          , bool snapping, float snap, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction
        )
        {
            size *= 0.5f;
            Event evt = Event.current;

            switch(evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    capFunction(id, position, rotation, size, EventType.Layout);

                    break;
                }
                case EventType.MouseDown:
                {
                    if(CSGHandles.Disabled)
                    {
                        break;
                    }

                    if(((HandleUtility.nearestControl == id && evt.button == 0) ||
                           (GUIUtility.keyboardControl == id && evt.button == 2)) && GUIUtility.hotControl == 0)
                    {
                        if(initFunction != null)
                        {
                            initFunction();
                        }

                        GUIUtility.hotControl = GUIUtility.keyboardControl = id;
                        m_StartScale3         = value;
                        m_ValueDrag           = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        m_ValueDrag += HandleUtility.niceMouseDelta * .01f;

                        float oldScaleValue = m_StartScale3.x;
                        float newScaleValue = (SnapValue(m_ValueDrag, snap, snapping) + 1.0f) * m_StartScale3.x;

                        float dif = newScaleValue / oldScaleValue;

                        if(!CSGSettings.LockAxisX)
                        {
                            value.x = newScaleValue;
                        }

                        if(!CSGSettings.LockAxisY)
                        {
                            value.y = m_StartScale3.y * dif;
                        }

                        if(!CSGSettings.LockAxisZ)
                        {
                            value.z = m_StartScale3.z * dif;
                        }

                        m_ScaleDrawLength = value.x / m_StartScale3.x;
                        GUI.changed       = true;
                        evt.Use();
                    }

                    break;
                }
                case EventType.KeyDown:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        if(evt.keyCode == KeyCode.Escape)
                        {
                            value                 = m_StartScale3;
                            m_ScaleDrawLength     = 1.0f;
                            GUIUtility.hotControl = 0;
                            GUI.changed           = true;
                            evt.Use();
                        }
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        m_ScaleDrawLength     = 1.0f;
                        evt.Use();

                        if(shutdownFunction != null)
                        {
                            shutdownFunction();
                        }

                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }

                    break;
                }
                case EventType.Repaint:
                {
                    Color originalColor = Handles.color;

                    if(id == GUIUtility.keyboardControl)
                    {
                        Handles.color = Handles.selectedColor;
                    }
                    else if(CSGHandles.Disabled)
                    {
                        Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);
                    }

                    capFunction(id, position, rotation, size, EventType.Repaint);

                    Handles.color = originalColor;

                    break;
                }
            }

            return value;
        }
    }
}

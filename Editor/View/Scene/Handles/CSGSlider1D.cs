using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
    public static class CSGSlider1D
    {
        private static Vector2 m_StartMousePosition;
        private static Vector2 m_CurrentMousePosition;
        private static Vector3 m_StartPosition;
        private static Vector3[] m_SnapVertices;

        internal static Vector3 Do(
            Camera camera, int id, Vector3 position, Vector3 direction, float size, CSGHandles.CapFunction capFunction
          , SnapMode snapMode, Vector3[] snapVertices, CSGHandles.InitFunction initFunction = null
          , CSGHandles.InitFunction shutdownFunction = null
        )
        {
            Event evt = Event.current;

            switch(evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    if(capFunction != null)
                    {
                        capFunction(id, position, Quaternion.LookRotation(direction), size, EventType.Layout);
                    }
                    else
                    {
                        HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(position, size * .2f));
                    }

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
                        m_SnapVertices = null;

                        if(initFunction != null)
                        {
                            initFunction();
                        }

                        GUIUtility.hotControl  = GUIUtility.keyboardControl = id;
                        m_CurrentMousePosition = m_StartMousePosition       = evt.mousePosition;
                        m_StartPosition        = position;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        if(m_SnapVertices == null)
                        {
                            m_SnapVertices = snapVertices != null ? snapVertices.ToArray() : new[] { m_StartPosition };
                        }

                        m_CurrentMousePosition += evt.delta;

                        float dist = HandleUtility.CalcLineTranslation(
                            m_StartMousePosition, m_CurrentMousePosition, m_StartPosition, direction
                        );

                        Vector3 worldDirection = Handles.matrix.MultiplyVector(direction);
                        Vector3 worldPosition  = Handles.matrix.MultiplyPoint(m_StartPosition) + worldDirection * dist;
                        position = Handles.inverseMatrix.MultiplyPoint(worldPosition);

                        switch(snapMode)
                        {
                            case SnapMode.GridSnapping:
                            {
                                Vector3 delta = CSGGrid.SnapDeltaToGrid(
                                    camera, position - m_StartPosition, m_SnapVertices, false, true
                                );

                                position = delta + m_StartPosition;

                                break;
                            }
                            case SnapMode.RelativeSnapping:
                            {
                                Vector3 delta = CSGGrid.SnapDeltaRelative(camera, position - m_StartPosition, false);
                                position = delta + m_StartPosition;

                                break;
                            }
                            default:
                            case SnapMode.None:
                            {
                                position = CSGGrid.HandleLockedAxi(position - m_StartPosition) + m_StartPosition;

                                break;
                            }
                        }

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

                        if(shutdownFunction != null)
                        {
                            shutdownFunction();
                        }

                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }

                    break;
                }
                case EventType.Repaint:
                {
                    Color originalColor = Handles.color;

                    Handles.color = CSGHandles.StateColor(
                        originalColor, CSGHandles.Disabled, CSGHandles.FocusControl == id
                    );

                    if(capFunction != null)
                    {
                        capFunction(id, position, Quaternion.LookRotation(direction), size, EventType.Repaint);
                    }

                    Handles.color = originalColor;

                    break;
                }
            }

            return position;
        }
    }
}

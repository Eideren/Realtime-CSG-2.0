using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
    public class CSGDisc
    {
        private static Vector2 m_StartMousePosition;
        private static Vector2 m_CurrentMousePosition;
        private static Vector3 m_StartPosition;
        private static Vector3 m_StartAxis;
        private static Quaternion m_StartRotation;
        private static float m_RotationDist;

        private static float SnapValue(float val, float snap, bool snapping)
        {
            if(snapping && snap > 0)
            {
                return Mathf.Round(val / snap) * snap;
            }

            return val;
        }

        public static Quaternion Do(Camera camera, int id, string name, Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, bool snapping, float snap, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction)
        {
            if(Mathf.Abs(Vector3.Dot(camera.transform.forward, axis)) > .999f)
            {
                cutoffPlane = false;
            }

            Event evt = Event.current;

            switch(evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    float d;

                    if(cutoffPlane)
                    {
                        Vector3 from = Vector3.Cross(axis, camera.transform.forward).normalized;
                        d = HandleUtility.DistanceToArc(position, axis, from, 180, size) / 2;
                    }
                    else
                    {
                        d = HandleUtility.DistanceToDisc(position, axis, size) / 2;
                    }

                    HandleUtility.AddControl(id, d);

                    break;
                }
                case EventType.MouseDown:
                {
                    if(CSGHandles.Disabled)
                    {
                        break;
                    }

                    if(((HandleUtility.nearestControl == id && evt.button == 0) || (GUIUtility.keyboardControl == id && evt.button == 2)) && GUIUtility.hotControl == 0)
                    {
                        if(initFunction != null)
                        {
                            initFunction();
                        }

                        GUIUtility.hotControl = GUIUtility.keyboardControl = id; // Grab mouse focus

                        if(cutoffPlane)
                        {
                            Vector3 from = Vector3.Cross(axis, camera.transform.forward).normalized;
                            m_StartPosition = HandleUtility.ClosestPointToArc(position, axis, from, 180, size);
                        }
                        else
                        {
                            m_StartPosition = HandleUtility.ClosestPointToDisc(position, axis, size);
                        }

                        m_RotationDist         = 0;
                        m_StartRotation        = rotation;
                        m_StartAxis            = axis;
                        m_CurrentMousePosition = m_StartMousePosition = Event.current.mousePosition;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        Vector3 direction = Vector3.Cross(axis, position - m_StartPosition).normalized;
                        m_CurrentMousePosition += evt.delta;

                        m_RotationDist = HandleUtility.CalcLineTranslation(m_StartMousePosition, m_CurrentMousePosition, m_StartPosition, direction) / size * 30;

                        m_RotationDist = SnapValue(m_RotationDist, snap, snapping);
                        rotation       = Quaternion.AngleAxis(m_RotationDist * -1, m_StartAxis) * m_StartRotation;

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
                case EventType.KeyDown:
                {
                    if(evt.keyCode == KeyCode.Escape && GUIUtility.hotControl == id)
                    {
                        // We do not use the event nor clear hotcontrol to ensure auto revert value kicks in from native side
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

                    // If we're dragging it, we'll go a bit further and draw a selection pie
                    if(GUIUtility.hotControl == id)
                    {
                        Vector3 from = Vector3.Cross(axis, Vector3.Cross(axis, (m_StartPosition - position).normalized).normalized);

                        float radius   = size * 1.1f;
                        float endAngle = m_RotationDist;

                        PaintUtility.DrawRotateCircle(camera, position, axis, from, radius, 0, 0, endAngle, Handles.color, name, true);
                        PaintUtility.DrawRotateCirclePie(position, axis, from, radius, 0, endAngle, Handles.color);
                    }
                    else if(CSGHandles.Disabled)
                    {
                        Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);
                    }

                    if(cutoffPlane)
                    {
                        Vector3 from = Vector3.Cross(axis, camera.transform.forward).normalized;
                        Handles.DrawWireArc(position, axis, from, 180, size);
                    }
                    else
                    {
                        Handles.DrawWireDisc(position, axis, size);
                    }

                    Handles.color = originalColor;

                    break;
                }
            }

            return rotation;
        }
    }
}

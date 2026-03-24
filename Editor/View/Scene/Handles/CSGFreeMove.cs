using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
    public class CSGFreeMove
    {
        private static Vector2 m_StartMousePosition;
        private static Vector2 m_CurrentMousePosition;
        private static Vector3 m_StartPosition;
        private static Vector3[] m_SnapVertices;

        public static Vector3 Do(Camera camera, int id, Vector3 position, Quaternion rotation, float size, CSGHandles.CapFunction handleFunction, bool snapping, Vector3[] snapVertices)
        {
            Vector3   worldPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 origMatrix    = Handles.matrix;

            Event evt = Event.current;

            switch(evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    Handles.matrix = Matrix4x4.identity;
                    handleFunction(id, position, rotation, size, EventType.Layout);
                    Handles.matrix = origMatrix;

                    break;
                }
                case EventType.MouseDown:
                {
                    if((HandleUtility.nearestControl == id && evt.button == 0) || (GUIUtility.keyboardControl == id && evt.button == 2))
                    {
                        GUIUtility.hotControl  = GUIUtility.keyboardControl = id;
                        m_CurrentMousePosition = m_StartMousePosition       = evt.mousePosition;
                        m_StartPosition        = position;

                        m_SnapVertices = snapVertices != null ? snapVertices : new[] { m_StartPosition };

                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        m_CurrentMousePosition += new Vector2(evt.delta.x, -evt.delta.y);
                        Vector3 screenPos = camera.WorldToScreenPoint(Handles.matrix.MultiplyPoint(m_StartPosition));
                        screenPos += (Vector3)(m_CurrentMousePosition - m_StartMousePosition);
                        position  =  Handles.inverseMatrix.MultiplyPoint(camera.ScreenToWorldPoint(screenPos));

                        if(snapping)
                        {
                            position = CSGGrid.SnapDeltaToGrid(camera, position - m_StartPosition, m_SnapVertices, false, true) + m_StartPosition;
                        }
                        else
                        {
                            position = CSGGrid.HandleLockedAxi(position - m_StartPosition) + m_StartPosition;
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
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }

                    break;
                }
                case EventType.Repaint:
                {
                    Color temp = Color.white;

                    if(id == GUIUtility.keyboardControl)
                    {
                        temp          = Handles.color;
                        Handles.color = Handles.selectedColor;
                    }

                    Handles.matrix = Matrix4x4.identity;
                    handleFunction(id, worldPosition, camera.transform.rotation, size, EventType.Repaint);
                    Handles.matrix = origMatrix;

                    if(id == GUIUtility.keyboardControl)
                    {
                        Handles.color = temp;
                    }

                    break;
                }
            }

            return position;
        }
    }
}

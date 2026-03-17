using UnityEditor;
using UnityEngine;
using InternalRealtimeCSG;
using RealtimeCSG.Legacy;

namespace RealtimeCSG
{
    internal static class SnappedPoint
    {
        internal static int SnappedPointHash = "SnappedPoint".GetHashCode();
        private static Vector2 m_StartMousePosition;
        private static CSGPlane m_MovePlane;

        public delegate void DrawCapFunction(int controlID, Vector3 position, Quaternion rotation, float size);
        public delegate Vector3 SnapFunction(Vector2 startMousePosition, Vector2 currentMousePosition);

        public static SnapFunction snapFunction = null;

        public static Vector3 FreeMoveHandle(
            Camera camera, Vector3 position, Quaternion rotation, float size, DrawCapFunction capFunc = null
        )
        {
            int id = GUIUtility.GetControlID(SnappedPointHash, FocusType.Keyboard);

            return FreeMoveHandle(camera, id, position, rotation, size, capFunc);
        }

        public static Vector3 FreeMoveHandle(
            Camera camera, int id, Vector3 position, Quaternion rotation, float size, DrawCapFunction capFunc = null
        )
        {
            if(capFunc == null)
            {
                capFunc = PaintUtility.SquareDotCap;
            }

            Vector3   worldPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 origMatrix    = Handles.matrix;

            switch(Event.current.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    Handles.matrix = MathConstants.identityMatrix;
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(worldPosition, size * 1.2f));
                    Handles.matrix = origMatrix;

                    break;
                }
                case EventType.MouseDown:
                {
                    if(GUIUtility.hotControl == 0 &&
                       HandleUtility.nearestControl == id && Event.current.button == 0)
                    {
                        GUIUtility.hotControl             = id;
                        GUIUtility.keyboardControl        = id;
                        EditorGUIUtility.editingTextField = false;

                        m_MovePlane = CSGGrid.CurrentGridPlane;
                        m_MovePlane = new CSGPlane(m_MovePlane.normal, position);

                        m_StartMousePosition = Event.current.mousePosition;

                        Event.current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }

                    break;
                }
                case EventType.MouseDrag:
                {
                    if(GUIUtility.hotControl == id)
                    {
                        if(snapFunction == null)
                        {
                            break;
                        }

                        position = snapFunction(m_StartMousePosition, Event.current.mousePosition);

                        if(camera != null && camera.orthographic)
                        {
                            position = m_MovePlane.Project(position);
                        }

                        GUI.changed = true;
                        Event.current.Use();
                    }

                    break;
                }
                case EventType.MouseUp:
                {
                    if(GUIUtility.hotControl == id && Event.current.button == 0)
                    {
                        GUIUtility.hotControl             = 0;
                        GUIUtility.keyboardControl        = 0;
                        EditorGUIUtility.editingTextField = false;
                        Event.current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }

                    break;
                }
                case EventType.Repaint:
                {
                    bool  isSelected = id == GUIUtility.keyboardControl || id == GUIUtility.hotControl;
                    Color temp       = Handles.color;

                    Handles.color = isSelected ? Handles.selectedColor : Color.gray; // Why U not work?

                    Handles.matrix = MathConstants.identityMatrix;
                    capFunc(id, worldPosition, camera.transform.rotation, size);
                    Handles.matrix = origMatrix;

                    if(isSelected)
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

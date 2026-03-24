using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
    public class CSGFreeRotate
    {
        private static Vector2 m_CurrentMousePosition;
        internal const float PICK_DISTANCE = 5.0f;

        public static Quaternion Do(Camera camera, int id, Quaternion rotation, Vector3 position, float size, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction)
        {
            Vector3   worldPosition = Handles.matrix.MultiplyPoint(position);
            Matrix4x4 origMatrix    = Handles.matrix;

            Event evt = Event.current;

            switch(evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    Handles.matrix = Matrix4x4.identity;
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCircle(worldPosition, size) + PICK_DISTANCE);
                    Handles.matrix = origMatrix;

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

                        GUIUtility.hotControl  = GUIUtility.keyboardControl = id; // Grab mouse focus
                        m_CurrentMousePosition = evt.mousePosition;
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

                        Vector3 rotDir = camera.transform.TransformDirection(new Vector3(-evt.delta.y, -evt.delta.x, 0));

                        rotation = Quaternion.AngleAxis(evt.delta.magnitude, rotDir.normalized) * rotation;

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
                    Color originalColor = Color.white;

                    if(id == GUIUtility.keyboardControl)
                    {
                        Handles.color = Handles.selectedColor;
                    }
                    else if(CSGHandles.Disabled)
                    {
                        Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);
                    }

                    // We only want the position to be affected by the Handles.matrix.
                    Handles.matrix = Matrix4x4.identity;
                    Handles.DrawWireDisc(worldPosition, camera.transform.forward, size);

                    Handles.matrix = origMatrix;
                    Handles.color  = originalColor;

                    break;
                }
            }

            return rotation;
        }
    }
}

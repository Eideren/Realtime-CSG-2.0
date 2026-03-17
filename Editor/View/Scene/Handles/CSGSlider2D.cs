using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
    public class CSGSlider2D
    {
        private static Vector2 m_CurrentMousePosition;
        private static Vector3 m_StartPosition;
        private static Vector3[] m_SnapVertices;
        private static Vector2 m_StartPlaneOffset;

        public static Vector3 Do(
            Camera camera, int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1
          , Vector3 slideDir2, float handleSize, CSGHandles.CapFunction capFunction, SnapMode snapMode
          , Vector3[] snapVertices, CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction
        )
        {
            bool orgGuiChanged = GUI.changed;
            GUI.changed = false;

            Vector2 delta = CalcDeltaAlongDirections(
                id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snapMode, snapVertices
              , initFunction, shutdownFunction
            );

            if(GUI.changed)
            {
                handlePos = m_StartPosition + slideDir1 * delta.x + slideDir2 * delta.y;

                if(m_SnapVertices == null)
                {
                    m_SnapVertices = snapVertices != null ? snapVertices : new[] { m_StartPosition };
                }

                switch(snapMode)
                {
                    case SnapMode.GridSnapping:
                    {
                        handlePos = CSGGrid.SnapDeltaToGrid(
                            camera, handlePos - m_StartPosition, m_SnapVertices, false, true
                        ) + m_StartPosition;

                        break;
                    }
                    case SnapMode.RelativeSnapping:
                    {
                        handlePos = CSGGrid.SnapDeltaRelative(camera, handlePos - m_StartPosition, false)
                          + m_StartPosition;

                        break;
                    }
                    default:
                    case SnapMode.None:
                    {
                        handlePos = CSGGrid.HandleLockedAxi(handlePos - m_StartPosition) + m_StartPosition;

                        break;
                    }
                }
            }

            GUI.changed |= orgGuiChanged;

            return handlePos;
        }

        private static Vector2 CalcDeltaAlongDirections(
            int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2
          , float handleSize, CSGHandles.CapFunction capFunction, SnapMode snapMode, Vector3[] snapVertices
          , CSGHandles.InitFunction initFunction, CSGHandles.InitFunction shutdownFunction
        )
        {
            Vector3    position                     = handlePos + offset;
            Quaternion rotation                     = Quaternion.LookRotation(handleDir, slideDir1);
            Vector2    deltaDistanceAlongDirections = new Vector2(0, 0);

            Event evt = Event.current;

            switch(evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                {
                    if(capFunction != null)
                    {
                        capFunction(id, position, rotation, handleSize, EventType.Layout);
                    }
                    else
                    {
                        HandleUtility.AddControl(
                            id, HandleUtility.DistanceToCircle(handlePos + offset, handleSize * .5f)
                        );
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

                        Plane plane = new Plane(
                            Handles.matrix.MultiplyVector(handleDir), Handles.matrix.MultiplyPoint(handlePos)
                        );

                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(evt.mousePosition);
                        plane.Raycast(mouseRay, out float dist);

                        GUIUtility.hotControl  = GUIUtility.keyboardControl = id;
                        m_CurrentMousePosition = evt.mousePosition;
                        m_StartPosition        = handlePos;

                        Vector3 localMousePoint = Handles.inverseMatrix.MultiplyPoint(mouseRay.GetPoint(dist));
                        Vector3 clickOffset     = localMousePoint - handlePos;
                        m_StartPlaneOffset.x = Vector3.Dot(clickOffset, slideDir1);
                        m_StartPlaneOffset.y = Vector3.Dot(clickOffset, slideDir2);

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
                        Vector3 worldPosition  = Handles.matrix.MultiplyPoint(handlePos);
                        Vector3 worldSlideDir1 = Handles.matrix.MultiplyVector(slideDir1).normalized;
                        Vector3 worldSlideDir2 = Handles.matrix.MultiplyVector(slideDir2).normalized;

                        Ray mouseRay = HandleUtility.GUIPointToWorldRay(m_CurrentMousePosition);

                        Plane plane = new Plane(
                            worldPosition, worldPosition + worldSlideDir1, worldPosition + worldSlideDir2
                        );

                        if(plane.Raycast(mouseRay, out float dist))
                        {
                            Vector3 hitpos = Handles.inverseMatrix.MultiplyPoint(mouseRay.GetPoint(dist));

                            deltaDistanceAlongDirections.x = HandleUtility.PointOnLineParameter(
                                hitpos, m_StartPosition, slideDir1
                            );

                            deltaDistanceAlongDirections.y = HandleUtility.PointOnLineParameter(
                                hitpos, m_StartPosition, slideDir2
                            );

                            deltaDistanceAlongDirections -= m_StartPlaneOffset;

                            GUI.changed = true;
                        }

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
                    if(capFunction == null)
                    {
                        break;
                    }

                    Color originalColor = Handles.color;

                    if(id == GUIUtility.keyboardControl)
                    {
                        Handles.color = Handles.selectedColor;
                    }
                    else if(CSGHandles.Disabled)
                    {
                        Handles.color = Color.Lerp(originalColor, Handles.secondaryColor, 0.75f);
                    }

                    capFunction(id, position, rotation, handleSize, EventType.Repaint);

                    Handles.color = originalColor;

                    break;
                }
            }

            return deltaDistanceAlongDirections;
        }
    }
}

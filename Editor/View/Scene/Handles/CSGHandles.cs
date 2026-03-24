using UnityEditor;
using UnityEngine;

namespace RealtimeCSG.Helpers
{
    public sealed class CSGHandles
    {
        public delegate void InitFunction();

        public delegate void CapFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType);

        private static readonly Vector3[] vertices = new Vector3[4];
        private static readonly Vector3[] points = new Vector3[5];
        private static Vector3 m_PlanarHandlesOctant = Vector3.one;

        internal static Color StaticColor = new Color(.5f, .5f, .5f, 0f);
        internal static float StaticBlend = 0.6f;

        internal static int XAxisMoveHandleHash = "xAxisFreeMoveHandleHash".GetHashCode();
        internal static int YAxisMoveHandleHash = "yAxisFreeMoveHandleHash".GetHashCode();
        internal static int ZAxisMoveHandleHash = "zAxisFreeMoveHandleHash".GetHashCode();
        internal static int XZAxisMoveHandleHash = "xzAxesFreeMoveHandleHash".GetHashCode();
        internal static int XYAxisMoveHandleHash = "xyAxesFreeMoveHandleHash".GetHashCode();
        internal static int YZAxisMoveHandleHash = "yzAxesFreeMoveHandleHash".GetHashCode();
        internal static int CenterMoveHandleHash = "centerFreeMoveHandleHash".GetHashCode();

        internal static int ScaleSliderHash = "ScaleSliderHash".GetHashCode();
        internal static int ScaleValueHandleHash = "ScaleValueHandleHash".GetHashCode();
        internal static int DiscHash = "DiscHash".GetHashCode();
        internal static int FreeRotateHandleHash = "FreeRotateHandleHash".GetHashCode();

        internal static int SliderHash = "SliderHash".GetHashCode();

        public static void CubeHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.CubeHandleCap(controlID, position, rotation, size, eventType);
        }

        public static void DiamondDotCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch(eventType)
            {
                case EventType.Layout:
                {
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * .1f));

                    break;
                }
                case EventType.Repaint:
                {
                    PaintUtility.DiamondDotCap(position, size * .2f);

                    break;
                }
            }
        }

        public static void SquareDotCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch(eventType)
            {
                case EventType.Layout:
                {
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * .1f));

                    break;
                }
                case EventType.Repaint:
                {
                    PaintUtility.SquareDotCap(controlID, position, Quaternion.LookRotation(rotation * Vector3.forward), size * .2f);

                    break;
                }
            }
        }

        public static void CircleDotCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            switch(eventType)
            {
                case EventType.Layout:
                {
                    HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * .1f));

                    break;
                }
                case EventType.Repaint:
                {
                    PaintUtility.CircleDotCap(position, size * .2f);

                    break;
                }
            }
        }

        public static void DotHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.DotHandleCap(controlID, position, rotation, size, eventType);
        }

        public static void ArrowHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.ArrowHandleCap(controlID, position, rotation, size, eventType);
        }

        public static void HoverArrowHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.ArrowHandleCap(controlID, position, rotation, size, eventType);
        }

        public static void ConeHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            Handles.ConeHandleCap(controlID, position, rotation, size, eventType);
        }

        internal static float DistanceToRectangleInternal(Vector3 position, Quaternion rotation, Vector2 size)
        {
            Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
            Vector3 up       = rotation * new Vector3(0, size.y, 0);

            points[0] = HandleUtility.WorldToGUIPoint(position + sideways + up);
            points[1] = HandleUtility.WorldToGUIPoint(position + sideways - up);
            points[2] = HandleUtility.WorldToGUIPoint(position - sideways - up);
            points[3] = HandleUtility.WorldToGUIPoint(position - sideways + up);
            points[4] = points[0];

            Vector2 pos      = Event.current.mousePosition;
            bool    oddNodes = false;
            int     j        = 4;

            for(int i = 0; i < 5; i++)
            {
                if(points[i].y > pos.y != points[j].y > pos.y)
                {
                    if(pos.x < (points[j].x - points[i].x) * (pos.y - points[i].y) / (points[j].y - points[i].y) + points[i].x)
                    {
                        oddNodes = !oddNodes;
                    }
                }

                j = i;
            }

            if(!oddNodes)
            {
                float closestDist = -1f;
                j = 1;

                for(int i = 0; i < 4; i++)
                {
                    float dist = HandleUtility.DistancePointToLineSegment(pos, points[i], points[j++]);

                    if(dist < closestDist || closestDist < 0)
                    {
                        closestDist = dist;
                    }
                }

                return closestDist;
            }

            return 0;
        }

        internal static bool ContainsStatic(GameObject[] objects)
        {
            if(objects == null || objects.Length == 0)
            {
                return false;
            }

            for(int i = 0; i < objects.Length; i++)
            {
                if(objects[i] != null && objects[i].isStatic)
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly Vector3[] rectangleHandlePointsCache = new Vector3[5];

        public static void RectangleHandleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            RectangleHandleCap(controlID, position, rotation, new Vector2(size, size), eventType);
        }

        internal static void RectangleHandleCap(int controlID, Vector3 position, Quaternion rotation, Vector2 size, EventType eventType)
        {
            switch(eventType)
            {
                case EventType.Layout:
                    HandleUtility.AddControl(controlID, DistanceToRectangleInternal(position, rotation, size));

                    break;
                case EventType.Repaint:
                    Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
                    Vector3 up       = rotation * new Vector3(0, size.y, 0);

                    rectangleHandlePointsCache[0] = position + sideways + up;
                    rectangleHandlePointsCache[1] = position + sideways - up;
                    rectangleHandlePointsCache[2] = position - sideways - up;
                    rectangleHandlePointsCache[3] = position - sideways + up;
                    rectangleHandlePointsCache[4] = position + sideways + up;
                    Handles.DrawPolyLine(rectangleHandlePointsCache);

                    break;
            }
        }

        public static Vector3 Slider(Camera camera, Vector3 position, Vector3 direction, float size, CapFunction capFunction, SnapMode snapMode, Vector3[] snapVertices, InitFunction initFunction, InitFunction shutdownFunction)
        {
            return CSGSlider1D.Do(camera, GUIUtility.GetControlID(SliderHash, FocusType.Keyboard), position, direction, size, capFunction, snapMode, snapVertices, initFunction, shutdownFunction);
        }

        public static Vector3 Slider2D(Camera camera, int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1, Vector3 slideDir2, float handleSize, CapFunction capFunction, SnapMode snapMode, Vector3[] snapVertices, InitFunction initFunction, InitFunction shutdownFunction)
        {
            return CSGSlider2D.Do(camera, id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snapMode, snapVertices, initFunction, shutdownFunction);
        }

        internal static AABB Box(Camera camera, AABB bounds, Quaternion rotation, bool showEdgePoints = true)
        {
            return CSGBounds.Do(camera, bounds, rotation, showEdgePoints);
        }

        internal static bool Disabled;

        public static int FocusControl
        {
            get
            {
                if(!GUI.enabled || (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
                {
                    return 0;
                }

                return GUIUtility.hotControl == 0 ? HandleUtility.nearestControl : GUIUtility.keyboardControl;
            }
        }

        public static Color ToActiveColorSpace(Color color)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        }

        public static Color PreselectionColor = new Color(201f / 255, 200f / 255, 144f / 255, 0.89f);

        public static Color StateColor(Color color, bool isDisabled = false, bool isSelected = false, bool isPreSelected = false)
        {
            return ToActiveColorSpace(isDisabled || Disabled ? Color.Lerp(color, StaticColor, StaticBlend) : isSelected ? Handles.selectedColor : isPreSelected ? PreselectionColor : color);
        }

        public static Vector3 PositionHandle(Camera camera, Vector3 position, Quaternion rotation, SnapMode snapMode, Vector3[] snapVertices = null, InitFunction initFunction = null, InitFunction shutdownFunction = null)
        {
            GUI.SetNextControlName("xAxis");
            int xAxisSlider = GUIUtility.GetControlID(XAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("yAxis");
            int yAxisSlider = GUIUtility.GetControlID(YAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("zAxis");
            int zAxisSlider = GUIUtility.GetControlID(ZAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("xzPlane");
            int xzPlaneSlider = GUIUtility.GetControlID(XZAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("xyPlane");
            int xyPlaneSlider = GUIUtility.GetControlID(XYAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("yzPlane");
            int yzPlaneSlider = GUIUtility.GetControlID(YZAxisMoveHandleHash, FocusType.Passive);

            bool isStatic     = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);
            bool prevDisabled = Disabled;

            int hotControl = GUIUtility.hotControl;

            bool xAxisIsHot  = xAxisSlider == hotControl;
            bool yAxisIsHot  = yAxisSlider == hotControl;
            bool zAxisIsHot  = zAxisSlider == hotControl;
            bool xzAxisIsHot = xzPlaneSlider == hotControl;
            bool xyAxisIsHot = xyPlaneSlider == hotControl;
            bool yzAxisIsHot = yzPlaneSlider == hotControl;

            bool isControlHot = xAxisIsHot || yAxisIsHot || zAxisIsHot || xzAxisIsHot || xyAxisIsHot || yzAxisIsHot;

            float size          = HandleUtility.GetHandleSize(position);
            Color originalColor = Handles.color;

            bool lockedAxisX = CSGSettings.LockAxisX;
            bool lockedAxisY = CSGSettings.LockAxisY;
            bool lockedAxisZ = CSGSettings.LockAxisZ;

            bool lockedAxisXY = lockedAxisX || lockedAxisY;
            bool lockedAxisXZ = lockedAxisX || lockedAxisZ;
            bool lockedAxisYZ = lockedAxisY || lockedAxisZ;

            //,.,.., look at 2018.1 how the position handle works w/ colors

            bool xAxisDisabled = isStatic || prevDisabled || lockedAxisX || (isControlHot && !xAxisIsHot && !xzAxisIsHot && !xyAxisIsHot);
            bool yAxisDisabled = isStatic || prevDisabled || lockedAxisY || (isControlHot && !yAxisIsHot && !xyAxisIsHot && !yzAxisIsHot);
            bool zAxisDisabled = isStatic || prevDisabled || lockedAxisZ || (isControlHot && !zAxisIsHot && !xzAxisIsHot && !yzAxisIsHot);

            bool xzPlaneDisabled = isStatic || prevDisabled || lockedAxisXZ || (isControlHot && !xzAxisIsHot);
            bool xyPlaneDisabled = isStatic || prevDisabled || lockedAxisXY || (isControlHot && !xyAxisIsHot);
            bool yzPlaneDisabled = isStatic || prevDisabled || lockedAxisYZ || (isControlHot && !yzAxisIsHot);

            int currentFocusControl = FocusControl;

            bool xAxisIndirectlyFocused = currentFocusControl == xyPlaneSlider || currentFocusControl == xzPlaneSlider;
            bool yAxisIndirectlyFocused = currentFocusControl == xyPlaneSlider || currentFocusControl == yzPlaneSlider;
            bool zAxisIndirectlyFocused = currentFocusControl == xzPlaneSlider || currentFocusControl == yzPlaneSlider;

            bool xAxisSelected = xAxisIndirectlyFocused || currentFocusControl == xAxisSlider;
            bool yAxisSelected = yAxisIndirectlyFocused || currentFocusControl == yAxisSlider;
            bool zAxisSelected = zAxisIndirectlyFocused || currentFocusControl == zAxisSlider;
            bool xzAxiSelected = currentFocusControl == xAxisSlider || currentFocusControl == zAxisSlider;
            bool xyAxiSelected = currentFocusControl == xAxisSlider || currentFocusControl == yAxisSlider;
            bool yzAxiSelected = currentFocusControl == yAxisSlider || currentFocusControl == zAxisSlider;

            Color xAxisColor   = StateColor(Handles.xAxisColor, xAxisDisabled, xAxisSelected);
            Color yAxisColor   = StateColor(Handles.yAxisColor, yAxisDisabled, yAxisSelected);
            Color zAxisColor   = StateColor(Handles.zAxisColor, zAxisDisabled, zAxisSelected);
            Color xzPlaneColor = StateColor(Handles.yAxisColor, xzPlaneDisabled, xzAxiSelected);
            Color xyPlaneColor = StateColor(Handles.zAxisColor, xyPlaneDisabled, xyAxiSelected);
            Color yzPlaneColor = StateColor(Handles.xAxisColor, yzPlaneDisabled, yzAxiSelected);

            Disabled = xAxisDisabled;

            Handles.color = xAxisColor;

            position = CSGSlider1D.Do(camera, xAxisSlider, position, rotation * Vector3.right, size, ArrowHandleCap, snapMode, snapVertices, initFunction, shutdownFunction);

            Disabled = yAxisDisabled;

            Handles.color = yAxisColor;

            position = CSGSlider1D.Do(camera, yAxisSlider, position, rotation * Vector3.up, size, ArrowHandleCap, snapMode, snapVertices, initFunction, shutdownFunction);

            Disabled = zAxisDisabled;

            Handles.color = zAxisColor;

            position = CSGSlider1D.Do(camera, zAxisSlider, position, rotation * Vector3.forward, size, ArrowHandleCap, snapMode, snapVertices, initFunction, shutdownFunction);

            Disabled = xzPlaneDisabled;

            if(!Disabled)
            {
                Handles.color = xzPlaneColor;

                position = DoPlanarHandle(camera, xzPlaneSlider, PrincipleAxis2.XZ, position, rotation, size * 0.25f, snapMode, snapVertices, initFunction, shutdownFunction);
            }

            Disabled = xyPlaneDisabled;

            if(!Disabled)
            {
                Handles.color = xyPlaneColor;

                position = DoPlanarHandle(camera, xyPlaneSlider, PrincipleAxis2.XY, position, rotation, size * 0.25f, snapMode, snapVertices, initFunction, shutdownFunction);
            }

            Disabled = yzPlaneDisabled;

            if(!Disabled)
            {
                Handles.color = yzPlaneColor;

                position = DoPlanarHandle(camera, yzPlaneSlider, PrincipleAxis2.YZ, position, rotation, size * 0.25f, snapMode, snapVertices, initFunction, shutdownFunction);
            }

            Disabled      = prevDisabled;
            Handles.color = originalColor;

            return position;
        }

        public static float ScaleSlider(float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, bool snapping, float snap, InitFunction initFunction, InitFunction shutdownFunction)
        {
            return CSGScaleSlider.DoAxis(GUIUtility.GetControlID(ScaleSliderHash, FocusType.Keyboard), scale, position, direction, rotation, size, snapping, snap, initFunction, shutdownFunction);
        }

        public static Vector3 ScaleValueHandle(Vector3 value, Vector3 position, Quaternion rotation, float size, CapFunction capFunction, bool snapping, float snap, InitFunction initFunction, InitFunction shutdownFunction)
        {
            return CSGScaleSlider.DoCenter(GUIUtility.GetControlID(ScaleValueHandleHash, FocusType.Keyboard), value, position, rotation, size, capFunction, snapping, snap, initFunction, shutdownFunction);
        }

        public static Vector3 ScaleHandle(Vector3 scale, Vector3 position, Quaternion rotation, bool snapping, InitFunction initFunction, InitFunction shutdownFunction)
        {
            float size          = HandleUtility.GetHandleSize(position);
            Color originalColor = Handles.color;
            bool  isStatic      = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);

            bool prevDisabled = Disabled;

            Disabled = prevDisabled || CSGSettings.LockAxisX;

            Handles.color = isStatic ? Color.Lerp(Handles.xAxisColor, StaticColor, StaticBlend) : Handles.xAxisColor;

            GUI.SetNextControlName("xAxis");

            scale.x = ScaleSlider(scale.x, position, rotation * Vector3.right, rotation, size, snapping, CSGSettings.SnapScale, initFunction, shutdownFunction);

            Disabled = prevDisabled || CSGSettings.LockAxisY;

            Handles.color = isStatic ? Color.Lerp(Handles.yAxisColor, StaticColor, StaticBlend) : Handles.yAxisColor;

            GUI.SetNextControlName("yAxis");

            scale.y = ScaleSlider(scale.y, position, rotation * Vector3.up, rotation, size, snapping, CSGSettings.SnapScale, initFunction, shutdownFunction);

            Disabled = prevDisabled || CSGSettings.LockAxisZ;

            Handles.color = isStatic ? Color.Lerp(Handles.zAxisColor, StaticColor, StaticBlend) : Handles.zAxisColor;

            GUI.SetNextControlName("zAxis");

            scale.z = ScaleSlider(scale.z, position, rotation * Vector3.forward, rotation, size, snapping, CSGSettings.SnapScale, initFunction, shutdownFunction);

            Disabled      = prevDisabled;
            Handles.color = Handles.centerColor;

            scale = ScaleValueHandle(scale, position, rotation, size * 0.2f, CubeHandleCap, snapping, CSGSettings.SnapScale, initFunction, shutdownFunction);

            if(float.IsInfinity(scale.x) || float.IsNaN(scale.x) ||
               float.IsInfinity(scale.y) || float.IsNaN(scale.y) ||
               float.IsInfinity(scale.z) || float.IsNaN(scale.z))
            {
                scale = Vector3.zero;
            }

            Disabled      = prevDisabled;
            Handles.color = originalColor;

            return scale;
        }

        public static Quaternion Disc(Camera camera, Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane, bool snapping, float snap, InitFunction initFunction, InitFunction shutdownFunction = null)
        {
            return CSGDisc.Do(camera, GUIUtility.GetControlID(DiscHash, FocusType.Keyboard), null, rotation, position, axis, size, cutoffPlane, snapping, snap, initFunction, shutdownFunction);
        }

        public static Quaternion FreeRotateHandle(Camera camera, Quaternion rotation, Vector3 position, float size, bool snapping, InitFunction initFunction, InitFunction shutdownFunction = null)
        {
            return CSGFreeRotate.Do(camera, GUIUtility.GetControlID(FreeRotateHandleHash, FocusType.Keyboard), rotation, position, size, initFunction, shutdownFunction);
        }

        public static Quaternion DoRotationHandle(Camera camera, Quaternion rotation, Vector3 position, bool snapping, InitFunction initFunction, InitFunction shutdownFunction = null)
        {
            int hotControl   = GUIUtility.hotControl;
            int xAxisDiscID  = GUIUtility.GetControlID(DiscHash, FocusType.Keyboard);
            int yAxisDiscID  = GUIUtility.GetControlID(DiscHash, FocusType.Keyboard);
            int zAxisDiscID  = GUIUtility.GetControlID(DiscHash, FocusType.Keyboard);
            int freeDiscID   = GUIUtility.GetControlID(DiscHash, FocusType.Keyboard);
            int freeRotateID = GUIUtility.GetControlID(FreeRotateHandleHash, FocusType.Keyboard);

            bool haveControl = hotControl == xAxisDiscID
             || hotControl == yAxisDiscID
             || hotControl == zAxisDiscID
             || hotControl == freeDiscID
             || hotControl == freeRotateID;

            float size = HandleUtility.GetHandleSize(position);
            Color temp = Handles.color;

            bool isStatic = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);
            Handles.color = isStatic ? Color.Lerp(Handles.xAxisColor, StaticColor, StaticBlend) : Handles.xAxisColor;

            if(!haveControl || hotControl == xAxisDiscID)
            {
                rotation = CSGDisc.Do(camera, xAxisDiscID, "X", rotation, position, rotation * Vector3.right, size, true, snapping, CSGSettings.SnapRotation, initFunction, shutdownFunction);
            }

            Handles.color = isStatic ? Color.Lerp(Handles.yAxisColor, StaticColor, StaticBlend) : Handles.yAxisColor;

            if(!haveControl || hotControl == yAxisDiscID)
            {
                rotation = CSGDisc.Do(camera, yAxisDiscID, "Y", rotation, position, rotation * Vector3.up, size, true, snapping, CSGSettings.SnapRotation, initFunction, shutdownFunction);
            }

            Handles.color = isStatic ? Color.Lerp(Handles.zAxisColor, StaticColor, StaticBlend) : Handles.zAxisColor;

            if(!haveControl || hotControl == zAxisDiscID)
            {
                rotation = CSGDisc.Do(camera, zAxisDiscID, "Z", rotation, position, rotation * Vector3.forward, size, true, snapping, CSGSettings.SnapRotation, initFunction, shutdownFunction);
            }

            if(!isStatic)
            {
                Handles.color = Handles.centerColor;

                if(!haveControl || hotControl == freeDiscID)
                {
                    rotation = CSGDisc.Do(camera, freeDiscID, null, rotation, position, camera.transform.forward, size * 1.1f, false, snapping, 0, initFunction, shutdownFunction);
                }

                if(!haveControl || hotControl == freeRotateID)
                {
                    rotation = CSGFreeRotate.Do(camera, freeRotateID, rotation, position, size, initFunction, shutdownFunction);
                }
            }

            Handles.color = temp;

            return rotation;
        }

        internal static Vector3 DoPlanarHandle(Camera camera, PrincipleAxis2 planeID, Vector3 position, Quaternion rotation, float handleSize, SnapMode snapMode, Vector3[] snapVertices, InitFunction initFunction, InitFunction shutdownFunction = null)
        {
            int moveHandleHash = planeID switch
            {
                PrincipleAxis2.XZ => XZAxisMoveHandleHash
              , PrincipleAxis2.XY => XYAxisMoveHandleHash
              , PrincipleAxis2.YZ => YZAxisMoveHandleHash
              , var _             => 0
            };

            int id = GUIUtility.GetControlID(moveHandleHash, FocusType.Keyboard);

            return DoPlanarHandle(camera, id, planeID, position, rotation, handleSize, snapMode, snapVertices, initFunction, shutdownFunction);
        }

        internal static Vector3 DoPlanarHandle(Camera camera, int id, PrincipleAxis2 planeID, Vector3 position, Quaternion rotation, float handleSize, SnapMode snapMode, Vector3[] snapVertices, InitFunction initFunction, InitFunction shutdownFunction = null)
        {
            int  axis1Index = 0;
            int  axis2Index = 0;
            bool isStatic   = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);

            switch(planeID)
            {
                case PrincipleAxis2.XZ:
                {
                    axis1Index    = 0;
                    axis2Index    = 2;
                    Handles.color = isStatic ? StaticColor : Handles.yAxisColor;

                    break;
                }
                case PrincipleAxis2.XY:
                {
                    axis1Index    = 0;
                    axis2Index    = 1;
                    Handles.color = isStatic ? StaticColor : Handles.zAxisColor;

                    break;
                }
                case PrincipleAxis2.YZ:
                {
                    axis1Index    = 1;
                    axis2Index    = 2;
                    Handles.color = isStatic ? StaticColor : Handles.xAxisColor;

                    break;
                }
            }

            int   axisNormalIndex = 3 - axis2Index - axis1Index;
            Color prevColor       = Handles.color;

            Matrix4x4 handleTransform = Matrix4x4.TRS(position, rotation, Vector3.one);

            Vector3 cameraToTransformToolVector = handleTransform.inverse.MultiplyPoint(camera.transform.position).normalized;

            if(Mathf.Abs(cameraToTransformToolVector[axisNormalIndex]) < 0.05f && GUIUtility.hotControl != id)
            {
                Handles.color = prevColor;

                return position;
            }

            if(GUIUtility.hotControl == 0)
            {
                m_PlanarHandlesOctant[axis1Index] = cameraToTransformToolVector[axis1Index] < -0.01f ? -1 : 1;
                m_PlanarHandlesOctant[axis2Index] = cameraToTransformToolVector[axis2Index] < -0.01f ? -1 : 1;
            }

            Vector3 handleOffset = m_PlanarHandlesOctant;
            handleOffset[axisNormalIndex] = 0;
            handleOffset                  = rotation * (handleOffset * handleSize * 0.5f);

            Vector3 axis1      = Vector3.zero;
            Vector3 axis2      = Vector3.zero;
            Vector3 axisNormal = Vector3.zero;
            axis1[axis1Index]           = 1;
            axis2[axis2Index]           = 1;
            axisNormal[axisNormalIndex] = 1;
            axis1                       = rotation * axis1;
            axis2                       = rotation * axis2;
            axisNormal                  = rotation * axisNormal;

            vertices[0] = position + handleOffset + (axis1 + axis2) * handleSize * 0.5f;
            vertices[1] = position + handleOffset + (-axis1 + axis2) * handleSize * 0.5f;
            vertices[2] = position + handleOffset + (-axis1 - axis2) * handleSize * 0.5f;
            vertices[3] = position + handleOffset + (axis1 - axis2) * handleSize * 0.5f;

            Color innerColor = Handles.color;
            Color outerColor = Color.black;
            innerColor = new Color(innerColor.r, innerColor.g, innerColor.b, 0.1f);

            if(Disabled)
            {
                innerColor = Handles.secondaryColor;
                outerColor = innerColor;
            }

            Handles.DrawSolidRectangleWithOutline(vertices, innerColor, outerColor);

            position = Slider2D(camera, id, position, handleOffset, axisNormal, axis1, axis2, handleSize * 0.5f, RectangleHandleCap, snapMode, snapVertices, initFunction, shutdownFunction);

            Handles.color = prevColor;

            return position;
        }
    }
}

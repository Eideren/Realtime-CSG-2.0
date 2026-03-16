using UnityEngine;
using UnityEditor;

namespace RealtimeCSG.Helpers
{
    public sealed partial class CSGHandles
    {
        public delegate void InitFunction();

        public delegate void CapFunction(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        );

        private static Vector3[] s_Vertices = new Vector3[4];
        private static Vector3[] s_Points = new Vector3[5];
        private static Vector3 s_PlanarHandlesOctant = Vector3.one;

        internal static Color staticColor = new Color(.5f, .5f, .5f, 0f);
        internal static float staticBlend = 0.6f;

        internal static int s_xAxisMoveHandleHash = "xAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_yAxisMoveHandleHash = "yAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_zAxisMoveHandleHash = "zAxisFreeMoveHandleHash".GetHashCode();
        internal static int s_xzAxisMoveHandleHash = "xzAxesFreeMoveHandleHash".GetHashCode();
        internal static int s_xyAxisMoveHandleHash = "xyAxesFreeMoveHandleHash".GetHashCode();
        internal static int s_yzAxisMoveHandleHash = "yzAxesFreeMoveHandleHash".GetHashCode();
        internal static int s_centerMoveHandleHash = "centerFreeMoveHandleHash".GetHashCode();

        internal static int s_ScaleSliderHash = "ScaleSliderHash".GetHashCode();
        internal static int s_ScaleValueHandleHash = "ScaleValueHandleHash".GetHashCode();
        internal static int s_DiscHash = "DiscHash".GetHashCode();
        internal static int s_FreeRotateHandleHash = "FreeRotateHandleHash".GetHashCode();

        internal static int s_SliderHash = "SliderHash".GetHashCode();

        public static void CubeHandleCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
        {
#if UNITY_5_6_OR_NEWER
            Handles.CubeHandleCap(controlID, position, rotation, size, eventType);
#else
			switch (eventType)
			{
				case EventType.Layout:
				{
					HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * .1f));
					break;
				}
				case EventType.Repaint:
				{ 
					var direction = rotation * Vector3.forward;
					Handles.CubeCap(controlID, position, Quaternion.LookRotation(direction), size * .2f);
					break;
				}
			}
#endif
        }

        public static void DiamondDotCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
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
                    Vector3 direction = rotation * Vector3.forward;
                    PaintUtility.DiamondDotCap(controlID, position, Quaternion.LookRotation(direction), size * .2f);

                    break;
                }
            }
        }

        public static void SquareDotCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
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
                    Vector3 direction = rotation * Vector3.forward;
                    PaintUtility.SquareDotCap(controlID, position, Quaternion.LookRotation(direction), size * .2f);

                    break;
                }
            }
        }

        public static void CircleDotCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
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
                    Vector3 direction = rotation * Vector3.forward;
                    PaintUtility.CircleDotCap(controlID, position, Quaternion.LookRotation(direction), size * .2f);

                    break;
                }
            }
        }

        public static void DotHandleCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
        {
#if UNITY_5_6_OR_NEWER
            Handles.DotHandleCap(controlID, position, rotation, size, eventType);
#else
			switch (eventType)
			{
				case EventType.Layout:
				{
					HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * .1f));
					break;
				}
				case EventType.Repaint:
				{ 
					var direction = rotation * Vector3.forward;
					Handles.DotCap(controlID, position, Quaternion.LookRotation(direction), size * .2f);
					break;
				}
			}
#endif
        }

        public static void ArrowHandleCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
        {
#if UNITY_5_6_OR_NEWER
            Handles.ArrowHandleCap(controlID, position, rotation, size, eventType);
#else
			switch (eventType)
			{
				case EventType.Layout:
				{
					var direction = rotation * Vector3.forward;
					HandleUtility.AddControl (controlID, HandleUtility.DistanceToLine (position, position + direction * size * .9f));
					HandleUtility.AddControl (controlID, HandleUtility.DistanceToCircle (position + direction * size, size * .2f));
					break;
				}
				case EventType.Repaint:
				{
					var direction = rotation * Vector3.forward;
					ConeHandleCap (controlID, position + direction * size, Quaternion.LookRotation (direction), size * .2f, eventType);
					Handles.DrawLine (position, position + direction * size * .9f);
					break;
				}
			}
#endif
        }

        public static void HoverArrowHandleCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
        {
#if UNITY_5_6_OR_NEWER
            Handles.ArrowHandleCap(controlID, position, rotation, size, eventType);
#else
			switch (eventType)
			{
				case EventType.Layout:
				{
					//var direction = rotation * Vector3.forward;
					//HandleUtility.AddControl(controlID, HandleUtility.DistanceToLine(position, position + direction * size * .9f));
					//HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position + direction * size, size * .2f));
					HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * .1f));
					break;
				}
				case EventType.Repaint:
				{
					if (HandleUtility.nearestControl == controlID ||
						GUIUtility.hotControl == controlID)
					{
						var direction = rotation * Vector3.forward;
						ConeHandleCap(controlID, position + direction * size, Quaternion.LookRotation(direction), size * .2f, eventType);
						Handles.DrawLine(position, position + direction * size * .9f);
					}
					break;
				}
			}
#endif
        }

        public static void ConeHandleCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
        {
#if UNITY_5_6_OR_NEWER
            Handles.ConeHandleCap(controlID, position, rotation, size, eventType);
#else
			switch (eventType)
			{
				case (EventType.Layout):
					HandleUtility.AddControl (controlID, HandleUtility.DistanceToCircle (position, size));
					break;
				case (EventType.Repaint):
					Handles.ConeCap(controlID, position, rotation, size);
					break;
			}
#endif
        }

        internal static float DistanceToRectangleInternal(Vector3 position, Quaternion rotation, Vector2 size)
        {
            Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
            Vector3 up       = rotation * new Vector3(0, size.y, 0);
            s_Points[0] = HandleUtility.WorldToGUIPoint(position + sideways + up);
            s_Points[1] = HandleUtility.WorldToGUIPoint(position + sideways - up);
            s_Points[2] = HandleUtility.WorldToGUIPoint(position - sideways - up);
            s_Points[3] = HandleUtility.WorldToGUIPoint(position - sideways + up);
            s_Points[4] = s_Points[0];

            Vector2 pos      = Event.current.mousePosition;
            bool    oddNodes = false;
            int     j        = 4;

            for(int i = 0; i < 5; i++)
            {
                if(s_Points[i].y > pos.y != s_Points[j].y > pos.y)
                {
                    if(pos.x < (s_Points[j].x - s_Points[i].x) * (pos.y - s_Points[i].y)
                     / (s_Points[j].y - s_Points[i].y) + s_Points[i].x)
                    {
                        oddNodes = !oddNodes;
                    }
                }

                j = i;
            }

            if(!oddNodes)
            {
                float dist, closestDist = -1f;
                j = 1;

                for(int i = 0; i < 4; i++)
                {
                    dist = HandleUtility.DistancePointToLineSegment(pos, s_Points[i], s_Points[j++]);

                    if(dist < closestDist || closestDist < 0)
                    {
                        closestDist = dist;
                    }
                }

                return closestDist;
            }
            else
            {
                return 0;
            }
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

        private static Vector3[] s_RectangleHandlePointsCache = new Vector3[5];

        public static void RectangleHandleCap(
            int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType
        )
        {
            RectangleHandleCap(controlID, position, rotation, new Vector2(size, size), eventType);
        }

        internal static void RectangleHandleCap(
            int controlID, Vector3 position, Quaternion rotation, Vector2 size, EventType eventType
        )
        {
            switch(eventType)
            {
                case EventType.Layout:
                    HandleUtility.AddControl(controlID, DistanceToRectangleInternal(position, rotation, size));

                    break;
                case EventType.Repaint:
                    Vector3 sideways = rotation * new Vector3(size.x, 0, 0);
                    Vector3 up       = rotation * new Vector3(0, size.y, 0);
                    s_RectangleHandlePointsCache[0] = position + sideways + up;
                    s_RectangleHandlePointsCache[1] = position + sideways - up;
                    s_RectangleHandlePointsCache[2] = position - sideways - up;
                    s_RectangleHandlePointsCache[3] = position - sideways + up;
                    s_RectangleHandlePointsCache[4] = position + sideways + up;
                    Handles.DrawPolyLine(s_RectangleHandlePointsCache);

                    break;
            }
        }

        public static Vector3 Slider(
            Camera camera, Vector3 position, Vector3 direction, float size, CapFunction capFunction, SnapMode snapMode
          , Vector3[] snapVertices, InitFunction initFunction, InitFunction shutdownFunction
        )
        {
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Keyboard);

            return CSGSlider1D.Do(
                camera, id, position, direction, size, capFunction, snapMode, snapVertices, initFunction
              , shutdownFunction
            );
        }

        public static Vector3 Slider2D(
            Camera camera, int id, Vector3 handlePos, Vector3 offset, Vector3 handleDir, Vector3 slideDir1
          , Vector3 slideDir2, float handleSize, CapFunction capFunction, SnapMode snapMode, Vector3[] snapVertices
          , InitFunction initFunction, InitFunction shutdownFunction
        )
        {
            return CSGSlider2D.Do(
                camera, id, handlePos, offset, handleDir, slideDir1, slideDir2, handleSize, capFunction, snapMode
              , snapVertices, initFunction, shutdownFunction
            );
        }

        internal static AABB Box(Camera camera, AABB bounds, Quaternion rotation, bool showEdgePoints = true)
        {
            return CSGBounds.Do(camera, bounds, rotation, showEdgePoints);
        }

        internal static bool disabled = false;

        public static int FocusControl
        {
            get
            {
                if(!GUI.enabled || (Tools.viewTool != ViewTool.None && Tools.viewTool != ViewTool.Pan))
                {
                    return 0;
                }

                int hotControl = GUIUtility.hotControl;

                if(hotControl == 0)
                {
                    return HandleUtility.nearestControl;
                }

                return GUIUtility.keyboardControl;
            }
        }

        public static Color ToActiveColorSpace(Color color)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
        }

        public static Color preselectionColor = new Color(201f / 255, 200f / 255, 144f / 255, 0.89f);

        public static Color StateColor(
            Color color, bool isDisabled = false, bool isSelected = false, bool isPreSelected = false
        )
        {
            return ToActiveColorSpace(
                isDisabled || disabled ? Color.Lerp(color, staticColor, staticBlend) :
                isSelected ? Handles.selectedColor :
                isPreSelected ? preselectionColor : color
            );
        }

        public static Vector3 PositionHandle(
            Camera camera, Vector3 position, Quaternion rotation, SnapMode snapMode, Vector3[] snapVertices = null
          , InitFunction initFunction = null, InitFunction shutdownFunction = null
        )
        {
            GUI.SetNextControlName("xAxis");
            int xAxisSlider = GUIUtility.GetControlID(s_xAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("yAxis");
            int yAxisSlider = GUIUtility.GetControlID(s_yAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("zAxis");
            int zAxisSlider = GUIUtility.GetControlID(s_zAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("xzPlane");
            int xzPlaneSlider = GUIUtility.GetControlID(s_xzAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("xyPlane");
            int xyPlaneSlider = GUIUtility.GetControlID(s_xyAxisMoveHandleHash, FocusType.Passive);
            GUI.SetNextControlName("yzPlane");
            int yzPlaneSlider = GUIUtility.GetControlID(s_yzAxisMoveHandleHash, FocusType.Passive);

            bool isStatic     = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);
            bool prevDisabled = disabled;

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

            bool xAxisDisabled = isStatic || prevDisabled || lockedAxisX
             || (isControlHot && !xAxisIsHot && !xzAxisIsHot && !xyAxisIsHot);

            bool yAxisDisabled = isStatic || prevDisabled || lockedAxisY
             || (isControlHot && !yAxisIsHot && !xyAxisIsHot && !yzAxisIsHot);

            bool zAxisDisabled = isStatic || prevDisabled || lockedAxisZ
             || (isControlHot && !zAxisIsHot && !xzAxisIsHot && !yzAxisIsHot);

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

            disabled = xAxisDisabled;

            {
                Handles.color = xAxisColor;

                position = CSGSlider1D.Do(
                    camera, xAxisSlider, position, rotation * Vector3.right, size, ArrowHandleCap, snapMode
                  , snapVertices, initFunction, shutdownFunction
                );
            }

            disabled = yAxisDisabled;

            {
                Handles.color = yAxisColor;

                position = CSGSlider1D.Do(
                    camera, yAxisSlider, position, rotation * Vector3.up, size, ArrowHandleCap, snapMode, snapVertices
                  , initFunction, shutdownFunction
                );
            }

            disabled = zAxisDisabled;

            {
                Handles.color = zAxisColor;

                position = CSGSlider1D.Do(
                    camera, zAxisSlider, position, rotation * Vector3.forward, size, ArrowHandleCap, snapMode
                  , snapVertices, initFunction, shutdownFunction
                );
            }

            disabled = xzPlaneDisabled;

            if(!disabled)
            {
                Handles.color = xzPlaneColor;

                position = DoPlanarHandle(
                    camera, xzPlaneSlider, PrincipleAxis2.XZ, position, rotation, size * 0.25f, snapMode, snapVertices
                  , initFunction, shutdownFunction
                );
            }

            disabled = xyPlaneDisabled;

            if(!disabled)
            {
                Handles.color = xyPlaneColor;

                position = DoPlanarHandle(
                    camera, xyPlaneSlider, PrincipleAxis2.XY, position, rotation, size * 0.25f, snapMode, snapVertices
                  , initFunction, shutdownFunction
                );
            }

            disabled = yzPlaneDisabled;

            if(!disabled)
            {
                Handles.color = yzPlaneColor;

                position = DoPlanarHandle(
                    camera, yzPlaneSlider, PrincipleAxis2.YZ, position, rotation, size * 0.25f, snapMode, snapVertices
                  , initFunction, shutdownFunction
                );
            }

            disabled      = prevDisabled;
            Handles.color = originalColor;

            return position;
        }

        public static float ScaleSlider(
            float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, bool snapping, float snap
          , InitFunction initFunction, InitFunction shutdownFunction
        )
        {
            int id = GUIUtility.GetControlID(s_ScaleSliderHash, FocusType.Keyboard);

            return CSGScaleSlider.DoAxis(
                id, scale, position, direction, rotation, size, snapping, snap, initFunction, shutdownFunction
            );
        }

        public static Vector3 ScaleValueHandle(
            Vector3 value, Vector3 position, Quaternion rotation, float size, CapFunction capFunction, bool snapping
          , float snap, InitFunction initFunction, InitFunction shutdownFunction
        )
        {
            int id = GUIUtility.GetControlID(s_ScaleValueHandleHash, FocusType.Keyboard);

            return CSGScaleSlider.DoCenter(
                id, value, position, rotation, size, capFunction, snapping, snap, initFunction, shutdownFunction
            );
        }

        public static Vector3 ScaleHandle(
            Vector3 scale, Vector3 position, Quaternion rotation, bool snapping, InitFunction initFunction
          , InitFunction shutdownFunction
        )
        {
            float size          = HandleUtility.GetHandleSize(position);
            Color originalColor = Handles.color;
            bool  isStatic      = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);

            bool prevDisabled = disabled;

            disabled = prevDisabled || CSGSettings.LockAxisX;

            {
                Handles.color =
                    isStatic ? Color.Lerp(Handles.xAxisColor, staticColor, staticBlend) : Handles.xAxisColor;

                GUI.SetNextControlName("xAxis");

                scale.x = ScaleSlider(
                    scale.x, position, rotation * Vector3.right, rotation, size, snapping, CSGSettings.SnapScale
                  , initFunction, shutdownFunction
                );
            }

            disabled = prevDisabled || CSGSettings.LockAxisY;

            {
                Handles.color =
                    isStatic ? Color.Lerp(Handles.yAxisColor, staticColor, staticBlend) : Handles.yAxisColor;

                GUI.SetNextControlName("yAxis");

                scale.y = ScaleSlider(
                    scale.y, position, rotation * Vector3.up, rotation, size, snapping, CSGSettings.SnapScale
                  , initFunction, shutdownFunction
                );
            }

            disabled = prevDisabled || CSGSettings.LockAxisZ;

            {
                Handles.color =
                    isStatic ? Color.Lerp(Handles.zAxisColor, staticColor, staticBlend) : Handles.zAxisColor;

                GUI.SetNextControlName("zAxis");

                scale.z = ScaleSlider(
                    scale.z, position, rotation * Vector3.forward, rotation, size, snapping, CSGSettings.SnapScale
                  , initFunction, shutdownFunction
                );
            }

            disabled      = prevDisabled;
            Handles.color = Handles.centerColor;

            scale = ScaleValueHandle(
                scale, position, rotation, size * 0.2f, CubeHandleCap, snapping, CSGSettings.SnapScale, initFunction
              , shutdownFunction
            );

            if(float.IsInfinity(scale.x) || float.IsNaN(scale.x) ||
               float.IsInfinity(scale.y) || float.IsNaN(scale.y) ||
               float.IsInfinity(scale.z) || float.IsNaN(scale.z))
            {
                scale = Vector3.zero;
            }

            disabled      = prevDisabled;
            Handles.color = originalColor;

            return scale;
        }

        public static Quaternion Disc(
            Camera camera, Quaternion rotation, Vector3 position, Vector3 axis, float size, bool cutoffPlane
          , bool snapping, float snap, InitFunction initFunction, InitFunction shutdownFunction = null
        )
        {
            int id = GUIUtility.GetControlID(s_DiscHash, FocusType.Keyboard);

            return CSGDisc.Do(
                camera, id, null, rotation, position, axis, size, cutoffPlane, snapping, snap, initFunction
              , shutdownFunction
            );
        }

        public static Quaternion FreeRotateHandle(
            Camera camera, Quaternion rotation, Vector3 position, float size, bool snapping, InitFunction initFunction
          , InitFunction shutdownFunction = null
        )
        {
            int id = GUIUtility.GetControlID(s_FreeRotateHandleHash, FocusType.Keyboard);

            return CSGFreeRotate.Do(camera, id, rotation, position, size, snapping, initFunction, shutdownFunction);
        }

        public static Quaternion DoRotationHandle(
            Camera camera, Quaternion rotation, Vector3 position, bool snapping, InitFunction initFunction
          , InitFunction shutdownFunction = null
        )
        {
            int hotControl   = EditorGUIUtility.hotControl;
            int xAxisDiscID  = GUIUtility.GetControlID(s_DiscHash, FocusType.Keyboard);
            int yAxisDiscID  = GUIUtility.GetControlID(s_DiscHash, FocusType.Keyboard);
            int zAxisDiscID  = GUIUtility.GetControlID(s_DiscHash, FocusType.Keyboard);
            int freeDiscID   = GUIUtility.GetControlID(s_DiscHash, FocusType.Keyboard);
            int freeRotateID = GUIUtility.GetControlID(s_FreeRotateHandleHash, FocusType.Keyboard);

            bool haveControl = hotControl == xAxisDiscID ||
                hotControl == yAxisDiscID ||
                hotControl == zAxisDiscID ||
                hotControl == freeDiscID ||
                hotControl == freeRotateID;

            float size = HandleUtility.GetHandleSize(position);
            Color temp = Handles.color;

            bool isStatic = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);
            Handles.color = isStatic ? Color.Lerp(Handles.xAxisColor, staticColor, staticBlend) : Handles.xAxisColor;

            if(!haveControl || hotControl == xAxisDiscID)
            {
                rotation = CSGDisc.Do(
                    camera, xAxisDiscID, "X", rotation, position, rotation * Vector3.right, size, true, snapping
                  , CSGSettings.SnapRotation, initFunction, shutdownFunction
                );
            }

            Handles.color = isStatic ? Color.Lerp(Handles.yAxisColor, staticColor, staticBlend) : Handles.yAxisColor;

            if(!haveControl || hotControl == yAxisDiscID)
            {
                rotation = CSGDisc.Do(
                    camera, yAxisDiscID, "Y", rotation, position, rotation * Vector3.up, size, true, snapping
                  , CSGSettings.SnapRotation, initFunction, shutdownFunction
                );
            }

            Handles.color = isStatic ? Color.Lerp(Handles.zAxisColor, staticColor, staticBlend) : Handles.zAxisColor;

            if(!haveControl || hotControl == zAxisDiscID)
            {
                rotation = CSGDisc.Do(
                    camera, zAxisDiscID, "Z", rotation, position, rotation * Vector3.forward, size, true, snapping
                  , CSGSettings.SnapRotation, initFunction, shutdownFunction
                );
            }

            if(!isStatic)
            {
                Handles.color = Handles.centerColor;

                if(!haveControl || hotControl == freeDiscID)
                {
                    rotation = CSGDisc.Do(
                        camera, freeDiscID, null, rotation, position, camera.transform.forward, size * 1.1f, false
                      , snapping, 0, initFunction, shutdownFunction
                    );
                }

                if(!haveControl || hotControl == freeRotateID)
                {
                    rotation = CSGFreeRotate.Do(
                        camera, freeRotateID, rotation, position, size, snapping, initFunction, shutdownFunction
                    );
                }
            }

            Handles.color = temp;

            return rotation;
        }

        internal static Vector3 DoPlanarHandle(
            Camera camera, PrincipleAxis2 planeID, Vector3 position, Quaternion rotation, float handleSize
          , SnapMode snapMode, Vector3[] snapVertices, InitFunction initFunction, InitFunction shutdownFunction = null
        )
        {
            int moveHandleHash = 0;

            switch(planeID)
            {
                case PrincipleAxis2.XZ:
                {
                    moveHandleHash = s_xzAxisMoveHandleHash;

                    break;
                }
                case PrincipleAxis2.XY:
                {
                    moveHandleHash = s_xyAxisMoveHandleHash;

                    break;
                }
                case PrincipleAxis2.YZ:
                {
                    moveHandleHash = s_yzAxisMoveHandleHash;

                    break;
                }
            }

            int id = GUIUtility.GetControlID(moveHandleHash, FocusType.Keyboard);

            return DoPlanarHandle(
                camera, id, planeID, position, rotation, handleSize, snapMode, snapVertices, initFunction
              , shutdownFunction
            );
        }

        internal static Vector3 DoPlanarHandle(
            Camera camera, int id, PrincipleAxis2 planeID, Vector3 position, Quaternion rotation, float handleSize
          , SnapMode snapMode, Vector3[] snapVertices, InitFunction initFunction, InitFunction shutdownFunction = null
        )
        {
            int  axis1index = 0;
            int  axis2index = 0;
            bool isStatic   = !Tools.hidden && EditorApplication.isPlaying && ContainsStatic(Selection.gameObjects);

            switch(planeID)
            {
                case PrincipleAxis2.XZ:
                {
                    axis1index    = 0;
                    axis2index    = 2;
                    Handles.color = isStatic ? staticColor : Handles.yAxisColor;

                    break;
                }
                case PrincipleAxis2.XY:
                {
                    axis1index    = 0;
                    axis2index    = 1;
                    Handles.color = isStatic ? staticColor : Handles.zAxisColor;

                    break;
                }
                case PrincipleAxis2.YZ:
                {
                    axis1index    = 1;
                    axis2index    = 2;
                    Handles.color = isStatic ? staticColor : Handles.xAxisColor;

                    break;
                }
            }

            int   axisNormalIndex = 3 - axis2index - axis1index;
            Color prevColor       = Handles.color;

            Matrix4x4 handleTransform = Matrix4x4.TRS(position, rotation, Vector3.one);

            Vector3 cameraToTransformToolVector =
                handleTransform.inverse.MultiplyPoint(camera.transform.position).normalized;

            if(Mathf.Abs(cameraToTransformToolVector[axisNormalIndex]) < 0.05f && GUIUtility.hotControl != id)
            {
                Handles.color = prevColor;

                return position;
            }

            if(EditorGUIUtility.hotControl == 0)
            {
                s_PlanarHandlesOctant[axis1index] = cameraToTransformToolVector[axis1index] < -0.01f ? -1 : 1;
                s_PlanarHandlesOctant[axis2index] = cameraToTransformToolVector[axis2index] < -0.01f ? -1 : 1;
            }

            Vector3 handleOffset = s_PlanarHandlesOctant;
            handleOffset[axisNormalIndex] = 0;
            handleOffset                  = rotation * (handleOffset * handleSize * 0.5f);

            Vector3 axis1      = Vector3.zero;
            Vector3 axis2      = Vector3.zero;
            Vector3 axisNormal = Vector3.zero;
            axis1[axis1index]           = 1;
            axis2[axis2index]           = 1;
            axisNormal[axisNormalIndex] = 1;
            axis1                       = rotation * axis1;
            axis2                       = rotation * axis2;
            axisNormal                  = rotation * axisNormal;

            s_Vertices[0] = position + handleOffset + (axis1 + axis2) * handleSize * 0.5f;
            s_Vertices[1] = position + handleOffset + (-axis1 + axis2) * handleSize * 0.5f;
            s_Vertices[2] = position + handleOffset + (-axis1 - axis2) * handleSize * 0.5f;
            s_Vertices[3] = position + handleOffset + (axis1 - axis2) * handleSize * 0.5f;

            Color innerColor = Handles.color;
            Color outerColor = Color.black;
            innerColor = new Color(innerColor.r, innerColor.g, innerColor.b, 0.1f);

            if(disabled)
            {
                innerColor = Handles.secondaryColor;
                outerColor = innerColor;
            }

            Handles.DrawSolidRectangleWithOutline(s_Vertices, innerColor, outerColor);

            position = Slider2D(
                camera, id, position, handleOffset, axisNormal, axis1, axis2, handleSize * 0.5f, RectangleHandleCap
              , snapMode, snapVertices, initFunction, shutdownFunction
            );

            Handles.color = prevColor;

            return position;
        }
    }
}

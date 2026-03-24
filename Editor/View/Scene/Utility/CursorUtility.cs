using InternalRealtimeCSG;
using UnityEditor;
using UnityEngine;

namespace RealtimeCSG
{
    internal static class CursorUtility
    {
        private static readonly MouseCursor[] segmentCursors = { MouseCursor.ResizeVertical, MouseCursor.ResizeUpRight, MouseCursor.ResizeHorizontal, MouseCursor.ResizeUpLeft, MouseCursor.ResizeVertical, MouseCursor.ResizeUpRight, MouseCursor.ResizeHorizontal, MouseCursor.ResizeUpLeft };

        public static MouseCursor GetCursorForDirection(Matrix4x4 matrix, Vector3 center, Vector3 direction, float angleOffset = 0)
        {
            Vector3 worldCenterPoint1 = matrix.MultiplyPoint(center);
            Vector3 worldCenterPoint2 = worldCenterPoint1 + matrix.MultiplyVector(direction * 10.0f);

            Vector2 guiPoint1 = HandleUtility.WorldToGUIPoint(worldCenterPoint1);
            Vector2 guiPoint2 = HandleUtility.WorldToGUIPoint(worldCenterPoint2);
            Vector2 delta     = (guiPoint2 - guiPoint1).normalized;

            return GetCursorForDirection(delta, angleOffset);
        }

        public static MouseCursor GetCursorForDirection(Vector2 direction, float angleOffset = 0)
        {
            const float segmentAngle = 360 / 8.0f;

            float angle   = (360 + (GeometryUtility.SignedAngle(MathConstants.upVector2, direction) + 180 + angleOffset)) % 360;
            int   segment = Mathf.FloorToInt((angle / segmentAngle + 0.5f) % 8.0f);

            return segmentCursors[segment];
        }

        public static MouseCursor GetCursorForEdge(Vector2 direction)
        {
            const float segmentAngle = 360 / 8.0f;

            float angle   = (360 + (GeometryUtility.SignedAngle(MathConstants.upVector2, direction) + 180)) % 360;
            int   segment = Mathf.FloorToInt((angle / segmentAngle + 2.5f) % 8.0f);

            return segmentCursors[segment];
        }

        public static MouseCursor GetToolCursor()
        {
            return Tools.current switch
            {
                Tool.Move   => MouseCursor.MoveArrow
              , Tool.Rotate => MouseCursor.RotateArrow
              , Tool.Scale  => MouseCursor.ScaleArrow
              , Tool.Rect   => MouseCursor.SlideArrow
              , Tool.View   => MouseCursor.Orbit
              , var _       => MouseCursor.Arrow
            };
        }

        public static MouseCursor GetSelectionCursor(SelectionType selectionType)
        {
            return selectionType switch
            {
                SelectionType.Additive    => MouseCursor.ArrowPlus
              , SelectionType.Subtractive => MouseCursor.ArrowMinus
              , var _                     => MouseCursor.Arrow
            };
        }
    }
}

using UnityEngine;

namespace InternalRealtimeCSG
{
    public static class MathConstants
    {
        public const float ANGLE_EPSILON = 0.05f;
        public const float DISTANCE_EPSILON = 0.00015f;
        public const float EQUALITY_EPSILON = 0.0001f;
        public const float EQUALITY_EPSILON_SQR = EQUALITY_EPSILON * EQUALITY_EPSILON; // + (0*0) + (0*0)
        public const float ALIGNMENT_TEST_EPSILON = 0.000001f;
        public const float NORMAL_EPSILON = 0.0001f;
        public const float MINIMUM_SCALE = 0.00001f;

        public const float CONSIDERED_ZERO = 0.001f;
        public const float MINIMUM_MOUSE_MOVEMENT = 2.0f;

        public static readonly Vector3 zeroVector3 = Vector3.zero;
        public static readonly Vector3 oneVector3 = Vector3.one;

        public static readonly Vector3 upVector3 = Vector3.up;
        public static readonly Vector3 forwardVector3 = Vector3.forward;
        public static readonly Vector3 rightVector3 = Vector3.right;
        public static readonly Vector3 leftVector3 = Vector3.left;
        public static readonly Vector3 downVector3 = Vector3.down;
        public static readonly Vector3 backVector3 = Vector3.back;
        public static readonly Vector3 naNVector3 = new Vector3(float.NaN, float.NaN, float.NaN);

        public static readonly Vector3 positiveInfinityVector3 = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        public static readonly Vector3 negativeInfinityVector3 = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        public static readonly Vector2 zeroVector2 = Vector2.zero;
        public static readonly Vector2 oneVector2 = Vector2.one;
        public static readonly Vector2 upVector2 = Vector2.up;

        public static readonly Matrix4x4 identityMatrix = Matrix4x4.identity;
        public static readonly Quaternion identityQuaternion = Quaternion.identity;
    }
}

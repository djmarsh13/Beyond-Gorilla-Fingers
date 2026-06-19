using UnityEngine;

namespace BeyondGorillaFingers.Physics;

internal sealed class FingerPhysicsSettings
{
    private readonly float[] maxBendAngles =
    [
            58f,
            68f,
            76f,
    ];

    internal const int CollisionIterations = 2;

    internal const int   ConstraintIterations  = 7;
    internal const float ContactOffset         = 0.0015f;
    internal const float FollowStrength        = 145f;
    internal const float HandMotionInheritance = 0.72f;
    internal const float ParentFlex            = 0.22f;
    internal const float PostSolveDamping      = 0.82f;
    internal const float Radius                = 0.012f;
    internal const float SnapDistance          = 0.18f;
    internal const float VelocityDamping       = 20f;
    internal const float VisualSharpness       = 38f;

    internal float GetMaxBendAngle(int segment)
    {
        if (segment < 0)
            return maxBendAngles[0];

        return segment >= maxBendAngles.Length ? maxBendAngles[^1] : maxBendAngles[segment];
    }

    internal static float DampAlpha(float sharpness, float deltaTime) => 1f - Mathf.Exp(-sharpness * deltaTime);
}
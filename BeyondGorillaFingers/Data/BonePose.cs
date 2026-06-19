using UnityEngine;

namespace BeyondGorillaFingers.Data;

internal struct BonePose
{
    internal Vector3    Position;
    internal Quaternion Rotation;
    internal Vector3    LocalPosition;
    internal Quaternion LocalRotation;

    internal static BonePose Capture(Transform bone) =>
            new()
            {
                    Position      = bone.position,
                    Rotation      = bone.rotation,
                    LocalPosition = bone.localPosition,
                    LocalRotation = bone.localRotation,
            };

    internal void ApplyLocal(Transform bone)
    {
        bone.localPosition = LocalPosition;
        bone.localRotation = LocalRotation;
    }
}
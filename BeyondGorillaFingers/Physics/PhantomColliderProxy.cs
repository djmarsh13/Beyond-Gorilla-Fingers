using BeyondGorillaFingers.Data;
using UnityEngine;

namespace BeyondGorillaFingers.Physics;

internal sealed class PhantomColliderProxy : MonoBehaviour
{
    private  CapsuleCollider capsule;
    private  Transform       end;
    private  float           radius;
    private  Transform       start;
    internal Collider        Collider { get; private set; }
    internal PhantomOwner    Owner    { get; private set; }

    internal void SetupSphere(SphereCollider sphere, PhantomOwner owner)
    {
        Collider = sphere;
        Owner    = owner;
    }

    internal void SetupCapsule(CapsuleCollider capsuleCollider, Transform    startBone, Transform endBone,
                               float           capsuleRadius,   PhantomOwner owner)
    {
        capsule  = capsuleCollider;
        Collider = capsuleCollider;
        start    = startBone;
        end      = endBone;
        radius   = capsuleRadius;
        Owner    = owner;

        Align();
    }

    internal void Align()
    {
        if (capsule == null || start == null || end == null)
            return;

        Vector3 delta  = end.position - start.position;
        float   length = delta.magnitude;

        if (length <= 0.0001f)
            return;

        transform.position = start.position + delta * 0.5f;
        transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);

        capsule.direction = 2;
        capsule.radius    = radius;
        capsule.height    = length + radius * 2f;
    }
}
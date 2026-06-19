using UnityEngine;
using Object = UnityEngine.Object;

namespace BeyondGorillaFingers.Physics;

internal sealed class FingerParticle
{
    internal readonly SphereCollider Collider;
    internal readonly GameObject     GameObject;

    internal Vector3 Position;
    internal Vector3 PreviousPosition;
    internal Vector3 Velocity;

    internal FingerParticle(string name, Vector3 position, float radius)
    {
        GameObject       = new GameObject(name)
        {
                layer = 10,
        };

        Collider           = GameObject.AddComponent<SphereCollider>();
        Collider.radius    = radius;
        Collider.isTrigger = true;

        Position         = position;
        PreviousPosition = position;
        Velocity         = Vector3.zero;

        SyncTransform();
    }

    internal void SyncTransform()
    {
        GameObject.transform.position = Position;
        GameObject.transform.rotation = Quaternion.identity;
    }

    internal void Dispose()
    {
        if (GameObject != null)
            Object.Destroy(GameObject);
    }
}
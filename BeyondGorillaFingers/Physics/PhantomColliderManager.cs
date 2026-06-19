using System.Collections.Generic;
using System.Linq;
using BeyondGorillaFingers.Data;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BeyondGorillaFingers.Physics;

internal sealed class PhantomColliderManager
{
    internal readonly List<PhantomColliderProxy> Colliders = [];
    private readonly  Transform                  container;

    private readonly Dictionary<VRRig, RigPhantomSet> rigSets = [];

    internal PhantomColliderManager()
    {
        GameObject root = new("BeyondGorillaFingers_Phantoms");
        container = root.transform;
    }

    internal void RegisterRig(VRRig rig, bool isLocal)
    {
        if (rig == null)
            return;

        if (rigSets.TryGetValue(rig, out RigPhantomSet existing))
        {
            if (existing.IsLocal == isLocal)
                return;

            UnregisterRig(rig);
        }

        RigPhantomSet set = new(rig, isLocal);
        BuildForRig(rig.transform, set);
        rigSets[rig] = set;
    }

    internal void UnregisterRig(VRRig rig)
    {
        if (rig == null)
            return;

        if (!rigSets.TryGetValue(rig, out RigPhantomSet set))
            return;

        foreach (PhantomColliderProxy proxy in set.Colliders.Where(proxy => proxy != null))
        {
            Colliders.Remove(proxy);
            Object.Destroy(proxy.gameObject);
        }

        set.Colliders.Clear();
        rigSets.Remove(rig);
    }

    internal void UpdateAll()
    {
        for (int i = Colliders.Count - 1; i >= 0; i--)
        {
            PhantomColliderProxy proxy = Colliders[i];

            if (proxy == null || proxy.Collider == null)
            {
                Colliders.RemoveAt(i);

                continue;
            }

            proxy.Align();
        }
    }

    internal void Dispose()
    {
        foreach (RigPhantomSet set in rigSets.Values)
        {
            foreach (PhantomColliderProxy t in set.Colliders.Where(t => t != null))
                Object.Destroy(t.gameObject);

            set.Colliders.Clear();
        }

        rigSets.Clear();
        Colliders.Clear();

        if (container != null)
            Object.Destroy(container.gameObject);
    }

    private void BuildForRig(Transform rigRoot, RigPhantomSet set)
    {
        Transform head = rigRoot.Find("rig/head");
        Transform body = rigRoot.Find("rig/body_pivot/body");

        Transform upperL = rigRoot.Find("rig/body_pivot/shoulder.L/upper_arm.L");
        Transform lowerL = rigRoot.Find("rig/body_pivot/shoulder.L/upper_arm.L/forearm.L");
        Transform handL  = rigRoot.Find("rig/hand.L");

        Transform upperR = rigRoot.Find("rig/body_pivot/shoulder.R/upper_arm.R");
        Transform lowerR = rigRoot.Find("rig/body_pivot/shoulder.R/upper_arm.R/forearm.R");
        Transform handR  = rigRoot.Find("rig/hand.R");

        AddCapsule(set, body, head, 0.18f, PhantomOwner.Neutral);
        AddSphere(set, body, 0.2f,  new Vector3(0f, -0.1f, 0f), PhantomOwner.Neutral);
        AddSphere(set, head, 0.15f, Vector3.zero,               PhantomOwner.Neutral);

        AddCapsule(set, upperL, lowerL, 0.08f, set.IsLocal ? PhantomOwner.LeftHand : PhantomOwner.Neutral);
        AddCapsule(set, lowerL, handL,  0.07f, set.IsLocal ? PhantomOwner.LeftHand : PhantomOwner.Neutral);
        AddSphere(set, handL, 0.08f, new Vector3(0f, -0.04f, 0f),
                set.IsLocal ? PhantomOwner.LeftHand : PhantomOwner.Neutral);

        AddCapsule(set, upperR, lowerR, 0.08f, set.IsLocal ? PhantomOwner.RightHand : PhantomOwner.Neutral);
        AddCapsule(set, lowerR, handR,  0.07f, set.IsLocal ? PhantomOwner.RightHand : PhantomOwner.Neutral);
        AddSphere(set, handR, 0.08f, new Vector3(0f, -0.04f, 0f),
                set.IsLocal ? PhantomOwner.RightHand : PhantomOwner.Neutral);

        foreach (FingerDefinition definition in FingerCatalog.All)
        {
            PhantomOwner     owner      = set.IsLocal ? FingerCatalog.OwnerFor(definition.Side) : PhantomOwner.Neutral;

            for (int i = 0; i < definition.BonePaths.Length - 1; i++)
            {
                Transform start = rigRoot.Find(definition.BonePaths[i]);
                Transform end   = rigRoot.Find(definition.BonePaths[i + 1]);

                AddCapsule(set, start, end, 0.014f, owner);
            }

            Transform tip = rigRoot.Find(definition.BonePaths[^1]);
            AddSphere(set, tip, 0.014f, Vector3.zero, owner);
        }
    }

    private void AddSphere(RigPhantomSet set, Transform parent, float radius, Vector3 offset, PhantomOwner owner)
    {
        if (parent == null)
            return;

        GameObject go = new("BGF_PhantomSphere_" + parent.name)
        {
                layer = 10,
        };

        go.transform.SetParent(parent, false);
        go.transform.localPosition = offset;
        go.transform.localRotation = Quaternion.identity;

        SphereCollider sphere = go.AddComponent<SphereCollider>();
        sphere.radius    = radius;
        sphere.isTrigger = true;

        PhantomColliderProxy proxy = go.AddComponent<PhantomColliderProxy>();
        proxy.SetupSphere(sphere, owner);

        set.Colliders.Add(proxy);
        Colliders.Add(proxy);
    }

    private void AddCapsule(RigPhantomSet set, Transform start, Transform end, float radius, PhantomOwner owner)
    {
        if (start == null || end == null)
            return;

        GameObject go = new("BGF_PhantomCapsule_" + start.name);
        go.layer = 10;
        go.transform.SetParent(container, false);

        CapsuleCollider capsule = go.AddComponent<CapsuleCollider>();
        capsule.isTrigger = true;

        PhantomColliderProxy proxy = go.AddComponent<PhantomColliderProxy>();
        proxy.SetupCapsule(capsule, start, end, radius, owner);

        set.Colliders.Add(proxy);
        Colliders.Add(proxy);
    }

    private sealed class RigPhantomSet
    {
        internal readonly List<PhantomColliderProxy> Colliders = [];
        internal readonly bool                       IsLocal;
        internal readonly VRRig                      Rig;

        internal RigPhantomSet(VRRig rig, bool isLocal)
        {
            Rig     = rig;
            IsLocal = isLocal;
        }
    }
}
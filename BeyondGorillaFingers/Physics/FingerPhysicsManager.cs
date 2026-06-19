using System.Collections.Generic;
using BeyondGorillaFingers.Data;
using UnityEngine;

namespace BeyondGorillaFingers.Physics;

[DefaultExecutionOrder(10000)]
internal sealed class FingerPhysicsManager : MonoBehaviour
{
    internal static FingerPhysicsManager Active { get; private set; }

    private readonly List<FingerChain> fingers = [];

    private bool built;
    private VRRig rig;
    private FingerPhysicsSettings settings;
    private PhantomColliderManager phantomManager;

    private void OnEnable()
    {
        Active = this;
    }

    private void Start()
    {
        Build();
    }

    private void FixedUpdate()
    {
        if (!built)
            Build();

        if (!built || phantomManager == null)
            return;

        float deltaTime = Time.fixedDeltaTime;

        phantomManager.UpdateAll();

        for (int i = 0; i < fingers.Count; i++)
            fingers[i].Step(deltaTime);

        for (int iteration = 0; iteration < FingerPhysicsSettings.ConstraintIterations; iteration++)
        {
            for (int i = 0; i < fingers.Count; i++)
                fingers[i].Constrain();

            for (int collisionIteration = 0; collisionIteration < FingerPhysicsSettings.CollisionIterations; collisionIteration++)
            {
                for (int i = 0; i < fingers.Count; i++)
                    fingers[i].CollideWithPhantoms(phantomManager.Colliders);

                for (int i = 0; i < fingers.Count; i++)
                {
                    for (int j = i + 1; j < fingers.Count; j++)
                        fingers[i].CollideWithFinger(fingers[j]);
                }
            }
        }

        for (int i = 0; i < fingers.Count; i++)
        {
            fingers[i].CommitVelocities(deltaTime);
            fingers[i].SyncParticleTransforms();
        }
    }

    private void OnDestroy()
    {
        if (Active == this)
            Active = null;

        for (int i = 0; i < fingers.Count; i++)
            fingers[i].Dispose();

        fingers.Clear();

        phantomManager?.Dispose();
        phantomManager = null;
    }

    internal void BeforeRigLateUpdate()
    {
        if (!built)
            Build();

        if (!built)
            return;

        for (int i = 0; i < fingers.Count; i++)
            fingers[i].RestoreCleanPose();
    }

    internal void AfterRigLateUpdate()
    {
        if (!built)
            Build();

        if (!built)
            return;

        phantomManager?.UpdateAll();

        for (int i = 0; i < fingers.Count; i++)
            fingers[i].CaptureAnimatedPose();

        for (int i = 0; i < fingers.Count; i++)
            fingers[i].ApplyVisuals(Time.deltaTime);
    }

    internal void RegisterRig(VRRig targetRig)
    {
        if (targetRig == null)
            return;

        if (!built)
            Build();

        if (!built || phantomManager == null)
            return;

        bool isLocal = targetRig == rig || targetRig == VRRig.LocalRig;
        phantomManager.RegisterRig(targetRig, isLocal);
    }

    internal void UnregisterRig(VRRig targetRig)
    {
        if (targetRig == null || phantomManager == null)
            return;

        if (targetRig == rig || targetRig == VRRig.LocalRig)
            return;

        phantomManager.UnregisterRig(targetRig);
    }

    private void Build()
    {
        if (built)
            return;

        rig = GetComponent<VRRig>();

        if (rig == null)
            rig = VRRig.LocalRig;

        if (rig == null)
            return;

        settings = new FingerPhysicsSettings();

        for (int i = 0; i < FingerCatalog.All.Length; i++)
        {
            FingerChain chain = new(FingerCatalog.All[i], settings);

            if (chain.Build(transform))
                fingers.Add(chain);
        }

        if (fingers.Count == 0)
            return;

        phantomManager = new PhantomColliderManager();
        phantomManager.RegisterRig(rig, true);
        RegisterCachedRigs();

        built = true;
    }

    private void RegisterCachedRigs()
    {
        if (VRRigCache.m_activeRigs == null)
            return;

        foreach (VRRig activeRig in VRRigCache.m_activeRigs)
        {
            if (activeRig == null || activeRig == rig)
                continue;

            phantomManager.RegisterRig(activeRig, false);
        }
    }
}
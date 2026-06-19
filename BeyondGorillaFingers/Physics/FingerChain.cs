using System.Collections.Generic;
using System.Linq;
using BeyondGorillaFingers.Data;
using UnityEngine;

namespace BeyondGorillaFingers.Physics;

internal sealed class FingerChain
{
    private readonly FingerDefinition      definition;
    private readonly FingerPhysicsSettings settings;
    private          BonePose[]            animatedPoses;
    private          BonePose[]            bindPoses;

    private Transform[] bones;

    private bool             hasAnimatedPose;
    private Vector3          lastRootPosition;
    private FingerParticle[] particles;
    private Vector3[]        previousPositions;
    private float[]          segmentLengths;
    private Quaternion[]     smoothedLocalRotations;

    internal FingerChain(FingerDefinition definition, FingerPhysicsSettings settings)
    {
        this.definition = definition;
        this.settings   = settings;
    }

    private HandSide Side    => definition.Side;
    private bool     IsBuilt => bones is { Length: > 0, };

    internal bool Build(Transform rigRoot)
    {
        bones = new Transform[definition.BonePaths.Length];

        for (int i = 0; i < definition.BonePaths.Length; i++)
        {
            bones[i] = rigRoot.Find(definition.BonePaths[i]);

            if (bones[i] == null)
                return false;
        }

        bindPoses              = new BonePose[bones.Length];
        animatedPoses          = new BonePose[bones.Length];
        smoothedLocalRotations = new Quaternion[bones.Length];
        particles              = new FingerParticle[bones.Length];
        previousPositions      = new Vector3[bones.Length];
        segmentLengths         = new float[bones.Length - 1];

        for (int i = 0; i < bones.Length; i++)
        {
            bindPoses[i]              = BonePose.Capture(bones[i]);
            animatedPoses[i]          = bindPoses[i];
            smoothedLocalRotations[i] = bones[i].localRotation;

            particles[i] = new FingerParticle(
                    "BGF_Particle_" + definition.Name + "_" + i,
                    bones[i].position,
                    FingerPhysicsSettings.Radius
            );

            previousPositions[i] = particles[i].Position;

            if (i < bones.Length - 1)
                segmentLengths[i] = Vector3.Distance(bones[i].position, bones[i + 1].position);
        }

        hasAnimatedPose  = true;
        lastRootPosition = particles[0].Position;

        return true;
    }

    internal void RestoreCleanPose()
    {
        if (!IsBuilt)
            return;

        BonePose[] source = hasAnimatedPose ? animatedPoses : bindPoses;

        for (int i = 0; i < bones.Length; i++)
            source[i].ApplyLocal(bones[i]);
    }

    internal void CaptureAnimatedPose()
    {
        if (!IsBuilt)
            return;

        for (int i = 0; i < bones.Length; i++)
            animatedPoses[i] = BonePose.Capture(bones[i]);

        if (!hasAnimatedPose)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Position         = animatedPoses[i].Position;
                particles[i].PreviousPosition = animatedPoses[i].Position;
                particles[i].Velocity         = Vector3.zero;
                particles[i].SyncTransform();
            }

            lastRootPosition = animatedPoses[0].Position;
        }

        hasAnimatedPose = true;
    }

    internal void Step(float deltaTime)
    {
        if (!hasAnimatedPose || deltaTime <= 0f)
            return;

        Vector3 rootDelta    = animatedPoses[0].Position - lastRootPosition;
        Vector3 rootVelocity = rootDelta / deltaTime;
        lastRootPosition = animatedPoses[0].Position;

        for (int i = 0; i < particles.Length; i++)
            previousPositions[i] = particles[i].Position;

        particles[0].Position = animatedPoses[0].Position;
        particles[0].Velocity = rootVelocity;

        float snapSqr = FingerPhysicsSettings.SnapDistance * FingerPhysicsSettings.SnapDistance;
        float damping = Mathf.Exp(-FingerPhysicsSettings.VelocityDamping * deltaTime);

        for (int i = 1; i < particles.Length; i++)
        {
            FingerParticle particle = particles[i];

            particle.Position += rootDelta * FingerPhysicsSettings.HandMotionInheritance;

            Vector3 target   = animatedPoses[i].Position;
            Vector3 toTarget = target - particle.Position;

            if (toTarget.sqrMagnitude > snapSqr)
            {
                particle.Position = target;
                particle.Velocity = rootVelocity;

                continue;
            }

            particle.Velocity += toTarget * (FingerPhysicsSettings.FollowStrength * deltaTime);
            particle.Velocity *= damping;
            particle.Position += particle.Velocity * deltaTime;
        }
    }

    internal void Constrain()
    {
        if (!hasAnimatedPose)
            return;

        particles[0].Position = animatedPoses[0].Position;

        for (int i = 0; i < segmentLengths.Length; i++)
            SolveLength(i);

        for (int i = 0; i < segmentLengths.Length; i++)
            SolveBendLimit(i);
    }

    internal void CollideWithPhantoms(List<PhantomColliderProxy> phantoms)
    {
        if (!hasAnimatedPose)
            return;

        for (int i = 1; i < particles.Length; i++)
        {
            FingerParticle particle = particles[i];
            particle.SyncTransform();

            foreach (PhantomColliderProxy phantom in phantoms.Where(phantom => phantom != null && phantom.Collider != null).Where(phantom => CanCollideWith(phantom.Owner)))
            {
                if (!UnityEngine.Physics.ComputePenetration(
                            particle.Collider,
                            particle.Position,
                            Quaternion.identity,
                            phantom.Collider,
                            phantom.Collider.transform.position,
                            phantom.Collider.transform.rotation,
                            out Vector3 direction,
                            out float distance
                    ))
                    continue;

                particle.Position += direction * (distance + FingerPhysicsSettings.ContactOffset);
                RemoveVelocityIntoSurface(particle, direction);
                particle.SyncTransform();
            }
        }
    }

    internal void CollideWithFinger(FingerChain other)
    {
        if (other == null || other == this)
            return;

        if (Side != HandSide.Left || other.Side != HandSide.Right)
            return;

        for (int i = 1; i < particles.Length; i++)
        {
            FingerParticle a = particles[i];
            a.SyncTransform();

            for (int j = 1; j < other.particles.Length; j++)
            {
                FingerParticle b = other.particles[j];
                b.SyncTransform();

                if (!UnityEngine.Physics.ComputePenetration(
                            a.Collider,
                            a.Position,
                            Quaternion.identity,
                            b.Collider,
                            b.Position,
                            Quaternion.identity,
                            out Vector3 direction,
                            out float distance
                    ))
                    continue;

                Vector3 push = direction * ((distance + FingerPhysicsSettings.ContactOffset) * 0.5f);

                a.Position += push;
                b.Position -= push;

                RemoveVelocityIntoSurface(a, direction);
                RemoveVelocityIntoSurface(b, -direction);

                a.SyncTransform();
                b.SyncTransform();
            }
        }
    }

    internal void CommitVelocities(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        particles[0].Velocity = Vector3.zero;

        for (int i = 1; i < particles.Length; i++)
            particles[i].Velocity =
                    (particles[i].Position - previousPositions[i]) / deltaTime * FingerPhysicsSettings.PostSolveDamping;
    }

    internal void SyncParticleTransforms()
    {
        foreach (FingerParticle t in particles)
            t.SyncTransform();
    }

    internal void ApplyVisuals(float deltaTime)
    {
        if (!hasAnimatedPose)
            return;

        float alpha = FingerPhysicsSettings.DampAlpha(FingerPhysicsSettings.VisualSharpness, deltaTime);

        for (int i = 0; i < bones.Length; i++)
        {
            bones[i].localPosition = animatedPoses[i].LocalPosition;
            bones[i].localRotation = animatedPoses[i].LocalRotation;
        }

        bones[0].position = animatedPoses[0].Position;
        bones[0].rotation = animatedPoses[0].Rotation;

        for (int i = 0; i < segmentLengths.Length; i++)
        {
            Vector3 animatedDirection = animatedPoses[i + 1].Position - animatedPoses[i].Position;
            Vector3 solvedDirection   = particles[i     + 1].Position - particles[i].Position;

            if (animatedDirection.sqrMagnitude <= 0.000001f || solvedDirection.sqrMagnitude <= 0.000001f)
                continue;

            animatedDirection.Normalize();
            solvedDirection.Normalize();

            Quaternion desiredWorldRotation = Quaternion.FromToRotation(animatedDirection, solvedDirection) *
                                              animatedPoses[i].Rotation;

            Quaternion desiredLocalRotation = bones[i].parent == null
                                                      ? desiredWorldRotation
                                                      : Quaternion.Inverse(bones[i].parent.rotation) *
                                                        desiredWorldRotation;

            smoothedLocalRotations[i] = Quaternion.Slerp(smoothedLocalRotations[i], desiredLocalRotation, alpha);
            bones[i].localRotation    = smoothedLocalRotations[i];

            bones[i + 1].position = bones[i].position + solvedDirection * segmentLengths[i];
        }

        int last = bones.Length - 1;
        smoothedLocalRotations[last] =
                Quaternion.Slerp(smoothedLocalRotations[last], animatedPoses[last].LocalRotation, alpha);

        bones[last].localRotation = smoothedLocalRotations[last];
    }

    internal void Dispose()
    {
        if (particles == null)
            return;

        foreach (FingerParticle t in particles)
            t.Dispose();
    }

    private void SolveLength(int segment)
    {
        FingerParticle parent = particles[segment];
        FingerParticle child  = particles[segment + 1];

        Vector3 delta    = child.Position - parent.Position;
        float   distance = delta.magnitude;

        if (distance <= 0.0001f)
        {
            Vector3 fallback = animatedPoses[segment + 1].Position - animatedPoses[segment].Position;

            if (fallback.sqrMagnitude <= 0.0001f)
                return;

            delta    = fallback.normalized;
            distance = segmentLengths[segment];
        }
        else
        {
            delta /= distance;
        }

        float error = distance - segmentLengths[segment];

        float       parentWeight = segment == 0 ? 0f : FingerPhysicsSettings.ParentFlex;
        const float ChildWeight  = 1f;
        float       totalWeight  = parentWeight + ChildWeight;

        Vector3 correction = delta * error;

        if (parentWeight > 0f)
            parent.Position += correction * (parentWeight / totalWeight);

        child.Position -= correction * (ChildWeight / totalWeight);

        particles[0].Position = animatedPoses[0].Position;
    }

    private void SolveBendLimit(int segment)
    {
        FingerParticle parent = particles[segment];
        FingerParticle child  = particles[segment + 1];

        Vector3 animatedDirection = animatedPoses[segment + 1].Position - animatedPoses[segment].Position;
        Vector3 currentDirection  = child.Position                      - parent.Position;

        if (animatedDirection.sqrMagnitude <= 0.000001f || currentDirection.sqrMagnitude <= 0.000001f)
            return;

        animatedDirection.Normalize();
        currentDirection.Normalize();

        float maxAngle = settings.GetMaxBendAngle(segment);
        float angle    = Vector3.Angle(animatedDirection, currentDirection);

        if (angle <= maxAngle)
            return;

        Vector3 axis = Vector3.Cross(animatedDirection, currentDirection);

        if (axis.sqrMagnitude <= 0.000001f)
            axis = animatedPoses[segment].Rotation * Vector3.right;

        Vector3 limitedDirection = Quaternion.AngleAxis(maxAngle, axis.normalized) * animatedDirection;
        child.Position = parent.Position + limitedDirection.normalized * segmentLengths[segment];
    }

    private bool CanCollideWith(PhantomOwner owner)
    {
        if (owner == PhantomOwner.Neutral)
            return true;

        if (Side == HandSide.Left)
            return owner == PhantomOwner.RightHand;

        return owner == PhantomOwner.LeftHand;
    }

    private static void RemoveVelocityIntoSurface(FingerParticle particle, Vector3 pushDirection)
    {
        float intoSurface = Vector3.Dot(particle.Velocity, -pushDirection);

        if (intoSurface > 0f)
            particle.Velocity += pushDirection * intoSurface;
    }
}
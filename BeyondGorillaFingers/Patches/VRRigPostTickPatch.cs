using BeyondGorillaFingers.Physics;
using HarmonyLib;

namespace BeyondGorillaFingers.Patches;

[HarmonyPatch(typeof(VRRig), nameof(VRRig.PostTick))]
internal static class VRRigPostTickPatch
{
    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    private static void Prefix(VRRig __instance)
    {
        if (__instance == null || !__instance.isLocal)
            return;

        FingerPhysicsManager manager = FingerPhysicsManager.Active;

        if (manager == null)
            manager = __instance.GetComponent<FingerPhysicsManager>();

        manager?.BeforeRigLateUpdate();
    }

    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(VRRig __instance)
    {
        if (__instance == null || !__instance.isLocal)
            return;

        FingerPhysicsManager manager = FingerPhysicsManager.Active;

        if (manager == null)
            manager = __instance.GetComponent<FingerPhysicsManager>();

        if (manager == null)
            manager = __instance.gameObject.AddComponent<FingerPhysicsManager>();

        manager.AfterRigLateUpdate();
    }
}
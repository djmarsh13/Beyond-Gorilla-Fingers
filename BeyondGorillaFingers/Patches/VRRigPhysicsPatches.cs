using BeyondGorillaFingers.Physics;
using HarmonyLib;

namespace BeyondGorillaFingers.Patches;

[HarmonyPatch(typeof(VRRig))]
internal static class VRRigPhysicsPatches
{
    [HarmonyPatch(nameof(VRRig.OnEnable))]
    [HarmonyPostfix]
    private static void OnEnablePostfix(VRRig __instance)
    {
        if (__instance == null)
            return;

        FingerPhysicsManager.Active?.RegisterRig(__instance);
    }

    [HarmonyPatch(nameof(VRRig.OnDisable))]
    [HarmonyPrefix]
    private static void OnDisablePrefix(VRRig __instance)
    {
        if (__instance == null)
            return;

        FingerPhysicsManager.Active?.UnregisterRig(__instance);
    }
}
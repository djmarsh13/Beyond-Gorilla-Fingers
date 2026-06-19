using System.Reflection;
using HarmonyLib;

namespace BeyondGorillaFingers;

internal static class HarmonyPatches
{
    private static Harmony harmony;

    internal static bool IsPatched { get; private set; }

    internal static void ApplyHarmonyPatches()
    {
        if (IsPatched)
            return;

        harmony ??= new Harmony(Constants.Guid);
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        IsPatched = true;
    }

    internal static void RemoveHarmonyPatches()
    {
        if (!IsPatched || harmony == null)
            return;

        harmony.UnpatchSelf();
        IsPatched = false;
    }
}
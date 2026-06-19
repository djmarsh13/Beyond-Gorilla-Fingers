using BepInEx;
using BeyondGorillaFingers.Physics;

namespace BeyondGorillaFingers;

[BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
public sealed class Plugin : BaseUnityPlugin
{
    private void Start()
    {
        HarmonyPatches.ApplyHarmonyPatches();
        GorillaTagger.OnPlayerSpawned(() => VRRig.LocalRig.AddComponent<FingerPhysicsManager>());
    }
}
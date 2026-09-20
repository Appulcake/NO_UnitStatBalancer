using Blueprinter;
using HarmonyLib;

namespace NO_USB;

[HarmonyPatch]
internal static class HarmonyPatches
{
    [HarmonyPatch(typeof(PatchRunner), nameof(PatchRunner.ApplyAllOps))]
    [HarmonyPostfix]
    private static void BlueprinterReadyPatch()
    {
        TweakManager.ApplyAll();
    }
}
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
internal static class SluggishThresholdDownStackLimitPatch
{
    private static void Prefix(PowerModel __instance, ref int amount)
    {
        if (__instance is SluggishThresholdDownPower)
        {
            amount = Math.Min(amount, SluggishThresholdDownPower.MaxStacks);
        }
    }
}

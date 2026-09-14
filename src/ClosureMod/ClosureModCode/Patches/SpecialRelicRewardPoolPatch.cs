using ClosureMod.Characters;
using ClosureMod.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(RelicPoolModel), nameof(RelicPoolModel.GetUnlockedRelics))]
internal static class SpecialRelicRewardPoolPatch
{
    private static void Postfix(
        RelicPoolModel __instance,
        ref IEnumerable<RelicModel> __result,
        UnlockState unlockState)
    {
        if (__instance is not ClosureModRelicPool)
        {
            return;
        }

        __result = __result
            .Where(relic => relic is not StrangeButton and not ClosureModRelic)
            .ToList();
    }
}

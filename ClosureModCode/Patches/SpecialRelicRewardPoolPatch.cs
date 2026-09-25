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

        // Starter relics must remain unlocked for the library; RelicGrabBag excludes Starter rarity itself.
        __result = __result
            .Where(relic => relic is not GaulCheque and not GiftCard
                and not StructuralPrinciple and not SniperScope)
            .ToList();
    }
}

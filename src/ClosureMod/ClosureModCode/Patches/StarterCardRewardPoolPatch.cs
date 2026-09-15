using ClosureMod.Cards;
using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using System.Reflection;

namespace ClosureMod.Patches;

internal static class NonRewardCardFilter
{
    public static bool IsAllowed(CardModel card)
    {
        return card is not ClearDebt
            and not HeavyStrike
            and not Settlement
            and not DrawGame
            and not WaitForStartup
            and not IHaveStarted;
    }
}

[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
internal static class StarterCardRewardPoolPatch
{
    private static void Postfix(
        CardPoolModel __instance,
        ref IEnumerable<CardModel> __result,
        UnlockState unlockState,
        CardMultiplayerConstraint multiplayerConstraint)
    {
        __result = __result
            .Where(card => __instance is not ClosureModCardPool || NonRewardCardFilter.IsAllowed(card))
            .ToList();
    }
}

[HarmonyPatch(typeof(CardCreationOptions), nameof(CardCreationOptions.GetPossibleCards))]
internal static class StarterCardPossibleCardsPatch
{
    private static void Postfix(ref IEnumerable<CardModel> __result)
    {
        __result = __result
            .Where(NonRewardCardFilter.IsAllowed)
            .ToList();
    }
}

[HarmonyPatch]
internal static class StarterCardRewardResultPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CardFactory),
            nameof(CardFactory.CreateForReward),
            [typeof(Player), typeof(int), typeof(CardCreationOptions)]);
    }

    private static void Postfix(ref IEnumerable<CardCreationResult> __result)
    {
        __result = __result
            .Where(result => NonRewardCardFilter.IsAllowed(result.Card))
            .ToList();
    }
}

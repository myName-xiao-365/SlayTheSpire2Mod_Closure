using System.Runtime.CompilerServices;
using ClosureMod.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers.Models;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Exceptions;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Patches;

internal static class TurnEnergySpendTracker
{
    private static readonly ConditionalWeakTable<Player, PlayerSpendState> SpentByPlayer = new();

    public static bool HasPlayedCardCostingAtLeast(Player? player, int threshold)
    {
        if (player?.PlayerCombatState is null)
        {
            return false;
        }

        PlayerSpendState state = SpentByPlayer.GetOrCreateValue(player);
        state.SyncTurn(player.PlayerCombatState.TurnNumber);
        return state.HighestSingleCardSpend >= threshold;
    }

    public static void RecordSpent(Player? player, int amount)
    {
        if (player?.PlayerCombatState is null || amount <= 0)
        {
            return;
        }

        PlayerSpendState state = SpentByPlayer.GetOrCreateValue(player);
        state.SyncTurn(player.PlayerCombatState.TurnNumber);
        if (amount <= state.HighestSingleCardSpend)
        {
            return;
        }

        state.HighestSingleCardSpend = amount;

        foreach (CardModel handCard in player.PlayerCombatState.Hand.Cards)
        {
            if (handCard is AdditionalOrder)
            {
                handCard.RequestVisualReload();
            }
        }
    }

    private sealed class PlayerSpendState
    {
        private int _turnNumber = -1;

        public int HighestSingleCardSpend { get; set; }

        public void SyncTurn(int turnNumber)
        {
            if (_turnNumber == turnNumber)
            {
                return;
            }

            _turnNumber = turnNumber;
            HighestSingleCardSpend = 0;
        }
    }

    public static bool TryGetCombatOwner(CardModel card, out Player owner)
    {
        try
        {
            owner = card.Owner;
            return card.IsInCombat && owner?.PlayerCombatState is not null;
        }
        catch (CanonicalModelException)
        {
            owner = null!;
            return false;
        }
    }
}

[HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.GetWithModifiers), [typeof(CostModifiers)])]
internal static class AdditionalOrderCostPatch
{
    private static void Postfix(CardEnergyCost __instance, ref int __result)
    {
        CardModel? card = __instance._card;
        if (card is not AdditionalOrder)
        {
            return;
        }

        if (TurnEnergySpendTracker.TryGetCombatOwner(card, out Player owner) &&
            TurnEnergySpendTracker.HasPlayedCardCostingAtLeast(owner, AdditionalOrder.DiscountThreshold))
        {
            __result = 0;
        }
    }
}

[HarmonyPatch(typeof(CardEnergyCost), "HasLocalModifiers", MethodType.Getter)]
internal static class AdditionalOrderHasLocalModifierPatch
{
    private static void Postfix(CardEnergyCost __instance, ref bool __result)
    {
        CardModel? card = __instance._card;
        if (!__result &&
            card is AdditionalOrder &&
            TurnEnergySpendTracker.TryGetCombatOwner(card, out Player owner) &&
            TurnEnergySpendTracker.HasPlayedCardCostingAtLeast(owner, AdditionalOrder.DiscountThreshold))
        {
            __result = true;
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
internal static class TurnEnergySpendTrackerPatch
{
    private static async Task<(int, int)> Postfix(
        Task<(int, int)> __result,
        CardModel __instance)
    {
        (int energySpent, int starsSpent) result = await __result;
        TurnEnergySpendTracker.RecordSpent(__instance.Owner, result.energySpent);
        return result;
    }
}

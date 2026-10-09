using System.Runtime.CompilerServices;
using System.Reflection;
using ClosureMod.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.LoseGold))]
internal static class GaulChequeGoldFloorPatch
{
    private static void Prefix(Player player, ref int __state)
    {
        __state = player.Gold;
    }

    private static void Postfix(decimal amount, Player player, int __state)
    {
        if (amount > 0 && player.Relics.Any(relic => relic is GaulCheque))
        {
            player.Gold = Math.Max(-100, __state - (int)amount);
        }
    }
}

[HarmonyPatch(typeof(MerchantEntry), "get_EnoughGold")]
internal static class GaulChequeShopCreditPatch
{
    private static void Postfix(MerchantEntry __instance, Player ____player, ref bool __result)
    {
        if (!__result && ____player.Relics.Any(relic => relic is GaulCheque))
        {
            __result = ____player.Gold + 100 >= __instance.Cost;
        }
    }
}

[HarmonyPatch(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant))]
internal static class WorkshopShopDiscounts
{
    private sealed class GaulDiscountState
    {
        public int Percent { get; init; }
    }

    private static readonly ConditionalWeakTable<MerchantEntry, GaulDiscountState> GaulDiscounts = new();
    private static readonly FieldInfo BaseCostField = AccessTools.Field(typeof(MerchantEntry), "_cost");

    public static bool TryGetGaulDiscount(MerchantEntry entry, out int percent)
    {
        if (GaulDiscounts.TryGetValue(entry, out GaulDiscountState? discount))
        {
            percent = discount.Percent;
            return true;
        }

        percent = 0;
        return false;
    }

    public static void ClearGaulDiscount(MerchantEntry entry) => GaulDiscounts.Remove(entry);

    private static void Postfix(MerchantInventory __result, Player player)
    {
        if (player.Relics.Any(relic => relic is GiftCard))
        {
            GiftCardShopRefresh.Subscribe(__result, player);
        }

        GaulCheque? cheque = player.Relics.OfType<GaulCheque>().FirstOrDefault();
        if (cheque is null)
        {
            return;
        }

        int chancePercent = Math.Min(100, 20 + 10 * Math.Min(8, cheque.ShopsVisited));
        int normalDiscountPercent = Math.Min(90, 20 + 10 * Math.Min(7, cheque.ShopsVisited));
        var rng = player.PlayerRng.Shops;
        foreach (MerchantEntry entry in __result.AllEntries)
        {
            if (entry is MerchantCardEntry { IsOnSale: true } ||
                entry.Cost < (int)BaseCostField.GetValue(entry)!)
            {
                continue;
            }

            if (rng.NextInt(100) >= chancePercent)
            {
                continue;
            }

            GaulDiscounts.Add(entry, new GaulDiscountState
            {
                Percent = rng.NextInt(100) < 2 ? 99 : normalDiscountPercent
            });
        }

        cheque.RecordShopVisit();
    }
}

internal static class GiftCardShopRefresh
{
    private sealed class RefreshState
    {
        public bool Used;
    }

    private sealed class RefreshedStock
    {
        public required object Item { get; init; }
        public bool Free { get; init; }
    }

    private static readonly ConditionalWeakTable<MerchantInventory, RefreshState> Refreshes = new();
    private static readonly ConditionalWeakTable<MerchantEntry, RefreshedStock> RefreshedPrices = new();
    private static readonly MethodInfo CardRestock = AccessTools.Method(typeof(MerchantCardEntry), "RestockAfterPurchase");
    private static readonly MethodInfo RelicRestock = AccessTools.Method(typeof(MerchantRelicEntry), "RestockAfterPurchase");
    private static readonly MethodInfo PotionRestock = AccessTools.Method(typeof(MerchantPotionEntry), "RestockAfterPurchase");

    public static void Subscribe(MerchantInventory inventory, Player player)
    {
        if (inventory.CardRemovalEntry is not { } removal || Refreshes.TryGetValue(inventory, out _))
        {
            return;
        }

        var state = new RefreshState();
        Refreshes.Add(inventory, state);
        removal.PurchaseCompleted += (status, _) =>
        {
            if (status != PurchaseStatus.Success || state.Used ||
                !player.Relics.Any(relic => relic is GiftCard))
            {
                return;
            }

            state.Used = true;
            Refresh(inventory, player);
        };
    }

    public static bool TryGetRefreshedPrice(MerchantEntry entry, out bool free)
    {
        if (RefreshedPrices.TryGetValue(entry, out RefreshedStock? stock) &&
            ReferenceEquals(stock.Item, GetStockItem(entry)))
        {
            free = stock.Free;
            return true;
        }

        free = false;
        return false;
    }

    private static void Refresh(MerchantInventory inventory, Player player)
    {
        MerchantEntry[] merchandise = inventory.CardEntries.Cast<MerchantEntry>()
            .Concat(inventory.RelicEntries)
            .Concat(inventory.PotionEntries)
            .ToArray();

        foreach (MerchantEntry entry in merchandise)
        {
            WorkshopShopDiscounts.ClearGaulDiscount(entry);
            MethodInfo restock = entry switch
            {
                MerchantCardEntry => CardRestock,
                MerchantRelicEntry => RelicRestock,
                MerchantPotionEntry => PotionRestock,
                _ => throw new InvalidOperationException($"Unsupported merchant entry: {entry.GetType().Name}")
            };
            restock.Invoke(entry, [inventory]);
        }

        MerchantCardEntry[] characterCards = inventory.CharacterCardEntries
            .Where(card => card.CreationResult is not null)
            .ToArray();
        MerchantCardEntry? freeCard = characterCards.Length > 0
            ? characterCards[player.PlayerRng.Shops.NextInt(characterCards.Length)]
            : null;

        foreach (MerchantEntry entry in merchandise)
        {
            RefreshedPrices.Remove(entry);
            if (GetStockItem(entry) is { } item)
            {
                RefreshedPrices.Add(entry, new RefreshedStock
                {
                    Item = item,
                    Free = ReferenceEquals(entry, freeCard)
                });
            }
        }

        inventory.UpdateEntries(PurchaseStatus.Success, inventory.CardRemovalEntry!);
    }

    private static object? GetStockItem(MerchantEntry entry) => entry switch
    {
        MerchantCardEntry card => card.CreationResult,
        MerchantRelicEntry relic => relic.Model,
        MerchantPotionEntry potion => potion.Model,
        _ => null
    };
}

[HarmonyPatch(typeof(MerchantEntry), "get_Cost")]
internal static class WorkshopDiscountPricePatch
{
    private static void Postfix(MerchantEntry __instance, Player ____player, ref int __result)
    {
        if (____player.Relics.Any(relic => relic is GiftCard) &&
            GiftCardShopRefresh.TryGetRefreshedPrice(__instance, out bool free))
        {
            __result = free ? 0 : Math.Max(1, (int)Math.Round(__result * 0.8m,
                MidpointRounding.AwayFromZero));
            return;
        }

        if (____player.Relics.Any(relic => relic is GaulCheque) &&
            WorkshopShopDiscounts.TryGetGaulDiscount(__instance, out int percent))
        {
            __result = Math.Max(1, (int)Math.Round(__result * (100m - percent) / 100m,
                MidpointRounding.AwayFromZero));
        }
    }
}

[HarmonyPatch(typeof(Creature), "DamageBlockInternal")]
internal static class StructuralPrincipleBlockPatch
{
    private static void Prefix(Creature __instance, decimal amount, ValueProp props)
    {
        if (__instance.Block <= 0 || amount <= __instance.Block ||
            !props.IsPoweredAttack() || props.HasFlag(ValueProp.Unblockable))
        {
            return;
        }

        StructuralPrinciple? relic = __instance.Player?.Relics.OfType<StructuralPrinciple>().FirstOrDefault();
        if (relic?.TryUse() == true)
        {
            __instance.Block = (int)Math.Ceiling(amount);
        }
    }
}

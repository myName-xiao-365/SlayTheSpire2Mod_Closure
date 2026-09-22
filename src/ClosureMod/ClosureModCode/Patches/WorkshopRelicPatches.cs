using System.Runtime.CompilerServices;
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
    private sealed class DiscountState
    {
        public bool ExtraSale { get; init; }
        public bool SpecialOffer { get; init; }
    }

    private static readonly ConditionalWeakTable<MerchantEntry, DiscountState> Discounts = new();

    public static decimal GetPrice(MerchantEntry entry, decimal price)
    {
        if (!Discounts.TryGetValue(entry, out DiscountState? discount))
        {
            return price;
        }

        if (discount.SpecialOffer)
        {
            return price;
        }

        return discount.ExtraSale && entry is not MerchantCardEntry ? price * 0.5m : price;
    }

    public static bool IsSpecialOffer(MerchantEntry entry)
    {
        return Discounts.TryGetValue(entry, out DiscountState? discount) && discount.SpecialOffer;
    }

    private static void Postfix(MerchantInventory __result, Player player)
    {
        if (!player.Relics.Any(relic => relic is GiftCard))
        {
            return;
        }

        var rng = player.PlayerRng.Shops;
        foreach (MerchantEntry entry in __result.AllEntries)
        {
            if (entry is MerchantCardRemovalEntry)
            {
                continue;
            }

            bool extraSale = rng.NextFloat(1f) < 0.35f;
            if (entry is MerchantCardEntry card && extraSale && !card.IsOnSale)
            {
                card.SetOnSale();
            }

            bool discounted = extraSale || entry is MerchantCardEntry { IsOnSale: true };
            if (!discounted)
            {
                continue;
            }

            Discounts.Add(entry, new DiscountState
            {
                ExtraSale = extraSale,
                SpecialOffer = rng.NextFloat(1f) < 0.02f
            });

        }
    }
}

[HarmonyPatch(typeof(MerchantEntry), "get_Cost")]
internal static class WorkshopSpecialOfferPricePatch
{
    private static void Postfix(MerchantEntry __instance, Player ____player, int ____cost, ref int __result)
    {
        if (!____player.Relics.Any(relic => relic is GiftCard) ||
            !WorkshopShopDiscounts.IsSpecialOffer(__instance))
        {
            return;
        }

        decimal original = __instance is MerchantCardEntry ? ____cost * 2m : ____cost;
        __result = Math.Max(1, (int)Math.Round(original * 0.01m, MidpointRounding.AwayFromZero));
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

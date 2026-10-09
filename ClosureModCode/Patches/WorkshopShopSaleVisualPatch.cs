using System.Reflection;
using ClosureMod.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.addons.mega_text;

namespace ClosureMod.Patches;

[HarmonyPatch]
internal static class WorkshopShopSaleVisualPatch
{
    private const string SaleTagPath = "res://images/rooms/merchant_room/shop_sales_tag.png";
    private const string AddedSaleTagName = "WorkshopSaleVisual";
    private static readonly FieldInfo BaseCostField = AccessTools.Field(typeof(MerchantEntry), "_cost");

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NMerchantCard), "UpdateVisual");
        yield return AccessTools.Method(typeof(NMerchantPotion), "UpdateVisual");
        yield return AccessTools.Method(typeof(NMerchantRelic), "UpdateVisual");
        yield return AccessTools.Method(typeof(NMerchantCardRemoval), "UpdateVisual");
    }

    private static void Postfix(NMerchantSlot __instance)
    {
        MerchantEntry entry = __instance.Entry;
        if (entry.IsStocked && GiftCardShopRefresh.TryGetRefreshedPrice(entry, out _))
        {
            __instance.Visible = true;
            __instance.MouseFilter = Control.MouseFilterEnum.Stop;
        }

        int baseCost = (int)BaseCostField.GetValue(entry)!;
        bool discounted = entry.IsStocked &&
            entry is not MerchantCardRemovalEntry { Used: true } &&
            (entry is MerchantCardEntry { IsOnSale: true } || entry.Cost < baseCost);

        if (__instance.GetNodeOrNull<MegaLabel>("Cost/CostLabel") is { } label)
        {
            bool closureShop = __instance.Player?.Character is ClosureModCharacter;
            if (closureShop)
            {
                ClosureMerchantGoldPatch.SetDiscounted(label, discounted);
            }

            if (entry.IsStocked && entry is not MerchantCardRemovalEntry { Used: true })
            {
                if (closureShop && !entry.EnoughGold)
                {
                    ClosureMerchantGoldPatch.SetUnaffordable(label);
                }
                else
                {
                    label.Modulate = !entry.EnoughGold ? StsColors.red :
                        discounted ? StsColors.green : StsColors.cream;
                }
            }
        }

        if (__instance is NMerchantCard)
        {
            if (__instance.GetNodeOrNull<Node2D>("SaleVisual") is { } originalTag)
            {
                originalTag.Visible = discounted;
            }
            return;
        }

        Sprite2D? tag = __instance.GetNodeOrNull<Sprite2D>(AddedSaleTagName);
        if (!discounted)
        {
            if (tag is not null)
            {
                tag.Visible = false;
            }
            return;
        }

        if (tag is null && GD.Load<Texture2D>(SaleTagPath) is { } texture)
        {
            Control hitbox = __instance.Hitbox;
            bool smallSlot = hitbox.Size.X < 180;
            float relicOffsetX = __instance is NMerchantRelic ? 72f : 0f;
            tag = new Sprite2D
            {
                Name = AddedSaleTagName,
                Texture = texture,
                Position = hitbox.Position + new Vector2(
                    hitbox.Size.X - (smallSlot ? 6 : 8) + relicOffsetX, smallSlot ? 8 : 48),
                Scale = Vector2.One * (smallSlot ? 0.36f : 0.65f)
            };
            __instance.AddChild(tag);
        }

        if (tag is not null)
        {
            tag.Visible = true;
        }
    }
}

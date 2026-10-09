using ClosureMod.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.addons.mega_text;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(NMerchantSlot), nameof(NMerchantSlot.Initialize))]
internal static class ClosureMerchantGoldPatch
{
    private const string GoldIconPath = "res://ClosureMod/images/characters/ClosureMod_gold.png";
    private static readonly Color GoldFontColor = new(0.62f, 0.88f, 1f);
    private static readonly Color GoldOutlineColor = new(0.08f, 0.18f, 0.24f);
    private static readonly Color VanillaOutlineColor = new(0.0666667f, 0f, 0f, 0.431373f);

    private static void Postfix(NMerchantSlot __instance)
    {
        if (__instance.Player?.Character is not ClosureModCharacter)
        {
            return;
        }

        Texture2D? goldTexture = GD.Load<Texture2D>(GoldIconPath);
        TextureRect? icon = __instance.GetNodeOrNull<TextureRect>("Cost/GoldIcon") ??
                            __instance.GetNodeOrNull<TextureRect>("Cost/TextureRect");
        if (goldTexture is not null && icon is not null)
        {
            icon.Texture = goldTexture;
        }

        if (__instance.GetNodeOrNull<MegaLabel>("Cost/CostLabel") is { } label)
        {
            SetDiscounted(label, false);
        }
    }

    internal static void SetDiscounted(MegaLabel label, bool discounted)
    {
        if (discounted)
        {
            label.RemoveThemeColorOverride("font_color");
            label.AddThemeColorOverride("font_outline_color", VanillaOutlineColor);
        }
        else
        {
            label.AddThemeColorOverride("font_color", GoldFontColor);
            label.AddThemeColorOverride("font_outline_color", GoldOutlineColor);
        }
    }

    internal static void SetUnaffordable(MegaLabel label)
    {
        label.AddThemeColorOverride("font_color", StsColors.red);
        label.AddThemeColorOverride("font_outline_color", VanillaOutlineColor);
        label.Modulate = Colors.White;
        label.SelfModulate = Colors.White;
    }
}

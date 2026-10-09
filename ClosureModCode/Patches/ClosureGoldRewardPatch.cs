using ClosureMod.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.addons.mega_text;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(GoldReward), nameof(GoldReward.Description), MethodType.Getter)]
internal static class ClosureGoldRewardDescriptionPatch
{
    private static readonly System.Reflection.FieldInfo? WasGoldStolenBack =
        AccessTools.Field(typeof(GoldReward), "_wasGoldStolenBack");

    private static void Postfix(GoldReward __instance, ref LocString __result)
    {
        if (__instance.Player?.Character is not ClosureModCharacter)
        {
            return;
        }

        bool stolenBack = WasGoldStolenBack?.GetValue(__instance) is true;
        __result = new LocString("gameplay_ui", stolenBack
            ? "CLOSURE_MOD_COMBAT_REWARD_GOLD_STOLEN"
            : "CLOSURE_MOD_COMBAT_REWARD_GOLD");
        __result.Add("gold", __instance.Amount);
    }
}

[HarmonyPatch(typeof(NRewardButton), nameof(NRewardButton.Reload))]
internal static class ClosureGoldRewardButtonPatch
{
    private const string GoldIconPath = "res://ClosureMod/images/characters/ClosureMod_gold.png";
    private static readonly Color GoldFontColor = new(0.62f, 0.88f, 1f);
    private static readonly Color GoldOutlineColor = new(0.08f, 0.18f, 0.24f);

    private static void Postfix(NRewardButton __instance)
    {
        if (__instance.Reward is not GoldReward { Player.Character: ClosureModCharacter })
        {
            return;
        }

        if (AccessTools.Field(typeof(NRewardButton), "_iconContainer")
                ?.GetValue(__instance) is Control iconContainer &&
            iconContainer.GetChildren().OfType<TextureRect>().LastOrDefault() is { } icon &&
            GD.Load<Texture2D>(GoldIconPath) is { } texture)
        {
            icon.Texture = texture;
        }

        if (AccessTools.Field(typeof(NRewardButton), "_label")
                ?.GetValue(__instance) is MegaRichTextLabel label)
        {
            label.AddThemeColorOverride("default_color", GoldFontColor);
            label.AddThemeColorOverride("font_outline_color", GoldOutlineColor);
        }
    }
}

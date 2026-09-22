using System.Collections.Generic;
using System.Reflection;
using ClosureMod.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.sts2.Core.Nodes.TopBar;
using MegaCrit.Sts2.addons.mega_text;

namespace ClosureMod.Patches;

// The base top bar owns the gold icon in its scene, so the character asset profile
// cannot replace it. Apply the Closure-specific presentation after the bar is ready
// and after each gold refresh, leaving every other character untouched.
[HarmonyPatch]
internal static class ClosureTopBarGoldPatch
{
    private const string GoldIconPath = "res://ClosureMod/images/characters/ClosureMod_gold.png";
    private static readonly Color GoldFontColor = new(0.62f, 0.88f, 1f);
    private static readonly Color GoldOutlineColor = new(0.08f, 0.18f, 0.24f);

    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodBase? ready = AccessTools.Method(typeof(NTopBarGold), "_Ready");
        MethodBase? initialize = AccessTools.Method(typeof(NTopBarGold), nameof(NTopBarGold.Initialize));
        MethodBase? update = AccessTools.Method(typeof(NTopBarGold), "UpdateGold");

        if (ready is not null)
        {
            yield return ready;
        }

        if (initialize is not null)
        {
            yield return initialize;
        }

        if (update is not null)
        {
            yield return update;
        }
    }

    private static void Postfix(NTopBarGold __instance)
    {
        if (!IsClosureGoldBar(__instance))
        {
            return;
        }

        ApplyGoldLabelColor(__instance);
        ApplyGoldIcon(__instance);
    }

    private static bool IsClosureGoldBar(NTopBarGold topBarGold)
    {
        FieldInfo? playerField = AccessTools.Field(typeof(NTopBarGold), "_player");
        if (playerField?.GetValue(topBarGold) is not Player player)
        {
            return false;
        }

        return player.Character is ClosureModCharacter;
    }

    private static void ApplyGoldLabelColor(NTopBarGold topBarGold)
    {
        foreach (string fieldName in new[] { "_goldLabel", "_goldPopupLabel" })
        {
            if (AccessTools.Field(typeof(NTopBarGold), fieldName)?.GetValue(topBarGold) is not MegaLabel label)
            {
                continue;
            }

            label.AddThemeColorOverride("font_color", GoldFontColor);
            label.AddThemeColorOverride("font_outline_color", GoldOutlineColor);
        }
    }

    private static void ApplyGoldIcon(NTopBarGold topBarGold)
    {
        Texture2D? goldTexture = GD.Load<Texture2D>(GoldIconPath);
        if (goldTexture is null)
        {
            return;
        }

        ApplyGoldIconRecursive(topBarGold, goldTexture);
    }

    private static bool ApplyGoldIconRecursive(Node node, Texture2D texture)
    {
        foreach (Node child in node.GetChildren())
        {
            string nodeName = child.Name.ToString().ToLowerInvariant();
            bool isGoldIcon = nodeName.Contains("gold") || nodeName.Contains("coin") ||
                              nodeName.Contains("currency") || nodeName.Contains("icon");

            if (isGoldIcon && child is TextureRect textureRect)
            {
                textureRect.Texture = texture;
                return true;
            }

            if (isGoldIcon && child is Sprite2D sprite)
            {
                sprite.Texture = texture;
                return true;
            }

            if (ApplyGoldIconRecursive(child, texture))
            {
                return true;
            }
        }

        return false;
    }
}

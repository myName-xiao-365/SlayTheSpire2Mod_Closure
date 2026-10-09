using System.Runtime.CompilerServices;
using ClosureMod.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.addons.mega_text;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
internal static class ClosureCharacterSelectStatsPatch
{
    private const string HealthIconNode = "InfoPanel/VBoxContainer/HpGoldSpacer/HpGold/Hp/Icon";
    private const string GoldIconNode = "InfoPanel/VBoxContainer/HpGoldSpacer/HpGold/Gold/Icon";
    private const string GoldLabelNode = "InfoPanel/VBoxContainer/HpGoldSpacer/HpGold/Gold/Label";
    private const string HealthIconPath = "res://ClosureMod/images/characters/ClosureMod_health.png";
    private const string GoldIconPath = "res://ClosureMod/images/characters/ClosureMod_gold.png";
    private static readonly Color GoldFontColor = new(0.62f, 0.88f, 1f);
    private static readonly Color GoldOutlineColor = new(0.08f, 0.18f, 0.24f);
    private static readonly ConditionalWeakTable<NCharacterSelectScreen, OriginalStats> Originals = new();

    private static void Prefix(NCharacterSelectScreen __instance)
    {
        Originals.GetValue(__instance, CaptureOriginalStats);
    }

    private static void Postfix(
        NCharacterSelectScreen __instance,
        NCharacterSelectButton __0,
        CharacterModel __1)
    {
        if (!Originals.TryGetValue(__instance, out OriginalStats? original))
        {
            return;
        }

        bool isClosure = !__0.IsRandom && __1 is ClosureModCharacter;
        if (original.HealthIcon is { } healthIcon)
        {
            healthIcon.Texture = isClosure
                ? GD.Load<Texture2D>(HealthIconPath) ?? original.HealthTexture
                : original.HealthTexture;
        }

        if (original.GoldIcon is { } goldIcon)
        {
            goldIcon.Texture = isClosure
                ? GD.Load<Texture2D>(GoldIconPath) ?? original.GoldTexture
                : original.GoldTexture;
        }

        if (original.GoldLabel is not { } goldLabel)
        {
            return;
        }

        if (isClosure)
        {
            goldLabel.AddThemeColorOverride("font_color", GoldFontColor);
            goldLabel.AddThemeColorOverride("font_outline_color", GoldOutlineColor);
        }
        else
        {
            RestoreColor(goldLabel, "font_color", original.HadFontColorOverride, original.FontColor);
            RestoreColor(goldLabel, "font_outline_color", original.HadOutlineColorOverride, original.OutlineColor);
        }
    }

    private static OriginalStats CaptureOriginalStats(NCharacterSelectScreen screen)
    {
        TextureRect? healthIcon = screen.GetNodeOrNull<TextureRect>(HealthIconNode);
        TextureRect? goldIcon = screen.GetNodeOrNull<TextureRect>(GoldIconNode);
        MegaLabel? goldLabel = screen.GetNodeOrNull<MegaLabel>(GoldLabelNode);
        return new OriginalStats(
            healthIcon,
            healthIcon?.Texture,
            goldIcon,
            goldIcon?.Texture,
            goldLabel,
            goldLabel?.HasThemeColorOverride("font_color") == true,
            goldLabel?.GetThemeColor("font_color") ?? Colors.White,
            goldLabel?.HasThemeColorOverride("font_outline_color") == true,
            goldLabel?.GetThemeColor("font_outline_color") ?? Colors.Black);
    }

    private static void RestoreColor(MegaLabel label, string name, bool hadOverride, Color color)
    {
        if (hadOverride)
        {
            label.AddThemeColorOverride(name, color);
        }
        else
        {
            label.RemoveThemeColorOverride(name);
        }
    }

    private sealed record OriginalStats(
        TextureRect? HealthIcon,
        Texture2D? HealthTexture,
        TextureRect? GoldIcon,
        Texture2D? GoldTexture,
        MegaLabel? GoldLabel,
        bool HadFontColorOverride,
        Color FontColor,
        bool HadOutlineColorOverride,
        Color OutlineColor);
}

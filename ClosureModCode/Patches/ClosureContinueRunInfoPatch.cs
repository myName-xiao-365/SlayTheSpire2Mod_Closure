using System.Runtime.CompilerServices;
using ClosureMod.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Saves;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(NContinueRunInfo), nameof(NContinueRunInfo.ShowInfo))]
internal static class ClosureContinueRunInfoPatch
{
    private const string HeartIconNode = "MarginContainer/MarginContainer/RunInfoContainer/HBoxContainer2/HeartIcon";
    private const string GoldIconNode = "MarginContainer/MarginContainer/RunInfoContainer/HBoxContainer2/GoldIcon";
    private const string HealthIconPath = "res://ClosureMod/images/characters/ClosureMod_health.png";
    private const string GoldIconPath = "res://ClosureMod/images/characters/ClosureMod_gold.png";
    private static readonly ConditionalWeakTable<NContinueRunInfo, OriginalIcons> Originals = new();

    private static void Prefix(NContinueRunInfo __instance)
    {
        Originals.GetValue(__instance, CaptureOriginalIcons);
    }

    private static void Postfix(NContinueRunInfo __instance, SerializableRun __0)
    {
        if (!Originals.TryGetValue(__instance, out OriginalIcons? original))
        {
            return;
        }

        bool isClosure = __0.Players.Count > 0 &&
            __0.Players[0].CharacterId is { } characterId &&
            ModelDb.GetById<CharacterModel>(characterId) is ClosureModCharacter;
        if (original.HeartIcon is { } heartIcon)
        {
            heartIcon.Texture = isClosure
                ? GD.Load<Texture2D>(HealthIconPath) ?? original.HeartTexture
                : original.HeartTexture;
        }

        if (original.GoldIcon is { } goldIcon)
        {
            goldIcon.Texture = isClosure
                ? GD.Load<Texture2D>(GoldIconPath) ?? original.GoldTexture
                : original.GoldTexture;
        }

        if (isClosure)
        {
            var savedPlayer = __0.Players[0];
            __instance._healthLabel.Text = $"[color=#FF7080]{savedPlayer.CurrentHp}/{savedPlayer.MaxHp}[/color]";
            __instance._goldLabel.Text = $"[color=#9EE0FF]{savedPlayer.Gold}[/color]";
        }
    }

    private static OriginalIcons CaptureOriginalIcons(NContinueRunInfo info)
    {
        TextureRect? heartIcon = info.GetNodeOrNull<TextureRect>(HeartIconNode);
        TextureRect? goldIcon = info.GetNodeOrNull<TextureRect>(GoldIconNode);
        return new OriginalIcons(heartIcon, heartIcon?.Texture, goldIcon, goldIcon?.Texture);
    }

    private sealed record OriginalIcons(
        TextureRect? HeartIcon,
        Texture2D? HeartTexture,
        TextureRect? GoldIcon,
        Texture2D? GoldTexture);
}

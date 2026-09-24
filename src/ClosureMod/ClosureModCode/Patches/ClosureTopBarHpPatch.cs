using System.Collections.Generic;
using System.Reflection;
using ClosureMod.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace ClosureMod.Patches;

// Replace only the heart icon in Closure's top-bar HP widget. The native HP
// label remains responsible for the current/max health text and animation.
[HarmonyPatch]
internal static class ClosureTopBarHpPatch
{
    private const string HealthIconPath = "res://ClosureMod/images/characters/ClosureMod_health.png";

    private static IEnumerable<MethodBase> TargetMethods()
    {
        MethodBase? ready = AccessTools.Method(typeof(NTopBarHp), "_Ready");
        MethodBase? initialize = AccessTools.Method(typeof(NTopBarHp), nameof(NTopBarHp.Initialize));
        MethodBase? update = AccessTools.Method(typeof(NTopBarHp), "UpdateHealth");

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

    private static void Postfix(NTopBarHp __instance)
    {
        FieldInfo? playerField = AccessTools.Field(typeof(NTopBarHp), "_player");
        if (playerField?.GetValue(__instance) is not Player player ||
            player.Character is not ClosureModCharacter)
        {
            return;
        }

        Texture2D? texture = GD.Load<Texture2D>(HealthIconPath);
        if (texture is null)
        {
            return;
        }

        ApplyTexture(__instance, texture);
    }

    private static void ApplyTexture(Node node, Texture2D texture)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is TextureRect textureRect)
            {
                textureRect.Texture = texture;
            }
            else if (child is Sprite2D sprite)
            {
                sprite.Texture = texture;
            }

            ApplyTexture(child, texture);
        }
    }
}

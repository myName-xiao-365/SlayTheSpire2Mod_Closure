using System.Reflection;
using ClosureMod.Characters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ClosureMod.Patches;

[HarmonyPatch]
internal static class ClosureHealthBarWidthPatch
{
    private const string WidthVerifiedMeta = "closure_width_verified";

    private static MethodBase TargetMethod() => AccessTools.Method(
        "STS2RitsuLib.Combat.HealthBars.Patches.NHealthBarGraftUiPatchHelper:SyncHpBarToHitbox")
        ?? throw new MissingMethodException("RitsuLib health bar layout method was not found.");

    private static bool IsClosureCombatBar(NHealthBar healthBar) =>
        healthBar.GetParent()?.GetParent() is NCreature creature &&
        creature.Entity.Player?.Character is ClosureModCharacter;

    private static void Prefix(NHealthBar __0, ref float __1)
    {
        if (IsClosureCombatBar(__0))
        {
            // RitsuLib restores hitbox-based widths on every overlay refresh, including damage updates.
            __1 *= 1.6f;
        }
    }

    private static void Postfix(NHealthBar __0)
    {
        if (!IsClosureCombatBar(__0) || __0.GetParent() is not NCreatureStateDisplay display)
        {
            return;
        }

        Control bar = __0.HpBarContainer;
        float left = bar.GlobalPosition.X;
        Control block = __0._blockContainer;
        block.GlobalPosition = new Vector2(left - block.Size.X * 0.5f, block.GlobalPosition.Y);
        __0._originalBlockPosition = block.Position;

        Control hoverArea = display._hpBarHitbox;
        hoverArea.GlobalPosition = new Vector2(left, hoverArea.GlobalPosition.Y);
        hoverArea.Size = new Vector2(bar.Size.X, hoverArea.Size.Y);

        NPowerContainer powers = display._powerContainer;
        if (Mathf.Abs(powers.GlobalPosition.X - left) > 0.5f || Mathf.Abs(powers.Size.X - bar.Size.X - 25f) > 0.5f)
        {
            powers.GlobalPosition = new Vector2(left, powers.GlobalPosition.Y);
            powers.Size = new Vector2(bar.Size.X + 25f, powers.Size.Y);
            powers._originalPosition = powers.Position;
            powers.UpdatePositions();
        }

        if (!__0.HasMeta(WidthVerifiedMeta))
        {
            __0.SetMeta(WidthVerifiedMeta, true);
            Entry.Logger.Info($"Closure combat health bar actual width after RitsuLib layout: {bar.Size.X:0.#}.");
        }
    }
}

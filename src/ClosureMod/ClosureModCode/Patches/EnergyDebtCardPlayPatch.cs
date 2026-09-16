using System.Reflection;
using ClosureMod.Cards;
using ClosureMod.Powers;
using ClosureMod.Relics;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers.Models;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay), [])]
internal static class EnergyDebtCanPlaySimplePatch
{
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        if (SluggishStunLimiterPower.FindLimiter(__instance)?.BlocksCardPlay == true)
        {
            return;
        }

        if (__instance is DontWantToWork && SluggishStunLimiterPower.FindLimiter(__instance) is null)
        {
            return;
        }

        __result = ClosureModRelic.CanPlayWithEnergyDebt(__instance);
    }
}

[HarmonyPatch]
internal static class EnergyDebtCanPlayPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.CanPlay),
            [
                typeof(UnplayableReason).MakeByRefType(),
                typeof(AbstractModel).MakeByRefType()
            ]);
    }

    private static void Postfix(
        CardModel __instance,
        ref bool __result,
        ref UnplayableReason reason,
        ref AbstractModel preventer)
    {
        if (__result || reason != UnplayableReason.EnergyCostTooHigh)
        {
            return;
        }

        if (SluggishStunLimiterPower.FindLimiter(__instance)?.BlocksCardPlay == true)
        {
            return;
        }

        if (__instance is DontWantToWork && SluggishStunLimiterPower.FindLimiter(__instance) is null)
        {
            return;
        }

        if (!ClosureModRelic.CanPlayWithEnergyDebt(__instance))
        {
            return;
        }

        __result = true;
        reason = UnplayableReason.None;
        preventer = null!;
    }
}

[HarmonyPatch(typeof(CardCostHelper), nameof(CardCostHelper.GetEnergyCostColor))]
internal static class EnergyDebtCostColorPatch
{
    private static void Postfix(CardModel __0, ref CardCostColor __result)
    {
        if (__result != CardCostColor.InsufficientResources)
        {
            return;
        }

        if (SluggishStunLimiterPower.FindLimiter(__0)?.BlocksCardPlay == true)
        {
            return;
        }

        if (__0 is DontWantToWork && SluggishStunLimiterPower.FindLimiter(__0) is null)
        {
            return;
        }

        if (ClosureModRelic.CanPlayWithEnergyDebt(__0))
        {
            __result = CardCostColor.Unmodified;
        }
    }
}

[HarmonyPatch]
internal static class EnergyDebtCounterVisualPatch
{
    private static readonly FieldInfo PlayerField = AccessTools.Field(typeof(NEnergyCounter), "_player");
    private static readonly FieldInfo LabelField = AccessTools.Field(typeof(NEnergyCounter), "_label");
    private static readonly FieldInfo LayersField = AccessTools.Field(typeof(NEnergyCounter), "_layers");
    private static readonly FieldInfo RotationLayersField = AccessTools.Field(typeof(NEnergyCounter), "_rotationLayers");
    private static readonly FieldInfo BackVfxField = AccessTools.Field(typeof(NEnergyCounter), "_backVfx");
    private static readonly FieldInfo FrontVfxField = AccessTools.Field(typeof(NEnergyCounter), "_frontVfx");
    private static readonly StringName FontColorName = "font_color";
    private static readonly StringName FontOutlineColorName = "font_outline_color";
    private const string EnergyOrbPath = "res://ClosureMod/images/characters/energy.png";
    private static readonly Color NormalLayerColor = Colors.White;
    private static readonly Color DebtLimitLayerColor = new(0.52f, 0.52f, 0.52f, 1f);
    private static readonly Color NormalFontColor = new(1f, 0.9647059f, 0.8862745f, 1f);
    private static readonly Color DebtLimitFontColor = new(1f, 0.33f, 0.28f, 1f);
    private static readonly Color OutlineColor = new(0.08f, 0.18f, 0.24f, 1f);
    private static bool _loggedVisualException;

    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NEnergyCounter), nameof(NEnergyCounter._Ready));
        yield return AccessTools.Method(typeof(NEnergyCounter), nameof(NEnergyCounter.RefreshLabel));
        yield return AccessTools.Method(typeof(NEnergyCounter), "OnEnergyChanged");
        yield return AccessTools.Method(typeof(NEnergyCounter), nameof(NEnergyCounter._Process));
    }

    private static void Postfix(NEnergyCounter __instance)
    {
        try
        {
            Player? player = PlayerField.GetValue(__instance) as Player;
            int energy = player?.PlayerCombatState?.Energy ?? 0;
            int maxEnergyDebt = ClosureModRelic.GetMaxEnergyDebt(player);
            bool atDebtLimit = energy <= -maxEnergyDebt;

            if (LayersField.GetValue(__instance) is Control layers)
            {
                ApplyLayerVisual(layers, atDebtLimit);
            }

            if (LabelField.GetValue(__instance) is MegaLabel label)
            {
                label.AddThemeColorOverride(FontColorName, atDebtLimit ? DebtLimitFontColor : NormalFontColor);
                label.AddThemeColorOverride(FontOutlineColorName, OutlineColor);
            }

            HideNode(RotationLayersField.GetValue(__instance) as CanvasItem);
            HideNode(BackVfxField.GetValue(__instance) as CanvasItem);
            HideNode(FrontVfxField.GetValue(__instance) as CanvasItem);
        }
        catch (ObjectDisposedException exception)
        {
            LogVisualException(exception);
        }
        catch (InvalidOperationException exception)
        {
            LogVisualException(exception);
        }
    }

    private static void ApplyLayerVisual(Control layers, bool atDebtLimit)
    {
        layers.Material = null;
        layers.SelfModulate = NormalLayerColor;
        layers.Modulate = NormalLayerColor;

        if (layers.GetNodeOrNull<TextureRect>("Layer1") is { } baseLayer)
        {
            Texture2D? texture = GD.Load<Texture2D>(EnergyOrbPath);
            if (texture is not null)
            {
                baseLayer.Texture = texture;
            }

            baseLayer.Visible = true;
            baseLayer.Material = null;
            baseLayer.SelfModulate = atDebtLimit ? DebtLimitLayerColor : NormalLayerColor;
            baseLayer.Modulate = atDebtLimit ? DebtLimitLayerColor : NormalLayerColor;
        }

        if (atDebtLimit)
        {
            return;
        }

        ClearCanvasItemVisuals(layers);
    }

    private static void ClearCanvasItemVisuals(Node node)
    {
        if (node is CanvasItem canvasItem)
        {
            canvasItem.Material = null;
            canvasItem.Modulate = NormalLayerColor;
            canvasItem.SelfModulate = NormalLayerColor;
        }

        foreach (Node child in node.GetChildren())
        {
            ClearCanvasItemVisuals(child);
        }
    }

    private static void HideNode(CanvasItem? node)
    {
        if (node is null)
        {
            return;
        }

        node.Visible = false;
        node.Modulate = new Color(1f, 1f, 1f, 0f);
        switch (node)
        {
            case Control control:
                control.Scale = Vector2.Zero;
                break;
            case Node2D node2D:
                node2D.Scale = Vector2.Zero;
                break;
        }
    }

    private static void LogVisualException(Exception exception)
    {
        if (_loggedVisualException)
        {
            return;
        }

        _loggedVisualException = true;
        Entry.Logger.Info($"Energy counter visual skipped after scene reload: {exception.Message}");
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
internal static class EnergyDebtSpendResourcesPatch
{
    private readonly record struct EnergyDebtSpendState(int EnergyBeforeSpend, int EnergyCostToSpend, bool CostsX);

    private static void Prefix(CardModel __instance, out EnergyDebtSpendState __state)
    {
        int energyBeforeSpend = __instance.Owner?.PlayerCombatState?.Energy ?? 0;
        int energyCostToSpend = Math.Max(0, __instance.EnergyCost.GetAmountToSpend());
        __state = new EnergyDebtSpendState(energyBeforeSpend, energyCostToSpend, __instance.EnergyCost.CostsX);
    }

    private static async Task<(int, int)> Postfix(
        Task<(int, int)> __result,
        CardModel __instance,
        EnergyDebtSpendState __state)
    {
        (int energySpent, int starsSpent) result = await __result;
        Player? owner = __instance.Owner;
        var combatState = owner?.PlayerCombatState;
        if (owner is null || combatState is null || !ClosureModRelic.PlayerHasEnergyDebtRelic(owner))
        {
            return result;
        }

        int energyDebtLimit = -ClosureModRelic.GetMaxEnergyDebt(owner);
        int targetEnergy = __state.CostsX
            ? energyDebtLimit
            : Math.Max(energyDebtLimit, __state.EnergyBeforeSpend - __state.EnergyCostToSpend);
        if (targetEnergy < combatState.Energy)
        {
            combatState.Energy = targetEnergy;
        }

        if (__state.CostsX)
        {
            __instance.EnergyCost.CapturedXValue = Math.Max(0, __state.EnergyBeforeSpend - targetEnergy);
        }

        return result;
    }
}

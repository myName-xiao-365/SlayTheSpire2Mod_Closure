using System.Reflection;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
internal static class ParalysisStackLimitPatch
{
    private static void Prefix(PowerModel __instance, ref int amount)
    {
        if (__instance is ParalysisPower)
        {
            amount = Math.Min(amount, ParalysisPower.MaxStacks);
        }
    }
}

[HarmonyPatch(typeof(NPower), "OnDisplayAmountChanged")]
internal static class ParalysisIconRefreshPatch
{
    private static readonly MethodInfo Reload = AccessTools.Method(typeof(NPower), "Reload");

    private static void Postfix(NPower __instance)
    {
        if (__instance.Model is ParalysisPower)
        {
            Reload.Invoke(__instance, null);
        }
    }
}

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Damage),
    [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal),
     typeof(ValueProp), typeof(Creature), typeof(CardModel)])]
internal static class AttackSuppressionPatch
{
    private static bool Prefix(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref Task<IEnumerable<DamageResult>> __result)
    {
        if (dealer is null ||
            !(dealer.IsMonster ? props.IsPoweredAttack() : cardSource?.Type == CardType.Attack))
        {
            return true;
        }

        ParalysisPower? paralysis = ParalysisPower.Find(dealer);
        if (paralysis?.TryReserveHit() != true)
        {
            paralysis = null;
        }

        if (paralysis is null && !ShiverPower.IsActive(dealer))
        {
            return true;
        }

        __result = SuppressHit(choiceContext, targets, props, dealer, cardSource, paralysis);
        return false;
    }

    private static async Task<IEnumerable<DamageResult>> SuppressHit(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        ValueProp props,
        Creature dealer,
        CardModel? cardSource,
        ParalysisPower? paralysis)
    {
        if (paralysis is not null)
        {
            try
            {
                await PowerCmd.ModifyAmount(choiceContext, paralysis, -1, dealer, cardSource);
            }
            finally
            {
                paralysis.ReleaseHit();
            }
        }

        return targets.Select(target => new DamageResult(target, props)).ToArray();
    }
}

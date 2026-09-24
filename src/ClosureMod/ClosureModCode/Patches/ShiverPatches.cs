using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ClosureMod.Patches;

[HarmonyPatch]
internal static class ShiverCardPlayPatch
{
    private static readonly MethodInfo CardOnPlay = AccessTools.Method(typeof(CardModel), "OnPlay");
    private static readonly MethodInfo EnchantmentOnPlay = AccessTools.Method(typeof(EnchantmentModel), "OnPlay");
    private static readonly MethodInfo AfflictionOnPlay = AccessTools.Method(typeof(AfflictionModel), "OnPlay");

    private static MethodBase TargetMethod()
    {
        MethodInfo wrapper = AccessTools.Method(typeof(CardModel), nameof(CardModel.OnPlayWrapper));
        Type stateMachine = wrapper.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        return AccessTools.Method(stateMachine, "MoveNext");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(CardOnPlay))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(ShiverCardPlayPatch), nameof(PlayCardEffect));
            }
            else if (instruction.Calls(EnchantmentOnPlay))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(ShiverCardPlayPatch), nameof(PlayEnchantmentEffect));
            }
            else if (instruction.Calls(AfflictionOnPlay))
            {
                instruction.opcode = OpCodes.Call;
                instruction.operand = AccessTools.Method(typeof(ShiverCardPlayPatch), nameof(PlayAfflictionEffect));
            }

            yield return instruction;
        }
    }

    private static bool SuppressesEffects(CardModel card) =>
        card.Type == CardType.Attack && ShiverPower.IsActive(card.Owner?.Creature);

    private static Task PlayCardEffect(CardModel card, PlayerChoiceContext context, CardPlay play) =>
        SuppressesEffects(card)
            ? Task.CompletedTask
            : InvokeOnPlay(CardOnPlay, card, context, play);

    private static Task PlayEnchantmentEffect(EnchantmentModel enchantment, PlayerChoiceContext context, CardPlay play) =>
        SuppressesEffects(play.Card)
            ? Task.CompletedTask
            : InvokeOnPlay(EnchantmentOnPlay, enchantment, context, play);

    private static Task PlayAfflictionEffect(AfflictionModel affliction, PlayerChoiceContext context, Creature? target) =>
        SuppressesEffects(affliction.Card)
            ? Task.CompletedTask
            : InvokeOnPlay(AfflictionOnPlay, affliction, context, target);

    private static Task InvokeOnPlay(MethodInfo method, object model, params object?[] args)
    {
        try
        {
            return (Task)method.Invoke(model, args)!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }
}

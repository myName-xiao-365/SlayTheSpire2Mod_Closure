using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using ClosureMod.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ClosureMod.Patches;

internal static class EnergyDebtRules
{
    internal static readonly MethodInfo EnergyGetter =
        AccessTools.PropertyGetter(typeof(PlayerCombatState), nameof(PlayerCombatState.Energy));
    internal static readonly MethodInfo AvailableEnergyGetter =
        AccessTools.Method(typeof(EnergyDebtRules), nameof(GetAvailableEnergy));

    public static int GetAvailableEnergy(PlayerCombatState state)
    {
        int limit = ClosureModRelic.GetMaxEnergyDebt(state._player);
        return limit > 0 ? Math.Max(0, state.Energy + limit) : state.Energy;
    }

    public static decimal GetMinimumEnergy(PlayerCombatState state) =>
        -Math.Max(0, ClosureModRelic.GetMaxEnergyDebt(state._player));
}

[HarmonyPatch]
internal static class EnergyDebtResourcesPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(PlayerCombatState), nameof(PlayerCombatState.HasEnoughResourcesFor));
        yield return AccessTools.Method(typeof(CardEnergyCost), nameof(CardEnergyCost.GetAmountToSpend));
        MethodInfo spend = AccessTools.Method(typeof(CardModel), nameof(CardModel.SpendResources));
        Type stateMachine = spend.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        yield return AccessTools.Method(stateMachine, "MoveNext");
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        int replaced = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(EnergyDebtRules.EnergyGetter))
            {
                // Native affordability, star payment, and X costs share the same available balance.
                instruction.opcode = OpCodes.Call;
                instruction.operand = EnergyDebtRules.AvailableEnergyGetter;
                replaced++;
            }

            yield return instruction;
        }

        int expected = __originalMethod.Name == nameof(PlayerCombatState.HasEnoughResourcesFor) ? 4 : 1;
        if (replaced != expected)
        {
            throw new InvalidOperationException($"Energy debt patch: unexpected energy reads in {__originalMethod.Name}: {replaced}.");
        }
    }
}

[HarmonyPatch]
internal static class EnergyDebtCounterVisualPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(NEnergyCounter), nameof(NEnergyCounter.RefreshLabel));
        yield return AccessTools.Method(typeof(NEnergyCounter), nameof(NEnergyCounter._Process));
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        bool refreshLabel = __originalMethod.Name == nameof(NEnergyCounter.RefreshLabel);
        int energyReads = 0;
        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.Calls(EnergyDebtRules.EnergyGetter))
            {
                energyReads++;
                // The first read formats the actual signed balance; only visual state uses availability.
                if (!refreshLabel || energyReads > 1)
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = EnergyDebtRules.AvailableEnergyGetter;
                }
            }

            yield return instruction;
        }

        if (energyReads != (refreshLabel ? 5 : 1))
        {
            throw new InvalidOperationException($"Energy counter patch: unexpected energy reads in {__originalMethod.Name}: {energyReads}.");
        }
    }
}

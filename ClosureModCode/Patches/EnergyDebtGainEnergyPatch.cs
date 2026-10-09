using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ClosureMod.Patches;

[HarmonyPatch]
internal static class EnergyDebtEnergyBoundsPatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(PlayerCombatState), nameof(PlayerCombatState.GainEnergy));
        yield return AccessTools.Method(typeof(PlayerCombatState), nameof(PlayerCombatState.LoseEnergy));
    }

    private static IEnumerable<CodeInstruction> Transpiler(
        IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var codes = instructions.ToList();
        FieldInfo zero = AccessTools.Field(typeof(decimal), nameof(decimal.Zero));
        MethodInfo minimum = AccessTools.Method(typeof(EnergyDebtRules), nameof(EnergyDebtRules.GetMinimumEnergy));
        int replaced = 0;
        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction instruction = codes[i];
            if (instruction.LoadsField(zero) && i + 1 < codes.Count && codes[i + 1].LoadsConstant(999999999))
            {
                // Change only the clamp's lower bound, retaining native validation and change events.
                instruction.opcode = OpCodes.Ldarg_0;
                instruction.operand = null;
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Call, minimum);
                replaced++;
            }
            else
            {
                yield return instruction;
            }
        }

        if (replaced != 1)
        {
            throw new InvalidOperationException($"Energy debt patch: lower bound not found in {__originalMethod.Name}.");
        }
    }
}

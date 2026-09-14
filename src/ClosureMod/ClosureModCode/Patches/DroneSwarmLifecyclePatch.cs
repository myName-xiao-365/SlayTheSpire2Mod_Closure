using ClosureMod.Summons;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterBlockGained),
    [typeof(ICombatState), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel)])]
internal static class DroneSwarmAfterBlockGainedPatch
{
    private static void Postfix(
        ICombatState __0,
        Creature __1,
        decimal __2,
        CardModel? __4,
        ref Task __result)
    {
        __result = After(__result, () => DroneSwarmManager.HandleBlockGained(__0, __1, __2, __4));
    }

    private static async Task After(Task original, Func<Task> next)
    {
        await original;
        await next();
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterCurrentHpChanged),
    [typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(decimal)])]
internal static class DroneSwarmAfterCurrentHpChangedPatch
{
    private static void Postfix(
        ICombatState __1,
        Creature __2,
        decimal __3,
        ref Task __result)
    {
        __result = After(__result, () => DroneSwarmManager.HandleCurrentHpChanged(__1, __2, __3));
    }

    private static async Task After(Task original, Func<Task> next)
    {
        await original;
        await next();
    }
}

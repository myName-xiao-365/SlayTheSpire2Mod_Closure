using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.Heal))]
internal static class EmberHealingPatch
{
    private static bool Prefix(Creature creature, ref Task __result)
    {
        if (!creature.Powers.OfType<EmberPower>().Any(power => power.Amount > 0))
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}

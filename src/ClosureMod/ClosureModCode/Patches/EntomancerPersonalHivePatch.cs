using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Monsters;

namespace ClosureMod.Patches;

[HarmonyPatch]
internal static class EntomancerPersonalHivePatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(Entomancer), "SpitMove");
    }

    private static void Postfix(ref Task __result)
    {
        __result = GuardPersonalHiveRemoved(__result);
    }

    private static async Task GuardPersonalHiveRemoved(Task original)
    {
        try
        {
            await original;
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("Sequence contains no elements", StringComparison.Ordinal))
        {
            Entry.Logger.Info("Entomancer spit move skipped because Personal Hive was removed.");
        }
    }
}

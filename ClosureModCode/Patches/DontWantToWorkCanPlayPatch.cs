using System.Reflection;
using ClosureMod.Cards;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay), [])]
[HarmonyPriority(Priority.Last)]
internal static class DontWantToWorkCanPlaySimplePatch
{
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__instance is not DontWantToWork)
        {
            return;
        }

        __result = __result && SluggishStunLimiterPower.IsStunned(__instance);
    }
}

[HarmonyPatch]
[HarmonyPriority(Priority.Last)]
internal static class DontWantToWorkCanPlayPatch
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
        if (__instance is not DontWantToWork || !__result)
        {
            return;
        }

        if (SluggishStunLimiterPower.IsStunned(__instance))
        {
            return;
        }

        __result = false;
        reason = UnplayableReason.BlockedByHook;
        preventer = __instance;
    }
}

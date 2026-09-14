using System.Reflection;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.CanPlay), [])]
internal static class SluggishCanPlaySimplePatch
{
    private static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        __result = SluggishStunLimiterPower.FindLimiter(__instance)?.BlocksCardPlay != true;
    }
}

[HarmonyPatch]
internal static class SluggishCanPlayPatch
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
        if (!__result)
        {
            return;
        }

        SluggishStunLimiterPower? limiter = SluggishStunLimiterPower.FindLimiter(__instance);
        if (limiter?.BlocksCardPlay != true)
        {
            return;
        }

        __result = false;
        reason = UnplayableReason.BlockedByHook;
        preventer = limiter;
    }
}

using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ClosureMod.Patches;

[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
internal static class RestSiteCharacterVisualPatch
{
    private static void Prefix(NRestSiteCharacter __instance)
    {
        ClosureNonCombatVisualController.InstallIdleVisual(__instance);
    }

    private static void Postfix(NRestSiteCharacter __instance)
    {
        ClosureNonCombatVisualController.InstallIdleVisual(__instance);
    }
}

[HarmonyPatch(typeof(NMerchantCharacter), nameof(NMerchantCharacter._Ready))]
internal static class MerchantCharacterVisualPatch
{
    private static void Prefix(NMerchantCharacter __instance)
    {
        ClosureNonCombatVisualController.InstallIdleVisual(__instance);
    }

    private static void Postfix(NMerchantCharacter __instance)
    {
        ClosureNonCombatVisualController.InstallIdleVisual(__instance);
    }
}

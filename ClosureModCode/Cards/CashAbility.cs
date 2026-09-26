using ClosureMod.Characters;
using ClosureMod.Powers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class CashAbility : ModCardTemplate
{
    protected override bool HasEnergyCostX => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public CashAbility() : base(0, CardType.Power, CardRarity.Rare, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int cardsPerTurn = Math.Max(0, ResolveEnergyXValue());
        if (cardsPerTurn == 0)
        {
            return;
        }

        CashAbilityPower? power = await PowerCmd.Apply<CashAbilityPower>(
            choiceContext,
            Owner.Creature,
            cardsPerTurn,
            Owner.Creature,
            this);
        if (power is not null)
        {
            power.UpgradeSupportCards |= IsUpgraded;
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class CashAbilityDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is CashAbility cashAbility)
        {
            __result = new LocString(
                "cards",
                cashAbility.IsUpgraded
                    ? "CLOSURE_MOD_CARD_CASH_ABILITY.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_CASH_ABILITY.description");
        }
    }
}

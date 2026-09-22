using ClosureMod.Characters;
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
public sealed class LimitedRebate : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        // Temporary shared art until a dedicated LimitedRebate portrait is provided.
        PortraitPath: $"{Entry.ResPath}/images/cards/LimitedRebate.png");

    public LimitedRebate() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int energyGain = EnergyCost.CapturedXValue * 2 + (IsUpgraded ? 1 : 0);
        if (energyGain > 0)
        {
            await PlayerCmd.GainEnergy(energyGain, Owner);
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class LimitedRebateDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is LimitedRebate limitedRebate)
        {
            __result = new LocString(
                "cards",
                limitedRebate.IsUpgraded
                    ? "CLOSURE_MOD_CARD_LIMITED_REBATE.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_LIMITED_REBATE.description");
        }
    }
}

using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class Overstock : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/ForceBuySell.png");

    public Overstock() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int maxCards = Math.Max(0, ResolveEnergyXValue());
        int handCount = Owner.PlayerCombatState?.Hand.Cards.Count(card => !ReferenceEquals(card, this)) ?? 0;
        maxCards = Math.Min(maxCards, handCount);
        if (maxCards == 0)
        {
            return;
        }

        var selectorPrefs = new CardSelectorPrefs(
            new LocString("cards", "CLOSURE_MOD_CARD_OVERSTOCK.selectPrompt"),
            0,
            maxCards)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            selectorPrefs,
            card => !ReferenceEquals(card, this),
            this)).ToList();

        foreach (CardModel selectedCard in selectedCards)
        {
            CardPileAddResult? transformResult = await CardCmd.TransformTo<DerivedCard>(
                selectedCard,
                CardPreviewStyle.HorizontalLayout);
            if (IsUpgraded && transformResult is { } result && result.cardAdded.IsUpgradable)
            {
                CardCmd.Upgrade(result.cardAdded, CardPreviewStyle.HorizontalLayout);
            }
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class OverstockDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not Overstock overstock)
        {
            return;
        }

        __result = new LocString(
            "cards",
            overstock.IsUpgraded
                ? "CLOSURE_MOD_CARD_OVERSTOCK.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_OVERSTOCK.description");
    }
}

using ClosureMod.Characters;
using ClosureMod.Keywords;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class DebtFinancing : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Skill;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.Self;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool HasEnergyCostX => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.jpg");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DrawOffset", 0)
    ];

    public DebtFinancing() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int cardsToDraw = Math.Max(0, ResolveEnergyXValue() + (int)DynamicVars["DrawOffset"].BaseValue);
        if (cardsToDraw <= 0)
        {
            return;
        }

        IReadOnlyList<CardModel> drawnCards = (await CardPileCmd.Draw(choiceContext, cardsToDraw, Owner)).ToList();
        if (drawnCards.Count <= 0)
        {
            return;
        }

        int maxSelect = Math.Min(cardsToDraw, drawnCards.Count);
        var selectorPrefs = new CardSelectorPrefs(
            new LocString("cards", "CLOSURE_MOD_CARD_DEBT_FINANCING.selectPrompt"),
            0,
            maxSelect)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        HashSet<CardModel> drawnCardSet = drawnCards.ToHashSet();
        HashSet<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            selectorPrefs,
            drawnCardSet.Contains,
            this)).ToHashSet();

        List<CardModel> cardsToDiscard = drawnCards
            .Where(card => !selectedCards.Contains(card))
            .ToList();
        if (cardsToDiscard.Count <= 0)
        {
            return;
        }

        await CardCmd.Discard(choiceContext, cardsToDiscard);
        await PlayerCmd.GainEnergy(cardsToDiscard.Count, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["DrawOffset"].UpgradeValueBy(1);
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class DebtFinancingDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not DebtFinancing debtFinancing)
        {
            return;
        }

        __result = new LocString(
            "cards",
            debtFinancing.IsUpgraded
                ? "CLOSURE_MOD_CARD_DEBT_FINANCING.descriptionUpgraded"
                : "CLOSURE_MOD_CARD_DEBT_FINANCING.description");
    }
}

using ClosureMod.Characters;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class ExchangeGoods : ModCardTemplate
{
    [SavedProperty]
    public int PermanentCost
    {
        get => EnergyCost.GetWithModifiers(CostModifiers.None);
        set => EnergyCost.SetCustomBaseCost(value);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Eternal];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public ExchangeGoods() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardPile? hand = Owner.PlayerCombatState?.Hand;
        if (hand is null || hand.Cards.Count(card => !ReferenceEquals(card, this) && card.IsTransformable) == 0)
        {
            IncreasePermanentCost();
            return;
        }

        var selectorPrefs = new CardSelectorPrefs(
            new LocString("cards", "CLOSURE_MOD_CARD_EXCHANGE_GOODS.selectPrompt"),
            1,
            1)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        CardModel? selectedCard = (await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            selectorPrefs,
            card => !ReferenceEquals(card, this) && card.IsTransformable,
            this)).FirstOrDefault();

        if (selectedCard is not null)
        {
            CardModel permanentVersion = selectedCard.DeckVersion is { HasBeenRemovedFromState: false } deckVersion
                ? deckVersion
                : selectedCard;
            CardPileAddResult? permanentTransform = await CardCmd.TransformToRandom(
                permanentVersion,
                Owner.RunState.Rng.CombatCardGeneration,
                ReferenceEquals(permanentVersion, selectedCard)
                    ? CardPreviewStyle.HorizontalLayout
                    : CardPreviewStyle.None);

            if (permanentTransform is { success: true } result)
            {
                if (IsUpgraded && result.cardAdded.IsUpgradable)
                {
                    CardCmd.Upgrade(result.cardAdded, CardPreviewStyle.None);
                }

                if (!ReferenceEquals(permanentVersion, selectedCard))
                {
                    // Deck cards must be copied into the combat scope, not cloned as played cards.
                    CardModel combatReplacement = selectedCard.CombatState!.CloneCard(result.cardAdded);
                    combatReplacement.DeckVersion = result.cardAdded;
                    await CardCmd.Transform(selectedCard, combatReplacement, CardPreviewStyle.HorizontalLayout);
                }
            }
        }

        IncreasePermanentCost();
    }

    private void IncreasePermanentCost()
    {
        PermanentCost++;

        if (DeckVersion is ExchangeGoods { HasBeenRemovedFromState: false } deckVersion &&
            !ReferenceEquals(deckVersion, this))
        {
            deckVersion.PermanentCost++;
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class ExchangeGoodsDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is ExchangeGoods exchangeGoods)
        {
            __result = new LocString(
                "cards",
                exchangeGoods.IsUpgraded
                    ? "CLOSURE_MOD_CARD_EXCHANGE_GOODS.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_EXCHANGE_GOODS.description");
        }
    }
}

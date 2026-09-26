using ArkBase.Api;
using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
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
public sealed class FirstStep : ModCardTemplate
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        IsUpgraded ? [ClosureKeywords.EliteTwo, ClosureKeywords.UpgradeAction] : [ClosureKeywords.EliteTwo];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    public FirstStep() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = Owner.PlayerCombatState?.Hand;
        if (hand is null || !hand.Cards.Any(card => card.Rarity == CardRarity.Common && SupportCards.CanPromote(card)))
        {
            return;
        }

        var selectorPrefs = new CardSelectorPrefs(
            new LocString("cards", "CLOSURE_MOD_CARD_FIRST_STEP.selectPrompt"),
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
            card => card.Rarity == CardRarity.Common && SupportCards.CanPromote(card),
            this)).FirstOrDefault();
        if (selectedCard is null)
        {
            return;
        }

        NextTurnSupportPromotionPower? pendingPromotion = await PowerCmd.Apply<NextTurnSupportPromotionPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
        if (pendingPromotion is null)
        {
            return;
        }

        pendingPromotion.AddCard(selectedCard, IsUpgraded);
        await CardPileCmd.Add(selectedCard, PileType.Exhaust, CardPilePosition.Top, this, false);
    }

}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
internal static class FirstStepDescriptionPatch
{
    private static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is FirstStep firstStep)
        {
            __result = new LocString(
                "cards",
                firstStep.IsUpgraded
                    ? "CLOSURE_MOD_CARD_FIRST_STEP.descriptionUpgraded"
                    : "CLOSURE_MOD_CARD_FIRST_STEP.description");
        }
    }
}

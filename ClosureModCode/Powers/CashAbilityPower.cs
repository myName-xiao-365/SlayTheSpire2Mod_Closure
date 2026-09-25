using ArkBase.Api;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class CashAbilityPower : ModPowerTemplate
{
    public bool UpgradeSupportCards { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature) || Amount <= 0 ||
            player.PlayerCombatState is not { } combatState)
        {
            return;
        }

        List<CardModel> hand = combatState.Hand.Cards.ToList();
        List<CardModel> supportCards = SupportCards.GetCards(CardRarity.Common).ToList();
        if (hand.Count == 0 || supportCards.Count == 0)
        {
            return;
        }

        int selectionCount = Math.Min(Amount, hand.Count);
        var selectorPrefs = new CardSelectorPrefs(
            new LocString("cards", "CLOSURE_MOD_CARD_CASH_ABILITY.selectPrompt"),
            0,
            selectionCount)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        List<CardModel> selectedCards = (await CardSelectCmd.FromHand(
            choiceContext,
            player,
            selectorPrefs,
            _ => true,
            this)).ToList();
        if (selectedCards.Count == 0)
        {
            return;
        }

        Flash();
        IEnumerable<CardPileAddResult> transformedCards = await CardCmd.Transform(
            selectedCards.Select(card => new CardTransformation(card, supportCards)),
            player.RunState.Rng.CombatCardGeneration,
            CardPreviewStyle.HorizontalLayout);

        if (!UpgradeSupportCards)
        {
            return;
        }

        foreach (CardPileAddResult result in transformedCards)
        {
            if (result.cardAdded.IsUpgradable)
            {
                CardCmd.Upgrade(result.cardAdded, CardPreviewStyle.HorizontalLayout);
            }
        }
    }
}

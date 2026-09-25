using ArkBase.Api;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class NextTurnSupportPromotionPower : ModPowerTemplate
{
    private readonly List<(CardModel Card, bool UpgradePromotedCard)> _pendingCards = [];

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/RandomPromotionPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/RandomPromotionPower.png");

    public void AddCard(CardModel card, bool upgradePromotedCard)
    {
        _pendingCards.Add((card, upgradePromotedCard));
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature) || _pendingCards.Count == 0)
        {
            return;
        }

        foreach ((CardModel card, bool upgradePromotedCard) in _pendingCards.ToArray())
        {
            if (card.HasBeenRemovedFromState)
            {
                continue;
            }

            CardPileAddResult? promotion = await SupportCards.Promote(card);
            CardModel returnedCard = promotion?.cardAdded ?? card;
            if (upgradePromotedCard && returnedCard.IsUpgradable)
            {
                CardCmd.Upgrade(returnedCard, CardPreviewStyle.None);
            }

            await CardPileCmd.Add(returnedCard, PileType.Hand, CardPilePosition.Top, this, false);
        }

        _pendingCards.Clear();
        await PowerCmd.Remove(this);
    }
}

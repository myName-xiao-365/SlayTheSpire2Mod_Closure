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
public sealed class RandomPromotionPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/RandomPromotionPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/RandomPromotionPower.png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature) || Amount <= 0 || player.PlayerCombatState is not { } combatState)
        {
            return;
        }

        for (int i = 0; i < Amount; i++)
        {
            List<CardModel> candidates = combatState.Hand.Cards
                .Where(card => card.Rarity == CardRarity.Common && SupportCards.CanPromote(card))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            Flash();
            CardModel selected = candidates[player.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count)];
            CardModel? deckVersion = selected.DeckVersion is { HasBeenRemovedFromState: false } deckCard &&
                                     !ReferenceEquals(deckCard, selected)
                ? deckCard
                : null;

            if (deckVersion is not null)
            {
                await SupportCards.Promote(deckVersion);
            }

            await SupportCards.Promote(selected);
        }
    }
}

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
public sealed class FriendPower : ModPowerTemplate
{
    public bool UpgradeGeneratedCards { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature) || Owner.CombatState is not { } combatState)
        {
            return;
        }

        List<CardModel> candidates = SupportCards.GetCards(CardRarity.Common).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        Flash();
        CardModel generatedCard = combatState.CreateCard(
            candidates[player.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count)],
            player);
        if (UpgradeGeneratedCards && generatedCard.IsUpgradable)
        {
            CardCmd.Upgrade(generatedCard, CardPreviewStyle.None);
        }

        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, player);
    }
}

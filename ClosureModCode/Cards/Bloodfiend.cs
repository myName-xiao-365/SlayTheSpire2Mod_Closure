using ArkBase.Api;
using ClosureMod.Characters;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class Bloodfiend : ModCardTemplate
{
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/Bloodfiend.jpg");

    public Bloodfiend() : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GenerateSupportCard();

        await PowerCmd.Apply<BloodfiendPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this);
    }

    private async Task GenerateSupportCard()
    {
        if (CombatState is not { } combatState)
        {
            return;
        }

        List<CardModel> candidates = SupportCards.GetCards(CardRarity.Rare).ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        CardModel generatedCard = combatState.CreateCard(
            candidates[Owner.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count)],
            Owner);
        if (IsUpgraded && generatedCard.IsUpgradable)
        {
            CardCmd.Upgrade(generatedCard, CardPreviewStyle.None);
        }

        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, Owner);
    }
}

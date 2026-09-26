using ArkBase.Api;
using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class EliteOperator : ModCardTemplate
{
    public override int MaxUpgradeLevel => int.MaxValue;

    // Loading applies saved properties before replaying all upgrades.
    [SavedProperty]
    public int UpgradeReplayBaseCost
    {
        get => EnergyCost.GetWithModifiers(CostModifiers.None) + CurrentUpgradeLevel;
        set => EnergyCost.SetCustomBaseCost(value);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/EliteOperator.png");

    public EliteOperator() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self, showInCardLibrary: true)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is { } combatState)
        {
            IReadOnlyList<CardModel> candidates = SupportCards.GetCards(CardRarity.Rare);
            if (candidates.Count > 0)
            {
                CardModel generated = combatState.CreateCard(
                    candidates[Owner.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count)], Owner);
                generated.EnergyCost.SetThisTurn(0);
                generated.SetStarCostThisTurn(0);
                await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, Owner);
            }
        }

        IncreasePermanentCost(this);
        if (DeckVersion is EliteOperator { HasBeenRemovedFromState: false } deckVersion &&
            !ReferenceEquals(deckVersion, this))
        {
            IncreasePermanentCost(deckVersion);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private static void IncreasePermanentCost(EliteOperator card)
    {
        card.EnergyCost.SetCustomBaseCost(card.EnergyCost.GetWithModifiers(CostModifiers.None) + 1);
    }
}

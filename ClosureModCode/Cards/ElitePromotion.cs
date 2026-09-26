using ArkBase.Api;
using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class ElitePromotion : ModCardTemplate
{
    private const int BaseEnergyCost = 3;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/ElitePromotion.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(21, ValueProp.Move)
    ];

    public ElitePromotion() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        if (Owner.PlayerCombatState is not { } combatState)
        {
            return;
        }

        List<SupportCardTemplate> candidates = combatState.Hand.Cards
            .OfType<SupportCardTemplate>()
            .Where(card => card.Rarity == CardRarity.Common && SupportCards.CanPromote(card))
            .ToList();
        if (candidates.Count == 0)
        {
            return;
        }

        SupportCardTemplate selected = candidates[
            Owner.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count)];
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

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(4);
    }
}

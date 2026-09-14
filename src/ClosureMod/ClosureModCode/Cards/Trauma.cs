using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class Trauma : ModCardTemplate
{
    private const int BaseEnergyCost = -1;
    private const CardType CardKind = CardType.Curse;
    private const CardRarity CardRarityValue = CardRarity.Curse;
    private const TargetType CardTarget = TargetType.None;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Eternal];

    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override bool HasTurnEndInHandEffect => true;
    public override int MaxUpgradeLevel => 0;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/ClearDebt.png");

    public Trauma() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        if (Owner.Creature.MaxHp <= 1)
        {
            return;
        }

        await CreatureCmd.LoseMaxHp(choiceContext, Owner.Creature, 1, isFromCard: true);
    }
}

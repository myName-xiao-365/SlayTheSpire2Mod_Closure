using ClosureMod.Patches;
using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class AutoVerification : ModCardTemplate
{
    private const int BaseEnergyCost = 3;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;
    private const int TriggerThreshold = 3;

    private bool _autoPlayInProgress;

    public override bool ShouldReceiveCombatHooks => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/AutoVerification.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(24, ValueProp.Move)
    ];

    public AutoVerification() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner.Creature) ||
            _autoPlayInProgress ||
            Pile?.Type is not (PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust) ||
            !TurnEnergySpendTracker.HasPlayedCardCostingAtLeast(Owner, TriggerThreshold))
        {
            return;
        }

        _autoPlayInProgress = true;
        try
        {
            await CardCmd.AutoPlay(
                choiceContext,
                this,
                target: null,
                AutoPlayType.Default,
                skipXCapture: false,
                skipCardPileVisuals: false);
        }
        finally
        {
            _autoPlayInProgress = false;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6);
    }
}

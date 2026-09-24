using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class Gift : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.RandomEnemy;
    private const bool ShowInCardLibrary = true;

    private bool _autoPlayInProgress;

    protected override bool HasEnergyCostX => true;

    public override bool ShouldReceiveCombatHooks => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/Gift.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(4, ValueProp.Move)
    ];

    public Gift() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int repeatCount = Math.Max(0, EnergyCost.CapturedXValue);
        if (repeatCount == 0 || CombatState?.HittableEnemies.Any() != true)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(repeatCount)
            .FromCard(this)
            .TargetingRandomOpponents(CombatState, true)
            .Execute(choiceContext);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        CardModel playedCard = cardPlay.Card;
        if (_autoPlayInProgress ||
            ReferenceEquals(playedCard, this) ||
            playedCard.Owner != Owner ||
            !playedCard.EnergyCost.CostsX ||
            Pile?.Type is not (PileType.Hand or PileType.Draw or PileType.Discard or PileType.Exhaust))
        {
            return;
        }

        _autoPlayInProgress = true;
        try
        {
            EnergyCost.CapturedXValue = Math.Max(0, playedCard.EnergyCost.CapturedXValue);
            await CardCmd.AutoPlay(
                choiceContext,
                this,
                target: null,
                AutoPlayType.Default,
                skipXCapture: true,
                skipCardPileVisuals: false);
        }
        finally
        {
            _autoPlayInProgress = false;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}

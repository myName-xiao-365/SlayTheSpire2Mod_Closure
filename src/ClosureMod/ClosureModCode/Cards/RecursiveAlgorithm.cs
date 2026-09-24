using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class RecursiveAlgorithm : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.RandomEnemy;
    private const bool ShowInCardLibrary = true;
    private int _currentHitBonus;

    protected override bool HasEnergyCostX => true;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9, ValueProp.Move),
        new CalculationBaseVar(0),
        new ExtraDamageVar(1),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (card, _) => card.DynamicVars.Damage.BaseValue + ((RecursiveAlgorithm)card)._currentHitBonus)
    ];

    public RecursiveAlgorithm() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int hitCount = Math.Max(0, ResolveEnergyXValue());
        if (hitCount == 0 || CombatState?.HittableEnemies.Any() != true)
        {
            return;
        }

        _currentHitBonus = 0;
        int nextHit = 0;
        AttackCommand attack = DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .WithHitCount(hitCount)
            .FromCard(this)
            .TargetingRandomOpponents(CombatState, true);
        attack.BeforeDamage(() =>
        {
            _currentHitBonus = nextHit++;
            return Task.CompletedTask;
        });
        try
        {
            await attack.Execute(choiceContext);
        }
        finally
        {
            _currentHitBonus = 0;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2);
    }
}

using ClosureMod.Characters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureSupportCardPool))]
public sealed class ChixiaoTianwei : SupportCardTemplate
{
    public override bool ShouldReceiveCombatHooks => IsInCombat;

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/ChixiaoTianwei.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(12, ValueProp.Move),
        new DynamicVar("HitCount", 2),
        new DynamicVar("MinimumPercent", 6)
    ];

    public ChixiaoTianwei() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState is not { } combatState)
        {
            return;
        }

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount((int)DynamicVars["HitCount"].BaseValue)
            .FromCard(this)
            .TargetingAllOpponents(combatState)
            .Execute(choiceContext);
    }

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (!ReferenceEquals(cardSource, this) || !ReferenceEquals(dealer, Owner.Creature) ||
            !props.IsPoweredAttack())
        {
            return amount;
        }

        decimal minimum = Math.Ceiling(target.CurrentHp * DynamicVars["MinimumPercent"].BaseValue / 100m);
        return Math.Max(amount, minimum);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(-2);
        DynamicVars["HitCount"].UpgradeValueBy(1);
        DynamicVars["MinimumPercent"].UpgradeValueBy(2);
    }
}

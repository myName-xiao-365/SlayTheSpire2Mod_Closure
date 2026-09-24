using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
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
public sealed class FaultDetection : ModCardTemplate
{
    private const int BaseEnergyCost = 0;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Uncommon;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;
    private bool _bonusCurrentHit;

    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ClosureKeywords.Sluggish];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/FaultDetection.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
        new DynamicVar("BonusDamage", 4),
        new CalculationBaseVar(0),
        new ExtraDamageVar(1),
        new CalculatedDamageVar(ValueProp.Move).WithMultiplier(
            (card, _) => card.DynamicVars.Damage.BaseValue +
                         (((FaultDetection)card)._bonusCurrentHit ? card.DynamicVars["BonusDamage"].BaseValue : 0))
    ];

    public FaultDetection() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        int repeatCount = Math.Max(0, EnergyCost.CapturedXValue);
        if (repeatCount == 0)
        {
            return;
        }

        AttackCommand attack = DamageCmd.Attack(DynamicVars.CalculatedDamage)
            .WithHitCount(repeatCount)
            .FromCard(this)
            .Targeting(cardPlay.Target);
        attack.BeforeDamage(async () =>
        {
            _bonusCurrentHit = false;
            if (!cardPlay.Target.IsAlive)
            {
                return;
            }

            SluggishPower? sluggish = cardPlay.Target.Powers
                .OfType<SluggishPower>()
                .FirstOrDefault(power => power.Amount > 0);
            if (sluggish is not null)
            {
                _bonusCurrentHit = true;
                await PowerCmd.ModifyAmount(choiceContext, sluggish, -1, Owner.Creature, this);
            }
        });

        try
        {
            await attack.Execute(choiceContext);
        }
        finally
        {
            _bonusCurrentHit = false;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3);
    }
}

using ClosureMod.Characters;
using ClosureMod.Keywords;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Cards.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Cards;

[RegisterCard(typeof(ClosureModCardPool))]
public sealed class NumericalDetection : ModCardTemplate
{
    private const int BaseEnergyCost = 3;
    private const CardType CardKind = CardType.Attack;
    private const CardRarity CardRarityValue = CardRarity.Rare;
    private const TargetType CardTarget = TargetType.AnyEnemy;
    private const bool ShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [ClosureKeywords.Sluggish];

    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"{Entry.ResPath}/images/cards/NumericalDetection.png");

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(36, ValueProp.Move),
        new DynamicVar("DamagePerSluggish", 5),
        ModCardVars.ComputedDamage("TotalDamage", 36,
            (card, target) => card is NumericalDetection detection ? detection.CalculateDamage(target) : 36, ValueProp.Move)
    ];

    public NumericalDetection() : base(BaseEnergyCost, CardKind, CardRarityValue, CardTarget, ShowInCardLibrary)
    {
    }

    private decimal CalculateDamage(Creature? target)
    {
        decimal sluggishStacks = target?.Powers
            .OfType<SluggishPower>()
            .Where(power => power.Amount > 0)
            .Sum(power => power.Amount) ?? 0;
        return DynamicVars.Damage.BaseValue +
               sluggishStacks * DynamicVars["DamagePerSluggish"].BaseValue;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(CalculateDamage(cardPlay.Target))
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6);
        DynamicVars["TotalDamage"].UpgradeValueBy(6);
    }
}

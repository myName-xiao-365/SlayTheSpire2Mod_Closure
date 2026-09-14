using ClosureMod.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.AttackHits;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class TimelyFeedbackPower : ModPowerTemplate, IAttackHitHookListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<string> RegisteredKeywordIds => [ClosureKeywords.SluggishId];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/SluggishPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/SluggishPower.png");

    public async Task AfterAttackHit(AttackHitContext context)
    {
        if (!ReferenceEquals(context.Dealer, Owner) || context.Damage <= 0 || Amount <= 0)
        {
            return;
        }

        int sluggishStacks = context.Targets
            .Where(target => target is { IsEnemy: true })
            .Distinct()
            .Sum(target => target.Powers.OfType<SluggishPower>()
                .Where(power => power.Amount > 0)
                .Sum(power => power.Amount));
        int blockAmount = sluggishStacks * Amount;
        if (blockAmount <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner, blockAmount, ValueProp.Unpowered, null);
    }
}

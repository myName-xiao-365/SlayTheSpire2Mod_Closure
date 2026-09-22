using ClosureMod.Keywords;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class MerchantFormPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<string> RegisteredKeywordIds => [ClosureKeywords.SluggishId];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/MerchantFormPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/MerchantFormPower.png");

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!ReferenceEquals(dealer, Owner) || !props.IsPoweredAttack())
        {
            return 1m;
        }

        int sluggishStacks = Math.Max(0, Owner.GetPowerAmount<SluggishPower>());
        if (sluggishStacks == 0)
        {
            return 1m;
        }

        return 1m + sluggishStacks * SluggishPower.DamageLossPerStack;
    }
}

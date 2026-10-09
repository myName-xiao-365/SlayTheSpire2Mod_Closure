using ArkBase.Api;
using ClosureMod.Keywords;
using ClosureMod.Utils;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class SluggishThresholdDownPower : ModPowerTemplate, ISluggishThresholdModifier
{
    public const int MaxStacks = 9;

    public int SluggishThresholdReduction => Math.Clamp(Amount, 0, MaxStacks);
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        RelatedPowerHoverTips.For<SluggishPower>(this, ClosureKeywords.SluggishId);

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/SluggishThresholdDownPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/SluggishThresholdDownPower.png");

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await ResolveExistingSluggish(new ThrowingPlayerChoiceContext(), applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount <= 0 || amount == Amount)
        {
            return;
        }

        await ResolveExistingSluggish(choiceContext, applier, cardSource);
    }

    private async Task ResolveExistingSluggish(
        PlayerChoiceContext choiceContext,
        Creature? applier,
        CardModel? cardSource)
    {
        SluggishPower? sluggish = Owner.Powers
            .OfType<SluggishPower>()
            .FirstOrDefault(power => power.Amount > 0);
        if (sluggish is null)
        {
            return;
        }

        await sluggish.ResolveStunThreshold(choiceContext, applier ?? Owner, cardSource);
    }
}

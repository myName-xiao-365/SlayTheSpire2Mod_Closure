using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class SluggishGuardPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/SluggishPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/SluggishPower.png");

    public static async Task ResolveSluggishApplied(
        PlayerChoiceContext choiceContext,
        decimal appliedAmount,
        Creature? applier,
        CardModel? cardSource)
    {
        int stacksApplied = (int)Math.Floor(appliedAmount);
        if (stacksApplied <= 0 || applier is null)
        {
            return;
        }

        foreach (SluggishGuardPower power in applier.Powers
            .OfType<SluggishGuardPower>()
            .Where(power => power.Amount > 0)
            .ToList())
        {
            int block = stacksApplied * power.Amount;
            if (block <= 0)
            {
                continue;
            }

            power.Flash();
            await CreatureCmd.GainBlock(applier, block, ValueProp.Unpowered, null);
        }
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        await PowerCmd.Remove(this);
    }
}

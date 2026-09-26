using ArkBase.Api;
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
public sealed class SluggishGuardPower : ModPowerTemplate, ISluggishAppliedListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
    IconPath: $"{Entry.ResPath}/images/powers/SluggishGuardPower.png",
    BigIconPath: $"{Entry.ResPath}/images/powers/SluggishGuardPower.png");

    public async Task OnSluggishApplied(
        PlayerChoiceContext choiceContext,
        int stacksApplied,
        Creature applier,
        CardModel? cardSource)
    {
        if (stacksApplied <= 0 || Amount <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(applier, stacksApplied * Amount, ValueProp.Unpowered, null);
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

using ClosureMod.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class CostRefundPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
    IconPath: $"{Entry.ResPath}/images/powers/CostRefundPower.png",
    BigIconPath: $"{Entry.ResPath}/images/powers/CostRefundPower.png");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner.Player;
        if (player is null || cardPlay.Card.Owner != player || cardPlay.Card is CostRefund)
        {
            return;
        }

        int costValue = Math.Max(cardPlay.Resources.EnergyValue, cardPlay.Resources.EnergySpent);
        int refund = Math.Max(0, costValue / 2);
        if (refund <= 0)
        {
            return;
        }

        Flash();
        await PlayerCmd.GainEnergy(refund, player);
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

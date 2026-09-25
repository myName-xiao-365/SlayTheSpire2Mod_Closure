using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class DefensiveExpensePower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/DefensiveExpensePower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/DefensiveExpensePower.png");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner.Player;
        if (player is null || cardPlay.Card.Owner != player || !cardPlay.Card.EnergyCost.CostsX)
        {
            return;
        }

        int energySpent = Math.Max(0, cardPlay.Card.EnergyCost.CapturedXValue);
        int block = energySpent * Amount;
        if (block <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.GainBlock(Owner, block, ValueProp.Unpowered, null);
    }
}

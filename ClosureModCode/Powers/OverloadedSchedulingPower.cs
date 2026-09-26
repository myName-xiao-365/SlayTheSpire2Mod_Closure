using ClosureMod.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class OverloadedSchedulingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<string> RegisteredKeywordIds => [ClosureKeywords.SluggishId];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/OverloadedSchedulingPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/OverloadedSchedulingPower.png");

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return ReferenceEquals(Owner, player.Creature) ? amount + Amount : amount;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature) || Amount <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<SluggishPower>(choiceContext, Owner, Amount, Owner, null);
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class TimelyStopNextTurnDrawPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/characters/energy.png",
        BigIconPath: $"{Entry.ResPath}/images/characters/energy.png");

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        if (!ReferenceEquals(Owner, player.Creature))
        {
            return count;
        }

        return count + Amount;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature))
        {
            return;
        }

        await PowerCmd.Remove(this);
    }
}

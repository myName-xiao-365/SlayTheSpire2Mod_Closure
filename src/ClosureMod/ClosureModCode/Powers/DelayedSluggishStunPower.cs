using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class DelayedSluggishStunPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/power_atlas.sprites/ringing_power.tres",
        BigIconPath: "res://images/atlases/power_atlas.sprites/ringing_power.tres");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature))
        {
            return;
        }

        int stunAmount = Amount;
        await PowerCmd.Remove(this);
        await PowerCmd.Apply<SluggishStunLimiterPower>(
            choiceContext,
            Owner,
            stunAmount,
            Owner,
            null);
    }
}

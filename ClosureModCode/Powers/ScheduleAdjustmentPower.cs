using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class ScheduleAdjustmentPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

    // Keep the legacy power compatible with the native next-turn energy timing.
    public override async Task AfterEnergyReset(Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature))
        {
            return;
        }

        if (Amount > 0)
        {
            Flash();
            await PlayerCmd.GainEnergy(Amount, player);
        }

        await PowerCmd.Remove(this);
    }
}

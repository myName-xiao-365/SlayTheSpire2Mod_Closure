using ClosureMod.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class EnergyDebtPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<string> RegisteredKeywordIds => [ClosureKeywords.DebtId];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

    public override async Task AfterEnergyReset(Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature))
        {
            return;
        }

        int energyPenalty = Math.Max(0, Amount);
        if (energyPenalty > 0)
        {
            await PlayerCmd.LoseEnergy(energyPenalty, player);
        }

        await PowerCmd.Remove(this);
    }
}

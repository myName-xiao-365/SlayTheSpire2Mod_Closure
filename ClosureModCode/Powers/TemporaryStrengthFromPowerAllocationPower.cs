using ClosureMod.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class TemporaryStrengthFromPowerAllocationPower
    : ModTemporaryAppliedPowerTemplate<PowerAllocation, StrengthPower>
{
    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/characters/energy.png",
        BigIconPath: $"{Entry.ResPath}/images/characters/energy.png");
}

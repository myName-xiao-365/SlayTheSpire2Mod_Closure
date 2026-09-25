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
    public override PowerAssetProfile AssetProfile
    {
        get
        {
            string iconName = Amount < 0
                ? "PowerAllocationStrengthDown.png"
                : "PowerAllocationStrengthUp.png";
            string iconPath = $"{Entry.ResPath}/images/powers/{iconName}";
            return new PowerAssetProfile(IconPath: iconPath, BigIconPath: iconPath);
        }
    }
}

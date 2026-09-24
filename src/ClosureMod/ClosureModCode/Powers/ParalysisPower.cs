using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class ParalysisPower : ModPowerTemplate
{
    public const int MaxStacks = 3;

    private int _reservedHits;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile
    {
        get
        {
            int iconNumber = Math.Clamp(Amount, 1, MaxStacks);
            string path = $"{Entry.ResPath}/images/powers/ParalysisPower{iconNumber}.png";
            return new PowerAssetProfile(IconPath: path, BigIconPath: path);
        }
    }

    public static ParalysisPower? Find(Creature? creature) =>
        creature?.Powers.OfType<ParalysisPower>().FirstOrDefault(power => power.Amount > 0);

    public bool TryReserveHit()
    {
        while (true)
        {
            int reserved = Volatile.Read(ref _reservedHits);
            if (Amount <= reserved)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref _reservedHits, reserved + 1, reserved) == reserved)
            {
                return true;
            }
        }
    }

    public void ReleaseHit() => Interlocked.Decrement(ref _reservedHits);
}

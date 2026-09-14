using ClosureMod.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class SluggishPower : ModPowerTemplate
{
    public const int StunThreshold = 10;
    public const decimal DamageLossPerStack = 0.05m;
    private const string StunnedVfxPath = "res://scenes/vfx/stunned_vfx.tscn";

    private static readonly HashSet<string> LockedBuffPowerNames =
    [
        "HardToKillPower",
        "HardenedShellPower",
        "MinionPower"
    ];

    private bool _resolvingSideEffects;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<string> RegisteredKeywordIds => [ClosureKeywords.SluggishId];

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png");

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!ReferenceEquals(dealer, Owner) || !props.IsPoweredAttack())
        {
            return 1m;
        }

        int stunThreshold = GetStunThreshold();
        int stacks = Math.Clamp(Amount, 0, stunThreshold - 1);
        return Math.Max(0m, 1m - stacks * DamageLossPerStack);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount <= 0 || _resolvingSideEffects)
        {
            return;
        }

        _resolvingSideEffects = true;
        try
        {
            await ApplySluggishSideEffects(choiceContext, amount, applier ?? Owner, cardSource);
        }
        finally
        {
            _resolvingSideEffects = false;
        }
    }

    private async Task ApplySluggishSideEffects(
        PlayerChoiceContext choiceContext,
        decimal appliedAmount,
        Creature applier,
        CardModel? cardSource)
    {
        if (Owner.IsMonster && appliedAmount > 0)
        {
            int reductions = (int)Math.Floor(appliedAmount);
            await ReduceRandomBuffs(choiceContext, reductions, applier, cardSource);
        }

        await ResolveStunThreshold(choiceContext, applier, cardSource);
    }

    private async Task ReduceRandomBuffs(
        PlayerChoiceContext choiceContext,
        int reductions,
        Creature applier,
        CardModel? cardSource)
    {
        for (int i = 0; i < reductions; i++)
        {
            List<PowerModel> buffs = Owner.Powers
                .Where(IsReducibleBuff)
                .ToList();
            if (buffs.Count == 0)
            {
                return;
            }

            PowerModel buff = buffs[Random.Shared.Next(buffs.Count)];
            await PowerCmd.ModifyAmount(choiceContext, buff, -1, applier, cardSource);
        }
    }

    private static bool IsReducibleBuff(PowerModel power)
    {
        if (power.Type != PowerType.Buff || power.Amount <= 0)
        {
            return false;
        }

        return !LockedBuffPowerNames.Contains(power.GetType().Name);
    }

    public async Task ResolveStunThreshold(
        PlayerChoiceContext choiceContext,
        Creature applier,
        CardModel? cardSource)
    {
        int stunThreshold = GetStunThreshold();
        int stunCount = Math.Max(0, Amount / stunThreshold);
        if (stunCount == 0)
        {
            return;
        }

        await PowerCmd.ModifyAmount(choiceContext, this, -stunCount * stunThreshold, applier, cardSource);

        for (int i = 0; i < stunCount; i++)
        {
            await TriggerStun(choiceContext, cardSource);
        }
    }

    private int GetStunThreshold()
    {
        int thresholdReduction = Owner.Powers
            .OfType<SluggishThresholdDownPower>()
            .Sum(power => Math.Max(0, power.Amount));
        return Math.Max(1, StunThreshold - thresholdReduction);
    }

    private async Task TriggerStun(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        if (Owner.IsMonster && Owner.Monster?.NextMove is { } nextMove)
        {
            await CreatureCmd.Stun(Owner, nextMove.Id);
            return;
        }

        VfxCmd.PlayOnCreature(Owner, StunnedVfxPath);
        SluggishStunLimiterPower? limiter = await PowerCmd.Apply<SluggishStunLimiterPower>(
            choiceContext,
            Owner,
            1,
            Owner,
            cardSource,
            true);
        if (cardSource is not null && limiter is not null)
        {
            limiter.IgnoreCurrentCardPlay();
        }
    }
}

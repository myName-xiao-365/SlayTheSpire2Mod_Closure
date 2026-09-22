using ClosureMod.Keywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class SluggishPower : ModPowerTemplate
{
    public const int StunThreshold = 10;
    public const decimal DamageLossPerStack = 0.05m;

    private static readonly HashSet<string> LockedBuffPowerNames =
    [
        "EscapeArtistPower",
        "HardToKillPower",
        "HardenedShellPower",
        "MinionPower",
        // These native encounter states must not be removed by Sluggish:
        // Sandpit (沙坑) and Asleep (沉睡) control the encounter itself.
        "SandpitPower",
        "AsleepPower"
    ];

    private bool _resolvingSideEffects;
    private int _pendingPlayerStunCount;
    private Creature? _pendingPlayerStunApplier;
    private CardModel? _pendingPlayerStunCardSource;
    private PlayerChoiceContext? _pendingPlayerStunChoiceContext;

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

        if (Owner.Powers.OfType<MerchantFormPower>().Any(power => power.Amount > 0))
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
            await SluggishGuardPower.ResolveSluggishApplied(choiceContext, amount, applier, cardSource);
            await ApplySluggishSideEffects(choiceContext, amount, applier ?? Owner, cardSource);
        }
        finally
        {
            _resolvingSideEffects = false;
        }
    }

    public override async Task AfterCardChangedPilesLate(
        CardModel card,
        PileType previousPileType,
        AbstractModel? source)
    {
        if (!Owner.IsPlayer ||
            previousPileType != PileType.Play ||
            _pendingPlayerStunCount <= 0 ||
            !ReferenceEquals(card, _pendingPlayerStunCardSource))
        {
            return;
        }

        await ResolvePendingPlayerStun(null);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!Owner.IsPlayer ||
            _pendingPlayerStunCount <= 0 ||
            !ReferenceEquals(cardPlay.Card, _pendingPlayerStunCardSource))
        {
            return;
        }

        await ResolvePendingPlayerStun(choiceContext);
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature) ||
            _pendingPlayerStunCount <= 0)
        {
            return;
        }

        await ResolvePendingPlayerStun(choiceContext);
    }

    private async Task ResolvePendingPlayerStun(PlayerChoiceContext? choiceContext)
    {
        int stunCount = _pendingPlayerStunCount;
        Creature applier = _pendingPlayerStunApplier ?? Owner;
        CardModel? cardSource = _pendingPlayerStunCardSource;
        PlayerChoiceContext? context = choiceContext ?? _pendingPlayerStunChoiceContext;
        if (context is null)
        {
            return;
        }

        _pendingPlayerStunCount = 0;
        _pendingPlayerStunApplier = null;
        _pendingPlayerStunCardSource = null;
        _pendingPlayerStunChoiceContext = null;

        await PowerCmd.ModifyAmount(context, this, -Amount, applier, cardSource);
        for (int i = 0; i < stunCount; i++)
        {
            await TriggerPlayerStunThisTurn(context, cardSource);
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

        int stacksToRemove = Owner.IsPlayer
            ? Amount
            : stunCount * stunThreshold;

        if (Owner.IsPlayer && cardSource is not null)
        {
            _pendingPlayerStunCount += stunCount;
            _pendingPlayerStunApplier = applier;
            _pendingPlayerStunCardSource = cardSource;
            _pendingPlayerStunChoiceContext = choiceContext;
            return;
        }

        await PowerCmd.ModifyAmount(choiceContext, this, -stacksToRemove, applier, cardSource);

        for (int i = 0; i < stunCount; i++)
        {
            await QueueOrTriggerStun(choiceContext, cardSource);
        }
    }

    public static async Task ClearSluggishAndStun(
        PlayerChoiceContext choiceContext,
        Creature target,
        Creature applier,
        CardModel? cardSource)
    {
        SluggishPower? sluggish = target.Powers
            .OfType<SluggishPower>()
            .FirstOrDefault();
        if (sluggish is not null)
        {
            await sluggish.ClearSluggishAndStun(choiceContext, applier, cardSource);
            return;
        }

        await TriggerStunWithoutSluggish(choiceContext, target, cardSource);
    }

    private async Task ClearSluggishAndStun(
        PlayerChoiceContext choiceContext,
        Creature applier,
        CardModel? cardSource)
    {
        if (Amount > 0)
        {
            await PowerCmd.ModifyAmount(choiceContext, this, -Amount, applier, cardSource);
        }

        if (SluggishStunLimiterPower.IsStunned(Owner))
        {
            return;
        }

        if (Owner.IsPlayer && cardSource is not null)
        {
            _pendingPlayerStunCount++;
            _pendingPlayerStunApplier = applier;
            _pendingPlayerStunCardSource = cardSource;
            _pendingPlayerStunChoiceContext = choiceContext;
            return;
        }

        await QueueOrTriggerStun(choiceContext, cardSource);
    }

    private static async Task TriggerStunWithoutSluggish(
        PlayerChoiceContext choiceContext,
        Creature target,
        CardModel? cardSource)
    {
        if (SluggishStunLimiterPower.IsStunned(target))
        {
            return;
        }

        if (target.IsMonster && target.Monster?.NextMove is { } nextMove)
        {
            await CreatureCmd.Stun(target, nextMove.Id);
            return;
        }

        await PowerCmd.Apply<SluggishStunLimiterPower>(
            choiceContext,
            target,
            1,
            target,
            cardSource,
            true);
        PlayStunnedVfx(target);
    }

    private int GetStunThreshold()
    {
        int thresholdReduction = Owner.Powers
            .OfType<SluggishThresholdDownPower>()
            .Sum(power => Math.Max(0, power.Amount));
        return Math.Max(1, StunThreshold - thresholdReduction);
    }

    private async Task QueueOrTriggerStun(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        if (Owner.IsMonster && Owner.Monster?.NextMove is { } nextMove)
        {
            await CreatureCmd.Stun(Owner, nextMove.Id);
            return;
        }

        if (Owner.IsPlayer)
        {
            await TriggerPlayerStunThisTurn(choiceContext, cardSource);
            return;
        }

        await TriggerStun(choiceContext, cardSource);
    }

    private async Task TriggerPlayerStunThisTurn(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        await PowerCmd.Apply<SluggishStunLimiterPower>(
            choiceContext,
            Owner,
            1,
            Owner,
            cardSource,
            true);
        PlayStunnedVfx(Owner);
    }

    private async Task TriggerStun(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        await PowerCmd.Apply<SluggishStunLimiterPower>(
            choiceContext,
            Owner,
            1,
            Owner,
            cardSource,
            true);

        PlayStunnedVfx(Owner);
    }

    private static void PlayStunnedVfx(Creature owner)
    {
        // The native factory binds the creature; generic VfxCmd only accepts scene-relative paths.
        if (owner.GetVfxContainer() is { } container)
        {
            container.AddChildSafely(NStunnedVfx.Create(owner));
        }
    }
}

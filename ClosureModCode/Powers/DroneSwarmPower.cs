using ClosureMod.Cards;
using ClosureMod.Summons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.AttackHits;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class DroneSwarmPower : ModPowerTemplate, IAttackHitHookListener
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;

    public bool HasAttackModule { get; set; }
    public bool AttackModuleHitsAllEnemies { get; set; }
    public bool HasDefenseModule { get; set; }
    public bool HasSupportModule { get; set; }
    public bool WaitForStartupAdded { get; set; }

    public int AttackModuleMaxHp { get; set; }
    public int DefenseModuleMaxHp { get; set; }
    public int SupportModuleMaxHp { get; set; }

    public int ModuleCount =>
        (HasAttackModule ? 1 : 0) +
        (HasDefenseModule ? 1 : 0) +
        (HasSupportModule ? 1 : 0);

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/summons/AttackModule.png",
        BigIconPath: $"{Entry.ResPath}/images/summons/AttackModule.png");

    public bool HasModule(DroneModuleKind kind)
    {
        return kind switch
        {
            DroneModuleKind.Attack => HasAttackModule,
            DroneModuleKind.Defense => HasDefenseModule,
            DroneModuleKind.Support => HasSupportModule,
            _ => false
        };
    }

    public void AddModule(DroneModuleKind kind, int amount)
    {
        amount = Math.Max(1, amount);
        switch (kind)
        {
            case DroneModuleKind.Attack:
                HasAttackModule = true;
                AttackModuleMaxHp += amount;
                break;
            case DroneModuleKind.Defense:
                HasDefenseModule = true;
                DefenseModuleMaxHp += amount;
                break;
            case DroneModuleKind.Support:
                HasSupportModule = true;
                SupportModuleMaxHp += amount;
                break;
        }
    }

    public void ClearModules()
    {
        HasAttackModule = false;
        AttackModuleHitsAllEnemies = false;
        HasDefenseModule = false;
        HasSupportModule = false;
        AttackModuleMaxHp = 0;
        DefenseModuleMaxHp = 0;
        SupportModuleMaxHp = 0;
        WaitForStartupAdded = false;
    }

    public async Task TryAddWaitForStartup(PlayerChoiceContext choiceContext, Player owner)
    {
        if (WaitForStartupAdded || !HasAttackModule || !HasDefenseModule || !HasSupportModule)
        {
            return;
        }

        if (owner.PlayerCombatState is null || owner.Creature.CombatState is not { } combatState)
        {
            return;
        }

        WaitForStartup card = combatState.CreateCard(ModelDb.Card<WaitForStartup>(), owner) as WaitForStartup
                              ?? owner.RunState.CreateCard<WaitForStartup>(owner);
        var result = await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        if (!result.success)
        {
            return;
        }

        WaitForStartupAdded = true;
        Flash();
    }

    public async Task AfterAttackHit(AttackHitContext context)
    {
        if (!HasAttackModule ||
            AttackModuleMaxHp <= 0 ||
            !ReferenceEquals(context.Dealer, Owner) ||
            context.CardSource is not { Type: CardType.Attack })
        {
            return;
        }

        List<Creature> targets = (AttackModuleHitsAllEnemies
                ? Owner.CombatState?.HittableEnemies
                : context.Targets)
            ?.Where(target => target is { IsAlive: true, IsEnemy: true })
            .Distinct()
            .ToList() ?? [];
        if (targets.Count <= 0)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            context.ChoiceContext,
            targets,
            AttackModuleMaxHp,
            ValueProp.Unpowered,
            Owner);
    }
}

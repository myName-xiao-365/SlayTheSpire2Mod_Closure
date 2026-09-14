using ClosureMod.Cards;
using ClosureMod.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Characters;

namespace ClosureMod.Summons;

public static class DroneSwarmManager
{
    private static bool _resolvingDefenseModule;
    private static bool _resolvingSupportModule;

    public static async Task<DroneSwarmPower?> Summon(
        PlayerChoiceContext choiceContext,
        Player owner,
        DroneModuleKind kind,
        int maxHp,
        CardModel? cardSource)
    {
        if (owner.Creature is null || maxHp <= 0)
        {
            return null;
        }

        DroneSwarmPower? swarm = owner.Creature.Powers.OfType<DroneSwarmPower>().FirstOrDefault();
        bool hadModule = swarm?.HasModule(kind) == true;
        if (swarm is null)
        {
            swarm = await PowerCmd.Apply<DroneSwarmPower>(
                choiceContext,
                owner.Creature,
                1,
                owner.Creature,
                cardSource,
                true);
        }

        if (swarm is null)
        {
            return null;
        }

        swarm.AddModule(kind, maxHp);
        if (!hadModule && swarm.Amount < swarm.ModuleCount)
        {
            await PowerCmd.ModifyAmount(choiceContext, swarm, swarm.ModuleCount - swarm.Amount, owner.Creature, cardSource);
        }

        await swarm.TryAddWaitForStartup(choiceContext, owner);
        return swarm;
    }

    public static async Task HandleBlockGained(
        ICombatState combatState,
        Creature creature,
        decimal amount,
        CardModel? cardSource)
    {
        if (_resolvingDefenseModule || amount <= 0 || creature.Player is not { } player)
        {
            return;
        }

        DroneSwarmPower? swarm = creature.Powers.OfType<DroneSwarmPower>().FirstOrDefault();
        if (swarm is not { HasDefenseModule: true } || swarm.DefenseModuleMaxHp <= 0)
        {
            return;
        }

        _resolvingDefenseModule = true;
        try
        {
            swarm.Flash();
            await CreatureCmd.GainBlock(creature, (decimal)swarm.DefenseModuleMaxHp, ValueProp.Unpowered, null);
            await swarm.TryAddWaitForStartup(new ThrowingPlayerChoiceContext(), player);
        }
        finally
        {
            _resolvingDefenseModule = false;
        }
    }

    public static async Task HandleCurrentHpChanged(
        ICombatState? combatState,
        Creature creature,
        decimal delta)
    {
        if (_resolvingSupportModule || delta >= 0 || creature.Player is not { } player)
        {
            return;
        }

        DroneSwarmPower? swarm = creature.Powers.OfType<DroneSwarmPower>().FirstOrDefault();
        if (swarm is not { HasSupportModule: true } || swarm.SupportModuleMaxHp <= 0 || creature.IsDead)
        {
            return;
        }

        _resolvingSupportModule = true;
        try
        {
            swarm.Flash();
            await CreatureCmd.Heal(creature, swarm.SupportModuleMaxHp);
            await swarm.TryAddWaitForStartup(new ThrowingPlayerChoiceContext(), player);
        }
        finally
        {
            _resolvingSupportModule = false;
        }
    }
}

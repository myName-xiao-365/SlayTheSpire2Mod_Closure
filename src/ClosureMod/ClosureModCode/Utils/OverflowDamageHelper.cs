using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ClosureMod.Utils;

public static class OverflowDamageHelper
{
    private static readonly HashSet<string> DamageLimitPowerNames =
    [
        "HardToKillPower",
        "HardenedShellPower"
    ];

    public static async Task DealOverflowBounceAttack(
        PlayerChoiceContext choiceContext,
        CardModel source,
        Creature initialTarget,
        decimal damage,
        bool powerFirstHit = true)
    {
        ICombatState? combatState = initialTarget.CombatState ?? source.CombatState;
        if (combatState is null || damage <= 0 || !IsValidEnemyTarget(initialTarget))
        {
            return;
        }

        Creature target = initialTarget;
        decimal remainingDamage = damage;
        bool isFirstHit = true;

        while (remainingDamage > 0 && IsValidEnemyTarget(target))
        {
            int beforeDurability = GetDurability(target);
            AttackCommand attack = DamageCmd.Attack(remainingDamage)
                .FromCard(source)
                .Targeting(target);
            if (!isFirstHit || !powerFirstHit)
            {
                attack.Unpowered();
            }

            attack = await attack.Execute(choiceContext);

            int actualTaken = Math.Max(0, beforeDurability - GetDurability(target));
            if (actualTaken <= 0)
            {
                return;
            }

            decimal overflowDamage = GetOverflowDamage(attack, target, remainingDamage, actualTaken);
            if (overflowDamage <= 0)
            {
                return;
            }

            List<Creature> candidates = combatState.HittableEnemies
                .Where(enemy => !ReferenceEquals(enemy, target) && IsValidEnemyTarget(enemy))
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            target = candidates[Random.Shared.Next(candidates.Count)];
            remainingDamage = overflowDamage;
            isFirstHit = false;
        }
    }

    private static decimal GetOverflowDamage(
        AttackCommand attack,
        Creature target,
        decimal attemptedDamage,
        int actualTaken)
    {
        int overkillDamage = attack.Results
            .SelectMany(static hit => hit)
            .Where(result => ReferenceEquals(result.Receiver, target))
            .Sum(result => Math.Max(0, result.OverkillDamage));
        if (overkillDamage > 0)
        {
            return overkillDamage;
        }

        if (HasDamageLimitPower(target))
        {
            return Math.Max(0m, attemptedDamage - actualTaken);
        }

        return 0m;
    }

    private static int GetDurability(Creature creature)
    {
        return Math.Max(0, creature.CurrentHp) + Math.Max(0, creature.Block);
    }

    private static bool IsValidEnemyTarget(Creature creature)
    {
        return creature is { IsAlive: true, IsEnemy: true, IsHittable: true };
    }

    private static bool HasDamageLimitPower(Creature creature)
    {
        return creature.Powers.Any(power => DamageLimitPowerNames.Contains(power.GetType().Name));
    }
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class SluggishStunLimiterPower : ModPowerTemplate
{
    private int _cardPlaysAtApplication;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/power_atlas.sprites/ringing_power.tres",
        BigIconPath: "res://images/atlases/power_atlas.sprites/ringing_power.tres");

    public bool BlocksCardPlay => CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
        entry.HappenedThisTurn(CombatState) &&
        !entry.CardPlay.IsAutoPlay &&
        ReferenceEquals(entry.CardPlay.Card.Owner?.Creature, Owner)) > _cardPlaysAtApplication;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        // Include the triggering play even if its completion hook is still running.
        _cardPlaysAtApplication = CombatManager.Instance.History.CardPlaysStarted.Count(entry =>
            entry.HappenedThisTurn(CombatState) &&
            !entry.CardPlay.IsAutoPlay &&
            ReferenceEquals(entry.CardPlay.Card.Owner?.Creature, Owner));

        if (Owner.Player?.PlayerCombatState is { } state)
        {
            foreach (CardModel card in state.AllCards.ToArray())
            {
                await AfterCardEnteredCombat(card);
            }
        }
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (ReferenceEquals(card.Owner?.Creature, Owner) && card.Affliction is null &&
            card is not ClosureMod.Cards.DontWantToWork)
        {
            await CardCmd.Afflict<Ringing>(card, 1);
        }
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        if (!oldOwner.Powers.OfType<RingingPower>().Any() && oldOwner.Player?.PlayerCombatState is { } state)
        {
            foreach (CardModel card in state.AllCards.ToArray())
            {
                if (card.Affliction is Ringing)
                {
                    CardCmd.ClearAffliction(card);
                }
            }
        }

        return Task.CompletedTask;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        return !ReferenceEquals(card.Owner?.Creature, Owner) ||
               autoPlayType != AutoPlayType.None ||
               card is ClosureMod.Cards.DontWantToWork ||
               !BlocksCardPlay;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
        {
            await PowerCmd.Remove(this);
        }
    }

    public static SluggishStunLimiterPower? FindLimiter(CardModel card)
    {
        return card.Owner?.Creature.Powers.OfType<SluggishStunLimiterPower>().FirstOrDefault();
    }

    public static RingingPower? FindRinging(CardModel card)
    {
        return card.Owner?.Creature.Powers.OfType<RingingPower>().FirstOrDefault();
    }

    public static bool IsBlockedByStun(CardModel card)
    {
        if (card.GetType().Name == "DontWantToWork")
        {
            return false;
        }

        SluggishStunLimiterPower? limiter = FindLimiter(card);
        if (limiter?.BlocksCardPlay == true)
        {
            return true;
        }

        RingingPower? ringing = FindRinging(card);
        return ringing is not null && !ringing.ShouldPlay(card, AutoPlayType.Default);
    }

    public static bool IsStunned(CardModel card)
    {
        return card.Owner?.Creature is { } creature && IsStunned(creature);
    }

    public static bool IsStunned(Creature creature)
    {
        return creature.IsStunned ||
               creature.Powers.OfType<RingingPower>().Any() ||
               creature.Powers.OfType<SluggishStunLimiterPower>().Any();
    }
}

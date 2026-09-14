using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class SluggishStunLimiterPower : ModPowerTemplate
{
    private bool _ignoreNextFinishedCardPlay;
    private int _cardsPlayedAfterStun;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/characters/energy.png",
        BigIconPath: $"{Entry.ResPath}/images/characters/energy.png");

    public bool BlocksCardPlay => _cardsPlayedAfterStun >= 1;

    public void IgnoreCurrentCardPlay()
    {
        _ignoreNextFinishedCardPlay = true;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card.Owner?.Creature, Owner))
        {
            return Task.CompletedTask;
        }

        if (_ignoreNextFinishedCardPlay)
        {
            _ignoreNextFinishedCardPlay = false;
            return Task.CompletedTask;
        }

        _cardsPlayedAfterStun++;
        Flash();
        return Task.CompletedTask;
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
}

using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class InterestCompoundingPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/InterestCompoundingPower.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/InterestCompoundingPower.png");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner.Player;
        if (player is null || cardPlay.Card.Owner != player)
        {
            return;
        }

        if ((player.PlayerCombatState?.Energy ?? 0) > 0)
        {
            return;
        }

        List<Creature> targets = Owner.CombatState?.HittableEnemies.ToList() ?? [];
        if (targets.Count <= 0 || Amount <= 0)
        {
            return;
        }

        Creature? target = player.RunState.Rng.CombatTargets.NextItem(targets);
        if (target is null)
        {
            return;
        }

        Flash();
        await CreatureCmd.Damage(
            choiceContext,
            target,
            Amount,
            ValueProp.Unpowered,
            Owner);
    }
}

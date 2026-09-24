using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ClosureMod.Powers;

[RegisterPower]
public sealed class EmberPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/cards/Ember.png",
        BigIconPath: $"{Entry.ResPath}/images/cards/Ember.png");

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!ReferenceEquals(target, Owner))
        {
            return amount;
        }

        return Math.Min(amount, Math.Max(0m, Owner.CurrentHp - 1m));
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!ReferenceEquals(Owner, player.Creature))
        {
            return;
        }

        if (Amount > 1)
        {
            await PowerCmd.Decrement(this);
            return;
        }

        await PowerCmd.Remove(this);
        if (!Owner.IsDead)
        {
            await CreatureCmd.Kill(Owner, force: false);
        }
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (ReferenceEquals(Owner, creature))
        {
            await PowerCmd.Remove(this);
        }
    }
}
